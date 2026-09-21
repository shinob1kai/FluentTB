using System;
using System.IO;
using Newtonsoft.Json;

namespace FluentTB
{
    internal static class DebugLog
    {
        private const string LogPath = @"C:\Users\Kai\Documents\FluentTB\debug-964ba0.log";

        public static void Write(string location, string message, object data, string hypothesisId, string runId = "pre-fix")
        {
            // #region agent log
            try
            {
                string line = JsonConvert.SerializeObject(new
                {
                    sessionId = "964ba0",
                    location,
                    message,
                    data,
                    hypothesisId,
                    runId,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                File.AppendAllText(LogPath, line + "\n");
            }
            catch { }
            // #endregion
        }
    }
}
