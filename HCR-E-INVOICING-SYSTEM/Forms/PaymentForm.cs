using HCR_E_INVOICING_SYSTEM.Data;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

public class PaymentForm : Form
{
    private ComboBox cmbInvoices, cmbMethod;
    private TextBox txtAmount, txtCheckNo, txtBankName, txtAccountNo;
    private Label lblCheckNo, lblBankName, lblAccountNo;
    private Panel pnlCheckDetails, pnlBankDetails;
    private Button btnAdd, btnClear;
    private DataGridView dgvPayments;

    private int selectedInvoiceId = -1;

    // ===== INFO PANEL LABELS =====
    private Label lblInvoiceNumber, lblInvoiceDate, lblInvoiceTotal, lblInvoiceStatus;

    public PaymentForm()
    {
        this.Text = "Payment Management";
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.FromArgb(245, 247, 252);
        this.Font = new Font("Segoe UI", 9.5f);
        this.Load += (s, e) => FormTransitionHelper.AnimateFadeIn(this);

        // ===== MAIN LAYOUT =====
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.FromArgb(245, 247, 252)
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));   // Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // Info Panel
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Input + extra fields
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Buttons
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
            ColumnCount = 4,
            RowCount = 1,
            BackColor = ColorTranslator.FromHtml("#ECEEF8"),
            Padding = new Padding(15, 0, 15, 0)
        };
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        lblInvoiceNumber = MakeInfoLabel("📄 Invoice: -");
        lblInvoiceDate   = MakeInfoLabel("📅 Date: -");
        lblInvoiceTotal  = MakeInfoLabel("💵 Total: -");
        lblInvoiceStatus = MakeInfoLabel("🔖 Status: -");

        infoPanel.Controls.Add(lblInvoiceNumber, 0, 0);
        infoPanel.Controls.Add(lblInvoiceDate,   1, 0);
        infoPanel.Controls.Add(lblInvoiceTotal,  2, 0);
        infoPanel.Controls.Add(lblInvoiceStatus, 3, 0);

        mainLayout.Controls.Add(infoPanel, 0, 1);

        // ===== INPUT PANEL (outer wrapper) =====
        var inputWrapper = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = Color.FromArgb(245, 247, 252),
            Padding = new Padding(20, 12, 20, 8)
        };

        // GroupBox Card for inputs
        var gbForm = new GroupBox
        {
            Text = "  Payment Details",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1D2068"),
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(15, 20, 15, 10),
            BackColor = Color.White
        };

        var inputPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            Padding = new Padding(10, 8, 10, 4),
            BackColor = Color.White
        };
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // label
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));   // control
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // label2
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));   // control2

        // Row 0: Invoice | Payment Method
        inputPanel.Controls.Add(MakeFieldLabel("Select Invoice *"), 0, 0);
        cmbInvoices = MakeComboBox();
        cmbInvoices.DropDownWidth = 350;
        cmbInvoices.SelectedIndexChanged += (s, e) => InvoiceSelected();
        inputPanel.Controls.Add(cmbInvoices, 1, 0);

        inputPanel.Controls.Add(MakeFieldLabel("Payment Method *"), 2, 0);
        cmbMethod = MakeComboBox();
        cmbMethod.Items.AddRange(new string[] { "Cash", "Bank Transfer", "Cheque" });
        cmbMethod.SelectedIndexChanged += CmbMethod_SelectedIndexChanged;
        inputPanel.Controls.Add(cmbMethod, 3, 0);

        // Row 1: Amount | (conditional placeholder)
        inputPanel.Controls.Add(MakeFieldLabel("Amount (PKR) *"), 0, 1);
        txtAmount = MakeTextBox();
        txtAmount.KeyPress += (s, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                e.Handled = true;
        };
        inputPanel.Controls.Add(txtAmount, 1, 1);

        // empty right side for row 1 — will be filled by Cheque / Bank panels below
        inputPanel.Controls.Add(new Label(), 2, 1);
        inputPanel.Controls.Add(new Label(), 3, 1);

        gbForm.Controls.Add(inputPanel);

        // ===== CHEQUE DETAILS PANEL =====
        pnlCheckDetails = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.White,
            Padding = new Padding(10, 0, 10, 0),
            Visible = false
        };

        var tlpCheck = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            BackColor = Color.White
        };
        tlpCheck.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        tlpCheck.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        tlpCheck.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        tlpCheck.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        lblCheckNo = MakeFieldLabel("Cheque No *");
        txtCheckNo = MakeTextBox();
        lblBankName = MakeFieldLabel("Bank Name *");
        txtBankName = MakeTextBox();

        tlpCheck.Controls.Add(lblCheckNo, 0, 0);
        tlpCheck.Controls.Add(txtCheckNo, 1, 0);
        tlpCheck.Controls.Add(lblBankName, 2, 0);
        tlpCheck.Controls.Add(txtBankName, 3, 0);

        pnlCheckDetails.Controls.Add(tlpCheck);
        gbForm.Controls.Add(pnlCheckDetails);

        // ===== BANK TRANSFER DETAILS PANEL =====
        pnlBankDetails = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.White,
            Padding = new Padding(10, 0, 10, 0),
            Visible = false
        };

        var tlpBank = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            BackColor = Color.White
        };
        tlpBank.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        tlpBank.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        tlpBank.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        tlpBank.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        lblAccountNo = MakeFieldLabel("Account / IBAN No *");
        txtAccountNo = MakeTextBox();
        var lblBankNameTransfer = MakeFieldLabel("Bank Name *");
        var txtBankNameTransfer = MakeTextBox();
        // reuse txtBankName for bank transfer as well
        // (we'll use txtBankName for both, sharing via reference)
        // Actually let's use separate controls but wire them together
        txtBankNameTransfer.Tag = "bankNameTransfer";

        tlpBank.Controls.Add(lblAccountNo, 0, 0);
        tlpBank.Controls.Add(txtAccountNo, 1, 0);
        tlpBank.Controls.Add(lblBankNameTransfer, 2, 0);
        tlpBank.Controls.Add(txtBankNameTransfer, 3, 0);

        // Store bank transfer's bank name textbox
        txtBankName = txtBankNameTransfer; // share same field reference

        pnlBankDetails.Controls.Add(tlpBank);
        gbForm.Controls.Add(pnlBankDetails);

        inputWrapper.Controls.Add(gbForm);
        mainLayout.Controls.Add(inputWrapper, 0, 2);

        // ===== BUTTON PANEL =====
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(20, 8, 20, 8),
            BackColor = Color.FromArgb(245, 247, 252)
        };

        btnAdd   = MakeButton("➕ Add Payment", "#1D9348");
        btnClear = MakeButton("🧹 Clear", "#4A4A6A");
        btnAdd.Width = 160;
        btnClear.Width = 130;
        btnAdd.Click   += (s, e) => AddPayment();
        btnClear.Click += (s, e) => ClearFields();

        btnPanel.Controls.Add(btnAdd);
        btnPanel.Controls.Add(btnClear);
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
            RowTemplate = { Height = 36 },
            ColumnHeadersHeight = 44,
            RowHeadersVisible = false
        };

        dgvPayments.EnableHeadersVisualStyles = false;
        dgvPayments.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#1D2068");
        dgvPayments.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvPayments.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgvPayments.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvPayments.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
        dgvPayments.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 60);
        dgvPayments.DefaultCellStyle.BackColor = Color.White;
        dgvPayments.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 250);
        dgvPayments.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#C8A84B");
        dgvPayments.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvPayments.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

        mainLayout.Controls.Add(dgvPayments, 0, 4);

        this.Controls.Add(mainLayout);

        LoadInvoices();
    }

    private void CmbMethod_SelectedIndexChanged(object sender, EventArgs e)
    {
        string method = cmbMethod.Text;
        pnlCheckDetails.Visible = (method == "Cheque");
        pnlBankDetails.Visible  = (method == "Bank Transfer");
    }

    // ===== HELPER CONTROLS =====
    private Label MakeInfoLabel(string text) => new Label
    {
        Text = text,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = ColorTranslator.FromHtml("#1D2068"),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private Label MakeFieldLabel(string text) => new Label
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Segoe UI", 9.5f),
        ForeColor = Color.FromArgb(60, 60, 90),
        Margin = new Padding(3, 6, 3, 3)
    };

    private ComboBox MakeComboBox() => new ComboBox
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10),
        Margin = new Padding(3, 4, 3, 4),
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private TextBox MakeTextBox() => new TextBox
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10),
        Margin = new Padding(3, 4, 3, 4)
    };

    private Button MakeButton(string text, string color) => new Button
    {
        Text = text,
        Height = 38,
        BackColor = ColorTranslator.FromHtml(color),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        Margin = new Padding(0, 0, 10, 0),
        Cursor = Cursors.Hand
    };

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
            lblInvoiceNumber.Text = $"📄 Invoice: {invoice["invoiceNumber"]}";
            lblInvoiceDate.Text   = $"📅 Date: {Convert.ToDateTime(invoice["invoiceDate"]).ToString("yyyy-MM-dd")}";
            lblInvoiceTotal.Text  = $"💵 Total: {Convert.ToDecimal(invoice["grandTotal"]).ToString("N2")} PKR";
            lblInvoiceStatus.Text = $"🔖 Status: {invoice["status"]}";

            bool isPaid = invoice["status"].ToString().ToLower() == "paid";
            btnAdd.Enabled = !isPaid;
            btnAdd.Text = isPaid ? "✅ Already Paid" : "➕ Add Payment";
        }
        else
        {
            selectedInvoiceId = -1;
            dgvPayments.DataSource = null;
            lblInvoiceNumber.Text = "📄 Invoice: -";
            lblInvoiceDate.Text   = "📅 Date: -";
            lblInvoiceTotal.Text  = "💵 Total: -";
            lblInvoiceStatus.Text = "🔖 Status: -";
            btnAdd.Enabled = true;
            btnAdd.Text = "➕ Add Payment";
        }
    }

    // ===== LOAD PAYMENTS =====
    private void LoadPayments(int invoiceId)
    {
        dgvPayments.DataSource = DatabaseHelper.GetPayments(invoiceId);
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
        { MessageBox.Show("Please select an invoice.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        if (string.IsNullOrWhiteSpace(txtAmount.Text) || !decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
        { MessageBox.Show("Enter a valid amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtAmount.Focus(); return; }

        if (string.IsNullOrWhiteSpace(cmbMethod.Text))
        { MessageBox.Show("Please select a payment method.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); cmbMethod.Focus(); return; }

        string method = cmbMethod.Text;
        string checkNo = null;
        string bankName = null;
        string accountNo = null;

        // Validate Cheque fields
        if (method == "Cheque")
        {
            if (string.IsNullOrWhiteSpace(txtCheckNo.Text))
            { MessageBox.Show("Please enter the Cheque Number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtCheckNo.Focus(); return; }
            if (string.IsNullOrWhiteSpace(txtBankName.Text))
            { MessageBox.Show("Please enter the Bank Name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtBankName.Focus(); return; }
            checkNo = txtCheckNo.Text.Trim();
            bankName = txtBankName.Text.Trim();
        }

        // Validate Bank Transfer fields
        if (method == "Bank Transfer")
        {
            if (string.IsNullOrWhiteSpace(txtAccountNo.Text))
            { MessageBox.Show("Please enter the Account / IBAN Number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtAccountNo.Focus(); return; }
            if (string.IsNullOrWhiteSpace(txtBankName.Text))
            { MessageBox.Show("Please enter the Bank Name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtBankName.Focus(); return; }
            accountNo = txtAccountNo.Text.Trim();
            bankName = txtBankName.Text.Trim();
        }

        // Validate remaining balance
        try
        {
            DataRow invoice = DatabaseHelper.GetInvoices().Select($"invoiceId={selectedInvoiceId}")[0];
            decimal invoiceTotal = Convert.ToDecimal(invoice["grandTotal"]);
            DataTable payments = DatabaseHelper.GetPayments(selectedInvoiceId);
            decimal totalPaid = 0;
            foreach (DataRow row in payments.Rows)
                totalPaid += Convert.ToDecimal(row["amount"]);

            decimal remaining = invoiceTotal - totalPaid;
            if (amount != remaining)
            {
                MessageBox.Show($"Payment must equal the remaining balance: {remaining:N2} PKR", "Amount Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAmount.Focus();
                return;
            }
        }
        catch
        {
            MessageBox.Show("Error validating payment amount.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Combine account no + bank name for bank transfer storage
        string storedCheckNo = (method == "Cheque") ? checkNo : accountNo;

        try
        {
            DatabaseHelper.AddPayment(selectedInvoiceId, amount, method, "Paid", storedCheckNo, bankName);
            DatabaseHelper.UpdateInvoiceStatus(selectedInvoiceId, "Paid");

            LoadPayments(selectedInvoiceId);
            InvoiceSelected();
            ClearFields();
            MessageBox.Show("✅ Payment recorded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error adding payment: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearFields()
    {
        cmbMethod.SelectedIndex = -1;
        txtAmount.Clear();
        txtCheckNo?.Clear();
        txtBankName?.Clear();
        txtAccountNo?.Clear();
        pnlCheckDetails.Visible = false;
        pnlBankDetails.Visible = false;
    }
}