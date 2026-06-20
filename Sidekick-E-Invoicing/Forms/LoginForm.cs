using Sidekick_E_Invoicing;
using Sidekick_E_Invoicing.Data;
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
        private Button btnTogglePassword;
        private bool showPassword = false;

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
            this.Size = new Size(700, 450);
            this.MinimumSize = new Size(700, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            this.SuspendLayout();

            // Main container to split left and right panels cleanly
            var mainContainer = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300)); // Left branding panel
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Right login form panel
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            this.Controls.Add(mainContainer);

            // Left Branding Panel - Sidekick White
            leftPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            mainContainer.Controls.Add(leftPanel, 0, 0);

            // =====================
            // Sidekick Logo
            // =====================
            Image logoImage = null;
            try
            {
                string logoFile = Path.Combine(Application.StartupPath, "Sidekick-LOGO.png");
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
                ForeColor = ColorTranslator.FromHtml("#1b6656"),
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
                ForeColor = ColorTranslator.FromHtml("#1b6656"),
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
                BackColor = ColorTranslator.FromHtml("#1b6656")
            };

            // =====================
            // Developer Credit
            // =====================
            var lblDev = new Label()
            {
                Text = "DEVELOPED BY SIDEKICK | © 2025\r\n0300-0228444  •  info@sidekick.pk  •  www.sidekick.pk",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = ColorTranslator.FromHtml("#1b6656"),
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

            // Add in clean docking order to prevent internal overlaps
            leftPanel.Controls.Add(lblDev);      // DockBottom
            leftPanel.Controls.Add(topBar);      // DockTop (very top)
            leftPanel.Controls.Add(picLogo);     // DockTop (under topBar)
            leftPanel.Controls.Add(lblChamber);  // DockTop (under picLogo)
            leftPanel.Controls.Add(lblTagline);  // DockTop (under lblChamber)
            leftPanel.Controls.Add(divider);     // DockTop (under lblTagline)
            leftPanel.Controls.Add(filler);      // DockFill (occupies remaining spacer)


            // ===== RIGHT PANEL (Responsive Login Form) =====
            var rightPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(30)
            };
            mainContainer.Controls.Add(rightPanel, 1, 0);

            // Center layout inside right panel
            var centerLayout = new TableLayoutPanel()
            {
                ColumnCount = 1,
                RowCount = 7,
                Dock = DockStyle.None,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            centerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Title
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));  // Username label
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Username box
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));  // Password label
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Password box
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));  // Login button
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));  // Forgot Password button

            // Title
            lblTitle = new Label()
            {
                Text = "LOGIN TO SYSTEM",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1b6656"),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Username label
            lblUsername = new Label()
            {
                Text = "USERNAME",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            // Username TextBox
            txtUsername = new TextBox()
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.Gray,
                Text = "ENTER USERNAME"
            };
            txtUsername.GotFocus += RemoveUserPlaceholder;
            txtUsername.LostFocus += AddUserPlaceholder;
            txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtPassword.Focus(); }
            };

            // Password label
            lblPassword = new Label()
            {
                Text = "PASSWORD",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            // Password TextBox container Panel
            var pnlPassword = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0),
                Height = 30
            };
            pnlPassword.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // TextBox
            pnlPassword.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35)); // Eye Button
            pnlPassword.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Password TextBox
            txtPassword = new TextBox()
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.Gray,
                Text = "ENTER PASSWORD",
                UseSystemPasswordChar = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            txtPassword.GotFocus += RemovePassPlaceholder;
            txtPassword.LostFocus += AddPassPlaceholder;
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; PerformLogin(); }
            };

            btnTogglePassword = new Button()
            {
                Text = "👁",
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = ColorTranslator.FromHtml("#1b6656"),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 0, 0, 0)
            };
            btnTogglePassword.FlatAppearance.BorderSize = 1;
            btnTogglePassword.FlatAppearance.BorderColor = Color.LightGray;
            btnTogglePassword.Click += (s, e) => TogglePasswordVisibility();

            pnlPassword.Controls.Add(txtPassword, 0, 0);
            pnlPassword.Controls.Add(btnTogglePassword, 1, 0);

            // Login Button
            btnLogin = new Button()
            {
                Text = "LOGIN",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 0),
                Height = 45,
                BackColor = ColorTranslator.FromHtml("#1b6656"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = ColorTranslator.FromHtml("#1b6656");
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = ColorTranslator.FromHtml("#1b6656");
            btnLogin.Click += (s, e) => PerformLogin();

            // Forgot Password
            btnForgot = new Button()
            {
                Text = "FORGOT PASSWORD?",
                Dock = DockStyle.None,
                FlatStyle = FlatStyle.Flat,
                ForeColor = ColorTranslator.FromHtml("#1b6656"),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Underline),
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 5, 0, 0)
            };
            btnForgot.FlatAppearance.BorderSize = 0;
            btnForgot.Click += (s, e) => {
                using (var forgotForm = new ForgotPasswordForm())
                {
                    forgotForm.ShowDialog(this);
                }
            };

            // Add to table
            centerLayout.Controls.Add(lblTitle, 0, 0);
            centerLayout.Controls.Add(lblUsername, 0, 1);
            centerLayout.Controls.Add(txtUsername, 0, 2);
            centerLayout.Controls.Add(lblPassword, 0, 3);
            centerLayout.Controls.Add(pnlPassword, 0, 4);
            centerLayout.Controls.Add(btnLogin, 0, 5);
            centerLayout.Controls.Add(btnForgot, 0, 6);

            // Center the layout dynamically
            rightPanel.Controls.Add(centerLayout);
            rightPanel.SizeChanged += (s, e) =>
            {
                int targetW = Math.Min(360, rightPanel.ClientSize.Width - 60);
                centerLayout.Width = targetW;
                centerLayout.Left = (rightPanel.ClientSize.Width - targetW) / 2;
                centerLayout.Top = Math.Max(10, (rightPanel.ClientSize.Height - centerLayout.Height) / 2 - 35);
            };

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
            if (!isPassPlaceholder && txtPassword.Text.Length > 0 && !showPassword)
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
                showPassword = false;
                btnTogglePassword.Text = "👁";
            }
        }

        private void TogglePasswordVisibility()
        {
            if (isPassPlaceholder) return;

            showPassword = !showPassword;
            if (showPassword)
            {
                txtPassword.UseSystemPasswordChar = false;
                btnTogglePassword.Text = "🙈";
            }
            else
            {
                if (txtPassword.Text.Length > 0)
                {
                    txtPassword.UseSystemPasswordChar = true;
                }
                btnTogglePassword.Text = "👁";
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
            // Check in database
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

    public class ForgotPasswordForm : Form
    {
        private Label lblTitle, lblUsername, lblResetKey, lblNewPassword;
        private TextBox txtUsername, txtResetKey, txtNewPassword;
        private Button btnReset, btnCancel;

        public ForgotPasswordForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "RESET PASSWORD";
            this.Size = new Size(420, 390);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            this.SuspendLayout();

            // Orange top accent bar
            var topBar = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 6,
                BackColor = ColorTranslator.FromHtml("#E84B2B")
            };
            this.Controls.Add(topBar);

            var mainLayout = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(30, 20, 30, 20),
                BackColor = Color.White
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); // Username
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); // Master Reset Key
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); // New Password
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 15)); // Spacer
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Buttons Layout
            this.Controls.Add(mainLayout);

            // Title
            lblTitle = new Label()
            {
                Text = "Reset Account Password",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1b6656"),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainLayout.Controls.Add(lblTitle, 0, 0);

            // Username group
            var pnlUser = new Panel() { Dock = DockStyle.Fill };
            lblUsername = new Label()
            {
                Text = "USERNAME",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Location = new Point(0, 0),
                Size = new Size(340, 15)
            };
            txtUsername = new TextBox()
            {
                Font = new Font("Segoe UI", 10),
                Location = new Point(0, 18),
                Width = 340
            };
            pnlUser.Controls.Add(lblUsername);
            pnlUser.Controls.Add(txtUsername);
            mainLayout.Controls.Add(pnlUser, 0, 1);

            // Master Reset Key group
            var pnlKey = new Panel() { Dock = DockStyle.Fill };
            lblResetKey = new Label()
            {
                Text = "MASTER RESET KEY",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Location = new Point(0, 0),
                Size = new Size(340, 15)
            };
            txtResetKey = new TextBox()
            {
                Font = new Font("Segoe UI", 10),
                Location = new Point(0, 18),
                Width = 340
            };
            pnlKey.Controls.Add(lblResetKey);
            pnlKey.Controls.Add(txtResetKey);
            mainLayout.Controls.Add(pnlKey, 0, 2);

            // New Password group
            var pnlPass = new Panel() { Dock = DockStyle.Fill };
            lblNewPassword = new Label()
            {
                Text = "NEW PASSWORD",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Location = new Point(0, 0),
                Size = new Size(340, 15)
            };
            txtNewPassword = new TextBox()
            {
                Font = new Font("Segoe UI", 10),
                Location = new Point(0, 18),
                Width = 340,
                UseSystemPasswordChar = true
            };
            pnlPass.Controls.Add(lblNewPassword);
            pnlPass.Controls.Add(txtNewPassword);
            mainLayout.Controls.Add(pnlPass, 0, 3);

            // Buttons panel
            var pnlButtons = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            btnReset = new Button()
            {
                Text = "RESET",
                Dock = DockStyle.Fill,
                BackColor = ColorTranslator.FromHtml("#1b6656"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 5, 0)
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.MouseEnter += (s, e) => btnReset.BackColor = ColorTranslator.FromHtml("#1b6656");
            btnReset.MouseLeave += (s, e) => btnReset.BackColor = ColorTranslator.FromHtml("#1b6656");
            btnReset.Click += BtnReset_Click;

            btnCancel = new Button()
            {
                Text = "CANCEL",
                Dock = DockStyle.Fill,
                BackColor = Color.LightGray,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            pnlButtons.Controls.Add(btnReset, 0, 0);
            pnlButtons.Controls.Add(btnCancel, 1, 0);
            mainLayout.Controls.Add(pnlButtons, 0, 5);

            this.ResumeLayout(false);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string resetKey = txtResetKey.Text.Trim();
            string newPassword = txtNewPassword.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter the Username.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(resetKey))
            {
                MessageBox.Show("Please enter the Master Reset Key.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtResetKey.Focus();
                return;
            }

            if (resetKey != "SDK-RESET-2025")
            {
                MessageBox.Show("Invalid Master Reset Key.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtResetKey.Focus();
                return;
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                MessageBox.Show("Please enter a New Password.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewPassword.Focus();
                return;
            }

            // Check if username exists in DB
            try
            {
                bool userExists = false;
                using (var conn = new SQLiteConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string checkSql = "SELECT COUNT(*) FROM Users WHERE Username=@Username";
                    using (var cmd = new SQLiteCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        userExists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }

                    if (!userExists)
                    {
                        // Special case: If they want to reset default admin but it wasn't inserted (should be though)
                        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                        {
                            string insertSql = "INSERT INTO Users (Username, Password) VALUES ('admin', @Password)";
                            using (var cmd = new SQLiteCommand(insertSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@Password", newPassword);
                                cmd.ExecuteNonQuery();
                            }
                            MessageBox.Show("Admin account password has been successfully reset!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                            return;
                        }

                        MessageBox.Show("Username not found in the database.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Reset password
                    string updateSql = "UPDATE Users SET Password=@Password WHERE Username=@Username";
                    using (var cmd = new SQLiteCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Password", newPassword);
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Password has been reset successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}