using InvoiceApp;
using HCR_E_INVOICING_SYSTEM;
using HCR_E_INVOICING_SYSTEM.Data;
using System;
using System.Windows.Forms;
using HCR_E_INVOICING_SYSTEM.Helpers;
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
                        MessageBox.Show($"An unexpected error occurred.\n\n{e.Exception.Message}\n\nSee log: {logFile}", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        if (e.ExceptionObject is Exception ex)
                        {
                            Logger.LogException(ex);
                            MessageBox.Show($"A fatal error occurred.\n\n{ex.Message}\n\nSee log: {logFile}", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            Logger.LogException(new Exception("Unhandled exception object not Exception"));
                            MessageBox.Show($"A fatal error occurred. See log: {logFile}", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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