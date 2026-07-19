using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace house_management
{
    public partial class Form1
    {
        // --- Tenants Content Controls ---
        private DataGridView dgvTenants;
        private TextBox txtTenantSearch;
        private Button btnAddTenant;
        private Button btnDeleteTenant;
        private bool tenantSearchPlaceholderActive = true;

        // --- Add Tenant Dialog ---
        private Panel pnlTenantDialog;
        private Label lblTenantDialogTitle;
        private TextBox txtTenantName;
        private TextBox txtTenantEmail;
        private TextBox txtTenantPhone;
        private Button btnTenantSave;
        private Button btnTenantCancel;

        // =====================================================================
        //  BUILD
// =====================================================================

        private void BuildTenantsView()
        {
            txtTenantSearch = new TextBox
            {
                Multiline = true,
                Text = " 🔍 Search tenants...",
                Font = new Font("Segoe UI", 11, FontStyle.Italic),
                BackColor = inputBgColor,
                ForeColor = Color.DarkGray,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(200, 40),
                Location = new Point(230, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false
            };

            txtTenantSearch.TextChanged += (s, e) =>
            {
                if (!tenantSearchPlaceholderActive)
                {
                    FilterTenants(txtTenantSearch.Text.Trim());
                }
            };
            txtTenantSearch.Enter += (s, e) =>
            {
                if (tenantSearchPlaceholderActive)
                {
                    txtTenantSearch.Text = "";
                    txtTenantSearch.ForeColor = Color.White;
                    txtTenantSearch.Font = new Font("Segoe UI", 11, FontStyle.Regular);
                    tenantSearchPlaceholderActive = false;
                }
            };
            txtTenantSearch.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtTenantSearch.Text))
                {
                    txtTenantSearch.Text = " 🔍 Search tenants...";
                    txtTenantSearch.ForeColor = Color.DarkGray;
                    txtTenantSearch.Font = new Font("Segoe UI", 11, FontStyle.Italic);
                    tenantSearchPlaceholderActive = true;
                }
            };
            pnlMainContent.Controls.Add(txtTenantSearch);

            btnDeleteTenant = CreateActionButton("🗑 Delete Tenant", deleteBtnColor, 480, 180);
            btnDeleteTenant.Click += (s, e) => HandleDeleteTenant();
            pnlMainContent.Controls.Add(btnDeleteTenant);

            btnAddTenant = CreateActionButton("+ Add Tenant", buttonColor, 670, 160);
            btnAddTenant.Click += (s, e) => OpenAddTenantDialog();
            pnlMainContent.Controls.Add(btnAddTenant);

            BuildTenantsGrid();
            BuildTenantDialog();
        }

        private Button CreateActionButton(string text, Color backColor, int x, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = backColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(width, 40),
                Location = new Point(x, 20),
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

        private void BuildTenantsGrid()
        {
            dgvTenants = new DataGridView
            {
                Size = new Size(800, 470),
                Location = new Point(25, 90),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
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

            dgvTenants.SelectionChanged += (s, e) =>
            {
                btnDeleteTenant.Visible = dgvTenants.SelectedRows.Count > 0;
            };

            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(45, 30, 60),
                ForeColor = Color.FromArgb(255, 60, 130),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgvTenants.ColumnHeadersDefaultCellStyle = headerStyle;
            dgvTenants.ColumnHeadersHeight = 50;

            DataGridViewCellStyle rowStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(32, 22, 44),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11, FontStyle.Regular),
                SelectionBackColor = Color.FromArgb(215, 45, 95),
                SelectionForeColor = Color.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgvTenants.RowsDefaultCellStyle = rowStyle;
            dgvTenants.RowTemplate.Height = 42;

            dgvTenants.Columns.Add("TenantID", "Tenant ID");
            dgvTenants.Columns.Add("TenantName", "Name");
            dgvTenants.Columns.Add("TenantEmail", "Email Address");
            dgvTenants.Columns.Add("TenantPhone", "Phone Number");

            pnlMainContent.Controls.Add(dgvTenants);
        }

        private void BuildTenantDialog()
        {
            pnlTenantDialog = new Panel
            {
                Size = new Size(400, 320),
                BackColor = Color.FromArgb(35, 22, 48),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            pnlMainContent.Controls.Add(pnlTenantDialog);
            pnlMainContent.Resize += (s, e) =>
            {
                pnlTenantDialog.Location = new Point(
                    (pnlMainContent.Width - pnlTenantDialog.Width) / 2,
                    (pnlMainContent.Height - pnlTenantDialog.Height) / 2);
            };

            lblTenantDialogTitle = new Label
            {
                Text = "Add New Tenant Details",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                Size = new Size(300, 30)
            };
            pnlTenantDialog.Controls.Add(lblTenantDialogTitle);

            Panel pnlTenantNameContainer = CreateModernTextBox("Tenant Name", 20, 60, 360, 45, "👤", false, out txtTenantName);
            pnlTenantDialog.Controls.Add(pnlTenantNameContainer);

            Panel pnlTenantEmailContainer = CreateModernTextBox("Email Address", 20, 120, 360, 45, "✉️", false, out txtTenantEmail);
            pnlTenantDialog.Controls.Add(pnlTenantEmailContainer);

            Panel pnlTenantPhoneContainer = CreateModernTextBox("Phone Number", 20, 180, 360, 45, "📞", false, out txtTenantPhone);
            pnlTenantDialog.Controls.Add(pnlTenantPhoneContainer);

            btnTenantSave = new Button { Text = "SAVE", BackColor = buttonColor, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(20, 245), Size = new Size(160, 45), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnTenantCancel = new Button { Text = "CANCEL", BackColor = Color.FromArgb(70, 60, 80), ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(220, 245), Size = new Size(160, 45), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

            btnTenantSave.FlatAppearance.BorderSize = 0;
            btnTenantCancel.FlatAppearance.BorderSize = 0;

            btnTenantSave.Click += (s, e) => HandleSaveTenant();
            btnTenantCancel.Click += (s, e) => { pnlTenantDialog.Visible = false; };

            pnlTenantDialog.Controls.Add(btnTenantSave);
            pnlTenantDialog.Controls.Add(btnTenantCancel);

            try { pnlTenantDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlTenantDialog.Width, pnlTenantDialog.Height, 15, 15)); } catch { }
        }

        // =====================================================================
        //  ACTIONS & CRUD ROUTING
        // =====================================================================

        private void ShowTenantsGrid()
        {
            SetActiveButton(btnTenants);

            lblSectionTitle.Text = "Manage Tenant Users";

            // Hide other views
            HideDashboardDataView();
            HideHousesViewHelper();
            HideUsersView();
            HideRentalsView();

            // Show Tenant View
            dgvTenants.Visible = true;
            txtTenantSearch.Visible = true;
            btnAddTenant.Visible = true;
            btnDeleteTenant.Visible = dgvTenants.SelectedRows.Count > 0;

            ReloadTenantsGrid();
        }

        private void HideTenantsView()
        {
            if (dgvTenants != null) dgvTenants.Visible = false;
            if (txtTenantSearch != null) txtTenantSearch.Visible = false;
            if (btnAddTenant != null) btnAddTenant.Visible = false;
            if (btnDeleteTenant != null) btnDeleteTenant.Visible = false;
            if (pnlTenantDialog != null) pnlTenantDialog.Visible = false;
        }

        private void ReloadTenantsGrid()
        {
            string keyword = tenantSearchPlaceholderActive ? "" : txtTenantSearch.Text.Trim();
            FilterTenants(keyword);
        }

        private void FilterTenants(string keyword)
        {
            dgvTenants.Rows.Clear();
            var list = DatabaseHelper.GetTenants(keyword);
            foreach (var t in list)
            {
                dgvTenants.Rows.Add(t.ID, t.Name, t.Email, t.Phone);
            }
        }

        private void OpenAddTenantDialog()
        {
            txtTenantName.Text = "";
            txtTenantEmail.Text = "";
            txtTenantPhone.Text = "";
            pnlTenantDialog.Location = new Point(
                (pnlMainContent.Width - pnlTenantDialog.Width) / 2,
                (pnlMainContent.Height - pnlTenantDialog.Height) / 2);
            pnlTenantDialog.Visible = true;
            pnlTenantDialog.BringToFront();
            txtTenantName.Focus();
        }

        private void HandleSaveTenant()
        {
            string name = txtTenantName.Text.Trim();
            string email = txtTenantEmail.Text.Trim();
            string phone = txtTenantPhone.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Please fill in all details.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool success = DatabaseHelper.AddTenant(name, email, phone);
            if (success)
            {
                pnlTenantDialog.Visible = false;
                ReloadTenantsGrid();
            }
            else
            {
                MessageBox.Show("Could not add tenant. Please check if the email address already exists.", "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleDeleteTenant()
        {
            if (dgvTenants.SelectedRows.Count == 0) return;

            string id = dgvTenants.SelectedRows[0].Cells["TenantID"].Value.ToString();
            string name = dgvTenants.SelectedRows[0].Cells["TenantName"].Value.ToString();

            var res = MessageBox.Show($"Are you sure you want to delete tenant '{name}'? This will terminate all their active rental agreements.", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                DatabaseHelper.DeleteTenant(id);
                ReloadTenantsGrid();
            }
        }

        // --- Helper methods to hide views from other modules ---
        private void HideDashboardDataView()
        {
            if (cardTotalHouses != null) cardTotalHouses.Visible = false;
            if (cardAvailableHouses != null) cardAvailableHouses.Visible = false;
            if (cardRentedHouses != null) cardRentedHouses.Visible = false;
        }

        private void HideHousesViewHelper()
        {
            if (dgvHouses != null) dgvHouses.Visible = false;
            if (btnAddHouse != null) btnAddHouse.Visible = false;
            if (btnDeleteHouse != null) btnDeleteHouse.Visible = false;
            if (txtSearch != null) txtSearch.Visible = false;
            if (pnlAddHouseDialog != null) pnlAddHouseDialog.Visible = false;
        }
    }
}
