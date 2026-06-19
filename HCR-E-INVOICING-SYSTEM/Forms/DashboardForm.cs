using InvoiceApp;
using HCR_E_INVOICING_SYSTEM.Data;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace HCR_E_INVOICING_SYSTEM
{
    public class DashboardForm : Form
    {
        private Panel sidebar, mainPanel;
        private Button activeButton = null;
        private Label lblTotalCustomers, lblTotalProducts, lblPostedInvoices, lblPendingInvoices, lblPendingPayments, lblTotalSellers;
        private SalesTrendChart trendChart;
        private InvoiceStatusChart statusChart;
        private bool _loggingOut = false;


        public DashboardForm()
        {
            

            // ===== Form Settings =====
            this.Text = "HCR e-Invoice - Dashboard";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.BackColor = Color.White;
            this.FormClosing += DashboardForm_FormClosing;

            // ===== Sidebar (Full Height) =====
            sidebar = new Panel()
            {
                Dock = DockStyle.Left,
                Width = 230,
                BackColor = ColorTranslator.FromHtml("#1D2068")
            };
            this.Controls.Add(sidebar);

            // ===== FBR Logo (Top of Sidebar) =====
            PictureBox fbrLogo = new PictureBox()
            {
                // Image = Image.FromFile("fbr_logo2.PNG"),
                Image = LoadImageSafely("HCR-LOGO-SIDEBAR.png"),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(200, 90),
                Location = new Point(10, 20),
                BackColor = Color.Transparent
            };
            sidebar.Controls.Add(fbrLogo);

            // ===== LIVE badge under HCR logo =====
            Panel liveBadge = new Panel()
            {
                Size = new Size(70, 26),
                BackColor = Color.FromArgb(220, 53, 69), // Bootstrap-like danger color
                Cursor = Cursors.Default,
                Location = new Point(fbrLogo.Left + (fbrLogo.Width - 70) / 2, fbrLogo.Bottom + 8)
            };
            // Rounded corners for the badge
            liveBadge.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, liveBadge.Width, liveBadge.Height, 14, 14));

            Label lblLive = new Label()
            {
                Text = "LIVE",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            liveBadge.Controls.Add(lblLive);
            sidebar.Controls.Add(liveBadge);

            // ===== Sidebar Buttons =====
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(0, 140, 0, 10),
                AutoScroll = false
            };
            sidebar.Controls.Add(buttonPanel);

            // Section Labels
            buttonPanel.Controls.Add(CreateSidebarLabel("MAIN"));

            // Buttons
            Button btnDashboard = CreateSidebarButton("📊 Dashboard");
            Button btnCustomers = CreateSidebarButton("👤 Customers");
            Button btnSeller = CreateSidebarButton("🏷️ Sellers");
            Button btnProducts = CreateSidebarButton("📦 Products");
            Button btnInvoice = CreateSidebarButton("🧾 Create Invoice");
            Button btnViewInvoices = CreateSidebarButton("📑 View Invoices");

            buttonPanel.Controls.AddRange(new Control[] {
                btnDashboard,
                btnCustomers,
                btnSeller,
                btnProducts,
                btnInvoice,
                btnViewInvoices
            });

            buttonPanel.Controls.Add(CreateSidebarLabel("MANAGEMENT"));
            Button btnPayments = CreateSidebarButton("💳 Payments");
            Button btnReports  = CreateSidebarButton("📊 Reports");
            Button btnLogout   = CreateSidebarButton("🚪 Logout");
            buttonPanel.Controls.Add(btnPayments);
            buttonPanel.Controls.Add(btnReports);
            buttonPanel.Controls.Add(btnLogout);

            // ===== Bottom Logo =====
            PictureBox logoBox = new PictureBox()
            {
                Image = LoadImageSafely("fbr_logo2.PNG"),
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(0, 5, 0, 10)
            };
            sidebar.Controls.Add(logoBox);
            sidebar.Controls.SetChildIndex(logoBox, 0); // ensure it's at bottom


            // ===== Main Panel =====
            mainPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = ColorTranslator.FromHtml("#F5F7FA"),
                Padding = new Padding(20, 15, 20, 15)
            };
            this.Controls.Add(mainPanel);
            mainPanel.BringToFront(); // Fix layout overlap so it docks after the sidebar

            // ===== Outer vertical layout: Header row + Cards + Charts =====
            var dashLayout = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            dashLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));   // Row 0: Refresh button
            dashLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));  // Row 1: Info Cards
            dashLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Row 2: Charts
            mainPanel.Controls.Add(dashLayout);

            // ===== Top bar (Refresh button) =====
            var topBar = new Panel() { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            Button btnRefresh = new Button()
            {
                Text = "🔄 Refresh",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(130, 40),
                BackColor = ColorTranslator.FromHtml("#1D2068"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.MouseEnter += (s, e) => btnRefresh.BackColor = ColorTranslator.FromHtml("#0D1340");
            btnRefresh.MouseLeave += (s, e) => btnRefresh.BackColor = ColorTranslator.FromHtml("#1D2068");
            btnRefresh.Click += (s, e) => LoadDashboardValues();
            // Position button to the right
            topBar.Controls.Add(btnRefresh);
            topBar.SizeChanged += (s, e) =>
                btnRefresh.Location = new Point(topBar.ClientSize.Width - btnRefresh.Width - 5, 7);
            dashLayout.Controls.Add(topBar, 0, 0);

            // ===== Info Cards Grid (fully docked) =====
            TableLayoutPanel infoGrid = new TableLayoutPanel()
            {
                ColumnCount = 3,
                RowCount = 2,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 0, 10)
            };
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            dashLayout.Controls.Add(infoGrid, 0, 1);

            // ===== Info Cards =====
            infoGrid.Controls.Add(CreateInfoCard("Total Customers", "0", "👤", Color.FromArgb(16, 185, 129), out lblTotalCustomers), 0, 0);
            infoGrid.Controls.Add(CreateInfoCard("Total Products", "0", "📦", Color.FromArgb(59, 130, 246), out lblTotalProducts), 1, 0);
            infoGrid.Controls.Add(CreateInfoCard("Posted Invoices", "0", "✅", Color.FromArgb(139, 92, 246), out lblPostedInvoices), 2, 0);
            infoGrid.Controls.Add(CreateInfoCard("Pending Invoices", "0", "🕓", Color.FromArgb(245, 158, 11), out lblPendingInvoices), 0, 1);
            infoGrid.Controls.Add(CreateInfoCard("Pending Payments", "0", "🏦", Color.FromArgb(244, 63, 94), out lblPendingPayments), 1, 1);
            infoGrid.Controls.Add(CreateInfoCard("Total Sellers", "0", "🏷️", Color.FromArgb(234, 179, 8), out lblTotalSellers), 2, 1);

            // ===== Charts Grid (fully docked) =====
            TableLayoutPanel chartsGrid = new TableLayoutPanel()
            {
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            chartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            chartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            trendChart = new SalesTrendChart() { Dock = DockStyle.Fill, Margin = new Padding(10) };
            statusChart = new InvoiceStatusChart() { Dock = DockStyle.Fill, Margin = new Padding(10) };

            chartsGrid.Controls.Add(trendChart, 0, 0);
            chartsGrid.Controls.Add(statusChart, 1, 0);
            dashLayout.Controls.Add(chartsGrid, 0, 2);

            // ===== Load Data =====
            LoadDashboardValues();
            SetActiveButton(btnDashboard);

            // ===== Button Events =====
            void LaunchChildForm(Form form, Button btn, string title)
            {
                SetActiveButton(btn);
                this.Text = title;
                
                FormClosingEventHandler closingHandler = null;
                closingHandler = (sender2, args) => {
                    if (args.CloseReason == CloseReason.UserClosing)
                    {
                        args.Cancel = true;
                        form.FormClosing -= closingHandler;
                        this.Text = "HCR e-Invoice - Dashboard";
                        SetActiveButton(btnDashboard);
                        FormTransitionHelper.ReturnToParent(form, this);
                    }
                };
                form.FormClosing += closingHandler;

                FormTransitionHelper.NavigateTo(this, form, false);
            }

            btnDashboard.Click += (s, e) => SetActiveButton(btnDashboard, "You are already on Dashboard");
            btnCustomers.Click += (s, e) => LaunchChildForm(new CustomerForm(), btnCustomers, "HCR e-Invoice - Customer Management");
            btnProducts.Click += (s, e) => LaunchChildForm(new ProductForm(), btnProducts, "HCR e-Invoice - Product Management");
            btnInvoice.Click += (s, e) => LaunchChildForm(new GenerateInvoiceForm(), btnInvoice, "HCR e-Invoice - Generate Invoice");
            btnViewInvoices.Click += (s, e) => LaunchChildForm(new InvoiceViewerForm(), btnViewInvoices, "HCR e-Invoice - Invoice List");
            btnSeller.Click += (s, e) => LaunchChildForm(new SellerForm(), btnSeller, "HCR e-Invoice - Seller Management");
            btnPayments.Click += (s, e) => LaunchChildForm(new PaymentForm(), btnPayments, "HCR e-Invoice - Payment Management");
            btnReports.Click  += (s, e) => LaunchChildForm(new ReportingForm(), btnReports, "HCR e-Invoice - Reports");
            btnLogout.Click += (s, e) =>
            {
                _loggingOut = true;
                var login = new LoginForm();
                login.Show();
                FormTransitionHelper.AnimateFadeOut(this, () =>
                {
                    if (!this.IsDisposed) this.Close();
                }, 200);
            };
        }

        // ===== Sidebar Label =====
        private Label CreateSidebarLabel(string text)
        {
            return new Label()
            {
                Text = text,
                Width = 230,
                Height = 28,
                ForeColor = ColorTranslator.FromHtml("#90A4AE"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 5, 0, 0),
                BackColor = Color.Transparent
            };
        }

        // ===== Active Button =====
        private void SetActiveButton(Button btn, string message = "")
        {
            if (activeButton != null)
                activeButton.BackColor = Color.Transparent;

            activeButton = btn;
            activeButton.BackColor = ColorTranslator.FromHtml("#2A2F7A");

            if (!string.IsNullOrEmpty(message))
                MessageBox.Show(message, "Info");
        }

        // ===== Load Data =====
        private void LoadDashboardValues()
        {
            lblTotalCustomers.Text = DatabaseHelper.GetCount("Customers").ToString();
            lblTotalProducts.Text = DatabaseHelper.GetCount("Products").ToString();
            lblTotalSellers.Text = DatabaseHelper.GetCount("Sellers").ToString();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Invoices WHERE postStatus='Posted'", conn))
                    lblPostedInvoices.Text = cmd.ExecuteScalar().ToString();

                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Invoices WHERE postStatus='Saved' OR postStatus='Unpost'", conn))
                    lblPendingInvoices.Text = cmd.ExecuteScalar().ToString();

                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Invoices WHERE status='Unpaid'", conn))
                    lblPendingPayments.Text = cmd.ExecuteScalar().ToString();

                // Trend data
                DataTable dtTrend = new DataTable();
                string sqlTrend = @"
                    SELECT substr(invoiceDate, 1, 10) as date, SUM(grandTotal) as total
                    FROM Invoices
                    GROUP BY substr(invoiceDate, 1, 10)
                    ORDER BY substr(invoiceDate, 1, 10) DESC
                    LIMIT 7";
                using (var cmd = new SQLiteCommand(sqlTrend, conn))
                using (var da = new SQLiteDataAdapter(cmd))
                {
                    da.Fill(dtTrend);
                }

                // Payment Status data
                double paidSum = 0;
                double unpaidSum = 0;
                using (var cmd = new SQLiteCommand("SELECT SUM(grandTotal) FROM Invoices WHERE status='Paid'", conn))
                {
                    var res = cmd.ExecuteScalar();
                    if (res != null && res != DBNull.Value) paidSum = Convert.ToDouble(res);
                }
                using (var cmd = new SQLiteCommand("SELECT SUM(grandTotal) FROM Invoices WHERE status='Unpaid'", conn))
                {
                    var res = cmd.ExecuteScalar();
                    if (res != null && res != DBNull.Value) unpaidSum = Convert.ToDouble(res);
                }

                trendChart.SetData(dtTrend);
                statusChart.SetData(paidSum, unpaidSum);
            }
        }

        // ===== Info Card =====
        private Panel CreateInfoCard(string title, string value, string emoji, Color accentColor, out Label valueLabel)
        {
            Panel card = new Panel()
            {
                Size = new Size(200, 100),
                Margin = new Padding(15),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            card.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, card.Width, card.Height, 12, 12));
            card.Padding = new Padding(15, 10, 10, 10);

            card.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw accent color line on the left edge (6px wide)
                using (var accentBrush = new SolidBrush(accentColor))
                {
                    g.FillRectangle(accentBrush, 0, 0, 6, card.Height);
                }

                // Draw subtle custom border
                using (var borderPen = new Pen(Color.FromArgb(230, 235, 240), 1))
                {
                    g.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            Label lblIcon = new Label()
            {
                Text = emoji,
                Font = new Font("Segoe UI Emoji", 20, FontStyle.Regular),
                ForeColor = accentColor,
                AutoSize = false,
                Size = new Size(45, 45),
                Location = new Point(20, 25),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblIcon);

            valueLabel = new Label()
            {
                Text = value,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = accentColor,
                AutoSize = true,
                Location = new Point(75, 25),
                BackColor = Color.Transparent
            };
            card.Controls.Add(valueLabel);

            Label lblTitle = new Label()
            {
                Text = title,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(75, 60),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            card.MouseEnter += (s, e) => card.BackColor = ColorTranslator.FromHtml("#F1F1F1");
            card.MouseLeave += (s, e) => card.BackColor = Color.White;

            return card;
        }

        // ===== Sidebar Button =====
        private Button CreateSidebarButton(string text)
        {
            Button btn = new Button()
            {
                Text = text,
                Width = 230,
                Height = 45,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 2)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = ColorTranslator.FromHtml("#2A2F7A");
            btn.MouseLeave += (s, e) =>
            {
                if (btn != activeButton)
                    btn.BackColor = Color.Transparent;
                else
                    btn.BackColor = ColorTranslator.FromHtml("#2A2F7A");
            };
            return btn;
        }

        private void DashboardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_loggingOut)
            {
                e.Cancel = true;
                this.Hide();
                new LoginForm().Show();
            }
        }

        // helper to load image from app folder safely
        private Image LoadImageSafely(string fileName)
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, fileName);
                if (File.Exists(path))
                {
                    using (var imgTemp = Image.FromFile(path))
                    {
                        return new Bitmap(imgTemp);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Image load failed: " + ex.Message);
            }
            return null;
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );
    }

    public class SalesTrendChart : Panel
    {
        private DataTable chartData;

        public SalesTrendChart()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.Transparent;
            this.Paint += SalesTrendChart_Paint;
            this.SizeChanged += (s, e) => UpdateRegion();
        }

        private void UpdateRegion()
        {
            this.Region = Region.FromHrgn(DashboardForm.CreateRoundRectRgn(0, 0, this.Width, this.Height, 16, 16));
        }

        public void SetData(DataTable data)
        {
            this.chartData = data;
            this.Invalidate();
        }

        private void SalesTrendChart_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw title
            using (Font titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
            using (Brush titleBrush = new SolidBrush(ColorTranslator.FromHtml("#1D2068")))
            {
                g.DrawString("WEEKLY REVENUE TREND (PKR)", titleFont, titleBrush, 15, 10);
            }

            int paddingLeft = 55;
            int paddingRight = 15;
            int paddingTop = 45;
            int paddingBottom = 35;

            int chartWidth = this.Width - paddingLeft - paddingRight;
            int chartHeight = this.Height - paddingTop - paddingBottom;

            if (chartData == null || chartData.Rows.Count == 0)
            {
                using (Font font = new Font("Segoe UI", 9, FontStyle.Italic))
                using (Brush brush = new SolidBrush(Color.Gray))
                {
                    g.DrawString("No sales data available.", font, brush, this.Width / 2 - 70, this.Height / 2 - 10);
                }
                return;
            }

            double maxValue = 1000;
            foreach (DataRow row in chartData.Rows)
            {
                double val = Convert.ToDouble(row["total"]);
                if (val > maxValue) maxValue = val;
            }
            maxValue = Math.Ceiling(maxValue / 1000) * 1000;

            int rowCount = chartData.Rows.Count;
            float barSpacing = (float)chartWidth / rowCount;
            float barWidth = barSpacing * 0.5f;

            // Draw grid lines
            using (Pen gridPen = new Pen(Color.FromArgb(240, 243, 246), 1))
            using (Font labelFont = new Font("Segoe UI", 8, FontStyle.Regular))
            using (Brush labelBrush = new SolidBrush(Color.Gray))
            {
                for (int i = 0; i <= 4; i++)
                {
                    float y = paddingTop + chartHeight - (chartHeight * (i / 4f));
                    g.DrawLine(gridPen, paddingLeft, y, this.Width - paddingRight, y);

                    double valLabel = maxValue * (i / 4f);
                    string valText = valLabel >= 1000 ? (valLabel / 1000).ToString("F1") + "k" : valLabel.ToString("F0");
                    g.DrawString(valText, labelFont, labelBrush, 8, y - 7);
                }
            }

            // Draw bars
            int idx = 0;
            using (Font xFont = new Font("Segoe UI", 8, FontStyle.Bold))
            using (Brush xBrush = new SolidBrush(ColorTranslator.FromHtml("#1D2068")))
            {
                for (int i = rowCount - 1; i >= 0; i--)
                {
                    DataRow row = chartData.Rows[i];
                    double val = Convert.ToDouble(row["total"]);
                    string dateStr = row["date"].ToString();
                    string dateText = dateStr;
                    try
                    {
                        dateText = DateTime.Parse(dateStr).ToString("dd-MMM");
                    }
                    catch { }

                    float barHeight = (float)(val / maxValue * chartHeight);
                    float x = paddingLeft + (idx * barSpacing) + (barSpacing - barWidth) / 2;
                    float y = paddingTop + chartHeight - barHeight;

                    if (barHeight > 0)
                    {
                        float barRadius = Math.Min(barWidth / 2, 8);
                        if (barHeight > barRadius * 2)
                        {
                            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                            {
                                path.AddArc(x, y, barRadius * 2, barRadius * 2, 180, 90);
                                path.AddArc(x + barWidth - barRadius * 2, y, barRadius * 2, barRadius * 2, 270, 90);
                                path.AddLine(x + barWidth, y + barRadius, x + barWidth, y + barHeight);
                                path.AddLine(x + barWidth, y + barHeight, x, y + barHeight);
                                path.AddLine(x, y + barHeight, x, y + barRadius);
                                path.CloseFigure();

                                using (var lgb = new System.Drawing.Drawing2D.LinearGradientBrush(
                                    new RectangleF(x, y, barWidth, barHeight),
                                    ColorTranslator.FromHtml("#4A90E2"),
                                    ColorTranslator.FromHtml("#1D2068"),
                                    90F))
                                {
                                    g.FillPath(lgb, path);
                                }
                            }
                        }
                        else
                        {
                            using (var lgb = new System.Drawing.Drawing2D.LinearGradientBrush(
                                new RectangleF(x, y, barWidth, barHeight),
                                ColorTranslator.FromHtml("#4A90E2"),
                                ColorTranslator.FromHtml("#1D2068"),
                                90F))
                            {
                                g.FillRectangle(lgb, x, y, barWidth, barHeight);
                            }
                        }
                    }

                    if (val > 0)
                    {
                        string valOnBar = val >= 1000 ? (val / 1000).ToString("F1") + "k" : val.ToString("F0");
                        using (Font valFont = new Font("Segoe UI", 8, FontStyle.Regular))
                        using (Brush valBrush = new SolidBrush(ColorTranslator.FromHtml("#1D2068")))
                        {
                            g.DrawString(valOnBar, valFont, valBrush, x - 2, y - 14);
                        }
                    }

                    g.DrawString(dateText, xFont, xBrush, x - 5, paddingTop + chartHeight + 8);
                    idx++;
                }
            }
        }
    }

    public class InvoiceStatusChart : Panel
    {
        private double paidAmount = 0;
        private double unpaidAmount = 0;

        public InvoiceStatusChart()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.Transparent;
            this.Paint += InvoiceStatusChart_Paint;
            this.SizeChanged += (s, e) => UpdateRegion();
        }

        private void UpdateRegion()
        {
            this.Region = Region.FromHrgn(DashboardForm.CreateRoundRectRgn(0, 0, this.Width, this.Height, 16, 16));
        }

        public void SetData(double paid, double unpaid)
        {
            this.paidAmount = paid;
            this.unpaidAmount = unpaid;
            this.Invalidate();
        }

        private void InvoiceStatusChart_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw title
            using (Font titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
            using (Brush titleBrush = new SolidBrush(ColorTranslator.FromHtml("#1D2068")))
            {
                g.DrawString("PAYMENT STATUS BREAKDOWN", titleFont, titleBrush, 15, 10);
            }

            double total = paidAmount + unpaidAmount;
            if (total == 0)
            {
                using (Font font = new Font("Segoe UI", 9, FontStyle.Italic))
                using (Brush brush = new SolidBrush(Color.Gray))
                {
                    g.DrawString("No status data available.", font, brush, this.Width / 2 - 70, this.Height / 2 - 10);
                }
                return;
            }

            float paidAngle = (float)(paidAmount / total * 360f);
            float unpaidAngle = (float)(unpaidAmount / total * 360f);

            int size = Math.Min(this.Width, this.Height) - 80;
            int cx = (this.Width - size) / 2 - 50;
            int cy = (this.Height - size) / 2 + 10;

            Rectangle rect = new Rectangle(cx, cy, size, size);

            using (Brush paidBrush = new SolidBrush(Color.FromArgb(16, 185, 129)))
            {
                g.FillPie(paidBrush, rect, -90, paidAngle);
            }

            using (Brush unpaidBrush = new SolidBrush(Color.FromArgb(244, 63, 94)))
            {
                g.FillPie(unpaidBrush, rect, -90 + paidAngle, unpaidAngle);
            }

            int innerSize = (int)(size * 0.6f);
            int icx = cx + (size - innerSize) / 2;
            int icy = cy + (size - innerSize) / 2;
            
            // Walk up parent chain to find the actual solid background color
            Color parentBg = ColorTranslator.FromHtml("#F5F7FA");
            Control parent = this.Parent;
            while (parent != null)
            {
                if (parent.BackColor != Color.Transparent && parent.BackColor != Color.Empty)
                {
                    parentBg = parent.BackColor;
                    break;
                }
                parent = parent.Parent;
            }

            using (Brush bgBrush = new SolidBrush(parentBg))
            {
                g.FillEllipse(bgBrush, icx, icy, innerSize, innerSize);
            }

            using (Font percentFont = new Font("Segoe UI", 11, FontStyle.Bold))
            using (Brush centerBrush = new SolidBrush(ColorTranslator.FromHtml("#1D2068")))
            {
                double pct = (paidAmount / total * 100);
                string pctText = pct.ToString("F0") + "%";
                SizeF pctSize = g.MeasureString(pctText, percentFont);
                g.DrawString(pctText, percentFont, centerBrush, icx + (innerSize - pctSize.Width) / 2, icy + (innerSize / 2) - 16);

                using (Font paidLabelFont = new Font("Segoe UI", 7, FontStyle.Bold))
                {
                    string labelText = "PAID";
                    SizeF labelSize = g.MeasureString(labelText, paidLabelFont);
                    g.DrawString(labelText, paidLabelFont, centerBrush, icx + (innerSize - labelSize.Width) / 2, icy + (innerSize / 2) + 4);
                }
            }

            int legendX = cx + size + 20;
            int legendY = cy + (size / 2) - 20;

            using (Brush paidBrush = new SolidBrush(Color.FromArgb(16, 185, 129)))
            {
                g.FillRectangle(paidBrush, legendX, legendY, 12, 12);
            }
            using (Font legendFont = new Font("Segoe UI", 8, FontStyle.Bold))
            using (Brush legendTextBrush = new SolidBrush(ColorTranslator.FromHtml("#1D2068")))
            {
                g.DrawString($"PAID: {paidAmount.ToString("N0")} PKR", legendFont, legendTextBrush, legendX + 18, legendY - 2);
            }

            using (Brush unpaidBrush = new SolidBrush(Color.FromArgb(244, 63, 94)))
            {
                g.FillRectangle(unpaidBrush, legendX, legendY + 20, 12, 12);
            }
            using (Font legendFont = new Font("Segoe UI", 8, FontStyle.Bold))
            using (Brush legendTextBrush = new SolidBrush(ColorTranslator.FromHtml("#1D2068")))
            {
                g.DrawString($"UNPAID: {unpaidAmount.ToString("N0")} PKR", legendFont, legendTextBrush, legendX + 18, legendY + 18);
            }
        }
    }
}