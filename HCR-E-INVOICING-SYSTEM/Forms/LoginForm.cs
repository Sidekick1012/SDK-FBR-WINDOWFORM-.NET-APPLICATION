using SDK_E_INVOICING_SYSTEM;
using SDK_E_INVOICING_SYSTEM.Data;
using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace InvoiceApp
{
    public class LoginForm : Form
    {
        private Panel leftPanel;
        private Label lblBrand, lblTitle, lblUsername, lblPassword;
        private TextBox txtUsername, txtPassword;
        private Button btnLogin, btnForgot;

        private bool isUserPlaceholder = true;
        private bool isPassPlaceholder = true;

        public LoginForm()
        {

            InitializeUI();
            // Fade in on load
            this.Load += (s, e) => FormTransitionHelper.AnimateFadeIn(this);
        }

        private void InitializeUI()
        {

            // Form Properties
            this.Text = "LOGIN - INVOICE SYSTEM";
            this.MinimumSize = new Size(700, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            this.SuspendLayout();

            // Left Branding Panel - HCR White
            leftPanel = new Panel()
            {
                Dock = DockStyle.Left,
                Width = 300,
                BackColor = Color.White
            };
            this.Controls.Add(leftPanel);

            // =====================
            // HCR Logo
            // =====================
            Image logoImage = null;
            try
            {
                string logoFile = Path.Combine(Application.StartupPath, "HCR-LOGO.png");
                if (File.Exists(logoFile))
                    logoImage = Image.FromFile(logoFile);
            }
            catch { }

            var picLogo = new PictureBox()
            {
                Image = logoImage,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Color.White,
                Padding = new Padding(35, 20, 35, 15)
            };

            // =====================
            // Chamber of Commerce Text
            // =====================
            var lblChamber = new Label()
            {
                Text = "REGISTERED MEMBER OF ISLAMABAD\r\nCHAMBER OF COMMERCE & INDUSTRY",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1D2068"),
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White
            };

            // =====================
            // Orange top accent bar
            // =====================
            var topBar = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 6,
                BackColor = ColorTranslator.FromHtml("#E84B2B")
            };

            // =====================
            // Tagline
            // =====================
            var lblTagline = new Label()
            {
                Text = "FBR E-INVOICING SYSTEM",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1D2068"),
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // =====================
            // Divider Line
            // =====================
            var divider = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 2,
                BackColor = ColorTranslator.FromHtml("#1D2068")
            };

            // =====================
            // Developer Credit
            // =====================
            var lblDev = new Label()
            {
                Text = "DEVELOPED BY HCR | © 2026",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = ColorTranslator.FromHtml("#1D2068"),
                Dock = DockStyle.Bottom,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // =====================
            // Spacer (Filler)
            // =====================
            var filler = new Panel()
            {
                Dock = DockStyle.Fill
            };

            // Add order: later added = visually higher for DockStyle.Top
            // Desired visual (top→bottom): topBar, picLogo, lblChamber, lblTagline, divider
            // So add in reverse: divider first, then lblTagline, lblChamber, picLogo, topBar last
            leftPanel.Controls.Add(lblDev);      // DockBottom
            leftPanel.Controls.Add(filler);      // DockFill
            leftPanel.Controls.Add(divider);     // visual: lowest top-docked
            leftPanel.Controls.Add(lblTagline);  // visual: above divider
            leftPanel.Controls.Add(lblChamber);  // visual: above tagline
            leftPanel.Controls.Add(picLogo);     // visual: above chamber
            leftPanel.Controls.Add(topBar);      // visual: very top (added last)


            // Title
            lblTitle = new Label()
            {
                Text = "LOGIN TO SYSTEM",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1D2068"),
                AutoSize = true,
                Location = new Point(350, 50)
            };
            this.Controls.Add(lblTitle);

            // Username Label
            lblUsername = new Label()
            {
                Text = "USERNAME:",
                Location = new Point(310, 120),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                AutoSize = true
            };
            this.Controls.Add(lblUsername);

            // Username TextBox
            txtUsername = new TextBox()
            {
                Location = new Point(410, 115),
                Width = 240,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.Gray,
                Text = "ENTER USERNAME"
            };
            this.Controls.Add(txtUsername);

            txtUsername.GotFocus += RemoveUserPlaceholder;
            txtUsername.LostFocus += AddUserPlaceholder;
            txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    txtPassword.Focus();
                }
            };

            // Password Label
            lblPassword = new Label()
            {
                Text = "PASSWORD:",
                Location = new Point(310, 170),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                AutoSize = true
            };
            this.Controls.Add(lblPassword);

            // Password TextBox
            txtPassword = new TextBox()
            {
                Location = new Point(410, 165),
                Width = 240,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.Gray,
                Text = "ENTER PASSWORD",
                UseSystemPasswordChar = false
            };
            this.Controls.Add(txtPassword);

            txtPassword.GotFocus += RemovePassPlaceholder;
            txtPassword.LostFocus += AddPassPlaceholder;
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    PerformLogin();
                }
            };

            // Login Button
            btnLogin = new Button()
            {
                Text = "LOGIN",
                Location = new Point(410, 220),
                Width = 240,
                Height = 45,
                BackColor = ColorTranslator.FromHtml("#1D2068"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += (s, e) => PerformLogin();
            this.Controls.Add(btnLogin);

            // Forgot Password Button
            btnForgot = new Button()
            {
                Text = "FORGOT PASSWORD?",
                Location = new Point(410, 270),
                Width = 240,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Blue,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnForgot.FlatAppearance.BorderSize = 0;


            // Footer Panel removed




            this.ResumeLayout(false);
        }

        // Placeholder Handlers
        private void RemoveUserPlaceholder(object sender, EventArgs e)
        {
            if (isUserPlaceholder)
            {
                txtUsername.Text = "";
                txtUsername.ForeColor = Color.Black;
                isUserPlaceholder = false;
            }
        }
        private void AddUserPlaceholder(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                txtUsername.Text = "ENTER USERNAME";
                txtUsername.ForeColor = Color.Gray;
                isUserPlaceholder = true;
            }
        }

        // Password Placeholder Safe Handling
        // ---- Password Placeholder Safe Handling ----
        private void RemovePassPlaceholder(object sender, EventArgs e)
        {
            if (isPassPlaceholder)
            {
                txtPassword.Clear();
                txtPassword.ForeColor = Color.Black;
                isPassPlaceholder = false;
                txtPassword.UseSystemPasswordChar = false; // still plain at first
            }

            // ✅ Enable masking only after first key press
            txtPassword.TextChanged -= TxtPassword_TextChanged; // avoid double attach
            txtPassword.TextChanged += TxtPassword_TextChanged;
        }
        private void TxtPassword_TextChanged(object sender, EventArgs e)
        {
            if (!isPassPlaceholder && txtPassword.Text.Length > 0)
            {
                txtPassword.UseSystemPasswordChar = true;
                txtPassword.TextChanged -= TxtPassword_TextChanged; // disable after done
            }
        }

        private void AddPassPlaceholder(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                isPassPlaceholder = true;
                txtPassword.UseSystemPasswordChar = false;  // placeholder ke liye plain text
                txtPassword.ForeColor = Color.Gray;
                txtPassword.Text = "ENTER PASSWORD";
            }
        }


        // Login Logic
        private void PerformLogin()
        {
            string username = isUserPlaceholder ? "" : txtUsername.Text.Trim();
            string password = isPassPlaceholder ? "" : txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Username is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Password is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            if (ValidateUser(username, password))
            {
                // Fade out then open Dashboard
                FormTransitionHelper.AnimateFadeOut(this, () =>
                {
                    var dashboard = new DashboardForm();
                    dashboard.Show();
                    FormTransitionHelper.AnimateFadeIn(dashboard);
                    this.Hide();
                });
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Login Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // SQLite Validation
        private bool ValidateUser(string username, string password)
        {
            // First check for default admin
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) &&
                password == "admin123")
            {
                return true;
            }

            // Otherwise check in database
            try
            {
                using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM Users WHERE Username=@Username AND Password=@Password";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Password", password);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

    }
}