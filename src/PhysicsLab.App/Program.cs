namespace PhysicsLab.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Report(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report(e.ExceptionObject as Exception);
        string? verificationDirectory = args.Length == 2 && args[0] == "--verify-ui" ? args[1] : null;
        Application.Run(new MainForm(verificationDirectory));
    }

    private static void Report(Exception? error)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhysicsLab");
        Directory.CreateDirectory(folder);
        File.AppendAllText(Path.Combine(folder, "error.log"), $"{DateTime.Now:O}\n{error}\n");
        MessageBox.Show("程序遇到错误，详情已写入：\n" + Path.Combine(folder, "error.log")
            + "\n请关闭后重试。", "绳索与粒子实验室", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
