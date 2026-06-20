using InvoiceApp;
using HCR_E_INVOICING_SYSTEM.Data;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;

namespace HCR_E_INVOICING_SYSTEM
{
    public class DashboardForm : Form
    {
        private Panel sidebar, mainPanel;
        private Button activeButton = null;
        private Label lblTotalCustomers, lblTotalProducts, lblPostedInvoices, lblPendingInvoices, lblPendingPayments, lblTotalSellers;
        private Label lblClockTime;
        private SalesTrendChart trendChart;
        private InvoiceStatusChart statusChart;
        private bool _loggingOut = false;
        private System.Windows.Forms.Timer clockTimer;

        // Brand colours
        private static readonly Color NavyDark   = ColorTranslator.FromHtml("#0F1552");
        private static readonly Color NavyMid    = ColorTranslator.FromHtml("#1D2068");
        private static readonly Color NavyLight  = ColorTranslator.FromHtml("#2A2F7A");
        private static readonly Color AccentGold = ColorTranslator.FromHtml("#F0A500");
        private static readonly Color BgPage     = ColorTranslator.FromHtml("#F0F2F8");

        public DashboardForm()
        {
            // ── Form settings ──────────────────────────────────────────
            this.Text = "HCR e-Invoice — Dashboard";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = BgPage;
            this.FormClosing += DashboardForm_FormClosing;
            this.DoubleBuffered = true;

            // ── Sidebar ────────────────────────────────────────────────
            BuildSidebar(out Button btnDashboard, out Button btnCustomers,
                         out Button btnSeller, out Button btnProducts,
                         out Button btnInvoice, out Button btnViewInvoices,
                         out Button btnPayments, out Button btnReports,
                         out Button btnLogout);

            // ── Main area ──────────────────────────────────────────────
            mainPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = BgPage,
                Padding = new Padding(24, 16, 24, 16)
            };
            this.Controls.Add(mainPanel);
            mainPanel.BringToFront();

