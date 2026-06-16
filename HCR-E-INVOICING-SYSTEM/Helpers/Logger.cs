using System;
using System.IO;

namespace SDK_E_INVOICING_SYSTEM.Helpers
{
    public static class Logger
    {
        private static readonly object _sync = new object();

        public static void LogException(Exception ex)
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "app.log");

                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n";

                lock (_sync)
                {
                    File.AppendAllText(logFile, entry);
                }
            }
            catch
            {
                // Swallow any logging exceptions to avoid crashing the app during error handling
            }
        }
    }
}
