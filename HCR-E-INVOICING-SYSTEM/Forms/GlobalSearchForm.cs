using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using SDK_E_INVOICING_SYSTEM.Data;
using SDK_E_INVOICING_SYSTEM;
using InvoiceApp;

public class GlobalSearchForm : Form
{
    private TabControl tabControl;
    private TabPage tabCustomers, tabProducts, tabInvoices;
    private DataGridView dgvCustomers, dgvProducts, dgvInvoices;
    private TextBox txtSearchTerm;
    private Button btnSearch;
    private Label lblResultCount;
    private Form parentDashboard;

    public GlobalSearchForm(Form dashboard, string initialSearch = "")
    {
        this.parentDashboard = dashboard;

        // Form settings
        this.Text = "Global Search Results";
        this.Size = new Size(900, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.White;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        // Main Layout
        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(15)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Search query header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Tab control
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Status bar
        this.Controls.Add(mainLayout);

        // Header Panel
        Panel headerPanel = new Panel { Dock = DockStyle.Fill };
        mainLayout.Controls.Add(headerPanel, 0, 0);

        Label lblSearch = new Label
        {
            Text = "Query:",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1D2068"),
            Location = new Point(0, 12),
            AutoSize = true
        };
        headerPanel.Controls.Add(lblSearch);

        txtSearchTerm = new TextBox
        {
            Text = initialSearch,
            Font = new Font("Segoe UI", 11),
            Location = new Point(65, 8),
            Size = new Size(600, 30)
        };
        txtSearchTerm.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { PerformSearch(); e.Handled = true; e.SuppressKeyPress = true; } };
        headerPanel.Controls.Add(txtSearchTerm);

        btnSearch = new Button
        {
            Text = "🔍 Search",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#1D2068"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(675, 7),
            Size = new Size(100, 30),
            Cursor = Cursors.Hand
        };
        btnSearch.FlatAppearance.BorderSize = 0;
        btnSearch.Click += (s, e) => PerformSearch();
        headerPanel.Controls.Add(btnSearch);

        // Tab Control
        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10)
        };
        mainLayout.Controls.Add(tabControl, 0, 1);

        // Tab Pages
        tabCustomers = new TabPage("👥 Customers (0)");
        tabProducts = new TabPage("📦 Products (0)");
        tabInvoices = new TabPage("🧾 Invoices (0)");

        tabControl.TabPages.Add(tabCustomers);
        tabControl.TabPages.Add(tabProducts);
        tabControl.TabPages.Add(tabInvoices);

        // Grids Setup
        dgvCustomers = CreateStyledGrid();
        dgvCustomers.CellDoubleClick += DgvCustomers_CellDoubleClick;
        tabCustomers.Controls.Add(dgvCustomers);

        dgvProducts = CreateStyledGrid();
        dgvProducts.CellDoubleClick += DgvProducts_CellDoubleClick;
        tabProducts.Controls.Add(dgvProducts);

        dgvInvoices = CreateStyledGrid();
        dgvInvoices.CellDoubleClick += DgvInvoices_CellDoubleClick;
        tabInvoices.Controls.Add(dgvInvoices);

        // Status bar
        lblResultCount = new Label
        {
            Text = "Ready",
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            ForeColor = Color.Gray,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        mainLayout.Controls.Add(lblResultCount, 0, 2);

        // Perform initial search
        if (!string.IsNullOrEmpty(initialSearch))
        {
            PerformSearch();
        }

        // Animate fade-in
        FormTransitionHelper.AnimateFadeIn(this);
    }

    private DataGridView CreateStyledGrid()
    {
        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowTemplate = { Height = 35 },
            ColumnHeadersHeight = 40,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#1D2068");
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9);
        dgv.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 60);
        dgv.DefaultCellStyle.BackColor = Color.White;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
        dgv.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#C8A84B");
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;

        return dgv;
    }

    private void PerformSearch()
    {
        string term = txtSearchTerm.Text.Trim();
        if (string.IsNullOrEmpty(term)) return;

        int customersCount = SearchCustomers(term);
        int productsCount = SearchProducts(term);
        int invoicesCount = SearchInvoices(term);

        tabCustomers.Text = $"👥 Customers ({customersCount})";
        tabProducts.Text = $"📦 Products ({productsCount})";
        tabInvoices.Text = $"🧾 Invoices ({invoicesCount})";

        lblResultCount.Text = $"Found {customersCount} customers, {productsCount} products, and {invoicesCount} invoices matching '{term}'. Double-click a row to open.";
    }

    private int SearchCustomers(string term)
    {
        try
        {
            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                string sql = "SELECT customerId AS [ID], customerBusinessName AS [Business Name], customerNTNCNIC AS [NTN/CNIC], customerProvince AS [Province], customerAddress AS [Address] FROM Customers WHERE customerBusinessName LIKE @term OR customerNTNCNIC LIKE @term";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvCustomers.DataSource = dt;
                    return dt.Rows.Count;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Customer search failed: " + ex.Message);
            return 0;
        }
    }

    private int SearchProducts(string term)
    {
        try
        {
            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                string sql = "SELECT productId AS [ID], hsCode AS [HS Code], productDescription AS [Description], rate AS [Rate], uoM AS [UOM] FROM Products WHERE productDescription LIKE @term OR hsCode LIKE @term";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvProducts.DataSource = dt;
                    return dt.Rows.Count;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Product search failed: " + ex.Message);
            return 0;
        }
    }

    private int SearchInvoices(string term)
    {
        try
        {
            using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT 
                        i.invoiceId AS [ID], 
                        i.invoiceNumber AS [Invoice Number], 
                        i.fbrInvoiceNumber AS [FBR Number], 
                        i.invoiceDate AS [Date], 
                        c.customerBusinessName AS [Customer], 
                        i.grandTotal AS [Grand Total], 
                        i.status AS [Status]
                    FROM Invoices i
                    LEFT JOIN Customers c ON i.customerId = c.customerId
                    WHERE i.invoiceNumber LIKE @term OR i.fbrInvoiceNumber LIKE @term OR c.customerBusinessName LIKE @term";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvInvoices.DataSource = dt;
                    return dt.Rows.Count;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Invoice search failed: " + ex.Message);
            return 0;
        }
    }

    private void DgvCustomers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && dgvCustomers.Rows[e.RowIndex].Cells["Business Name"].Value != null)
        {
            this.Close();
            parentDashboard.Hide();
            var form = new CustomerForm();
            form.FormClosed += (s, args) => {
                parentDashboard.Show();
                FormTransitionHelper.AnimateFadeIn(parentDashboard);
            };
            form.Show();
            FormTransitionHelper.AnimateFadeIn(form);
        }
    }

    private void DgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && dgvProducts.Rows[e.RowIndex].Cells["Description"].Value != null)
        {
            this.Close();
            parentDashboard.Hide();
            var form = new ProductForm();
            form.FormClosed += (s, args) => {
                parentDashboard.Show();
                FormTransitionHelper.AnimateFadeIn(parentDashboard);
            };
            form.Show();
            FormTransitionHelper.AnimateFadeIn(form);
        }
    }

    private void DgvInvoices_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && dgvInvoices.Rows[e.RowIndex].Cells["Invoice Number"].Value != null)
        {
            string invNo = dgvInvoices.Rows[e.RowIndex].Cells["Invoice Number"].Value.ToString();
            this.Close();
            parentDashboard.Hide();
            var form = new InvoiceViewerForm(invNo);
            form.FormClosed += (s, args) => {
                parentDashboard.Show();
                FormTransitionHelper.AnimateFadeIn(parentDashboard);
            };
            form.Show();
        }
    }
}
