using System.Runtime.InteropServices;

namespace PhysicsLab.App;

/// <summary>Capture only our own window, including native child controls.</summary>
internal static class WindowCapture
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);

    public static Bitmap Client(Form form)
    {
        var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        IntPtr context = graphics.GetHdc();
        bool success;
        try { success = PrintWindow(form.Handle, context, 1); }
        finally { graphics.ReleaseHdc(context); }
        if (!success)
        {
            bitmap.Dispose();
            throw new InvalidOperationException("Windows could not render the application window for verification.");
        }
        return bitmap;
    }
}
