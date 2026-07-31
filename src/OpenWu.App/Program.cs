using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenWu.App.Cli;
using OpenWu.App.Gui;

namespace OpenWu.App;

internal static class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    private const int ATTACH_PARENT_PROCESS = -1;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args != null && args.Length > 0)
        {
            if (AttachConsole(ATTACH_PARENT_PROCESS))
            {
                var stdout = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                var stderr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
                Console.SetOut(stdout);
                Console.SetError(stderr);
            }

            int exitCode = CliHost.Run(args);
            FreeConsole();
            return exitCode;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}
