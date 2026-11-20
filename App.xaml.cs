using System;
using System.Windows;
namespace DJRussoFXHub;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Length == 1 && ShellInstaller.IsWorkerArgument(e.Args[0]))
            Environment.Exit(ShellInstaller.RunElevatedWorker(e.Args[0]));
        base.OnStartup(e);
    }
}
