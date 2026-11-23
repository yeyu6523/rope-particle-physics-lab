using System.Numerics;

namespace PhysicsLab.Core;

public enum RopePreset { Bridge, Pendulum, Free }

public sealed class RopeNode(Vector2 position, bool pinned)
{
    public Vector2 Position = position;
    public Vector2 Previous = position;
    public Vector2 Anchor = position;
    public bool Pinned = pinned;
}

public sealed class RopeLink(int a, int b, float length)
{
    public int A { get; } = a;
    public int B { get; } = b;
    public float Length { get; } = length;
    public bool Active { get; set; } = true;
}

public sealed class Obstacle(Vector2 center, float radius)
{
    public Vector2 Center { get; set; } = center;
    public float Radius { get; } = radius;
}

/// <summary>Fixed-step Verlet integration with iterative position constraints (PBD).</summary>
public sealed class RopeSimulation
{
    public const float Width = 1200, Height = 760, Floor = 690, NodeRadius = 4;
    public List<RopeNode> Nodes { get; } = [];
    public List<RopeLink> Links { get; } = [];
    public List<Obstacle> Obstacles { get; } = [];
    public float Gravity { get; set; } = 900;
    public float Wind { get; set; }
    public int Iterations { get; set; } = 36;
    public int SegmentCount { get; set; } = 64;
    public bool Collisions { get; set; } = true;
    public RopePreset Preset { get; private set; }
    public int DraggedNode { get; private set; } = -1;
    public float Time { get; private set; }
    private Vector2 dragPosition;

    public RopeSimulation() => Reset(RopePreset.Bridge);

    public void Reset(RopePreset preset)
    {
        Preset = preset;
        Nodes.Clear(); Links.Clear(); Obstacles.Clear();
        DraggedNode = -1; Time = 0;
        Obstacles.Add(new(new(550, 470), 92));
        Obstacles.Add(new(new(850, 555), 61));
        int count = Math.Clamp(SegmentCount, 12, 128);
        float length = preset == RopePreset.Pendulum ? 490 : 1000;
        for (int i = 0; i <= count; i++)
        {
            float t = (float)i / count;
            Vector2 point = preset switch
            {
                RopePreset.Pendulum => new(460 + t * 420, 130 + t * 230),
                RopePreset.Free => new(150 + t * 900, 170 + MathF.Sin(t * MathF.PI * 3) * 55),
                _ => new(170 + t * 860, 155 + MathF.Sin(t * MathF.PI) * 180)
            };
            bool pinned = preset == RopePreset.Bridge ? i == 0 || i == count
                : preset == RopePreset.Pendulum && i == 0;
            Nodes.Add(new(point, pinned));
            if (i > 0) Links.Add(new(i - 1, i, length / count));
        }
    }

    public void Step(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0 || dt > 0.05f)
            throw new ArgumentOutOfRangeException(nameof(dt));
        Time += dt;
        for (int i = 0; i < Nodes.Count; i++)
        {
            var node = Nodes[i];
            if (IsFixed(i)) { SetFixed(i); continue; }
            Vector2 velocity = (node.Position - node.Previous) * 0.997f;
            node.Previous = node.Position;
            float gust = Wind * (1 + 0.22f * MathF.Sin(Time * 1.7f + i * 0.13f));
            node.Position += velocity + new Vector2(gust, Gravity) * dt * dt;
        }

        for (int iteration = 0; iteration < Math.Clamp(Iterations, 4, 80); iteration++)
        {
            // Alternate traversal order to reduce directional bias.
            for (int j = 0; j < Links.Count; j++)
            {
                var link = Links[iteration % 2 == 0 ? j : Links.Count - 1 - j];
                if (!link.Active) continue;
                var a = Nodes[link.A]; var b = Nodes[link.B];
                Vector2 delta = b.Position - a.Position;
                float distance = delta.Length();
                if (distance < 0.0001f) continue;
                float wa = IsFixed(link.A) ? 0 : 1, wb = IsFixed(link.B) ? 0 : 1;
                if (wa + wb == 0) continue;
                Vector2 correction = delta * ((distance - link.Length) / distance / (wa + wb));
                a.Position += correction * wa;
                b.Position -= correction * wb;
            }
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (IsFixed(i)) { SetFixed(i); continue; }
                Constrain(Nodes[i]);
            }
        }
    }

    private bool IsFixed(int index) => Nodes[index].Pinned || index == DraggedNode;
    private void SetFixed(int index)
    {
        var node = Nodes[index];
        node.Position = index == DraggedNode ? dragPosition : node.Anchor;
        node.Previous = node.Position;
    }

    private void Constrain(RopeNode node)
    {
        node.Position.X = Math.Clamp(node.Position.X, 28, Width - 28);
        node.Position.Y = Math.Clamp(node.Position.Y, 50, Floor - NodeRadius);
        if (!Collisions) return;
        foreach (var obstacle in Obstacles)
        {
            Vector2 delta = node.Position - obstacle.Center;
            float radius = obstacle.Radius + NodeRadius;
            if (delta.LengthSquared() < radius * radius)
            {
                Vector2 normal = delta.LengthSquared() < 0.0001f ? -Vector2.UnitY : Vector2.Normalize(delta);
                node.Position = obstacle.Center + normal * radius;
            }
        }
    }

    public bool BeginDrag(Vector2 position)
    {
        float nearest = 32 * 32;
        int found = -1;
        for (int i = 0; i < Nodes.Count; i++)
        {
            float distance = Vector2.DistanceSquared(position, Nodes[i].Position);
            if (distance < nearest) { nearest = distance; found = i; }
        }
        if (found < 0) return false;
        DraggedNode = found;
        MoveDrag(position);
        return true;
    }

    public void MoveDrag(Vector2 position)
    {
        if (DraggedNode < 0) return;
        dragPosition = new(Math.Clamp(position.X, 28, Width - 28), Math.Clamp(position.Y, 55, Floor - 8));
        var node = Nodes[DraggedNode];
        if (node.Pinned) node.Anchor = dragPosition;
        SetFixed(DraggedNode);
    }

    public void EndDrag() => DraggedNode = -1;

    public bool Cut(Vector2 position, float radius = 22)
    {
        RopeLink? nearest = null;
        float best = radius;
        foreach (var link in Links)
        {
            if (!link.Active) continue;
            float distance = DistanceToSegment(position, Nodes[link.A].Position, Nodes[link.B].Position);
            if (distance < best) { best = distance; nearest = link; }
        }
        if (nearest is null) return false;
        nearest.Active = false;
        return true;
    }

    public float MaxRelativeError()
    {
        float error = 0;
        foreach (var link in Links.Where(link => link.Active))
            error = Math.Max(error, MathF.Abs(Vector2.Distance(Nodes[link.A].Position, Nodes[link.B].Position) / link.Length - 1));
        return error;
    }

    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        float t = d.LengthSquared() < 0.00001f ? 0 : Math.Clamp(Vector2.Dot(p - a, d) / d.LengthSquared(), 0, 1);
        return Vector2.Distance(p, a + t * d);
    }
}
