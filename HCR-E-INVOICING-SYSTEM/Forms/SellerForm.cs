using HCR_E_INVOICING_SYSTEM.Data;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

public class SellerForm : Form
{
    private TextBox txtBusinessName, txtNTNCNIC, txtProvince, txtAddress, txtRegistrationType, txtToken, txtSearch, txtInvoiceFooter;
    private PictureBox picLogo;
    private Button btnAdd, btnUpdate, btnDelete, btnClear, btnUploadLogo;
    private DataGridView dgvSellers;
    private int selectedSellerId = -1;
    private byte[] logoBytes;

    public SellerForm()
    {
        
        this.Text = "Seller Management";
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.White;
        this.Load += (s, e) => FormTransitionHelper.AnimateFadeIn(this);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Header
        var header = new Label
        {
            Text = "🛒 Seller Management",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorTranslator.FromHtml("#1D2068"),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Search
        var searchPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
        txtSearch = new TextBox { Font = new Font("Segoe UI", 12), Dock = DockStyle.Fill, Text = "🔍 Search by Name or NTN/CNIC..." };
        var lblSearch = new Label { Text = "Search:", Font = new Font("Segoe UI", 11, FontStyle.Bold), Dock = DockStyle.Left, Width = 70, TextAlign = ContentAlignment.MiddleLeft };
        searchPanel.Controls.Add(txtSearch);
        searchPanel.Controls.Add(lblSearch);
        txtSearch.BringToFront();
        txtSearch.TextChanged += (s, e) => LoadSellers(txtSearch.Text);

        // Input Fields
        var fieldsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(10),
            AutoSize = true
        };

        fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

        var lblBusinessName = MakeLabel("Business Name *");
        var lblNTNCNIC = MakeLabel("NTN / CNIC *");
        var lblProvince = MakeLabel("Province *");
        var lblAddress = MakeLabel("Address (City) *");
        var lblRegistrationType = MakeLabel("Registration Type *");
        var lblToken = MakeLabel("Token *");
        //var lblLogo = MakeLabel("Logo");

        txtBusinessName = MakeTextbox();
        txtNTNCNIC = MakeTextbox();
        txtProvince = MakeTextbox();
        txtAddress = MakeTextbox();
        txtRegistrationType = MakeTextbox();
        txtToken = MakeTextbox();
        txtInvoiceFooter = MakeTextbox();

        picLogo = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill, Height = 100, Width = 150 };
        btnUploadLogo = new Button { Text = "📷 Upload Logo", BackColor = ColorTranslator.FromHtml("#607D8B"), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Dock = DockStyle.Fill };
        btnUploadLogo.Click += (s, e) => UploadLogo();

        var lblInvoiceFooter = MakeLabel("Invoice Footer *");

        fieldsPanel.Controls.Add(lblBusinessName, 0, 0);
        fieldsPanel.Controls.Add(lblNTNCNIC, 1, 0);
        fieldsPanel.Controls.Add(lblProvince, 2, 0);
        fieldsPanel.Controls.Add(txtBusinessName, 0, 1);
        fieldsPanel.Controls.Add(txtNTNCNIC, 1, 1);
        fieldsPanel.Controls.Add(txtProvince, 2, 1);
        fieldsPanel.Controls.Add(lblAddress, 0, 2);
        fieldsPanel.Controls.Add(lblRegistrationType, 1, 2);
        fieldsPanel.Controls.Add(lblToken, 2, 2);
        fieldsPanel.Controls.Add(txtAddress, 0, 3);
        fieldsPanel.Controls.Add(txtRegistrationType, 1, 3);
        fieldsPanel.Controls.Add(txtToken, 2, 3);
        fieldsPanel.Controls.Add(lblInvoiceFooter, 0, 4);
        fieldsPanel.Controls.Add(txtInvoiceFooter, 0, 5);
        fieldsPanel.Controls.Add(btnUploadLogo, 1, 5);
        fieldsPanel.Controls.Add(picLogo, 2, 5);

