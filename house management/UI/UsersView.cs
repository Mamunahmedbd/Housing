using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using house_management.Models;
using house_management.Services;

namespace house_management
{
    /// <summary>
    /// User-management view for the main form. Kept as a partial class so
    /// Form1.cs stays focused on Houses while all user CRUD UI lives here.
    /// Shares colors, helpers and layout containers with Form1.
    /// </summary>
    public partial class Form1
    {
        // --- Sidebar entry ---
        // btnUsers is declared in Form1.cs because the sidebar is built there.
        private Button btnProfile;

        // --- Users content controls ---
        private DataGridView dgvUsers;
        private TextBox txtUserSearch;
        private Button btnAddUser;
        private Button btnEditUser;
        private Button btnDeleteUser;
        private Button btnResetUserPassword;
        private bool userSearchPlaceholderActive = true;

        // --- Add/Edit user dialog ---
        private Panel pnlUserDialog;
        private Label lblUserDialogTitle;
        private TextBox txtUUsername;
        private TextBox txtUEmail;
        private TextBox txtUFullName;
        private TextBox txtUPhone;
        private ComboBox cmbURole;
        private ComboBox cmbUStatus;
        private Panel pnlUPasswordContainer;
        private TextBox txtUPassword;
        private Label lblUPasswordHint;
        private Button btnUSave;
        private Button btnUCancel;
        private int? editingUserId;
        private bool userDialogIsEdit;

        // --- Change password dialog (self-service) ---
        private Panel pnlChangePasswordDialog;
        private TextBox txtCPCurrent;
        private TextBox txtCPNew;
        private TextBox txtCPConfirm;
        private Button btnCPSave;
        private Button btnCPCancel;
        private Label lblCPDialogTitle;

        // --- Reset password dialog (admin) ---
        private Panel pnlResetPasswordDialog;
        private TextBox txtRPNew;
        private TextBox txtRPConfirm;
        private Button btnRPSave;
        private Button btnRPCancel;
        private Label lblRPDialogTitle;
        private int resetPasswordTargetId;

        // =====================================================================
        //  BUILD
        // =====================================================================

        /// <summary>
        /// Builds the "Change Password" sidebar button and applies admin-only
        /// visibility to the existing "Users" sidebar button. The btnUsers
        /// button itself is created in Form1.BuildDashboardUI().
        /// </summary>
        private void BuildUsersSidebar()
        {
            btnProfile = CreateSidebarButton("🔑  Change Password", this.Height - 125);
            btnProfile.BackColor = Color.FromArgb(70, 50, 90);
            btnProfile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnProfile.Click += (s, e) => OpenChangePasswordDialog();
            pnlSidebar.Controls.Add(btnProfile);
        }

        private void UpdateUsersSidebarVisibility()
        {
            if (btnUsers == null) return;
            btnUsers.Visible = true;
        }

        /// <summary>
        /// Builds the users grid, search box and action buttons. Appended to
        /// the main content panel; toggled via ShowUsersGrid.
        /// </summary>
        private void BuildUsersView()
        {
            txtUserSearch = new TextBox
            {
                Multiline = true,
                Text = " 🔍 Search users...",
                Font = new Font("Segoe UI", 11, FontStyle.Italic),
                BackColor = inputBgColor,
                ForeColor = Color.DarkGray,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(200, 40),
                Location = new Point(230, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false
            };

            txtUserSearch.TextChanged += (s, e) =>
            {
                if (!userSearchPlaceholderActive)
                {
                    FilterUsers(txtUserSearch.Text.Trim());
                }
            };
            txtUserSearch.Enter += (s, e) =>
            {
                if (userSearchPlaceholderActive)
                {
                    txtUserSearch.Text = "";
                    txtUserSearch.ForeColor = Color.White;
                    txtUserSearch.Font = new Font("Segoe UI", 11, FontStyle.Regular);
                    userSearchPlaceholderActive = false;
                }
            };
            txtUserSearch.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtUserSearch.Text))
                {
                    txtUserSearch.Text = " 🔍 Search users...";
                    txtUserSearch.ForeColor = Color.DarkGray;
                    txtUserSearch.Font = new Font("Segoe UI", 11, FontStyle.Italic);
                    userSearchPlaceholderActive = true;
                }
            };
            pnlMainContent.Controls.Add(txtUserSearch);

