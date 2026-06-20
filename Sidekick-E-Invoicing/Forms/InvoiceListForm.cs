using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sidekick_E_Invoicing;
using Sidekick_E_Invoicing.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static QRCoder.PayloadGenerator;
public class InvoiceViewerForm : Form
{
    private DataGridView dgvInvoices;
    private Button btnPreview, btnDelete, btnSearch;
    private TextBox txtSearch;
    private DateTimePicker dtpFrom, dtpTo;
    private Label lblFrom, lblTo;
    private TableLayoutPanel mainLayout, filterPanel;
    private int currentInvoiceId = -1;

    // Quick invoice no search controls
    private Panel invoiceNoPanel;
    private Label lblInvoiceNo;
    private TextBox txtInvoiceNo;

    public InvoiceViewerForm()
    {
        InitializeComponent();
        InitializeGrid();
        LoadInvoices();
        this.Load += (s, e) => FormTransitionHelper.AnimateFadeIn(this);
    }

    public InvoiceViewerForm(string preFilterInvoiceNo) : this()
    {
        // Pre-populate the invoice no search box if provided
        if (!string.IsNullOrWhiteSpace(preFilterInvoiceNo) && txtInvoiceNo != null)
        {
            txtInvoiceNo.Text = preFilterInvoiceNo;
            LoadInvoices(dtpFrom.Value, dtpTo.Value, "", preFilterInvoiceNo);
        }
    }

    private void InitializeComponent()
    {
        // ===== FORM PROPERTIES =====
        this.Text = "Invoice Viewer - Sidekick";
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.White;
        this.MinimumSize = new Size(800, 600);
        this.Padding = new Padding(10);

        // ===== MAIN LAYOUT =====
        mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.White
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Invoice No quick search
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Filter + Buttons
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Grid

        // ===== HEADER =====
        var header = new Label
        {
            Text = "🧾 Invoice Viewer",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorTranslator.FromHtml("#1b6656"),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0)
        };
        mainLayout.Controls.Add(header, 0, 0);

