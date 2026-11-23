using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using PhysicsLab.Core;

const float Dt = 1f / 120;
var results = new List<object>();
int failures = 0;
void Assert(bool success, string message)
{
    if (!success) throw new InvalidOperationException(message);
}
void Test(string name, Action action)
{
    var watch = Stopwatch.StartNew();
    try { action(); results.Add(new { name, passed = true, milliseconds = watch.ElapsedMilliseconds }); Console.WriteLine("PASS " + name); }
    catch (Exception error) { failures++; results.Add(new { name, passed = false, error = error.Message }); Console.WriteLine("FAIL " + name + ": " + error.Message); }
}
bool Finite(Vector2 vector) => float.IsFinite(vector.X) && float.IsFinite(vector.Y);

Test("rope anchors stay fixed and constraints settle", () =>
{
    var rope = new RopeSimulation();
    var left = rope.Nodes[0].Position; var right = rope.Nodes[^1].Position;
    for (int i = 0; i < 2400; i++) rope.Step(Dt);
    Assert(rope.Nodes[0].Position == left && rope.Nodes[^1].Position == right, "anchor drift");
    Assert(rope.MaxRelativeError() < 0.035f, $"constraint relative error {rope.MaxRelativeError():P2}");
    Assert(rope.Nodes.All(n => Finite(n.Position) && n.Position.Y <= RopeSimulation.Floor), "invalid or below floor");
});
Test("rope respects circular collisions", () =>
{
    var rope = new RopeSimulation();
    for (int i = 0; i < 1200; i++) rope.Step(Dt);
    foreach (var node in rope.Nodes.Where(n => !n.Pinned))
        foreach (var obstacle in rope.Obstacles)
            Assert(Vector2.Distance(node.Position, obstacle.Center) >= obstacle.Radius + RopeSimulation.NodeRadius - 0.02f, "circle penetration");
});
Test("rope cutting disconnects and reset repairs topology", () =>
{
    var rope = new RopeSimulation();
    var link = rope.Links[30];
    Assert(rope.Cut((rope.Nodes[link.A].Position + rope.Nodes[link.B].Position) / 2), "cut missed");
    Assert(rope.Links.Count(l => !l.Active) == 1, "wrong cut count");
    for (int i = 0; i < 600; i++) rope.Step(Dt);
    Assert(Vector2.Distance(rope.Nodes[link.A].Position, rope.Nodes[link.B].Position) > link.Length * 2, "cut did not separate");
    rope.Reset(rope.Preset);
    Assert(rope.Links.All(l => l.Active), "reset left a cut");
});
Test("dragged anchors persist and free nodes are released", () =>
{
    var rope = new RopeSimulation();
    Assert(rope.BeginDrag(rope.Nodes[0].Position), "anchor grab");
    rope.MoveDrag(new(300, 180)); rope.EndDrag(); rope.Step(Dt);
    Assert(rope.Nodes[0].Position == new Vector2(300, 180), "anchor location");
    Assert(rope.BeginDrag(rope.Nodes[32].Position), "node grab");
    rope.MoveDrag(new(620, 220)); rope.EndDrag();
    for (int i = 0; i < 120; i++) rope.Step(Dt);
    Assert(Vector2.Distance(rope.Nodes[32].Position, new(620, 220)) > 20, "node stayed pinned");
});
Test("all rope presets remain finite at maximum wind", () =>
{
    foreach (var preset in Enum.GetValues<RopePreset>())
    {
        var rope = new RopeSimulation { Gravity = 1800, Wind = 800, SegmentCount = 100, Iterations = 8 };
        rope.Reset(preset);
        for (int i = 0; i < 1200; i++) rope.Step(Dt);
        Assert(rope.Nodes.All(n => Finite(n.Position)), "nonfinite in " + preset);
    }
});
Test("disabling obstacles removes circle projection", () =>
{
    var rope = new RopeSimulation { Collisions = false, Gravity = 0 };
    rope.Reset(RopePreset.Free);
    foreach (var link in rope.Links) link.Active = false;
    var point = rope.Obstacles[0].Center;
    rope.Nodes[10].Position = rope.Nodes[10].Previous = point;
    rope.Step(Dt);
    Assert(Vector2.Distance(rope.Nodes[10].Position, point) < 0.01, "disabled obstacle still projects");
    rope.Collisions = true; rope.Step(Dt);
    Assert(Vector2.Distance(rope.Nodes[10].Position, point) >= 95.9f, "enabled obstacle did not project");
});
Test("particle emission follows elapsed time", () =>
{
    var particles = new ParticleSimulation { EmissionRate = 240, Lifetime = 8 };
    for (int i = 0; i < 120; i++) particles.Step(Dt);
    Assert(Math.Abs(particles.Count - 240) <= 1, $"count {particles.Count}");
});
Test("expired particles are recycled", () =>
{
    var particles = new ParticleSimulation { EmissionRate = 200, Lifetime = 0.1f };
    for (int i = 0; i < 60; i++) particles.Step(Dt);
    Assert(particles.Count > 0 && particles.Count < 30, "lifetime not limiting count");
    particles.EmissionRate = 0;
    for (int i = 0; i < 240; i++) particles.Step(Dt);
    Assert(particles.Count == 0, "dead particles remain");
});
Test("all particle presets remain finite for 30 seconds", () =>
{
    foreach (var preset in Enum.GetValues<ParticlePreset>())
    {
        var particles = new ParticleSimulation(); particles.Reset(preset);
        for (int i = 0; i < 3600; i++) particles.Step(Dt);
        Assert(particles.Count > 0 && particles.Count <= ParticleSimulation.Capacity, "particle count " + preset);
        foreach (var p in particles.Particles)
            Assert(Finite(p.Position) && Finite(p.Velocity) && p.Age < p.Lifetime, "invalid particle " + preset);
    }
});
Test("firework rockets trigger secondary sparks", () =>
{
    var particles = new ParticleSimulation(); particles.Reset(ParticlePreset.Fireworks);
    for (int i = 0; i < 240; i++) particles.Step(Dt);
    Assert(particles.Particles.ToArray().Any(p => p.Kind == ParticleKind.Spark), "no explosion sparks");
});
Test("moving the firework emitter changes launch origin", () =>
{
    var particles = new ParticleSimulation(); particles.Reset(ParticlePreset.Fireworks);
    particles.Emitter = new(100, 450); particles.Step(Dt);
    Assert(particles.Particles[0].Position.X < 300 && particles.Particles[0].Position.Y < 500, "emitter ignored");
});
Test("fountain bounces on the floor", () =>
{
    var particles = new ParticleSimulation { Emitter = new(600, 675), Speed = 100, Gravity = 1200, Lifetime = 8 };
    bool bounced = false;
    for (int i = 0; i < 300; i++)
    {
        particles.Step(Dt);
        foreach (var p in particles.Particles)
            if (p.Age > 0.2f && p.Position.Y > 650 && p.Velocity.Y < -5) bounced = true;
    }
    Assert(bounced, "no upward velocity following floor contact");
});
Test("particle pool is bounded under repeated bursts", () =>
{
    var particles = new ParticleSimulation();
    for (int i = 0; i < 500; i++) particles.Burst(new(600, 300));
    Assert(particles.Count == ParticleSimulation.Capacity, "capacity guard");
    particles.Reset(ParticlePreset.Snow);
    Assert(particles.Count == 0, "reset did not clear pool");
});
Test("reset restores deterministic simulation", () =>
{
    var particles = new ParticleSimulation();
    for (int i = 0; i < 120; i++) particles.Step(Dt);
    Particle[] first = particles.Particles.ToArray();
    particles.Reset(ParticlePreset.Fountain);
    for (int i = 0; i < 120; i++) particles.Step(Dt);
    Assert(first.SequenceEqual(particles.Particles.ToArray()), "reset differs from seeded baseline");
});
Test("invalid timesteps are rejected", () =>
{
    bool ropeRejected = false, particleRejected = false;
    try { new RopeSimulation().Step(float.NaN); } catch (ArgumentOutOfRangeException) { ropeRejected = true; }
    try { new ParticleSimulation().Step(-1); } catch (ArgumentOutOfRangeException) { particleRejected = true; }
    Assert(ropeRejected && particleRejected, "invalid dt accepted");
});

Console.WriteLine($"{results.Count - failures}/{results.Count} passed");
if (args.Length == 2 && args[0] == "--report")
{
    string path = Path.GetFullPath(args[1]);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, JsonSerializer.Serialize(new { passed = failures == 0, total = results.Count, failures, results }, new JsonSerializerOptions { WriteIndented = true }));
}
return failures == 0 ? 0 : 1;
