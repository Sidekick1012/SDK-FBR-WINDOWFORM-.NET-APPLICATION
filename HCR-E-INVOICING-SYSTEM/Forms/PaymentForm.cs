using SDK_E_INVOICING_SYSTEM.Data;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

public class PaymentForm : Form
{
    private ComboBox cmbInvoices, cmbMethod;
    private TextBox txtAmount;
    private Button btnAdd, btnClear;
    private DataGridView dgvPayments;

    private int selectedInvoiceId = -1;

    // ===== INFO PANEL LABELS =====
    private Label lblInvoiceNumber, lblInvoiceDate, lblInvoiceTotal, lblInvoiceStatus;

    public PaymentForm()
    {
        
        this.Text = "Payment Management";
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.White;
        this.Load += (s, e) => FormTransitionHelper.AnimateFadeIn(this);

        // ===== MAIN LAYOUT =====
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Info Panel
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Inputs
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Buttons
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // DataGridView

        // ===== HEADER =====
        var header = new Label
        {
            Text = "💰 Payment Management",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorTranslator.FromHtml("#1D2068"),
            TextAlign = ContentAlignment.MiddleCenter
        };
        mainLayout.Controls.Add(header, 0, 0);

        // ===== INFO PANEL =====
        var infoPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 60,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(10)
        };
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        lblInvoiceNumber = MakeInfoLabel("Invoice: -");
        lblInvoiceDate = MakeInfoLabel("Date: -");
        lblInvoiceTotal = MakeInfoLabel("Total: -");
        lblInvoiceStatus = MakeInfoLabel("Status: -");

        infoPanel.Controls.Add(lblInvoiceNumber, 0, 0);
        infoPanel.Controls.Add(lblInvoiceDate, 1, 0);
        infoPanel.Controls.Add(lblInvoiceTotal, 2, 0);
        infoPanel.Controls.Add(lblInvoiceStatus, 3, 0);

        mainLayout.Controls.Add(infoPanel, 0, 1);

