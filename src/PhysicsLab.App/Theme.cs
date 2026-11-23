namespace PhysicsLab.App;

internal static class Theme
{
    public static readonly Color Header = Color.FromArgb(24, 43, 65);
    public static readonly Color Canvas = Color.FromArgb(12, 25, 42);
    public static readonly Color Panel = Color.FromArgb(242, 246, 250);
    public static readonly Color Ink = Color.FromArgb(28, 48, 70);
    public static readonly Color Muted = Color.FromArgb(81, 103, 123);
    public static readonly Color White = Color.FromArgb(240, 246, 252);
    public static readonly Color Copper = Color.FromArgb(247, 178, 108);
    public static readonly Color Teal = Color.FromArgb(104, 222, 211);
    public static readonly Color Caption = Color.FromArgb(164, 185, 205);

    public static Button Button(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text, AutoSize = false, Height = 32, Width = 94,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = primary ? Header : Color.White, ForeColor = primary ? Color.White : Ink,
            Margin = new Padding(0, 0, 10, 0), UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = primary ? Header : Color.FromArgb(195, 208, 219);
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(47, 75, 101) : Color.FromArgb(224, 234, 243);
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(63, 94, 120) : Color.FromArgb(206, 222, 235);
        return button;
    }
}
