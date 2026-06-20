using System;
using System.IO;

namespace Sidekick_E_Invoicing.Helpers
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

                string entry = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}\r\n", DateTime.Now, ex);

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
