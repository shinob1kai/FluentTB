using System;
using System.IO;

namespace FluentTB
{
    internal static class TaskbarDiagnostics
    {
        private static readonly object Sync = new object();
        internal static void Log(string message)
        {
            try
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentTB");
                Directory.CreateDirectory(directory);
                lock (Sync) File.AppendAllText(Path.Combine(directory, "FluentTB-crash.log"), DateTime.Now.ToString("O") + " " + message + Environment.NewLine);
            }
            catch { }
        }
        internal static void LogCrash(string source, Exception exception) => Log(source + Environment.NewLine + exception);
    }
}
