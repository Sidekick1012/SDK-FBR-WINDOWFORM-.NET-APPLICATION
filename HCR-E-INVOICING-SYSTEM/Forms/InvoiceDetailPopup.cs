using HCR_E_INVOICING_SYSTEM.Data;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

public class InvoiceDetailPopup : Form
{
    private readonly int _invoiceId;

    public InvoiceDetailPopup(int invoiceId)
    {
        _invoiceId = invoiceId;
        this.Text = "Invoice Detail";
        this.Size = new Size(1000, 720);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(245, 247, 252);
        this.Font = new Font("Segoe UI", 9.5f);
        this.MinimumSize = new Size(860, 600);

        BuildUI();
    }

    private void BuildUI()
    {
        DataSet ds = DatabaseHelper.GetInvoicePreviewData(_invoiceId);
        DataTable payments = DatabaseHelper.GetPayments(_invoiceId);

        if (ds == null || ds.Tables["InvoiceHeader"].Rows.Count == 0)
        {
            this.Controls.Add(new Label { Text = "Invoice not found.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 14) });
            return;
        }

        DataRow h = ds.Tables["InvoiceHeader"].Rows[0];
        DataTable items = ds.Tables["InvoiceItems"];

        // ===== OUTER LAYOUT =====
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.FromArgb(245, 247, 252)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Header
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Seller + Customer
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));   // Section title: Items
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Items grid
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Payment + Totals

        // ===== HEADER =====
        string statusColor = h["status"].ToString() == "Paid" ? "#1D9348" : "#C0392B";
        string postColor   = h["postStatus"].ToString() == "Posted" ? "#1D9348" : "#E67E22";

        var header = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#1D2068") };
        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        headerLayout.Controls.Add(MakeHeaderLabel($"🧾 {h["invoiceNumber"]}", 16, FontStyle.Bold, ContentAlignment.MiddleLeft, new Padding(16, 0, 0, 0)), 0, 0);
        headerLayout.Controls.Add(MakeBadgeLabel($"💳 {h["status"]}", statusColor), 1, 0);
        headerLayout.Controls.Add(MakeBadgeLabel($"📤 {h["postStatus"]}", postColor), 2, 0);
        header.Controls.Add(headerLayout);
        outer.Controls.Add(header, 0, 0);

        // ===== SELLER + CUSTOMER CARDS =====
        var infoRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(12, 10, 12, 4)
        };
        infoRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        infoRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var sellerCard = MakeInfoCard("🏷️ Seller Information", new[]
        {
            ("Business Name", h["sellerBusinessName"].ToString()),
            ("NTN / CNIC",    h["sellerNTNCNIC"].ToString()),
            ("Province",      h["sellerProvince"].ToString()),
            ("Address",       h["sellerAddress"].ToString())
        });

        var customerCard = MakeInfoCard("👤 Customer Information", new[]
        {
            ("Business Name",      h["customerBusinessName"].ToString()),
            ("NTN / CNIC",         h["customerNTNCNIC"].ToString()),
            ("Province",           h["customerProvince"].ToString()),
            ("Address",            h["customerAddress"].ToString()),
            ("Registration Type",  h["registrationType"].ToString())
        });

        infoRow.Controls.Add(sellerCard, 0, 0);
        infoRow.Controls.Add(customerCard, 1, 0);
        outer.Controls.Add(infoRow, 0, 1);

        // ===== ITEMS SECTION TITLE =====
        var lblItems = new Label
        {
            Text = "  📦 Invoice Items",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1D2068"),
            BackColor = ColorTranslator.FromHtml("#ECEEF8"),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };
        outer.Controls.Add(lblItems, 0, 2);

