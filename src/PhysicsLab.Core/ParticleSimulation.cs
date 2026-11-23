using System.Numerics;

namespace PhysicsLab.Core;

public enum ParticlePreset { Fountain, Fireworks, Snow, Vortex }
public enum ParticleKind { Normal, Rocket, Spark }

public struct Particle
{
    public Vector2 Position, Previous, Velocity;
    public float Age, Lifetime, Size, Hue;
    public ParticleKind Kind;
}

/// <summary>Bounded particle pool; semi-implicit Euler; seeded, reproducible random spawning.</summary>
public sealed class ParticleSimulation
{
    public const int Capacity = 10000;
    private readonly Particle[] pool = new Particle[Capacity];
    private Random random = new(2026);
    private float emissionDebt, rocketClock;
    public int Count { get; private set; }
    public ReadOnlySpan<Particle> Particles => pool.AsSpan(0, Count);
    public ParticlePreset Preset { get; private set; }
    public Vector2 Emitter { get; set; } = new(600, 625);
    public float EmissionRate { get; set; } = 420;
    public float Gravity { get; set; } = 420;
    public float Wind { get; set; }
    public float Lifetime { get; set; } = 3.5f;
    public float Speed { get; set; } = 400;
    public bool Trails { get; set; } = true;
    public float Time { get; private set; }

    public ParticleSimulation() => Reset(ParticlePreset.Fountain);

    public void Reset(ParticlePreset preset)
    {
        Preset = preset; Count = 0; Time = 0; emissionDebt = 0; rocketClock = 0;
        random = new(2026);
        Emitter = preset == ParticlePreset.Vortex ? new(600, 370) : new(600, 625);
    }

    private float Range(float low, float high) => low + random.NextSingle() * (high - low);

    public void Step(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0 || dt > 0.05f)
            throw new ArgumentOutOfRangeException(nameof(dt));
        Time += dt;
        if (Preset == ParticlePreset.Fireworks)
        {
            // Emission-rate controls launches per second; spark counts remain bounded.
            rocketClock -= dt;
            if (rocketClock <= 0)
            {
                rocketClock += 150 / Math.Clamp(EmissionRate, 30, 2000);
                Add(new Particle { Position = new(Math.Clamp(Emitter.X + Range(-180, 180), 40, 1160), Math.Clamp(Emitter.Y + 45, 110, 675)),
                    Velocity = new(Range(-80, 80), -Speed * Range(1.05f, 1.35f)),
                    Lifetime = Range(0.9f, 1.45f), Size = 3.2f,
                    Hue = Range(0, 1), Kind = ParticleKind.Rocket });
            }
        }
        else
        {
            emissionDebt = Math.Min(emissionDebt + Math.Clamp(EmissionRate, 0, 2000) * dt, 2000);
            int amount = (int)emissionDebt;
            emissionDebt -= amount;
            for (int i = 0; i < amount; i++) Spawn();
        }

        // Traverse backwards: swap removal is O(1), and new sparks start next step.
        for (int index = Count - 1; index >= 0; index--)
        {
            var particle = pool[index];
            particle.Age += dt;
            bool dead = particle.Age >= particle.Lifetime;
            if (dead)
            {
                RemoveAt(index);
                if (particle.Kind == ParticleKind.Rocket) Explode(particle.Position, particle.Hue, 130);
                continue;
            }
            particle.Previous = particle.Position;
            Vector2 force;
            if (Preset == ParticlePreset.Vortex)
            {
                Vector2 delta = Emitter - particle.Position;
                float distance = Math.Max(delta.Length(), 25);
                Vector2 normal = delta / distance;
                force = normal * (Gravity * 0.9f) + new Vector2(-normal.Y, normal.X) * 270
                    - particle.Velocity * 0.65f + new Vector2(Wind, 0);
            }
            else if (Preset == ParticlePreset.Snow)
                force = new(Wind * 0.16f + MathF.Sin(Time * 1.8f + particle.Hue * 30) * 32 - particle.Velocity.X * 0.3f,
                    Gravity * 0.12f - particle.Velocity.Y * 0.4f);
            else
                force = new(Wind, Gravity * (particle.Kind == ParticleKind.Rocket ? 0.28f : 1));
            particle.Velocity += force * dt;
            if (particle.Kind == ParticleKind.Spark) particle.Velocity *= MathF.Exp(-0.5f * dt);
            particle.Position += particle.Velocity * dt;

            if (Preset == ParticlePreset.Snow)
            {
                if (particle.Position.X < -10) particle.Position.X = 1210;
                if (particle.Position.X > 1210) particle.Position.X = -10;
            }
            if (particle.Position.Y > 688 && particle.Velocity.Y > 0 && Preset != ParticlePreset.Snow)
            {
                particle.Position.Y = 688;
                particle.Velocity.Y *= -0.48f;
                particle.Velocity.X *= 0.84f;
            }
            if (particle.Position.Y > 800 || particle.Position.X < -250 || particle.Position.X > 1450)
                RemoveAt(index);
            else pool[index] = particle;
        }
    }

    private void Spawn()
    {
        var particle = new Particle { Position = Emitter, Lifetime = Lifetime * Range(0.65f, 1.2f),
            Size = Range(1.5f, 3.5f), Hue = Range(0, 1) };
        switch (Preset)
        {
            case ParticlePreset.Snow:
                particle.Position = new(Range(20, 1180), Range(-20, 25));
                particle.Velocity = new(Range(-25, 25), Speed * Range(0.1f, 0.2f));
                particle.Lifetime = Lifetime * 5;
                particle.Size = Range(1.5f, 4);
                break;
            case ParticlePreset.Vortex:
                float angle = Range(0, MathF.Tau);
                Vector2 radial = new(MathF.Cos(angle), MathF.Sin(angle));
                particle.Position += radial * Range(95, 240);
                particle.Velocity = new Vector2(-radial.Y, radial.X) * Speed * 0.62f;
                break;
            default:
                float spread = Range(-0.5f, 0.5f);
                particle.Velocity = new(MathF.Sin(spread), -MathF.Cos(spread));
                particle.Velocity *= Speed * Range(0.65f, 1.12f);
                break;
        }
        Add(particle);
    }

    private void Add(Particle particle)
    {
        if (Count == Capacity) return;
        particle.Previous = particle.Position;
        pool[Count++] = particle;
    }

    private void RemoveAt(int index) => pool[index] = pool[--Count];

    public void Burst(Vector2 position)
    {
        if (Preset == ParticlePreset.Snow)
        {
            for (int i = 0; i < 100; i++)
                Add(new Particle { Position = position + new Vector2(Range(-65, 65), Range(-30, 30)),
                    Velocity = new(Range(-70, 70), Range(-60, 100)), Lifetime = Lifetime * 3,
                    Size = Range(1.5f, 4), Hue = Range(0, 1) });
        }
        else Explode(position, Range(0, 1), 180);
    }

    private void Explode(Vector2 position, float hue, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            float angle = Range(0, MathF.Tau), speed = Speed * Range(0.12f, 0.75f);
            Add(new Particle { Position = position,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Lifetime = Lifetime * Range(0.45f, 0.95f), Size = Range(1.3f, 3),
                Hue = hue + Range(-0.07f, 0.07f), Kind = ParticleKind.Spark });
        }
    }
}
