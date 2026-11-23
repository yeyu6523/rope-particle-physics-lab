using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Numerics;
using PhysicsLab.Core;

namespace PhysicsLab.App;

internal sealed class SimulationCanvas : Control
{
    public RopeSimulation Rope { get; }
    public ParticleSimulation Particles { get; }
    [DefaultValue(false)] public bool ParticleMode { get; set; }
    [DefaultValue(true)] public bool ShowNodes { get; set; } = true;
    [DefaultValue(true)] public bool ShowGrid { get; set; } = true;
    [DefaultValue(false)] public bool Paused { get; set; }
    public event Action<string>? Interaction;
    private int draggedObstacle = -1;
    private bool movingEmitter;
    private Vector2 pointer;
    private bool pointerInside;
    private readonly Font captionFont = new("Microsoft YaHei UI", 9);
    private readonly Font titleFont = new("Microsoft YaHei UI", 15, FontStyle.Bold);
    private readonly Pen[] particlePens;
    private readonly SolidBrush[] particleBrushes;
    private readonly Color[] spectrum;

    public SimulationCanvas(RopeSimulation rope, ParticleSimulation particles)
    {
        Rope = rope; Particles = particles;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = Theme.Canvas;
        Dock = DockStyle.Fill;
        AccessibleName = "物理模拟画布";
        AccessibleDescription = "绳索：左键拖动，右键剪断；粒子：左键移动发射器，右键产生爆发。";
        Cursor = Cursors.Cross;
        TabStop = true;
        spectrum = Enumerable.Range(0, 48).Select(index => Hsv(index / 48f, 0.52f, 1)).ToArray();
        particlePens = new Pen[48 * 4];
        particleBrushes = new SolidBrush[48 * 4];
        for (int hue = 0; hue < 48; hue++)
            for (int fade = 0; fade < 4; fade++)
            {
                Color color = Color.FromArgb(70 + fade * 60, spectrum[hue]);
                particlePens[hue * 4 + fade] = new Pen(color, 1.4f);
                particleBrushes[hue * 4 + fade] = new SolidBrush(color);
            }
    }

    private (float Scale, float X, float Y) Transform()
    {
        float scale = Math.Max(0.01f, Math.Min(ClientSize.Width / 1200f, ClientSize.Height / 760f));
        return (scale, (ClientSize.Width - 1200 * scale) / 2, (ClientSize.Height - 760 * scale) / 2);
    }

    internal Point WorldToClient(Vector2 point)
    {
        var (s, x, y) = Transform();
        return new((int)(point.X * s + x), (int)(point.Y * s + y));
    }

    private Vector2 ToWorld(Point point)
    {
        var (s, x, y) = Transform();
        return new((point.X - x) / s, (point.Y - y) / s);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics graphics = e.Graphics;
        graphics.Clear(Theme.Canvas);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var (scale, x, y) = Transform();
        GraphicsState state = graphics.Save();
        graphics.TranslateTransform(x, y); graphics.ScaleTransform(scale, scale);
        DrawWorld(graphics);
        graphics.Restore(state);

        string title = ParticleMode ? Particles.Preset switch
        {
            ParticlePreset.Fireworks => "烟花 · 寿命触发与二次发射",
            ParticlePreset.Snow => "飘雪 · 风场与缓慢沉降",
            ParticlePreset.Vortex => "旋涡 · 向心力与切向运动",
            _ => "喷泉 · 连续发射与地面反弹"
        } : Rope.Preset switch
        {
            RopePreset.Pendulum => "单端摆 · 固定锚点与自由摆动",
            RopePreset.Free => "自由落绳 · 重力与碰撞",
            _ => "悬索桥 · 距离约束与碰撞"
        };
        using var titleBrush = new SolidBrush(Theme.White);
        using var captionBrush = new SolidBrush(Theme.Caption);
        graphics.DrawString(title, titleFont, titleBrush, 25, 20);
        graphics.DrawString(ParticleMode ? "左键移动发射器   /   右键粒子爆发" : "左键拖拽节点或障碍物   /   右键剪断绳索", captionFont, captionBrush, 26, 57);
        string legend = ParticleMode ? "颜色表示粒子分组，透明度随剩余寿命变化" : "实心方块：固定锚点     圆点：可运动节点     圆形：碰撞障碍物";
        graphics.DrawString(legend, captionFont, captionBrush, 25, Height - 32);
        if (Paused)
        {
            const string pausedText = "已暂停 · Space 继续";
            SizeF size = graphics.MeasureString(pausedText, captionFont);
            using var background = new SolidBrush(Color.FromArgb(40, 64, 86));
            graphics.FillRectangle(background, Width - size.Width - 40, 24, size.Width + 20, 30);
            graphics.DrawString(pausedText, captionFont, titleBrush, Width - size.Width - 30, 30);
        }
        if (Focused) ControlPaint.DrawFocusRectangle(graphics, new Rectangle(3, 3, Width - 7, Height - 7), Theme.Caption, Theme.Canvas);
    }

