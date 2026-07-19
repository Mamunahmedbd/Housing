using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using house_management.Models;
using house_management.Services;

namespace house_management
{
    /// <summary>
    /// Tenant-management view for the main form. Kept as a partial class so
    /// Form1.cs stays focused on Houses while all tenant CRUD UI lives here.
    /// Mirrors the architecture of <see cref="UsersView"/>: the UI only talks
    /// to <see cref="TenantService"/>, never to the database directly.
    /// </summary>
    public partial class Form1
    {
        // --- Tenants content controls ---
        private DataGridView dgvTenants;
        private TextBox txtTenantSearch;
        private Button btnAddTenant;
        private Button btnEditTenant;
        private Button btnDeleteTenant;
        private bool tenantSearchPlaceholderActive = true;

        // --- Add/Edit tenant dialog ---
        private Panel pnlTenantDialog;
        private Label lblTenantDialogTitle;
        private TextBox txtTenantName;
        private TextBox txtTenantEmail;
        private TextBox txtTenantPhone;
        private Button btnTenantSave;
        private Button btnTenantCancel;
        private int? editingTenantId;
        private bool tenantDialogIsEdit;

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

            btnDeleteTenant = CreateActionButton("🗑 Delete", deleteBtnColor, 430, ActionButtonWidth);
            btnDeleteTenant.Click += (s, e) => HandleDeleteTenant();
            pnlMainContent.Controls.Add(btnDeleteTenant);

            btnEditTenant = CreateActionButton("✎ Edit", Color.FromArgb(70, 110, 90), 580, ActionButtonWidth);
            btnEditTenant.Click += (s, e) => HandleEditTenant();
            pnlMainContent.Controls.Add(btnEditTenant);

            btnAddTenant = CreateActionButton("+ Add Tenant", buttonColor, 710, ActionButtonWidth);
            btnAddTenant.Click += (s, e) => OpenAddTenantDialog();
            pnlMainContent.Controls.Add(btnAddTenant);

            BuildTenantsGrid();
            BuildTenantDialog();

            dgvTenants.CellDoubleClick += (s, e) => HandleEditTenant();
        }

        private void BuildTenantsGrid()
        {
            dgvTenants = new DataGridView
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

            dgvTenants.SelectionChanged += (s, e) => UpdateTenantActionButtonsVisibility();

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

            dgvTenants.Columns.Add("colTenantId", "ID");
            dgvTenants.Columns.Add("colTenantName", "Name");
            dgvTenants.Columns.Add("colTenantEmail", "Email");
            dgvTenants.Columns.Add("colTenantPhone", "Phone");
            dgvTenants.Columns.Add("colTenantCreated", "Registered On");
            dgvTenants.Columns["colTenantId"].Visible = false;

            pnlMainContent.Controls.Add(dgvTenants);
        }

