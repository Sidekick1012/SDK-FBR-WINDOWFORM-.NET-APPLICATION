using System;
using System.Collections.Generic;
using System.ComponentModel;
using PdfSharp.Fonts;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using QRCoder;
using HCR_E_INVOICING_SYSTEM.Data;
using static QRCoder.PayloadGenerator;

public class InvoicePreviewForm : Form
{
    private int invoiceId;
    private Label lblFurtherTax;
    private Label lblInvoiceNumber, lblFbrNumber, lblDate, lblStatus;
    private Label lblSubTotal, lblTax, lblGrandTotal, lblDueAmount;
    private Label lblSellerInfo, lblCustomerInfo;
    private DataGridView dgvItems;
    private PictureBox fbrLogo, sellerlogo, qrBox;
    private Button btnPrint, btnPdf;

    private Panel pnlInvoice;
    private TableLayoutPanel mainPanel;

    // Multi-page printing variables
    private int currentPrintPage = 0;
    private int totalPrintPages = 0;
    private Bitmap[] pageBitmaps = null;

    // Printing fields (legacy — kept for PrintDocument event handler compatibility)
    private PrintDocument invoicePrintDoc;

    // Invoice status fields
    private bool invoiceIsPaid = false;
    private decimal invoiceDueAmount = 0m;
    private decimal invoiceTotalAmount = 0m;
    private decimal invoicePaidAmount = 0m;

    // Footer panel reference
    private Panel footerPanel;
    private Label footerTextLabel;
    private Label computerGeneratedLabel;

    // For async operations
    private System.Windows.Forms.Timer loadTimer;
    private bool isGeneratingPdf = false;

    // PDF generation variables
    private string currentInvoiceFooter = "";

    public InvoicePreviewForm(int invoiceId)
    {
        this.invoiceId = invoiceId;
        this.Text = "Invoice Preview";
        this.Width = 1200;
        this.Height = 900;
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.FromArgb(245, 247, 250);
        this.AutoScroll = true;
        this.MaximizeBox = false;
        this.Padding = new Padding(20);
        this.DoubleBuffered = true; // Reduce flickering

        InitializeLayout();

        // Load data after UI is rendered
        loadTimer = new System.Windows.Forms.Timer();
        loadTimer.Interval = 100;
        loadTimer.Tick += LoadTimer_Tick;
        loadTimer.Start();
    }

    private void LoadTimer_Tick(object sender, EventArgs e)
    {
        loadTimer.Stop();
        LoadInvoiceData();
    }

