using Sidekick_E_Invoicing.Data;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using OfficeOpenXml;
using OfficeOpenXml.Style;

public class ReportingForm : Form
{
    private ComboBox cmbReportType;
    private DateTimePicker dtpFrom, dtpTo;
    private Button btnGenerate, btnExportCsv, btnPrint;
    private DataGridView dgvReport;
    private Label lblTotalLabel, lblTotal;
    private Panel pnlSummary;

    public ReportingForm()
    {
        this.Text = "Reports";
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.FromArgb(245, 247, 252);
        this.Font = new Font("Segoe UI", 9.5f);
        this.Load += (s, e) => FormTransitionHelper.AnimateFadeIn(this);

        // ===== MAIN LAYOUT =====
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.FromArgb(245, 247, 252)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));  // Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));  // Filters
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));  // Summary
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Grid

        // ===== HEADER =====
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorTranslator.FromHtml("#1b6656")
        };
        var lblHeader = new Label
        {
            Text = "📊 Reports & Analytics",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter
        };
        header.Controls.Add(lblHeader);
        mainLayout.Controls.Add(header, 0, 0);

        // ===== FILTER BAR =====
        var filterPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20, 0, 20, 0)
        };
        var filterLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1,
            BackColor = Color.White
        };
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // label
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); // combo
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));  // from label
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155)); // from picker
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));  // to label
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155)); // to picker
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // buttons

        filterLayout.Controls.Add(MakeFilterLabel("Report Type:"), 0, 0);
        cmbReportType = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(0, 16, 10, 10)
        };
        cmbReportType.Items.AddRange(new string[] {
            "Sales Summary by Date",
            "Invoice List (All)",
            "Payment Report",
            "Top Products by Revenue"
        });
        cmbReportType.SelectedIndex = 0;
        filterLayout.Controls.Add(cmbReportType, 1, 0);

        filterLayout.Controls.Add(MakeFilterLabel("From:"), 2, 0);
        dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(0, 16, 10, 10),
            Value = DateTime.Now.AddMonths(-1)
        };
        filterLayout.Controls.Add(dtpFrom, 3, 0);

        filterLayout.Controls.Add(MakeFilterLabel("To:"), 4, 0);
        dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(0, 16, 10, 10),
            Value = DateTime.Now
        };
        filterLayout.Controls.Add(dtpTo, 5, 0);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10, 14, 0, 6),
            BackColor = Color.White
        };
        btnGenerate  = MakeBtn("🔍 Generate", "#1b6656");
        btnExportCsv = MakeBtn("📥 Export Excel", "#1D9348");
        btnGenerate.Click  += (s, e) => GenerateReport();
        btnExportCsv.Click += (s, e) => ExportExcelReport();
        btnPanel.Controls.AddRange(new Control[] { btnGenerate, btnExportCsv });

        filterLayout.Controls.Add(btnPanel, 6, 0);
        filterPanel.Controls.Add(filterLayout);
        mainLayout.Controls.Add(filterPanel, 0, 1);

        // ===== SUMMARY BAR =====
        pnlSummary = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorTranslator.FromHtml("#ECEEF8"),
            Padding = new Padding(20, 0, 20, 0)
        };
        var summaryLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };
        lblTotalLabel = new Label
        {
            Text = "Total:",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1b6656"),
            AutoSize = true,
            Margin = new Padding(0, 13, 6, 0)
        };
        lblTotal = new Label
        {
            Text = "-",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1D9348"),
            AutoSize = true,
            Margin = new Padding(0, 13, 0, 0)
        };
        summaryLayout.Controls.Add(lblTotalLabel);
        summaryLayout.Controls.Add(lblTotal);
        pnlSummary.Controls.Add(summaryLayout);
        mainLayout.Controls.Add(pnlSummary, 0, 2);

        // ===== DATA GRID =====
        dgvReport = new DataGridView
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
            RowHeadersVisible = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };
        dgvReport.EnableHeadersVisualStyles = false;
        dgvReport.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#1b6656");
        dgvReport.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgvReport.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvReport.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
        dgvReport.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 60);
        dgvReport.DefaultCellStyle.BackColor = Color.White;
        dgvReport.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 250);
        dgvReport.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#C8A84B");
        dgvReport.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvReport.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

        // Double-click row to open full invoice detail (Invoice List only)
        dgvReport.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex < 0) return;
            if (cmbReportType.Text != "Invoice List (All)") return;
            var row = dgvReport.Rows[e.RowIndex];
            if (row.Cells["invoiceId"] == null || row.Cells["invoiceId"].Value == null) return;
            int invoiceId = Convert.ToInt32(row.Cells["invoiceId"].Value);
            var popup = new InvoiceDetailPopup(invoiceId);
            popup.ShowDialog(this);
        };

        // Hint label
        var lblHint = new Label
        {
            Text = "💡 Double-click a row in Invoice List to view full details",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(120, 120, 160),
            AutoSize = true,
            Margin = new Padding(20, 13, 0, 0)
        };
        summaryLayout.Controls.Add(lblHint);

        mainLayout.Controls.Add(dgvReport, 0, 3);

        this.Controls.Add(mainLayout);
    }

    // ===== GENERATE REPORT =====
    private void GenerateReport()
    {
        string from = dtpFrom.Value.ToString("yyyy-MM-dd");
        string to   = dtpTo.Value.ToString("yyyy-MM-dd 23:59:59");

        if (dtpFrom.Value > dtpTo.Value)
        {
            MessageBox.Show("'From' date cannot be after 'To' date.", "Date Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            DataTable dt = null;
            string report = cmbReportType.Text;

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                if (report == "Sales Summary by Date")
                {
                    string sql = @"
SELECT 
    substr(invoiceDate,1,10) AS [Date],
    COUNT(*) AS [Invoices],
    SUM(grandTotal) AS [Total Revenue (PKR)],
    SUM(CASE WHEN status='Paid' THEN grandTotal ELSE 0 END) AS [Paid (PKR)],
    SUM(CASE WHEN status='Unpaid' THEN grandTotal ELSE 0 END) AS [Unpaid (PKR)]
FROM Invoices
WHERE substr(invoiceDate,1,10) BETWEEN @from AND @to2
GROUP BY substr(invoiceDate,1,10)
ORDER BY substr(invoiceDate,1,10) DESC";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to2", dtpTo.Value.ToString("yyyy-MM-dd"));
                        using (var da = new SQLiteDataAdapter(cmd)) { dt = new DataTable(); da.Fill(dt); }
                    }
                    ShowSummary("Total Revenue", dt, "Total Revenue (PKR)");
                }
                else if (report == "Invoice List (All)")
                {
                    string sql = @"
SELECT 
    i.invoiceId AS [invoiceId],
    i.invoiceNumber AS [Invoice No],
    substr(i.invoiceDate,1,10) AS [Date],
    c.customerBusinessName AS [Customer],
    i.scenarioId AS [Scenario],
    i.grandTotal AS [Grand Total (PKR)],
    i.status AS [Payment Status],
    i.postStatus AS [Post Status]
FROM Invoices i
LEFT JOIN Customers c ON i.customerId = c.customerId
WHERE substr(i.invoiceDate,1,10) BETWEEN @from AND @to2
ORDER BY substr(i.invoiceDate,1,10) DESC";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to2", dtpTo.Value.ToString("yyyy-MM-dd"));
                        using (var da = new SQLiteDataAdapter(cmd)) { dt = new DataTable(); da.Fill(dt); }
                    }
                    ShowSummary("Total Grand Total", dt, "Grand Total (PKR)");
                }
                else if (report == "Payment Report")
                {
                    string sql = @"
SELECT 
    i.invoiceNumber AS [Invoice No],
    substr(p.paymentDate,1,10) AS [Payment Date],
    p.method AS [Method],
    p.checkNo AS [Cheque/Account No],
    p.bankName AS [Bank Name],
    p.amount AS [Amount (PKR)],
    p.status AS [Status]
FROM Payments p
JOIN Invoices i ON p.invoiceId = i.invoiceId
WHERE substr(p.paymentDate,1,10) BETWEEN @from AND @to2
ORDER BY substr(p.paymentDate,1,10) DESC";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to2", dtpTo.Value.ToString("yyyy-MM-dd"));
                        using (var da = new SQLiteDataAdapter(cmd)) { dt = new DataTable(); da.Fill(dt); }
                    }
                    ShowSummary("Total Payments Received", dt, "Amount (PKR)");
                }
                else if (report == "Top Products by Revenue")
                {
                    string sql = @"
SELECT 
    p.productDescription AS [Product],
    p.hsCode AS [HS Code],
    COUNT(ii.itemId) AS [Times Sold],
    SUM(ii.quantity) AS [Total Qty],
    SUM(ii.totalValues) AS [Total Revenue (PKR)]
FROM InvoiceItems ii
JOIN Products p ON ii.productId = p.productId
JOIN Invoices i ON ii.invoiceId = i.invoiceId
WHERE substr(i.invoiceDate,1,10) BETWEEN @from AND @to2
GROUP BY p.productId, p.productDescription, p.hsCode
ORDER BY SUM(ii.totalValues) DESC
LIMIT 50";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to2", dtpTo.Value.ToString("yyyy-MM-dd"));
                        using (var da = new SQLiteDataAdapter(cmd)) { dt = new DataTable(); da.Fill(dt); }
                    }
                    ShowSummary("Total Revenue", dt, "Total Revenue (PKR)");
                }
            }

            dgvReport.DataSource = dt;
            StyleGrid();

            if (dt == null || dt.Rows.Count == 0)
                lblTotal.Text = "No data found for selected range.";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error generating report: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowSummary(string label, DataTable dt, string columnName)
    {
        if (dt == null || !dt.Columns.Contains(columnName))
        { lblTotalLabel.Text = "Records:"; lblTotal.Text = (dt?.Rows.Count ?? 0).ToString(); return; }

        decimal sum = 0;
        foreach (DataRow row in dt.Rows)
        {
            if (row[columnName] != DBNull.Value)
                sum += Convert.ToDecimal(row[columnName]);
        }
        lblTotalLabel.Text = $"{label}:";
        lblTotal.Text = $"{sum:N2} PKR   |   Records: {dt.Rows.Count}";
    }

    private void StyleGrid()
    {
        foreach (DataGridViewColumn col in dgvReport.Columns)
        {
            string name = col.Name.ToLower();
            // Hide the internal invoiceId column
            if (name == "invoiceid") { col.Visible = false; continue; }

            if (name.Contains("pkr") || name.Contains("total") || name.Contains("amount") || name.Contains("revenue"))
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.DefaultCellStyle.Format = "N2";
            }
            else if (name.Contains("qty") || name.Contains("times") || name.Contains("count") || name.Contains("invoices"))
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            else
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
        }
    }

    // ===== EXPORT EXCEL REPORT =====
    private void ExportExcelReport()
    {
        if (dgvReport.DataSource == null || dgvReport.Rows.Count == 0)
        { MessageBox.Show("Please generate a report first.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        string fileName = "";
        using (var dlg = new SaveFileDialog())
        {
            dlg.Filter = "Excel Worksheets (*.xlsx)|*.xlsx";
            dlg.FileName = $"Sidekick_Report_{cmbReportType.Text.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            dlg.Title = "Save Excel Report";
            if (dlg.ShowDialog() != DialogResult.OK) return;
            fileName = dlg.FileName;
        }

        bool isInvoiceList = cmbReportType.Text == "Invoice List (All)";

        using (var package = new ExcelPackage())
        {
            var sheet = package.Workbook.Worksheets.Add("Report");
            sheet.View.ShowGridLines = true;

            // Set column widths
            if (isInvoiceList)
            {
                sheet.Column(1).Width = 14;  // HS Code
                sheet.Column(2).Width = 32;  // Description
                sheet.Column(3).Width = 10;  // UoM
                sheet.Column(4).Width = 10;  // Qty
                sheet.Column(5).Width = 15;  // Unit Price
                sheet.Column(6).Width = 14;  // Sales Tax
                sheet.Column(7).Width = 14;  // Further Tax
                sheet.Column(8).Width = 14;  // Discount
                sheet.Column(9).Width = 18;  // Total Values
                sheet.Column(10).Width = 15; // Sale Type
            }

            // Draw Header Title Block
            sheet.Cells[1, 1].Value = $"📊 Sidekick E-Invoicing Report — {cmbReportType.Text}";
            sheet.Cells[1, 1, 1, 10].Merge = true;
            sheet.Cells[1, 1].Style.Font.Name = "Segoe UI";
            sheet.Cells[1, 1].Style.Font.Size = 16;
            sheet.Cells[1, 1].Style.Font.Bold = true;
            sheet.Cells[1, 1].Style.Font.Color.SetColor(ColorTranslator.FromHtml("#1b6656"));
            sheet.Row(1).Height = 28;

            sheet.Cells[2, 1].Value = $"Period: {dtpFrom.Value:yyyy-MM-dd} to {dtpTo.Value:yyyy-MM-dd}  |  Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
            sheet.Cells[2, 1, 2, 10].Merge = true;
            sheet.Cells[2, 1].Style.Font.Name = "Segoe UI";
            sheet.Cells[2, 1].Style.Font.Size = 10;
            sheet.Cells[2, 1].Style.Font.Color.SetColor(Color.Gray);
            sheet.Row(2).Height = 18;

            sheet.Cells[3, 1].Value = $"{lblTotalLabel.Text} {lblTotal.Text}";
            sheet.Cells[3, 1, 3, 10].Merge = true;
            sheet.Cells[3, 1].Style.Font.Name = "Segoe UI";
            sheet.Cells[3, 1].Style.Font.Size = 11;
            sheet.Cells[3, 1].Style.Font.Bold = true;
            sheet.Cells[3, 1].Style.Font.Color.SetColor(ColorTranslator.FromHtml("#1b6656"));
            sheet.Cells[3, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells[3, 1].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#ECEEF8"));
            sheet.Row(3).Height = 22;

            int rowIdx = 5;

            if (!isInvoiceList)
            {
                // ===== SIMPLE TABULAR REPORT =====
                int colCount = 0;
                foreach (DataGridViewColumn col in dgvReport.Columns)
                    if (col.Visible) colCount++;

                // Draw Table Headers
                int cIdx = 1;
                foreach (DataGridViewColumn col in dgvReport.Columns)
                {
                    if (!col.Visible) continue;
                    var cell = sheet.Cells[rowIdx, cIdx];
                    cell.Value = col.HeaderText;
                    cell.Style.Font.Name = "Segoe UI";
                    cell.Style.Font.Size = 10.5f;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#1b6656"));
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cIdx++;
                }
                sheet.Row(rowIdx).Height = 24;
                rowIdx++;

                // Draw Data Rows
                foreach (DataGridViewRow row in dgvReport.Rows)
                {
                    if (row.IsNewRow) continue;
                    cIdx = 1;
                    sheet.Row(rowIdx).Height = 20;

                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (!dgvReport.Columns[cell.ColumnIndex].Visible) continue;
                        var excelCell = sheet.Cells[rowIdx, cIdx];
                        excelCell.Value = cell.Value;

                        // Formatting
                        excelCell.Style.Font.Name = "Segoe UI";
                        excelCell.Style.Font.Size = 10;
                        excelCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        excelCell.Style.Border.Top.Color.SetColor(ColorTranslator.FromHtml("#D0D4EE"));
                        excelCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        excelCell.Style.Border.Bottom.Color.SetColor(ColorTranslator.FromHtml("#D0D4EE"));
                        excelCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        excelCell.Style.Border.Left.Color.SetColor(ColorTranslator.FromHtml("#D0D4EE"));
                        excelCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        excelCell.Style.Border.Right.Color.SetColor(ColorTranslator.FromHtml("#D0D4EE"));

                        if (rowIdx % 2 == 0)
                        {
                            excelCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            excelCell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#F8F9FE"));
                        }

                        if (cell.Value is decimal || cell.Value is double || cell.Value is float)
                        {
                            excelCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            excelCell.Style.Numberformat.Format = "#,##0.00";
                        }
                        else if (cell.Value is int || cell.Value is long)
                        {
                            excelCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }
                        else
                        {
                            excelCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        }

                        cIdx++;
                    }
                    rowIdx++;
                }

                sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
            }
            else
            {
                // ===== INVOICE LIST WITH FULL DETAILS =====
                foreach (DataGridViewRow row in dgvReport.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (row.Cells["invoiceId"]?.Value == null) continue;
                    int invoiceId = Convert.ToInt32(row.Cells["invoiceId"].Value);

                    DataSet ds = DatabaseHelper.GetInvoicePreviewData(invoiceId);
                    DataTable payments = DatabaseHelper.GetPayments(invoiceId);
                    if (ds == null || ds.Tables["InvoiceHeader"].Rows.Count == 0) continue;

                    DataRow h = ds.Tables["InvoiceHeader"].Rows[0];
                    DataTable items = ds.Tables["InvoiceItems"];

                    string statusText = h["status"].ToString();
                    string postText = h["postStatus"].ToString();

                    // Invoice Header Row
                    var invHeaderCell = sheet.Cells[rowIdx, 1, rowIdx, 10];
                    invHeaderCell.Merge = true;
                    invHeaderCell.Value = $"🧾 Invoice No: {h["invoiceNumber"]}      [Status: {statusText} | {postText}]";
                    invHeaderCell.Style.Font.Name = "Segoe UI";
                    invHeaderCell.Style.Font.Size = 12;
                    invHeaderCell.Style.Font.Bold = true;
                    invHeaderCell.Style.Font.Color.SetColor(Color.White);
                    invHeaderCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    invHeaderCell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#1b6656"));
                    invHeaderCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    sheet.Row(rowIdx).Height = 26;
                    rowIdx++;

                    // Seller & Customer Section Headers
                    var sellerHeader = sheet.Cells[rowIdx, 1, rowIdx, 5];
                    sellerHeader.Merge = true;
                    sellerHeader.Value = "🏷️ Seller Information";
                    sellerHeader.Style.Font.Name = "Segoe UI";
                    sellerHeader.Style.Font.Size = 11;
                    sellerHeader.Style.Font.Bold = true;
                    sellerHeader.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#1b6656"));
                    sellerHeader.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sellerHeader.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#ECEEF8"));
                    sellerHeader.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));

                    var customerHeader = sheet.Cells[rowIdx, 6, rowIdx, 10];
                    customerHeader.Merge = true;
                    customerHeader.Value = "👤 Customer Information";
                    customerHeader.Style.Font.Name = "Segoe UI";
                    customerHeader.Style.Font.Size = 11;
                    customerHeader.Style.Font.Bold = true;
                    customerHeader.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#1b6656"));
                    customerHeader.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    customerHeader.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#ECEEF8"));
                    customerHeader.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));
                    sheet.Row(rowIdx).Height = 22;
                    rowIdx++;

                    // Helper for side-by-side details
                    Action<string, object, string, object> drawInfoRow = (lbl1, val1, lbl2, val2) =>
                    {
                        sheet.Row(rowIdx).Height = 20;

                        var cellLbl1 = sheet.Cells[rowIdx, 1];
                        cellLbl1.Value = lbl1;
                        cellLbl1.Style.Font.Name = "Segoe UI";
                        cellLbl1.Style.Font.Size = 9.5f;
                        cellLbl1.Style.Font.Color.SetColor(Color.FromArgb(80, 80, 80));
                        cellLbl1.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cellLbl1.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#F8F9FE"));
                        cellLbl1.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));

                        var cellVal1 = sheet.Cells[rowIdx, 2, rowIdx, 5];
                        cellVal1.Merge = true;
                        cellVal1.Value = val1;
                        cellVal1.Style.Font.Name = "Segoe UI";
                        cellVal1.Style.Font.Size = 9.5f;
                        cellVal1.Style.Font.Bold = true;
                        cellVal1.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));

                        var cellLbl2 = sheet.Cells[rowIdx, 6];
                        cellLbl2.Value = lbl2;
                        cellLbl2.Style.Font.Name = "Segoe UI";
                        cellLbl2.Style.Font.Size = 9.5f;
                        cellLbl2.Style.Font.Color.SetColor(Color.FromArgb(80, 80, 80));
                        cellLbl2.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cellLbl2.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#F8F9FE"));
                        cellLbl2.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));

                        var cellVal2 = sheet.Cells[rowIdx, 7, rowIdx, 10];
                        cellVal2.Merge = true;
                        cellVal2.Value = val2;
                        cellVal2.Style.Font.Name = "Segoe UI";
                        cellVal2.Style.Font.Size = 9.5f;
                        cellVal2.Style.Font.Bold = true;
                        cellVal2.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));

                        rowIdx++;
                    };

                    drawInfoRow("Business Name:", h["sellerBusinessName"], "Business Name:", h["customerBusinessName"]);
                    drawInfoRow("NTN / CNIC:", h["sellerNTNCNIC"], "NTN / CNIC:", h["customerNTNCNIC"]);
                    drawInfoRow("Province:", h["sellerProvince"], "Province:", h["customerProvince"]);
                    drawInfoRow("Address:", h["sellerAddress"], "Address:", h["customerAddress"]);
                    drawInfoRow("", "", "Reg. Type:", h["registrationType"]);

                    // Blank separator row
                    rowIdx++;

                    // Items Header Row
                    string[] colHeaders = { "HS Code", "Description", "UoM", "Qty", "Unit Price", "Sales Tax", "Further Tax", "Discount", "Total (PKR)", "Sale Type" };
                    for (int i = 0; i < colHeaders.Length; i++)
                    {
                        var cell = sheet.Cells[rowIdx, i + 1];
                        cell.Value = colHeaders[i];
                        cell.Style.Font.Name = "Segoe UI";
                        cell.Style.Font.Size = 10;
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.Color.SetColor(Color.White);
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#1b6656"));
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));
                    }
                    sheet.Row(rowIdx).Height = 22;
                    rowIdx++;

                    // Item rows
                    int itemIdx = 0;
                    foreach (DataRow it in items.Rows)
                    {
                        sheet.Row(rowIdx).Height = 20;
                        string rowBg = (itemIdx % 2 == 0) ? "#FFFFFF" : "#F8F9FE";

                        for (int i = 0; i < 10; i++)
                        {
                            var cell = sheet.Cells[rowIdx, i + 1];
                            cell.Style.Font.Name = "Segoe UI";
                            cell.Style.Font.Size = 9.5f;
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(rowBg));
                            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));
                        }

                        sheet.Cells[rowIdx, 1].Value = it["hsCode"];
                        sheet.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        sheet.Cells[rowIdx, 2].Value = it["description"];
                        sheet.Cells[rowIdx, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                        sheet.Cells[rowIdx, 3].Value = it["uoM"];
                        sheet.Cells[rowIdx, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        sheet.Cells[rowIdx, 4].Value = it["quantity"];
                        sheet.Cells[rowIdx, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        sheet.Cells[rowIdx, 5].Value = it["unitPrice"];
                        sheet.Cells[rowIdx, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        sheet.Cells[rowIdx, 5].Style.Numberformat.Format = "#,##0.00";

                        sheet.Cells[rowIdx, 6].Value = it["salesTaxApplicable"];
                        sheet.Cells[rowIdx, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        sheet.Cells[rowIdx, 6].Style.Numberformat.Format = "#,##0.00";

                        sheet.Cells[rowIdx, 7].Value = it["furtherTax"];
                        sheet.Cells[rowIdx, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        sheet.Cells[rowIdx, 7].Style.Numberformat.Format = "#,##0.00";

                        sheet.Cells[rowIdx, 8].Value = it["discount"];
                        sheet.Cells[rowIdx, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        sheet.Cells[rowIdx, 8].Style.Numberformat.Format = "#,##0.00";

                        sheet.Cells[rowIdx, 9].Value = it["totalValues"];
                        sheet.Cells[rowIdx, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        sheet.Cells[rowIdx, 9].Style.Numberformat.Format = "#,##0.00";
                        sheet.Cells[rowIdx, 9].Style.Font.Bold = true;

                        sheet.Cells[rowIdx, 10].Value = it["saleType"];
                        sheet.Cells[rowIdx, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                        itemIdx++;
                        rowIdx++;
                    }

                    // Payment and totals block (spans 4 rows)
                    int startPayRow = rowIdx;
                    
                    // Totals Labels and values (Rows 1-4)
                    Action<string, object, bool> drawTotalRow = (lbl, val, isGrand) =>
                    {
                        var lblCell = sheet.Cells[rowIdx, 8, rowIdx, 9];
                        lblCell.Merge = true;
                        lblCell.Value = lbl;
                        lblCell.Style.Font.Name = "Segoe UI";
                        lblCell.Style.Font.Size = 9.5f;
                        lblCell.Style.Font.Bold = true;
                        lblCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        lblCell.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));
                        lblCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        lblCell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(isGrand ? "#ECEEF8" : "#F8F9FE"));
                        if (isGrand) lblCell.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#1b6656"));

                        var valCell = sheet.Cells[rowIdx, 10];
                        valCell.Value = val;
                        valCell.Style.Font.Name = "Segoe UI";
                        valCell.Style.Font.Size = 9.5f;
                        valCell.Style.Font.Bold = true;
                        valCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        valCell.Style.Numberformat.Format = "#,##0.00";
                        valCell.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));
                        valCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        valCell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(isGrand ? "#1D9348" : "#FFFFFF"));
                        if (isGrand) valCell.Style.Font.Color.SetColor(Color.White);

                        sheet.Row(rowIdx).Height = 20;
                        rowIdx++;
                    };

                    drawTotalRow("Sub Total:", h["subTotal"], false);
                    drawTotalRow("Tax:", h["totalTax"], false);
                    drawTotalRow("Discount:", h["discount"], false);
                    drawTotalRow("Grand Total:", h["grandTotal"], true);

                    // Draw payment info in Columns 1-7 merged across rows startPayRow to rowIdx-1
                    var payCell = sheet.Cells[startPayRow, 1, rowIdx - 1, 7];
                    payCell.Merge = true;
                    payCell.Style.Font.Name = "Segoe UI";
                    payCell.Style.Font.Size = 9.5f;
                    payCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    payCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    payCell.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#D0D4EE"));
                    payCell.Style.Fill.PatternType = ExcelFillStyle.Solid;

                    if (payments != null && payments.Rows.Count > 0)
                    {
                        DataRow p = payments.Rows[0];
                        string method = p["method"].ToString();
                        string extra = "";
                        if (method == "Cheque")
                            extra = $"\r\nCheque No: {p["checkNo"]} | Bank: {p["bankName"]}";
                        else if (method == "Bank Transfer")
                            extra = $"\r\nAccount/IBAN: {p["checkNo"]} | Bank: {p["bankName"]}";

                        payCell.Value = $"  ✅ PAID\r\n  Method: {method}\r\n  Date: {Convert.ToDateTime(p["paymentDate"]):yyyy-MM-dd}\r\n  Amount: {Convert.ToDecimal(p["amount"]):N2} PKR{extra}";
                        payCell.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#065f46"));
                        payCell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#f0fdf4"));
                        payCell.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#bbf7d0"));
                    }
                    else
                    {
                        payCell.Value = "  ❌ UNPAID\r\n  No payment recorded yet for this invoice.";
                        payCell.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#991b1b"));
                        payCell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#fff1f2"));
                        payCell.Style.Border.BorderAround(ExcelBorderStyle.Thin, ColorTranslator.FromHtml("#fecdd3"));
                    }

                    // Add an extra empty row after invoice block
                    sheet.Row(rowIdx).Height = 22;
                    rowIdx++;
                }
            }

            // Save file
            var fileInfo = new FileInfo(fileName);
            if (fileInfo.Exists) fileInfo.Delete();
            package.SaveAs(fileInfo);
        }

        try
        {
            System.Diagnostics.Process.Start(fileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Report exported successfully to:\n{fileName}\n\nCould not open automatically: {ex.Message}", "Export Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        MessageBox.Show($"✅ Styled Excel Report saved & opened successfully!\n\nFile saved at:\n{fileName}", "Export Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private string H(object val)
        => System.Net.WebUtility.HtmlEncode(val?.ToString() ?? "");

    private string FmtN(object val)
    {
        if (val == null || val == DBNull.Value) return "0.00";
        try { return Convert.ToDecimal(val).ToString("N2"); } catch { return val.ToString(); }
    }

    // ===== HELPERS =====
    private Label MakeFilterLabel(string text) => new Label
    {
        Text = text,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(60, 60, 90),
        TextAlign = ContentAlignment.MiddleRight,
        Margin = new Padding(0, 16, 8, 10)
    };

    private Button MakeBtn(string text, string hex) => new Button
    {
        Text = text,
        Height = 36,
        Width = 150,
        BackColor = ColorTranslator.FromHtml(hex),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        Margin = new Padding(0, 0, 8, 0),
        Cursor = Cursors.Hand
    };
}