        private void LayoutTenantViews()
        {
            if (pnlTenantDialog == null || dgvTenants == null || pnlMainContent == null) return;

            // All three action buttons cascade right-to-left via the shared
            // helper, guaranteeing consistent spacing with the other modules.
            LayoutActionBarRow(btnDeleteTenant, btnEditTenant, btnAddTenant);

            if (pnlTenantDialog.Visible)
            {
                pnlTenantDialog.Width = 380;
                pnlTenantDialog.Height = pnlMainContent.Height - 115;
                pnlTenantDialog.Location = new Point(pnlMainContent.Width - 380 - 25, 90);
                dgvTenants.Width = pnlMainContent.Width - 380 - 60;
                try { pnlTenantDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlTenantDialog.Width, pnlTenantDialog.Height, 15, 15)); } catch { }
            }
            else
            {
                dgvTenants.Width = pnlMainContent.Width - 50;
            }
            dgvTenants.Height = pnlMainContent.Height - 115;
        }

        private void BuildTenantDialog()
        {
            pnlTenantDialog = new Panel
            {
                Size = new Size(380, 470),
                BackColor = Color.FromArgb(35, 22, 48),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            pnlMainContent.Controls.Add(pnlTenantDialog);
            pnlMainContent.Resize += (s, e) =>
            {
                LayoutTenantViews();
            };

            lblTenantDialogTitle = new Label
            {
                Text = "Add New Tenant",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(300, 30)
            };
            pnlTenantDialog.Controls.Add(lblTenantDialogTitle);

            Panel pnlName = CreateModernTextBox("Tenant Name", 20, 75, 340, 45, "👤", false, out txtTenantName);
            Panel pnlEmail = CreateModernTextBox("Email Address", 20, 140, 340, 45, "✉", false, out txtTenantEmail);
            Panel pnlPhone = CreateModernTextBox("Phone Number", 20, 205, 340, 45, "📞", false, out txtTenantPhone);
            pnlTenantDialog.Controls.Add(pnlName);
            pnlTenantDialog.Controls.Add(pnlEmail);
            pnlTenantDialog.Controls.Add(pnlPhone);

            btnTenantSave = new Button
            {
                Text = "SAVE",
                BackColor = buttonColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(20, 270),
                Size = new Size(160, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTenantSave.FlatAppearance.BorderSize = 0;
            btnTenantSave.Click += (s, e) => SaveTenant();
            pnlTenantDialog.Controls.Add(btnTenantSave);

            btnTenantCancel = new Button
            {
                Text = "CANCEL",
                BackColor = Color.FromArgb(70, 60, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(200, 270),
                Size = new Size(160, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTenantCancel.FlatAppearance.BorderSize = 0;
            btnTenantCancel.Click += (s, e) => CloseTenantDialog();
            pnlTenantDialog.Controls.Add(btnTenantCancel);

            Label lblHint = new Label
            {
                Text = "All fields are required. Email must be unique.",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 150, 180),
                Location = new Point(20, 305),
                Size = new Size(380, 30)
            };
            pnlTenantDialog.Controls.Add(lblHint);

            try { pnlTenantDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlTenantDialog.Width, pnlTenantDialog.Height, 15, 15)); } catch { }
        }

        // =====================================================================
        //  NAVIGATION
        // =====================================================================

        private void ShowTenantsGrid()
        {
            SetActiveButton(btnTenants);

            lblSectionTitle.Text = "Manage Tenant Users";

            // Hide other modules' UI.
            HideDashboardDataView();
            HideHousesViewHelper();
            HideUsersView();
            HideRentalsView();

            // Show tenant UI.
            dgvTenants.Visible = true;
            txtTenantSearch.Visible = true;
            btnAddTenant.Visible = true;

            LayoutTenantViews();
            ReloadTenantsGrid();
            UpdateTenantActionButtonsVisibility();
        }

        private void HideTenantsView()
        {
            if (dgvTenants != null) dgvTenants.Visible = false;
            if (txtTenantSearch != null) txtTenantSearch.Visible = false;
            if (btnAddTenant != null) btnAddTenant.Visible = false;
            if (btnEditTenant != null) btnEditTenant.Visible = false;
            if (btnDeleteTenant != null) btnDeleteTenant.Visible = false;
            if (pnlTenantDialog != null) pnlTenantDialog.Visible = false;
        }

        private void ReloadTenantsGrid()
        {
            string keyword = tenantSearchPlaceholderActive ? string.Empty : txtTenantSearch.Text.Trim();
            FilterTenants(keyword);
        }

        private void FilterTenants(string keyword)
        {
            dgvTenants.Rows.Clear();

            List<Tenant> tenants = TenantService.GetAll(keyword);
            foreach (Tenant t in tenants)
            {
                dgvTenants.Rows.Add(
                    t.Id,
                    t.Name,
                    t.Email,
                    t.Phone,
                    t.CreatedAt.HasValue ? t.CreatedAt.Value.ToString("yyyy-MM-dd") : "—"
                );
            }

            UpdateTenantActionButtonsVisibility();
        }

        private void UpdateTenantActionButtonsVisibility()
        {
            bool hasSelection = dgvTenants != null && dgvTenants.SelectedRows.Count > 0;
            if (btnEditTenant != null) btnEditTenant.Visible = hasSelection;
            if (btnDeleteTenant != null) btnDeleteTenant.Visible = hasSelection;
        }

        // =====================================================================
        //  ADD / EDIT
        // =====================================================================

        private void OpenAddTenantDialog()
        {
            tenantDialogIsEdit = false;
            editingTenantId = null;
            lblTenantDialogTitle.Text = "Add New Tenant";
            btnTenantSave.Text = "CREATE TENANT";

            txtTenantName.Text = "";
            txtTenantEmail.Text = "";
            txtTenantPhone.Text = "";

            pnlTenantDialog.Visible = true;
            pnlTenantDialog.BringToFront();
            LayoutTenantViews();
            txtTenantName.Focus();
        }

        private void HandleEditTenant()
        {
            if (dgvTenants == null || dgvTenants.SelectedRows.Count == 0) return;

            int tenantId = Convert.ToInt32(dgvTenants.SelectedRows[0].Cells["colTenantId"].Value);
            Tenant tenant = TenantService.GetById(tenantId);
            if (tenant == null)
            {
                MessageBox.Show("This tenant no longer exists.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ReloadTenantsGrid();
                return;
            }

            OpenEditTenantDialog(tenant);
        }

        private void OpenEditTenantDialog(Tenant tenant)
        {
            tenantDialogIsEdit = true;
            editingTenantId = tenant.Id;
            lblTenantDialogTitle.Text = "Edit Tenant";
            btnTenantSave.Text = "SAVE CHANGES";

            txtTenantName.Text = tenant.Name;
            txtTenantEmail.Text = tenant.Email;
            txtTenantPhone.Text = tenant.Phone;

            pnlTenantDialog.Visible = true;
            pnlTenantDialog.BringToFront();
            LayoutTenantViews();
            txtTenantName.Focus();
        }

        private void SaveTenant()
        {
            Tenant candidate = new Tenant
            {
                Id = editingTenantId ?? 0,
                Name = txtTenantName.Text,
                Email = txtTenantEmail.Text,
                Phone = txtTenantPhone.Text
            };

            TenantResult result = tenantDialogIsEdit
                ? TenantService.Update(candidate)
                : TenantService.Create(candidate);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CloseTenantDialog();
                ReloadTenantsGrid();
            }
            else
            {
                MessageBox.Show(result.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CloseTenantDialog()
        {
            pnlTenantDialog.Visible = false;
            editingTenantId = null;
            tenantDialogIsEdit = false;
            LayoutTenantViews();
        }



        // =====================================================================
        //  DELETE
        // =====================================================================

        private void HandleDeleteTenant()
        {
            if (dgvTenants == null || dgvTenants.SelectedRows.Count == 0) return;

            int tenantId = Convert.ToInt32(dgvTenants.SelectedRows[0].Cells["colTenantId"].Value);
            string name = Convert.ToString(dgvTenants.SelectedRows[0].Cells["colTenantName"].Value);

            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to delete tenant '{name}'?\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            TenantResult result = TenantService.Delete(tenantId);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReloadTenantsGrid();
            }
            else
            {
                MessageBox.Show(result.Message, "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // =====================================================================
        //  SHARED HIDE HELPERS
        //  Also used by other module views; kept here for the tenant module.
        // =====================================================================

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