        // ===== INVOICE NO QUICK SEARCH PANEL =====
        invoiceNoPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(10, 8, 10, 8)
        };

        lblInvoiceNo = new Label
        {
            Text = "Invoice No:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(6, 12)
        };

        txtInvoiceNo = new TextBox
        {
            Width = 220,
            Location = new Point(100, 8)
        };
        // Allow Enter to trigger invoice number search (uses single Search button semantics)
        txtInvoiceNo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadInvoices(dtpFrom.Value, dtpTo.Value, txtSearch.Text.Trim(), txtInvoiceNo.Text.Trim()); e.Handled = true; } };

        invoiceNoPanel.Controls.Add(lblInvoiceNo);
        invoiceNoPanel.Controls.Add(txtInvoiceNo);

        mainLayout.Controls.Add(invoiceNoPanel, 0, 1);

        // ===== FILTER PANEL =====
        CreateFilterPanel();
        mainLayout.Controls.Add(filterPanel, 0, 2);

        // ===== DATAGRIDVIEW =====
        CreateDataGridView();
        mainLayout.Controls.Add(dgvInvoices, 0, 3);

        this.Controls.Add(mainLayout);

        // ===== RESPONSIVE HANDLING =====
        this.SizeChanged += InvoiceViewerForm_SizeChanged;
    }

    private void CreateFilterPanel()
    {
        filterPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 1,
            AutoSize = true,
            Padding = new Padding(10, 15, 10, 15),
            BackColor = Color.White,
            Margin = new Padding(0)
        };

        // Responsive column styles
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // From label
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // From date
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // To label
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // To date
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Search label
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); // Search textbox
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Search button
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Preview button
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Delete button
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Fill space

        // From Label
        lblFrom = new Label
        {
            Text = "From:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(0, 0, 5, 0)
        };

        // From Date Picker
        dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            Margin = new Padding(0, 0, 20, 0)
        };

        // To Label
        lblTo = new Label
        {
            Text = "To:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(0, 0, 5, 0)
        };

        // To Date Picker
        dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now,
            Margin = new Padding(0, 0, 20, 0)
        };

        // Buttons
        btnSearch = MakeButton("🔍 Search", "#1b6656");
        btnPreview = MakeButton("👁 Preview", "#C8A84B");
        btnDelete = MakeButton("🗑 Delete", "#8B1A1A");

        // Search textbox and label
        var lblSearch = new Label
        {
            Text = "Search:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(10, 0, 5, 0)
        };

        txtSearch = new TextBox
        {
            Width = 180,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 10, 0)
        };

        // Allow Enter key to trigger search
        txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadInvoices(dtpFrom.Value, dtpTo.Value, txtSearch.Text.Trim(), txtInvoiceNo?.Text?.Trim()); e.Handled = true; } };

        btnSearch.Click += (s, e) => LoadInvoices(dtpFrom.Value, dtpTo.Value, txtSearch.Text.Trim(), txtInvoiceNo?.Text?.Trim());
        btnPreview.Click += BtnPreview_Click;
        btnDelete.Click += BtnDelete_Click;

        // Add controls to filter panel
        filterPanel.Controls.Add(lblFrom, 0, 0);
        filterPanel.Controls.Add(dtpFrom, 1, 0);
        filterPanel.Controls.Add(lblTo, 2, 0);
        filterPanel.Controls.Add(dtpTo, 3, 0);
        filterPanel.Controls.Add(lblSearch, 4, 0);
        filterPanel.Controls.Add(txtSearch, 5, 0);
        filterPanel.Controls.Add(btnSearch, 6, 0);
        filterPanel.Controls.Add(btnPreview, 7, 0);
        filterPanel.Controls.Add(btnDelete, 8, 0);
    }

    private void CreateDataGridView()
    {
        dgvInvoices = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            RowTemplate = { Height = 45 },
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            ColumnHeadersHeight = 50,
            RowHeadersVisible = false,
            AllowUserToResizeRows = true,
            AllowUserToResizeColumns = true
        };

        // ===== STYLE DATAGRIDVIEW =====
        dgvInvoices.EnableHeadersVisualStyles = false;
        dgvInvoices.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#1b6656");
        dgvInvoices.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvInvoices.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgvInvoices.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvInvoices.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

        dgvInvoices.DefaultCellStyle.Font = new Font("Segoe UI", 9);
        dgvInvoices.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 60);
        dgvInvoices.DefaultCellStyle.BackColor = Color.White;
        dgvInvoices.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 250);
        dgvInvoices.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#C8A84B");
        dgvInvoices.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvInvoices.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        dgvInvoices.DefaultCellStyle.Padding = new Padding(5);
    }

    private void InvoiceViewerForm_SizeChanged(object sender, EventArgs e)
    {
        AdjustLayoutForScreenSize();
    }

    private void AdjustLayoutForScreenSize()
    {
        int screenWidth = this.ClientSize.Width;

        if (screenWidth < 900)
        {
            // Mobile layout
            SetMobileLayout();
        }
        else if (screenWidth < 1200)
        {
            // Tablet layout
            SetTabletLayout();
        }
        else
        {
            // Desktop layout
            SetDesktopLayout();
        }

        AdjustFontSizes(screenWidth);
        filterPanel.PerformLayout();
    }

    private void SetMobileLayout()
    {
        filterPanel.ColumnCount = 2;
        filterPanel.RowCount = 4;
        filterPanel.ColumnStyles.Clear();
        filterPanel.RowStyles.Clear();

        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        for (int i = 0; i < 4; i++)
        {
            filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        RearrangeControlsForMobile();

        // Adjust DataGrid for mobile
        dgvInvoices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        SetColumnWidthsForMobile();
    }

    private void SetTabletLayout()
    {
        filterPanel.ColumnCount = 4;
        filterPanel.RowCount = 2;
        filterPanel.ColumnStyles.Clear();
        filterPanel.RowStyles.Clear();

        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        RearrangeControlsForTablet();

        // Adjust DataGrid for tablet
        dgvInvoices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    private void SetDesktopLayout()
    {
        filterPanel.ColumnCount = 8;
        filterPanel.RowCount = 1;
        filterPanel.ColumnStyles.Clear();
        filterPanel.RowStyles.Clear();

        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        RearrangeControlsForDesktop();

        // Adjust DataGrid for desktop
        dgvInvoices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    private void RearrangeControlsForMobile()
    {
        filterPanel.Controls.Clear();

        filterPanel.Controls.Add(lblFrom, 0, 0);
        filterPanel.Controls.Add(dtpFrom, 1, 0);
        filterPanel.Controls.Add(lblTo, 0, 1);
        filterPanel.Controls.Add(dtpTo, 1, 1);
        filterPanel.Controls.Add(btnSearch, 0, 2);
        filterPanel.Controls.Add(btnPreview, 1, 2);
        filterPanel.SetColumnSpan(btnDelete, 2);
        filterPanel.Controls.Add(btnDelete, 0, 3);

        foreach (Control control in filterPanel.Controls)
        {
            if (control is Button button)
            {
                button.Height = 35;
                button.Margin = new Padding(2);
            }
        }
    }

    private void RearrangeControlsForTablet()
    {
        filterPanel.Controls.Clear();

        filterPanel.Controls.Add(lblFrom, 0, 0);
        filterPanel.Controls.Add(dtpFrom, 1, 0);
        filterPanel.Controls.Add(lblTo, 2, 0);
        filterPanel.Controls.Add(dtpTo, 3, 0);
        filterPanel.Controls.Add(btnSearch, 0, 1);
        filterPanel.Controls.Add(btnPreview, 1, 1);
        filterPanel.Controls.Add(btnDelete, 2, 1);
    }

    private void RearrangeControlsForDesktop()
    {
        filterPanel.Controls.Clear();

        filterPanel.Controls.Add(lblFrom, 0, 0);
        filterPanel.Controls.Add(dtpFrom, 1, 0);
        filterPanel.Controls.Add(lblTo, 2, 0);
        filterPanel.Controls.Add(dtpTo, 3, 0);
        filterPanel.Controls.Add(btnSearch, 4, 0);
        filterPanel.Controls.Add(btnPreview, 5, 0);
        filterPanel.Controls.Add(btnDelete, 6, 0);
    }

    private void SetColumnWidthsForMobile()
    {
        if (dgvInvoices.Columns.Count > 0)
        {
            dgvInvoices.Columns["invoiceId"].Width = 40;
            dgvInvoices.Columns["invoiceNumber"].Width = 120;
            dgvInvoices.Columns["fbrInvoiceNumber"].Width = 130;
            dgvInvoices.Columns["invoiceDate"].Width = 80;
            dgvInvoices.Columns["subTotal"].Width = 80;
            dgvInvoices.Columns["totalTax"].Width = 80;
            dgvInvoices.Columns["grandTotal"].Width = 90;
            dgvInvoices.Columns["status"].Width = 70;
            dgvInvoices.Columns["postStatus"].Width = 120;
        }
    }
    public static string GetSellerToken(string sellerNTN)
    {
        using (var con = new SQLiteConnection(DatabaseHelper.ConnectionString))
        {
            con.Open();
            using (var cmd = new SQLiteCommand("SELECT token FROM Sellers WHERE sellerNTNCNIC = @ntn", con))
            {
                cmd.Parameters.AddWithValue("@ntn", sellerNTN);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? string.Empty;
            }
        }
    }

    private void AdjustFontSizes(int screenWidth)
    {
        if (screenWidth < 768)
        {
            dgvInvoices.DefaultCellStyle.Font = new Font("Segoe UI", 8);
            dgvInvoices.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblFrom.Font = lblTo.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        }
        else if (screenWidth < 1200)
        {
            dgvInvoices.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgvInvoices.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblFrom.Font = lblTo.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        }
        else
        {
            dgvInvoices.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgvInvoices.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblFrom.Font = lblTo.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        }
    }

    // ===== BUTTON MAKER =====
    private Button MakeButton(string text, string color)
    {
        return new Button
        {
            Text = text,
            Height = 40,
            BackColor = ColorTranslator.FromHtml(color),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(5),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
    }

    // ===== GRID COLUMNS =====
    private void InitializeGrid()
    {
        dgvInvoices.Columns.Clear();

        // Add regular columns
        var columns = new[]
        {
            new { Name = "invoiceId", Header = "ID", Width = 60, MinWidth = 40 },
            new { Name = "invoiceNumber", Header = "Invoice Number", Width = 180, MinWidth = 120 },
            new { Name = "fbrInvoiceNumber", Header = "FBR Invoice No.", Width = 200, MinWidth = 150 },
            new { Name = "invoiceDate", Header = "Date", Width = 100, MinWidth = 80 },
            new { Name = "subTotal", Header = "Sub Total", Width = 120, MinWidth = 90 },
            new { Name = "totalTax", Header = "Total Tax", Width = 120, MinWidth = 90 },
            new { Name = "grandTotal", Header = "Grand Total", Width = 120, MinWidth = 90 },
            new { Name = "status", Header = "Status", Width = 100, MinWidth = 70 }
        };

        foreach (var col in columns)
        {
            dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = col.Name,
                HeaderText = col.Header,
                Width = col.Width,
                MinimumWidth = col.MinWidth
            });
        }

        // Add Post Status column with conditional View button
        var postStatusColumn = new DataGridViewButtonColumn
        {
            Name = "postStatus",
            HeaderText = "Actions",
            Width = 150,
            MinimumWidth = 120,
            UseColumnTextForButtonValue = false,
            FlatStyle = FlatStyle.Flat
        };

        dgvInvoices.Columns.Add(postStatusColumn);

        // Set alignment
        dgvInvoices.Columns["subTotal"].DefaultCellStyle.Alignment =
        dgvInvoices.Columns["totalTax"].DefaultCellStyle.Alignment =
        dgvInvoices.Columns["grandTotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        dgvInvoices.Columns["invoiceId"].DefaultCellStyle.Alignment =
        dgvInvoices.Columns["invoiceDate"].DefaultCellStyle.Alignment =
        dgvInvoices.Columns["status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        // Format numeric columns
        dgvInvoices.Columns["subTotal"].DefaultCellStyle.Format =
        dgvInvoices.Columns["totalTax"].DefaultCellStyle.Format =
        dgvInvoices.Columns["grandTotal"].DefaultCellStyle.Format = "N2";

        // Handle button click event
        // Handle button click event
        dgvInvoices.CellClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvInvoices.Columns["postStatus"].Index)
            {
                // View button clicked - only if invoice is unposted
                if (dgvInvoices.Rows[e.RowIndex].Cells["invoiceId"].Value != null)
                {
                    string postStatus = dgvInvoices.Rows[e.RowIndex].Cells["postStatus"].Tag?.ToString() ?? "";

                    // Only proceed if invoice is unposted
                    if (postStatus.ToLower() != "posted")
                    {
                        currentInvoiceId = Convert.ToInt32(dgvInvoices.Rows[e.RowIndex].Cells["invoiceId"].Value);
                        ShowInvoiceInfo(currentInvoiceId);
                    }
                    else
                    {
                        // Posted invoice - show message or do nothing
                        MessageBox.Show("This invoice is already posted!", "Information",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else if (e.RowIndex >= 0 && dgvInvoices.Rows[e.RowIndex].Cells["invoiceId"].Value != null)
            {
                // Regular cell clicked
                DataGridViewRow row = dgvInvoices.Rows[e.RowIndex];
                currentInvoiceId = Convert.ToInt32(row.Cells["invoiceId"].Value);
            }
        };

        dgvInvoices.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex != dgvInvoices.Columns["postStatus"].Index)
            {
                BtnPreview_Click(s, e);
            }
        };

        // Style the View button
        dgvInvoices.Columns["postStatus"].DefaultCellStyle.Font = new Font("Segoe UI", 8, FontStyle.Bold);
        dgvInvoices.Columns["postStatus"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
    }

    private void ShowInvoiceInfo(int invoiceId)
    {
        try
        {
            DataSet ds = DatabaseHelper.GetInvoicePreviewData(invoiceId);
            if (ds.Tables["InvoiceHeader"].Rows.Count == 0)
            {
                MessageBox.Show("Invoice not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataRow header = ds.Tables["InvoiceHeader"].Rows[0];
            DataTable items = ds.Tables["InvoiceItems"];

            // === POPUP FORM ===
            Form infoForm = new Form
            {
                Text = $"Invoice Details - {header["invoiceNumber"]}",
                Size = new Size(1000, 800),
                //StartPosition = FormStartPosition.Manual,
               WindowState = FormWindowState.Maximized,
                BackColor = Color.White
            };

            // === MAIN LAYOUT ===
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(15)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Invoice Info
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Customer & Seller
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Profit
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Items

            // === HEADER TITLE ===
            var lblTitle = new Label
            {
                Text = "🧾 Invoice Summary",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#1b6656"),
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainLayout.Controls.Add(lblTitle, 0, 0);

            // === INVOICE, BUYER & SELLER INFO PANELS (3 COLUMNS) ===
            string headerFbrInvoiceNo = header.Table.Columns.Contains("fbrInvoiceNumber") && header["fbrInvoiceNumber"] != DBNull.Value ? header["fbrInvoiceNumber"].ToString() : "N/A";
            string scenario = header.Table.Columns.Contains("scenarioId") && header["scenarioId"] != DBNull.Value ? header["scenarioId"].ToString() : "N/A";

            var infoPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 0, 10)
            };
            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            infoPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 1. Invoice Info Card
            var gbInvoice = new GroupBox
            {
                Text = "📄 Invoice Details",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1b6656"),
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(10, 25, 10, 10),
                FlatStyle = FlatStyle.Flat
            };
            var lblHeader = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.Black,
                Dock = DockStyle.Fill,
                Text =
    $@"Invoice No: {header["invoiceNumber"]}
FBR Invoice No: {headerFbrInvoiceNo}
Date: {Convert.ToDateTime(header["invoiceDate"]):yyyy-MM-dd}
Status: {(header.Table.Columns.Contains("postStatus") ? header["postStatus"] : "N/A")}
Scenario ID: {scenario}
Payment Mode: {(header.Table.Columns.Contains("paymentMode") ? header["paymentMode"] : "N/A")}"
            };
            gbInvoice.Controls.Add(lblHeader);

            // 2. Buyer Info Card
            var gbCustomer = new GroupBox
            {
                Text = "👤 Buyer Information",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(10, 25, 10, 10),
                FlatStyle = FlatStyle.Flat
            };
            var lblCustomer = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.Black,
                Dock = DockStyle.Fill,
                Text =
    $@"Name: {header["customerBusinessName"]}
NTN/CNIC: {(header.Table.Columns.Contains("customerNTNCNIC") ? header["customerNTNCNIC"] : "N/A")}
Province: {(header.Table.Columns.Contains("customerProvince") ? header["customerProvince"] : "N/A")}
Address: {(header.Table.Columns.Contains("customerAddress") ? header["customerAddress"] : "N/A")}
Reg. Type: {(header.Table.Columns.Contains("registrationType") ? header["registrationType"] : "N/A")}"
            };
            gbCustomer.Controls.Add(lblCustomer);

            // 3. Seller Info Card
            var gbSeller = new GroupBox
            {
                Text = "🏢 Seller Information",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(10, 25, 10, 10),
                FlatStyle = FlatStyle.Flat
            };
            string sellerNTN = header.Table.Columns.Contains("sellerNTNCNIC") ? header["sellerNTNCNIC"]?.ToString() : "N/A";
            string sellerProvince = header.Table.Columns.Contains("sellerProvince") ? header["sellerProvince"]?.ToString() : "N/A";
            string sellerAddress = header.Table.Columns.Contains("sellerAddress") ? header["sellerAddress"]?.ToString() : "N/A";

            var lblSeller = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.Black,
                Dock = DockStyle.Fill,
                Text =
            $@"Name: {(header.Table.Columns.Contains("sellerBusinessName") ? header["sellerBusinessName"] : "N/A")}
NTN: {sellerNTN}
Province: {sellerProvince}
Address: {sellerAddress}"
            };
            gbSeller.Controls.Add(lblSeller);

            infoPanel.Controls.Add(gbInvoice, 0, 0);
            infoPanel.Controls.Add(gbCustomer, 1, 0);
            infoPanel.Controls.Add(gbSeller, 2, 0);

            mainLayout.Controls.Add(infoPanel, 0, 1);

            // === ITEMS GRID ===
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "description", HeaderText = "Item Description" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "quantity", HeaderText = "Qty", DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "unitPrice", HeaderText = "Unit Price", DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
            if (items.Columns.Contains("totalValues"))
                dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "totalValues", HeaderText = "Total", DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
            

            dgv.DataSource = items;
            mainLayout.Controls.Add(dgv, 0, 4);

            // === BUTTON PANEL (Validate / Post) ===
             var buttonPanel = new FlowLayoutPanel
             {
                 FlowDirection = FlowDirection.RightToLeft,
                 Dock = DockStyle.Bottom,
                 Padding = new Padding(10),
                 Height = 60
             };
 
             // === POST BUTTON ===
             var btnPost = new Button
             {
                 Text = "🚀 Post Invoice",
                 BackColor = ColorTranslator.FromHtml("#1E88E5"),
                 ForeColor = Color.White,
                 Font = new Font("Segoe UI", 10, FontStyle.Bold),
                 Width = 160,
                 Height = 40,
                 FlatStyle = FlatStyle.Flat,
                 Enabled = false
             };
             btnPost.FlatAppearance.BorderSize = 0;

             // === VALIDATE BUTTON ===
             var btnValidate = new Button
             {
                 Text = "✅ Validate Invoice",
                 BackColor = ColorTranslator.FromHtml("#C8A84B"),
                 ForeColor = Color.White,
                 Font = new Font("Segoe UI", 10, FontStyle.Bold),
                 Width = 160,
                 Height = 40,
                 FlatStyle = FlatStyle.Flat
             };
             btnValidate.FlatAppearance.BorderSize = 0;

            // === PROGRESS INDICATOR ===
            var postingProgress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Width = 220,
                Height = 20,
                Visible = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            // === BUTTON ACTIONS ===
            btnValidate.Click += (s, e) =>
            {
                btnValidate.Enabled = false;
                btnPost.Enabled = false;
                postingProgress.Visible = true;

                string payload = BuildInvoiceJsonFromData(header, items);

                string sellerNtn = header.Table.Columns.Contains("sellerNTNCNIC") ? header["sellerNTNCNIC"]?.ToString() : string.Empty;
                string sellerToken = GetSellerToken(sellerNtn);
                if (string.IsNullOrEmpty(sellerToken))
                {
                    postingProgress.Visible = false;
                    btnValidate.Enabled = true;
                    MessageBox.Show("Seller API token not found. Cannot validate.", "Token Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Task.Run(() =>
                {
                    try
                    {
                        var service = new FbrApiService();
                        return service.ValidateInvoiceDataAsync(payload, sellerToken).GetAwaiter().GetResult();
                    }
                    catch (Exception ex) { return "__EXCEPTION__:" + ex.ToString(); }
                }).ContinueWith(t =>
                {
                    postingProgress.Visible = false;
                    btnValidate.Enabled = true;

                    if (t.IsFaulted)
                    {
                        MessageBox.Show("Error validating invoice: " + t.Exception?.GetBaseException().Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string result = t.Result ?? string.Empty;
                    if (result.StartsWith("__EXCEPTION__:"))
                    {
                        MessageBox.Show("Error validating invoice: " + result.Substring("__EXCEPTION__:".Length), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    try
                    {
                        int idx = result.IndexOf('{');
                        string jsonBody = idx >= 0 ? result.Substring(idx) : result;
                        var resp = JObject.Parse(jsonBody);

                        string status = resp["validationResponse"]?["status"]?.ToString() ?? "UNKNOWN";
                        string statusCode = resp["validationResponse"]?["statusCode"]?.ToString() ?? "";

                        if (status.Equals("Valid", StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show($"✅ Validation Successful!\n\n🟢 Status: {status}\nCode: {statusCode}", "Validation Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnPost.Enabled = true;
                        }
                        else
                        {
                            MessageBox.Show($"❌ Validation Failed!\n\nRaw Response:\n{result}", "Validation Result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            btnPost.Enabled = false;
                        }
                    }
                    catch
                    {
                        MessageBox.Show($"⚠️ Could not parse response.\n\nRaw Response:\n{result}", "Validation Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        btnPost.Enabled = false;
                    }

                }, TaskScheduler.FromCurrentSynchronizationContext());
            };

            btnPost.Click += (s, e) =>
            {
                btnPost.Enabled = false;
                btnValidate.Enabled = false;
                postingProgress.Visible = true;

                string payload = BuildInvoiceJsonFromData(header, items);

                string sellerNtn = header.Table.Columns.Contains("sellerNTNCNIC") ? header["sellerNTNCNIC"]?.ToString() : string.Empty;
                string sellerToken = GetSellerToken(sellerNtn);
                if (string.IsNullOrEmpty(sellerToken))
                {
                    postingProgress.Visible = false;
                    btnPost.Enabled = true;
                    btnValidate.Enabled = true;
                    MessageBox.Show("Seller API token not found. Cannot post.", "Token Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Run network call off UI thread so marquee keeps animating
                Task.Run(() =>
                {
                    try
                    {
                        var service = new FbrApiService();
                        return service.PostInvoiceDataAsync(payload, sellerToken).GetAwaiter().GetResult();
                    }
                    catch (Exception ex) { return "__EXCEPTION__:" + ex.ToString(); }
                }).ContinueWith(t =>
                {
                    postingProgress.Visible = false;
                    btnPost.Enabled = true;
                    btnValidate.Enabled = true;

                    if (t.IsFaulted)
                    {
                        MessageBox.Show("Error posting invoice: " + t.Exception?.GetBaseException().Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string result = t.Result ?? string.Empty;
                    if (result.StartsWith("__EXCEPTION__:"))
                    {
                        MessageBox.Show("Error posting invoice: " + result.Substring("__EXCEPTION__:".Length), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    int idx = result.IndexOf('{');
                    string jsonBody = idx >= 0 ? result.Substring(idx) : result;
                    string fbrInvoiceNo = null;
                    try
                    {
                        var resp = JObject.Parse(jsonBody);
                        if (resp["invoiceNumber"] != null)
                            fbrInvoiceNo = resp["invoiceNumber"]?.ToString();
                        else if (resp["validationResponse"] != null && resp["validationResponse"]["invoiceNumber"] != null)
                            fbrInvoiceNo = resp["validationResponse"]["invoiceNumber"]?.ToString();
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(fbrInvoiceNo))
                    {
                        try
                        {
                            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
                            {
                                conn.Open();
                                using (var cmd = new SQLiteCommand("UPDATE Invoices SET fbrInvoiceNumber=@fbr, postStatus='Posted' WHERE invoiceId=@id", conn))
                                {
                                    cmd.Parameters.AddWithValue("@fbr", fbrInvoiceNo);
                                    cmd.Parameters.AddWithValue("@id", invoiceId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            MessageBox.Show($"✅ Posted successfully. FBR Invoice No: {fbrInvoiceNo}", "Posted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadInvoices();
                            infoForm.Close();
                        }
                        catch (Exception dbEx)
                        {
                            MessageBox.Show("Posted but failed to update local DB: " + dbEx.Message, "DB Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"❌ Posting failed. Response:\n{result}", "FBR Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }, TaskScheduler.FromCurrentSynchronizationContext());
            };
 
            // === ADD BUTTONS TO PANEL ===
            buttonPanel.Controls.Add(postingProgress);
            buttonPanel.Controls.Add(btnPost);
            buttonPanel.Controls.Add(btnValidate);
            infoForm.Controls.Add(buttonPanel);

            // === ADD TO FORM ===
            infoForm.Controls.Add(mainLayout);
            infoForm.ShowDialog();

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading invoice info:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }



    // ===== LOAD INVOICES =====
    private void LoadInvoices(DateTime? fromDate = null, DateTime? toDate = null, string search = null, string invoiceNumber = null)
    {
        try
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                DateTime startDate = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime endDate = toDate ?? DateTime.Now.Date;

                string sql = @"
SELECT 
    i.invoiceId,
    i.invoiceNumber,
    i.fbrInvoiceNumber,
    i.invoiceDate,
    i.subTotal,
    i.totalTax,
    i.grandTotal,
    i.status,
    i.postStatus,
    c.customerBusinessName
FROM Invoices i
LEFT JOIN Customers c ON i.customerId = c.customerId
WHERE i.invoiceDate BETWEEN @startDate AND @endDate";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    sql += " AND (i.invoiceNumber LIKE @search OR c.customerBusinessName LIKE @search OR i.fbrInvoiceNumber LIKE @search)";
                }

                if (!string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    sql += " AND i.invoiceNumber LIKE @invoiceNumber";
                }

                sql += " ORDER BY i.invoiceId DESC";

                using (var da = new System.Data.SQLite.SQLiteDataAdapter(sql, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
                    da.SelectCommand.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd 23:59:59"));
                    if (!string.IsNullOrWhiteSpace(search))
                        da.SelectCommand.Parameters.AddWithValue("@search", "%" + search + "%");
                    if (!string.IsNullOrWhiteSpace(invoiceNumber))
                        da.SelectCommand.Parameters.AddWithValue("@invoiceNumber", "%" + invoiceNumber + "%");

                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dgvInvoices.Rows.Clear();
                    foreach (DataRow dr in dt.Rows)
                    {
                        string postStatus = dr["postStatus"]?.ToString() ?? "";
                        string buttonText = "";
                        Color buttonColor = Color.LightGray;
                        Color textColor = Color.Gray;
                        bool enabled = false;

                        // Conditionally set button properties based on post status
                        if (postStatus.ToLower() == "posted")
                        {
                            buttonText = "Posted";
                            buttonColor = ColorTranslator.FromHtml("#4CAF50"); // Green color for posted
                            textColor = Color.White;
                            enabled = false;
                        }
                        else
                        {
                            buttonText = "Invoice info";
                            buttonColor = ColorTranslator.FromHtml("#2196F3"); // Blue color for unposted
                            textColor = Color.White;
                            enabled = true;
                        }

                        int rowIndex = dgvInvoices.Rows.Add(
                            dr["invoiceId"],
                            dr["invoiceNumber"],
                            dr["fbrInvoiceNumber"] == DBNull.Value ? "" : dr["fbrInvoiceNumber"],
                            Convert.ToDateTime(dr["invoiceDate"]).ToString("yyyy-MM-dd"),
                            Convert.ToDecimal(dr["subTotal"]),
                            Convert.ToDecimal(dr["totalTax"]),
                            Convert.ToDecimal(dr["grandTotal"]),
                            dr["status"],
                            buttonText
                        );

                        // Set button appearance and store post status in Tag
                        var buttonCell = dgvInvoices.Rows[rowIndex].Cells["postStatus"] as DataGridViewButtonCell;
                        buttonCell.Style.BackColor = buttonColor;
                        buttonCell.Style.ForeColor = textColor;
                        buttonCell.Style.SelectionBackColor = buttonColor;
                        buttonCell.Style.SelectionForeColor = textColor;
                        dgvInvoices.Rows[rowIndex].Cells["postStatus"].Tag = postStatus;
                        dgvInvoices.Rows[rowIndex].Cells["postStatus"].ReadOnly = !enabled;
                    }

                    currentInvoiceId = -1;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading invoices: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ===== PREVIEW =====
    private void BtnPreview_Click(object sender, EventArgs e)
    {
        if (currentInvoiceId == -1)
        {
            MessageBox.Show("⚠ Please select an invoice first!", "Information",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var previewForm = new InvoicePreviewForm(currentInvoiceId);
        previewForm.ShowDialog();
    }

    // ===== DELETE =====
    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (currentInvoiceId == -1)
        {
            MessageBox.Show("⚠ Please select an invoice first!", "Information",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Are you sure you want to delete this invoice and all its items?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );

        if (confirm == DialogResult.Yes)
        {
            try
            {
                DatabaseHelper.DeleteInvoice(currentInvoiceId);
                MessageBox.Show("✅ Invoice deleted successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error deleting invoice: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private string BuildInvoiceJsonFromData(DataRow header, DataTable items)
    {
        var invoiceJson = new JObject();
        invoiceJson["invoiceType"] = "Sale Invoice";
        invoiceJson["invoiceDate"] = Convert.ToDateTime(header["invoiceDate"]).ToString("yyyy-MM-dd");
        invoiceJson["sellerNTNCNIC"] = header.Table.Columns.Contains("sellerNTNCNIC") ? header["sellerNTNCNIC"]?.ToString() : "";
        invoiceJson["sellerBusinessName"] = header.Table.Columns.Contains("sellerBusinessName") ? header["sellerBusinessName"]?.ToString() : "";
        invoiceJson["sellerProvince"] = header.Table.Columns.Contains("sellerProvince") ? header["sellerProvince"]?.ToString() : "";
        invoiceJson["sellerAddress"] = header.Table.Columns.Contains("sellerAddress") ? header["sellerAddress"]?.ToString() : "";
        invoiceJson["buyerNTNCNIC"] = header.Table.Columns.Contains("customerNTNCNIC") ? header["customerNTNCNIC"]?.ToString() : "";
        invoiceJson["buyerBusinessName"] = header.Table.Columns.Contains("customerBusinessName") ? header["customerBusinessName"]?.ToString() : "";
        invoiceJson["buyerProvince"] = header.Table.Columns.Contains("customerProvince") ? header["customerProvince"]?.ToString() : "";
        invoiceJson["buyerAddress"] = header.Table.Columns.Contains("customerAddress") ? header["customerAddress"]?.ToString() : "";
        invoiceJson["buyerRegistrationType"] = header.Table.Columns.Contains("registrationType") ? header["registrationType"]?.ToString() : "";

        // FIX 1: FBR expects "sourceInvoiceNo", not "invoiceRefNo"
        invoiceJson["sourceInvoiceNo"] = header.Table.Columns.Contains("invoiceNumber") ? header["invoiceNumber"]?.ToString() : "";

        // FIX 2: scenarioId from header (falls back to first item's saleType if header has none)
        string scenarioId = header.Table.Columns.Contains("scenarioId") ? header["scenarioId"]?.ToString() : "";
        if (string.IsNullOrWhiteSpace(scenarioId)) scenarioId = "SN001";
        invoiceJson["scenarioId"] = scenarioId;

        JArray itemsArray = new JArray();
        foreach (DataRow it in items.Rows)
        {
            var itemObj = new JObject();
            itemObj["hsCode"] = it.Table.Columns.Contains("hsCode") ? FbrApiService.SanitizeForJson(it["hsCode"]?.ToString(), 50) : "";
            itemObj["productDescription"] = it.Table.Columns.Contains("description") && it["description"] != DBNull.Value && !string.IsNullOrWhiteSpace(it["description"].ToString()) ? FbrApiService.SanitizeForJson(it["description"].ToString(), 300) : (it.Table.Columns.Contains("productDescription") ? FbrApiService.SanitizeForJson(it["productDescription"]?.ToString(), 300) : "");
            itemObj["rate"] = it.Table.Columns.Contains("rate") ? it["rate"]?.ToString() : "";
            itemObj["uoM"] = it.Table.Columns.Contains("uoM") ? FbrApiService.SanitizeForJson(it["uoM"]?.ToString(), 20) : "";

            decimal qty = 0m;
            decimal totalValues = 0m;
            decimal valueExcl = 0m;
            decimal salesTax = 0m;
            decimal further = 0m;

            decimal.TryParse(it.Table.Columns.Contains("quantity") ? it["quantity"]?.ToString() : "0", out qty);
            decimal.TryParse(it.Table.Columns.Contains("totalValues") ? it["totalValues"]?.ToString() : "0", out totalValues);

            // FIX 3: use stored valueSalesExcludingST first, then fall back to calculation
            if (it.Table.Columns.Contains("valueSalesExcludingST") && it["valueSalesExcludingST"] != DBNull.Value && decimal.TryParse(it["valueSalesExcludingST"]?.ToString(), out valueExcl) && valueExcl != 0m)
            {
                // use stored value
            }
            else
            {
                decimal rateVal = 0m;
                string rateStr = it.Table.Columns.Contains("rate") ? it["rate"]?.ToString()?.Replace("%", "").Trim() : "0";
                decimal.TryParse(rateStr, out rateVal);
                valueExcl = rateVal != 0m ? totalValues / (1 + rateVal / 100) : totalValues;
            }

            decimal.TryParse(it.Table.Columns.Contains("salesTaxApplicable") ? it["salesTaxApplicable"]?.ToString() : "0", out salesTax);
            decimal.TryParse(it.Table.Columns.Contains("furtherTax") ? it["furtherTax"]?.ToString() : "0", out further);

            itemObj["quantity"] = qty;
            itemObj["totalValues"] = totalValues;
            itemObj["valueSalesExcludingST"] = valueExcl;
            itemObj["fixedNotifiedValueOrRetailPrice"] = 0.00m;
            itemObj["salesTaxApplicable"] = salesTax;
            itemObj["salesTaxWithheldAtSource"] = 0.00m;
            itemObj["extraTax"] = 0.00m;
            itemObj["furtherTax"] = further;
            itemObj["fedPayable"] = 0.00m;
            itemObj["discount"] = it.Table.Columns.Contains("discount") && it["discount"] != DBNull.Value ? Convert.ToDecimal(it["discount"]) : 0.00m;

            // FIX 4: Normalize saleType to FBR-accepted values
            string saleTypeRaw = it.Table.Columns.Contains("saleType") ? it["saleType"]?.ToString() : null;
            string saleType = NormalizeSaleType(saleTypeRaw);
            itemObj["saleType"] = FbrApiService.SanitizeForJson(saleType, 100);

            // FIX 5: sroScheduleNo and sroItemSerialNo must be EMPTY for SN001 (standard rate).
            // Sending Eighth/Third Schedule info alongside SN001 causes FBR error 0204 ("Sale type not match").
            bool isStandardRate = saleType.Equals("Goods at standard rate (default)", StringComparison.OrdinalIgnoreCase);
            string sroSchedule = isStandardRate ? "" : (it.Table.Columns.Contains("sroScheduleNo") ? FbrApiService.SanitizeForJson(it["sroScheduleNo"]?.ToString(), 100) : "");
            string sroSerial   = isStandardRate ? "" : (it.Table.Columns.Contains("sroItemSerialNo") ? FbrApiService.SanitizeForJson(it["sroItemSerialNo"]?.ToString(), 50) : "");
            itemObj["sroScheduleNo"]  = sroSchedule;
            itemObj["sroItemSerialNo"] = sroSerial;

            itemsArray.Add(itemObj);
        }
        invoiceJson["items"] = itemsArray;
        return invoiceJson.ToString(Newtonsoft.Json.Formatting.Indented);
    }

    /// <summary>
    /// Normalizes saleType values from the database/UI dropdowns to FBR-accepted strings.
    /// Legacy values like "Standard" are mapped to "Goods at standard rate (default)".
    /// </summary>
    private static string NormalizeSaleType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("Select", StringComparison.OrdinalIgnoreCase))
            return "Goods at standard rate (default)";

        // If it's standard or standard rate, map to the default FBR string
        if (raw.StartsWith("Goods at standard rate", StringComparison.OrdinalIgnoreCase) || raw.Equals("standard", StringComparison.OrdinalIgnoreCase))
            return "Goods at standard rate (default)";

        // Strip parenthetical suffixes: "Goods at standard rate (default)" was handled above, for others e.g. "Services (fed in st mode)" -> "Services" etc.
        int paren = raw.IndexOf('(');
        string cleaned = paren > 0 ? raw.Substring(0, paren).Trim() : raw.Trim();

        // Map legacy / shorthand values saved to DB
        switch (cleaned.ToLowerInvariant())
        {
            case "reduced":                  return "Goods at Reduced Rate";
            case "zero": case "zero-rate":   return "Goods at Zero-rate";
            case "exempt": case "exempt goods": return "Exempt goods";
            case "services":                 return "Services";
            case "petroleum": case "petroleum products": return "Petroleum Products";
            case "sim":                      return "SIM";
            case "cng": case "gas to cng stations": return "Gas to CNG stations";
            case "mobile phones":            return "Mobile Phones";
            case "3rd schedule": case "3rd schedule goods": return "3rd Schedule Goods";
            case "fed (st mode)": case "goods (fed in st mode)": return "Goods (FED in ST Mode)";
            case "services (fed in st mode)": return "Services (FED in ST Mode)";
            case "ship breaking":            return "Ship breaking";
            default:
                // Already a proper FBR value — return as-is
                return raw.Trim();
        }
    }
}