        // ===== INPUT PANEL =====
        var inputPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(10, 10, 10, 15),
            AutoSize = true
        };
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

        inputPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        inputPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        inputPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

        // --- Invoice ComboBox ---
        inputPanel.Controls.Add(new Label { Text = "Select Invoice *", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        cmbInvoices = new ComboBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11),
            Margin = new Padding(3),
            DropDownStyle = ComboBoxStyle.DropDownList,
            DropDownWidth = 300
        };
        cmbInvoices.SelectedIndexChanged += (s, e) => InvoiceSelected();
        inputPanel.Controls.Add(cmbInvoices, 1, 0);

        // --- Payment Method ComboBox ---
        inputPanel.Controls.Add(new Label { Text = "Payment Method *", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        cmbMethod = new ComboBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11),
            Margin = new Padding(3),
            DropDownStyle = ComboBoxStyle.DropDownList,
            DropDownWidth = 150
        };
        cmbMethod.Items.AddRange(new string[] { "Cash", "Bank Transfer", "Cheque" });
        inputPanel.Controls.Add(cmbMethod, 1, 1);

        // --- Amount TextBox ---
        inputPanel.Controls.Add(new Label { Text = "Amount *", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        txtAmount = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11),
            Margin = new Padding(3),
            TextAlign = HorizontalAlignment.Left
        };
        inputPanel.Controls.Add(txtAmount, 1, 2);

        mainLayout.Controls.Add(inputPanel, 0, 2);

        // ===== BUTTON PANEL =====
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10),
            AutoSize = true
        };
        btnAdd = MakeButton("➕ Add Payment", "#C8A84B");
        btnClear = MakeButton("🧹 Clear", "#4A4A6A");
        btnAdd.Click += (s, e) => AddPayment();
        btnClear.Click += (s, e) => ClearFields();
        btnPanel.Controls.AddRange(new Control[] { btnAdd, btnClear });

        mainLayout.Controls.Add(btnPanel, 0, 3);

        // ===== DATA GRID =====
        dgvPayments = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowTemplate = { Height = 35 },
            ColumnHeadersHeight = 50,
            RowHeadersVisible = false
        };

        // ===== STYLE LIKE CUSTOMER/PRODUCT GRID =====
        dgvPayments.EnableHeadersVisualStyles = false;
        dgvPayments.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#1D2068");
        dgvPayments.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvPayments.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgvPayments.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvPayments.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

        dgvPayments.DefaultCellStyle.Font = new Font("Segoe UI", 10);
        dgvPayments.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 60);
        dgvPayments.DefaultCellStyle.BackColor = Color.White;
        dgvPayments.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 250);
        dgvPayments.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#C8A84B");
        dgvPayments.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvPayments.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        dgvPayments.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;


        mainLayout.Controls.Add(dgvPayments, 0, 4);

        this.Controls.Add(mainLayout);

        LoadInvoices();
    }

    private Label MakeInfoLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.Black,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Button MakeButton(string text, string color)
    {
        return new Button
        {
            Text = text,
            Width = 120,
            Height = 32,
            BackColor = ColorTranslator.FromHtml(color),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(3)
        };
    }

    // ===== LOAD INVOICES =====
    private void LoadInvoices()
    {
        DataTable dt = DatabaseHelper.GetInvoices();
        cmbInvoices.DataSource = dt;
        cmbInvoices.DisplayMember = "invoiceNumber";
        cmbInvoices.ValueMember = "invoiceId";
        cmbInvoices.SelectedIndex = -1;

        ClearFields();
        dgvPayments.DataSource = null;
    }

    // ===== SELECT INVOICE =====
    private void InvoiceSelected()
    {
        if (cmbInvoices.SelectedValue != null && int.TryParse(cmbInvoices.SelectedValue.ToString(), out int invoiceId))
        {
            selectedInvoiceId = invoiceId;
            LoadPayments(invoiceId);

            DataRow invoice = DatabaseHelper.GetInvoices().Select($"invoiceId={invoiceId}")[0];
            lblInvoiceNumber.Text = $"Invoice: {invoice["invoiceNumber"]}";
            lblInvoiceDate.Text = $"Date: {Convert.ToDateTime(invoice["invoiceDate"]).ToString("yyyy-MM-dd")}";
            lblInvoiceTotal.Text = $"Total: {invoice["grandTotal"]}";
            lblInvoiceStatus.Text = $"Status: {invoice["status"]}";

            // Disable Add button if already paid
            if (invoice["status"].ToString().ToLower() == "paid")
            {
                btnAdd.Enabled = false;
                btnAdd.Text = "✅ Already Paid";
            }
            else
            {
                btnAdd.Enabled = true;
                btnAdd.Text = "➕ Add Payment";
            }
        }
        else
        {
            selectedInvoiceId = -1;
            dgvPayments.DataSource = null;
            lblInvoiceNumber.Text = "Invoice: -";
            lblInvoiceDate.Text = "Date: -";
            lblInvoiceTotal.Text = "Total: -";
            lblInvoiceStatus.Text = "Status: -";
            btnAdd.Enabled = true;
            btnAdd.Text = "➕ Add Payment";
        }
    }

    // ===== LOAD PAYMENTS =====
    private void LoadPayments(int invoiceId)
    {
        dgvPayments.DataSource = DatabaseHelper.GetPayments(invoiceId);

        // Align numeric columns to right
        foreach (DataGridViewColumn col in dgvPayments.Columns)
        {
            if (col.Name.ToLower().Contains("amount") || col.Name.ToLower().Contains("total"))
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            else
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }
    }

    // ===== ADD PAYMENT =====
    private void AddPayment()
    {
        if (selectedInvoiceId == -1)
        {
            MessageBox.Show("Please select an invoice.");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtAmount.Text) || !decimal.TryParse(txtAmount.Text, out decimal amount))
        {
            MessageBox.Show("Enter a valid amount.");
            txtAmount.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(cmbMethod.Text))
        {
            MessageBox.Show("Select payment method.");
            cmbMethod.Focus();
            return;
        }

        // ===== VALIDATE EXACT AMOUNT =====
        decimal invoiceTotal = 0;
        decimal totalPaid = 0;

        try
        {
            DataRow invoice = DatabaseHelper.GetInvoices().Select($"invoiceId={selectedInvoiceId}")[0];
            invoiceTotal = Convert.ToDecimal(invoice["grandTotal"]);

            // Get total payments already made
            DataTable payments = DatabaseHelper.GetPayments(selectedInvoiceId);
            foreach (DataRow row in payments.Rows)
            {
                totalPaid += Convert.ToDecimal(row["amount"]);
            }

            decimal remaining = invoiceTotal - totalPaid;

            if (amount != remaining)
            {
                MessageBox.Show("Payment must be exactly equal to the remaining balance: {remaining:C}");
                txtAmount.Focus();
                return;
            }
        }
        catch
        {
            MessageBox.Show("Error validating payment amount.");
            return;
        }

        // ===== ADD PAYMENT =====
        try
        {
            DatabaseHelper.AddPayment(selectedInvoiceId, amount, cmbMethod.Text, "Paid");

            // Mark invoice as Paid
            string sql = $"UPDATE Invoices SET status='Paid' WHERE invoiceId={selectedInvoiceId}";
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new System.Data.SQLite.SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            LoadPayments(selectedInvoiceId);
            InvoiceSelected(); // update info panel
            ClearFields();
            MessageBox.Show("Payment added successfully!");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error adding payment: " + ex.Message);
        }
    }



    private void ClearFields()
    {
        cmbMethod.SelectedIndex = -1;
        txtAmount.Clear();
    }
}