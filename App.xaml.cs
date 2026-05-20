using System.IO;
using System.Windows;

namespace CraftifyWPF;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            try
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Craftify", "crash.log");
                var dir = System.IO.Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(logPath, $"{e.Exception}");
            }
            catch { }
            e.Handled = true;
        };
    }
}