            // ── Outer vertical stack ───────────────────────────────────
            var rootLayout = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));   // 0 – Header banner
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));  // 1 – Stat cards
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));   // 2 – Quick actions
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // 3 – Charts
            mainPanel.Controls.Add(rootLayout);

            // ── Row 0: Header banner ───────────────────────────────────
            rootLayout.Controls.Add(BuildHeaderBanner(), 0, 0);

            // ── Row 1: Stat cards ──────────────────────────────────────
            var cardsRow = new TableLayoutPanel()
            {
                ColumnCount = 6,
                RowCount = 1,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 6)
            };
            for (int i = 0; i < 6; i++)
                cardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));

            cardsRow.Controls.Add(CreateStatCard("Total Customers",  "0", "👤", Color.FromArgb(99,  102, 241), out lblTotalCustomers),   0, 0);
            cardsRow.Controls.Add(CreateStatCard("Total Products",   "0", "📦", Color.FromArgb(59,  130, 246), out lblTotalProducts),    1, 0);
            cardsRow.Controls.Add(CreateStatCard("Posted Invoices",  "0", "✅", Color.FromArgb(16,  185, 129), out lblPostedInvoices),   2, 0);
            cardsRow.Controls.Add(CreateStatCard("Pending Invoices", "0", "🕓", Color.FromArgb(245, 158,  11), out lblPendingInvoices),  3, 0);
            cardsRow.Controls.Add(CreateStatCard("Pending Payments", "0", "💳", Color.FromArgb(239,  68,  68), out lblPendingPayments), 4, 0);
            cardsRow.Controls.Add(CreateStatCard("Total Sellers",    "0", "🏷️", Color.FromArgb(139,  92, 246), out lblTotalSellers),    5, 0);
            rootLayout.Controls.Add(cardsRow, 0, 1);

            // ── Row 2: Quick actions bar ───────────────────────────────
            rootLayout.Controls.Add(BuildQuickActionsBar(
                btnInvoice, btnViewInvoices, btnCustomers, btnReports), 0, 2);

            // ── Row 3: Charts ──────────────────────────────────────────
            var chartsRow = new TableLayoutPanel()
            {
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };
            chartsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            chartsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            trendChart  = new SalesTrendChart()  { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
            statusChart = new InvoiceStatusChart() { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
            chartsRow.Controls.Add(trendChart,  0, 0);
            chartsRow.Controls.Add(statusChart, 1, 0);
            rootLayout.Controls.Add(chartsRow, 0, 3);

            // ── Load data & wire buttons ───────────────────────────────
            LoadDashboardValues();
            SetActiveButton(btnDashboard);
            WireButtons(btnDashboard, btnCustomers, btnSeller, btnProducts,
                        btnInvoice, btnViewInvoices, btnPayments, btnReports, btnLogout);

            // ── Live clock ─────────────────────────────────────────────
            clockTimer = new System.Windows.Forms.Timer() { Interval = 1000 };
            clockTimer.Tick += (s, e) =>
            {
                if (lblClockTime != null && !lblClockTime.IsDisposed)
                    lblClockTime.Text = DateTime.Now.ToString("hh:mm:ss tt");
            };
            clockTimer.Start();
        }

        // ══════════════════════════════════════════════════════════════
        // BUILD SIDEBAR
        // ══════════════════════════════════════════════════════════════
        private void BuildSidebar(
            out Button btnDashboard, out Button btnCustomers,
            out Button btnSeller,    out Button btnProducts,
            out Button btnInvoice,   out Button btnViewInvoices,
            out Button btnPayments,  out Button btnReports,
            out Button btnLogout)
        {
            sidebar = new Panel()
            {
                Dock      = DockStyle.Left,
                Width     = 235,
                BackColor = NavyDark
            };
            this.Controls.Add(sidebar);

            // Gold top accent strip
            var topAccent = new Panel()
            {
                Dock      = DockStyle.Top,
                Height    = 4,
                BackColor = AccentGold
            };
            sidebar.Controls.Add(topAccent);

            // Logo area
            PictureBox logoBox = new PictureBox()
            {
                Image     = LoadImageSafely("HCR-LOGO-SIDEBAR.png"),
                SizeMode  = PictureBoxSizeMode.Zoom,
                Size      = new Size(195, 85),
                Location  = new Point(20, 18),
                BackColor = Color.Transparent
            };
            sidebar.Controls.Add(logoBox);

            // "LIVE" badge
            Panel liveBadge = new Panel()
            {
                Size      = new Size(60, 22),
                BackColor = Color.FromArgb(220, 53, 69),
                Location  = new Point(87, 106)
            };
            liveBadge.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, 60, 22, 11, 11));
            var lblLive = new Label()
            {
                Text      = "● LIVE",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            liveBadge.Controls.Add(lblLive);
            sidebar.Controls.Add(liveBadge);

            // Divider line under logo area
            var divider = new Panel()
            {
                Size      = new Size(195, 1),
                Location  = new Point(20, 135),
                BackColor = Color.FromArgb(50, 255, 255, 255)
            };
            sidebar.Controls.Add(divider);

            // Button scroll area
            var btnFlow = new FlowLayoutPanel()
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding       = new Padding(0, 148, 0, 10),
                AutoScroll    = false,
                BackColor     = Color.Transparent
            };
            sidebar.Controls.Add(btnFlow);

            btnFlow.Controls.Add(CreateSidebarSection("MAIN"));
            btnDashboard   = CreateSidebarButton("  📊  Dashboard");
            btnCustomers   = CreateSidebarButton("  👤  Customers");
            btnSeller      = CreateSidebarButton("  🏷️  Sellers");
            btnProducts    = CreateSidebarButton("  📦  Products");
            btnInvoice     = CreateSidebarButton("  🧾  Create Invoice");
            btnViewInvoices= CreateSidebarButton("  📑  View Invoices");
            btnFlow.Controls.AddRange(new Control[] {
                btnDashboard, btnCustomers, btnSeller,
                btnProducts,  btnInvoice,  btnViewInvoices });

            btnFlow.Controls.Add(CreateSidebarSection("MANAGEMENT"));
            btnPayments = CreateSidebarButton("  💳  Payments");
            btnReports  = CreateSidebarButton("  📊  Reports");
            btnFlow.Controls.Add(btnPayments);
            btnFlow.Controls.Add(btnReports);

            // Bottom logout button
            btnLogout = new Button()
            {
                Text      = "  🚪  Logout",
                Width     = 235,
                Height    = 44,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(255, 120, 120),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor    = Cursors.Hand,
                Dock      = DockStyle.Bottom
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            var _logoutBtn = btnLogout; // local alias — out params can't be captured in lambdas
            _logoutBtn.MouseEnter += (s, e) => _logoutBtn.BackColor = Color.FromArgb(80, 220, 50, 50);
            _logoutBtn.MouseLeave += (s, e) => _logoutBtn.BackColor = Color.Transparent;
            sidebar.Controls.Add(btnLogout);

            // FBR bottom logo
            PictureBox fbrLogoBottom = new PictureBox()
            {
                Image     = LoadImageSafely("fbr_logo2.PNG"),
                Dock      = DockStyle.Bottom,
                Height    = 70,
                BackColor = Color.Transparent,
                SizeMode  = PictureBoxSizeMode.Zoom
            };
            sidebar.Controls.Add(fbrLogoBottom);
            sidebar.Controls.SetChildIndex(fbrLogoBottom, 0);
        }

        // ══════════════════════════════════════════════════════════════
        // HEADER BANNER
        // ══════════════════════════════════════════════════════════════
        private Panel BuildHeaderBanner()
        {
            var banner = new Panel()
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8)
            };

            // Gradient background via Paint
            banner.Paint += (s, e) =>
            {
                using (var lgb = new LinearGradientBrush(
                    banner.ClientRectangle,
                    NavyMid, NavyLight, LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(lgb, banner.ClientRectangle);
                }
                // Gold bottom accent
                using (var pen = new Pen(AccentGold, 3))
                    e.Graphics.DrawLine(pen, 0, banner.Height - 2,
                                        banner.Width, banner.Height - 2);
            };

            // Welcome label
            var lblWelcome = new Label()
            {
                Text      = "HCR E-Invoicing System Dashboard",
                Font      = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(550, 36),
                Location  = new Point(18, 12),
                TextAlign = ContentAlignment.MiddleLeft
            };
            banner.Controls.Add(lblWelcome);

            // Date label
            var lblDate = new Label()
            {
                Text      = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                Font      = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 220, 255),
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(320, 20),
                Location  = new Point(20, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            banner.Controls.Add(lblDate);

            // Clock label (right side)
            lblClockTime = new Label()
            {
                Text      = DateTime.Now.ToString("hh:mm:ss tt"),
                Font      = new Font("Consolas", 15, FontStyle.Bold),
                ForeColor = AccentGold,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(160, 40),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top
            };
            banner.SizeChanged += (s, e) =>
                lblClockTime.Location = new Point(banner.Width - 180, 20);
            banner.Controls.Add(lblClockTime);

            // Refresh button
            var btnRefresh = new Button()
            {
                Text      = "🔄  Refresh",
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Size      = new Size(110, 34),
                BackColor = AccentGold,
                ForeColor = NavyDark,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, 110, 34, 8, 8));
            btnRefresh.MouseEnter += (s, e) => btnRefresh.BackColor = Color.FromArgb(220, 150, 0);
            btnRefresh.MouseLeave += (s, e) => btnRefresh.BackColor = AccentGold;
            btnRefresh.Click += (s, e) => LoadDashboardValues();
            banner.SizeChanged += (s, e) =>
                btnRefresh.Location = new Point(banner.Width - 300, 22);
            banner.Controls.Add(btnRefresh);

            return banner;
        }

        // ══════════════════════════════════════════════════════════════
        // QUICK ACTIONS BAR
        // ══════════════════════════════════════════════════════════════
        private Panel BuildQuickActionsBar(Button btnInvoice, Button btnViewInvoices,
                                           Button btnCustomers, Button btnReports)
        {
            var bar = new Panel()
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 4, 0, 4)
            };

            var lbl = new Label()
            {
                Text      = "Quick Actions:",
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 90, 120),
                AutoSize  = true,
                Location  = new Point(0, 12)
            };
            bar.Controls.Add(lbl);

            string[] qaTitles = { "➕  New Invoice", "📑  View Invoices", "👤  Customers", "📊  Reports" };
            Button[] qaSources = { btnInvoice, btnViewInvoices, btnCustomers, btnReports };
            Color[]  qaColors  = {
                Color.FromArgb(16, 185, 129),
                Color.FromArgb(59, 130, 246),
                Color.FromArgb(139, 92, 246),
                Color.FromArgb(245, 158, 11)
            };

            int offsetX = 120;
            for (int i = 0; i < qaTitles.Length; i++)
            {
                var src = qaSources[i];
                var clr = qaColors[i];
                var btn = new Button()
                {
                    Text      = qaTitles[i],
                    Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                    Size      = new Size(148, 36),
                    Location  = new Point(offsetX, 6),
                    BackColor = clr,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor    = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, 148, 36, 8, 8));
                var capClr = clr;
                btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Dark(capClr, 0.12f);
                btn.MouseLeave += (s, e) => btn.BackColor = capClr;
                btn.Click += (s, e) => src.PerformClick();
                bar.Controls.Add(btn);
                offsetX += 156;
            }

            return bar;
        }

        // ══════════════════════════════════════════════════════════════
        // STAT CARD
        // ══════════════════════════════════════════════════════════════
        private Panel CreateStatCard(string title, string value, string emoji,
                                     Color accent, out Label valueLabel)
        {
            var card = new Panel()
            {
                Margin    = new Padding(5),
                BackColor = Color.White,
                Dock      = DockStyle.Fill
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // White body rounded rect
                DrawRoundedRect(g, card.ClientRectangle, 12, Color.White);

                // Accent left bar
                using (var b = new SolidBrush(accent))
                    g.FillRectangle(b, 0, 0, 5, card.Height);

                // Top accent gradient strip
                using (var lgb = new LinearGradientBrush(
                    new Rectangle(0, 0, card.Width, 6),
                    Color.FromArgb(60, accent), Color.Transparent,
                    LinearGradientMode.Vertical))
                    g.FillRectangle(lgb, 0, 0, card.Width, 6);

                // Shadow-like border
                using (var pen = new Pen(Color.FromArgb(20, 0, 0, 0), 1))
                    g.DrawRectangle(pen, 1, 1, card.Width - 2, card.Height - 2);
            };

            // Emoji icon circle
            var iconPanel = new Panel()
            {
                Size      = new Size(44, 44),
                Location  = new Point(12, 22),
                BackColor = Color.FromArgb(30, accent)
            };
            iconPanel.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, 44, 44, 22, 22));
            var lblEmoji = new Label()
            {
                Text      = emoji,
                Font      = new Font("Segoe UI Emoji", 16),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconPanel.Controls.Add(lblEmoji);
            card.Controls.Add(iconPanel);

            // Value
            valueLabel = new Label()
            {
                Text      = value,
                Font      = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = accent,
                AutoSize  = true,
                Location  = new Point(64, 14),
                BackColor = Color.Transparent
            };
            card.Controls.Add(valueLabel);

            // Title
            var lblTitle = new Label()
            {
                Text      = title,
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 140, 160),
                AutoSize  = true,
                Location  = new Point(64, 56),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            // Hover effect
            card.MouseEnter += (s, e) => { card.BackColor = Color.FromArgb(248, 250, 255); card.Invalidate(); };
            card.MouseLeave += (s, e) => { card.BackColor = Color.White; card.Invalidate(); };

            return card;
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════
        private static void DrawRoundedRect(Graphics g, Rectangle r, int radius, Color fill)
        {
            using (var path = RoundedPath(r, radius))
            using (var br = new SolidBrush(fill))
                g.FillPath(br, path);
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Label CreateSidebarSection(string text) => new Label()
        {
            Text      = text,
            Width     = 235,
            Height    = 26,
            ForeColor = Color.FromArgb(140, 160, 200),
            Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(18, 0, 0, 0),
            BackColor = Color.Transparent
        };

        private Button CreateSidebarButton(string text)
        {
            var btn = new Button()
            {
                Text      = text,
                Width     = 235,
                Height    = 43,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(210, 225, 255),
                Font      = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 1, 0, 1)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) =>
            {
                if (btn != activeButton)
                {
                    btn.BackColor = Color.FromArgb(35, 255, 255, 255);
                    btn.ForeColor = Color.White;
                }
            };
            btn.MouseLeave += (s, e) =>
            {
                if (btn != activeButton)
                {
                    btn.BackColor = Color.Transparent;
                    btn.ForeColor = Color.FromArgb(210, 225, 255);
                }
            };
            return btn;
        }

        private void SetActiveButton(Button btn, string message = "")
        {
            if (activeButton != null)
            {
                activeButton.BackColor = Color.Transparent;
                activeButton.ForeColor = Color.FromArgb(210, 225, 255);
                activeButton.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            }
            activeButton = btn;
            activeButton.BackColor = Color.FromArgb(50, 255, 255, 255);
            activeButton.ForeColor = Color.White;
            activeButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            if (!string.IsNullOrEmpty(message))
                MessageBox.Show(message, "Info");
        }

        private void WireButtons(
            Button btnDashboard, Button btnCustomers, Button btnSeller,
            Button btnProducts, Button btnInvoice, Button btnViewInvoices,
            Button btnPayments, Button btnReports, Button btnLogout)
        {
            void Go(Form form, Button btn, string title)
            {
                SetActiveButton(btn);
                this.Text = title;
                FormClosingEventHandler h = null;
                h = (s2, a) =>
                {
                    if (a.CloseReason == CloseReason.UserClosing)
                    {
                        a.Cancel = true;
                        form.FormClosing -= h;
                        this.Text = "HCR e-Invoice — Dashboard";
                        SetActiveButton(btnDashboard);
                        FormTransitionHelper.ReturnToParent(form, this);
                    }
                };
                form.FormClosing += h;
                FormTransitionHelper.NavigateTo(this, form, false);
            }

            btnDashboard.Click    += (s, e) => SetActiveButton(btnDashboard, "You are already on Dashboard");
            btnCustomers.Click    += (s, e) => Go(new CustomerForm(),           btnCustomers,    "HCR e-Invoice — Customers");
            btnProducts.Click     += (s, e) => Go(new ProductForm(),            btnProducts,     "HCR e-Invoice — Products");
            btnInvoice.Click      += (s, e) => Go(new GenerateInvoiceForm(),    btnInvoice,      "HCR e-Invoice — New Invoice");
            btnViewInvoices.Click += (s, e) => Go(new InvoiceViewerForm(),      btnViewInvoices, "HCR e-Invoice — Invoice List");
            btnSeller.Click       += (s, e) => Go(new SellerForm(),             btnSeller,       "HCR e-Invoice — Sellers");
            btnPayments.Click     += (s, e) => Go(new PaymentForm(),            btnPayments,     "HCR e-Invoice — Payments");
            btnReports.Click      += (s, e) => Go(new ReportingForm(),          btnReports,      "HCR e-Invoice — Reports");
            btnLogout.Click += (s, e) =>
            {
                _loggingOut = true;
                var login = new LoginForm();
                login.Show();
                FormTransitionHelper.AnimateFadeOut(this, () => { if (!this.IsDisposed) this.Close(); }, 200);
            };
        }

        // ══════════════════════════════════════════════════════════════
        // LOAD DASHBOARD DATA
        // ══════════════════════════════════════════════════════════════
        private void LoadDashboardValues()
        {
            lblTotalCustomers.Text = DatabaseHelper.GetCount("Customers").ToString();
            lblTotalProducts.Text  = DatabaseHelper.GetCount("Products").ToString();
            lblTotalSellers.Text   = DatabaseHelper.GetCount("Sellers").ToString();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Invoices WHERE postStatus='Posted'", conn))
                    lblPostedInvoices.Text = cmd.ExecuteScalar().ToString();
                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Invoices WHERE postStatus='Saved' OR postStatus='Unpost'", conn))
                    lblPendingInvoices.Text = cmd.ExecuteScalar().ToString();
                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Invoices WHERE status='Unpaid'", conn))
                    lblPendingPayments.Text = cmd.ExecuteScalar().ToString();

                DataTable dtTrend = new DataTable();
                string sqlTrend = @"
                    SELECT substr(invoiceDate,1,10) as date, SUM(grandTotal) as total
                    FROM Invoices
                    GROUP BY substr(invoiceDate,1,10)
                    ORDER BY substr(invoiceDate,1,10) DESC
                    LIMIT 7";
                using (var cmd = new SQLiteCommand(sqlTrend, conn))
                using (var da = new SQLiteDataAdapter(cmd))
                    da.Fill(dtTrend);

                double paidSum = 0, unpaidSum = 0;
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

        // ── Misc ────────────────────────────────────────────────────
        private void DashboardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_loggingOut)
            {
                e.Cancel = true;
                this.Hide();
                new LoginForm().Show();
            }
        }

        private Image LoadImageSafely(string fileName)
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, fileName);
                if (File.Exists(path))
                    using (var tmp = Image.FromFile(path))
                        return new Bitmap(tmp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Image load failed: " + ex.Message);
            }
            return null;
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);
    }

    // ════════════════════════════════════════════════════════════════
    // SALES TREND CHART
    // ════════════════════════════════════════════════════════════════
    public class SalesTrendChart : Panel
    {
        private DataTable chartData;
        private static readonly Color NavyDark  = ColorTranslator.FromHtml("#0F1552");
        private static readonly Color NavyMid   = ColorTranslator.FromHtml("#1D2068");
        private static readonly Color AccentGold= ColorTranslator.FromHtml("#F0A500");

        public SalesTrendChart()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.White;
            this.Paint += OnPaint;
            this.SizeChanged += (s, e) => Invalidate();
        }

        public void SetData(DataTable data) { chartData = data; Invalidate(); }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var r = this.ClientRectangle;

            // Card background
            using (var path = RoundedPath(r, 14))
            using (var br = new SolidBrush(Color.White))
                g.FillPath(br, path);
            using (var pen = new Pen(Color.FromArgb(18, 0, 0, 0), 1))
            using (var path = RoundedPath(r, 14))
                g.DrawPath(pen, path);

            // Title bar strip
            using (var lgb = new LinearGradientBrush(
                new Rectangle(0, 0, r.Width, 42), NavyMid, NavyDark, LinearGradientMode.Horizontal))
            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, 28, 28, 180, 90);
                path.AddArc(r.Width - 28, 0, 28, 28, 270, 90);
                path.AddLine(r.Width, 14, r.Width, 42);
                path.AddLine(0, 42, 0, 14);
                path.CloseFigure();
                g.FillPath(lgb, path);
            }
            using (var pen = new Pen(AccentGold, 2))
                g.DrawLine(pen, 0, 42, r.Width, 42);

            using (var f = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var br = new SolidBrush(Color.White))
                g.DrawString("📈  Weekly Revenue Trend (PKR)", f, br, 12, 11);

            int pl = 60, pr = 18, pt = 58, pb = 42;
            int cw = r.Width - pl - pr;
            int ch = r.Height - pt - pb;

            if (chartData == null || chartData.Rows.Count == 0)
            {
                using (var f = new Font("Segoe UI", 9, FontStyle.Italic))
                using (var br = new SolidBrush(Color.Gray))
                    g.DrawString("No sales data available.", f, br, r.Width / 2 - 70, r.Height / 2);
                return;
            }

            double maxV = 1000;
            foreach (DataRow row in chartData.Rows)
            {
                double v = Convert.ToDouble(row["total"]);
                if (v > maxV) maxV = v;
            }
            maxV = Math.Ceiling(maxV / 1000.0) * 1000;

            int rc = chartData.Rows.Count;
            float bsp = (float)cw / rc;
            float bw  = bsp * 0.55f;

            // Grid lines
            using (var pen = new Pen(Color.FromArgb(230, 235, 245), 1))
            using (var lf = new Font("Segoe UI", 7.5f))
            using (var lb = new SolidBrush(Color.FromArgb(140, 150, 170)))
            {
                for (int i = 0; i <= 4; i++)
                {
                    float y = pt + ch - (ch * (i / 4f));
                    g.DrawLine(pen, pl, y, pl + cw, y);
                    double vl = maxV * (i / 4f);
                    string vt = vl >= 1000 ? (vl / 1000).ToString("F1") + "k" : vl.ToString("F0");
                    g.DrawString(vt, lf, lb, 4, y - 8);
                }
            }

            // Bars
            int idx = 0;
            using (var xf = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var xb = new SolidBrush(Color.FromArgb(90, 100, 130)))
            {
                for (int i = rc - 1; i >= 0; i--)
                {
                    DataRow row = chartData.Rows[i];
                    double val = Convert.ToDouble(row["total"]);
                    string ds = row["date"].ToString();
                    try { ds = DateTime.Parse(ds).ToString("dd-MMM"); } catch { }

                    float bh = (float)(val / maxV * ch);
                    float bx = pl + (idx * bsp) + (bsp - bw) / 2;
                    float by = pt + ch - bh;

                    if (bh > 2)
                    {
                        float rad = Math.Min(bw / 2, 6);
                        using (var path = new GraphicsPath())
                        {
                            path.AddArc(bx, by, rad * 2, rad * 2, 180, 90);
                            path.AddArc(bx + bw - rad * 2, by, rad * 2, rad * 2, 270, 90);
                            path.AddLine(bx + bw, by + rad, bx + bw, by + bh);
                            path.AddLine(bx + bw, by + bh, bx, by + bh);
                            path.CloseFigure();
                            using (var lgb = new LinearGradientBrush(
                                new RectangleF(bx, by, bw, bh),
                                ColorTranslator.FromHtml("#6366F1"),
                                NavyDark, 90f))
                                g.FillPath(lgb, path);
                        }

                        if (val > 0)
                        {
                            string vt = val >= 1000 ? (val / 1000).ToString("F1") + "k" : val.ToString("F0");
                            using (var vf = new Font("Segoe UI", 7))
                            using (var vb = new SolidBrush(NavyDark))
                            {
                                var vs = g.MeasureString(vt, vf);
                                g.DrawString(vt, vf, vb, bx + (bw - vs.Width) / 2, by - 13);
                            }
                        }
                    }
                    g.DrawString(ds, xf, xb, bx - 2, pt + ch + 6);
                    idx++;
                }
            }
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // INVOICE STATUS CHART
    // ════════════════════════════════════════════════════════════════
    public class InvoiceStatusChart : Panel
    {
        private double paidAmount = 0, unpaidAmount = 0;
        private static readonly Color NavyDark  = ColorTranslator.FromHtml("#0F1552");
        private static readonly Color NavyMid   = ColorTranslator.FromHtml("#1D2068");
        private static readonly Color AccentGold= ColorTranslator.FromHtml("#F0A500");

        public InvoiceStatusChart()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.White;
            this.Paint += OnPaint;
            this.SizeChanged += (s, e) => Invalidate();
        }

        public void SetData(double paid, double unpaid)
        {
            paidAmount = paid; unpaidAmount = unpaid; Invalidate();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var r = this.ClientRectangle;

            // Card background
            using (var path = RoundedPath(r, 14))
            using (var br = new SolidBrush(Color.White))
                g.FillPath(br, path);
            using (var pen = new Pen(Color.FromArgb(18, 0, 0, 0), 1))
            using (var path = RoundedPath(r, 14))
                g.DrawPath(pen, path);

            // Title strip
            using (var lgb = new LinearGradientBrush(
                new Rectangle(0, 0, r.Width, 42), NavyMid, NavyDark, LinearGradientMode.Horizontal))
            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, 28, 28, 180, 90);
                path.AddArc(r.Width - 28, 0, 28, 28, 270, 90);
                path.AddLine(r.Width, 14, r.Width, 42);
                path.AddLine(0, 42, 0, 14);
                path.CloseFigure();
                g.FillPath(lgb, path);
            }
            using (var pen = new Pen(AccentGold, 2))
                g.DrawLine(pen, 0, 42, r.Width, 42);
            using (var f = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var br = new SolidBrush(Color.White))
                g.DrawString("🥧  Payment Status", f, br, 12, 11);

            double total = paidAmount + unpaidAmount;
            if (total == 0)
            {
                using (var f = new Font("Segoe UI", 9, FontStyle.Italic))
                using (var br = new SolidBrush(Color.Gray))
                    g.DrawString("No data available.", f, br, r.Width / 2 - 55, r.Height / 2);
                return;
            }

            float paidAngle   = (float)(paidAmount   / total * 360f);
            float unpaidAngle = (float)(unpaidAmount  / total * 360f);

            int size = Math.Max(60, Math.Min(r.Width - 40, r.Height - 110));
            int cx = (r.Width - size) / 2;
            int cy = 52 + (r.Height - 52 - size - 35) / 2;

            var rect = new Rectangle(cx, cy, size, size);

            // Pie slices
            Color paidColor   = Color.FromArgb(16, 185, 129);
            Color unpaidColor = Color.FromArgb(239, 68, 68);
            using (var b = new SolidBrush(paidColor))
                g.FillPie(b, rect, -90, paidAngle);
            using (var b = new SolidBrush(unpaidColor))
                g.FillPie(b, rect, -90 + paidAngle, unpaidAngle);

            // Donut hole
            int inner = (int)(size * 0.55f);
            int icx = cx + (size - inner) / 2;
            int icy = cy + (size - inner) / 2;
            using (var b = new SolidBrush(Color.White))
                g.FillEllipse(b, icx, icy, inner, inner);

            // Center text
            double pct = paidAmount / total * 100;
            using (var f = new Font("Segoe UI", 13, FontStyle.Bold))
            using (var br = new SolidBrush(NavyDark))
            {
                string t = pct.ToString("F0") + "%";
                var sz = g.MeasureString(t, f);
                g.DrawString(t, f, br, icx + (inner - sz.Width) / 2, icy + inner / 2 - sz.Height);
            }
            using (var f = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var br = new SolidBrush(Color.FromArgb(120, 130, 150)))
            {
                string t = "PAID";
                var sz = g.MeasureString(t, f);
                g.DrawString(t, f, br, icx + (inner - sz.Width) / 2, icy + inner / 2 + 2);
            }

            // Legend
            int ly = cy + size + 10;
            using (var f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                string t1 = $"Paid: {paidAmount:N0} PKR";
                string t2 = $"Unpaid: {unpaidAmount:N0} PKR";
                
                int w1 = (int)g.MeasureString(t1, f).Width + 20;
                int w2 = (int)g.MeasureString(t2, f).Width + 20;
                
                int spacing = 24;
                int totalW = w1 + spacing + w2;
                
                int lx1 = (r.Width - totalW) / 2;
                int lx2 = lx1 + w1 + spacing;
                
                DrawLegendItem(g, lx1, ly, paidColor, t1);
                DrawLegendItem(g, lx2, ly, unpaidColor, t2);
            }
        }

        private void DrawLegendItem(Graphics g, int x, int y, Color c, string text)
        {
            using (var b = new SolidBrush(c))
                g.FillRectangle(b, x, y + 2, 10, 10);
            using (var f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var br = new SolidBrush(ColorTranslator.FromHtml("#1D2068")))
                g.DrawString(text, f, br, x + 15, y);
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}