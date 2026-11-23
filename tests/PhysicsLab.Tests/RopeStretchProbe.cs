using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using PhysicsLab.Core;

/// <summary>Reproducible stretch measurements; percentages exclude compression.</summary>
internal static class RopeStretchProbe
{
    public static void Run(string reportPath)
    {
        const float dt = 1f / 120;
        var results = new List<object>();
        float Stretch(RopeSimulation rope) => rope.Links.Where(l => l.Active)
            .Max(l => MathF.Max(0, Vector2.Distance(rope.Nodes[l.A].Position, rope.Nodes[l.B].Position) / l.Length - 1));
        float Total(RopeSimulation rope) => rope.Links.Where(l => l.Active)
            .Sum(l => Vector2.Distance(rope.Nodes[l.A].Position, rope.Nodes[l.B].Position));
        foreach (var preset in new[] { RopePreset.Bridge, RopePreset.Pendulum })
        foreach (bool stress in new[] { false, true })
        {
            var rope = new RopeSimulation
            {
                SegmentCount = stress ? 100 : 64, Iterations = stress ? 8 : 36,
                Gravity = stress ? 1800 : 900, Wind = stress ? 800 : 0
            };
            rope.Reset(preset);
            var watch = Stopwatch.StartNew();
            float peak = 0;
            for (int i = 0; i < 1200; i++) { rope.Step(dt); peak = MathF.Max(peak, Stretch(rope)); }
            results.Add(new
            {
                scenario = preset + (stress ? "-stress" : "-default"), peakStretch = peak,
                settledStretch = Stretch(rope), totalLength = Total(rope), elapsedMs = watch.ElapsedMilliseconds
            });
        }
        foreach (bool jump in new[] { false, true })
        {
            var rope = new RopeSimulation { Collisions = false };
            for (int i = 0; i < 600; i++) rope.Step(dt);
            int index = rope.Nodes.Count / 2;
            Vector2 start = rope.Nodes[index].Position, target = new(600, 670);
            if (!rope.BeginDrag(start)) throw new InvalidOperationException("drag missed");
            float peak = 0;
            for (int i = 1; i <= 120; i++)
            {
                rope.MoveDrag(Vector2.Lerp(start, target, jump ? 1 : i / 120f));
                rope.Step(dt); peak = MathF.Max(peak, Stretch(rope));
            }
            Vector2 held = rope.Nodes[index].Position;
            float heldStretch = Stretch(rope), heldLength = Total(rope);
            rope.EndDrag();
            float releasePeak = 0;
            for (int i = 0; i < 600; i++) { rope.Step(dt); releasePeak = MathF.Max(releasePeak, Stretch(rope)); }
            results.Add(new
            {
                scenario = jump ? "drag-jump-unreachable" : "drag-slow-unreachable", peakStretch = peak,
                heldStretch, totalLength = heldLength, heldX = held.X, heldY = held.Y, releasePeak
            });
        }
        string path = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        Console.WriteLine(json);
    }
}
