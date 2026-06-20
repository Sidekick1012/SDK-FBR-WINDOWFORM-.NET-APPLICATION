using InvoiceApp;
using Sidekick_E_Invoicing;
using Sidekick_E_Invoicing.Data;
using System;
using System.Windows.Forms;
using Sidekick_E_Invoicing.Helpers;
using System.IO;

namespace HCR_eInvoice
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                DatabaseHelper.InitializeDatabase();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show("Database initialization failed. Check log for details.", "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // compute log path for user guidance
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                string logFile = Path.Combine(logDir, "app.log");

                Application.ThreadException += (s, e) => {
                    try
                    {
                        Logger.LogException(e.Exception);
                        MessageBox.Show(string.Format("An unexpected error occurred.\n\n{0}\n\nSee log: {1}", e.Exception.Message, logFile), "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch
                    {
                        // fallback
                        MessageBox.Show("An unexpected error occurred.", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                    try
                    {
                        Exception ex = e.ExceptionObject as Exception;
                        if (ex != null)
                        {
                            Logger.LogException(ex);
                            MessageBox.Show(string.Format("A fatal error occurred.\n\n{0}\n\nSee log: {1}", ex.Message, logFile), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            Logger.LogException(new Exception("Unhandled exception object not Exception"));
                            MessageBox.Show(string.Format("A fatal error occurred. See log: {0}", logFile), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch
                    {
                        try { MessageBox.Show("A fatal error occurred.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
                    }
                };

                Application.Run(new LoginForm());
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show("Fatal error during application startup. See log for details.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}