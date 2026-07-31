using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FluentTB
{
    /// <summary>
    /// Interaction logic for App.xaml
    ///
    /// Sets up the global crash log next to the EXE (cleared on every start)
    /// and hooks all unhandled-exception surfaces so nothing silently swallows errors.
    /// </summary>
    public partial class App : Application
    {
        // ── Log path: in AppData/Local, cleared on every launch ────────
        public static readonly string CrashLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentTB",
            "FluentTB-crash.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure the log directory exists
            try
            {
                string logDir = Path.GetDirectoryName(CrashLogPath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
            }
            catch { /* If we can't create the directory, keep going */ }

            // Clear / create the crash log at the start of every session
            try { File.WriteAllText(CrashLogPath, $"=== FluentTB started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\r\n"); }
            catch { /* If we can't write the log, keep going */ }

            // 1. WPF Dispatcher exceptions (UI thread)
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // 2. Background thread exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // 3. async Task exceptions that were not awaited
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            base.OnStartup(e);
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void OnDispatcherUnhandledException(object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("DispatcherUnhandledException", e.Exception);
            // Keep the app alive for non-critical errors when possible
            e.Handled = !IsFatal(e.Exception);
            if (!e.Handled)
                Shutdown(1);
        }

        private static void OnUnhandledException(object sender,
            UnhandledExceptionEventArgs e)
        {
            LogCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);
            // IsTerminating is already true here, nothing we can do to stop it
        }

        private static void OnUnobservedTaskException(object sender,
            UnobservedTaskExceptionEventArgs e)
        {
            LogCrash("UnobservedTaskException", e.Exception);
            e.SetObserved(); // Prevent process termination for fire-and-forget tasks
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a full exception report (type, message, stack, inners) to the crash log.
        /// Thread-safe via a simple lock.
        /// </summary>
        public static void LogCrash(string source, Exception ex)
        {
            try
            {
                lock (CrashLogPath)
                {
                    using (var w = new StreamWriter(CrashLogPath, append: true))
                    {
                        w.WriteLine();
                        w.WriteLine($"--- {source} @ {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ---");
                        WriteException(w, ex, 0);
                        w.WriteLine();
                    }
                }
            }
            catch { /* Never let logging crash the process */ }
        }

        /// <summary>
        /// Quick helper to log any non-exception message (e.g. from Background worker).
        /// </summary>
        public static void Log(string message)
        {
            try
            {
                lock (CrashLogPath)
                {
                    File.AppendAllText(CrashLogPath,
                        $"[{DateTime.Now:HH:mm:ss.fff}] {message}\r\n");
                }
            }
            catch { }
        }

        private static void WriteException(StreamWriter w, Exception ex, int depth)
        {
            if (ex == null) { w.WriteLine("  (null exception)"); return; }
            string indent = new string(' ', depth * 2);
            w.WriteLine($"{indent}Type   : {ex.GetType().FullName}");
            w.WriteLine($"{indent}Message: {ex.Message}");
            w.WriteLine($"{indent}Stack  :");
            foreach (var line in (ex.StackTrace ?? "").Split('\n'))
                w.WriteLine($"{indent}  {line.TrimEnd()}");

            if (ex is AggregateException agg)
                foreach (var inner in agg.InnerExceptions)
                {
                    w.WriteLine($"{indent}InnerException:");
                    WriteException(w, inner, depth + 1);
                }
            else if (ex.InnerException != null)
            {
                w.WriteLine($"{indent}InnerException:");
                WriteException(w, ex.InnerException, depth + 1);
            }
        }

        /// <summary>True for exceptions where we cannot safely continue.</summary>
        private static bool IsFatal(Exception ex) =>
            ex is OutOfMemoryException      ||
            ex is StackOverflowException    ||
            ex is AccessViolationException  ||
            ex is ThreadAbortException;
    }
}
