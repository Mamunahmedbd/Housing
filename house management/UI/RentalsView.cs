using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace house_management
{
    public partial class Form1
    {
        // --- Rentals Content Controls ---
        private DataGridView dgvRentals;
        private TextBox txtRentalSearch;
        private Button btnAddRental;
        private Button btnDeleteRental;
        private bool rentalSearchPlaceholderActive = true;

        // --- Add Rental Dialog ---
        private Panel pnlRentalDialog;
        private Label lblRentalDialogTitle;
        private ComboBox cmbRentalHouse;
        private ComboBox cmbRentalTenant;
        private TextBox txtRentalAmount;
        private DateTimePicker dtpRentalStart;
        private DateTimePicker dtpRentalEnd;
        private Button btnRentalSave;
        private Button btnRentalCancel;

        // =====================================================================
        //  BUILD
        // =====================================================================

        private void BuildRentalsView()
        {
            txtRentalSearch = new TextBox
            {
                Multiline = true,
                Text = " 🔍 Search rentals...",
                Font = new Font("Segoe UI", 11, FontStyle.Italic),
                BackColor = inputBgColor,
                ForeColor = Color.DarkGray,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(200, 40),
                Location = new Point(230, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false
            };

            txtRentalSearch.TextChanged += (s, e) =>
            {
                if (!rentalSearchPlaceholderActive)
                {
                    FilterRentals(txtRentalSearch.Text.Trim());
                }
            };
            txtRentalSearch.Enter += (s, e) =>
            {
                if (rentalSearchPlaceholderActive)
                {
                    txtRentalSearch.Text = "";
                    txtRentalSearch.ForeColor = Color.White;
                    txtRentalSearch.Font = new Font("Segoe UI", 11, FontStyle.Regular);
                    rentalSearchPlaceholderActive = false;
                }
            };
            txtRentalSearch.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtRentalSearch.Text))
                {
                    txtRentalSearch.Text = " 🔍 Search rentals...";
                    txtRentalSearch.ForeColor = Color.DarkGray;
                    txtRentalSearch.Font = new Font("Segoe UI", 11, FontStyle.Italic);
                    rentalSearchPlaceholderActive = true;
                }
            };
            pnlMainContent.Controls.Add(txtRentalSearch);

            btnDeleteRental = CreateActionButton("🗑 Terminate", deleteBtnColor, 580, ActionButtonWidth);
            btnDeleteRental.Click += (s, e) => HandleDeleteRental();
            pnlMainContent.Controls.Add(btnDeleteRental);

            btnAddRental = CreateActionButton("+ Add Rental", buttonColor, 710, ActionButtonWidth);
            btnAddRental.Click += (s, e) => OpenAddRentalDialog();
            pnlMainContent.Controls.Add(btnAddRental);

            BuildRentalsGrid();
            BuildRentalDialog();
        }

        private void LayoutRentalViews()
        {
            if (pnlRentalDialog == null || dgvRentals == null || pnlMainContent == null) return;

            // Action buttons cascade right-to-left via the shared helper.
            LayoutActionBarRow(btnDeleteRental, btnAddRental);

            if (pnlRentalDialog.Visible)
            {
                pnlRentalDialog.Width = 380;
                pnlRentalDialog.Height = pnlMainContent.Height - 115;
                pnlRentalDialog.Location = new Point(pnlMainContent.Width - 380 - 25, 90);
                dgvRentals.Width = pnlMainContent.Width - 380 - 60;
                try { pnlRentalDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlRentalDialog.Width, pnlRentalDialog.Height, 15, 15)); } catch { }
            }
            else
            {
                dgvRentals.Width = pnlMainContent.Width - 50;
            }
            dgvRentals.Height = pnlMainContent.Height - 115;
        }

        private void BuildRentalsGrid()
        {
            dgvRentals = new DataGridView
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

            dgvRentals.SelectionChanged += (s, e) =>
            {
                btnDeleteRental.Visible = dgvRentals.SelectedRows.Count > 0;
            };

            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(45, 30, 60),
                ForeColor = Color.FromArgb(255, 60, 130),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgvRentals.ColumnHeadersDefaultCellStyle = headerStyle;
            dgvRentals.ColumnHeadersHeight = 50;

            DataGridViewCellStyle rowStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(32, 22, 44),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11, FontStyle.Regular),
                SelectionBackColor = Color.FromArgb(215, 45, 95),
                SelectionForeColor = Color.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgvRentals.RowsDefaultCellStyle = rowStyle;
            dgvRentals.RowTemplate.Height = 42;

            dgvRentals.Columns.Add("RentalID", "Contract ID");
            dgvRentals.Columns.Add("RentalHouse", "House Name");
            dgvRentals.Columns.Add("RentalTenant", "Tenant Name");
            dgvRentals.Columns.Add("RentalAmount", "Rent Amount");
            dgvRentals.Columns.Add("RentalStart", "Start Date");
            dgvRentals.Columns.Add("RentalEnd", "End Date");
            dgvRentals.Columns.Add("RentalStatus", "Status");

            pnlMainContent.Controls.Add(dgvRentals);
        }

        private void BuildRentalDialog()
        {
            pnlRentalDialog = new Panel
            {
                Size = new Size(380, 470),
                BackColor = Color.FromArgb(35, 22, 48),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            pnlMainContent.Controls.Add(pnlRentalDialog);
            pnlMainContent.Resize += (s, e) =>
            {
                LayoutRentalViews();
            };

            lblRentalDialogTitle = new Label
            {
                Text = "New Rental Contract",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(300, 30)
            };
            pnlRentalDialog.Controls.Add(lblRentalDialogTitle);

            Label lblHouse = new Label { Text = "House", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(200, 190, 210), Location = new Point(20, 60), Size = new Size(160, 18) };
            Panel pnlHouseCombo = CreateModernComboBox(20, 80, 160, 45, "🏠", out cmbRentalHouse);
            pnlRentalDialog.Controls.Add(lblHouse);
            pnlRentalDialog.Controls.Add(pnlHouseCombo);

            Label lblTenant = new Label { Text = "Tenant", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(200, 190, 210), Location = new Point(200, 60), Size = new Size(160, 18) };
            Panel pnlTenantCombo = CreateModernComboBox(200, 80, 160, 45, "👤", out cmbRentalTenant);
            pnlRentalDialog.Controls.Add(lblTenant);
            pnlRentalDialog.Controls.Add(pnlTenantCombo);

            Panel pnlRentContainer = CreateModernTextBox("Rent Amount (e.g. 1200)", 20, 135, 340, 45, "💵", false, out txtRentalAmount);
            pnlRentalDialog.Controls.Add(pnlRentContainer);

            Label lblStart = new Label { Text = "Start Date", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(200, 190, 210), Location = new Point(20, 195), Size = new Size(160, 18) };
            Panel pnlStartContainer = CreateModernDateTimePicker(20, 215, 160, 45, "📅", out dtpRentalStart);
            pnlRentalDialog.Controls.Add(lblStart);
            pnlRentalDialog.Controls.Add(pnlStartContainer);

            Label lblEnd = new Label { Text = "End Date", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(200, 190, 210), Location = new Point(200, 195), Size = new Size(160, 18) };
            Panel pnlEndContainer = CreateModernDateTimePicker(200, 215, 160, 45, "📅", out dtpRentalEnd);
            pnlRentalDialog.Controls.Add(lblEnd);
            pnlRentalDialog.Controls.Add(pnlEndContainer);

            btnRentalSave = new Button { Text = "SAVE", BackColor = buttonColor, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(20, 280), Size = new Size(160, 40), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRentalCancel = new Button { Text = "CANCEL", BackColor = Color.FromArgb(70, 60, 80), ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(200, 280), Size = new Size(160, 40), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

            btnRentalSave.FlatAppearance.BorderSize = 0;
            btnRentalCancel.FlatAppearance.BorderSize = 0;

            btnRentalSave.Click += (s, e) => HandleSaveRental();
            btnRentalCancel.Click += (s, e) => { pnlRentalDialog.Visible = false; LayoutRentalViews(); };

            pnlRentalDialog.Controls.Add(btnRentalSave);
            pnlRentalDialog.Controls.Add(btnRentalCancel);

            try { pnlRentalDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlRentalDialog.Width, pnlRentalDialog.Height, 15, 15)); } catch { }
        }

        // =====================================================================
        //  ACTIONS & CRUD ROUTING
        // =====================================================================

        private void ShowRentalsGrid()
        {
            SetActiveButton(btnRentals);

            lblSectionTitle.Text = "Manage Rentals";

            // Hide other views
            HideDashboardDataView();
            HideHousesViewHelper();
            HideUsersView();
            HideTenantsView();

            // Show Rental View
            dgvRentals.Visible = true;
            txtRentalSearch.Visible = true;
            btnAddRental.Visible = true;
            btnDeleteRental.Visible = dgvRentals.SelectedRows.Count > 0;

            LayoutRentalViews();
            ReloadRentalsGrid();
        }

        private void HideRentalsView()
        {
            if (dgvRentals != null) dgvRentals.Visible = false;
            if (txtRentalSearch != null) txtRentalSearch.Visible = false;
            if (btnAddRental != null) btnAddRental.Visible = false;
            if (btnDeleteRental != null) btnDeleteRental.Visible = false;
            if (pnlRentalDialog != null) pnlRentalDialog.Visible = false;
        }

        private void ReloadRentalsGrid()
        {
            string keyword = rentalSearchPlaceholderActive ? "" : txtRentalSearch.Text.Trim();
            FilterRentals(keyword);
        }

        private void FilterRentals(string keyword)
        {
            dgvRentals.Rows.Clear();
            var list = DatabaseHelper.GetRentals(keyword);
            foreach (var r in list)
            {
                dgvRentals.Rows.Add(r.ID, r.HouseName, r.TenantName, "$" + r.RentAmount, r.StartDate, r.EndDate, r.Status);
            }
        }

        private void OpenAddRentalDialog()
        {
            // Populate House dropdown (only available properties)
            cmbRentalHouse.Items.Clear();
            var houses = Services.HouseService.GetAll();
            foreach (var h in houses)
            {
                if (h.Status == Models.HouseStatus.Available)
                {
                    cmbRentalHouse.Items.Add(new RentalComboItem { ID = h.Id.ToString(), Name = h.Name });
                }
            }
            if (cmbRentalHouse.Items.Count > 0) cmbRentalHouse.SelectedIndex = 0;

            // Populate Tenant dropdown
            cmbRentalTenant.Items.Clear();
            var tenants = Services.TenantService.GetAll();
            foreach (var t in tenants)
            {
                cmbRentalTenant.Items.Add(new RentalComboItem { ID = t.Id.ToString(), Name = t.Name });
            }
            if (cmbRentalTenant.Items.Count > 0) cmbRentalTenant.SelectedIndex = 0;

            txtRentalAmount.Text = "";
            dtpRentalStart.Value = DateTime.Now;
            dtpRentalEnd.Value = DateTime.Now.AddYears(1);

            pnlRentalDialog.Visible = true;
            pnlRentalDialog.BringToFront();
            LayoutRentalViews();
            txtRentalAmount.Focus();
        }

        private void HandleSaveRental()
        {
            if (cmbRentalHouse.SelectedItem == null || cmbRentalTenant.SelectedItem == null)
            {
                MessageBox.Show("Please select a house and tenant.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string houseId = ((RentalComboItem)cmbRentalHouse.SelectedItem).ID;
            string tenantId = ((RentalComboItem)cmbRentalTenant.SelectedItem).ID;
            string amountStr = txtRentalAmount.Text.Trim();

            if (!decimal.TryParse(amountStr, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid rent amount.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime start = dtpRentalStart.Value.Date;
            DateTime end = dtpRentalEnd.Value.Date;
            if (start >= end)
            {
                MessageBox.Show("Please enter valid start and end dates. Start date must be before end date.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool success = DatabaseHelper.AddRental(houseId, tenantId, amount, start, end);
            if (success)
            {
                pnlRentalDialog.Visible = false;
                LayoutRentalViews();
                ReloadRentalsGrid();
            }
            else
            {
                MessageBox.Show("Could not add rental agreement contract.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleDeleteRental()
        {
            if (dgvRentals.SelectedRows.Count == 0) return;

            string id = dgvRentals.SelectedRows[0].Cells["RentalID"].Value.ToString();
            string houseName = dgvRentals.SelectedRows[0].Cells["RentalHouse"].Value.ToString();

            var res = MessageBox.Show($"Are you sure you want to terminate rental contract for house '{houseName}'? This will free the house status back to 'Available'.", "Confirm Termination", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                DatabaseHelper.DeleteRental(id);
                ReloadRentalsGrid();
            }
        }
    }

    public class RentalComboItem
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}