    private void InitializeLayout()
    {
        // ===== PRINT BUTTON =====
        /*btnPrint = new Button
        {
            Text = "🖨 Print Invoice",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Width = 180,
            Height = 38,
            BackColor = Color.FromArgb(30, 60, 114),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(this.ClientSize.Width - 410, 15)
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += BtnPrint_Click;
        this.Controls.Add(btnPrint);*/

        // ===== PDF BUTTON =====
        btnPdf = new Button
        {
            Text = "📄 Download PDF",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Width = 180,
            Height = 38,
            BackColor = Color.FromArgb(0, 102, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(this.ClientSize.Width - 210, 15)
        };
        btnPdf.FlatAppearance.BorderSize = 0;
        btnPdf.Click += BtnPdf_Click;
        this.Controls.Add(btnPdf);

        // ===== PANEL FOR INVOICE =====
        pnlInvoice = new Panel
        {
            Name = "pnlInvoice",
            Width = Math.Max(900, this.ClientSize.Width - 100),
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            Location = new Point(50, 70),
            Padding = new Padding(30, 20, 30, 30),
            AutoScroll = false,
            AutoSize = false
        };

        this.Controls.Add(pnlInvoice);

        // Re-center panel on resize
        this.Resize += (s, e) =>
        {
            pnlInvoice.Width = Math.Max(900, this.ClientSize.Width - 100);
            pnlInvoice.Location = new Point((this.ClientSize.Width - pnlInvoice.Width) / 2, 70);
        };

        // ===== MAIN LAYOUT PANEL =====
        mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0)
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pnlInvoice.Controls.Add(mainPanel);

        // ===== VERTICAL INVOICE TEXT =====
        Label lblVerticalInvoice = new Label
        {
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 114),
            AutoSize = false,
            Width = 28,
            Height = 330,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 200),
            BackColor = Color.Transparent
        };

        lblVerticalInvoice.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TranslateTransform(0, lblVerticalInvoice.Height);
            e.Graphics.RotateTransform(-90);

            using (SolidBrush brush = new SolidBrush(lblVerticalInvoice.ForeColor))
            {
                e.Graphics.DrawString("SALES TAX INVOICE", lblVerticalInvoice.Font, brush, 0, 0);
            }

            e.Graphics.ResetTransform();
        };
        pnlInvoice.Controls.Add(lblVerticalInvoice);
        lblVerticalInvoice.BringToFront();

        // ===== HEADER =====
        var headerTable = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Height = 150
        };
        headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        FlowLayoutPanel leftHeader = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Height = 120
        };
        sellerlogo = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Width = 250,
            Height = 90,
            Margin = new Padding(0, 0, 0, 10)
        };
        leftHeader.Controls.Add(sellerlogo);

        lblInvoiceNumber = new Label
        {
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        };
        lblFbrNumber = new Label
        {
            Font = new Font("Segoe UI", 12),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        };
        lblDate = new Label
        {
            Font = new Font("Segoe UI", 12),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        };
        lblStatus = new Label
        {
            Font = new Font("Segoe UI", 12, FontStyle.Italic),
            AutoSize = true,
            ForeColor = Color.DarkGreen,
            Margin = new Padding(0, 0, 0, 5)
        };

        leftHeader.Controls.Add(lblInvoiceNumber);
        leftHeader.Controls.Add(lblFbrNumber);
        leftHeader.Controls.Add(lblDate);
        leftHeader.Controls.Add(lblStatus);
        headerTable.Controls.Add(leftHeader, 0, 0);

        FlowLayoutPanel rightHeader = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            RightToLeft = RightToLeft.Yes,
            Height = 120
        };
        fbrLogo = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Width = 100,
            Height = 100,
            Margin = new Padding(10, 0, 0, 0)
        };
        qrBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Width = 100,
            Height = 100,
            Margin = new Padding(10, 0, 0, 0)
        };
        rightHeader.Controls.Add(fbrLogo);
        rightHeader.Controls.Add(qrBox);
        headerTable.Controls.Add(rightHeader, 1, 0);

        mainPanel.Controls.Add(headerTable);

        // ===== TOP NOTICE (below FBR logo / QR) =====
        var noticeRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Height = 30
        };
        noticeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        // Let the right column auto-size to the notice so text won't wrap
        noticeRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Empty spacer on the left to keep notice aligned with the right header column
        var leftSpacer = new Label { AutoSize = true, Text = "", Dock = DockStyle.Fill };
        var topNoticeLabel = new Label
        {
            Text = "This is a computer generated invoice. No signature or stamp required.",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 100, 100),
            AutoSize = true,
            Height = 24,
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Right,
            UseMnemonic = false
        };

        noticeRow.Controls.Add(leftSpacer, 0, 0);
        noticeRow.Controls.Add(topNoticeLabel, 1, 0);
        mainPanel.Controls.Add(noticeRow);

        // Separator line
        Panel separator1 = new Panel
        {
            Height = 2,
            Dock = DockStyle.Top,
            BackColor = Color.LightGray,
            Margin = new Padding(0, 10, 0, 10)
        };
        mainPanel.Controls.Add(separator1);

        // ===== SELLER / BUYER INFO PANEL =====
        var infoPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 20, 0, 10),
            Height = 200
        };
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // ===== Seller Box =====
        var sellerBox = new GroupBox
        {
            Text = "Seller Information",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Padding = new Padding(10),
            ForeColor = Color.FromArgb(30, 60, 114),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 160)
        };

        lblSellerInfo = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 11.5f),
            MaximumSize = new Size(430, 0),  // wide enough; height=0 means unlimited
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(4, 2, 4, 4)
            // No Dock.Fill — conflicts with AutoSize and clips long addresses
        };
        // Ensure ampersands (&) render literally instead of being treated as mnemonics
        lblSellerInfo.UseMnemonic = false;

        sellerBox.Controls.Add(lblSellerInfo);

        // ===== Buyer Box =====
        var buyerBox = new GroupBox
        {
            Text = "Buyer Information",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Padding = new Padding(10),
            ForeColor = Color.FromArgb(30, 60, 114),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 160)
        };

        lblCustomerInfo = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 11.5f),
            MaximumSize = new Size(430, 0),  // wide enough; height=0 means unlimited
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(4, 2, 4, 4)
            // No Dock.Fill — conflicts with AutoSize and clips long addresses
        };
        lblCustomerInfo.UseMnemonic = false;

        buyerBox.Controls.Add(lblCustomerInfo);

        infoPanel.Controls.Add(sellerBox, 0, 0);
        infoPanel.Controls.Add(buyerBox, 1, 0);
        mainPanel.Controls.Add(infoPanel);

        // ===== DATAGRIDVIEW =====
        dgvItems = new DataGridView
        {
            Dock = DockStyle.Top,
            ReadOnly = true,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            GridColor = Color.LightGray,
            EnableHeadersVisualStyles = false,
            ScrollBars = ScrollBars.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ColumnHeadersHeight = 60,
            MinimumSize = new Size(0, 200)
        };

        // Enable double buffering for smooth scrolling
        typeof(DataGridView).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.SetProperty,
            null, dgvItems, new object[] { true });

        // ===== HEADER STYLE =====
        dgvItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10f);
        dgvItems.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
        dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        dgvItems.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

        // ===== CELL STYLE =====
        dgvItems.DefaultCellStyle.Font = new Font("Segoe UI", 10f);
        dgvItems.DefaultCellStyle.Padding = new Padding(5, 6, 5, 6);
        dgvItems.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        dgvItems.DefaultCellStyle.BackColor = Color.White;
        dgvItems.DefaultCellStyle.SelectionBackColor = Color.White;
        dgvItems.DefaultCellStyle.SelectionForeColor = Color.Black;
        dgvItems.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);

        // ===== HEADER WRAPPING =====
        // Force creation of window handle to ensure correct DPI detection in the constructor
        var forceHandle = this.Handle;
        float dpiScale = 1.0f;
        using (Graphics g = this.CreateGraphics())
        {
            dpiScale = g.DpiX / 96.0f;
        }
        dgvItems.ColumnHeadersHeight = (int)Math.Round(56 * dpiScale);

        // ===== COLUMNS =====
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "hsCode", HeaderText = "Item Code", FillWeight = 75 });

        var descCol = new DataGridViewTextBoxColumn
        {
            Name = "productDescription",
            HeaderText = "Item Description",
            FillWeight = 200,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                WrapMode = DataGridViewTriState.True,
                Alignment = DataGridViewContentAlignment.TopLeft
            }
        };
        dgvItems.Columns.Add(descCol);

        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "unitPrice",  HeaderText = "Unit\nPrice",            FillWeight = 75 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "quantity",   HeaderText = "Qty",                    FillWeight = 40 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalGross", HeaderText = "Gross\nValue",           FillWeight = 90 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "discount",   HeaderText = "Discount",               FillWeight = 75 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalEx",    HeaderText = "Amt Excl.\nS.Tax",       FillWeight = 95 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "rate",       HeaderText = "Tax\n%",                 FillWeight = 45 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaxValue",   HeaderText = "Sales Tax\nValue",       FillWeight = 95 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalInc",   HeaderText = "Amt Incl.\nS.Tax",       FillWeight = 95 });

        // ===== ALIGNMENT =====
        foreach (DataGridViewColumn col in dgvItems.Columns)
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        dgvItems.Columns["productDescription"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
        dgvItems.Columns["hsCode"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        // ===== ADD TO MAIN PANEL =====
        mainPanel.Controls.Add(dgvItems);

        // ===== TOTALS =====
        var totalsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Padding = new Padding(0, 15, 20, 10),
            Height = 140
        };
        totalsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        totalsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        Label MakeRightLabel(string text, bool bold = false, Color? foreColor = null)
        {
            Color color = foreColor ?? Color.Black;
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", bold ? 13f : 12f, bold ? FontStyle.Bold : FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Height = 25,
                ForeColor = color
            };
        }

        totalsPanel.Controls.Add(MakeRightLabel("Subtotal:"));
        totalsPanel.Controls.Add(lblSubTotal = MakeRightLabel("0.00"));
        totalsPanel.Controls.Add(MakeRightLabel("Sales Tax:"));
        totalsPanel.Controls.Add(lblTax = MakeRightLabel("0.00"));
        totalsPanel.Controls.Add(MakeRightLabel("Further Tax:"));
        totalsPanel.Controls.Add(lblFurtherTax = MakeRightLabel("0.00"));
        totalsPanel.Controls.Add(MakeRightLabel("Grand Total:", true));
        totalsPanel.Controls.Add(lblGrandTotal = MakeRightLabel("0.00", true));

        // Add Due Amount row (initially hidden)
        totalsPanel.Controls.Add(MakeRightLabel("Due Amount:", true, Color.Red));
        totalsPanel.Controls.Add(lblDueAmount = MakeRightLabel("0.00", true, Color.Red));

        mainPanel.Controls.Add(totalsPanel);

        // ===== COMPUTER GENERATED NOTICE (VISIBLE ON INVOICE) =====
        computerGeneratedLabel = new Label
        {
            Text = "This is a computer generated invoice. No signature or stamp required.",
            Font = new Font("Segoe UI", 10f, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 100, 100),
            AutoSize = false,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 10, 0, 10),
            UseMnemonic = false,
            Visible = false // Hidden to avoid duplicate; top notice under FBR/QR is used instead
        };
        mainPanel.Controls.Add(computerGeneratedLabel);

        // ===== BOTTOM SPACER PANEL (FOR FOOTER SPACING) =====
        Panel bottomSpacerPanel = new Panel
        {
            Name = "bottomSpacerPanel",
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(bottomSpacerPanel);

        // ===== MODERN FOOTER - FIXED TO BOTTOM =====
        footerPanel = new Panel
        {
            Name = "footerPanel",
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 20, 0, 0)
        };

        // Add separator line above footer
        Panel footerSeparator = new Panel
        {
            Height = 1,
            Dock = DockStyle.Top,
            BackColor = Color.LightGray,
            Margin = new Padding(20, 0, 20, 15)
        };

        footerTextLabel = new Label
        {
            Text = "Phone: +92 51 6144660 | Mobile: +92 300 230 2463, +92 332 5494660 | Email: usmanenterprises63@gmail.com",
            Font = new Font("Segoe UI", 11f, FontStyle.Regular),
            ForeColor = Color.FromArgb(120, 120, 120),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            UseMnemonic = false
        };

        footerPanel.Controls.Add(footerTextLabel);
        footerPanel.Controls.Add(footerSeparator);
        footerPanel.Controls.SetChildIndex(footerSeparator, 0);

        mainPanel.Controls.Add(footerPanel);

        // ===== COMPUTER GENERATED LABEL =====
         computerGeneratedLabel = new Label
         {
             // Ensure the exact requested wording is shown on the invoice
             Text = "This is a computer generated invoice. No signature or stamp required.",
             Font = new Font("Segoe UI", 10f, FontStyle.Italic),
             ForeColor = Color.FromArgb(100, 100, 100),
             Dock = DockStyle.Bottom,
             Height = 40,
             TextAlign = ContentAlignment.MiddleCenter,
             UseMnemonic = false,
             Visible = false // Keep bottom notice hidden so only top notice below FBR/QR is shown
         };
         pnlInvoice.Controls.Add(computerGeneratedLabel);
    }

    private void LoadInvoiceData()
    {
        try
        {
            DataSet ds = DatabaseHelper.GetInvoicePreviewData(invoiceId);
            if (ds == null || ds.Tables.Count == 0)
            {
                ShowErrorMessage("No data returned from database");
                return;
            }

            if (ds.Tables["InvoiceHeader"].Rows.Count == 0)
            {
                ShowErrorMessage("Invoice not found!");
                return;
            }

            DataRow header = ds.Tables["InvoiceHeader"].Rows[0];

            // Load seller logo
            try
            {
                if (header.Table.Columns.Contains("sellerLogoPath") && header["sellerLogoPath"] != DBNull.Value)
                {
                    byte[] logoBytes = (byte[])header["sellerLogoPath"];
                    using (MemoryStream ms = new MemoryStream(logoBytes))
                        sellerlogo.Image = Image.FromStream(ms);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading seller logo: " + ex.Message);
                sellerlogo.Image = null;
            }

            // Load FBR logo
            try
            {
                string fbrPath = Path.Combine(Application.StartupPath, "FBR_DIGITAL.PNG");
                if (File.Exists(fbrPath))
                    fbrLogo.Image = Image.FromFile(fbrPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading FBR logo: " + ex.Message);
            }

            // Set invoice details
            lblInvoiceNumber.Text = $"Invoice #: {header["invoiceNumber"]}";
            lblFbrNumber.Text = $"FBR Invoice #: {header["fbrInvoiceNumber"]}";
            lblDate.Text = $"Date: {Convert.ToDateTime(header["invoiceDate"]):yyyy-MM-dd}";

            lblSellerInfo.Text = $"{header["sellerBusinessName"]}\nNTN/CNIC: {header["sellerNTNCNIC"]}\nProvince: {header["sellerProvince"]}\nAddress: {header["sellerAddress"]}";
            lblCustomerInfo.Text = $"{header["customerBusinessName"]}\nNTN/CNIC: {header["customerNTNCNIC"]}\nProvince: {header["customerProvince"]}\nAddress: {header["customerAddress"]}";

            // Set dynamic invoice footer
            if (header.Table.Columns.Contains("invoiceFooter") && header["invoiceFooter"] != DBNull.Value && !string.IsNullOrWhiteSpace(header["invoiceFooter"].ToString()))
            {
                string rawFooter = header["invoiceFooter"].ToString();
                currentInvoiceFooter = rawFooter.Replace("\r\n", " | ").Replace("\n", " | ");
                while (currentInvoiceFooter.Contains(" |  | "))
                {
                    currentInvoiceFooter = currentInvoiceFooter.Replace(" |  | ", " | ");
                }
                footerTextLabel.Text = currentInvoiceFooter;
            }
            else
            {
                currentInvoiceFooter = footerTextLabel.Text.Replace("\r\n", " | ").Replace("\n", " | "); // fallback to hardcoded default
            }

            // Load items
            DataTable items = ds.Tables["InvoiceItems"];
            dgvItems.SuspendLayout();
            dgvItems.Rows.Clear();

            decimal subTotal = 0, totalTax = 0, furtherTax = 0, grandTotal = 0;

            foreach (DataRow row in items.Rows)
            {
                decimal qty = ParseDecimal(row["quantity"]);
                decimal unitPrice = ParseDecimal(row["unitPrice"]);

                bool hasRate = items.Columns.Contains("rate");
                decimal rate = hasRate ? ParseDecimal(row["rate"]) : 0m;

                bool hasDiscount = items.Columns.Contains("discount");
                decimal discount = hasDiscount ? ParseDecimal(row["discount"]) : 0m;

                string description = "";
                if (row["description"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["description"].ToString()))
                {
                    description = row["description"].ToString();
                }
                else if (row["productDescription"] != DBNull.Value)
                {
                    description = row["productDescription"].ToString();
                }

                bool hasFurtherTax = items.Columns.Contains("furtherTax");
                decimal further = hasFurtherTax ? ParseDecimal(row["furtherTax"]) : 0m;

                decimal totalGross = qty * unitPrice;
                decimal totalEx = totalGross - discount;
                if (totalEx < 0m) totalEx = 0m;
                decimal taxValue = totalEx * rate / 100m;
                decimal totalInc = totalEx + taxValue;

                subTotal += totalEx;
                totalTax += taxValue;
                furtherTax += further;
                grandTotal += totalInc;

                dgvItems.Rows.Add(
                    row["hsCode"],
                    description,
                    unitPrice,
                    qty,
                    totalGross,
                    discount,
                    totalEx,
                    rate,
                    taxValue,
                    totalInc
                );
            }

            dgvItems.ResumeLayout();

            // Calculate final totals
            invoiceTotalAmount = grandTotal + furtherTax;

            lblSubTotal.Text = $"{subTotal:N2}";
            lblTax.Text = $"{totalTax:N2}";
            lblFurtherTax.Text = $"{furtherTax:N2}";
            lblGrandTotal.Text = $"{invoiceTotalAmount:N2}";

            // ===== INVOICE STATUS DETECTION =====
            string invoiceStatus = "";
            if (header.Table.Columns.Contains("status") && header["status"] != DBNull.Value)
            {
                invoiceStatus = header["status"].ToString().Trim();
            }

            // Paid amount check
            invoicePaidAmount = 0m;
            if (header.Table.Columns.Contains("paidAmount") && header["paidAmount"] != DBNull.Value)
            {
                invoicePaidAmount = ParseDecimal(header["paidAmount"]);
            }

            // Determine if invoice is paid
            invoiceIsPaid = false;

            // Status se determine karen
            if (!string.IsNullOrEmpty(invoiceStatus) &&
                invoiceStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                invoiceIsPaid = true;
            }
            // Ya phir paid amount se check karen
            else if (invoicePaidAmount > 0m && invoiceTotalAmount > 0m && invoicePaidAmount >= invoiceTotalAmount)
            {
                invoiceIsPaid = true;
            }

            // Due amount calculate karen
            invoiceDueAmount = Math.Max(0m, invoiceTotalAmount - invoicePaidAmount);

            // ===== UPDATE UI BASED ON STATUS =====
            UpdateStatusUI(invoiceStatus);

            // ===== GENERATE QR CODE =====
            string fbrInvoiceNo = "";
            if (header["fbrInvoiceNumber"] != DBNull.Value)
            {
                fbrInvoiceNo = header["fbrInvoiceNumber"].ToString();
            }

            if (!string.IsNullOrEmpty(fbrInvoiceNo))
            {
                string qrData = $"https://www.fbr.gov.pk/verifyInvoice?invoice={fbrInvoiceNo}";
                using (QRCodeGenerator qrGen = new QRCodeGenerator())
                {
                    QRCodeData qrDataObj = qrGen.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                    using (QRCode qrCode = new QRCode(qrDataObj))
                    {
                        qrBox.Image = qrCode.GetGraphic(8);
                    }
                }
            }

            // Final height adjustment
            AdjustInvoiceHeight();

            // Force refresh
            dgvItems.Refresh();
            dgvItems.PerformLayout();

            // Final adjustment with delay
            System.Windows.Forms.Timer dataTimer = new System.Windows.Forms.Timer();
            dataTimer.Interval = 200;
            dataTimer.Tick += (s, e) =>
            {
                try
                {
                    int rowHeight = dgvItems.ColumnHeadersHeight;
                    foreach (DataGridViewRow row in dgvItems.Rows)
                    {
                        if (row.Visible)
                            rowHeight += row.Height;
                    }

                    dgvItems.Height = Math.Max(rowHeight, 200);
                    AdjustInvoiceHeight();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Timer error: " + ex.Message);
                }
                finally
                {
                    dataTimer.Stop();
                    dataTimer.Dispose();
                }
            };
            dataTimer.Start();
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Failed to load invoice data: {ex.Message}");
        }
    }

    private void UpdateStatusUI(string statusFromDB = "")
    {
        if (invoiceIsPaid)
        {
            // Invoice is paid - sirf "Paid" show karen
            lblStatus.Text = "Status: PAID ✓";
            lblStatus.ForeColor = Color.DarkGreen;

            // Due Amount ko COMPLETELY HIDE karen
            HideDueAmountRow();
        }
        else
        {
            // Invoice is unpaid
            lblStatus.Text = "Status: UNPAID";

            if (invoiceDueAmount > 0)
            {
                // There is a due amount
                lblStatus.Text += $" (Due: {invoiceDueAmount:N2})";
                lblStatus.ForeColor = Color.Red;

                // Show Due Amount row
                ShowDueAmountRow(invoiceDueAmount);
            }
            else
            {
                // No due amount
                lblStatus.ForeColor = Color.OrangeRed;

                // Show Due Amount as 0.00
                ShowDueAmountRow(0m);
            }
        }
    }

    private void HideDueAmountRow()
    {
        // Due Amount value hide karen
        lblDueAmount.Visible = false;

        // "Due Amount:" label bhi hide karen
        if (lblDueAmount.Parent != null)
        {
            foreach (Control control in lblDueAmount.Parent.Controls)
            {
                if (control is Label label && label.Text.Contains("Due Amount"))
                {
                    label.Visible = false;
                    break;
                }
            }
        }
    }

    private void ShowDueAmountRow(decimal amount)
    {
        // Due Amount value show karen
        lblDueAmount.Text = $"{amount:N2}";
        lblDueAmount.Visible = true;
        lblDueAmount.ForeColor = amount > 0 ? Color.Red : Color.OrangeRed;

        // "Due Amount:" label show karen
        if (lblDueAmount.Parent != null)
        {
            foreach (Control control in lblDueAmount.Parent.Controls)
            {
                if (control is Label label && label.Text.Contains("Due Amount"))
                {
                    label.Visible = true;
                    label.ForeColor = amount > 0 ? Color.Red : Color.OrangeRed;
                    break;
                }
            }
        }
    }

    private decimal ParseDecimal(object value)
    {
        if (value == null || value == DBNull.Value)
            return 0m;
        if (value is decimal dec)
            return dec;
        if (value is double d)
            return Convert.ToDecimal(d);
        if (value is float f)
            return Convert.ToDecimal(f);
        if (value is int i)
            return i;

        string s = value.ToString().Trim();
        if (string.IsNullOrEmpty(s))
            return 0m;

        // Accept percentage values like "5%"
        if (s.EndsWith("%"))
            s = s.TrimEnd('%').Trim();

        decimal result;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            return result;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
            return result;

        string cleaned = Regex.Replace(s, "[^\\d\\-\\.,]", "");
        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            return result;

        return 0m;
    }

    private void AdjustInvoiceHeight()
    {
        try
        {
            this.SuspendLayout();
            pnlInvoice.SuspendLayout();
            mainPanel.SuspendLayout();

            // Calculate DataGridView height
            int dgvHeight = dgvItems.ColumnHeadersHeight;
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                if (row.Visible)
                    dgvHeight += row.Height;
            }

            // Set DataGridView height
            dgvItems.Height = Math.Max(dgvHeight, 200);

            // Calculate total height of all controls BEFORE footer
            int contentHeight = 0;
            bool footerAdded = false;

            foreach (Control control in mainPanel.Controls)
            {
                if (control.Visible)
                {
                    // Don't count footer in initial content height
                    if (control.Name == "footerPanel")
                    {
                        footerAdded = true;
                        continue;
                    }

                    contentHeight += control.Height + control.Margin.Top + control.Margin.Bottom;
                }
            }

            // Set main panel and invoice panel height
            int totalHeight = contentHeight + (footerAdded ? footerPanel.Height + footerPanel.Margin.Top + footerPanel.Margin.Bottom : 0);
            mainPanel.Height = totalHeight;
            pnlInvoice.Height = totalHeight + pnlInvoice.Padding.Top + pnlInvoice.Padding.Bottom;

            // Calculate available space in pnlInvoice
            int availableSpace = pnlInvoice.ClientSize.Height - contentHeight - 30; // 30 for padding

            // Adjust footer spacing to push it to bottom if there's space
            if (availableSpace > 50)
            {
                Control bottomSpacer = mainPanel.Controls["bottomSpacerPanel"];
                if (bottomSpacer != null)
                {
                    bottomSpacer.Height = availableSpace - 30;
                }

                // Also adjust footer margin
                footerPanel.Margin = new Padding(0, 10, 0, 0);
            }
            else
            {
                // No extra space, reset to defaults
                Control bottomSpacer = mainPanel.Controls["bottomSpacerPanel"];
                if (bottomSpacer != null)
                {
                    bottomSpacer.Height = 50;
                }

                footerPanel.Margin = new Padding(0, 20, 0, 0);
            }

            // Enable form scrolling
            this.AutoScrollMinSize = new Size(0, pnlInvoice.Top + totalHeight + 100);

            // Force refresh
            this.Refresh();

            mainPanel.ResumeLayout(true);
            pnlInvoice.ResumeLayout(true);
            this.ResumeLayout(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Height adjustment error: " + ex.Message);
        }
    }

    // ===== PRINTING WITH FOOTER FIX =====
    private void BtnPrint_Click(object sender, EventArgs e)
    {
        try
        {
            if (btnPrint != null) { btnPrint.Enabled = false; btnPrint.Text = "Preparing..."; }
            Application.DoEvents();

            AdjustInvoiceHeight();

            // Capture invoice content
            Bitmap fullBmp = null;
            try
            {
                // Hide the on-screen footer while capturing to avoid duplicating footer text in the printed content
                bool prevFooterVisible = footerPanel?.Visible ?? false;
                try
                {
                    footerPanel.Visible = false;
                    fullBmp = CaptureInvoiceBitmap();
                    if (fullBmp == null)
                    {
                        ShowErrorMessage("Failed to capture invoice for printing");
                        return;
                    }
                }
                finally
                {
                    // Restore footer visibility
                    if (footerPanel != null) footerPanel.Visible = prevFooterVisible;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Bitmap capture error: " + ex.Message);
                ShowErrorMessage("Failed to capture invoice for printing: " + ex.Message);
                return;
            }

            // Reset page counters
            currentPrintPage = 0;
            totalPrintPages = 0;
            pageBitmaps = null;

            PrintDocument printDoc = new PrintDocument();
            printDoc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            // Ensure margins and origin
            printDoc.OriginAtMargins = true;
            printDoc.DefaultPageSettings.Margins = new Margins(40, 40, 50, 80);
            printDoc.DefaultPageSettings.Landscape = false;
            printDoc.PrintController = new StandardPrintController();
            printDoc.PrintPage += PrintDocument_PrintPage;
            printDoc.EndPrint += PrintDocument_EndPrint;

            // Let user pick printer/settings, then calculate pages based on those settings and print
            try
            {
                using (PrintDialog pd = new PrintDialog())
                {
                    pd.AllowSomePages = true;
                    pd.Document = printDoc;

                    if (pd.ShowDialog() == DialogResult.OK)
                    {
                        // Printer settings are applied to printDoc via pd.Document assignment

                        // Ensure printDoc uses selected PrinterSettings explicitly
                        printDoc.PrinterSettings = pd.PrinterSettings;
                        try { printDoc.DefaultPageSettings = pd.Document.DefaultPageSettings; } catch { }

                        // Recalculate pages using selected printer settings
                        CalculatePrintPages(fullBmp, printDoc);

                        // Ensure pageBitmaps produced; fallback to single page from fullBmp
                        if (pageBitmaps == null || pageBitmaps.Length == 0)
                        {
                            pageBitmaps = new Bitmap[] { (Bitmap)fullBmp.Clone() };
                            totalPrintPages = pageBitmaps.Length;
                        }

                        // Save a sample page to temp for debugging (so user can open and verify before printing)
                        try
                        {
                            string tempPath = Path.Combine(Path.GetTempPath(), $"invoice_preview_{invoiceId}_page0.png");
                            pageBitmaps[0].Save(tempPath, ImageFormat.Png);
                            Console.WriteLine("Saved sample page bitmap to: " + tempPath);
                        }
                        catch (Exception exSave)
                        {
                            Console.WriteLine("Could not save sample page bitmap: " + exSave.Message);
                        }

                        // Show print preview so user can verify alignment
                        try
                        {
                            using (PrintPreviewDialog previewDlg = new PrintPreviewDialog())
                            {
                                previewDlg.Document = printDoc;
                                previewDlg.Width = 1000;
                                previewDlg.Height = 800;
                                try { previewDlg.PrintPreviewControl.Zoom = 1.0; } catch { }
                                previewDlg.ShowDialog();
                            }
                        }
                        catch (Exception exPreview)
                        {
                            Console.WriteLine("Preview failed: " + exPreview.Message);
                        }

                        // After preview, confirm print
                        try
                        {
                            using (PrintDialog pd2 = new PrintDialog())
                            {
                                pd2.AllowSomePages = true;
                                pd2.Document = printDoc;

                                if (pd2.ShowDialog() == DialogResult.OK)
                                {
                                    // apply selected printer settings
                                    printDoc.PrinterSettings = pd2.PrinterSettings;
                                    try { printDoc.DefaultPageSettings = pd2.Document.DefaultPageSettings; } catch { }

                                    // Reset current page and print
                                    currentPrintPage = 0;
                                    printDoc.Print();
                                }
                            }
                        }
                        catch (Exception exPrint)
                        {
                            Console.WriteLine("Final print failed: " + exPrint.Message);
                            ShowErrorMessage("Printing failed: " + exPrint.Message);
                        }
                    }
                }
            }
            catch (Exception exPrint)
            {
                Console.WriteLine("Print dialog/print failed: " + exPrint.Message);
                ShowErrorMessage("Printing failed: " + exPrint.Message);
            }
            finally
            {
                // Always cleanup handlers and document
                printDoc.PrintPage -= PrintDocument_PrintPage;
                printDoc.EndPrint -= PrintDocument_EndPrint;
                printDoc.Dispose();
            }

            if (fullBmp != null)
                fullBmp.Dispose();
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Printing failed: " + ex.Message);
        }
        finally
        {
            if (btnPrint != null) { btnPrint.Enabled = true; btnPrint.Text = "🖨 Print Invoice"; }
        }
    }

    private void CalculatePrintPages(Bitmap fullBmp, PrintDocument printDoc)
    {
        try
        {
            // Use printer margins and bounds to calculate printable area
            int marginTop = 50;
            int marginBottom = 80; // Extra space for footer

            int pageBoundsWidth = printDoc.DefaultPageSettings.Bounds.Width;
            int pageBoundsHeight = printDoc.DefaultPageSettings.Bounds.Height;

            int marginsLeft = printDoc.DefaultPageSettings.Margins.Left;
            int marginsRight = printDoc.DefaultPageSettings.Margins.Right;
            int marginsTop = printDoc.DefaultPageSettings.Margins.Top;
            int marginsBottom = printDoc.DefaultPageSettings.Margins.Bottom;

            int printableWidth = Math.Max(1, pageBoundsWidth - marginsLeft - marginsRight - 20);
            int availableHeight = Math.Max(1, pageBoundsHeight - marginTop - marginBottom - marginsTop - marginsBottom);

            // Calculate scale to fit width
            float scale = (float)printableWidth / fullBmp.Width;
            float scaledContentHeight = fullBmp.Height * scale;

            // Calculate number of pages needed
            totalPrintPages = (int)Math.Ceiling(scaledContentHeight / availableHeight);
            if (totalPrintPages < 1) totalPrintPages = 1;

            // Create page bitmaps
            pageBitmaps = new Bitmap[totalPrintPages];

            for (int i = 0; i < totalPrintPages; i++)
            {
                // Compute source Y position in original bitmap corresponding to this page
                float srcYOffset = i * (availableHeight / scale);
                int startY = Math.Max(0, (int)Math.Round(srcYOffset));

                // Compute source height in original bitmap for this page
                int srcHeight = (int)Math.Min(Math.Round(availableHeight / scale), fullBmp.Height - startY);
                if (srcHeight <= 0 && startY < fullBmp.Height)
                    srcHeight = fullBmp.Height - startY;
                if (srcHeight <= 0)
                    srcHeight = 1;

                // Create and set resolution for this page bitmap to match printer (use 300 DPI)
                Bitmap bmpPage = new Bitmap(fullBmp.Width, srcHeight, PixelFormat.Format24bppRgb);
                bmpPage.SetResolution(300f, 300f);

                using (Graphics g = Graphics.FromImage(bmpPage))
                {
                    // Important: clear with white to avoid transparency
                    g.Clear(Color.White);
                    g.DrawImage(fullBmp, new Rectangle(0, 0, fullBmp.Width, srcHeight),
                               new Rectangle(0, startY, fullBmp.Width, srcHeight),
                               GraphicsUnit.Pixel);
                }

                // Store the generated page bitmap for printing
                pageBitmaps[i] = bmpPage;
             }
         }
         catch (Exception ex)
         {
             Console.WriteLine("CalculatePrintPages error: " + ex.Message);
             totalPrintPages = 1;
             pageBitmaps = new Bitmap[] { fullBmp };
         }
    }

    private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
    {
        try
        {
            if (pageBitmaps == null || pageBitmaps.Length == 0 || currentPrintPage >= pageBitmaps.Length)
            {
                e.HasMorePages = false;
                return;
            }

            Bitmap pageBmp = pageBitmaps[currentPrintPage];
            if (pageBmp == null)
            {
                e.HasMorePages = false;
                return;
            }

            // Map to PDF layout so printed output matches generated PDF
            // PDF: A4 = 595pt width, we use 50pt margins in PDF generation
            const double pdfPageWidthPoints = 595.0;
            const double pdfMarginPoints = 50.0;
            double pdfContentWidthPoints = pdfPageWidthPoints - (2 * pdfMarginPoints); // 495pt

            // Convert PDF points to printer pixels using device DPI
            float dpiX = e.Graphics.DpiX;
            float dpiY = e.Graphics.DpiY;
            int targetContentWidthPixels = Math.Max(1, (int)Math.Round(pdfContentWidthPoints * dpiX / 72.0));

            // Scale bitmap to target PDF content width in pixels
            float scale = (float)targetContentWidthPixels / (float)pageBmp.Width;

            int destWidth = Math.Max(1, (int)Math.Round(pageBmp.Width * scale));
            int destHeight = Math.Max(1, (int)Math.Round(pageBmp.Height * scale));

            // Compute left offset to match PDF left margin (convert 50pt to pixels)
            int leftOffsetPixels = Math.Max(0, (int)Math.Round(pdfMarginPoints * dpiX / 72.0));

            // Compute candidate destX relative to physical page
            int pageLeft = e.PageBounds.Left;
            int candidateDestX = pageLeft + leftOffsetPixels;

            // Ensure content is not placed outside printable margins
            int minAllowedX = e.MarginBounds.Left;
            int maxAllowedWidth = e.MarginBounds.Width;

            if (destWidth > maxAllowedWidth)
            {
                // If target width exceeds printable area, scale down to printable width
                float s2 = (float)maxAllowedWidth / (float)destWidth;
                destWidth = maxAllowedWidth;
                destHeight = Math.Max(1, (int)Math.Round(destHeight * s2));
                candidateDestX = minAllowedX;
            }

            int destX = Math.Max(minAllowedX, candidateDestX);
            // If candidateDestX would push content too far right such that right edge > margin right, adjust left to center in printable area
            if (destX + destWidth > e.MarginBounds.Right)
            {
                destX = e.MarginBounds.Left + Math.Max(0, (e.MarginBounds.Width - destWidth) / 2);
            }

            // Place at top margin vertically
            int destY = e.MarginBounds.Top;

            Rectangle destRect = new Rectangle(destX, destY, destWidth, destHeight);

            // Improve rendering quality for printing
            e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            e.Graphics.FillRectangle(Brushes.White, destRect);
            // Use explicit source rectangle to avoid issues
            Rectangle srcRect = new Rectangle(0, 0, pageBmp.Width, pageBmp.Height);
            e.Graphics.DrawImage(pageBmp, destRect, srcRect, GraphicsUnit.Pixel);

            // ===== ADD FOOTER ON EVERY PAGE =====
            AddFooterToPrintPage(e);

            // Draw page number just below the footer line (keep centered on physical page)
            string pageInfo = $"Page {currentPrintPage + 1} of {totalPrintPages}";
            using (Font pageFont = new Font("Arial", 7))
            {
                float footerY = e.MarginBounds.Bottom - 40; // same reference as AddFooterToPrintPage
                SizeF textSize = e.Graphics.MeasureString(pageInfo, pageFont);
                float textX = e.PageBounds.Left + (e.PageBounds.Width - textSize.Width) / 2;
                float textY = footerY + 18;
                if (textY + textSize.Height > e.PageBounds.Bottom)
                    textY = e.PageBounds.Bottom - textSize.Height - 5;

                e.Graphics.DrawString(pageInfo, pageFont, Brushes.Black, textX, textY);
            }

            currentPrintPage++;
            e.HasMorePages = currentPrintPage < totalPrintPages;
        }
        catch (Exception ex)
        {
            Console.WriteLine("PrintPage error: " + ex.Message);
            e.HasMorePages = false;
        }
    }

    private void AddFooterToPrintPage(PrintPageEventArgs e)
    {
        try
        {
            // Footer position (40px from bottom)
            float footerY = e.MarginBounds.Bottom - 40;

            // Draw separator line
            using (Pen separatorPen = new Pen(Color.LightGray, 1))
            {
                e.Graphics.DrawLine(separatorPen,
                                  e.MarginBounds.Left, footerY,
                                  e.MarginBounds.Right, footerY);
            }

            // Draw footer text
            using (Font footerFont = new Font("Arial", 8.5f))
            {
                string footerText = !string.IsNullOrWhiteSpace(currentInvoiceFooter) ? currentInvoiceFooter : "Phone: +92 51 6144660 | Mobile: +92 300 230 2463, +92 332 5494660 | Email: usmanenterprises63@gmail.com";

                // Center the footer text
                SizeF textSize = e.Graphics.MeasureString(footerText, footerFont);
                double footerX = (e.MarginBounds.Width - textSize.Width) / 2;
                double textY = footerY + 5;

                // Ensure text is fully visible
                if (textY + textSize.Height > e.MarginBounds.Bottom)
                    textY = e.MarginBounds.Bottom - textSize.Height - 5;

                e.Graphics.DrawString(footerText, footerFont, Brushes.Gray, (float)footerX, (float)textY);
            }
        }
        catch
        {
            // If Arial fails, try with default font
            try
            {
                float footerY = e.MarginBounds.Bottom - 40;
                using (Font footerFont = new Font(FontFamily.GenericSansSerif, 8.5f))
                {
                    string footerText = !string.IsNullOrWhiteSpace(currentInvoiceFooter) ? currentInvoiceFooter : "Phone: +92 51 6144660 | Mobile: +92 300 230 2463, +92 332 5494660 | Email: usmanenterprises63@gmail.com";
                    e.Graphics.DrawString(footerText, footerFont, Brushes.Gray, e.MarginBounds.Left, footerY + 5);
                }
            }
            catch { }
        }
    }

    private async void BtnPdf_Click(object sender, EventArgs e)
    {
        if (isGeneratingPdf)
            return;

        try
        {
            using (SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "PDF Files|*.pdf",
                FileName = $"Invoice_{invoiceId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                DefaultExt = ".pdf"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    isGeneratingPdf = true;
                    btnPdf.Enabled = false;
                    btnPdf.Text = "Generating PDF...";

                    string filePath = sfd.FileName;
                    await Task.Run(() => GenerateVectorPdf(filePath));

                    ShowInfoMessage("PDF saved successfully!", "Success");
                }
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"PDF generation failed: {ex.Message}");
        }
        finally
        {
            isGeneratingPdf = false;
            btnPdf.Enabled = true;
            btnPdf.Text = "📄 Download PDF";
        }
    }

    private void GenerateVectorPdf(string filePath)
    {
        try
        {
            PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to set system fonts resolver: " + ex.Message);
        }

        PdfDocument pdfDoc = null;
        try
        {
            pdfDoc = new PdfDocument();
            pdfDoc.Info.Title = $"Invoice {invoiceId}";
            pdfDoc.Info.Author = "HCR E-Invoicing System";
            pdfDoc.Info.CreationDate = DateTime.Now;

            // Load data
            DataSet ds = DatabaseHelper.GetInvoicePreviewData(invoiceId);
            if (ds == null || ds.Tables.Count == 0 || ds.Tables["InvoiceHeader"].Rows.Count == 0)
            {
                throw new Exception("Invoice not found in database.");
            }

            DataRow header = ds.Tables["InvoiceHeader"].Rows[0];
            DataTable items = ds.Tables["InvoiceItems"];

            // Colors
            XColor navyColor = XColor.FromArgb(30, 60, 114);
            XColor grayColor = XColor.FromArgb(74, 85, 104);
            XColor lightBgColor = XColor.FromArgb(248, 250, 252);
            XColor lightBorderColor = XColor.FromArgb(226, 232, 240);
            XColor darkTextColor = XColor.FromArgb(45, 55, 72);
            XColor lineSeparatorColor = XColor.FromArgb(203, 213, 225);

            XBrush navyBrush = new XSolidBrush(navyColor);
            XBrush grayBrush = new XSolidBrush(grayColor);
            XBrush darkTextBrush = new XSolidBrush(darkTextColor);
            XBrush whiteBrush = XBrushes.White;
            XBrush lightBgBrush = new XSolidBrush(lightBgColor);

            XPen borderPen = new XPen(lightBorderColor, 1);
            XPen linePen = new XPen(lineSeparatorColor, 0.75);

            // Fonts
            XFont titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
            XFont headingFont = new XFont("Arial", 10, XFontStyleEx.Bold);
            XFont boldFont = new XFont("Arial", 8.5, XFontStyleEx.Bold);
            XFont regularFont = new XFont("Arial", 8.5, XFontStyleEx.Regular);
            XFont smallFont = new XFont("Arial", 7.5, XFontStyleEx.Regular);
            XFont italicFont = new XFont("Arial", 8, XFontStyleEx.Italic);
            XFont tableHeaderFont = new XFont("Arial", 8, XFontStyleEx.Bold);
            XFont tableBodyFont = new XFont("Arial", 8, XFontStyleEx.Regular);

            // Layout settings
            double pageWidth = 595.0;  // A4 Width
            double pageHeight = 842.0; // A4 Height
            double margin = 36.0;      // 0.5 inch margins
            double printableWidth = pageWidth - 2 * margin; // 523pt
            double contentLimitY = 745.0; // Page break threshold (leave room for totals and footer)

            // Read footer text directly from DB header (safe for background thread)
            string pdfFooterText = header.Table.Columns.Contains("invoiceFooter") && header["invoiceFooter"] != DBNull.Value
                ? header["invoiceFooter"].ToString().Replace("\r\n", " | ").Replace("\n", " | ").Trim()
                : (!string.IsNullOrWhiteSpace(currentInvoiceFooter) ? currentInvoiceFooter : "Phone: +92 51 6144660 | Mobile: +92 300 230 2463 | Email: usmanenterprises63@gmail.com");
            while (pdfFooterText.Contains(" |  | ")) pdfFooterText = pdfFooterText.Replace(" |  | ", " | ");

            int pageNum = 0;
            double currentY = 0;
            XGraphics gfx = null;

            // Header drawing helper
            Action drawHeader = () =>
            {
                // Compact header for page 2+
                if (pageNum > 1)
                {
                    gfx.DrawString("SALES TAX INVOICE", headingFont, navyBrush, margin, 40);
                    gfx.DrawString($"Invoice #: {header["invoiceNumber"]}", regularFont, darkTextBrush, margin, 55);
                    gfx.DrawString($"FBR Invoice #: {header["fbrInvoiceNumber"]}", regularFont, darkTextBrush, margin + 150, 55);
                    string invDateStr = header["invoiceDate"] != DBNull.Value ? Convert.ToDateTime(header["invoiceDate"]).ToString("yyyy-MM-dd") : "";
                    gfx.DrawString($"Date: {invDateStr}", regularFont, darkTextBrush, margin + 350, 55);
                    gfx.DrawLine(new XPen(navyColor, 1), margin, 68, margin + printableWidth, 68);
                    currentY = 80;
                    return;
                }

                // First page header (full styling)
                // Draw Seller Logo on the left
                double logoY = 40;
                double logoHeight = 45;
                bool hasLogo = false;
                try
                {
                    if (header.Table.Columns.Contains("sellerLogoPath") && header["sellerLogoPath"] != DBNull.Value)
                    {
                        byte[] logoBytes = (byte[])header["sellerLogoPath"];
                        using (MemoryStream ms = new MemoryStream(logoBytes))
                        {
                            using (Image img = Image.FromStream(ms))
                            {
                                using (MemoryStream imgMs = new MemoryStream())
                                {
                                    img.Save(imgMs, ImageFormat.Png);
                                    imgMs.Position = 0;
                                    XImage xLogo = XImage.FromStream(imgMs);
                                    
                                    // Scale aspect ratio
                                    double aspect = (double)img.Width / img.Height;
                                    double drawWidth = logoHeight * aspect;
                                    if (drawWidth > 180) drawWidth = 180;
                                    gfx.DrawImage(xLogo, margin, logoY, drawWidth, logoHeight);
                                    hasLogo = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error rendering seller logo in PDF: " + ex.Message);
                }

                if (!hasLogo)
                {
                    // Fallback to seller name
                    string sName = header["sellerBusinessName"]?.ToString() ?? "HCR BUSINESS";
                    gfx.DrawString(sName, new XFont("Arial", 14, XFontStyleEx.Bold), navyBrush, margin, logoY + 15);
                }

                // Title
                gfx.DrawString("SALES TAX INVOICE", titleFont, navyBrush, margin + 180, 55);

                // Right side: FBR logo & QR code
                double rightX = margin + printableWidth;
                
                // Draw FBR verification QR code
                string fbrInvoiceNo = header["fbrInvoiceNumber"]?.ToString();
                bool hasQr = false;
                if (!string.IsNullOrEmpty(fbrInvoiceNo))
                {
                    try
                    {
                        string qrData = $"https://www.fbr.gov.pk/verifyInvoice?invoice={fbrInvoiceNo}";
                        using (QRCodeGenerator qrGen = new QRCodeGenerator())
                        {
                            QRCodeData qrDataObj = qrGen.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                            using (QRCode qrCode = new QRCode(qrDataObj))
                            {
                                using (Bitmap qrBmp = qrCode.GetGraphic(8))
                                {
                                    using (MemoryStream qrMs = new MemoryStream())
                                    {
                                        qrBmp.Save(qrMs, ImageFormat.Png);
                                        qrMs.Position = 0;
                                        XImage qrImg = XImage.FromStream(qrMs);
                                        gfx.DrawImage(qrImg, rightX - 50, 40, 50, 50);
                                        hasQr = true;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error rendering QR code in PDF: " + ex.Message);
                    }
                }

                // Draw FBR Digital Logo
                try
                {
                    string fbrPath = Path.Combine(Application.StartupPath, "FBR_DIGITAL.PNG");
                    if (File.Exists(fbrPath))
                    {
                        using (Image img = Image.FromFile(fbrPath))
                        {
                            using (MemoryStream fbrMs = new MemoryStream())
                            {
                                img.Save(fbrMs, ImageFormat.Png);
                                fbrMs.Position = 0;
                                XImage fbrImg = XImage.FromStream(fbrMs);
                                double fbrX = hasQr ? rightX - 110 : rightX - 50;
                                gfx.DrawImage(fbrImg, fbrX, 40, 50, 50);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error rendering FBR logo in PDF: " + ex.Message);
                }

                // Under FBR/QR section notice
                gfx.DrawString("Computer generated invoice. No signature or stamp required.", italicFont, grayBrush, margin, 105);

                // Meta row: Date, Invoice number, FBR number, status
                currentY = 125;
                gfx.DrawLine(new XPen(navyColor, 1.5), margin, currentY, margin + printableWidth, currentY);
                currentY += 15;

                // Two columns for meta data
                gfx.DrawString($"Invoice Number: {header["invoiceNumber"]}", boldFont, darkTextBrush, margin, currentY);
                string dateStr = header["invoiceDate"] != DBNull.Value ? Convert.ToDateTime(header["invoiceDate"]).ToString("yyyy-MM-dd HH:mm") : "";
                gfx.DrawString($"Invoice Date: {dateStr}", regularFont, darkTextBrush, margin + 280, currentY);
                currentY += 14;

                gfx.DrawString($"FBR Invoice Number: {header["fbrInvoiceNumber"]}", regularFont, darkTextBrush, margin, currentY);
                
                // Status colorized
                string invoiceStatus = header["status"]?.ToString() ?? "Unpaid";
                bool isPaid = invoiceStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase);
                XBrush statusBrush = isPaid ? new XSolidBrush(XColor.FromArgb(72, 187, 120)) : new XSolidBrush(XColor.FromArgb(229, 62, 62));
                gfx.DrawString($"Payment Status: {invoiceStatus.ToUpper()}", boldFont, statusBrush, margin + 280, currentY);

                currentY += 20;

                // Seller and Buyer Boxes side-by-side
                double boxWidth = 250;
                double boxHeight = 100;
                double sellerX = margin;
                double buyerX = margin + printableWidth - boxWidth;
                double boxY = currentY;

                // Draw Seller Box
                gfx.DrawRectangle(borderPen, lightBgBrush, sellerX, boxY, boxWidth, boxHeight);
                gfx.DrawString("Seller Information", headingFont, navyBrush, sellerX + 10, boxY + 18);
                
                string sBiz = header["sellerBusinessName"]?.ToString() ?? "";
                string sNtn = $"NTN/CNIC: {header["sellerNTNCNIC"]}";
                string sProv = $"Province: {header["sellerProvince"]}";
                string sAddr = $"Address: {header["sellerAddress"]}";
                
                gfx.DrawString(sBiz, boldFont, darkTextBrush, sellerX + 10, boxY + 33);
                gfx.DrawString(sNtn, regularFont, darkTextBrush, sellerX + 10, boxY + 47);
                gfx.DrawString(sProv, regularFont, darkTextBrush, sellerX + 10, boxY + 61);
                
                // Wrap and draw seller address
                List<string> sAddrLines = WrapText(sAddr, boxWidth - 20, regularFont, gfx);
                double addrY = boxY + 75;
                for (int idx = 0; idx < Math.Min(sAddrLines.Count, 2); idx++)
                {
                    gfx.DrawString(sAddrLines[idx], regularFont, darkTextBrush, sellerX + 10, addrY);
                    addrY += 11;
                }

                // Draw Buyer Box
                gfx.DrawRectangle(borderPen, lightBgBrush, buyerX, boxY, boxWidth, boxHeight);
                gfx.DrawString("Buyer Information", headingFont, navyBrush, buyerX + 10, boxY + 18);

                string cBiz = header["customerBusinessName"]?.ToString() ?? "";
                string cNtn = $"NTN/CNIC: {header["customerNTNCNIC"]}";
                string cProv = $"Province: {header["customerProvince"]}";
                string cAddr = $"Address: {header["customerAddress"]}";

                gfx.DrawString(cBiz, boldFont, darkTextBrush, buyerX + 10, boxY + 33);
                gfx.DrawString(cNtn, regularFont, darkTextBrush, buyerX + 10, boxY + 47);
                gfx.DrawString(cProv, regularFont, darkTextBrush, buyerX + 10, boxY + 61);

                // Wrap and draw buyer address
                List<string> cAddrLines = WrapText(cAddr, boxWidth - 20, regularFont, gfx);
                addrY = boxY + 75;
                for (int idx = 0; idx < Math.Min(cAddrLines.Count, 2); idx++)
                {
                    gfx.DrawString(cAddrLines[idx], regularFont, darkTextBrush, buyerX + 10, addrY);
                    addrY += 11;
                }

                currentY += boxHeight + 20;
            };

            // Helper: draw footer on current page (called before closing gfx)
            Action drawPageFooter = () =>
            {
                if (gfx == null) return;
                double footerY = pageHeight - 60;
                // Separator line
                gfx.DrawLine(new XPen(XColor.FromArgb(226, 232, 240), 1), margin, footerY, margin + printableWidth, footerY);
                // Footer text (contact info from DB)
                XRect fRect = new XRect(margin, footerY + 5, printableWidth, 14);
                gfx.DrawString(pdfFooterText, smallFont, grayBrush, fRect, XStringFormats.Center);
                // Computer-generated notice
                XRect cgRect = new XRect(margin, footerY + 18, printableWidth, 12);
                gfx.DrawString("This is a computer generated invoice. No signature or stamp required.", smallFont, grayBrush, cgRect, XStringFormats.Center);
            };

            // Function to add a page
            Action addPage = () =>
            {
                // Draw footer on the current page before leaving it
                if (gfx != null) drawPageFooter();

                PdfPage page = pdfDoc.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                pageNum++;
                drawHeader();
            };

            // Add first page
            addPage();

            // Column Widths
            // HS Code, Description, Unit Price, Qty, Gross, Disc, Excl Tax, Tax %, Tax Val, Incl Tax
            double[] colWidths = { 40.0, 133.0, 42.0, 25.0, 45.0, 42.0, 52.0, 28.0, 52.0, 64.0 };
            double[] colX = new double[colWidths.Length];
            colX[0] = margin;
            for (int i = 1; i < colWidths.Length; i++)
            {
                colX[i] = colX[i - 1] + colWidths[i - 1];
            }

            string[] headers = { "Item Code", "Item Description", "Unit Price", "Qty", "Gross", "Discount", "Amt Excl.", "Tax%", "S.Tax Val", "Amt Incl." };

            // Helper to draw table header row
            Action drawTableHeaderRow = () =>
            {
                // Draw header background band
                gfx.DrawRectangle(navyBrush, margin, currentY, printableWidth, 22);
                
                for (int i = 0; i < colWidths.Length; i++)
                {
                    XStringFormat format = XStringFormats.CenterRight;
                    if (i == 0) format = XStringFormats.Center;
                    else if (i == 1) format = XStringFormats.CenterLeft;

                    double xPos = colX[i];
                    if (format == XStringFormats.Center) xPos += colWidths[i] / 2;
                    else if (format == XStringFormats.CenterRight) xPos += colWidths[i] - 4;
                    else if (format == XStringFormats.CenterLeft) xPos += 4;

                    gfx.DrawString(headers[i], tableHeaderFont, whiteBrush, xPos, currentY + 11, format);
                }
                currentY += 22;
            };

            // Draw first table header
            drawTableHeaderRow();

            // Draw invoice items
            int itemIndex = 0;
            foreach (DataRow row in items.Rows)
            {
                decimal qty = ParseDecimal(row["quantity"]);
                decimal unitPrice = ParseDecimal(row["unitPrice"]);
                decimal rate = items.Columns.Contains("rate") ? ParseDecimal(row["rate"]) : 0m;
                decimal discount = items.Columns.Contains("discount") ? ParseDecimal(row["discount"]) : 0m;

                string description = "";
                if (row["description"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["description"].ToString()))
                {
                    description = row["description"].ToString();
                }
                else if (row["productDescription"] != DBNull.Value)
                {
                    description = row["productDescription"].ToString();
                }

                decimal totalGross = qty * unitPrice;
                decimal totalEx = totalGross - discount;
                if (totalEx < 0m) totalEx = 0m;
                decimal taxValue = totalEx * rate / 100m;
                decimal totalInc = totalEx + taxValue;

                string hsCode = row["hsCode"]?.ToString() ?? "";

                // Wrap description text
                List<string> descLines = WrapText(description, colWidths[1] - 8, tableBodyFont, gfx);
                if (descLines.Count == 0) descLines.Add("");

                // Calculate row height
                double rowHeight = Math.Max(18, descLines.Count * 11) + 4;

                // Check for page overflow
                if (currentY + rowHeight > contentLimitY)
                {
                    // Draw border line at the bottom of the table on current page
                    gfx.DrawLine(borderPen, margin, currentY, margin + printableWidth, currentY);
                    
                    // Add new page
                    addPage();
                    drawTableHeaderRow();
                }

                // Alternate row colors
                XBrush rowBrush = (itemIndex % 2 == 0) ? whiteBrush : lightBgBrush;
                gfx.DrawRectangle(borderPen, rowBrush, margin, currentY, printableWidth, rowHeight);

                // Draw cell contents
                // 1. hsCode
                gfx.DrawString(hsCode, tableBodyFont, darkTextBrush, colX[0] + colWidths[0]/2, currentY + rowHeight/2, XStringFormats.Center);

                // 2. description (multiline)
                double descY = currentY + 11;
                for (int l = 0; l < descLines.Count; l++)
                {
                    gfx.DrawString(descLines[l], tableBodyFont, darkTextBrush, colX[1] + 4, descY, XStringFormats.CenterLeft);
                    descY += 11;
                }

                // Numbers: Unit Price, Qty, Gross, Discount, Amt Excl, Tax %, S.Tax Val, Amt Incl
                gfx.DrawString($"{unitPrice:N2}", tableBodyFont, darkTextBrush, colX[2] + colWidths[2] - 4, currentY + rowHeight/2, XStringFormats.CenterRight);
                gfx.DrawString($"{qty:N0}", tableBodyFont, darkTextBrush, colX[3] + colWidths[3] - 4, currentY + rowHeight/2, XStringFormats.CenterRight);
                gfx.DrawString($"{totalGross:N2}", tableBodyFont, darkTextBrush, colX[4] + colWidths[4] - 4, currentY + rowHeight/2, XStringFormats.CenterRight);
                gfx.DrawString($"{discount:N2}", tableBodyFont, darkTextBrush, colX[5] + colWidths[5] - 4, currentY + rowHeight/2, XStringFormats.CenterRight);
                gfx.DrawString($"{totalEx:N2}", tableBodyFont, darkTextBrush, colX[6] + colWidths[6] - 4, currentY + rowHeight/2, XStringFormats.CenterRight);
                gfx.DrawString($"{rate:0.##}%", tableBodyFont, darkTextBrush, colX[7] + colWidths[7] - 4, currentY + rowHeight/2, XStringFormats.CenterRight);
                gfx.DrawString($"{taxValue:N2}", tableBodyFont, darkTextBrush, colX[8] + colWidths[8] - 4, currentY + rowHeight/2, XStringFormats.CenterRight);
                gfx.DrawString($"{totalInc:N2}", tableBodyFont, darkTextBrush, colX[9] + colWidths[9] - 4, currentY + rowHeight/2, XStringFormats.CenterRight);

                currentY += rowHeight;
                itemIndex++;
            }

            // Draw table bottom boundary line
            gfx.DrawLine(new XPen(navyColor, 1), margin, currentY, margin + printableWidth, currentY);

            // Calculations for totals
            decimal subTotal = 0, totalTax = 0, furtherTax = 0, grandTotal = 0;
            foreach (DataRow row in items.Rows)
            {
                decimal qty = ParseDecimal(row["quantity"]);
                decimal unitPrice = ParseDecimal(row["unitPrice"]);
                decimal rate = items.Columns.Contains("rate") ? ParseDecimal(row["rate"]) : 0m;
                decimal discount = items.Columns.Contains("discount") ? ParseDecimal(row["discount"]) : 0m;
                decimal further = items.Columns.Contains("furtherTax") ? ParseDecimal(row["furtherTax"]) : 0m;

                decimal totalGross = qty * unitPrice;
                decimal totalEx = totalGross - discount;
                if (totalEx < 0m) totalEx = 0m;
                decimal taxValue = totalEx * rate / 100m;
                decimal totalInc = totalEx + taxValue;

                subTotal += totalEx;
                totalTax += taxValue;
                furtherTax += further;
                grandTotal += totalInc;
            }
            decimal invoiceTotalAmount = grandTotal + furtherTax;

            string invoiceStatusStr = header["status"]?.ToString() ?? "Unpaid";
            bool isInvoicePaid = invoiceStatusStr.Equals("Paid", StringComparison.OrdinalIgnoreCase);
            decimal invoicePaidAmt = header.Table.Columns.Contains("paidAmount") && header["paidAmount"] != DBNull.Value ? ParseDecimal(header["paidAmount"]) : 0m;
            if (isInvoicePaid) invoicePaidAmt = invoiceTotalAmount;
            decimal invoiceDueAmt = Math.Max(0m, invoiceTotalAmount - invoicePaidAmt);

            // Draw Totals Block
            double totalsHeight = 85;
            if (invoiceDueAmt > 0) totalsHeight = 100; // room for due amount

            // Page break check for totals
            if (currentY + totalsHeight > contentLimitY)
            {
                addPage();
            }

            currentY += 15;
            double totalsWidth = 230;
            double totalsX = margin + printableWidth - totalsWidth;

            // Draw a subtle border for totals box
            gfx.DrawRectangle(borderPen, lightBgBrush, totalsX, currentY, totalsWidth, totalsHeight);

            double rowY = currentY + 15;
            Action<string, string, bool, XBrush> drawTotalRow = (label, val, isBold, valBrush) =>
            {
                XFont lblFont = isBold ? boldFont : regularFont;
                XFont vFont = isBold ? boldFont : regularFont;
                XBrush brushVal = valBrush ?? darkTextBrush;

                gfx.DrawString(label, lblFont, darkTextBrush, totalsX + 15, rowY);
                gfx.DrawString(val, vFont, brushVal, totalsX + totalsWidth - 15, rowY, XStringFormats.CenterRight);
                rowY += 16;
            };

            drawTotalRow("Subtotal:", $"{subTotal:N2}", false, null);
            drawTotalRow("Sales Tax:", $"{totalTax:N2}", false, null);
            if (furtherTax > 0)
            {
                drawTotalRow("Further Tax:", $"{furtherTax:N2}", false, null);
            }
            drawTotalRow("Grand Total:", $"{invoiceTotalAmount:N2}", true, navyBrush);

            if (invoiceDueAmt > 0)
            {
                drawTotalRow("Due Amount:", $"{invoiceDueAmt:N2}", true, new XSolidBrush(XColor.FromArgb(229, 62, 62)));
            }

            // Draw dynamic notes if present
            string notes = header["notes"]?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(notes))
            {
                double notesY = currentY + 15;
                if (notesY + 40 < pageHeight - 100)
                {
                    gfx.DrawString("Notes:", boldFont, navyBrush, margin, notesY);
                    List<string> noteLines = WrapText(notes, 250, regularFont, gfx);
                    double nLineY = notesY + 15;
                    for (int idx = 0; idx < Math.Min(noteLines.Count, 3); idx++)
                    {
                        gfx.DrawString(noteLines[idx], regularFont, darkTextBrush, margin, nLineY);
                        nLineY += 12;
                    }
                }
            }

            // Draw footer on the LAST page (addPage draws it on all pages except the last)
            drawPageFooter();

            // SECOND PASS: Add page numbers only (footer already drawn inline above)
            int totalPages = pdfDoc.Pages.Count;
            for (int i = 0; i < totalPages; i++)
            {
                PdfPage page = pdfDoc.Pages[i];
                XGraphics pgGfx = XGraphics.FromPdfPage(page);
                double footerY = pageHeight - 60;
                string pageInfo = $"Page {i + 1} of {totalPages}";
                XRect pageRect = new XRect(margin, footerY + 30, printableWidth, 10);
                pgGfx.DrawString(pageInfo, smallFont, grayBrush, pageRect, XStringFormats.Center);
                pgGfx.Dispose();
            }

            // Save PDF
            pdfDoc.Save(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("PDF Generation failed: " + ex.Message);
            throw;
        }
        finally
        {
            pdfDoc?.Dispose();
        }
    }

    private List<string> WrapText(string text, double maxWidth, XFont font, XGraphics gfx)
    {
        List<string> lines = new List<string>();
        if (string.IsNullOrEmpty(text))
            return lines;

        string[] paragraphs = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        foreach (var paragraph in paragraphs)
        {
            string[] words = paragraph.Split(' ');
            string currentLine = "";

            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                XSize size = gfx.MeasureString(testLine, font);
                if (size.Width > maxWidth)
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        lines.Add(word);
                        currentLine = "";
                    }
                }
                else
                {
                    currentLine = testLine;
                }
            }
            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }
        }
        return lines;
    }

    // Trim bottom whitespace (near-white) from bitmap
    private Bitmap TrimBottomWhitespace(Bitmap src, int tolerance = 10)
    {
        if (src == null) return null;
        try
        {
            int w = src.Width;
            int h = src.Height;
            int lastNonWhite = -1;

            for (int y = h - 1; y >= 0; y--)
            {
                bool rowHasContent = false;
                for (int x = 0; x < w; x++)
                {
                    Color c = src.GetPixel(x, y);
                    // consider near-white as whitespace
                    if (c.R < 255 - tolerance || c.G < 255 - tolerance || c.B < 255 - tolerance)
                    {
                        rowHasContent = true;
                        break;
                    }
                }
                if (rowHasContent)
                {
                    lastNonWhite = y;
                    break;
                }
            }

            if (lastNonWhite < 0 || lastNonWhite >= h - 1)
            {
                // nothing to trim
                return src;
            }

            int newH = lastNonWhite + 1;
            Bitmap dst = new Bitmap(w, newH);
            dst.SetResolution(src.HorizontalResolution, src.VerticalResolution);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.Clear(Color.White);
                g.DrawImage(src, new Rectangle(0, 0, w, newH), new Rectangle(0, 0, w, newH), GraphicsUnit.Pixel);
            }

            src.Dispose();
            return dst;
        }
        catch
        {
            return src;
        }
    }

    private Bitmap CaptureInvoiceBitmap()
    {
        try
        {
            // Ensure layout updated
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => { AdjustInvoiceHeight(); this.Refresh(); Application.DoEvents(); }));
            }
            else
            {
                AdjustInvoiceHeight();
                this.Refresh();
                Application.DoEvents();
            }

            // Temporarily hide bottom spacer and footer to avoid extra blank area
            Control bottomSpacer = mainPanel.Controls["bottomSpacerPanel"] as Control;
            bool prevBottomSpacerVisible = bottomSpacer?.Visible ?? false;
            bool prevFooterVisible = footerPanel?.Visible ?? false;
            if (bottomSpacer != null) bottomSpacer.Visible = false;
            if (footerPanel != null) footerPanel.Visible = false;

            try
            {
                // Calculate total content height using control bounds so we don't accidentally sum wrong order
                int totalHeight = 0;
                int totalWidth = pnlInvoice.Width; // use full panel width to avoid horizontal cropping

                foreach (Control ctrl in pnlInvoice.Controls)
                {
                    if (ctrl.Visible && ctrl != footerPanel && ctrl.Name != "bottomSpacerPanel")
                    {
                        totalHeight = Math.Max(totalHeight, ctrl.Bottom + ctrl.Margin.Bottom);
                    }
                }

                // Include some minimum and padding
                totalHeight = Math.Max(totalHeight + pnlInvoice.Padding.Bottom, 800);

                // Fallback width: sometimes pnlInvoice.Width can be 0 if layout not finished
                int bmpWidth = totalWidth;
                if (bmpWidth <= 1)
                {
                    bmpWidth = Math.Max(pnlInvoice.ClientSize.Width, 800);
                    Console.WriteLine($"Warning: pnlInvoice.Width was small, using fallback width {bmpWidth}");
                }

                // Create bitmap with white background; use higher DPI for better print quality
                Bitmap bmp = new Bitmap(bmpWidth, totalHeight);
                bmp.SetResolution(300, 300);

                try
                {
                    // Try drawing the whole pnlInvoice at once (this will include the vertical label)
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    }

                    // Attempt DrawToBitmap on pnlInvoice
                    try
                    {
                        pnlInvoice.SuspendLayout();
                        // Draw the whole panel using its full bounds
                        pnlInvoice.DrawToBitmap(bmp, new Rectangle(0, 0, bmpWidth, totalHeight));
                        pnlInvoice.ResumeLayout();
                        // Trim trailing whitespace before returning
                        Bitmap trimmed = TrimBottomWhitespace(bmp, 10);
                        return trimmed;
                    }
                    catch (Exception exDraw)
                    {
                        Console.WriteLine("DrawToBitmap on pnlInvoice failed, falling back to per-control render: " + exDraw.Message);
                    }

                    // Fallback: render each control individually from pnlInvoice so positions are preserved
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                        foreach (Control ctrl in pnlInvoice.Controls)
                        {
                            if (ctrl.Visible && ctrl != footerPanel && ctrl.Name != "bottomSpacerPanel")
                            {
                                try
                                {
                                    int width = Math.Max(ctrl.Width, 1);
                                    int height = Math.Max(ctrl.Height, 1);

                                    using (Bitmap ctrlBmp = new Bitmap(width, height))
                                    {
                                        ctrlBmp.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);
                                        try
                                        {
                                            ctrl.DrawToBitmap(ctrlBmp, new Rectangle(0, 0, width, height));
                                        }
                                        catch (Exception dEx)
                                        {
                                            Console.WriteLine($"DrawToBitmap failed for control {ctrl.Name}: {dEx.Message}");
                                            using (Graphics gg = Graphics.FromImage(ctrlBmp))
                                            {
                                                gg.Clear(Color.White);
                                            }
                                        }

                                        // Draw control bitmap at its actual location inside pnlInvoice
                                        g.DrawImage(ctrlBmp, ctrl.Left, ctrl.Top, width, height);
                                    }
                                }
                                catch (Exception ctrlEx)
                                {
                                    Console.WriteLine($"Error capturing control {ctrl.Name}: {ctrlEx.Message}");
                                }
                            }
                        }
                    }

                    Bitmap trimmed2 = TrimBottomWhitespace(bmp, 10);
                    return trimmed2;
                }
                catch
                {
                    bmp.Dispose();
                    throw;
                }
                finally
                {
                    // restore visibility
                    if (bottomSpacer != null) bottomSpacer.Visible = prevBottomSpacerVisible;
                    if (footerPanel != null) footerPanel.Visible = prevFooterVisible;
                }
            }
            finally
            {
                // Ensure restore even on error
                if (bottomSpacer != null) bottomSpacer.Visible = prevBottomSpacerVisible;
                if (footerPanel != null) footerPanel.Visible = prevFooterVisible;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bitmap capture error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    // Helper methods for thread-safe UI updates
    private void ShowErrorMessage(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
        }
        else
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowInfoMessage(string message, string title)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information)));
        }
        else
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void PrintDocument_EndPrint(object sender, PrintEventArgs e)
    {
        try
        {
            if (pageBitmaps != null)
            {
                foreach (var bmp in pageBitmaps)
                {
                    bmp?.Dispose();
                }
                pageBitmaps = null;
            }

            currentPrintPage = 0;
            totalPrintPages = 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("EndPrint cleanup error: " + ex.Message);
        }
    }

    // Find the nearest horizontal separator line (e.g., table row boundary) in the image.
    // Searches around a given Y position within a specified range, returning the Y coordinate of the separator if found.
    private int FindHorizontalSeparator(Bitmap bmp, int searchTop, int searchBottom)
    {
        try
        {
            // Clamp search bounds
            searchTop = Math.Max(0, searchTop);
            searchBottom = Math.Min(bmp.Height - 1, searchBottom);

            // Scan rows within the search range
            for (int y = searchTop; y <= searchBottom; y++)
            {
                bool isSeparator = true;
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixelColor = bmp.GetPixel(x, y);
                    // Check if the pixel is not near white (i.e., part of the separator)
                    if (pixelColor.R > 200 && pixelColor.G > 200 && pixelColor.B > 200)
                    {
                        isSeparator = false;
                        break;
                    }
                }
                // If a non-white pixel was found in this row, consider it a separator
                if (!isSeparator)
                {
                    return y;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error finding horizontal separator: " + ex.Message);
        }
        return -1;
    }
}