using System.Diagnostics;
using System.Drawing.Imaging;
using System.Numerics;
using System.Text.Json;
using PhysicsLab.Core;

namespace PhysicsLab.App;

internal sealed class MainForm : Form
{
    private readonly RopeSimulation rope = new();
    private readonly ParticleSimulation particles = new();
    private readonly SimulationCanvas canvas;
    private readonly FlowLayoutPanel sidebar;
    private readonly Button ropeTab, particleTab, pauseButton, stepButton;
    private readonly Label metrics, status;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly string? verificationDirectory;
    private bool paused, particleMode;
    private double previousTime, accumulator, metricTime;
    private int frames;
    private const float FixedStep = 1f / 120;

    public MainForm(string? verificationDirectory = null)
    {
        this.verificationDirectory = verificationDirectory;
        Text = "绳索与粒子实验室 | Physics Lab";
        Name = "PhysicsLabMainWindow";
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft YaHei UI", 9);
        BackColor = Theme.Panel;
        ClientSize = new Size(1320, 840);
        MinimumSize = new Size(1050, 740);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = Padding.Empty, Padding = Padding.Empty };
        root.RowStyles.Add(new(SizeType.Absolute, 82));
        root.RowStyles.Add(new(SizeType.Absolute, 48));
        root.RowStyles.Add(new(SizeType.Percent, 100));
        root.RowStyles.Add(new(SizeType.Absolute, 28));
        Controls.Add(root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Theme.Header, Padding = new(24, 0, 24, 0), Margin = Padding.Empty };
        header.ColumnStyles.Add(new(SizeType.Percent, 100));
        header.ColumnStyles.Add(new(SizeType.Absolute, 284));
        var brand = new Panel { Dock = DockStyle.Fill };
        brand.Controls.Add(new Label { Text = "绳索与粒子实验室", ForeColor = Theme.White, Font = new Font(Font.FontFamily, 19, FontStyle.Bold), AutoSize = true, Location = new(0, 12) });
        brand.Controls.Add(new Label { Text = "PHYSICS LAB   /   交互式物理模拟", ForeColor = Theme.Caption, AutoSize = true, Location = new(2, 49) });
        header.Controls.Add(brand, 0, 0);
        var tabs = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new(0, 24, 0, 0), WrapContents = false };
        ropeTab = Theme.Button("绳索模拟  [1]"); ropeTab.Width = 132; ropeTab.Name = "ropeTab";
        particleTab = Theme.Button("粒子系统  [2]"); particleTab.Width = 132; particleTab.Name = "particleTab";
        ropeTab.Click += (_, _) => SetMode(false);
        particleTab.Click += (_, _) => SetMode(true);
        tabs.Controls.AddRange([ropeTab, particleTab]);
        header.Controls.Add(tabs, 1, 0);
        root.Controls.Add(header, 0, 0);

        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new(24, 8, 20, 6), Margin = Padding.Empty, BackColor = Color.White };
        toolbar.RowStyles.Add(new(SizeType.Absolute, 32));
        toolbar.ColumnStyles.Add(new(SizeType.Absolute, 460));
        toolbar.ColumnStyles.Add(new(SizeType.Percent, 100));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        pauseButton = Theme.Button("暂停  Space", true); pauseButton.Width = 112; pauseButton.Name = "pauseButton";
        pauseButton.Click += (_, _) => TogglePause();
        stepButton = Theme.Button("单步  N"); stepButton.Name = "stepButton"; stepButton.Enabled = false;
        stepButton.Click += (_, _) => StepOnce();
        var resetButton = Theme.Button("重置  R"); resetButton.Name = "resetButton";
        resetButton.Click += (_, _) => ResetSimulation();
        var saveButton = Theme.Button("保存画面"); saveButton.Name = "saveButton";
        saveButton.Click += (_, _) => SaveCanvas();
        actions.Controls.AddRange([pauseButton, stepButton, resetButton, saveButton]);
        toolbar.Controls.Add(actions, 0, 0);
        metrics = new Label { Name = "metricsLabel", Text = "固定步长 1/120 s", AutoSize = false, Dock = DockStyle.Fill, Margin = Padding.Empty, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.Muted, Font = new Font("Consolas", 9) };
        toolbar.Controls.Add(metrics, 1, 0);
        root.Controls.Add(toolbar, 0, 1);

        var work = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
        work.ColumnStyles.Add(new(SizeType.Percent, 100));
        work.ColumnStyles.Add(new(SizeType.Absolute, 292));
        canvas = new(rope, particles);
        canvas.Interaction += SetStatus;
        work.Controls.Add(canvas, 0, 0);
        sidebar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, BackColor = Theme.Panel, Padding = new(20, 16, 12, 20), Margin = Padding.Empty };
        work.Controls.Add(sidebar, 1, 0);
        root.Controls.Add(work, 0, 2);
        status = new Label { Text = "准备就绪", Dock = DockStyle.Fill, AutoSize = false, Padding = new(24, 0, 20, 0),
            TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Muted, BackColor = Color.FromArgb(226, 234, 242) };
        root.Controls.Add(status, 0, 3);

        SetMode(false);
        timer.Tick += OnTick;
        Shown += (_, _) =>
        {
            previousTime = clock.Elapsed.TotalSeconds;
            timer.Start();
            if (this.verificationDirectory is not null) BeginInvoke(RunUiVerification);
        };
        Deactivate += (_, _) => canvas.CancelInteraction();
    }

    private void SetMode(bool useParticles)
    {
        particleMode = useParticles;
        canvas.CancelInteraction();
        canvas.ParticleMode = useParticles;
        ropeTab.BackColor = useParticles ? Theme.Header : Theme.Copper;
        ropeTab.ForeColor = useParticles ? Theme.White : Theme.Ink;
        particleTab.BackColor = useParticles ? Theme.Teal : Theme.Header;
        particleTab.ForeColor = useParticles ? Theme.Ink : Theme.White;
        ropeTab.FlatAppearance.BorderColor = useParticles ? Color.FromArgb(87, 117, 144) : Theme.Copper;
        particleTab.FlatAppearance.BorderColor = useParticles ? Theme.Teal : Color.FromArgb(87, 117, 144);
        ropeTab.FlatAppearance.MouseOverBackColor = useParticles ? Color.FromArgb(47, 75, 101) : Color.FromArgb(255, 197, 139);
        ropeTab.FlatAppearance.MouseDownBackColor = useParticles ? Color.FromArgb(63, 94, 120) : Color.FromArgb(224, 152, 83);
        particleTab.FlatAppearance.MouseOverBackColor = useParticles ? Color.FromArgb(137, 235, 226) : Color.FromArgb(47, 75, 101);
        particleTab.FlatAppearance.MouseDownBackColor = useParticles ? Color.FromArgb(76, 193, 184) : Color.FromArgb(63, 94, 120);
        accumulator = 0;
        BuildSidebar();
        SetStatus(useParticles ? "选择粒子预设，左键移动发射器，右键产生粒子爆发" : "拖拽节点观察绳索响应，右键剪断；障碍物也可拖动");
        UpdateMetrics(0);
        canvas.Invalidate();
    }

    private void BuildSidebar()
    {
        sidebar.SuspendLayout();
        foreach (Control control in sidebar.Controls.Cast<Control>().ToArray()) control.Dispose();
        sidebar.Controls.Clear();
        Heading("实验设置");
        if (particleMode)
        {
            Select("发射预设", "particlePreset", ["喷泉 Fountain", "烟花 Fireworks", "飘雪 Snow", "旋涡 Vortex"], (int)particles.Preset,
                index => { particles.Reset((ParticlePreset)index); UpdateTrailAvailability(); accumulator = 0; UpdateMetrics(0); canvas.Invalidate(); SetStatus("已切换粒子预设，参数保留当前设置"); });
            Slider("发射速率", "emissionRate", 30, 2000, (int)particles.EmissionRate, v => $"{v} /s", v => particles.EmissionRate = v);
            Slider("初始速度", "particleSpeed", 100, 700, (int)particles.Speed, v => $"{v} px/s", v => particles.Speed = v);
            Slider("粒子寿命", "particleLifetime", 10, 80, (int)(particles.Lifetime * 10), v => $"{v / 10f:0.0} s", v => particles.Lifetime = v / 10f);
            Slider("重力强度", "particleGravity", 0, 1200, (int)particles.Gravity, v => $"{v} px/s²", v => particles.Gravity = v);
            Slider("水平风力", "particleWind", -600, 600, (int)particles.Wind, v => $"{v:+0;-0;0} px/s²", v => particles.Wind = v);
            Check("显示运动拖尾", "trails", particles.Trails, value => particles.Trails = value);
            UpdateTrailAvailability();
            var burst = Theme.Button("在发射器处爆发"); burst.Width = 246; burst.Margin = new(0, 8, 0, 0); burst.Name = "burstButton";
            burst.Click += (_, _) => { particles.Burst(particles.Emitter); canvas.Invalidate(); };
            sidebar.Controls.Add(burst);
            Note("烟花：速率控制发射频率。\n飘雪：寿命 ×5，重力 ×0.12。\n旋涡：重力控制向心吸引强度。");
        }
        else
        {
            Select("绳索预设", "ropePreset", ["悬索桥 · 双端固定", "单端摆 · 单端固定", "自由落绳 · 无固定端"], (int)rope.Preset,
                index => { rope.Reset((RopePreset)index); accumulator = 0; canvas.Invalidate(); });
            Slider("重力强度", "ropeGravity", 0, 1800, (int)rope.Gravity, v => $"{v} px/s²", v => rope.Gravity = v);
            Slider("水平风力", "ropeWind", -800, 800, (int)rope.Wind, v => $"{v:+0;-0;0} px/s²", v => rope.Wind = v);
            Slider("求解迭代", "ropeIterations", 8, 64, rope.Iterations, v => $"{v} 次/子步", v => rope.Iterations = v);
            Slider("绳索分段", "ropeSegments", 24, 100, rope.SegmentCount, v => $"{v} 段", v => { rope.SegmentCount = v; rope.Reset(rope.Preset); });
            Note("分段变化会重建绳索；迭代越多，\n距离约束误差越小、CPU 开销越高。");
            Check("启用圆形障碍物碰撞", "collisions", rope.Collisions, value => rope.Collisions = value);
            Check("显示绳索节点", "showNodes", canvas.ShowNodes, value => canvas.ShowNodes = value);
            Note("左键：拖拽节点、锚点或障碍物\n右键：剪断最近的一段绳索\n重置：恢复完整绳索与初始位置");
        }
        Check("显示参考网格", "showGrid", canvas.ShowGrid, value => canvas.ShowGrid = value);
        sidebar.ResumeLayout(true);
    }

    private void Heading(string text) => sidebar.Controls.Add(new Label { Text = text, Width = 246, Height = 32,
        Font = new Font(Font.FontFamily, 12, FontStyle.Bold), ForeColor = Theme.Ink, Margin = new(0, 0, 0, 8) });

    private void UpdateTrailAvailability()
    {
        if (sidebar.Controls["trails"] is not CheckBox trails) return;
        trails.Enabled = particles.Preset != ParticlePreset.Snow;
        trails.Text = trails.Enabled ? "显示运动拖尾" : "显示运动拖尾（飘雪不适用）";
    }

    private void Note(string text) => sidebar.Controls.Add(new Label { Text = text, Width = 246,
        Height = (text.Count(c => c == '\n') + 1) * 21 + 10, ForeColor = Theme.Muted, Margin = new(0, 10, 0, 8) });

    private void Select(string label, string name, string[] items, int selected, Action<int> changed)
    {
        sidebar.Controls.Add(new Label { Text = label, Width = 246, Height = 22, ForeColor = Theme.Ink, Margin = Padding.Empty });
        var combo = new ComboBox { Width = 246, DropDownStyle = ComboBoxStyle.DropDownList, Name = name,
            AccessibleName = label, Margin = new(0, 0, 0, 16) };
        combo.Items.AddRange(items); combo.SelectedIndex = selected;
        combo.SelectedIndexChanged += (_, _) => changed(combo.SelectedIndex);
        sidebar.Controls.Add(combo);
    }

    private void Slider(string label, string name, int min, int max, int value, Func<int, string> format, Action<int> changed)
    {
        var row = new Panel { Width = 246, Height = 22, Margin = Padding.Empty };
        row.Controls.Add(new Label { Text = label, Width = 93, Dock = DockStyle.Left, ForeColor = Theme.Ink });
        var output = new Label { Text = format(value), Dock = DockStyle.Right, Width = 148, TextAlign = ContentAlignment.TopRight, ForeColor = Theme.Muted };
        row.Controls.Add(output); sidebar.Controls.Add(row);
        var slider = new TrackBar { Name = name, AccessibleName = label, Minimum = min, Maximum = max, Value = value,
            SmallChange = Math.Max(1, (max - min) / 100), LargeChange = Math.Max(1, (max - min) / 10),
            TickStyle = TickStyle.None, AutoSize = false, Width = 246, Height = 29, Margin = new(0, 0, 0, 8) };
        slider.ValueChanged += (_, _) => { output.Text = format(slider.Value); changed(slider.Value); canvas.Invalidate(); };
        sidebar.Controls.Add(slider);
    }

    private void Check(string label, string name, bool selected, Action<bool> changed)
    {
        var check = new CheckBox { Text = label, Name = name, Checked = selected, Width = 246, Height = 28,
            ForeColor = Theme.Ink, Margin = new(0, 1, 0, 2), AccessibleName = label };
        check.CheckedChanged += (_, _) => { changed(check.Checked); canvas.Invalidate(); };
        sidebar.Controls.Add(check);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        double now = clock.Elapsed.TotalSeconds;
        double elapsed = Math.Min(now - previousTime, 0.05);
        previousTime = now;
        if (!paused && WindowState != FormWindowState.Minimized)
        {
            accumulator += elapsed;
            int steps = 0;
            while (accumulator >= FixedStep && steps++ < 8)
            {
                Simulate(); accumulator -= FixedStep;
            }
            if (accumulator > FixedStep * 8) accumulator = 0;
        }
        frames++;
        if (now - metricTime >= 0.5)
        {
            UpdateMetrics(frames / (now - metricTime));
            frames = 0; metricTime = now;
        }
        canvas.Invalidate();
    }

    private void Simulate()
    {
        if (particleMode) particles.Step(FixedStep); else rope.Step(FixedStep);
    }

    private void UpdateMetrics(double fps)
    {
        string detail = particleMode ? $"{particles.Count:N0} / {ParticleSimulation.Capacity:N0} particles"
            : $"{rope.Nodes.Count} nodes  |  {rope.Links.Count(l => l.Active)} links";
        metrics.Text = $"{fps:0} FPS  |  {detail}  |  dt 1/120 s";
    }

    private void TogglePause()
    {
        paused = !paused; canvas.Paused = paused; accumulator = 0;
        pauseButton.Text = paused ? "继续  Space" : "暂停  Space";
        stepButton.Enabled = paused;
        SetStatus(paused ? "已暂停；按 N 或点击“单步”推进 1/120 秒。仍可拖拽和剪断。" : "模拟继续运行");
        canvas.Invalidate();
    }

    private void StepOnce()
    {
        if (!paused) return;
        Simulate(); UpdateMetrics(0); canvas.Invalidate();
        SetStatus("已推进一个物理步长：1/120 秒");
    }

    private void ResetSimulation()
    {
        canvas.CancelInteraction(); accumulator = 0;
        if (particleMode) particles.Reset(particles.Preset); else rope.Reset(rope.Preset);
        SetStatus("当前实验已重置，保留参数与暂停状态");
        UpdateMetrics(0); canvas.Invalidate();
    }

    private void SetStatus(string message) => status.Text = message;

    private void SaveCanvas()
    {
        using var dialog = new SaveFileDialog { Title = "保存当前模拟画面", Filter = "PNG 图像|*.png",
            FileName = $"PhysicsLab-{(particleMode ? "particles" : "rope")}-{DateTime.Now:yyyyMMdd-HHmmss}.png" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            using var bitmap = new Bitmap(canvas.Width, canvas.Height);
            canvas.DrawToBitmap(bitmap, canvas.ClientRectangle);
            bitmap.Save(dialog.FileName, ImageFormat.Png);
            SetStatus("画面已保存：" + dialog.FileName);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException)
        { MessageBox.Show(this, "保存失败，请选择可写入的目录。\n" + error.Message, "保存画面", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Space: TogglePause(); return true;
            case Keys.R: ResetSimulation(); return true;
            case Keys.N: StepOnce(); return true;
            case Keys.D1: SetMode(false); return true;
            case Keys.D2: SetMode(true); return true;
            default: return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    // Native UI integration check. Uses the same control event handlers as the user.
    private async void RunUiVerification()
    {
        timer.Stop();
        var checks = new List<object>();
        try
        {
            Directory.CreateDirectory(verificationDirectory!);
            void Require(bool success, string name)
            {
                checks.Add(new { name, success });
                if (!success) throw new InvalidOperationException(name);
            }
            async Task CaptureWindow(string name)
            {
                WindowState = FormWindowState.Normal;
                Show();
                UpdateMetrics(0); PerformLayout(); Refresh();
                await Task.Delay(100);
                using var bitmap = WindowCapture.Client(this);
                bitmap.Save(Path.Combine(verificationDirectory!, name + ".png"), ImageFormat.Png);
            }
            T Find<T>(string name) where T : Control => (T)Controls.Find(name, true).Single();

            SetMode(false);
            for (int i = 0; i < 480; i++) Simulate();
            await CaptureWindow("rope-desktop");
            Find<TrackBar>("ropeGravity").Value = 1200;
            Require(rope.Gravity == 1200, "rope gravity slider updates the physics model");
            Find<TrackBar>("ropeGravity").Value = 900;
            pauseButton.PerformClick();
            Require(paused && stepButton.Enabled, "pause enables single-step");
            float before = rope.Time; stepButton.PerformClick();
            Require(rope.Time > before && paused, "single-step advances the paused simulation");
            var midpoint = (rope.Nodes[30].Position + rope.Nodes[31].Position) / 2;
            Require(rope.Cut(midpoint), "rope cutting changes topology");
            ResetSimulation();
            Require(rope.Links.All(l => l.Active), "reset restores cut links");
            var anchor = rope.Nodes[0].Position;
            Require(rope.BeginDrag(anchor), "rope anchor can be grabbed");
            rope.MoveDrag(new(240, 180)); rope.EndDrag(); Simulate();
            Require(rope.Nodes[0].Anchor == new Vector2(240, 180), "dragging moves the fixed anchor");
            ResetSimulation();
            foreach (RopePreset preset in Enum.GetValues<RopePreset>())
            {
                Find<ComboBox>("ropePreset").SelectedIndex = (int)preset;
                Simulate(); Require(rope.Preset == preset, "rope preset " + preset);
            }
            Find<ComboBox>("ropePreset").SelectedIndex = 0;
            particleTab.PerformClick();
            Require(particleMode && canvas.ParticleMode, "particle tab switches the canvas and controls");
            Find<TrackBar>("emissionRate").Value = 700;
            Require(particles.EmissionRate == 700, "emission slider updates the particle model");
            Find<TrackBar>("emissionRate").Value = 420;
            foreach (ParticlePreset preset in Enum.GetValues<ParticlePreset>())
            {
                Find<ComboBox>("particlePreset").SelectedIndex = (int)preset;
                for (int i = 0; i < 480; i++) Simulate();
                Require(particles.Count > 0 && particles.Preset == preset, "live particle preset " + preset);
                Require(Find<CheckBox>("trails").Enabled == (preset != ParticlePreset.Snow), "trail control availability " + preset);
                if (preset == ParticlePreset.Fireworks) particles.Burst(new(620, 300));
                await CaptureWindow(preset.ToString().ToLowerInvariant() + "-desktop");
            }
            int priorCount = particles.Count;
            Find<Button>("burstButton").PerformClick();
            Require(particles.Count > priorCount, "burst button creates live particles");
            Find<CheckBox>("trails").Checked = false;
            Require(!particles.Trails, "trail checkbox changes rendering setting");
            Find<CheckBox>("trails").Checked = true;
            ClientSize = new Size(1050, 740);
            await CaptureWindow("particles-compact");
            SetMode(false);
            await CaptureWindow("rope-compact");
            Require(sidebar.AutoScroll && canvas.Width > 500, "compact window retains canvas and scrollable controls");
            Require(metrics.Height >= 18 && metrics.Bottom <= metrics.Parent!.ClientSize.Height - 6, "performance readout fits inside the toolbar");
            Require(pauseButton.Bottom <= pauseButton.Parent!.ClientSize.Height, "playback button fits inside its toolbar container");
            File.WriteAllText(Path.Combine(verificationDirectory!, "ui-verification.json"), JsonSerializer.Serialize(new { success = true, checks, metrics = new { metrics.Text, metrics.Width, metrics.Height, metrics.Visible } }, new JsonSerializerOptions { WriteIndented = true }));
            Environment.ExitCode = 0;
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(verificationDirectory!, "ui-verification.json"), JsonSerializer.Serialize(new { success = false, error = error.ToString(), checks }, new JsonSerializerOptions { WriteIndented = true }));
            Environment.ExitCode = 1;
        }
        finally { Close(); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}