            btnResetUserPassword = CreateUserActionButton("🔑 Reset", Color.FromArgb(60, 80, 130), 430, 95);
            btnResetUserPassword.Click += (s, e) => HandleResetUserPassword();
            pnlMainContent.Controls.Add(btnResetUserPassword);

            btnDeleteUser = CreateUserActionButton("🗑 Delete", deleteBtnColor, 535, 95);
            btnDeleteUser.Click += (s, e) => HandleDeleteUser();
            pnlMainContent.Controls.Add(btnDeleteUser);

            btnEditUser = CreateUserActionButton("✎ Edit", Color.FromArgb(70, 110, 90), 640, 95);
            btnEditUser.Click += (s, e) => HandleEditUser();
            pnlMainContent.Controls.Add(btnEditUser);

            btnAddUser = CreateUserActionButton("+ Add User", buttonColor, 745, 90);
            btnAddUser.Click += (s, e) => OpenAddUserDialog();
            pnlMainContent.Controls.Add(btnAddUser);

            BuildUsersGrid();
            BuildUserDialog();
            BuildChangePasswordDialog();
            BuildResetPasswordDialog();
        }

        private Button CreateUserActionButton(string text, Color backColor, int xLocation, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = backColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(width, 40),
                Location = new Point(xLocation, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                Visible = false
            };
            btn.FlatAppearance.BorderSize = 0;

            Color original = backColor;
            Color hover = ControlPaint.Light(backColor, 0.15f);
            btn.MouseEnter += (s, e) => btn.BackColor = hover;
            btn.MouseLeave += (s, e) => btn.BackColor = original;

            try { btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 10, 10)); } catch { }
            return btn;
        }

        private void LayoutUserViews()
        {
            if (pnlUserDialog == null || dgvUsers == null || pnlMainContent == null) return;

            // Layout top row controls dynamically from the right edge
            int rightEdge = pnlMainContent.Width - 25;
            if (btnAddUser != null)
            {
                btnAddUser.Left = rightEdge - btnAddUser.Width;
                if (btnEditUser != null)
                {
                    btnEditUser.Left = btnAddUser.Left - btnEditUser.Width - 15;
                    if (btnDeleteUser != null)
                    {
                        btnDeleteUser.Left = btnEditUser.Left - btnDeleteUser.Width - 15;
                        if (btnResetUserPassword != null)
                        {
                            btnResetUserPassword.Left = btnDeleteUser.Left - btnResetUserPassword.Width - 15;
                        }
                    }
                }
            }

            if (pnlUserDialog.Visible)
            {
                pnlUserDialog.Width = 380;
                pnlUserDialog.Height = pnlMainContent.Height - 115;
                pnlUserDialog.Location = new Point(pnlMainContent.Width - 380 - 25, 90);
                dgvUsers.Width = pnlMainContent.Width - 380 - 60;
                try { pnlUserDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlUserDialog.Width, pnlUserDialog.Height, 15, 15)); } catch { }
            }
            else
            {
                dgvUsers.Width = pnlMainContent.Width - 50;
            }
            dgvUsers.Height = pnlMainContent.Height - 115;
        }

        private void BuildUsersGrid()
        {
            dgvUsers = new DataGridView
            {
                Size = new Size(800, 470),
                Location = new Point(25, 90),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = Color.FromArgb(90, 70, 110),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                BackgroundColor = Color.FromArgb(26, 16, 36),
                Visible = false
            };

            dgvUsers.SelectionChanged += (s, e) => UpdateUserActionButtonsVisibility();
            dgvUsers.CellDoubleClick += (s, e) => HandleEditUser();

            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(45, 30, 60),
                ForeColor = Color.FromArgb(255, 60, 130),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgvUsers.ColumnHeadersDefaultCellStyle = headerStyle;
            dgvUsers.ColumnHeadersHeight = 50;

            DataGridViewCellStyle rowStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(32, 22, 44),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11, FontStyle.Regular),
                SelectionBackColor = Color.FromArgb(215, 45, 95),
                SelectionForeColor = Color.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgvUsers.RowsDefaultCellStyle = rowStyle;
            dgvUsers.RowTemplate.Height = 42;

            dgvUsers.Columns.Add("colUId", "ID");
            dgvUsers.Columns.Add("colUUsername", "Username");
            dgvUsers.Columns.Add("colUEmail", "Email");
            dgvUsers.Columns.Add("colUFullName", "Full Name");
            dgvUsers.Columns.Add("colUPhone", "Phone");
            dgvUsers.Columns.Add("colURole", "Role");
            dgvUsers.Columns.Add("colUStatus", "Status");
            dgvUsers.Columns.Add("colULastLogin", "Last Login");
            dgvUsers.Columns["colUId"].Visible = false;

            pnlMainContent.Controls.Add(dgvUsers);
        }

        private void BuildUserDialog()
        {
            pnlUserDialog = new Panel
            {
                Size = new Size(380, 470),
                BackColor = Color.FromArgb(35, 22, 48),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            pnlMainContent.Controls.Add(pnlUserDialog);
            pnlMainContent.Resize += (s, e) =>
            {
                LayoutUserViews();
            };

            lblUserDialogTitle = new Label
            {
                Text = "Add New User",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(300, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlUserDialog.Controls.Add(lblUserDialogTitle);

            Panel pnlUUsernameContainer = CreateModernTextBox("Username", 20, 75, 340, 45, "👤", false, out txtUUsername);
            pnlUserDialog.Controls.Add(pnlUUsernameContainer);

            Panel pnlUEmailContainer = CreateModernTextBox("Email Address", 20, 140, 340, 45, "✉", false, out txtUEmail);
            pnlUserDialog.Controls.Add(pnlUEmailContainer);

            Panel pnlUFullNameContainer = CreateModernTextBox("Full Name (optional)", 20, 205, 160, 45, "🪪", false, out txtUFullName);
            pnlUserDialog.Controls.Add(pnlUFullNameContainer);

            Panel pnlUPhoneContainer = CreateModernTextBox("Phone (optional)", 200, 205, 160, 45, "📞", false, out txtUPhone);
            pnlUserDialog.Controls.Add(pnlUPhoneContainer);

            Label lblRoleLabel = new Label
            {
                Text = "Role",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 190, 210),
                Location = new Point(20, 270),
                Size = new Size(160, 16)
            };
            pnlUserDialog.Controls.Add(lblRoleLabel);

            cmbURole = new ComboBox
            {
                BackColor = inputBgColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(20, 290),
                Size = new Size(160, 30),
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbURole.Items.AddRange(new object[] { "Administrator", "Manager", "User" });
            cmbURole.SelectedIndex = 2;
            pnlUserDialog.Controls.Add(cmbURole);

            Label lblStatusLabel = new Label
            {
                Text = "Status",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 190, 210),
                Location = new Point(200, 270),
                Size = new Size(160, 16)
            };
            pnlUserDialog.Controls.Add(lblStatusLabel);

            cmbUStatus = new ComboBox
            {
                BackColor = inputBgColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(200, 290),
                Size = new Size(160, 30),
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbUStatus.Items.AddRange(new object[] { "Active", "Locked" });
            cmbUStatus.SelectedIndex = 0;
            pnlUserDialog.Controls.Add(cmbUStatus);

            lblUPasswordHint = new Label
            {
                Text = "Password",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 190, 210),
                Location = new Point(20, 345),
                Size = new Size(340, 16)
            };
            pnlUserDialog.Controls.Add(lblUPasswordHint);

            pnlUPasswordContainer = CreateModernTextBox("Password", 20, 365, 340, 45, "🔒", true, out txtUPassword);
            pnlUserDialog.Controls.Add(pnlUPasswordContainer);

            btnUSave = new Button
            {
                Text = "SAVE",
                BackColor = buttonColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(20, 430),
                Size = new Size(160, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnUSave.FlatAppearance.BorderSize = 0;
            btnUSave.Click += (s, e) => SaveUser();
            pnlUserDialog.Controls.Add(btnUSave);

            btnUCancel = new Button
            {
                Text = "CANCEL",
                BackColor = Color.FromArgb(70, 60, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(200, 430),
                Size = new Size(160, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnUCancel.FlatAppearance.BorderSize = 0;
            btnUCancel.Click += (s, e) => CloseUserDialog();
            pnlUserDialog.Controls.Add(btnUCancel);

            try { pnlUserDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlUserDialog.Width, pnlUserDialog.Height, 15, 15)); } catch { }
        }

        private void BuildChangePasswordDialog()
        {
            pnlChangePasswordDialog = new Panel
            {
                Size = new Size(420, 360),
                BackColor = Color.FromArgb(35, 22, 48),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            pnlMainContent.Controls.Add(pnlChangePasswordDialog);
            pnlMainContent.Resize += (s, e) =>
            {
                pnlChangePasswordDialog.Location = new Point(
                    (pnlMainContent.Width - pnlChangePasswordDialog.Width) / 2,
                    (pnlMainContent.Height - pnlChangePasswordDialog.Height) / 2);
            };

            lblCPDialogTitle = new Label
            {
                Text = "Change My Password",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                Size = new Size(380, 30)
            };
            pnlChangePasswordDialog.Controls.Add(lblCPDialogTitle);

            Panel pnlCPCurrent = CreateModernTextBox("Current Password", 20, 60, 380, 45, "🔒", true, out txtCPCurrent);
            Panel pnlCPNew = CreateModernTextBox("New Password", 20, 115, 380, 45, "🔑", true, out txtCPNew);
            Panel pnlCPConfirm = CreateModernTextBox("Confirm New Password", 20, 170, 380, 45, "✅", true, out txtCPConfirm);

            pnlChangePasswordDialog.Controls.Add(pnlCPCurrent);
            pnlChangePasswordDialog.Controls.Add(pnlCPNew);
            pnlChangePasswordDialog.Controls.Add(pnlCPConfirm);

            btnCPSave = new Button
            {
                Text = "UPDATE PASSWORD",
                BackColor = buttonColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(20, 245),
                Size = new Size(185, 45),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCPSave.FlatAppearance.BorderSize = 0;
            btnCPSave.Click += (s, e) => SaveChangePassword();
            pnlChangePasswordDialog.Controls.Add(btnCPSave);

            btnCPCancel = new Button
            {
                Text = "CANCEL",
                BackColor = Color.FromArgb(70, 60, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(215, 245),
                Size = new Size(185, 45),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCPCancel.FlatAppearance.BorderSize = 0;
            btnCPCancel.Click += (s, e) => { pnlChangePasswordDialog.Visible = false; };
            pnlChangePasswordDialog.Controls.Add(btnCPCancel);

            Label lblCPHint = new Label
            {
                Text = "Minimum 4 characters. You will stay logged in.",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 150, 180),
                Location = new Point(20, 300),
                Size = new Size(380, 30)
            };
            pnlChangePasswordDialog.Controls.Add(lblCPHint);

            try { pnlChangePasswordDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlChangePasswordDialog.Width, pnlChangePasswordDialog.Height, 15, 15)); } catch { }
        }

        private void BuildResetPasswordDialog()
        {
            pnlResetPasswordDialog = new Panel
            {
                Size = new Size(420, 320),
                BackColor = Color.FromArgb(35, 22, 48),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            pnlMainContent.Controls.Add(pnlResetPasswordDialog);
            pnlMainContent.Resize += (s, e) =>
            {
                pnlResetPasswordDialog.Location = new Point(
                    (pnlMainContent.Width - pnlResetPasswordDialog.Width) / 2,
                    (pnlMainContent.Height - pnlResetPasswordDialog.Height) / 2);
            };

            lblRPDialogTitle = new Label
            {
                Text = "Reset User Password",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                Size = new Size(380, 30)
            };
            pnlResetPasswordDialog.Controls.Add(lblRPDialogTitle);

            Panel pnlRPNew = CreateModernTextBox("New Password", 20, 60, 380, 45, "🔑", true, out txtRPNew);
            Panel pnlRPConfirm = CreateModernTextBox("Confirm New Password", 20, 115, 380, 45, "✅", true, out txtRPConfirm);
            pnlResetPasswordDialog.Controls.Add(pnlRPNew);
            pnlResetPasswordDialog.Controls.Add(pnlRPConfirm);

            btnRPSave = new Button
            {
                Text = "RESET PASSWORD",
                BackColor = buttonColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(20, 190),
                Size = new Size(185, 45),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRPSave.FlatAppearance.BorderSize = 0;
            btnRPSave.Click += (s, e) => SaveResetPassword();
            pnlResetPasswordDialog.Controls.Add(btnRPSave);

            btnRPCancel = new Button
            {
                Text = "CANCEL",
                BackColor = Color.FromArgb(70, 60, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(215, 190),
                Size = new Size(185, 45),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRPCancel.FlatAppearance.BorderSize = 0;
            btnRPCancel.Click += (s, e) => { pnlResetPasswordDialog.Visible = false; };
            pnlResetPasswordDialog.Controls.Add(btnRPCancel);

            try { pnlResetPasswordDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlResetPasswordDialog.Width, pnlResetPasswordDialog.Height, 15, 15)); } catch { }
        }

        // =====================================================================
        //  NAVIGATION
        // =====================================================================

        private void ShowUsersGrid()
        {
            SetActiveButton(btnUsers);

            lblSectionTitle.Text = "Manage Users";

            // Hide houses UI.
            dgvHouses.Visible = false;
            btnAddHouse.Visible = false;
            btnDeleteHouse.Visible = false;
            txtSearch.Visible = false;
            pnlAddHouseDialog.Visible = false;

            // Hide dashboard cards.
            cardTotalHouses.Visible = false;
            cardAvailableHouses.Visible = false;
            cardRentedHouses.Visible = false;

            // Hide new views
            HideTenantsView();
            HideRentalsView();

            // Show users UI.
            dgvUsers.Visible = true;
            txtUserSearch.Visible = true;
            btnAddUser.Visible = UserSession.IsAdmin;
            ReloadUsersGrid();

            LayoutUserViews();
            UpdateUserActionButtonsVisibility();
        }

        private void ReloadUsersGrid()
        {
            string keyword = userSearchPlaceholderActive ? string.Empty : txtUserSearch.Text.Trim();
            FilterUsers(keyword);
        }

        private void FilterUsers(string keyword)
        {
            dgvUsers.Rows.Clear();

            List<User> users = Services.UserService.GetAll(keyword);
            foreach (User u in users)
            {
                dgvUsers.Rows.Add(
                    u.Id,
                    u.Username,
                    u.Email,
                    u.FullName ?? "—",
                    u.Phone ?? "—",
                    GetRoleDisplayName(u.Role),
                    GetStatusDisplayName(u.Status),
                    u.LastLogin.HasValue ? u.LastLogin.Value.ToString("yyyy-MM-dd HH:mm") : "Never"
                );
            }

            ColorizeUserRows();
            UpdateUserActionButtonsVisibility();
        }

        private void ColorizeUserRows()
        {
            int statusColIndex = dgvUsers.Columns["colUStatus"]?.Index ?? -1;
            int roleColIndex = dgvUsers.Columns["colURole"]?.Index ?? -1;

            foreach (DataGridViewRow row in dgvUsers.Rows)
            {
                if (statusColIndex >= 0 && Convert.ToString(row.Cells[statusColIndex].Value) == "Locked")
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 140, 110);
                }
                else if (roleColIndex >= 0 && Convert.ToString(row.Cells[roleColIndex].Value) == "Administrator")
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 215, 130);
                }
            }
        }

        private void UpdateUserActionButtonsVisibility()
        {
            bool hasSelection = dgvUsers != null && dgvUsers.SelectedRows.Count > 0;
            bool isAdmin = UserSession.IsAdmin;

            btnEditUser.Visible = isAdmin && hasSelection;
            btnDeleteUser.Visible = isAdmin && hasSelection;
            btnResetUserPassword.Visible = isAdmin && hasSelection;
            btnAddUser.Visible = isAdmin;
        }

        // =====================================================================
        //  ADD / EDIT
        // =====================================================================

        private void OpenAddUserDialog()
        {
            userDialogIsEdit = false;
            editingUserId = null;
            lblUserDialogTitle.Text = "Add New User";
            lblUPasswordHint.Text = "Password";
            lblUPasswordHint.Visible = true;
            pnlUPasswordContainer.Visible = true;
            btnUSave.Text = "CREATE USER";

            txtUUsername.Text = "";
            txtUEmail.Text = "";
            txtUFullName.Text = "";
            txtUPhone.Text = "";
            txtUPassword.Text = "";
            cmbURole.SelectedIndex = 2;
            cmbUStatus.SelectedIndex = 0;

            pnlUserDialog.Visible = true;
            pnlUserDialog.BringToFront();
            LayoutUserViews();
            txtUUsername.Focus();
        }

        private void HandleEditUser()
        {
            if (dgvUsers == null || dgvUsers.SelectedRows.Count == 0) return;

            int userId = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["colUId"].Value);
            User user = Services.UserService.GetById(userId);
            if (user == null)
            {
                MessageBox.Show("This user no longer exists.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ReloadUsersGrid();
                return;
            }

            OpenEditUserDialog(user);
        }

        private void OpenEditUserDialog(User user)
        {
            userDialogIsEdit = true;
            editingUserId = user.Id;
            lblUserDialogTitle.Text = "Edit User";
            lblUPasswordHint.Visible = false;
            pnlUPasswordContainer.Visible = false;
            btnUSave.Text = "SAVE CHANGES";

            txtUUsername.Text = user.Username;
            txtUEmail.Text = user.Email;
            txtUFullName.Text = user.FullName ?? string.Empty;
            txtUPhone.Text = user.Phone ?? string.Empty;
            cmbURole.SelectedIndex = (int)user.Role;
            cmbUStatus.SelectedIndex = (int)user.Status;

            pnlUserDialog.Visible = true;
            pnlUserDialog.BringToFront();
            LayoutUserViews();
            txtUUsername.Focus();
        }

        private void SaveUser()
        {
            UserRole role = (UserRole)cmbURole.SelectedIndex;
            UserStatus status = (UserStatus)cmbUStatus.SelectedIndex;

            User candidate = new User
            {
                Id = editingUserId ?? 0,
                Username = txtUUsername.Text,
                Email = txtUEmail.Text,
                FullName = txtUFullName.Text,
                Phone = txtUPhone.Text,
                Role = role,
                Status = status
            };

            UserResult result;
            if (userDialogIsEdit)
            {
                result = Services.UserService.Update(candidate);
            }
            else
            {
                result = Services.UserService.Create(candidate, txtUPassword.Text);
            }

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CloseUserDialog();
                ReloadUsersGrid();
            }
            else
            {
                MessageBox.Show(result.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CloseUserDialog()
        {
            pnlUserDialog.Visible = false;
            editingUserId = null;
            userDialogIsEdit = false;
            LayoutUserViews();
        }

        // =====================================================================
        //  DELETE
        // =====================================================================

        private void HandleDeleteUser()
        {
            if (dgvUsers == null || dgvUsers.SelectedRows.Count == 0) return;

            int userId = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["colUId"].Value);
            string username = Convert.ToString(dgvUsers.SelectedRows[0].Cells["colUUsername"].Value);

            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to delete user '{username}'?\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            UserResult result = Services.UserService.Delete(userId);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReloadUsersGrid();
            }
            else
            {
                MessageBox.Show(result.Message, "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // =====================================================================
        //  RESET PASSWORD (admin)
        // =====================================================================

        private void HandleResetUserPassword()
        {
            if (dgvUsers == null || dgvUsers.SelectedRows.Count == 0) return;

            resetPasswordTargetId = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["colUId"].Value);
            string username = Convert.ToString(dgvUsers.SelectedRows[0].Cells["colUUsername"].Value);

            lblRPDialogTitle.Text = $"Reset Password — {username}";
            txtRPNew.Text = "";
            txtRPConfirm.Text = "";

            pnlResetPasswordDialog.Location = new Point(
                (pnlMainContent.Width - pnlResetPasswordDialog.Width) / 2,
                (pnlMainContent.Height - pnlResetPasswordDialog.Height) / 2);
            pnlResetPasswordDialog.Visible = true;
            pnlResetPasswordDialog.BringToFront();
            txtRPNew.Focus();
        }

        private void SaveResetPassword()
        {
            string newPassword = txtRPNew.Text;
            string confirm = txtRPConfirm.Text;

            if (string.IsNullOrEmpty(newPassword) || newPassword != confirm)
            {
                MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UserResult result = Services.UserService.ResetPassword(resetPasswordTargetId, newPassword);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                pnlResetPasswordDialog.Visible = false;
            }
            else
            {
                MessageBox.Show(result.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // =====================================================================
        //  CHANGE MY PASSWORD
        // =====================================================================

        private void OpenChangePasswordDialog()
        {
            if (!UserSession.IsAuthenticated)
            {
                MessageBox.Show("Please log in first.", "Not Authenticated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            txtCPCurrent.Text = "";
            txtCPNew.Text = "";
            txtCPConfirm.Text = "";

            pnlChangePasswordDialog.Location = new Point(
                (pnlMainContent.Width - pnlChangePasswordDialog.Width) / 2,
                (pnlMainContent.Height - pnlChangePasswordDialog.Height) / 2);

            // Bring the panel above everything currently shown on the main content area.
            pnlChangePasswordDialog.BringToFront();
            pnlChangePasswordDialog.Visible = true;
            txtCPCurrent.Focus();
        }

        private void SaveChangePassword()
        {
            if (!UserSession.IsAuthenticated) return;

            string current = txtCPCurrent.Text;
            string next = txtCPNew.Text;
            string confirm = txtCPConfirm.Text;

            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(next))
            {
                MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (next != confirm)
            {
                MessageBox.Show("New password and confirmation do not match.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UserResult result = Services.UserService.ChangePassword(UserSession.CurrentUser.Id, current, next);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                pnlChangePasswordDialog.Visible = false;
            }
            else
            {
                MessageBox.Show(result.Message, "Cannot Change Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // =====================================================================
        //  DISPLAY HELPERS
        // =====================================================================

        private static string GetRoleDisplayName(UserRole role)
        {
            switch (role)
            {
                case UserRole.Admin: return "Administrator";
                case UserRole.Manager: return "Manager";
                default: return "User";
            }
        }

        private static string GetStatusDisplayName(UserStatus status)
        {
            return status == UserStatus.Active ? "Active" : "Locked";
        }
    }
}
