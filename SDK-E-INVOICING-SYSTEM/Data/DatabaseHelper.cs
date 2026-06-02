using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;

namespace SDK_E_INVOICING_SYSTEM.Data
{
    public static class DatabaseHelper
    {
        public static readonly string ConnectionString;
        private static readonly string DbFolder;

        static DatabaseHelper()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (IsDirectoryWritable(baseDir))
            {
                DbFolder = baseDir;
            }
            else
            {
                DbFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SDKEInvoicing");
                
                try
                {
                    if (!System.IO.Directory.Exists(DbFolder))
                        System.IO.Directory.CreateDirectory(DbFolder);

                    // Migrate database if it exists in baseDir but not in AppData
                    string sourceDb = System.IO.Path.Combine(baseDir, "einvoice.db");
                    string destDb = System.IO.Path.Combine(DbFolder, "einvoice.db");
                    if (System.IO.File.Exists(sourceDb) && !System.IO.File.Exists(destDb))
                    {
                        System.IO.File.Copy(sourceDb, destDb, true);
                    }
                }
                catch { }
            }

            ConnectionString = $"Data Source={System.IO.Path.Combine(DbFolder, "einvoice.db")};Version=3;";
        }

        private static bool IsDirectoryWritable(string dirPath)
        {
            try
            {
                if (!System.IO.Directory.Exists(dirPath))
                    System.IO.Directory.CreateDirectory(dirPath);
                string tempFile = System.IO.Path.Combine(dirPath, System.IO.Path.GetRandomFileName());
                System.IO.File.WriteAllText(tempFile, "test");
                System.IO.File.Delete(tempFile);
                return true;
            }
            catch
            {
                return false;
            }
        }


        


