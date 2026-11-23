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
    private float previousSubstep;

    public RopeSimulation() => Reset(RopePreset.Bridge);

    public void Reset(RopePreset preset)
    {
        Preset = preset;
        Nodes.Clear(); Links.Clear(); Obstacles.Clear();
        DraggedNode = -1; Time = 0; previousSubstep = 0;
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
        // Small integration steps prevent the apparent springiness of a long
        // chain whose tension cannot propagate through a single local solve.
        int iterations = Math.Clamp(Iterations, 4, 80);
        float resolutionScale = Math.Max(1, (Nodes.Count - 1) / 64f);
        float iterationScale = Math.Max(1, MathF.Sqrt(36f / iterations));
        int substeps = Math.Max(1, (int)MathF.Ceiling(dt * 960 * resolutionScale * iterationScale));
        float h = dt / substeps;
        float damping = MathF.Pow(0.99f, h * 120);
        float startTime = Time;
        Time += dt;
        for (int substep = 0; substep < substeps; substep++)
        {
            float substepTime = startTime + h * (substep + 1);
            float stepRatio = previousSubstep > 0 ? h / previousSubstep : 1;
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if (IsFixed(i)) { SetFixed(i); continue; }
                Vector2 velocity = (node.Position - node.Previous) * (damping * stepRatio);
                node.Previous = node.Position;
                float gust = Wind * (1 + 0.22f * MathF.Sin(substepTime * 1.7f + i * 0.13f));
                node.Position += velocity + new Vector2(gust, Gravity) * h * h;
            }
            SolveConstraints(iterations);
            previousSubstep = h;
        }
    }

    private void SolveConstraints(int iterations, int first = 0, int last = -1)
    {
        if (last < 0) last = Nodes.Count - 1;
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            // Alternate traversal order to reduce directional bias.
            for (int j = first; j < last; j++)
            {
                var link = Links[iteration % 2 == 0 ? j : last - 1 - (j - first)];
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
            for (int i = first; i <= last; i++)
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
        Vector2 target = new(Math.Clamp(position.X, 28, Width - 28), Math.Clamp(position.Y, 55, Floor - 8));
        dragPosition = ReachableDragPosition(target);
        // Moving a mouse target is a geometric edit, not a spring impulse.
        // Preserve each free node's velocity when distributing this displacement.
        var before = Nodes.Select(node => node.Position).ToArray();
        var node = Nodes[DraggedNode];
        if (node.Pinned) node.Anchor = dragPosition;
        SetFixed(DraggedNode);
        int first = DraggedNode, last = DraggedNode;
        while (first > 0 && Links[first - 1].Active) first--;
        while (last < Nodes.Count - 1 && Links[last].Active) last++;
        SolveConstraints(Math.Max(128, Math.Clamp(Iterations, 4, 80) * 8), first, last);
        for (int i = first; i <= last; i++)
            if (!IsFixed(i)) Nodes[i].Previous += Nodes[i].Position - before[i];
    }

    private Vector2 ReachableDragPosition(Vector2 target)
    {
        var limits = new List<(Vector2 Center, float Radius)>();
        // Only the first pin in each connected direction limits this node.
        // A cut must stop the traversal; disconnected pins exert no constraint.
        foreach (int direction in new[] { -1, 1 })
        {
            float length = 0;
            for (int i = DraggedNode; i + direction >= 0 && i + direction < Nodes.Count; i += direction)
            {
                var link = Links[direction < 0 ? i - 1 : i];
                if (!link.Active) break;
                length += link.Length;
                if (!Nodes[i + direction].Pinned) continue;
                limits.Add((Nodes[i + direction].Anchor, length));
                break;
            }
        }
        foreach (var limit in limits)
        {
            Vector2 delta = target - limit.Center;
            if (delta.LengthSquared() > limit.Radius * limit.Radius)
                target = limit.Center + Vector2.Normalize(delta) * limit.Radius;
        }
        // Project to the intersection of both reach disks, including cases
        // where projecting onto the second disk leaves the first disk again.
        if (limits.Count == 2 && Vector2.DistanceSquared(target, limits[0].Center) > limits[0].Radius * limits[0].Radius + 0.01f)
        {
            var a = limits[0]; var b = limits[1];
            Vector2 delta = b.Center - a.Center;
            float d = delta.Length();
            if (d > 0.0001f)
            {
                Vector2 axis = delta / d;
                float along = (a.Radius * a.Radius - b.Radius * b.Radius + d * d) / (2 * d);
                float height = MathF.Sqrt(MathF.Max(0, a.Radius * a.Radius - along * along));
                Vector2 center = a.Center + axis * along;
                Vector2 offset = new Vector2(-axis.Y, axis.X) * height;
                target = Vector2.DistanceSquared(target, center + offset) < Vector2.DistanceSquared(target, center - offset)
                    ? center + offset : center - offset;
            }
        }
        return target;
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