    private void DrawWorld(Graphics g)
    {
        if (ShowGrid)
        {
            using var grid = new Pen(Color.FromArgb(25, 47, 68), 1);
            for (int x = 60; x < 1200; x += 60) g.DrawLine(grid, x, 105, x, 690);
            for (int y = 150; y <= 690; y += 60) g.DrawLine(grid, 28, y, 1172, y);
        }
        using var groundBrush = new SolidBrush(Color.FromArgb(18, 35, 53));
        using var floorPen = new Pen(Color.FromArgb(74, 101, 125), 1.5f);
        g.FillRectangle(groundBrush, 28, 691, 1144, 30);
        g.DrawLine(floorPen, 28, 691, 1172, 691);
        if (ParticleMode) DrawParticles(g); else DrawRope(g);
    }

    private void DrawRope(Graphics g)
    {
        if (Rope.Collisions)
        {
            foreach (var obstacle in Rope.Obstacles)
            {
                float x = obstacle.Center.X, y = obstacle.Center.Y, r = obstacle.Radius;
                using var fill = new SolidBrush(Color.FromArgb(33, 57, 79));
                using var edge = new Pen(Color.FromArgb(92, 127, 155), 1.7f);
                g.FillEllipse(fill, x - r, y - r, r * 2, r * 2);
                g.DrawEllipse(edge, x - r, y - r, r * 2, r * 2);
                g.DrawLine(edge, x - 9, y, x + 9, y);
                g.DrawLine(edge, x, y - 9, x, y + 9);
            }
        }
        using var ropePen = new Pen(Theme.Copper, 4.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var nodeBrush = new SolidBrush(Color.FromArgb(255, 225, 182));
        using var anchorBrush = new SolidBrush(Theme.Teal);
        using var anchorEdge = new Pen(Color.FromArgb(106, 163, 168), 1);
        foreach (var link in Rope.Links)
        {
            if (!link.Active) continue;
            Vector2 a = Rope.Nodes[link.A].Position, b = Rope.Nodes[link.B].Position;
            g.DrawLine(ropePen, a.X, a.Y, b.X, b.Y);
        }
        foreach (var node in Rope.Nodes)
        {
            var p = node.Position;
            if (node.Pinned)
            {
                g.DrawRectangle(anchorEdge, p.X - 13, p.Y - 13, 26, 26);
                g.FillRectangle(anchorBrush, p.X - 6, p.Y - 6, 12, 12);
            }
            else if (ShowNodes) g.FillEllipse(nodeBrush, p.X - 3.3f, p.Y - 3.3f, 6.6f, 6.6f);
        }
        if (pointerInside)
        {
            var nearest = Rope.Nodes.MinBy(n => Vector2.DistanceSquared(n.Position, pointer));
            if (nearest is not null && Vector2.DistanceSquared(nearest.Position, pointer) < 32 * 32)
            {
                using var hoverPen = new Pen(Theme.White, 1.5f);
                g.DrawEllipse(hoverPen, nearest.Position.X - 11, nearest.Position.Y - 11, 22, 22);
            }
        }
    }

    private void DrawParticles(Graphics g)
    {
        foreach (ref readonly var p in Particles.Particles)
        {
            float life = Math.Clamp(1 - p.Age / p.Lifetime, 0, 1);
            int hue = Particles.Preset switch
            {
                ParticlePreset.Fountain => (int)(21 + p.Hue * 8),
                ParticlePreset.Snow => 25,
                ParticlePreset.Vortex => (int)(27 + p.Hue * 14),
                _ => ((int)(p.Hue * 48) % 48 + 48) % 48
            };
            int colorIndex = hue * 4 + Math.Clamp((int)(life * 4), 0, 3);
            float radius = p.Size * (0.55f + life * 0.45f);
            if (Particles.Trails && Particles.Preset != ParticlePreset.Snow)
            {
                Vector2 trail = p.Position - p.Velocity * 0.026f;
                g.DrawLine(particlePens[colorIndex], p.Position.X, p.Position.Y, trail.X, trail.Y);
            }
            g.FillEllipse(particleBrushes[colorIndex], p.Position.X - radius, p.Position.Y - radius, radius * 2, radius * 2);
        }
        if (Particles.Preset != ParticlePreset.Snow)
        {
            var p = Particles.Emitter;
            using var emitterPen = new Pen(Theme.Teal, 1.5f) { DashStyle = DashStyle.Dash };
            g.DrawEllipse(emitterPen, p.X - 16, p.Y - 16, 32, 32);
            g.DrawLine(emitterPen, p.X - 24, p.Y, p.X - 9, p.Y);
            g.DrawLine(emitterPen, p.X + 9, p.Y, p.X + 24, p.Y);
            g.DrawLine(emitterPen, p.X, p.Y - 24, p.X, p.Y - 9);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e); Focus();
        var world = ToWorld(e.Location);
        if (ParticleMode)
        {
            if (e.Button == MouseButtons.Right) { Particles.Burst(world); Interaction?.Invoke("已在点击位置发射粒子爆发"); }
            else if (e.Button == MouseButtons.Left)
            {
                movingEmitter = true; Capture = true;
                MoveEmitter(world);
                Interaction?.Invoke(Particles.Preset == ParticlePreset.Snow ? "飘雪从顶部持续发射；右键可在当前位置增加雪花" : "正在移动发射器 / 旋涡中心");
            }
        }
        else if (e.Button == MouseButtons.Right)
        {
            if (Rope.Cut(world)) Interaction?.Invoke("绳索已剪断，点击“重置”可恢复");
            else Interaction?.Invoke("请在绳索附近右键，剪断最近的一段连接");
        }
        else if (e.Button == MouseButtons.Left)
        {
            if (Rope.BeginDrag(world)) { Capture = true; Interaction?.Invoke("正在拖拽绳索节点；固定锚点也可移动"); }
            else if (Rope.Collisions)
                for (int i = 0; i < Rope.Obstacles.Count; i++)
                    if (Vector2.Distance(world, Rope.Obstacles[i].Center) < Rope.Obstacles[i].Radius)
                    { draggedObstacle = i; Capture = true; Interaction?.Invoke("正在移动碰撞障碍物"); break; }
        }
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        pointer = ToWorld(e.Location); pointerInside = true;
        if (movingEmitter) MoveEmitter(pointer);
        if (Rope.DraggedNode >= 0) Rope.MoveDrag(pointer);
        if (draggedObstacle >= 0)
        {
            var obstacle = Rope.Obstacles[draggedObstacle];
            obstacle.Center = new(Math.Clamp(pointer.X, obstacle.Radius + 28, 1172 - obstacle.Radius),
                Math.Clamp(pointer.Y, 115 + obstacle.Radius, 685 - obstacle.Radius));
        }
        Invalidate();
    }

    private void MoveEmitter(Vector2 point) => Particles.Emitter = new(Math.Clamp(point.X, 28, 1172), Math.Clamp(point.Y, 110, 675));

    public void CancelInteraction()
    {
        Rope.EndDrag(); draggedObstacle = -1; movingEmitter = false;
        Capture = false;
    }

    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); CancelInteraction(); }
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        if (!Capture) { Rope.EndDrag(); draggedObstacle = -1; movingEmitter = false; }
    }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); pointerInside = false; Invalidate(); }

    private static Color Hsv(float hue, float saturation, float value)
    {
        float h = hue * 6;
        int sector = (int)h;
        float f = h - sector, p = value * (1 - saturation), q = value * (1 - saturation * f), t = value * (1 - saturation * (1 - f));
        (float r, float g, float b) = (sector % 6) switch
        {
            0 => (value, t, p), 1 => (q, value, p), 2 => (p, value, t),
            3 => (p, q, value), 4 => (t, p, value), _ => (value, p, q)
        };
        return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            captionFont.Dispose(); titleFont.Dispose();
            foreach (var pen in particlePens) pen.Dispose();
            foreach (var brush in particleBrushes) brush.Dispose();
        }
        base.Dispose(disposing);
    }
}