        // ===== ITEMS GRID =====
        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowTemplate = { Height = 32 },
            ColumnHeadersHeight = 40,
            RowHeadersVisible = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };
        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#1D2068");
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
        dgv.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 60);
        dgv.DefaultCellStyle.BackColor = Color.White;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 250);
        dgv.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#C8A84B");
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

        // Build a clean version of items
        var cleanItems = new DataTable();
        cleanItems.Columns.Add("HS Code");
        cleanItems.Columns.Add("Description");
        cleanItems.Columns.Add("UoM");
        cleanItems.Columns.Add("Qty");
        cleanItems.Columns.Add("Unit Price (PKR)");
        cleanItems.Columns.Add("Sales Tax (PKR)");
        cleanItems.Columns.Add("Further Tax (PKR)");
        cleanItems.Columns.Add("Discount (PKR)");
        cleanItems.Columns.Add("Total (PKR)");
        cleanItems.Columns.Add("Sale Type");

        foreach (DataRow r in items.Rows)
        {
            cleanItems.Rows.Add(
                r["hsCode"],
                r["description"],
                r["uoM"],
                r["quantity"],
                FormatDecimal(r["unitPrice"]),
                FormatDecimal(r["salesTaxApplicable"]),
                FormatDecimal(r["furtherTax"]),
                FormatDecimal(r["discount"]),
                FormatDecimal(r["totalValues"]),
                r["saleType"]
            );
        }
        dgv.DataSource = cleanItems;

        // Align number columns right
        dgv.DataBindingComplete += (s, e) =>
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                string n = col.Name.ToLower();
                if (n.Contains("pkr") || n.Contains("price") || n.Contains("tax") || n.Contains("total") || n.Contains("discount") || n.Contains("qty"))
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        };
        outer.Controls.Add(dgv, 0, 3);

        // ===== BOTTOM: PAYMENT + TOTALS =====
        var bottomRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(12, 6, 12, 12)
        };
        bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        // Payment Card
        string paymentText = "No payment recorded.";
        string payMethod = "", payCheck = "", payBank = "", payDate = "";
        if (payments != null && payments.Rows.Count > 0)
        {
            DataRow p = payments.Rows[0];
            payMethod = p["method"].ToString();
            payCheck  = p["checkNo"] != DBNull.Value ? p["checkNo"].ToString() : "";
            payBank   = p["bankName"] != DBNull.Value ? p["bankName"].ToString() : "";
            payDate   = p["paymentDate"].ToString();
            paymentText = "";
        }

        var paymentFields = paymentText == "" ? new[]
        {
            ("Method",       payMethod),
            ("Date",         payDate),
            payMethod == "Cheque"        ? ("Cheque No",    payCheck) :
            payMethod == "Bank Transfer" ? ("Account/IBAN", payCheck) : ("", ""),
            ("Bank Name",    payBank)
        } : Array.Empty<(string, string)>();

        Panel payCard;
        if (paymentText != "")
        {
            payCard = MakeInfoCard("💰 Payment Information", Array.Empty<(string, string)>(), paymentText);
        }
        else
        {
            payCard = MakeInfoCard("💰 Payment Information", paymentFields);
        }
        bottomRow.Controls.Add(payCard, 0, 0);

        // Totals Card
        var totalsCard = MakeInfoCard("📊 Invoice Totals", new[]
        {
            ("Invoice Date",   Convert.ToDateTime(h["invoiceDate"]).ToString("yyyy-MM-dd")),
            ("FBR Invoice No", h["fbrInvoiceNumber"] != DBNull.Value ? h["fbrInvoiceNumber"].ToString() : "-"),
            ("Scenario",       h["scenarioId"].ToString()),
            ("Sub Total",      $"{FormatDecimal(h["subTotal"])} PKR"),
            ("Total Tax",      $"{FormatDecimal(h["totalTax"])} PKR"),
            ("Discount",       $"{FormatDecimal(h["discount"])} PKR"),
            ("Grand Total",    $"{FormatDecimal(h["grandTotal"])} PKR")
        }, highlightLast: true);
        bottomRow.Controls.Add(totalsCard, 1, 0);

        outer.Controls.Add(bottomRow, 0, 4);

        this.Controls.Add(outer);
    }

    // ===== HELPERS =====
    private string FormatDecimal(object val)
    {
        if (val == null || val == DBNull.Value) return "0.00";
        return Convert.ToDecimal(val).ToString("N2");
    }

    private Label MakeHeaderLabel(string text, float size, FontStyle style, ContentAlignment align, Padding padding = default)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", size, style),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = align,
            Padding = padding
        };
    }

    private Label MakeBadgeLabel(string text, string hex)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml(hex),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private Panel MakeInfoCard(string title, (string label, string value)[] fields, string emptyMessage = "", bool highlightLast = false)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(5),
            Padding = new Padding(14, 10, 14, 10),
            AutoSize = true
        };
        card.Paint += (s, e) =>
        {
            using (var pen = new Pen(Color.FromArgb(210, 215, 235), 1))
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            using (var b = new SolidBrush(ColorTranslator.FromHtml("#1D2068")))
                e.Graphics.FillRectangle(b, 0, 0, 4, card.Height);
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Title
        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1D2068"),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.SetColumnSpan(lblTitle, 2);
        layout.Controls.Add(lblTitle);

        if (!string.IsNullOrEmpty(emptyMessage))
        {
            var lbl = new Label { Text = emptyMessage, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9, FontStyle.Italic), Dock = DockStyle.Fill };
            layout.SetColumnSpan(lbl, 2);
            layout.Controls.Add(lbl);
        }

        int row = 1;
        foreach (var (label, value) in fields)
        {
            if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value)) continue;
            bool isLast = highlightLast && row == fields.Length;

            var lblKey = new Label
            {
                Text = label + ":",
                Font = new Font("Segoe UI", isLast ? 9.5f : 9f, isLast ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 100, 130),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 2)
            };
            var lblVal = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", isLast ? 10f : 9f, isLast ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isLast ? ColorTranslator.FromHtml("#1D9348") : Color.FromArgb(30, 30, 60),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 2)
            };

            layout.Controls.Add(lblKey);
            layout.Controls.Add(lblVal);
            row++;
        }

        card.Controls.Add(layout);
        return card;
    }
}
