using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDK_E_INVOICING_SYSTEM;
using SDK_E_INVOICING_SYSTEM.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;


public class GenerateInvoiceForm : Form
{
    private ComboBox cmbBuyerName, cmbHSCode, cmbScenario, cmbSaleType;
    private TextBox txtSellerNTN, txtSellerBusiness, txtSellerProvince, txtSellerAddress;
    private TextBox txtBuyerNTN, txtBuyerProvince, txtBuyerAddress, txtBuyerRegType;
    private TextBox txtProdDesc, txtProdRate, txtProdUOM;
    private TextBox txtHSCode;
    private TextBox txtQuantity, txtTotalValue, txtValueExclGST;
    private TextBox txtSalesTaxAmount, txtFurtherTaxAmount, txtExtraTaxAmount;
    private TextBox txtUnitPrice, txtDiscount;
    private ComboBox cmbSroSchedule; // SRO schedule dropdown
    private ComboBox cmbSroItemSerialNo; // SRO item serial no dropdown
    private TextBox txtItemNotes; // New Notes field
    private Button btnAddItem, btnValidateInvoice, btnPost, btnDeleteItem;
    private DataGridView dgvItems;
    private DataTable buyers, products;
    private Label lblSubtotal;
    private DateTimePicker dtInvoiceDate;
    private TextBox txtInvoiceNumber;

    private ProgressBar progressBar;
    // Seller info variables
    private string sellerBusinessName;
    private string sellerNTN;
    private string sellerProvince;
    private string sellerAddress;
    private string sellerToken; // from login
    private ComboBox cmbSellerName;





