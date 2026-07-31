using System;
using OpenWu.CliLib;

namespace OpenWu.Cli;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        return CliHost.Run(args);
    }
}
