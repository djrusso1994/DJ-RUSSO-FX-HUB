using System;
using System.Windows;
namespace DJRussoFXHub;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Length == 1 && ShellInstaller.IsWorkerArgument(e.Args[0]))
            Environment.Exit(ShellInstaller.RunElevatedWorker(e.Args[0]));
        if (e.Args.Length == 1 && ReverbInstaller.IsWorkerArgument(e.Args[0]))
            Environment.Exit(ReverbInstaller.RunElevatedWorker(e.Args[0]));
        if (e.Args.Length == 1 && DelayInstaller.IsWorkerArgument(e.Args[0]))
            Environment.Exit(DelayInstaller.RunElevatedWorker(e.Args[0]));
        if (e.Args.Length == 1 && CompressorInstaller.IsWorkerArgument(e.Args[0]))
            Environment.Exit(CompressorInstaller.RunElevatedWorker(e.Args[0]));
        if (e.Args.Length == 1 && EqInstaller.IsWorkerArgument(e.Args[0]))
            Environment.Exit(EqInstaller.RunElevatedWorker(e.Args[0]));
        if (e.Args.Length == 1 && DriveInstaller.IsWorkerArgument(e.Args[0]))
            Environment.Exit(DriveInstaller.RunElevatedWorker(e.Args[0]));
        base.OnStartup(e);
    }
}