    private void InitializeLoader()
    {
        progressBar = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Dock = DockStyle.Top,
            Height = 20,
            Visible = false
        };
        this.Controls.Add(progressBar);
        progressBar.BringToFront();
    }



    private int editingRowIndex = -1;

    public GenerateInvoiceForm()
    {

        this.Text = "Generate Invoice";
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.WhiteSmoke;

        DatabaseHelper.InitializeDatabase();
        InitializeLoader();
        // Initialize Invoice Number first
        txtInvoiceNumber = CreateTextBox(GetNextInvoiceNumber().ToString(), true);



        InitializeUI();
    }

    private void InitializeUI()
    {



        FlowLayoutPanel mainLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10),
            WrapContents = true,
        };
        this.Controls.Add(mainLayout);


        // Seller Info
        GroupBox gbSeller = CreateGroupBox("🏢 Seller Info", new Size(400, 150));
        cmbSellerName = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        txtSellerNTN = CreateTextBox("", true);
        txtSellerBusiness = CreateTextBox("", true);
        txtSellerProvince = CreateTextBox("", true);
        txtSellerAddress = CreateTextBox("", true);

        AddLabeledControls(gbSeller,
            ("Select Seller:", cmbSellerName),
            ("NTN:", txtSellerNTN),

            ("Province:", txtSellerProvince),
            ("Address:", txtSellerAddress));

        mainLayout.Controls.Add(gbSeller);
        DataTable sellers = DatabaseHelper.GetSellers(); // make sure you have a GetSellers() method

        cmbSellerName.DisplayMember = "sellerBusinessName"; // column in DB
        cmbSellerName.ValueMember = "sellerId";

        cmbSellerName.SelectedIndexChanged += (s, e) =>
        {
            if (cmbSellerName.SelectedIndex >= 0 && sellers != null && sellers.Rows.Count > cmbSellerName.SelectedIndex)
            {
                DataRow row = sellers.Rows[cmbSellerName.SelectedIndex];
                txtSellerNTN.Text = row["sellerNTNCNIC"].ToString();
                txtSellerBusiness.Text = row["sellerBusinessName"].ToString();
                txtSellerProvince.Text = row["sellerProvince"].ToString();
                txtSellerAddress.Text = row["sellerAddress"].ToString();
                sellerToken = row["token"].ToString();

                // optional: store in variables for API
                sellerNTN = txtSellerNTN.Text;
                sellerBusinessName = txtSellerBusiness.Text;
                sellerProvince = txtSellerProvince.Text;
                sellerAddress = txtSellerAddress.Text;
            }
        };

        cmbSellerName.DataSource = sellers;

        // Buyer Info
        GroupBox gbBuyer = CreateGroupBox("👤 Buyer Info", new Size(400, 180));
        cmbBuyerName = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        txtBuyerNTN = CreateTextBox("", true);
        txtBuyerProvince = CreateTextBox("", true);
        txtBuyerAddress = CreateTextBox("", true);
        txtBuyerRegType = CreateTextBox("", true);
        AddLabeledControls(gbBuyer,
            ("Business:", cmbBuyerName), ("NTN/CNIC:", txtBuyerNTN),
            ("Province:", txtBuyerProvince), ("Address:", txtBuyerAddress),
            ("Reg Type:", txtBuyerRegType));
        mainLayout.Controls.Add(gbBuyer);

        // Product Info
        GroupBox gbProduct = CreateGroupBox("📦 Product Info", new Size(400, 150));
        cmbHSCode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        txtProdDesc = CreateTextBox("", true);
        txtHSCode = CreateTextBox("", true);
        txtProdRate = CreateTextBox("", true);
        txtProdUOM = CreateTextBox("", true);
        // Show product description in dropdown and HS Code in a textbox
        AddLabeledControls(gbProduct,
            ("Description:", cmbHSCode), ("HS Code:", txtHSCode),
            ("Tax Rate:", txtProdRate), ("UOM:", txtProdUOM));
        mainLayout.Controls.Add(gbProduct);

        // Invoice Item Panel
        GroupBox gbItem = CreateGroupBox("🧾 Invoice Item", new Size(330, 390));

        cmbScenario = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        cmbScenario.Items.Add("Select");
        cmbScenario.Items.Add("SN001");
        cmbScenario.Items.Add("SN002");
        cmbScenario.Items.Add("SN003");
        cmbScenario.Items.Add("SN004");
        cmbScenario.Items.Add("SN005");
        cmbScenario.Items.Add("SN006");
        cmbScenario.Items.Add("SN007");
        cmbScenario.Items.Add("SN008");
        cmbScenario.Items.Add("SN009");
        cmbScenario.Items.Add("SN010");
        cmbScenario.Items.Add("SN011");
        cmbScenario.Items.Add("SN012");
        cmbScenario.Items.Add("SN013");
        cmbScenario.Items.Add("SN014");
        cmbScenario.Items.Add("SN015");
        cmbScenario.Items.Add("SN016");
        cmbScenario.Items.Add("SN017");
        cmbScenario.Items.Add("SN018");
        cmbScenario.Items.Add("SN019");
        cmbScenario.Items.Add("SN020");
        cmbScenario.Items.Add("SN021");
        cmbScenario.Items.Add("SN022");
        cmbScenario.Items.Add("SN023");
        cmbScenario.Items.Add("SN024");
        cmbScenario.Items.Add("SN025");
        cmbScenario.Items.Add("SN026");
        cmbScenario.Items.Add("SN027");
        cmbScenario.Items.Add("SN028");

        cmbScenario.SelectedIndex = 0;

        txtQuantity = CreateTextBox();
        txtUnitPrice = CreateTextBox();
        txtDiscount = CreateTextBox();
        // SRO schedule dropdown (empty + two options)
        cmbSroSchedule = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        cmbSroSchedule.Items.Add(string.Empty);
        cmbSroSchedule.Items.Add("EIGHTH SCHEDULE Table 1");
        cmbSroSchedule.Items.Add("6th Schd Table I");
        cmbSroSchedule.Items.Add("327(I)/2008");
        cmbSroSchedule.Items.Add("1450(I)/2021");
        cmbSroSchedule.Items.Add("NINTH SCHEDULE");
        cmbSroSchedule.Items.Add("Goods (FED in ST Mode)");
        cmbSroSchedule.Items.Add("ICTO TABLE I");
        cmbSroSchedule.Items.Add("ICTO TABLE II");
        cmbSroSchedule.Items.Add("6th Schd Table III");
        cmbSroSchedule.Items.Add("581(1)/2024");
        cmbSroSchedule.Items.Add("297(I)/2023-Table-I");
        

        cmbSroSchedule.SelectedIndex = 0;

        // SRO item serial no dropdown (1..100)
        cmbSroItemSerialNo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        for (int i = 1; i <= 100; i++) cmbSroItemSerialNo.Items.Add(i.ToString());
        cmbSroItemSerialNo.SelectedIndex = -1; // no default selection

        txtTotalValue = CreateTextBox();
        txtValueExclGST = CreateTextBox();
        txtSalesTaxAmount = CreateTextBox();
        txtFurtherTaxAmount = CreateTextBox();
        //txtExtraTaxAmount = CreateTextBox();
        txtItemNotes = CreateTextBox(); // Notes field
        txtQuantity.TextChanged += RecalculateTotalValue;
        txtUnitPrice.TextChanged += RecalculateTotalValue;
        txtDiscount.TextChanged += RecalculateTotalValue;
        // ✅ Numbers only
        txtQuantity.KeyPress += NumericOnly_KeyPress;
        txtUnitPrice.KeyPress += DecimalTwoPlaces_KeyPress;
        txtDiscount.KeyPress += DecimalTwoPlaces_KeyPress;
        txtSalesTaxAmount.KeyPress += NumericOnly_KeyPress;
        txtFurtherTaxAmount.KeyPress += NumericOnly_KeyPress;
        txtValueExclGST.KeyPress += NumericOnly_KeyPress;

        // ✅ Notes (alphanumeric only, max length = 20)
        txtItemNotes.KeyPress += Notes_KeyPress;
        txtItemNotes.MaxLength = 20;

        //dtInvoiceDate = new DateTimePicker();
        //dtInvoiceDate.Format = DateTimePickerFormat.Short;

        cmbSaleType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        cmbSaleType.Items.Add("Select");
        cmbSaleType.Items.Add("Goods at standard rate (default)");
        cmbSaleType.Items.Add("Goods at Reduced Rate");
        cmbSaleType.Items.Add("Goods at Zero-rate");
        cmbSaleType.Items.Add("Petroleum Products");
        cmbSaleType.Items.Add("Electricity Supply to Retailers");
        cmbSaleType.Items.Add("SIM");
        cmbSaleType.Items.Add("Gas to CNG stations");
        cmbSaleType.Items.Add("Mobile Phones");
        cmbSaleType.Items.Add("Processing/Conversion of Goods");
        cmbSaleType.Items.Add("3rd Schedule Goods");
        cmbSaleType.Items.Add("Goods (FED in ST Mode)");
        cmbSaleType.Items.Add("Services (FED in ST Mode)");
        cmbSaleType.Items.Add("Services");
        cmbSaleType.Items.Add("Exempt goods");
        cmbSaleType.Items.Add("Ship breaking");

        cmbSaleType.SelectedIndex = 0;

        AddLabeledControls(gbItem,
            //  ("Invoice No:", txtInvoiceNumber),
            ("Scenario:", cmbScenario),
            ("Quantity:", txtQuantity),
            ("Unit Price:", txtUnitPrice),
            ("Discount:", txtDiscount),
            // ("Invoice Date:", dtInvoiceDate),
            ("Total Value:", txtTotalValue),
            ("Excl GST:", txtValueExclGST),
            ("Sales Tax:", txtSalesTaxAmount),
            ("Further Tax:", txtFurtherTaxAmount),
             /* ("Extra Tax:", txtExtraTaxAmount),*/
             ("Sale Type:", cmbSaleType),
             ("SRO Item Serial No:", cmbSroItemSerialNo),
             ("SRO Schedule:", cmbSroSchedule)
           /* ("Notes:", txtItemNotes)*/ // Add Notes to UI
        );

        FlowLayoutPanel itemButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40 };
        btnAddItem = CreateButton("➕ Add Item", ColorTranslator.FromHtml("#C8A84B"));
        btnDeleteItem = CreateButton("🗑️ Delete Item", ColorTranslator.FromHtml("#8B1A1A"));
        btnAddItem.Enabled = true;
        btnDeleteItem.Enabled = false;
        itemButtons.Controls.AddRange(new Control[] { btnAddItem, btnDeleteItem });
        gbItem.Controls.Add(itemButtons);

        // Invoice Grid
        GroupBox gbGrid = CreateGroupBox("📋 Invoice Items", new Size(980, 350));
        dgvItems = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };



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
        dgvItems.Columns.Add("sroItemSerialNo", "SRO Item Serial No");
        dgvItems.Columns.Add("sroScheduleNo", "SRO/Schedule No");
        dgvItems.Columns.Add("Notes", "Notes");





        dgvItems.BackgroundColor = Color.White;
        dgvItems.EnableHeadersVisualStyles = false;
        dgvItems.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#1D2068");
        dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        dgvItems.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        dgvItems.DefaultCellStyle.Font = new Font("Segoe UI", 9);
        dgvItems.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 60);
        dgvItems.DefaultCellStyle.BackColor = Color.White;
        dgvItems.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#C8A84B");
        dgvItems.DefaultCellStyle.SelectionForeColor = Color.White;

        dgvItems.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 250);
        dgvItems.AlternatingRowsDefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 60);



        lblSubtotal = new Label
        {
            Text = "Grand Total: 0.00",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1D2068"),
            Dock = DockStyle.Bottom,
            Height = 30,
            TextAlign = ContentAlignment.MiddleRight
        };
        gbGrid.Controls.Add(dgvItems);
        gbGrid.Controls.Add(lblSubtotal);

        FlowLayoutPanel gridButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            FlowDirection = FlowDirection.RightToLeft
        };
        btnPost = CreateButton("📤 Post Invoice", ColorTranslator.FromHtml("#1D2068"));
        btnValidateInvoice = CreateButton("✅ Validate Invoice", ColorTranslator.FromHtml("#C8A84B"));
        // Save button removed from UI per request
        gridButtons.Controls.AddRange(new Control[] { btnPost, btnValidateInvoice });
        btnValidateInvoice.Click += btnValidate_Click;
        btnPost.Click += btnSubmit_Click;
        // Save functionality disabled per request

        btnPost.Enabled = false;
        // Save button is removed; nothing to enable/disable
        txtQuantity.TextChanged += RecalculateSalesTax;
        txtUnitPrice.TextChanged += RecalculateSalesTax;
        txtDiscount.TextChanged += RecalculateSalesTax;
        txtProdRate.TextChanged += RecalculateSalesTax;

        gbGrid.Controls.Add(gridButtons);

        FlowLayoutPanel itemLayout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            Dock = DockStyle.Top,
            Padding = new Padding(5),
        };
        itemLayout.Controls.Add(gbItem);
        itemLayout.Controls.Add(gbGrid);
        mainLayout.Controls.Add(itemLayout);

        // Load Data
        buyers = DatabaseHelper.GetCustomers();
        cmbBuyerName.DisplayMember = "customerBusinessName";
        cmbBuyerName.ValueMember = "customerId";
        cmbBuyerName.SelectedIndexChanged += (s, e) =>
        {
            if (cmbBuyerName.SelectedIndex >= 0 && buyers != null && buyers.Rows.Count > cmbBuyerName.SelectedIndex)
            {
                DataRow row = buyers.Rows[cmbBuyerName.SelectedIndex];
                txtBuyerNTN.Text = row["customerNTNCNIC"].ToString();
                txtBuyerProvince.Text = row["customerProvince"].ToString();
                txtBuyerAddress.Text = row["customerAddress"].ToString();
                txtBuyerRegType.Text = row["registrationType"].ToString();
            }
        };
        cmbBuyerName.DataSource = buyers;

        products = DatabaseHelper.GetProducts();
        // Show product description in dropdown; HS Code will appear in txtHSCode
        cmbHSCode.DisplayMember = "productDescription";
        cmbHSCode.ValueMember = "productId";
        cmbHSCode.SelectedIndexChanged += (s, e) =>
        {
            if (cmbHSCode.SelectedIndex >= 0 && products != null && products.Rows.Count > cmbHSCode.SelectedIndex)
            {
                DataRow row = products.Rows[cmbHSCode.SelectedIndex];
                // fill description textbox and HS code textbox
                txtProdDesc.Text = row["productDescription"].ToString();
                txtHSCode.Text = row["hsCode"].ToString();
                txtProdRate.Text = row["rate"].ToString();
                txtProdUOM.Text = row["uoM"].ToString();
            }
        };
        cmbHSCode.DataSource = products;

        // Events
        btnAddItem.Click += BtnAddItem_Click;
        btnDeleteItem.Click += BtnDeleteItem_Click;
        dgvItems.CellDoubleClick += DgvItems_CellDoubleClick;
        dgvItems.SelectionChanged += (s, e) =>
        {
            btnDeleteItem.Enabled = dgvItems.SelectedRows.Count > 0;
        };
    }

    private void BtnAddItem_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtQuantity.Text) || string.IsNullOrWhiteSpace(txtProdRate.Text))
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

            int idx = dgvItems.Rows.Add();
            FillRow(dgvItems.Rows[idx]);
        }

        ClearFields();
        UpdateSubtotal();
        txtInvoiceNumber.Text = GetNextInvoiceNumber().ToString(); // auto increment for next item
    }

    private void BtnDeleteItem_Click(object sender, EventArgs e)
    {
        if (dgvItems.SelectedRows.Count > 0)
        {
            dgvItems.Rows.RemoveAt(dgvItems.SelectedRows[0].Index);
            editingRowIndex = -1;
            btnAddItem.Text = "➕ Add Item";
            ClearFields();
            UpdateSubtotal();
        }
    }

    private void DgvItems_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            DataGridViewRow row = dgvItems.Rows[e.RowIndex];
            editingRowIndex = e.RowIndex;


            txtQuantity.Text = row.Cells["Quantity"].Value?.ToString();
            txtUnitPrice.Text = row.Cells["UnitPrice"].Value?.ToString();
            txtDiscount.Text = row.Cells["Discount"].Value?.ToString();
            //dtInvoiceDate.Value = DateTime.TryParse(row.Cells["InvoiceDate"].Value?.ToString(), out DateTime d) ? d : DateTime.Today;
            txtProdRate.Text = row.Cells["Rate"].Value?.ToString();
            txtTotalValue.Text = row.Cells["TotalValue"].Value?.ToString();
            txtValueExclGST.Text = row.Cells["ValueExclGST"].Value?.ToString();
            txtSalesTaxAmount.Text = row.Cells["SalesTaxAmount"].Value?.ToString();
            txtFurtherTaxAmount.Text = row.Cells["FurtherTaxAmount"].Value?.ToString();
            //txtExtraTaxAmount.Text = row.Cells["ExtraTaxAmount"].Value?.ToString();
            cmbSaleType.Text = row.Cells["saleType"].Value?.ToString();
            // If sroScheduleNo column exists, ignore if empty
            // (no UI field for SRO currently)
            cmbSroSchedule.Text = row.Cells["sroScheduleNo"]?.Value?.ToString() ?? string.Empty;
            cmbSroItemSerialNo.Text = row.Cells["sroItemSerialNo"]?.Value?.ToString() ?? string.Empty;

            txtItemNotes.Text = row.Cells["Notes"].Value?.ToString();

            // If grid stores HS Code but combobox shows descriptions, select matching product so combobox shows description
            string hsFromRow = row.Cells["HSCode"].Value?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(hsFromRow) && products != null)
            {
                for (int i = 0; i < products.Rows.Count; i++)
                {
                    if (products.Rows[i]["hsCode"].ToString() == hsFromRow)
                    {
                        cmbHSCode.SelectedIndex = i;
                        // also set HS code textbox
                        txtHSCode.Text = hsFromRow;
                        break;
                    }
                }
            }

            btnAddItem.Text = "✏️ Update Item";
        }
    }

    private void FillRow(DataGridViewRow row)
    {
        //row.Cells["InvoiceNo"].Value = txtInvoiceNumber.Text;
        row.Cells["ScenarioID"].Value = cmbScenario.Text;
        // Ensure HS Code cell gets actual HS code even though dropdown shows description
        string selectedHs = "";
        if (cmbHSCode.SelectedIndex >= 0 && products != null)
        {
            selectedHs = products.Rows[cmbHSCode.SelectedIndex]["hsCode"].ToString();
        }
        row.Cells["HSCode"].Value = selectedHs;
        row.Cells["Product_Desc"].Value = txtProdDesc.Text;
        row.Cells["UOM"].Value = txtProdUOM.Text;
        row.Cells["Quantity"].Value = txtQuantity.Text;
        row.Cells["UnitPrice"].Value = txtUnitPrice.Text;
        row.Cells["Discount"].Value = txtDiscount.Text;
        // row.Cells["InvoiceDate"].Value = dtInvoiceDate.Value.ToShortDateString();
        row.Cells["Rate"].Value = txtProdRate.Text;
        row.Cells["TotalValue"].Value = txtTotalValue.Text;
        row.Cells["ValueExclGST"].Value = txtValueExclGST.Text;
        row.Cells["SalesTaxAmount"].Value = txtSalesTaxAmount.Text;
        row.Cells["FurtherTaxAmount"].Value = txtFurtherTaxAmount.Text;
        // row.Cells["ExtraTaxAmount"].Value = txtExtraTaxAmount.Text;
        row.Cells["saleType"].Value = cmbSaleType.Text;
        // ensure sroScheduleNo exists to avoid missing column errors
        if (dgvItems.Columns.Contains("sroScheduleNo"))
            row.Cells["sroScheduleNo"].Value = cmbSroSchedule.Text;
        if (dgvItems.Columns.Contains("sroItemSerialNo"))
            row.Cells["sroItemSerialNo"].Value = cmbSroItemSerialNo.Text;

        row.Cells["Notes"].Value = txtItemNotes.Text;
    }

    private void ClearFields()
    {
        txtQuantity.Clear();
        txtUnitPrice.Clear();
        txtDiscount.Clear();
        txtTotalValue.Clear();
        txtValueExclGST.Clear();
        txtSalesTaxAmount.Clear();
        txtFurtherTaxAmount.Clear();
        // txtExtraTaxAmount.Clear();
        txtItemNotes.Clear();
        if (cmbSroSchedule != null) cmbSroSchedule.SelectedIndex = 0;
        if (cmbSroItemSerialNo != null) cmbSroItemSerialNo.SelectedIndex = -1;
        editingRowIndex = -1;
        btnAddItem.Text = "➕ Add Item";
    }

    private void UpdateSubtotal()
    {
        decimal subtotal = 0;
        decimal salesTax = 0;
        decimal furtherTax = 0;

        foreach (DataGridViewRow row in dgvItems.Rows)
        {
            subtotal += Convert.ToDecimal(row.Cells["TotalValue"].Value ?? 0);
            salesTax += Convert.ToDecimal(row.Cells["SalesTaxAmount"].Value ?? 0);
            furtherTax += Convert.ToDecimal(row.Cells["FurtherTaxAmount"].Value ?? 0);
        }

        // Subtotal ab product total + sales tax
        // lblSubtotal.Text = $"Subtotal (Incl. Sales Tax): {(subtotal + salesTax):C}";

        // Total ab subtotal + sales tax + further tax
        lblSubtotal.Text = $"Grand Total: {(subtotal + salesTax + furtherTax):N2}";

    }



    private GroupBox CreateGroupBox(string title, Size size) => new GroupBox
    {
        Text = title,
        Size = size,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.DarkSlateGray,
        Padding = new Padding(8),
        Margin = new Padding(8),
    };

    private TextBox CreateTextBox(string text = "", bool readOnly = false) => new TextBox { Text = text, ReadOnly = readOnly, Width = 200 };

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
        TableLayoutPanel tlp = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var (labelText, control) in pairs)
        {
            tlp.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(2) });
            tlp.Controls.Add(control);
        }
        gb.Controls.Add(tlp);
    }
    // ✅ Allow only numbers + control keys (Backspace, Delete etc.)
    private void NumericOnly_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
        {
            e.Handled = true; // block invalid input
        }
    }

    // ✅ Allow decimal numbers with max 2 decimal places (e.g. 100.87)
    private void DecimalTwoPlaces_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar)) return; // allow Backspace etc.

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
                // Count digits after decimal point (excluding selected text that will be replaced)
                string afterDot = currentText.Substring(dotIndex + 1);
                // Remove selected portion from afterDot calculation
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
                    e.Handled = true; // max 2 decimal places
                    return;
                }
            }
            return;
        }

        e.Handled = true; // block all other chars
    }

    // ✅ Allow only letters, numbers, space, and basic symbols (alphanumeric for Notes)
    private void Notes_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) &&
            !char.IsLetterOrDigit(e.KeyChar) &&
            !char.IsWhiteSpace(e.KeyChar))
        {
            e.Handled = true;
        }
    }

    private void RecalculateTotalValue(object sender, EventArgs e)
    {
        if (decimal.TryParse(txtQuantity.Text, out decimal qty) &&
            decimal.TryParse(txtUnitPrice.Text, out decimal unitPrice) &&
            decimal.TryParse(txtProdRate.Text, out decimal rate)) // Rate in %
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


    public static int GetNextInvoiceNumber()
    {
        using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
        {
            conn.Open();
            string sql = "SELECT MAX(invoiceId) FROM Invoices";
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                    return Convert.ToInt32(result) + 1;
                else
                    return 1;
            }
        }
    }




    // Inside your button click:
    private async void btnValidate_Click(object sender, EventArgs e)
    {
        try
        {
            // Ensure SRO values are present for items with rates other than 18%
            if (!EnsureSroForItems(out string sroErr))
            {
                MessageBox.Show($"⚠️ Validation blocked: {sroErr}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // --- Pre-flight: Validate Seller NTN and Token ---
            string currentNtn = txtSellerNTN.Text.Replace("-", "").Replace(" ", "").Trim();
            string currentToken = (sellerToken ?? "").Trim();

            if (string.IsNullOrWhiteSpace(currentNtn) || (currentNtn.Length != 7 && currentNtn.Length != 13))
            {
                MessageBox.Show(
                    $"❌ Seller NTN/CNIC is invalid!\n\n" +
                    $"Current value: \"{txtSellerNTN.Text}\"\n" +
                    $"Digits (after removing hyphens/spaces): {currentNtn.Length}\n\n" +
                    $"NTN must be exactly 7 digits, or CNIC must be exactly 13 digits.\n" +
                    $"Please go to Seller Management and correct the NTN/CNIC.",
                    "Invalid Seller NTN", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(currentToken))
            {
                MessageBox.Show(
                    "❌ Seller API Token is empty!\n\n" +
                    "Please go to Seller Management and enter the FBR Bearer Token for this seller.",
                    "Missing Token", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Show debug info so user can verify what is being sent
            var confirm = MessageBox.Show(
                $"📤 Sending to FBR with:\n\n" +
                $"Seller NTN/CNIC: {currentNtn} ({currentNtn.Length} digits)\n" +
                $"Token (first 20 chars): {currentToken.Substring(0, Math.Min(20, currentToken.Length))}...\n\n" +
                $"Click OK to proceed.",
                "Confirm API Call", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (confirm != DialogResult.OK) return;

            btnValidateInvoice.Enabled = false;
            progressBar.Visible = true;
            this.UseWaitCursor = true;

            string jsonPayload = BuildInvoiceJson();

            var service = new FbrApiService();
            string result = await service.ValidateInvoiceDataAsync(jsonPayload, currentToken);

            ShowApiResponse("Validation", result);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message, "Validation Failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnValidateInvoice.Enabled = true;
            progressBar.Visible = false;
            this.UseWaitCursor = false;
        }
    }





    // Custom converter to remove .0 from integer values
    public class IntDecimalConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal) || objectType == typeof(double);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is decimal dec)
            {
                writer.WriteRawValue(dec % 1 == 0 ? dec.ToString("0") : dec.ToString());
            }
            else if (value is double dbl)
            {
                writer.WriteRawValue(dbl % 1 == 0 ? dbl.ToString("0") : dbl.ToString());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return Convert.ToDecimal(reader.Value);
        }
    }

    private async void btnSubmit_Click(object sender, EventArgs e)
    {
        try
        {
            // Ensure SRO values are present for items with rates other than 18% before posting
            if (!EnsureSroForItems(out string sroErrPost))
            {
                MessageBox.Show($"⚠️ Posting blocked: {sroErrPost}", "Posting Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Disable buttons during processing
            btnPost.Enabled = false;
            btnValidateInvoice.Enabled = false;
            progressBar.Visible = true;
            this.UseWaitCursor = true;

            // --- 1. Build JSON payload for FBR API ---
            string jsonPayload = BuildInvoiceJson();

            // --- 2. Call FBR API ---
            var service = new FbrApiService();
            string result = await service.PostInvoiceDataAsync(jsonPayload, sellerToken);

            // --- 3. Parse response ---
            string invoiceNumber = TryExtractInvoiceNumber(result);

            // --- 4. Handle result ---
            if (!string.IsNullOrEmpty(invoiceNumber))
            {
                // Save posted invoice locally with FBR invoice number
                try
                {
                    // Resolve customerId and sellerId from selected controls
                    int customerId = cmbBuyerName.SelectedValue is int cv ? cv : (int)Convert.ToInt32(cmbBuyerName.SelectedValue);
                    int sellerId = cmbSellerName.SelectedValue is int sv ? sv : (int)Convert.ToInt32(cmbSellerName.SelectedValue);

                    decimal subTotal = 0, totalTax = 0, grandTotal = 0;
                    foreach (DataGridViewRow r in dgvItems.Rows)
                    {
                        subTotal += Convert.ToDecimal(r.Cells["TotalValue"].Value ?? 0m);
                        totalTax += Convert.ToDecimal(r.Cells["SalesTaxAmount"].Value ?? 0m);
                    }
                    grandTotal = subTotal + totalTax + dgvItems.Rows.Cast<DataGridViewRow>().Sum(r => Convert.ToDecimal(r.Cells["FurtherTaxAmount"].Value ?? 0m));

                    int insertedInvoiceId = DatabaseHelper.PostInvoice(
                        customerId,
                        sellerId,
                        DateTime.Now,
                        subTotal,
                        totalTax,
                        0m,
                        grandTotal,
                        "",
                        "Unpaid",
                        "Posted",
                        invoiceNumber
                    );

                    // Save items
                    foreach (DataGridViewRow row in dgvItems.Rows)
                    {


                        if (row.IsNewRow) continue;
                        string hsCode = row.Cells["HSCode"].Value?.ToString();
                        int productId = DatabaseHelper.GetProductIdByHsCode(hsCode);
                        if (productId == -1) productId = 0; // fallback

                        string desc = row.Cells["Product_Desc"].Value?.ToString();
                        decimal qty = Convert.ToDecimal(row.Cells["Quantity"].Value ?? 0m);
                        string rate = row.Cells["Rate"].Value?.ToString() ?? "";
                        decimal unitPrice = Convert.ToDecimal(row.Cells["UnitPrice"].Value ?? 0m);
                        decimal totalVal = Convert.ToDecimal(row.Cells["TotalValue"].Value ?? 0m);
                        decimal valueExcl = Convert.ToDecimal(row.Cells["ValueExclGST"].Value ?? 0m);
                        decimal salesTax = Convert.ToDecimal(row.Cells["SalesTaxAmount"].Value ?? 0m);
                        decimal further = Convert.ToDecimal(row.Cells["FurtherTaxAmount"].Value ?? 0m);
                        decimal discountVal = Convert.ToDecimal(row.Cells["Discount"].Value ?? 0m);
                        string saleType = row.Cells["saleType"].Value?.ToString() ?? "Goods at standard rate";
                        string sroSerial = row.Cells["sroItemSerialNo"]?.Value?.ToString() ?? "";
                        string sroSched = row.Cells["sroScheduleNo"]?.Value?.ToString() ?? "";

                        DatabaseHelper.AddInvoiceItem(
                            insertedInvoiceId,
                            productId,
                            desc,
                            qty,
                            rate,
                            unitPrice,
                            totalVal,
                            valueExcl,
                            0m,
                            salesTax,
                            0m,
                            0m,
                            further,
                            0m,
                            discountVal,
                            saleType,
                            sroSerial,
                            sroSched
                        );
                    }

                    MessageBox.Show($"✅ Posted to FBR and saved locally.\nFBR Invoice No: {invoiceNumber}", "Posted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dgvItems.Rows.Clear();
                    ClearFields();
                    UpdateSubtotal();
                    txtInvoiceNumber.Text = GetNextInvoiceNumber().ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Posted to FBR but failed to save locally: " + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                // Show truncated raw response for debugging
                string display = string.IsNullOrWhiteSpace(result) ? "(empty response)" : result;
                if (display.Length > 1500) display = display.Substring(0, 1500) + "...";
                MessageBox.Show($"❌ FBR response did not contain a valid invoice number.\n\nRaw response:\n{display}",
                    "Posting Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("❌ Error while posting invoice: " + ex.Message,
                "Posting Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // Enable UI again
            btnPost.Enabled = true;
            btnValidateInvoice.Enabled = true;
            progressBar.Visible = false;
            this.UseWaitCursor = false;
        }
    }



    private string BuildInvoiceJson()
    {
        var invoice = new
        {
            invoiceType = "Sale Invoice",
            invoiceDate = DateTime.Now.ToString("yyyy-MM-dd"),
            sellerNTNCNIC = txtSellerNTN.Text,
            sellerBusinessName = string.IsNullOrWhiteSpace(txtSellerBusiness.Text) ? (cmbSellerName?.Text ?? "") : txtSellerBusiness.Text,
            sellerProvince = txtSellerProvince.Text,
            sellerAddress = txtSellerAddress.Text,
            buyerNTNCNIC = txtBuyerNTN.Text,
            buyerBusinessName = string.IsNullOrWhiteSpace(cmbBuyerName?.Text) ? txtBuyerNTN.Text : cmbBuyerName.Text,
            buyerProvince = txtBuyerProvince.Text,
            buyerAddress = txtBuyerAddress.Text,
            buyerRegistrationType = txtBuyerRegType.Text,
            invoiceRefNo = txtInvoiceNumber.Text,
            scenarioId = cmbScenario.Text,

            items = GetInvoiceItems()
        };

        return JsonConvert.SerializeObject(invoice, Newtonsoft.Json.Formatting.Indented);
    }

    private List<object> GetInvoiceItems()
    {
        var items = new List<object>();

        foreach (DataGridViewRow row in dgvItems.Rows)
        {
            if (row.IsNewRow) continue;

            decimal quantity = Convert.ToDecimal(row.Cells["Quantity"].Value ?? 0m);
            decimal totalValues = Convert.ToDecimal(row.Cells["TotalValue"].Value ?? 0m);
            decimal valueSalesExcludingST = Convert.ToDecimal(row.Cells["ValueExclGST"].Value ?? 0m);
            decimal salesTaxApplicable = Convert.ToDecimal(row.Cells["SalesTaxAmount"].Value ?? 0m);
            decimal furtherTax = Convert.ToDecimal(row.Cells["FurtherTaxAmount"].Value ?? 0m);
            decimal discount = Convert.ToDecimal(row.Cells["Discount"].Value ?? 0m);

            // Prefer grid cell value for SRO schedule, fallback to UI dropdown
            string sroSchedule = row.Cells["sroScheduleNo"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(sroSchedule)) sroSchedule = cmbSroSchedule?.Text;

            items.Add(new
            {
                hsCode = row.Cells["HSCode"].Value?.ToString(),
                productDescription = row.Cells["Product_Desc"].Value?.ToString(),
                rate = row.Cells["Rate"].Value?.ToString(),
                uoM = row.Cells["UOM"].Value?.ToString(),

                // Numbers as numeric types
                quantity = quantity,
                totalValues = totalValues,
                valueSalesExcludingST = valueSalesExcludingST,

                fixedNotifiedValueOrRetailPrice = 0.00m,
                salesTaxApplicable = salesTaxApplicable,
                salesTaxWithheldAtSource = 0.00m,
                extraTax = "0",
                furtherTax = furtherTax,
                fedPayable = 0.00m,
                discount = discount,

                saleType = row.Cells["saleType"].Value?.ToString() ?? "Goods at standard rate",
                sroScheduleNo = sroSchedule ?? "",
                notes = row.Cells["Notes"].Value?.ToString(),
                sroItemSerialNo = row.Cells["sroItemSerialNo"]?.Value?.ToString() ?? ""
            });

        }

        return items;
    }
    private void ShowApiResponse(string action, string rawResponse)
    {
        try
        {
            // Sirf JSON nikaalna (agar headers bhi aaye to)
            if (rawResponse.Contains("Response Body:"))
            {
                int index = rawResponse.IndexOf("Response Body:") + "Response Body:".Length;
                rawResponse = rawResponse.Substring(index).Trim();
            }

            var parsed = JObject.Parse(rawResponse);

            string status = parsed["validationResponse"]?["status"]?.ToString() ?? "UNKNOWN";
            string statusCode = parsed["validationResponse"]?["statusCode"]?.ToString() ?? "";
            string errorMsg = parsed["validationResponse"]?["error"]?.ToString() ?? "";

            if (status.Equals("Valid", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    $"✅ {action} Successful!\n\n🟢 Status: {status}\nCode: {statusCode}",
                    $"{action} Result",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                if (action == "Validation")
                    btnPost.Enabled = true;
                // sirf validation ke baad post enable hoga
                // sirf validation ke baad post enable hoga
            }
            else
            {
                MessageBox.Show(
                    $"❌ {action} Failed!\n\nRaw Response:\n{rawResponse}",
                    $"{action} Result",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                if (action == "Validation")
                    btnPost.Enabled = false;
            }
        }
        catch
        {
            MessageBox.Show(
                $"⚠️ Could not parse response.\n\nRaw Response:\n{rawResponse}",
                $"{action} Result",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            if (action == "Validation")
                btnPost.Enabled = false;
        }
    }

    private void RecalculateSalesTax(object sender, EventArgs e)
    {
        decimal qty = 0, unitPrice = 0, rate = 0, discount = 0;

        // Qty, UnitPrice, and Discount parse karo
        decimal.TryParse(txtQuantity.Text, out qty);
        decimal.TryParse(txtUnitPrice.Text, out unitPrice);
        decimal.TryParse(txtDiscount.Text, out discount);

        // Rate textbox se % remove karke number nikaal lo
        string rateText = txtProdRate.Text.Replace("%", "").Trim();
        decimal.TryParse(rateText, out rate);

        // 1️⃣ Total Value = Qty * UnitPrice
        decimal totalValue = qty * unitPrice;
        txtTotalValue.Text = totalValue.ToString("N2");

        // 2️⃣ Scenario Code ke hisaab se Sales Tax calculate karo
        string scenarioCode = cmbScenario.SelectedItem?.ToString() ?? "DEFAULT";
        decimal salesTax = 0;
        decimal discountedValue = totalValue - discount;
        if (discountedValue < 0) discountedValue = 0;
        decimal valueExclGST = discountedValue;

        switch (scenarioCode)
        {
            case "SN001": // Scenario 1: Standard Product (Rate from product)
                          // Inclusive nahi, simple % tax
                salesTax = discountedValue * rate / 100;
                valueExclGST = discountedValue;
                break;

            case "SN002": // Scenario 2: Services (Rate from product)
                          // Agar inclusive GST ho to:
                valueExclGST = discountedValue / (1 + rate / 100);
                salesTax = discountedValue - valueExclGST;
                break;

            default: // Fallbacka
                salesTax = discountedValue * rate / 100;
                valueExclGST = discountedValue;
                break;
        }

        // 3️⃣ Textboxes update karo
        txtValueExclGST.Text = valueExclGST.ToString("N2");
        txtSalesTaxAmount.Text = salesTax.ToString("N2");
    }

    private bool EnsureSroForItems(out string errorMessage)
    {
        // Ensure the sro column exists in the grid so we can set values
        if (!dgvItems.Columns.Contains("sroScheduleNo"))
        {
            dgvItems.Columns.Add("sroScheduleNo", "SRO/Schedule No");
        }

        int rowNo = 1;
        foreach (DataGridViewRow row in dgvItems.Rows)
        {
            if (row.IsNewRow) { rowNo++; continue; }

            string rateText = row.Cells["Rate"].Value?.ToString() ?? "";
            decimal rate = 0m;
            decimal.TryParse(rateText.Replace("%", "").Trim(), out rate);

            // If rate is not 18% ensure there's an SRO value; auto-fill with the example SRO values if missing
            if (rate != 18m)
            {
                string sro = row.Cells["sroScheduleNo"]?.Value?.ToString();
                if (string.IsNullOrWhiteSpace(sro))
                {
                    // default to the example schedule used in Postman
                    row.Cells["sroScheduleNo"].Value = "EIGHTH SCHEDULE TABLE I";//"ICTO TABLE I";
                }
             }
             rowNo++;
        }

        errorMessage = null;
        return true;
    }

    // Try to extract invoiceNumber or similar from API response string (robust recursive search)
    private string TryExtractInvoiceNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        int idx = raw.IndexOf('{');
        string json = idx >= 0 ? raw.Substring(idx) : raw;
        try
        {
            JToken token = JToken.Parse(json);
            // common keys
            var candidates = new[] { "invoiceNumber", "fbrInvoiceNumber", "invoiceNo", "validationResponse.invoiceNumber" };
            foreach (var key in candidates)
            {
                // support dotted path
                if (key.Contains('.'))
                {
                    var val = token.SelectToken(key);
                    if (val != null && !string.IsNullOrWhiteSpace(val.ToString())) return val.ToString();
                }
                else
                {
                    var found = FindTokenByName(token, key);
                    if (found != null && !string.IsNullOrWhiteSpace(found.ToString())) return found.ToString();
                }
            }

            // fallback: search any property named invoiceNumber (case-insensitive)
            var any = FindTokenByNameCaseInsensitive(token, "invoiceNumber");
            if (any != null) return any.ToString();
        }
        catch
        {
            // ignore parse errors
        }
        return null;
    }

    private JToken FindTokenByName(JToken token, string name)
    {
        if (token == null) return null;
        if (token.Type == JTokenType.Object)
        {
            foreach (var prop in token.Children<JProperty>())
            {
                if (prop.Name == name) return prop.Value;
                var rec = FindTokenByName(prop.Value, name);
                if (rec != null) return rec;
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var child in token.Children())
            {
                var rec = FindTokenByName(child, name);
                if (rec != null) return rec;
            }
        }
        return null;
    }

    private JToken FindTokenByNameCaseInsensitive(JToken token, string name)
    {
        if (token == null) return null;
        if (token.Type == JTokenType.Object)
        {
            foreach (var prop in token.Children<JProperty>())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)) return prop.Value;
                var rec = FindTokenByNameCaseInsensitive(prop.Value, name);
                if (rec != null) return rec;
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var child in token.Children())
            {
                var rec = FindTokenByNameCaseInsensitive(child, name);
                if (rec != null) return rec;
            }
        }
        return null;
    }
}