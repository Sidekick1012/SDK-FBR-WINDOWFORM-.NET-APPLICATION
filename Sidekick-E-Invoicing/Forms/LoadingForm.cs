using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Sidekick_E_Invoicing
{
    public class LoadingForm : Form
    {
        private Timer animationTimer;
        private float startAngle = 0;
        private string loadingText = "Loading...";

        public LoadingForm(string text = "Loading...")
        {
            this.loadingText = text;
            this.Size = new Size(240, 240);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorTranslator.FromHtml("#1b6656");
            this.ShowInTaskbar = false;
            this.TopMost = true;

            // Make the form rounded
            this.Load += (s, e) => {
                this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 24, 24));
            };

            this.DoubleBuffered = true;

            animationTimer = new Timer();
            animationTimer.Interval = 15; // smooth rotation
            animationTimer.Tick += (s, e) => {
                startAngle = (startAngle + 10) % 360;
                this.Invalidate();
            };
            animationTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw a subtle border inside the rounded corners
            using (Pen borderPen = new Pen(ColorTranslator.FromHtml("#7bb06b"), 3))
            {
                g.DrawRectangle(borderPen, 1, 1, this.Width - 3, this.Height - 3);
            }

            // Draw spinning circle
            int spinnerSize = 75;
            int x = (this.Width - spinnerSize) / 2;
            int y = (this.Height - spinnerSize) / 2 - 20;

            // Background track arc (translucent white)
            using (Pen bgPen = new Pen(Color.FromArgb(40, Color.White), 6))
            {
                g.DrawEllipse(bgPen, x, y, spinnerSize, spinnerSize);
            }

            // Spinning accent arc
            using (Pen fgPen = new Pen(ColorTranslator.FromHtml("#7bb06b"), 6))
            {
                fgPen.StartCap = LineCap.Round;
                fgPen.EndCap = LineCap.Round;
                g.DrawArc(fgPen, x, y, spinnerSize, spinnerSize, startAngle, 110);
            }

            // Loading Text
            using (Font font = new Font("Segoe UI", 12, FontStyle.Bold))
            using (Brush brush = new SolidBrush(Color.White))
            {
                SizeF textSize = g.MeasureString(loadingText, font);
                g.DrawString(loadingText, font, brush, (this.Width - textSize.Width) / 2, y + spinnerSize + 30);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Stop();
                animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );
    }

    public static class FormNavigationHelper
    {
        public static void NavigateTo(Form currentForm, Form targetForm, string loadingMessage = "Loading...", bool reopenCurrentOnClose = true)
        {
            // 1. Show the animated loader form
            using (var loader = new LoadingForm(loadingMessage))
            {
                loader.Show();
                loader.Refresh();

                // Briefly allow the animation timer to run so the transition feels smooth
                DateTime start = DateTime.Now;
                while ((DateTime.Now - start).TotalMilliseconds < 550)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(10);
                }

                // 2. Hide the current form
                currentForm.Hide();

                // 3. Close the loader
                loader.Close();
            }

            // 4. Hook FormClosed event of the target form to reopen the current form (unless false)
            if (reopenCurrentOnClose)
            {
                targetForm.FormClosed += (s, e) => {
                    try
                    {
                        if (!currentForm.IsDisposed)
                        {
                            currentForm.Show();
                        }
                    }
                    catch { }
                };
            }

            // 5. Show the new form
            targetForm.Show();
        }
    }
}
