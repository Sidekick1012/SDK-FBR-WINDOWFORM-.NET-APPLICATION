using SDK_E_INVOICING_SYSTEM.Data;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

public class CustomerForm : Form
{
    private TextBox txtBusinessName, txtNTNCNIC, txtProvince, txtAddress, txtRegistrationType, txtSearch;
    private Button btnAdd, btnUpdate, btnDelete, btnClear;
    private DataGridView dgvCustomers;
    private int selectedCustomerId = -1;

    public CustomerForm()
    {
       
        this.Text = "Customer Management";
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

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));    // Header
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));    // Search
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Input Fields
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));    // Buttons
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // DataGridView


        // ===== HEADER =====
        var header = new Label
        {
            Text = "👥 Customer Management",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorTranslator.FromHtml("#1D2068"),  // HCR navy header
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ===== SEARCH =====
        var searchPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
        txtSearch = new TextBox
        {
            Font = new Font("Segoe UI", 12),
            Dock = DockStyle.Fill,
            Text = "🔍 Search by Name Or NTN/CNIC..."
        };
        var lblSearch = new Label
        {
            Text = "Search:",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Dock = DockStyle.Left,
            Width = 70,
            TextAlign = ContentAlignment.MiddleLeft
        };
        searchPanel.Controls.Add(txtSearch);
        searchPanel.Controls.Add(lblSearch);
        txtSearch.BringToFront();
        txtSearch.TextChanged += (s, e) => LoadCustomers(txtSearch.Text);

        // ===== INPUT FIELDS =====
        var fieldsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
            Padding = new Padding(10),
            AutoSize = true
        };

        fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

        fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        var lblBusinessName = new Label { Text = "Business Name *", Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        var lblNTNCNIC = new Label { Text = "NTN / CNIC *", Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        var lblProvince = new Label { Text = "Province *", Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        var lblAddress = new Label { Text = "Address (City) *", Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        var lblRegistrationType = new Label { Text = "Registration Type (Reg Or NotReg)*", Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };

        txtBusinessName = new TextBox { Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill };
        txtNTNCNIC = new TextBox { Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill };
        txtProvince = new TextBox { Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill };
        txtAddress = new TextBox { Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill };
        txtRegistrationType = new TextBox { Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill };

        fieldsPanel.Controls.Add(lblBusinessName, 0, 0);
        fieldsPanel.Controls.Add(lblNTNCNIC, 1, 0);
        fieldsPanel.Controls.Add(lblProvince, 2, 0);
        fieldsPanel.Controls.Add(txtBusinessName, 0, 1);
        fieldsPanel.Controls.Add(txtNTNCNIC, 1, 1);
        fieldsPanel.Controls.Add(txtProvince, 2, 1);

        fieldsPanel.Controls.Add(lblAddress, 0, 2);
        fieldsPanel.Controls.Add(lblRegistrationType, 1, 2);
        fieldsPanel.Controls.Add(txtAddress, 0, 3);
        fieldsPanel.Controls.Add(txtRegistrationType, 1, 3);

        // ===== BUTTONS =====
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10),
            AutoSize = true
        };

        btnAdd = MakeButton("➕ Add", "#C8A84B");
        btnUpdate = MakeButton("✏️ Update", "#1D2068");
        btnDelete = MakeButton("🗑 Delete", "#8B1A1A");
        btnClear = MakeButton("🧹 Clear", "#4A4A6A");

        btnPanel.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnClear });

        // ===== DATAGRIDVIEW =====
        dgvCustomers = new DataGridView
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
        dgvCustomers.EnableHeadersVisualStyles = false;
        dgvCustomers.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#1D2068");
        dgvCustomers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvCustomers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgvCustomers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvCustomers.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

        dgvCustomers.DefaultCellStyle.Font = new Font("Segoe UI", 10);
        dgvCustomers.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 60);
        dgvCustomers.DefaultCellStyle.BackColor = Color.White;
        dgvCustomers.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 250);
        dgvCustomers.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#C8A84B");
        dgvCustomers.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvCustomers.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

        // ===== EVENTS =====
        dgvCustomers.CellClick += DgvCustomers_CellClick;

        // Optional: double-click to edit fields
        dgvCustomers.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0)
                txtBusinessName.Focus(); // Or open an edit form
        };


        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(searchPanel, 0, 1);
        layout.Controls.Add(fieldsPanel, 0, 2);
        layout.Controls.Add(btnPanel, 0, 3);
        layout.Controls.Add(dgvCustomers, 0, 4);

        this.Controls.Add(layout);

        btnAdd.Click += (s, e) => AddCustomer();
        btnUpdate.Click += (s, e) => UpdateCustomer();
        btnDelete.Click += (s, e) => DeleteCustomer();
        btnClear.Click += (s, e) => ClearFields();

        // === Auto-generate columns manually ===
        InitializeGrid();

        LoadCustomers();
    }

    private Button MakeButton(string text, string color)
    {
        return new Button
        {
            Text = text,
            Width = 90,
            Height = 32,
            BackColor = ColorTranslator.FromHtml(color),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(3)
        };
    }

    private void InitializeGrid()
    {
        dgvCustomers.Columns.Clear();

        dgvCustomers.Columns.Add("customerId", "ID");
        dgvCustomers.Columns.Add("customerBusinessName", "Business Name");
        dgvCustomers.Columns.Add("customerNTNCNIC", "NTN / CNIC");
        dgvCustomers.Columns.Add("customerProvince", "Province");
        dgvCustomers.Columns.Add("customerAddress", "Address");
        dgvCustomers.Columns.Add("registrationType", "Registration Type");
    }

    private void LoadCustomers(string filter = "")
    {
        try
        {
            DataTable dt = DatabaseHelper.GetCustomers(filter);
            dgvCustomers.Rows.Clear();

            foreach (DataRow dr in dt.Rows)
            {
                dgvCustomers.Rows.Add(
                    dr["customerId"],
                    dr["customerBusinessName"],
                    dr["customerNTNCNIC"],
                    dr["customerProvince"],
                    dr["customerAddress"],
                    dr["registrationType"]
                );
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading customers: " + ex.Message);
        }
    }

    private void AddCustomer()
    {
        if (!ValidateFields())
            return;

        try
        {
            DatabaseHelper.AddCustomer(
                txtBusinessName.Text,
                txtNTNCNIC.Text,
                txtProvince.Text,
                txtAddress.Text,
                txtRegistrationType.Text
            );
            LoadCustomers();
            ClearFields();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to add customer: " + ex.Message);
        }
    }

    private void UpdateCustomer()
    {
        if (selectedCustomerId == -1)
            return;

        if (!ValidateFields())
            return;

        try
        {
            DatabaseHelper.UpdateCustomer(
                selectedCustomerId,
                txtBusinessName.Text,
                txtNTNCNIC.Text,
                txtProvince.Text,
                txtAddress.Text,
                txtRegistrationType.Text
            );
            LoadCustomers();
            ClearFields();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to update customer: " + ex.Message);
        }
    }

    private void DeleteCustomer()
    {
        if (selectedCustomerId == -1) return;

        var confirm = MessageBox.Show("Are you sure you want to delete this customer?", "Confirm Delete", MessageBoxButtons.YesNo);
        if (confirm == DialogResult.Yes)
        {
            try
            {
                DatabaseHelper.DeleteCustomer(selectedCustomerId);
                LoadCustomers();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete customer: " + ex.Message);
            }
        }
    }

    private void ClearFields()
    {
        selectedCustomerId = -1;
        txtBusinessName.Clear();
        txtNTNCNIC.Clear();
        txtProvince.Clear();
        txtAddress.Clear();
        txtRegistrationType.Clear();
    }

    private void DgvCustomers_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        try
        {
            if (e.RowIndex >= 0 && dgvCustomers.Rows[e.RowIndex].Cells["customerId"].Value != null)
            {
                DataGridViewRow row = dgvCustomers.Rows[e.RowIndex];
                selectedCustomerId = Convert.ToInt32(row.Cells["customerId"].Value);

                txtBusinessName.Text = row.Cells["customerBusinessName"]?.Value?.ToString() ?? "";
                txtNTNCNIC.Text = row.Cells["customerNTNCNIC"]?.Value?.ToString() ?? "";
                txtProvince.Text = row.Cells["customerProvince"]?.Value?.ToString() ?? "";
                txtAddress.Text = row.Cells["customerAddress"]?.Value?.ToString() ?? "";
                txtRegistrationType.Text = row.Cells["registrationType"]?.Value?.ToString() ?? "";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error selecting customer: " + ex.Message);
        }
    }

    private bool ValidateFields()
    {
        if (string.IsNullOrWhiteSpace(txtBusinessName.Text))
        {
            MessageBox.Show("Business Name is required.");
            txtBusinessName.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtNTNCNIC.Text))
        {
            MessageBox.Show("NTN / CNIC is required.");
            txtNTNCNIC.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtProvince.Text))
        {
            MessageBox.Show("Province is required.");
            txtProvince.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtAddress.Text))
        {
            MessageBox.Show("Address (City) is required.");
            txtAddress.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtRegistrationType.Text))
        {
            MessageBox.Show("Registration Type is required.");
            txtRegistrationType.Focus();
            return false;
        }

        return true;
    }
}