        // Buttons
        var btnPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            Padding = new Padding(10, 5, 10, 5)
        };
        btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));  // Add
        btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));  // Update
        btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));  // Delete
        btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));  // Clear
        btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Spacer
        btnPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        btnAdd = MakeButton("➕ Add", "#C8A84B");
        btnUpdate = MakeButton("✏️ Update", "#1D2068");
        btnDelete = MakeButton("🗑 Delete", "#8B1A1A");
        btnClear = MakeButton("🧹 Clear", "#4A4A6A");
        btnAdd.Dock = btnUpdate.Dock = btnDelete.Dock = btnClear.Dock = DockStyle.Fill;

        btnPanel.Controls.Add(btnAdd, 0, 0);
        btnPanel.Controls.Add(btnUpdate, 1, 0);
        btnPanel.Controls.Add(btnDelete, 2, 0);
        btnPanel.Controls.Add(btnClear, 3, 0);

        // Grid
        dgvSellers = new DataGridView
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

        // ===== STYLE LIKE INVOICE GRID =====
        dgvSellers.EnableHeadersVisualStyles = false;
        dgvSellers.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#1D2068");
        dgvSellers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvSellers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgvSellers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSellers.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

        dgvSellers.DefaultCellStyle.Font = new Font("Segoe UI", 10);
        dgvSellers.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 60);
        dgvSellers.DefaultCellStyle.BackColor = Color.White;
        dgvSellers.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 250);
        dgvSellers.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#C8A84B");
        dgvSellers.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvSellers.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

        // ===== EVENTS =====
        dgvSellers.CellClick += DgvSellers_CellClick;

        // Optional: double-click to edit fields
        dgvSellers.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0)
                txtBusinessName.Focus(); // Or open an edit form
        };


        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(searchPanel, 0, 1);
        layout.Controls.Add(fieldsPanel, 0, 2);
        layout.Controls.Add(btnPanel, 0, 3);
        layout.Controls.Add(dgvSellers, 0, 4);
        this.Controls.Add(layout);

        btnAdd.Click += (s, e) => AddSeller();
        btnUpdate.Click += (s, e) => UpdateSeller();
        btnDelete.Click += (s, e) => DeleteSeller();
        btnClear.Click += (s, e) => ClearFields();

        LoadSellers();
    }

    private Label MakeLabel(string text)
    {
        return new Label { Text = text, Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
    }
    private TextBox MakeTextbox()
    {
        return new TextBox { Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill };
    }
    private Button MakeButton(string text, string color)
    {
        return new Button { Text = text, Width = 90, Height = 32, BackColor = ColorTranslator.FromHtml(color), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
    }

    private void UploadLogo()
    {
        using (OpenFileDialog ofd = new OpenFileDialog())
        {
            ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                picLogo.Image = Image.FromFile(ofd.FileName);
                logoBytes = File.ReadAllBytes(ofd.FileName);
            }
        }
    }

    private void LoadSellers(string filter = "")
    {
        try
        {
            DataTable dt = DatabaseHelper.GetSellers(filter);
            dgvSellers.DataSource = dt;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading sellers: " + ex.Message);
        }
    }

    private void AddSeller()
    {
        if (!ValidateFields()) return;
        try
        {
            string cleanNtn = txtNTNCNIC.Text.Replace("-", "").Replace(" ", "").Trim();
            string cleanToken = txtToken.Text.Trim();
            DatabaseHelper.AddSeller(txtBusinessName.Text.Trim(), cleanNtn, txtProvince.Text.Trim(), txtAddress.Text.Trim(), txtRegistrationType.Text.Trim(), cleanToken, logoBytes, txtInvoiceFooter.Text.Trim());
            LoadSellers();
            ClearFields();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to add seller: " + ex.Message);
        }
    }

    private void UpdateSeller()
    {
        if (selectedSellerId == -1) return;
        if (!ValidateFields()) return;
        try
        {
            string cleanNtn = txtNTNCNIC.Text.Replace("-", "").Replace(" ", "").Trim();
            string cleanToken = txtToken.Text.Trim();
            DatabaseHelper.UpdateSeller(selectedSellerId, txtBusinessName.Text.Trim(), cleanNtn, txtProvince.Text.Trim(), txtAddress.Text.Trim(), txtRegistrationType.Text.Trim(), cleanToken, logoBytes, txtInvoiceFooter.Text.Trim());
            LoadSellers();
            ClearFields();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to update seller: " + ex.Message);
        }
    }

    private void DeleteSeller()
    {
        if (selectedSellerId == -1) return;
        var confirm = MessageBox.Show("Are you sure you want to delete this seller?", "Confirm Delete", MessageBoxButtons.YesNo);
        if (confirm == DialogResult.Yes)
        {
            try
            {
                DatabaseHelper.DeleteSeller(selectedSellerId);
                LoadSellers();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete seller: " + ex.Message);
            }
        }
    }

    private void DgvSellers_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        DataGridViewRow row = dgvSellers.Rows[e.RowIndex];
        selectedSellerId = Convert.ToInt32(row.Cells["sellerId"].Value);
        txtBusinessName.Text = row.Cells["sellerBusinessName"].Value.ToString();
        txtNTNCNIC.Text = row.Cells["sellerNTNCNIC"].Value.ToString();
        txtProvince.Text = row.Cells["sellerProvince"].Value.ToString();
        txtAddress.Text = row.Cells["sellerAddress"].Value.ToString();
        txtRegistrationType.Text = row.Cells["registrationType"].Value.ToString();
        txtToken.Text = row.Cells["token"].Value.ToString();
        txtInvoiceFooter.Text = row.Cells["invoiceFooter"].Value?.ToString();

        if (row.Cells["logoPath"].Value != DBNull.Value)
        {
            byte[] imgData = (byte[])row.Cells["logoPath"].Value;
            using (MemoryStream ms = new MemoryStream(imgData))
                picLogo.Image = Image.FromStream(ms);
            logoBytes = imgData;
        }
        else
        {
            picLogo.Image = null;
            logoBytes = null;
        }
    }

    private void ClearFields()
    {
        selectedSellerId = -1;
        txtBusinessName.Clear();
        txtNTNCNIC.Clear();
        txtProvince.Clear();
        txtAddress.Clear();
        txtRegistrationType.Clear();
        txtToken.Clear();
        txtInvoiceFooter.Clear();
        picLogo.Image = null;
        logoBytes = null;
    }

    private bool ValidateFields()
    {
        if (string.IsNullOrWhiteSpace(txtBusinessName.Text)) { MessageBox.Show("Business Name is required."); return false; }
        if (string.IsNullOrWhiteSpace(txtNTNCNIC.Text)) { MessageBox.Show("NTN/CNIC is required."); return false; }
        
        string cleanNtn = txtNTNCNIC.Text.Replace("-", "").Replace(" ", "").Trim();
        if (cleanNtn.Length != 7 && cleanNtn.Length != 13)
        {
            MessageBox.Show("Seller NTN must be exactly 7 digits, or CNIC must be exactly 13 digits (numeric only with no spaces or hyphens).");
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtProvince.Text)) { MessageBox.Show("Province is required."); return false; }
        if (string.IsNullOrWhiteSpace(txtAddress.Text)) { MessageBox.Show("Address is required."); return false; }
        if (string.IsNullOrWhiteSpace(txtRegistrationType.Text)) { MessageBox.Show("Registration Type is required."); return false; }
        if (string.IsNullOrWhiteSpace(txtToken.Text)) { MessageBox.Show("Token is required."); return false; }
        if (string.IsNullOrWhiteSpace(txtInvoiceFooter.Text)) { MessageBox.Show("Invoice Footer is required."); return false; }
        return true;
    }
}