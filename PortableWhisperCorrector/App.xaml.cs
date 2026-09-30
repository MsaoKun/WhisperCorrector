using System.Windows;
using System.IO;
using System;
using System.Diagnostics;

namespace PortableWhisperCorrector
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                string path = Path.Combine(AppContext.BaseDirectory, "crash.log");
                File.WriteAllText(path, e.Exception.ToString());
                MessageBox.Show("Crash: " + e.Exception.Message);
            };
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                string path = Path.Combine(AppContext.BaseDirectory, "crash_fatal.log");
                File.WriteAllText(path, ev.ExceptionObject.ToString());
                MessageBox.Show("Fatal Crash: " + ev.ExceptionObject.ToString());
            };
            base.OnStartup(e);
        }
    }
}