        /// <summary>
        /// Call this once at application startup to ensure DB + tables exist.
        /// </summary>
        public static void InitializeDatabase()
        {
            // Ensure the AppData folder exists before opening the database
            if (!System.IO.Directory.Exists(DbFolder))
                System.IO.Directory.CreateDirectory(DbFolder);

            using (var conn = new SQLiteConnection(ConnectionString))

            {
                conn.Open();

                // Users (for simple authentication)
                string createUsers = @"
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    Password TEXT NOT NULL
);";
                using (var cmd = new SQLiteCommand(createUsers, conn)) cmd.ExecuteNonQuery();

                // Add default admin if not exists
                string checkAdmin = "SELECT COUNT(*) FROM Users WHERE Username='admin';";
                using (var cmd = new SQLiteCommand(checkAdmin, conn))
                {
                    long count = (long)cmd.ExecuteScalar();
                    if (count == 0)
                    {
                        string insertAdmin = "INSERT INTO Users (Username, Password) VALUES ('admin','admin123');";
                        using (var cmdInsert = new SQLiteCommand(insertAdmin, conn))
                            cmdInsert.ExecuteNonQuery();
                    }
                }

                // Customers (master)
                string createCustomers = @"
CREATE TABLE IF NOT EXISTS Customers (
    customerId INTEGER PRIMARY KEY AUTOINCREMENT,          
    customerBusinessName TEXT NOT NULL,          
    customerNTNCNIC TEXT,                
    customerProvince TEXT,
    customerAddress TEXT,
    registrationType TEXT                -- Registered / Unregistered (sirf Buyer ke liye)
);";
                using (var cmd = new SQLiteCommand(createCustomers, conn)) cmd.ExecuteNonQuery();

                // Sellerrs (master)
                string createSellers = @"
CREATE TABLE IF NOT EXISTS Sellers (
    sellerId INTEGER PRIMARY KEY AUTOINCREMENT,          
    sellerBusinessName TEXT NOT NULL,          
    sellerNTNCNIC TEXT,                
    sellerProvince TEXT,

    sellerAddress TEXT,
    registrationType TEXT,               -- Registered / Unregistered (sirf Buyer ke liye)
    token TEXT,                           -- Seller-specific token
    logoPath BLOB,                     -- path of logo (png/jpg)
    invoiceFooter TEXT                 -- seller specific invoice footer
);";

                using (var cmd = new SQLiteCommand(createSellers, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Ensure 'invoiceFooter' column exists
                using (var checkCmd = new SQLiteCommand("PRAGMA table_info('Sellers');", conn))
                {
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        bool hasInvoiceFooter = false;
                        while (reader.Read())
                        {
                            string colName = reader[1].ToString();
                            if (string.Equals(colName, "invoiceFooter", StringComparison.OrdinalIgnoreCase))
                            {
                                hasInvoiceFooter = true;
                                break;
                            }
                        }

                        if (!hasInvoiceFooter)
                        {
                            using (var alter = new SQLiteCommand("ALTER TABLE Sellers ADD COLUMN invoiceFooter TEXT;", conn))
                            {
                                alter.ExecuteNonQuery();
                            }
                        }
                    }
                }



                // Products (master) - FBR friendly (hsCode, description, rate, uoM)
                // Note: rate is TEXT to allow values like '18%'
                string createProducts = @"
CREATE TABLE IF NOT EXISTS Products (
    productId INTEGER PRIMARY KEY AUTOINCREMENT,
    hsCode TEXT NOT NULL,
    productDescription TEXT NOT NULL,
    rate TEXT NOT NULL,
    uoM TEXT NOT NULL
);";
                using (var cmd = new SQLiteCommand(createProducts, conn)) cmd.ExecuteNonQuery();

                // Invoices (header)
                string createInvoices = @"
CREATE TABLE IF NOT EXISTS Invoices (
    invoiceId INTEGER PRIMARY KEY AUTOINCREMENT,   -- internal unique id
    invoiceNumber TEXT NOT NULL UNIQUE,            -- local system invoice no.
    fbrInvoiceNumber TEXT UNIQUE,                  -- returned by FBR after posting
    invoiceDate TEXT NOT NULL,                     -- yyyy-MM-dd HH:mm:ss format
    sellerId INTEGER NOT NULL,                     -- link to Sellers
    customerId INTEGER,                            -- link to Customers
    subTotal REAL DEFAULT 0,                       -- subtotal before tax
    totalTax REAL DEFAULT 0,                       -- total tax amount
    discount REAL DEFAULT 0,                       -- discount
    grandTotal REAL DEFAULT 0,                     -- final total
    notes TEXT,                                    -- additional notes
    status TEXT DEFAULT 'Unpaid',                  -- payment status (Unpaid/Paid)
    postStatus TEXT DEFAULT 'Saved',               -- Saved / Posted / Failed
    FOREIGN KEY(sellerId) REFERENCES Sellers(sellerId),
    FOREIGN KEY(customerId) REFERENCES Customers(customerId)
);";
                using (var cmd = new SQLiteCommand(createInvoices, conn)) cmd.ExecuteNonQuery();

                // InvoiceItems (line items) - full FBR related fields kept here
                string createInvoiceItems = @"
CREATE TABLE IF NOT EXISTS InvoiceItems (
    itemId INTEGER PRIMARY KEY AUTOINCREMENT,
    invoiceId INTEGER NOT NULL,
    productId INTEGER NOT NULL,
    description TEXT,
    quantity REAL NOT NULL,
    rate TEXT,
    unitPrice REAL,
    totalValues REAL,
    valueSalesExcludingST REAL,
    fixedNotifiedValueOrRetailPrice REAL,
    salesTaxApplicable REAL,
    salesTaxWithheldAtSource REAL,
    extraTax REAL,
    furtherTax REAL,
    fedPayable REAL,
    discount REAL,
    saleType TEXT,
    sroItemSerialNo TEXT,
    sroScheduleNo TEXT,
    FOREIGN KEY(invoiceId) REFERENCES Invoices(invoiceId),
    FOREIGN KEY(productId) REFERENCES Products(productId)
);";
                using (var cmd = new SQLiteCommand(createInvoiceItems, conn)) cmd.ExecuteNonQuery();

                // Ensure 'sroScheduleNo' column exists in case DB was created before this change
                using (var checkCmd = new SQLiteCommand("PRAGMA table_info('InvoiceItems');", conn))
                {
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        bool hasSroSchedule = false;
                        while (reader.Read())
                        {
                            string colName = reader[1].ToString();
                            if (string.Equals(colName, "sroScheduleNo", StringComparison.OrdinalIgnoreCase))
                            {
                                hasSroSchedule = true;
                                break;
                            }
                        }

                        if (!hasSroSchedule)
                        {
                            using (var alter = new SQLiteCommand("ALTER TABLE InvoiceItems ADD COLUMN sroScheduleNo TEXT;", conn))
                            {
                                alter.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // Payments
                string createPayments = @"
CREATE TABLE IF NOT EXISTS Payments (
    paymentId INTEGER PRIMARY KEY AUTOINCREMENT,
    invoiceId INTEGER,
    amount REAL,
    method TEXT,
    status TEXT,
    paymentDate TEXT,
    FOREIGN KEY(invoiceId) REFERENCES Invoices(invoiceId)
);";
                using (var cmd = new SQLiteCommand(createPayments, conn)) cmd.ExecuteNonQuery();
            }

            // Perform auto-backup
            BackupDatabase();
        }

        public static void BackupDatabase()
        {
            try
            {
                // Source path (einvoice.db location)
                string sourceFile = System.IO.Path.Combine(DbFolder, "einvoice.db");
                if (!System.IO.File.Exists(sourceFile))
                    return;

                // Destination path (My Documents\SDKEInvoicing_Backups)
                string backupFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "SDKEInvoicing_Backups");

                // Ensure backup folder exists
                if (!System.IO.Directory.Exists(backupFolder))
                    System.IO.Directory.CreateDirectory(backupFolder);

                // Check if we already backed up today to avoid redundancy
                string todayString = DateTime.Now.ToString("yyyyMMdd");
                string todayBackupFile = System.IO.Path.Combine(backupFolder, $"einvoice_backup_{todayString}.db");

                if (!System.IO.File.Exists(todayBackupFile))
                {
                    // Copy file
                    System.IO.File.Copy(sourceFile, todayBackupFile, true);

                    // Clean up older backups (keep only last 10 backups)
                    var files = System.IO.Directory.GetFiles(backupFolder, "einvoice_backup_*.db");
                    if (files.Length > 10)
                    {
                        Array.Sort(files); // Sorts older dates first
                        for (int i = 0; i < files.Length - 10; i++)
                        {
                            try
                            {
                                System.IO.File.Delete(files[i]);
                            }
                            catch { /* Ignore deletion errors */ }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database backup failed: {ex.Message}");
            }
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }

        // ---------------- Products CRUD ----------------
        public static DataTable GetProducts(string filter = "")
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string query = @"SELECT 
                            productId,
                            hsCode,
                            productDescription,
                            rate,
                            uoM
                         FROM Products";

                // Agar search text likha ho tab filter lagayen
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    query += " WHERE hsCode LIKE @filter OR productDescription LIKE @filter";
                }

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.Parameters.AddWithValue("@filter", "%" + filter + "%");
                    }

                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }




        public static void AddProduct(string hsCode, string desc, string rate, string uom)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string query = "INSERT INTO Products (hsCode, productDescription, rate, uoM) VALUES (@hsCode, @desc, @rate, @uom)";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@hsCode", hsCode);
                    cmd.Parameters.AddWithValue("@desc", desc);
                    cmd.Parameters.AddWithValue("@rate", rate);
                    cmd.Parameters.AddWithValue("@uom", uom);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static void UpdateProduct(int id, string hsCode, string desc, string rate, string uom)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string query = "UPDATE Products SET hsCode=@hsCode, productDescription=@desc, rate=@rate, uoM=@uom WHERE productId=@id";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@hsCode", hsCode);
                    cmd.Parameters.AddWithValue("@desc", desc);
                    cmd.Parameters.AddWithValue("@rate", rate);
                    cmd.Parameters.AddWithValue("@uom", uom);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteProduct(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string query = "DELETE FROM Products WHERE productId=@id";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static DataRow GetProductById(int productId)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT productId, hsCode, productDescription, rate, uoM FROM Products WHERE productId=@productId";
                using (var da = new SQLiteDataAdapter(sql, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@productId", productId);
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                }
            }
        }

        // Return productId for given hsCode or -1 if not found
        public static int GetProductIdByHsCode(string hsCode)
        {
            if (string.IsNullOrWhiteSpace(hsCode)) return -1;
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT productId FROM Products WHERE hsCode = @hsCode LIMIT 1";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@hsCode", hsCode);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        return Convert.ToInt32(result);
                    return -1;
                }
            }
        }

        // ---------------- Customers CRUD ----------------
        public static DataTable GetCustomers(string filter = "")
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT customerId, customerBusinessName, customerNTNCNIC, customerProvince, customerAddress, registrationType FROM Customers";

                if (!string.IsNullOrWhiteSpace(filter))
                    sql += " WHERE customerBusinessName LIKE @f OR customerNTNCNIC LIKE @f";

                using (var da = new SQLiteDataAdapter(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(filter))
                        da.SelectCommand.Parameters.AddWithValue("@f", "%" + filter + "%");

                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }


        public static void AddCustomer(string businessName, string ntnCnic, string province, string address, string registrationType)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO Customers (customerBusinessName, customerNTNCNIC, customerProvince, customerAddress, registrationType)
                       VALUES (@businessName, @ntnCnic, @province, @address, @registrationType)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@businessName", businessName);
                    cmd.Parameters.AddWithValue("@ntnCnic", ntnCnic);
                    cmd.Parameters.AddWithValue("@province", province);
                    cmd.Parameters.AddWithValue("@address", address);
                    cmd.Parameters.AddWithValue("@registrationType", registrationType);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static void UpdateCustomer(int customerId, string businessName, string ntnCnic, string province, string address, string registrationType)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"UPDATE Customers
                       SET customerBusinessName = @businessName,
                           customerNTNCNIC = @ntnCnic,
                           customerProvince = @province,
                           customerAddress = @address,
                           registrationType = @registrationType
                       WHERE customerId = @customerId";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@businessName", businessName);
                    cmd.Parameters.AddWithValue("@ntnCnic", ntnCnic);
                    cmd.Parameters.AddWithValue("@province", province);
                    cmd.Parameters.AddWithValue("@address", address);
                    cmd.Parameters.AddWithValue("@registrationType", registrationType);
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static void DeleteCustomer(int customerId)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "DELETE FROM Customers WHERE customerId = @customerId";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        // ---------------- Sellers CRUD ----------------
        public static void AddSeller(string businessName, string ntncnic, string province, string address, string regType, string token, byte[] logo, string invoiceFooter)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO Sellers 
                               (sellerBusinessName, sellerNTNCNIC, sellerProvince, sellerAddress, registrationType, token, logoPath, invoiceFooter)
                               VALUES (@bn, @ntn, @prov, @addr, @reg, @tok, @logo, @footer)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bn", businessName);
                    cmd.Parameters.AddWithValue("@ntn", ntncnic);
                    cmd.Parameters.AddWithValue("@prov", province);
                    cmd.Parameters.AddWithValue("@addr", address);
                    cmd.Parameters.AddWithValue("@reg", regType);
                    cmd.Parameters.AddWithValue("@tok", token);
                    cmd.Parameters.Add("@logo", DbType.Binary).Value = (object)logo ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("@footer", invoiceFooter ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ✅ Update seller
        public static void UpdateSeller(int id, string businessName, string ntncnic, string province, string address, string regType, string token, byte[] logo, string invoiceFooter)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"UPDATE Sellers SET 
                               sellerBusinessName=@bn, sellerNTNCNIC=@ntn, sellerProvince=@prov, 
                               sellerAddress=@addr, registrationType=@reg, token=@tok, logoPath=@logo, invoiceFooter=@footer 
                               WHERE sellerId=@id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bn", businessName);
                    cmd.Parameters.AddWithValue("@ntn", ntncnic);
                    cmd.Parameters.AddWithValue("@prov", province);
                    cmd.Parameters.AddWithValue("@addr", address);
                    cmd.Parameters.AddWithValue("@reg", regType);
                    cmd.Parameters.AddWithValue("@tok", token);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.Add("@logo", DbType.Binary).Value = (object)logo ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("@footer", invoiceFooter ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ✅ Delete seller
        public static void DeleteSeller(int id)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "DELETE FROM Sellers WHERE sellerId=@id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ✅ Load sellers (with optional filter)
        public static DataTable GetSellers(string filter = "")
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT sellerId, sellerBusinessName, sellerNTNCNIC, sellerProvince, sellerAddress, registrationType, token, logoPath, invoiceFooter FROM Sellers";

                if (!string.IsNullOrWhiteSpace(filter))
                    sql += " WHERE sellerBusinessName LIKE @f OR sellerNTNCNIC LIKE @f";

                using (var da = new SQLiteDataAdapter(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(filter))
                        da.SelectCommand.Parameters.AddWithValue("@f", "%" + filter + "%");

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }


        public static DataRow GetSellerById(int sellerId)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT sellerId, sellerBusinessName, sellerNTNCNIC, sellerProvince, sellerAddress, registrationType, token, logoPath, invoiceFooter FROM Sellers WHERE sellerId=@sellerId";
                using (var da = new SQLiteDataAdapter(sql, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@sellerId", sellerId);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                }
            }
        }


        // ---------------- Invoices & InvoiceItems ----------------

        /// <summary>
        /// Adds invoice header and returns inserted invoiceId.
        /// </summary>



        // Fetch all invoices for form listing / grid
        public static DataTable GetInvoices()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
SELECT i.invoiceId, i.invoiceNumber, i.invoiceDate, c.customerBusinessName, i.subTotal, i.totalTax, i.discount, i.grandTotal, i.status
FROM Invoices i
LEFT JOIN Customers c ON i.customerId = c.customerId
ORDER BY i.invoiceDate DESC";
                using (var da = new SQLiteDataAdapter(sql, conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        // AddInvoiceItem already exists
        public static DataTable GetInvoiceItems(int invoiceId)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
SELECT ii.itemId, p.productId, p.hsCode, p.productDescription, ii.description, ii.quantity, ii.rate, ii.unitPrice, ii.totalValues, ii.salesTaxApplicable, ii.furtherTax, ii.discount, ii.sroItemSerialNo, ii.sroScheduleNo
FROM InvoiceItems ii
LEFT JOIN Products p ON ii.productId = p.productId
WHERE ii.invoiceId = @invoiceId";

                using (var da = new SQLiteDataAdapter(sql, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@invoiceId", invoiceId);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        // ---------------- Payments ----------------
        public static void AddPayment(int invoiceId, decimal amount, string method, string status)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO Payments (invoiceId, amount, method, status, paymentDate)
                       VALUES (@invoiceId, @amount, @method, @status, @paymentDate)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@invoiceId", invoiceId);
                    cmd.Parameters.AddWithValue("@amount", amount);
                    cmd.Parameters.AddWithValue("@method", method);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@paymentDate", DateTime.Now.ToString("yyyy-MM-dd"));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static DataTable GetPayments(int invoiceId)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Payments WHERE invoiceId=@invoiceId";
                using (var da = new SQLiteDataAdapter(sql, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@invoiceId", invoiceId);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
        public static void AddInvoiceItem(
    int invoiceId,
    int productId,
    string description,
    decimal quantity,
    string rate,                       // Text (e.g. "18%")
    decimal unitPrice,
    decimal totalValues,
    decimal valueSalesExcludingST,
    decimal fixedNotifiedValueOrRetailPrice,
    decimal salesTaxApplicable,
    decimal salesTaxWithheldAtSource,
    decimal extraTax,
    decimal furtherTax,
    decimal fedPayable,
    decimal discount,
    string saleType,
    string sroItemSerialNo,
    string sroScheduleNo)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
INSERT INTO InvoiceItems 
(invoiceId, productId, description, quantity, rate, unitPrice, totalValues, valueSalesExcludingST,
 fixedNotifiedValueOrRetailPrice, salesTaxApplicable, salesTaxWithheldAtSource, extraTax, furtherTax, 
 fedPayable, discount, saleType, sroItemSerialNo, sroScheduleNo)
VALUES 
(@invoiceId, @productId, @description, @quantity, @rate, @unitPrice, @totalValues, @valueSalesExcludingST,
 @fixedNotifiedValueOrRetailPrice, @salesTaxApplicable, @salesTaxWithheldAtSource, @extraTax, @furtherTax,
 @fedPayable, @discount, @saleType, @sroItemSerialNo, @sroScheduleNo);";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@invoiceId", invoiceId);
                    cmd.Parameters.AddWithValue("@productId", productId);
                    cmd.Parameters.AddWithValue("@description", description);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.Parameters.AddWithValue("@rate", rate);
                    cmd.Parameters.AddWithValue("@unitPrice", unitPrice);
                    cmd.Parameters.AddWithValue("@totalValues", totalValues);
                    cmd.Parameters.AddWithValue("@valueSalesExcludingST", valueSalesExcludingST);
                    cmd.Parameters.AddWithValue("@fixedNotifiedValueOrRetailPrice", fixedNotifiedValueOrRetailPrice);
                    cmd.Parameters.AddWithValue("@salesTaxApplicable", salesTaxApplicable);
                    cmd.Parameters.AddWithValue("@salesTaxWithheldAtSource", salesTaxWithheldAtSource);
                    cmd.Parameters.AddWithValue("@extraTax", extraTax);
                    cmd.Parameters.AddWithValue("@furtherTax", furtherTax);
                    cmd.Parameters.AddWithValue("@fedPayable", fedPayable);
                    cmd.Parameters.AddWithValue("@discount", discount);
                    cmd.Parameters.AddWithValue("@saleType", saleType);
                    cmd.Parameters.AddWithValue("@sroItemSerialNo", sroItemSerialNo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@sroScheduleNo", sroScheduleNo ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static int AddInvoice(
    int customerId,
    int sellerId,
    DateTime invoiceDate,
    decimal subTotal,
    decimal totalTax,
    decimal discount,
    decimal grandTotal,
    string notes,
    string status = "Unpaid",
    string postStatus = "Unpost",
    string fbrInvoiceNumber = null)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                // Generate invoice number
                string invoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");

                string sql = @"
INSERT INTO Invoices 
(invoiceNumber, fbrInvoiceNumber, invoiceDate, customerId, sellerId, subTotal, totalTax, discount, grandTotal, notes, status, postStatus)
VALUES 
(@invoiceNumber, @fbrInvoiceNumber, @invoiceDate, @customerId, @sellerId, @subTotal, @totalTax, @discount, @grandTotal, @notes, @status, @postStatus);
SELECT last_insert_rowid();";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@invoiceNumber", invoiceNumber);
                    cmd.Parameters.AddWithValue("@fbrInvoiceNumber", fbrInvoiceNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@invoiceDate", invoiceDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.Parameters.AddWithValue("@sellerId", sellerId);
                    cmd.Parameters.AddWithValue("@subTotal", subTotal);
                    cmd.Parameters.AddWithValue("@totalTax", totalTax);
                    cmd.Parameters.AddWithValue("@discount", discount);
                    cmd.Parameters.AddWithValue("@grandTotal", grandTotal);
                    cmd.Parameters.AddWithValue("@notes", notes);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@postStatus", postStatus);

                    long id = (long)cmd.ExecuteScalar();
                    return (int)id;
                }
            }
        }
        public static int PostInvoice(
    int customerId,
    int sellerId,
    DateTime invoiceDate,
    decimal subTotal,
    decimal totalTax,
    decimal discount,
    decimal grandTotal,
    string notes,
    string status = "Unpaid",
    string postStatus = "Posted",
    string fbrInvoiceNumber = "")
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                // Generate invoice number
                string invoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");

                string sql = @"
INSERT INTO Invoices 
(invoiceNumber, fbrInvoiceNumber, invoiceDate, customerId, sellerId, subTotal, totalTax, discount, grandTotal, notes, status, postStatus)
VALUES 
(@invoiceNumber, @fbrInvoiceNumber, @invoiceDate, @customerId, @sellerId, @subTotal, @totalTax, @discount, @grandTotal, @notes, @status, @postStatus);
SELECT last_insert_rowid();";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@invoiceNumber", invoiceNumber);
                    cmd.Parameters.AddWithValue("@fbrInvoiceNumber", fbrInvoiceNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@invoiceDate", invoiceDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.Parameters.AddWithValue("@sellerId", sellerId);
                    cmd.Parameters.AddWithValue("@subTotal", subTotal);
                    cmd.Parameters.AddWithValue("@totalTax", totalTax);
                    cmd.Parameters.AddWithValue("@discount", discount);
                    cmd.Parameters.AddWithValue("@grandTotal", grandTotal);
                    cmd.Parameters.AddWithValue("@notes", notes);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@postStatus", postStatus);
                    cmd.Parameters.AddWithValue("@fbrInvoiceNumber", fbrInvoiceNumber);
                    long id = (long)cmd.ExecuteScalar();
                    return (int)id;
                }
            }
        }
        public static void DeleteInvoice(int invoiceId)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 🔹 Pehle InvoiceItems delete karo
                        string sqlItems = "DELETE FROM InvoiceItems WHERE invoiceId = @invoiceId";
                        using (var cmdItems = new SQLiteCommand(sqlItems, conn, transaction))
                        {
                            cmdItems.Parameters.AddWithValue("@invoiceId", invoiceId);
                            cmdItems.ExecuteNonQuery();
                        }

                        // 🔹 Ab Invoice delete karo
                        string sqlInvoice = "DELETE FROM Invoices WHERE invoiceId = @invoiceId";
                        using (var cmdInvoice = new SQLiteCommand(sqlInvoice, conn, transaction))
                        {
                            cmdInvoice.Parameters.AddWithValue("@invoiceId", invoiceId);
                            cmdInvoice.ExecuteNonQuery();
                        }

                        // ✅ Commit
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // ❌ Agar error aaye to rollback
                        transaction.Rollback();
                        throw new Exception("Error deleting invoice: " + ex.Message);
                    }
                }
            }
        }


        public static DataRow GetInvoiceById(int invoiceId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string sql = @"
            SELECT 
                invoiceId,
                invoiceNumber,
                fbrInvoiceNumber,
                invoiceDate,
                subTotal,
                totalTax,
                discount,
                grandTotal,
                notes,
                status,
                postStatus
            FROM Invoices
            WHERE invoiceId = @invoiceId";

                using (var da = new System.Data.SQLite.SQLiteDataAdapter(sql, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@invoiceId", invoiceId);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                        return dt.Rows[0];  // ✅ DataRow return karega
                    else
                        return null;
                }
            }
        }

        public static DataTable GetInvoiceItemss(int invoiceId)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string query = "SELECT * FROM InvoiceItems WHERE invoiceId = @id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", invoiceId);

                    using (var da = new SQLiteDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public static DataSet GetInvoicePreviewData(int invoiceId)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                DataSet ds = new DataSet();

                // 🧾 Invoice Header + Seller Logo Path
                string sqlHeader = @"
        SELECT 
            i.invoiceId, 
            i.invoiceNumber, 
            i.fbrInvoiceNumber, 
            i.invoiceDate, 
            i.postStatus,
            i.subTotal, 
            i.totalTax, 
            i.discount, 
            i.grandTotal, 
            i.notes,
            i.status, 
            s.sellerBusinessName, 
            s.sellerNTNCNIC, 
            s.sellerProvince, 
            s.sellerAddress, 
            s.logoPath AS sellerLogoPath,
            s.invoiceFooter,

            c.customerBusinessName, 
            c.customerNTNCNIC, 
            c.customerProvince, 
            c.customerAddress
        FROM Invoices i
        LEFT JOIN Sellers s ON i.sellerId = s.sellerId
        LEFT JOIN Customers c ON i.customerId = c.customerId
        WHERE i.invoiceId = @invoiceId";

                using (var daHeader = new SQLiteDataAdapter(sqlHeader, conn))
                {
                    daHeader.SelectCommand.Parameters.AddWithValue("@invoiceId", invoiceId);
                    daHeader.Fill(ds, "InvoiceHeader");
                }

                // 🧾 Invoice Items
                string sqlItems = @"
        SELECT 
            ii.itemId, 
            p.productId, 
            p.hsCode, 
            p.productDescription, 
            ii.description, 
            ii.quantity, 
            ii.rate, 
            ii.unitPrice, 
            ii.totalValues, 
            ii.salesTaxApplicable, 
            ii.furtherTax, 
            ii.discount,
            ii.sroItemSerialNo,
            ii.sroScheduleNo
        FROM InvoiceItems ii
        LEFT JOIN Products p ON ii.productId = p.productId
        WHERE ii.invoiceId = @invoiceId";

                using (var daItems = new SQLiteDataAdapter(sqlItems, conn))
                {
                    daItems.SelectCommand.Parameters.AddWithValue("@invoiceId", invoiceId);
                    daItems.Fill(ds, "InvoiceItems");
                }

                return ds;
            }
        }




        // ---------------- Utility ----------------
        public static int GetCount(string tableName)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = $"SELECT COUNT(*) FROM {tableName}";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    object result = cmd.ExecuteScalar();

                    return Convert.ToInt32(result);
                }
            }
        }



    }
}