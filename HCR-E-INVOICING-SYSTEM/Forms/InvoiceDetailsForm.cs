using HCR_E_INVOICING_SYSTEM.Data;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HCR_E_INVOICING_SYSTEM
{
    public partial class InvoiceUpdateForm : Form
    {
        private int invoiceId;
        private DataTable sellersData, customersData, productsData;
        private DataGridView dgvItems;

        // Seller Controls
        private ComboBox cmbSeller;
        private TextBox txtSellerNTN, txtSellerProvince, txtSellerAddress;

        // Buyer Controls
        private ComboBox cmbBuyer;
        private TextBox txtBuyerNTN, txtBuyerProvince, txtBuyerAddress, txtBuyerRegType;

        // Product Controls
        private ComboBox cmbProduct, cmbScenario, cmbSaleType;
        private TextBox txtProductCode, txtProductDescription, txtProductRate, txtProductUOM;
        private TextBox txtQuantity, txtUnitPrice, txtDiscount, txtTotalValue, txtValueExclGST, txtSalesTaxAmount, txtFurtherTaxAmount, txtItemNotes;

        // Other Controls
        private Button btnAddItem, btnRemoveItem, btnUpdate;
        private TextBox txtInvoiceNumber, txtFBRInvoiceNumber;
        private DateTimePicker dtpInvoiceDate;
        private ComboBox cmbStatus;
        private Label lblGrandTotal;

        // Store original IDs
        private int originalSellerId, originalCustomerId;

        public InvoiceUpdateForm(int invoiceId)
        {
            this.invoiceId = invoiceId;
            InitializeComponent();
            LoadData();
            LoadInvoiceData();
            this.Size = new Size(1300, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeComponent()
        {
            this.Text = "Update Invoice - HCR";
            this.BackColor = Color.WhiteSmoke;
            this.Padding = new Padding(10);

            // Main outer container
            TableLayoutPanel outerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10),
                BackColor = Color.WhiteSmoke
            };
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200)); // Row for Top Info Cards
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Row for Item Entry and Grid
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Row for Action Buttons
            this.Controls.Add(outerLayout);

            // Top Info Cards Layout
            TableLayoutPanel topCardsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            topCardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            topCardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            topCardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            outerLayout.Controls.Add(topCardsLayout, 0, 0);

            // Bottom Section Layout (Item Input + Grid)
            TableLayoutPanel bottomSectionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 0)
            };
            bottomSectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340)); // Fixed width for item input
            bottomSectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Remaining width for grid
            outerLayout.Controls.Add(bottomSectionLayout, 0, 1);

            // Create panels in the same style as GenerateInvoiceForm
            CreateSellerPanel(topCardsLayout);
            CreateBuyerPanel(topCardsLayout);
            CreateProductPanel(topCardsLayout);
            CreateInvoiceInfoPanel(bottomSectionLayout);
            CreateItemsGrid(bottomSectionLayout);

            // Action Buttons Panel
            Panel actionButtonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke,
                Margin = new Padding(0)
            };
            outerLayout.Controls.Add(actionButtonPanel, 0, 2);
            CreateButtonPanel(actionButtonPanel);
        }

        private void CreateSellerPanel(TableLayoutPanel parentLayout)
        {
            var sellerPanel = CreateGroupBox("🏢 Seller Information", new Size(400, 180));
            sellerPanel.Dock = DockStyle.Fill;

            cmbSeller = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
            txtSellerNTN = CreateTextBox("", true);
            txtSellerProvince = CreateTextBox("", true);
            txtSellerAddress = CreateTextBox("", true);

            AddLabeledControls(sellerPanel,
                ("Select Seller:", cmbSeller),
                ("NTN:", txtSellerNTN),
                ("Province:", txtSellerProvince),
                ("Address:", txtSellerAddress));

            parentLayout.Controls.Add(sellerPanel, 0, 0);
        }

        private void CreateBuyerPanel(TableLayoutPanel parentLayout)
        {
            var buyerPanel = CreateGroupBox("👤 Buyer Information", new Size(400, 180));
            buyerPanel.Dock = DockStyle.Fill;

            cmbBuyer = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
            txtBuyerNTN = CreateTextBox("", true);
            txtBuyerProvince = CreateTextBox("", true);
            txtBuyerAddress = CreateTextBox("", true);
            txtBuyerRegType = CreateTextBox("", true);

            AddLabeledControls(buyerPanel,
                ("Business:", cmbBuyer),
                ("NTN/CNIC:", txtBuyerNTN),
                ("Province:", txtBuyerProvince),
                ("Address:", txtBuyerAddress),
                ("Reg Type:", txtBuyerRegType));

            parentLayout.Controls.Add(buyerPanel, 1, 0);
        }

        private void CreateProductPanel(TableLayoutPanel parentLayout)
        {
            var productPanel = CreateGroupBox("📦 Product Information", new Size(400, 150));
            productPanel.Dock = DockStyle.Fill;

            cmbProduct = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
            txtProductCode = CreateTextBox("", true);
            txtProductDescription = CreateTextBox("", true);
            txtProductRate = CreateTextBox("", true);
            txtProductUOM = CreateTextBox("", true);

            AddLabeledControls(productPanel,
                ("HS Code:", cmbProduct),
                ("Description:", txtProductDescription),
                ("Tax Rate:", txtProductRate),
                ("UOM:", txtProductUOM));

            parentLayout.Controls.Add(productPanel, 2, 0);
        }

        private void CreateInvoiceInfoPanel(TableLayoutPanel parentLayout)
        {
            var infoPanel = CreateGroupBox("🧾 Invoice Item Details", new Size(400, 390));
            infoPanel.Dock = DockStyle.Fill;

            // Invoice Basic Info
            txtInvoiceNumber = CreateTextBox("", true);
            txtFBRInvoiceNumber = CreateTextBox("");
            dtpInvoiceDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 200 };
            cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
            cmbStatus.Items.AddRange(new[] { "Draft", "Final", "Cancelled" });

            // Scenario Dropdown
            cmbScenario = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
            cmbScenario.Items.AddRange(new[] {
                "Select", "SN001", "SN002", "SN003", "SN004", "SN005", "SN006", "SN007", "SN008", "SN009", "SN010",
                "SN011", "SN012", "SN013", "SN014", "SN015", "SN016", "SN017", "SN018", "SN019", "SN020",
                "SN021", "SN022", "SN023", "SN024", "SN025", "SN026", "SN027", "SN028"
            });
            cmbScenario.SelectedIndex = 0;

            // Quantity and Pricing
            txtQuantity = CreateTextBox("1");
            txtUnitPrice = CreateTextBox("0.00");
            txtDiscount = CreateTextBox("0.00");
            txtTotalValue = CreateTextBox("", true);
            txtValueExclGST = CreateTextBox("", true);
            txtSalesTaxAmount = CreateTextBox("", true);
            txtFurtherTaxAmount = CreateTextBox("", true);

            // Sale Type Dropdown
            cmbSaleType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
            cmbSaleType.Items.AddRange(new[] {
                "Select", "Goods at standard rate (default)", "Goods at Reduced Rate", "Goods at Zero-rate",
                "Petroleum Products", "Electricity Supply to Retailers", "SIM", "Gas to CNG stations",
                "Mobile Phones", "Processing/Conversion of Goods", "3rd Schedule Goods", "Goods (FED in ST Mode)",
                "Services (FED in ST Mode)", "Services", "Exempt goods", "Ship breaking"
            });
            cmbSaleType.SelectedIndex = 0;

            // Notes
            txtItemNotes = CreateTextBox();
            txtItemNotes.MaxLength = 20;

            // Add numeric validation
            txtQuantity.KeyPress += NumericOnly_KeyPress;
            txtUnitPrice.KeyPress += DecimalTwoPlaces_KeyPress;
            txtDiscount.KeyPress += DecimalTwoPlaces_KeyPress;
            txtItemNotes.KeyPress += Notes_KeyPress;

            // Add calculation events
            txtQuantity.TextChanged += RecalculateTotalValue;
            txtUnitPrice.TextChanged += RecalculateTotalValue;
            txtDiscount.TextChanged += RecalculateTotalValue;
            txtProductRate.TextChanged += RecalculateTotalValue;

            AddLabeledControls(infoPanel,
                ("Invoice No:", txtInvoiceNumber),
                ("FBR Invoice No:", txtFBRInvoiceNumber),
                ("Invoice Date:", dtpInvoiceDate),
                ("Status:", cmbStatus),
                ("Scenario:", cmbScenario),
                ("Quantity:", txtQuantity),
                ("Unit Price:", txtUnitPrice),
                ("Discount:", txtDiscount),
                ("Total Value:", txtTotalValue),
                ("Excl GST:", txtValueExclGST),
                ("Sales Tax:", txtSalesTaxAmount),
                ("Further Tax:", txtFurtherTaxAmount),
                ("Sale Type:", cmbSaleType),
                ("Notes:", txtItemNotes));

            // Add/Remove buttons
            FlowLayoutPanel itemButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40 };
            btnAddItem = CreateButton("➕ Add Item", Color.SeaGreen);
            btnRemoveItem = CreateButton("🗑️ Delete Item", Color.Firebrick);
            btnAddItem.Click += BtnAddItem_Click;
            btnRemoveItem.Click += BtnRemoveItem_Click;
            itemButtons.Controls.AddRange(new Control[] { btnAddItem, btnRemoveItem });
            infoPanel.Controls.Add(itemButtons);

            parentLayout.Controls.Add(infoPanel, 0, 0);
        }

        private void CreateItemsGrid(TableLayoutPanel parentLayout)
        {
            var gridPanel = CreateGroupBox("📋 Invoice Items", new Size(800, 400));
            gridPanel.Dock = DockStyle.Fill;

            dgvItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                RowHeadersVisible = false
            };

            // Add columns matching GenerateInvoiceForm style
            dgvItems.Columns.Add("ScenarioID", "Scenario ID");
            dgvItems.Columns.Add("HSCode", "HS Code");
            dgvItems.Columns.Add("Product_Desc", "Product Description");
            dgvItems.Columns.Add("UOM", "Unit of Measure");
            dgvItems.Columns.Add("Quantity", "Quantity");
            dgvItems.Columns.Add("UnitPrice", "Unit Price");
            dgvItems.Columns.Add("Rate", "Rate (%)");
            dgvItems.Columns.Add("TotalValue", "Total Value");
            dgvItems.Columns.Add("Discount", "Discount");
            dgvItems.Columns.Add("ValueExclGST", "Value Excl. GST");
            dgvItems.Columns.Add("SalesTaxAmount", "Sales Tax Amount");
            dgvItems.Columns.Add("FurtherTaxAmount", "Further Tax Amount");
            dgvItems.Columns.Add("saleType", "Sale Type");
            dgvItems.Columns.Add("Notes", "Notes");
            dgvItems.Columns["Notes"].Visible = false;

            // Style the grid like GenerateInvoiceForm
            dgvItems.BackgroundColor = Color.White;
            dgvItems.DefaultCellStyle.BackColor = Color.ForestGreen;
            dgvItems.DefaultCellStyle.ForeColor = Color.White;
            dgvItems.DefaultCellStyle.SelectionBackColor = Color.LightSteelBlue;
            dgvItems.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvItems.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
            dgvItems.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;

            // Grand Total Label
            lblGrandTotal = new Label
            {
                Text = "Grand Total: 0.00",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleRight
            };

            gridPanel.Controls.Add(dgvItems);
            gridPanel.Controls.Add(lblGrandTotal);

            parentLayout.Controls.Add(gridPanel, 1, 0);

            // Events
            dgvItems.CellDoubleClick += DgvItems_CellDoubleClick;
            dgvItems.SelectionChanged += (s, e) =>
            {
                btnRemoveItem.Enabled = dgvItems.SelectedRows.Count > 0;
            };
        }

        private void CreateButtonPanel(Panel parentPanel)
        {
            btnUpdate = CreateButton("💾 Update Invoice", Color.DarkSlateGray);
            btnUpdate.Click += BtnUpdate_Click;

            var btnCancel = CreateButton("❌ Cancel", Color.Firebrick);
            btnCancel.Click += (s, e) => this.Close();

            parentPanel.Controls.Add(btnUpdate);
            parentPanel.Controls.Add(btnCancel);
            btnUpdate.Location = new Point(10, 10);
            btnCancel.Location = new Point(170, 10);
        }

        // UI Helper Methods
        private GroupBox CreateGroupBox(string title, Size size) => new GroupBox
        {
            Text = title,
            Size = size,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            Padding = new Padding(8),
            Margin = new Padding(8),
        };

        private TextBox CreateTextBox(string text = "", bool readOnly = false) => new TextBox
        {
            Text = text,
            ReadOnly = readOnly,
            Width = 200
        };

        private Button CreateButton(string text, Color color) => new Button
        {
            Text = text,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Width = 140,
            Height = 35,
            Margin = new Padding(5),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        private void AddLabeledControls(GroupBox gb, params (string, Control)[] pairs)
        {
            // Create a scrollable panel to hold the TableLayoutPanel
            Panel scrollPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            gb.Controls.Add(scrollPanel);
            scrollPanel.BringToFront(); // Dock last, filling remaining space above any bottom buttons

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Padding = new Padding(0, 0, 15, 0) // Leave space for scrollbar
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            foreach (var (labelText, control) in pairs)
            {
                tlp.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(2) });
                control.Dock = DockStyle.Fill;
                tlp.Controls.Add(control);
            }
            scrollPanel.Controls.Add(tlp);
        }

        // Data Loading Methods
        private void LoadData()
        {
            try
            {
                // Load sellers
                sellersData = DatabaseHelper.GetSellers();
                cmbSeller.DisplayMember = "sellerBusinessName";
                cmbSeller.ValueMember = "sellerId";
                cmbSeller.DataSource = sellersData.Copy();

                // Load customers
                customersData = DatabaseHelper.GetCustomers();
                cmbBuyer.DisplayMember = "customerBusinessName";
                cmbBuyer.ValueMember = "customerId";
                cmbBuyer.DataSource = customersData.Copy();

                // Load products
                productsData = DatabaseHelper.GetProducts();
                cmbProduct.DisplayMember = "hsCode";
                cmbProduct.ValueMember = "productId";
                cmbProduct.DataSource = productsData.Copy();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadInvoiceData()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Load invoice header with seller and buyer details
                    string sql = @"
                SELECT 
                    i.*,
                    s.sellerId, s.sellerNTNCNIC, s.sellerProvince, s.sellerAddress,
                    c.customerId, c.customerNTNCNIC, c.customerProvince, c.customerAddress, c.registrationType
                FROM Invoices i
                LEFT JOIN Sellers s ON i.sellerId = s.sellerId
                LEFT JOIN Customers c ON i.customerId = c.customerId
                WHERE i.invoiceId = @invoiceId";

                    using (var cmd = new System.Data.SQLite.SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@invoiceId", invoiceId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Basic invoice info
                                txtInvoiceNumber.Text = reader["invoiceNumber"].ToString();
                                txtFBRInvoiceNumber.Text = reader["fbrInvoiceNumber"]?.ToString() ?? "";

                                if (reader["invoiceDate"] != DBNull.Value)
                                    dtpInvoiceDate.Value = Convert.ToDateTime(reader["invoiceDate"]);

                                cmbStatus.Text = reader["status"]?.ToString() ?? "Draft";

                                // Store original IDs
                                originalSellerId = SafeConvertToInt(reader["sellerId"]);
                                originalCustomerId = SafeConvertToInt(reader["customerId"]);

                                // Set seller and populate fields
                                if (originalSellerId > 0)
                                {
                                    cmbSeller.SelectedValue = originalSellerId;
                                    txtSellerNTN.Text = reader["sellerNTNCNIC"]?.ToString() ?? "";
                                    txtSellerProvince.Text = reader["sellerProvince"]?.ToString() ?? "";
                                    txtSellerAddress.Text = reader["sellerAddress"]?.ToString() ?? "";
                                }

                                // Set buyer and populate fields
                                if (originalCustomerId > 0)
                                {
                                    cmbBuyer.SelectedValue = originalCustomerId;
                                    txtBuyerNTN.Text = reader["customerNTNCNIC"]?.ToString() ?? "";
                                    txtBuyerProvince.Text = reader["customerProvince"]?.ToString() ?? "";
                                    txtBuyerAddress.Text = reader["customerAddress"]?.ToString() ?? "";
                                    txtBuyerRegType.Text = reader["registrationType"]?.ToString() ?? "";
                                }
                            }
                        }
                    }

                    // Load invoice items
                    string itemsSql = @"
                SELECT 
                    ii.*,
                    p.hsCode, p.productDescription, p.rate, p.uoM
                FROM InvoiceItems ii
                LEFT JOIN Products p ON ii.productId = p.productId
                WHERE ii.invoiceId = @invoiceId";

                    using (var cmd = new System.Data.SQLite.SQLiteCommand(itemsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@invoiceId", invoiceId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            dgvItems.Rows.Clear();
                            while (reader.Read())
                            {
                                dgvItems.Rows.Add(
                                    reader["saleType"]?.ToString() ?? "", // Scenario ID (using saleType as placeholder)
                                    reader["hsCode"]?.ToString() ?? "",
                                    reader["productDescription"]?.ToString() ?? "",
                                    reader["uoM"]?.ToString() ?? "",
                                    reader["quantity"] != DBNull.Value ? Convert.ToDecimal(reader["quantity"]) : 0m,
                                    reader["unitPrice"] != DBNull.Value ? Convert.ToDecimal(reader["unitPrice"]) : 0m,
                                    reader["rate"]?.ToString() ?? "",
                                    reader["totalValues"] != DBNull.Value ? Convert.ToDecimal(reader["totalValues"]) : 0m,
                                    reader["discount"] != DBNull.Value ? Convert.ToDecimal(reader["discount"]) : 0m,
                                    reader["valueSalesExcludingST"] != DBNull.Value ? Convert.ToDecimal(reader["valueSalesExcludingST"]) : 0m,
                                    reader["salesTaxApplicable"] != DBNull.Value ? Convert.ToDecimal(reader["salesTaxApplicable"]) : 0m,
                                    reader["furtherTax"] != DBNull.Value ? Convert.ToDecimal(reader["furtherTax"]) : 0m,
                                    reader["saleType"]?.ToString() ?? "",
                                    reader["description"]?.ToString() ?? "" // Using description as Notes
                                );
                            }
                        }
                    }

                    UpdateGrandTotal();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading invoice data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event Handlers
        private void CmbSeller_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSeller.SelectedValue != null && cmbSeller.SelectedValue != DBNull.Value)
            {
                try
                {
                    var sellerId = SafeConvertToInt(cmbSeller.SelectedValue);
                    var sellerRow = sellersData.AsEnumerable()
                        .FirstOrDefault(row => SafeConvertToInt(row["sellerId"]) == sellerId);

                    if (sellerRow != null)
                    {
                        txtSellerNTN.Text = sellerRow.Field<string>("sellerNTNCNIC") ?? "";
                        txtSellerProvince.Text = sellerRow.Field<string>("sellerProvince") ?? "";
                        txtSellerAddress.Text = sellerRow.Field<string>("sellerAddress") ?? "";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading seller details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CmbBuyer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbBuyer.SelectedValue != null && cmbBuyer.SelectedValue != DBNull.Value)
            {
                try
                {
                    var buyerId = SafeConvertToInt(cmbBuyer.SelectedValue);
                    var buyerRow = customersData.AsEnumerable()
                        .FirstOrDefault(row => SafeConvertToInt(row["customerId"]) == buyerId);

                    if (buyerRow != null)
                    {
                        txtBuyerNTN.Text = buyerRow.Field<string>("customerNTNCNIC") ?? "";
                        txtBuyerProvince.Text = buyerRow.Field<string>("customerProvince") ?? "";
                        txtBuyerAddress.Text = buyerRow.Field<string>("customerAddress") ?? "";
                        txtBuyerRegType.Text = buyerRow.Field<string>("registrationType") ?? "";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading buyer details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CmbProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbProduct.SelectedValue != null && cmbProduct.SelectedValue != DBNull.Value)
            {
                try
                {
                    var productId = SafeConvertToInt(cmbProduct.SelectedValue);
                    var productRow = productsData.AsEnumerable()
                        .FirstOrDefault(row => SafeConvertToInt(row["productId"]) == productId);

                    if (productRow != null)
                    {
                        txtProductCode.Text = productRow.Field<string>("hsCode") ?? "";
                        txtProductDescription.Text = productRow.Field<string>("productDescription") ?? "";
                        txtProductRate.Text = productRow.Field<string>("rate") ?? "";
                        txtProductUOM.Text = productRow.Field<string>("uoM") ?? "";

                        // Auto-fill unit price if available
                        if (decimal.TryParse(productRow.Field<string>("rate")?.Replace("%", ""), out decimal rate))
                        {
                            txtUnitPrice.Text = rate.ToString("N2");
                        }
                        else
                        {
                            txtUnitPrice.Text = "0.00";
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading product details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private int editingRowIndex = -1;

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtQuantity.Text) || string.IsNullOrWhiteSpace(txtProductRate.Text))
            {
                MessageBox.Show("⚠️ Quantity and Rate required!");
                return;
            }

            if (editingRowIndex >= 0)
            {
                FillRow(dgvItems.Rows[editingRowIndex]);
                editingRowIndex = -1;
                btnAddItem.Text = "➕ Add Item";
            }
            else
            {
                foreach (DataGridViewRow row in dgvItems.Rows)
                {
                    if (row.Cells["HSCode"].Value?.ToString() == cmbProduct.Text &&
                        row.Cells["ScenarioID"].Value?.ToString() == cmbScenario.Text)
                    {
                        MessageBox.Show("⚠️ Duplicate entry!");
                        return;
                    }
                }
                int idx = dgvItems.Rows.Add();
                FillRow(dgvItems.Rows[idx]);
            }

            ClearItemFields();
            UpdateGrandTotal();
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (dgvItems.SelectedRows.Count > 0)
            {
                dgvItems.Rows.RemoveAt(dgvItems.SelectedRows[0].Index);
                editingRowIndex = -1;
                btnAddItem.Text = "➕ Add Item";
                ClearItemFields();
                UpdateGrandTotal();
            }
        }

        private void DgvItems_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvItems.Rows[e.RowIndex];
                editingRowIndex = e.RowIndex;

                cmbScenario.Text = row.Cells["ScenarioID"].Value?.ToString();
                cmbProduct.Text = row.Cells["HSCode"].Value?.ToString();
                txtQuantity.Text = row.Cells["Quantity"].Value?.ToString();
                txtUnitPrice.Text = row.Cells["UnitPrice"].Value?.ToString();
                txtDiscount.Text = row.Cells["Discount"].Value?.ToString();
                txtProductRate.Text = row.Cells["Rate"].Value?.ToString();
                txtTotalValue.Text = row.Cells["TotalValue"].Value?.ToString();
                txtValueExclGST.Text = row.Cells["ValueExclGST"].Value?.ToString();
                txtSalesTaxAmount.Text = row.Cells["SalesTaxAmount"].Value?.ToString();
                txtFurtherTaxAmount.Text = row.Cells["FurtherTaxAmount"].Value?.ToString();
                cmbSaleType.Text = row.Cells["saleType"].Value?.ToString();
                txtItemNotes.Text = row.Cells["Notes"].Value?.ToString();

                btnAddItem.Text = "✏️ Update Item";
            }
        }

        private void FillRow(DataGridViewRow row)
        {
            row.Cells["ScenarioID"].Value = cmbScenario.Text;
            row.Cells["HSCode"].Value = cmbProduct.Text;
            row.Cells["Product_Desc"].Value = txtProductDescription.Text;
            row.Cells["UOM"].Value = txtProductUOM.Text;
            row.Cells["Quantity"].Value = txtQuantity.Text;
            row.Cells["UnitPrice"].Value = txtUnitPrice.Text;
            row.Cells["Rate"].Value = txtProductRate.Text;
            row.Cells["TotalValue"].Value = txtTotalValue.Text;
            row.Cells["Discount"].Value = txtDiscount.Text;
            row.Cells["ValueExclGST"].Value = txtValueExclGST.Text;
            row.Cells["SalesTaxAmount"].Value = txtSalesTaxAmount.Text;
            row.Cells["FurtherTaxAmount"].Value = txtFurtherTaxAmount.Text;
            row.Cells["saleType"].Value = cmbSaleType.Text;
            row.Cells["Notes"].Value = txtItemNotes.Text;
        }

        private void ClearItemFields()
        {
            txtQuantity.Clear();
            txtUnitPrice.Clear();
            txtDiscount.Clear();
            txtTotalValue.Clear();
            txtValueExclGST.Clear();
            txtSalesTaxAmount.Clear();
            txtFurtherTaxAmount.Clear();
            txtItemNotes.Clear();
            editingRowIndex = -1;
            btnAddItem.Text = "➕ Add Item";
        }

        private void UpdateGrandTotal()
        {
            decimal subtotal = 0;
            decimal salesTax = 0;
            decimal furtherTax = 0;

            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                if (row.IsNewRow) continue;

                subtotal += Convert.ToDecimal(row.Cells["TotalValue"].Value ?? 0);
                salesTax += Convert.ToDecimal(row.Cells["SalesTaxAmount"].Value ?? 0);
                furtherTax += Convert.ToDecimal(row.Cells["FurtherTaxAmount"].Value ?? 0);
            }

            lblGrandTotal.Text = $"Grand Total: {(subtotal + salesTax + furtherTax):N2}";
        }

        // Calculation Methods
        private void RecalculateTotalValue(object sender, EventArgs e)
        {
            if (decimal.TryParse(txtQuantity.Text, out decimal qty) &&
                decimal.TryParse(txtUnitPrice.Text, out decimal unitPrice) &&
                decimal.TryParse(txtProductRate.Text.Replace("%", ""), out decimal rate))
            {
                decimal.TryParse(txtDiscount.Text, out decimal discount);
                decimal total = qty * unitPrice;
                txtTotalValue.Text = total.ToString("N2");

                decimal discountedTotal = total - discount;
                if (discountedTotal < 0) discountedTotal = 0;

                decimal valueExclGST = discountedTotal / (1 + rate / 100);
                decimal salesTax = discountedTotal - valueExclGST;

                txtValueExclGST.Text = valueExclGST.ToString("N2");
                txtSalesTaxAmount.Text = salesTax.ToString("N2");
            }
            else
            {
                txtTotalValue.Text = "0.00";
                txtValueExclGST.Text = "0.00";
                txtSalesTaxAmount.Text = "0.00";
            }
        }

        // ✅ Allow only numbers + control keys
        private void NumericOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }
        }

        // ✅ Allow decimal numbers with max 2 decimal places (e.g. 100.87)
        private void DecimalTwoPlaces_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;

            TextBox tb = sender as TextBox;
            string currentText = tb.Text;

            // Allow only one decimal point
            if (e.KeyChar == '.')
            {
                e.Handled = currentText.Contains('.');
                return;
            }

            // Allow digits, but limit to 2 places after decimal
            if (char.IsDigit(e.KeyChar))
            {
                int dotIndex = currentText.IndexOf('.');
                if (dotIndex >= 0)
                {
                    string afterDot = currentText.Substring(dotIndex + 1);
                    int selStart = tb.SelectionStart;
                    int selLen = tb.SelectionLength;
                    if (selStart > dotIndex)
                    {
                        int removeFrom = selStart - dotIndex - 1;
                        int removeCount = Math.Min(selLen, afterDot.Length - removeFrom);
                        if (removeCount > 0)
                            afterDot = afterDot.Remove(removeFrom, removeCount);
                    }
                    if (afterDot.Length >= 2)
                    {
                        e.Handled = true;
                        return;
                    }
                }
                return;
            }

            e.Handled = true;
        }

        private void Notes_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) &&
                !char.IsLetterOrDigit(e.KeyChar) &&
                !char.IsWhiteSpace(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        // Safe Conversion Method
        private int SafeConvertToInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                try
                {
                    return (int)Convert.ToInt64(value);
                }
                catch
                {
                    return 0;
                }
            }
        }

        // Update Invoice
        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (ValidateData())
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                // Get selected IDs
                                int sellerId = cmbSeller.SelectedValue != null ?
                                    SafeConvertToInt(cmbSeller.SelectedValue) : originalSellerId;
                                int customerId = cmbBuyer.SelectedValue != null ?
                                    SafeConvertToInt(cmbBuyer.SelectedValue) : originalCustomerId;

                                // Calculate totals
                                decimal subTotal = 0, totalTax = 0;
                                foreach (DataGridViewRow row in dgvItems.Rows)
                                {
                                    if (row.IsNewRow) continue;

                                    subTotal += Convert.ToDecimal(row.Cells["TotalValue"].Value ?? 0);
                                    totalTax += Convert.ToDecimal(row.Cells["SalesTaxAmount"].Value ?? 0) +
                                               Convert.ToDecimal(row.Cells["FurtherTaxAmount"].Value ?? 0);
                                }
                                decimal grandTotal = subTotal + totalTax;

                                // Update invoice header
                                string updateInvoiceSql = @"
                            UPDATE Invoices SET 
                                invoiceNumber = @invoiceNumber,
                                fbrInvoiceNumber = @fbrInvoiceNumber,
                                invoiceDate = @invoiceDate,
                                sellerId = @sellerId,
                                customerId = @customerId,
                                subTotal = @subTotal,
                                totalTax = @totalTax,
                                grandTotal = @grandTotal,
                                status = @status
                            WHERE invoiceId = @invoiceId";

                                using (var cmd = new System.Data.SQLite.SQLiteCommand(updateInvoiceSql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@invoiceNumber", txtInvoiceNumber.Text);
                                    cmd.Parameters.AddWithValue("@fbrInvoiceNumber", string.IsNullOrEmpty(txtFBRInvoiceNumber.Text) ? DBNull.Value : (object)txtFBRInvoiceNumber.Text);
                                    cmd.Parameters.AddWithValue("@invoiceDate", dtpInvoiceDate.Value.ToString("yyyy-MM-dd"));
                                    cmd.Parameters.AddWithValue("@sellerId", sellerId);
                                    cmd.Parameters.AddWithValue("@customerId", customerId);
                                    cmd.Parameters.AddWithValue("@subTotal", subTotal);
                                    cmd.Parameters.AddWithValue("@totalTax", totalTax);
                                    cmd.Parameters.AddWithValue("@grandTotal", grandTotal);
                                    cmd.Parameters.AddWithValue("@status", cmbStatus.Text);
                                    cmd.Parameters.AddWithValue("@invoiceId", invoiceId);

                                    cmd.ExecuteNonQuery();
                                }

                                // Delete existing items
                                string deleteItemsSql = "DELETE FROM InvoiceItems WHERE invoiceId = @invoiceId";
                                using (var cmd = new System.Data.SQLite.SQLiteCommand(deleteItemsSql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@invoiceId", invoiceId);
                                    cmd.ExecuteNonQuery();
                                }

                                // Insert updated items
                                foreach (DataGridViewRow row in dgvItems.Rows)
                                {
                                    if (row.IsNewRow || row.Cells["HSCode"].Value == null) continue;

                                    var productRow = productsData.AsEnumerable()
                                        .FirstOrDefault(r => r.Field<string>("hsCode") == row.Cells["HSCode"].Value.ToString());

                                    if (productRow != null)
                                    {
                                        int productId = SafeConvertToInt(productRow["productId"]);
                                        decimal quantity = Convert.ToDecimal(row.Cells["Quantity"].Value);
                                        decimal unitPrice = Convert.ToDecimal(row.Cells["UnitPrice"].Value);
                                        decimal totalAmount = Convert.ToDecimal(row.Cells["TotalValue"].Value);
                                        decimal discount = Convert.ToDecimal(row.Cells["Discount"].Value ?? 0);
                                        decimal valueExclGST = Convert.ToDecimal(row.Cells["ValueExclGST"].Value ?? 0);
                                        decimal salesTax = Convert.ToDecimal(row.Cells["SalesTaxAmount"].Value);
                                        decimal furtherTax = Convert.ToDecimal(row.Cells["FurtherTaxAmount"].Value);

                                        DatabaseHelper.AddInvoiceItem(
                                            invoiceId,
                                            productId,
                                            row.Cells["Product_Desc"].Value?.ToString() ?? "",
                                            quantity,
                                            productRow.Field<string>("rate"),
                                            unitPrice,
                                            totalAmount,
                                            valueExclGST, // valueSalesExcludingST (excl. GST)
                                            totalAmount,  // fixedNotifiedValueOrRetailPrice
                                            salesTax,     // salesTaxApplicable
                                            0,            // salesTaxWithheldAtSource
                                            0,            // extraTax
                                            furtherTax,   // furtherTax
                                            0,            // fedPayable
                                            discount,     // discount (actual value from grid)
                                            row.Cells["saleType"].Value?.ToString() ?? "Standard",
                                            "", // sroItemSerialNo
                                            ""  // sroScheduleNo
                                        );
                                    }
                                }

                                transaction.Commit();
                                MessageBox.Show("✅ Invoice updated successfully!", "Success",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception("Error updating invoice: " + ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Error updating invoice: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(txtInvoiceNumber.Text))
            {
                MessageBox.Show("Please enter invoice number!", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (dgvItems.Rows.Count == 0 || (dgvItems.Rows.Count == 1 && dgvItems.Rows[0].IsNewRow))
            {
                MessageBox.Show("Please add at least one invoice item!", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                if (!row.IsNewRow)
                {
                    if (string.IsNullOrWhiteSpace(row.Cells["HSCode"].Value?.ToString()) ||
                        string.IsNullOrWhiteSpace(row.Cells["Product_Desc"].Value?.ToString()))
                    {
                        MessageBox.Show("Please enter item code and name for all items!", "Validation Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    if (!decimal.TryParse(row.Cells["Quantity"].Value?.ToString(), out decimal quantity) || quantity <= 0)
                    {
                        MessageBox.Show("Please enter valid quantity for all items!", "Validation Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}