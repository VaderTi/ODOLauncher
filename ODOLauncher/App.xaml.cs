using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace ODOLauncher
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex _mutex;

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var name = Assembly.GetExecutingAssembly().GetName().Name;
            var guid = Assembly.GetExecutingAssembly().GetType().GUID;
            var mutexName = string.Format(CultureInfo.InvariantCulture, "Local\\{0}{1}", name, guid);
            _mutex = new Mutex(true, mutexName, out var mutexCreated);
            if (!mutexCreated) Current.Shutdown();
        }

        ///// <summary>
        ///// Kill a process, and all of its children, grandchildren, etc.
        ///// </summary>
        ///// <param name="pid">Process ID.</param>
        //private static void KillProcessAndChildren(int pid)
        //{
        //    // Cannot close 'system idle process'.
        //    if (pid == 0)
        //    {
        //        return;
        //    }
        //    var searcher = new ManagementObjectSearcher
        //        ("Select * From Win32_Process Where ParentProcessID=" + pid);
        //    var moc = searcher.Get();
        //    foreach (var o in moc)
        //    {
        //        var mo = (ManagementObject) o;
        //        KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
        //    }
        //    try
        //    {
        //        var proc = Process.GetProcessById(pid);
        //        proc.Kill();
        //    }
        //    catch (ArgumentException)
        //    {
        //        // Process already exited.
        //    }
        //}
    }
}