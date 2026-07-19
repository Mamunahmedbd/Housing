using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace house_management
{
    public class House
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
    }

    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        // In-memory houseList is removed. DatabaseHelper is used instead.

        // --- عناصر شاشة تسجيل الدخول ---
        private Panel pnlGlassCard;
        private Label lblWelcome;
        private TextBox txtEmail;
        private TextBox txtPassword;
        private Panel pnlEmailContainer;
        private Panel pnlPasswordContainer;
        private Button btnLogin;
        private Label lblForgotPass;
        private Button btnTogglePassword;
        private bool isPasswordVisible = false;

        // --- Window control box buttons ---
        private Panel pnlControlBox;
        private Button btnHeaderMinimize;
        private Button btnHeaderExit;

        // --- Dragging & Window APIs ---
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int EM_SETCUEBANNER = 0x1501;

        private void DragForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        // --- عناصر شاشة استعادة كلمة المرور ---
        private Panel pnlForgotPassDialog;
        private TextBox txtFPUsername;
        private Button btnSendReset;
        private Button btnCancelReset;
        private Label lblFPTitle;

        // --- عناصر لوحة التحكم الجانبية والعلوية ---
        private Panel pnlSidebar;
        private Panel pnlHeader;
        private Label lblAppTitle;
        private Button btnDashboard;
        private Button btnHouses;
        private Button btnLogout;
        private Label lblWelcomeUser;

        // --- حاوية المحتوى الرئيسية والجدول والبحث ---
        private Panel pnlMainContent;
        private DataGridView dgvHouses;
        private Button btnAddHouse;
        private Button btnDeleteHouse;
        private Label lblSectionTitle;
        private TextBox txtSearch;

        // --- عناصر واجهة الـ Dashboard ---
        private Panel cardTotalHouses;
        private Panel cardAvailableHouses;
        private Panel cardRentedHouses;

        private Label lblTotalValue;
        private Label lblAvailableValue;
        private Label lblRentedValue;

        // لوحة إضافة منزل جديد 
        private Panel pnlAddHouseDialog;
        private TextBox txtNewName;
        private TextBox txtNewAddress;
        private ComboBox cmbNewStatus;

        // الألوان المعتمدة بالتصميم الفخم
        private Color cardColor = Color.FromArgb(26, 16, 36);
        private Color inputBgColor = Color.FromArgb(42, 26, 54);
        private Color buttonColor = Color.FromArgb(215, 45, 95);
        private Color sidebarBg = Color.FromArgb(20, 12, 28);
        private Color activeBtnColor = Color.FromArgb(215, 45, 95);
        private Color deleteBtnColor = Color.FromArgb(190, 35, 65);
        private Color deleteBtnHoverColor = Color.FromArgb(230, 45, 80);

        public Form1()
        {
            try { SetProcessDPIAware(); } catch { }

            InitializeComponent();
            SetupFormLayout();

            // Allow dragging the form by clicking on the background
            this.MouseDown += DragForm_MouseDown;

            BuildLoginUI();
            BuildForgotPasswordDialog();
            BuildDashboardUI();
            BuildMainContentArea();
            BuildDashboardCards();
            BuildAddHouseDialog();
            BuildControlBox();

            pnlGlassCard.Visible = true;
            pnlSidebar.Visible = false;
            pnlHeader.Visible = false;
            pnlMainContent.Visible = false;
        }

        private void SetupFormLayout()
        {
            this.Size = new Size(1100, 700);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        // InitializeDefaultData was removed as initialization and seeding are done via DatabaseHelper.

        private void BuildLoginUI()
        {
            pnlGlassCard = new Panel();
            pnlGlassCard.Size = new Size(430, 500);
            pnlGlassCard.Location = new Point((this.Width - pnlGlassCard.Width) / 2, (this.Height - pnlGlassCard.Height) / 2);
            pnlGlassCard.BackColor = cardColor;
            pnlGlassCard.MouseDown += DragForm_MouseDown;
            this.Controls.Add(pnlGlassCard);

            // Subtle glassmorphism border drawing
            pnlGlassCard.Paint += (s, e) => {
                using (Pen borderPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1.5f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle rect = pnlGlassCard.ClientRectangle;
                    rect.Width -= 1;
                    rect.Height -= 1;
                    using (GraphicsPath path = GetRoundedRectPath(rect, 26))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
            };

            lblWelcome = new Label();
            lblWelcome.Text = "Welcome";
            lblWelcome.Font = new Font("Segoe UI", 28, FontStyle.Bold);
            lblWelcome.ForeColor = Color.White;
            lblWelcome.Size = new Size(350, 60);
            lblWelcome.Location = new Point(40, 45);
            lblWelcome.TextAlign = ContentAlignment.MiddleCenter;
            lblWelcome.MouseDown += DragForm_MouseDown;
            pnlGlassCard.Controls.Add(lblWelcome);

            pnlEmailContainer = CreateModernTextBox("Username", 40, 150, 350, 45, "👤", false, out txtEmail);
            pnlGlassCard.Controls.Add(pnlEmailContainer);

            pnlPasswordContainer = CreateModernTextBox("Password", 40, 220, 350, 45, "🔒", true, out txtPassword);
            pnlGlassCard.Controls.Add(pnlPasswordContainer);

            // password visibility toggle button (image-based)
            btnTogglePassword = new Button();
            btnTogglePassword.Text = "";
            btnTogglePassword.Size = new Size(30, 30);
            btnTogglePassword.FlatStyle = FlatStyle.Flat;
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.BackColor = Color.Transparent;
            btnTogglePassword.Cursor = Cursors.Hand;
            btnTogglePassword.Location = new Point(pnlPasswordContainer.Width - btnTogglePassword.Width - 8, (pnlPasswordContainer.Height - btnTogglePassword.Height) / 2);
            btnTogglePassword.ImageAlign = ContentAlignment.MiddleCenter;
            btnTogglePassword.Click += (s, e) => {
                isPasswordVisible = !isPasswordVisible;
                if (isPasswordVisible)
                {
                    txtPassword.PasswordChar = '\0';
                    btnTogglePassword.Image = CreateEyeBitmap(true);
                }
                else
                {
                    txtPassword.PasswordChar = '●';
                    btnTogglePassword.Image = CreateEyeBitmap(false);
                }
            };
            btnTogglePassword.Image = CreateEyeBitmap(false);
            pnlPasswordContainer.Controls.Add(btnTogglePassword);

            btnLogin = new Button();
            btnLogin.Text = "LOGIN";
            btnLogin.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btnLogin.ForeColor = Color.White;
            btnLogin.BackColor = buttonColor;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Size = new Size(350, 50);
            btnLogin.Location = new Point(40, 305);
            btnLogin.Cursor = Cursors.Hand;
            pnlGlassCard.Controls.Add(btnLogin);

            // Hover styling for Login button
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = Color.FromArgb(240, 65, 115);
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = buttonColor;

            // allow Enter key to trigger login
            this.AcceptButton = btnLogin;

            btnLogin.Click += async (s, e) => {
                // visual feedback
                btnLogin.Enabled = false;
                string originalText = btnLogin.Text;
                btnLogin.Text = "Logging in...";

                // simulate processing (replace with real auth call)
                await Task.Delay(700);

                if (DatabaseHelper.ValidateUser(txtEmail.Text, txtPassword.Text))
                {
                    pnlGlassCard.Visible = false;
                    pnlSidebar.Visible = true;
                    pnlHeader.Visible = true;
                    pnlMainContent.Visible = true;
                    pnlControlBox.BringToFront(); // Keep control box on top of the dashboard panels
                    ShowDashboardData();
                }
                else
                {
                    MessageBox.Show("خطأ في اسم المستخدم أو كلمة المرور!", "خطأ في الدخول", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnLogin.Enabled = true;
                    btnLogin.Text = originalText;
                }
            };

            lblForgotPass = new Label();
            lblForgotPass.Text = "Forgot Password?";
            lblForgotPass.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblForgotPass.ForeColor = Color.FromArgb(200, 190, 210);
            lblForgotPass.Size = new Size(160, 25);
            lblForgotPass.Location = new Point(40, 385);
            lblForgotPass.Cursor = Cursors.Hand;
            lblForgotPass.Click += (s, e) => {
                if (pnlForgotPassDialog != null)
                {
                    pnlForgotPassDialog.Visible = true;
                    pnlForgotPassDialog.BringToFront();
                    txtFPUsername.Text = "";
                    txtFPUsername.Focus();
                }
            };

            // Forgot Password hover animation
            lblForgotPass.MouseEnter += (s, e) => {
                lblForgotPass.ForeColor = Color.White;
                lblForgotPass.Font = new Font(lblForgotPass.Font, FontStyle.Bold | FontStyle.Underline);
            };
            lblForgotPass.MouseLeave += (s, e) => {
                lblForgotPass.ForeColor = Color.FromArgb(200, 190, 210);
                lblForgotPass.Font = new Font(lblForgotPass.Font, FontStyle.Bold);
            };

            pnlGlassCard.Controls.Add(lblForgotPass);

            ApplySafeRoundedCorners();
        }

        private void BuildDashboardUI()
        {
            pnlSidebar = new Panel();
            pnlSidebar.Size = new Size(240, this.Height);
            pnlSidebar.Location = new Point(0, 0);
            pnlSidebar.BackColor = sidebarBg;
            pnlSidebar.MouseDown += DragForm_MouseDown;
            this.Controls.Add(pnlSidebar);

            lblAppTitle = new Label();
            lblAppTitle.Text = "🏠 Housing App";
            lblAppTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblAppTitle.ForeColor = Color.White;
            lblAppTitle.Location = new Point(20, 30);
            lblAppTitle.Size = new Size(200, 40);
            lblAppTitle.MouseDown += DragForm_MouseDown;
            pnlSidebar.Controls.Add(lblAppTitle);

            btnDashboard = CreateSidebarButton("Dashboard", 120);
            btnHouses = CreateSidebarButton("Manage Houses", 180);
            pnlSidebar.Controls.Add(btnDashboard);
            pnlSidebar.Controls.Add(btnHouses);

            btnDashboard.Click += (s, e) => ShowDashboardData();
            btnHouses.Click += (s, e) => ShowHousesGrid();

            btnLogout = CreateSidebarButton("Logout", this.Height - 70);
            btnLogout.BackColor = Color.FromArgb(105, 30, 75);
            btnLogout.Click += (s, e) => {
                pnlSidebar.Visible = false;
                pnlHeader.Visible = false;
                pnlMainContent.Visible = false;
                pnlGlassCard.Visible = true;
                pnlControlBox.BringToFront(); // Ensure control box remains visible on the login page
                txtEmail.Text = "";
                txtPassword.Text = "";
                if (btnLogin != null) { btnLogin.Enabled = true; btnLogin.Text = "LOGIN"; }
            };
            pnlSidebar.Controls.Add(btnLogout);

            pnlHeader = new Panel();
            pnlHeader.Size = new Size(this.Width - 240, 80);
            pnlHeader.Location = new Point(240, 0);
            pnlHeader.BackColor = Color.FromArgb(26, 16, 36);
            pnlHeader.MouseDown += DragForm_MouseDown;
            this.Controls.Add(pnlHeader);

            lblWelcomeUser = new Label();
            lblWelcomeUser.Text = "Welcome Back, Admin";
            lblWelcomeUser.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblWelcomeUser.ForeColor = Color.White;
            lblWelcomeUser.Location = new Point(20, 25);
            lblWelcomeUser.Size = new Size(300, 30);
            lblWelcomeUser.MouseDown += DragForm_MouseDown;
            pnlHeader.Controls.Add(lblWelcomeUser);
        }

        private void BuildMainContentArea()
        {
            pnlMainContent = new Panel();
            pnlMainContent.Size = new Size(860, 620);
            pnlMainContent.Location = new Point(240, 80);
            pnlMainContent.BackColor = Color.Transparent;
            this.Controls.Add(pnlMainContent);

            lblSectionTitle = new Label();
            lblSectionTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblSectionTitle.ForeColor = Color.White;
            lblSectionTitle.Location = new Point(25, 25);
            lblSectionTitle.Size = new Size(190, 35);
            pnlMainContent.Controls.Add(lblSectionTitle);

            // مربع البحث الاحترافي - تم نقله وضبط أبعاده لمنع التداخل تماماً
            txtSearch = new TextBox();
            txtSearch.Multiline = true;
            txtSearch.Text = " 🔍 Search...";
            txtSearch.Font = new Font("Segoe UI", 11, FontStyle.Italic);
            txtSearch.BackColor = inputBgColor;
            txtSearch.ForeColor = Color.DarkGray;
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.Size = new Size(200, 40);
            txtSearch.Location = new Point(230, 20);
            txtSearch.Visible = false;

            txtSearch.TextChanged += (s, e) => {
                if (txtSearch.Text != " 🔍 Search...")
                {
                    FilterHouses(txtSearch.Text.Trim().Replace("🔍 Search...", ""));
                }
            };
            txtSearch.Enter += (s, e) => {
                if (txtSearch.Text.Contains("Search..."))
                {
                    txtSearch.Text = "";
                    txtSearch.ForeColor = Color.White;
                    txtSearch.Font = new Font("Segoe UI", 11, FontStyle.Regular);
                }
            };
            txtSearch.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    txtSearch.Text = " 🔍 Search...";
                    txtSearch.ForeColor = Color.DarkGray;
                    txtSearch.Font = new Font("Segoe UI", 11, FontStyle.Italic);
                    ShowHousesGrid();
                }
            };
            pnlMainContent.Controls.Add(txtSearch);

            // زر حذف منزل - تم تحديد موقعه الأفقي بدقة هندسية تمنع التداخل
            btnDeleteHouse = new Button();
            btnDeleteHouse.Text = "🗑 Delete Selected";
            btnDeleteHouse.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnDeleteHouse.ForeColor = Color.White;
            btnDeleteHouse.BackColor = deleteBtnColor;
            btnDeleteHouse.FlatStyle = FlatStyle.Flat;
            btnDeleteHouse.FlatAppearance.BorderSize = 0;
            btnDeleteHouse.Size = new Size(200, 40);
            btnDeleteHouse.Location = new Point(450, 20);
            btnDeleteHouse.Cursor = Cursors.Hand;
            btnDeleteHouse.Visible = false;

            btnDeleteHouse.MouseEnter += (s, e) => btnDeleteHouse.BackColor = deleteBtnHoverColor;
            btnDeleteHouse.MouseLeave += (s, e) => btnDeleteHouse.BackColor = deleteBtnColor;
            btnDeleteHouse.Click += (s, e) => HandleDeleteHouse();
            pnlMainContent.Controls.Add(btnDeleteHouse);

            // زر إضافة منزل جديد - مستقر وثابت في أقصى اليمين بالتوازي التام
            btnAddHouse = new Button();
            btnAddHouse.Text = "+ Add New House";
            btnAddHouse.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnAddHouse.ForeColor = Color.White;
            btnAddHouse.BackColor = buttonColor;
            btnAddHouse.FlatStyle = FlatStyle.Flat;
            btnAddHouse.FlatAppearance.BorderSize = 0;
            btnAddHouse.Size = new Size(160, 40);
            btnAddHouse.Location = new Point(670, 20);
            btnAddHouse.Cursor = Cursors.Hand;
            btnAddHouse.Click += (s, e) => { pnlAddHouseDialog.Visible = true; pnlAddHouseDialog.BringToFront(); };
            pnlMainContent.Controls.Add(btnAddHouse);

            // إعدادات الجدول
            dgvHouses = new DataGridView();
            dgvHouses.Size = new Size(800, 470);
            dgvHouses.Location = new Point(25, 90);
            dgvHouses.BorderStyle = BorderStyle.FixedSingle;
            dgvHouses.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgvHouses.GridColor = Color.FromArgb(90, 70, 110);
            dgvHouses.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHouses.MultiSelect = false;
            dgvHouses.AllowUserToAddRows = false;
            dgvHouses.RowHeadersVisible = false;
            dgvHouses.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvHouses.EnableHeadersVisualStyles = false;
            dgvHouses.BackgroundColor = Color.FromArgb(26, 16, 36);

            dgvHouses.SelectionChanged += (s, e) => {
                btnDeleteHouse.Visible = dgvHouses.SelectedRows.Count > 0;
            };

            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            headerStyle.BackColor = Color.FromArgb(45, 30, 60);
            headerStyle.ForeColor = Color.FromArgb(255, 60, 130);
            headerStyle.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHouses.ColumnHeadersDefaultCellStyle = headerStyle;
            dgvHouses.ColumnHeadersHeight = 50;

            DataGridViewCellStyle rowStyle = new DataGridViewCellStyle();
            rowStyle.BackColor = Color.FromArgb(32, 22, 44);
            rowStyle.ForeColor = Color.White;
            rowStyle.Font = new Font("Segoe UI Semibold", 12, FontStyle.Regular);
            rowStyle.SelectionBackColor = Color.FromArgb(215, 45, 95);
            rowStyle.SelectionForeColor = Color.White;
            rowStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHouses.RowsDefaultCellStyle = rowStyle;
            dgvHouses.RowTemplate.Height = 45;

            pnlMainContent.Controls.Add(dgvHouses);

            dgvHouses.Columns.Add("ID", "House ID");
            dgvHouses.Columns.Add("Name", "House Name");
            dgvHouses.Columns.Add("Address", "Address");
            dgvHouses.Columns.Add("Status", "Status");

            try { txtSearch.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, txtSearch.Width, txtSearch.Height, 10, 10)); } catch { }
            try { btnAddHouse.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnAddHouse.Width, btnAddHouse.Height, 10, 10)); } catch { }
            try { btnDeleteHouse.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnDeleteHouse.Width, btnDeleteHouse.Height, 10, 10)); } catch { }
        }

        private void FilterHouses(string keyword)
        {
            dgvHouses.Rows.Clear();
            var filtered = DatabaseHelper.GetHouses(keyword);

            foreach (var house in filtered)
            {
                dgvHouses.Rows.Add(house.ID, house.Name, house.Address, house.Status);
            }
        }

        private void HandleDeleteHouse()
        {
            if (dgvHouses.SelectedRows.Count > 0)
            {
                string houseId = dgvHouses.SelectedRows[0].Cells["ID"].Value.ToString();
                string houseName = dgvHouses.SelectedRows[0].Cells["Name"].Value.ToString();

                DialogResult result = MessageBox.Show($"Are you sure you want to delete ({houseName})?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    DatabaseHelper.DeleteHouse(houseId);

                    txtSearch.Text = " 🔍 Search...";
                    txtSearch.ForeColor = Color.DarkGray;
                    txtSearch.Font = new Font("Segoe UI", 11, FontStyle.Italic);

                    ShowHousesGrid();
                    btnDeleteHouse.Visible = false;
                }
            }
        }

        private void BuildDashboardCards()
        {
            int cardWidth = 240;
            int cardHeight = 130;
            int topPos = 110;

            cardTotalHouses = CreateStatCard("Total Houses", Color.FromArgb(45, 30, 60), 25, topPos, cardWidth, cardHeight, out lblTotalValue);
            cardAvailableHouses = CreateStatCard("Available Houses", Color.FromArgb(25, 50, 45), 295, topPos, cardWidth, cardHeight, out lblAvailableValue);
            cardRentedHouses = CreateStatCard("Rented Houses", Color.FromArgb(65, 25, 45), 565, topPos, cardWidth, cardHeight, out lblRentedValue);

            pnlMainContent.Controls.Add(cardTotalHouses);
            pnlMainContent.Controls.Add(cardAvailableHouses);
            pnlMainContent.Controls.Add(cardRentedHouses);
        }

        private Panel CreateStatCard(string title, Color bgColor, int x, int y, int w, int h, out Label valueLabel)
        {
            Panel card = new Panel();
            card.Size = new Size(w, h);
            card.Location = new Point(x, y);
            card.BackColor = bgColor;

            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(220, 220, 220);
            lblTitle.Location = new Point(15, 20);
            lblTitle.Size = new Size(200, 30);
            card.Controls.Add(lblTitle);

            valueLabel = new Label();
            valueLabel.Text = "0";
            valueLabel.Font = new Font("Segoe UI", 28, FontStyle.Bold);
            valueLabel.ForeColor = Color.White;
            valueLabel.Location = new Point(15, 55);
            valueLabel.Size = new Size(150, 55);
            card.Controls.Add(valueLabel);

            try { card.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, w, h, 18, 18)); } catch { }
            return card;
        }

        private void BuildAddHouseDialog()
        {
            pnlAddHouseDialog = new Panel();
            pnlAddHouseDialog.Size = new Size(400, 320);
            pnlAddHouseDialog.Location = new Point((860 - 400) / 2, (620 - 320) / 2);
            pnlAddHouseDialog.BackColor = Color.FromArgb(35, 22, 48);
            pnlAddHouseDialog.BorderStyle = BorderStyle.FixedSingle;
            pnlAddHouseDialog.Visible = false;
            pnlMainContent.Controls.Add(pnlAddHouseDialog);

            Label lblTitle = new Label
            {
                Text = "Add New House Details",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                Size = new Size(300, 30)
            };
            pnlAddHouseDialog.Controls.Add(lblTitle);

            txtNewName = new TextBox { Text = "House Name", BackColor = inputBgColor, ForeColor = Color.DarkGray, Font = new Font("Segoe UI", 11), Location = new Point(20, 70), Size = new Size(360, 35), BorderStyle = BorderStyle.FixedSingle };
            txtNewName.Enter += (s, e) => {
                if (txtNewName.Text == "House Name")
                {
                    txtNewName.Text = "";
                    txtNewName.ForeColor = Color.White;
                }
            };
            txtNewName.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtNewName.Text))
                {
                    txtNewName.Text = "House Name";
                    txtNewName.ForeColor = Color.DarkGray;
                }
            };

            txtNewAddress = new TextBox { Text = "Address", BackColor = inputBgColor, ForeColor = Color.DarkGray, Font = new Font("Segoe UI", 11), Location = new Point(20, 125), Size = new Size(360, 35), BorderStyle = BorderStyle.FixedSingle };
            txtNewAddress.Enter += (s, e) => {
                if (txtNewAddress.Text == "Address")
                {
                    txtNewAddress.Text = "";
                    txtNewAddress.ForeColor = Color.White;
                }
            };
            txtNewAddress.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtNewAddress.Text))
                {
                    txtNewAddress.Text = "Address";
                    txtNewAddress.ForeColor = Color.DarkGray;
                }
            };

            cmbNewStatus = new ComboBox { BackColor = inputBgColor, ForeColor = Color.White, Font = new Font("Segoe UI", 11), Location = new Point(20, 180), Size = new Size(360, 35), FlatStyle = FlatStyle.Flat };
            cmbNewStatus.Items.AddRange(new string[] { "Available", "Rented" });
            cmbNewStatus.SelectedIndex = 0;

            pnlAddHouseDialog.Controls.Add(txtNewName);
            pnlAddHouseDialog.Controls.Add(txtNewAddress);
            pnlAddHouseDialog.Controls.Add(cmbNewStatus);

            Button btnSave = new Button { Text = "SAVE", BackColor = buttonColor, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(20, 245), Size = new Size(170, 45), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            Button btnCancel = new Button { Text = "CANCEL", BackColor = Color.FromArgb(70, 60, 80), ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(210, 245), Size = new Size(170, 45), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

            btnSave.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderSize = 0;

            btnSave.Click += (s, e) => {
                string name = txtNewName.Text == "House Name" ? "" : txtNewName.Text;
                string address = txtNewAddress.Text == "Address" ? "" : txtNewAddress.Text;
                string status = cmbNewStatus.SelectedItem != null ? cmbNewStatus.SelectedItem.ToString() : "Available";

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
                {
                    MessageBox.Show("Please enter valid house name and address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DatabaseHelper.AddHouse(name, address, status);

                txtNewName.Text = "House Name";
                txtNewName.ForeColor = Color.DarkGray;
                txtNewAddress.Text = "Address";
                txtNewAddress.ForeColor = Color.DarkGray;
                cmbNewStatus.SelectedIndex = 0;

                pnlAddHouseDialog.Visible = false;
                ShowHousesGrid();
            };

            btnCancel.Click += (s, e) => {
                pnlAddHouseDialog.Visible = false;
            };

            pnlAddHouseDialog.Controls.Add(btnSave);
            pnlAddHouseDialog.Controls.Add(btnCancel);

            try { pnlAddHouseDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlAddHouseDialog.Width, pnlAddHouseDialog.Height, 15, 15)); } catch { }
        }

        private void BuildForgotPasswordDialog()
        {
            pnlForgotPassDialog = new Panel();
            pnlForgotPassDialog.Size = new Size(360, 200);
            pnlForgotPassDialog.Location = new Point((pnlGlassCard.Width - pnlForgotPassDialog.Width) / 2, (pnlGlassCard.Height - pnlForgotPassDialog.Height) / 2);
            pnlForgotPassDialog.BackColor = Color.FromArgb(35, 22, 48);
            pnlForgotPassDialog.BorderStyle = BorderStyle.FixedSingle;
            pnlForgotPassDialog.Visible = false;
            pnlGlassCard.Controls.Add(pnlForgotPassDialog);

            lblFPTitle = new Label { Text = "Password Recovery", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 15), Size = new Size(300, 28) };
            pnlForgotPassDialog.Controls.Add(lblFPTitle);

            txtFPUsername = new TextBox { Text = "Username or Email", BackColor = inputBgColor, ForeColor = Color.DarkGray, Font = new Font("Segoe UI", 11), Location = new Point(20, 60), Size = new Size(320, 34), BorderStyle = BorderStyle.FixedSingle };
            txtFPUsername.Enter += (s, e) => {
                if (txtFPUsername.Text == "Username or Email") { txtFPUsername.Text = ""; txtFPUsername.ForeColor = Color.White; }
            };
            txtFPUsername.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtFPUsername.Text)) { txtFPUsername.Text = "Username or Email"; txtFPUsername.ForeColor = Color.DarkGray; }
            };
            pnlForgotPassDialog.Controls.Add(txtFPUsername);

            btnSendReset = new Button { Text = "SEND RESET", BackColor = buttonColor, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(20, 120), Size = new Size(150, 40), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSendReset.FlatAppearance.BorderSize = 0;
            btnSendReset.Click += (s, e) => {
                string input = txtFPUsername.Text == "Username or Email" ? "" : txtFPUsername.Text.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("Please enter your username or email.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // For this demo app we do not have a user store. Simulate sending a reset link.
                MessageBox.Show("If an account matching the provided username/email exists, a password reset link has been sent to the registered email address.", "Password Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
                pnlForgotPassDialog.Visible = false;
            };
            pnlForgotPassDialog.Controls.Add(btnSendReset);

            btnCancelReset = new Button { Text = "CANCEL", BackColor = Color.FromArgb(70, 60, 80), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(190, 120), Size = new Size(150, 40), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancelReset.FlatAppearance.BorderSize = 0;
            btnCancelReset.Click += (s, e) => { pnlForgotPassDialog.Visible = false; };
            pnlForgotPassDialog.Controls.Add(btnCancelReset);

            try { pnlForgotPassDialog.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlForgotPassDialog.Width, pnlForgotPassDialog.Height, 12, 12)); } catch { }
        }

        private void SetActiveButton(Button activeBtn)
        {
            btnDashboard.BackColor = Color.Transparent;
            btnHouses.BackColor = Color.Transparent;

            activeBtn.BackColor = activeBtnColor;
            try { activeBtn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, activeBtn.Width, activeBtn.Height, 12, 12)); } catch { }
        }

        private void ShowHousesGrid()
        {
            SetActiveButton(btnHouses);

            lblSectionTitle.Text = "Manage Houses";
            dgvHouses.Visible = true;
            btnAddHouse.Visible = true;
            txtSearch.Visible = true;
            btnDeleteHouse.Visible = dgvHouses.SelectedRows.Count > 0;

            cardTotalHouses.Visible = false;
            cardAvailableHouses.Visible = false;
            cardRentedHouses.Visible = false;

            if (txtSearch.Text == " 🔍 Search...")
            {
                dgvHouses.Rows.Clear();
                var houses = DatabaseHelper.GetHouses();
                foreach (var house in houses)
                {
                    dgvHouses.Rows.Add(house.ID, house.Name, house.Address, house.Status);
                }
            }
        }

        private void ShowDashboardData()
        {
            SetActiveButton(btnDashboard);

            lblSectionTitle.Text = "Dashboard Overview";
            dgvHouses.Visible = false;
            btnAddHouse.Visible = false;
            btnDeleteHouse.Visible = false;
            pnlAddHouseDialog.Visible = false;
            txtSearch.Visible = false;

            cardTotalHouses.Visible = true;
            cardAvailableHouses.Visible = true;
            cardRentedHouses.Visible = true;

            var houses = DatabaseHelper.GetHouses();
            lblTotalValue.Text = houses.Count.ToString();
            lblAvailableValue.Text = houses.FindAll(h => h.Status == "Available").Count.ToString();
            lblRentedValue.Text = houses.FindAll(h => h.Status == "Rented").Count.ToString();
        }

        private Button CreateSidebarButton(string text, int topLocation)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btn.ForeColor = Color.White;
            btn.Size = new Size(200, 45);
            btn.Location = new Point(20, topLocation);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;

            btn.MouseEnter += (s, e) => {
                if (btn.BackColor != activeBtnColor)
                    btn.BackColor = Color.FromArgb(45, 30, 60);
            };
            btn.MouseLeave += (s, e) => {
                if (btn.BackColor != activeBtnColor)
                    btn.BackColor = Color.Transparent;
            };

            return btn;
        }

        private Image CreateEyeBitmap(bool open)
        {
            int w = 20, h = 14;
            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (Pen pen = new Pen(Color.FromArgb(200, 190, 210), 1.8f))
                {
                    // draw eye bounds (ellipse)
                    Rectangle rect = new Rectangle(1, 1, w - 3, h - 3);
                    g.DrawEllipse(pen, rect);
                }

                if (open)
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(200, 190, 210)))
                    {
                        int px = w / 2 - 3;
                        int py = h / 2 - 3;
                        g.FillEllipse(b, new Rectangle(px, py, 6, 6));
                    }
                }
                else
                {
                    using (Pen strike = new Pen(Color.FromArgb(200, 190, 210), 2f))
                    {
                        g.DrawLine(strike, 2, h / 2, w - 2, h / 2);
                    }
                }
            }
            return bmp;
        }

        private void ApplySafeRoundedCorners()
        {
            try
            {
                pnlGlassCard.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlGlassCard.Width, pnlGlassCard.Height, 26, 26));
                btnLogin.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnLogin.Width, btnLogin.Height, 16, 16));
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (LinearGradientBrush dualBrush = new LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(105, 30, 75),
                Color.FromArgb(14, 12, 22),
                65F))
            {
                e.Graphics.FillRectangle(dualBrush, this.ClientRectangle);
            }
        }

        // --- CUSTOM HELPERS ---

        private Panel CreateModernTextBox(string placeholder, int x, int y, int width, int height, string iconText, bool isPassword, out TextBox targetTextBox)
        {
            Panel container = new Panel();
            container.Size = new Size(width, height);
            container.Location = new Point(x, y);
            container.BackColor = inputBgColor;

            TextBox textBox = new TextBox();
            TextBox localTextBox = textBox;

            Label lblIcon = new Label();
            lblIcon.Text = iconText;
            lblIcon.ForeColor = Color.FromArgb(200, 190, 210);
            lblIcon.Font = new Font("Segoe UI", 12);
            lblIcon.AutoSize = true;
            lblIcon.Location = new Point(12, (height - lblIcon.PreferredHeight) / 2);
            lblIcon.MouseDown += (s, e) => { if (localTextBox != null) localTextBox.Focus(); };
            container.Controls.Add(lblIcon);

            textBox.BorderStyle = BorderStyle.None;
            textBox.BackColor = inputBgColor;
            textBox.ForeColor = Color.White;
            textBox.Font = new Font("Segoe UI", 12);

            if (isPassword)
            {
                textBox.PasswordChar = '●';
            }

            // Dynamic padding based on the icon size to prevent overlap/clipping
            int textLeft = lblIcon.Right + 8;
            if (textLeft < 40) textLeft = 40;

            int textWidth = width - textLeft - 12;
            if (isPassword)
            {
                textWidth -= 32; // Leave space for eye toggle button
            }
            textBox.Location = new Point(textLeft, (height - textBox.PreferredHeight) / 2);
            textBox.Width = textWidth;

            container.Controls.Add(textBox);
            targetTextBox = textBox;

            // Set native placeholder cue banner
            textBox.HandleCreated += (s, e) => {
                SendMessage(textBox.Handle, EM_SETCUEBANNER, 1, placeholder);
            };

            Color currentBorderColor = Color.FromArgb(60, 90, 70, 110);

            container.Paint += (s, e) => {
                using (Pen pen = new Pen(currentBorderColor, 1.5f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle rect = container.ClientRectangle;
                    rect.Width -= 1;
                    rect.Height -= 1;
                    using (GraphicsPath path = GetRoundedRectPath(rect, 8))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            textBox.Enter += (s, e) => {
                currentBorderColor = buttonColor;
                container.Invalidate();
            };

            textBox.Leave += (s, e) => {
                currentBorderColor = Color.FromArgb(60, 90, 70, 110);
                container.Invalidate();
            };

            try { container.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, container.Width, container.Height, 8, 8)); } catch { }
            return container;
        }

        private void BuildControlBox()
        {
            pnlControlBox = new Panel();
            pnlControlBox.Size = new Size(90, 40);
            pnlControlBox.Location = new Point(this.Width - pnlControlBox.Width, 0);
            pnlControlBox.BackColor = Color.Transparent;

            btnHeaderMinimize = new Button();
            btnHeaderMinimize.Text = "─";
            btnHeaderMinimize.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnHeaderMinimize.ForeColor = Color.FromArgb(200, 190, 210);
            btnHeaderMinimize.BackColor = Color.Transparent;
            btnHeaderMinimize.FlatStyle = FlatStyle.Flat;
            btnHeaderMinimize.FlatAppearance.BorderSize = 0;
            btnHeaderMinimize.Size = new Size(45, 40);
            btnHeaderMinimize.Location = new Point(0, 0);
            btnHeaderMinimize.Cursor = Cursors.Hand;
            btnHeaderMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            btnHeaderMinimize.MouseEnter += (s, e) => {
                btnHeaderMinimize.BackColor = Color.FromArgb(40, 255, 255, 255);
                btnHeaderMinimize.ForeColor = Color.White;
            };
            btnHeaderMinimize.MouseLeave += (s, e) => {
                btnHeaderMinimize.BackColor = Color.Transparent;
                btnHeaderMinimize.ForeColor = Color.FromArgb(200, 190, 210);
            };

            btnHeaderExit = new Button();
            btnHeaderExit.Text = "✕";
            btnHeaderExit.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnHeaderExit.ForeColor = Color.FromArgb(200, 190, 210);
            btnHeaderExit.BackColor = Color.Transparent;
            btnHeaderExit.FlatStyle = FlatStyle.Flat;
            btnHeaderExit.FlatAppearance.BorderSize = 0;
            btnHeaderExit.Size = new Size(45, 40);
            btnHeaderExit.Location = new Point(45, 0);
            btnHeaderExit.Cursor = Cursors.Hand;
            btnHeaderExit.Click += (s, e) => Application.Exit();
            btnHeaderExit.MouseEnter += (s, e) => {
                btnHeaderExit.BackColor = Color.FromArgb(230, 45, 80);
                btnHeaderExit.ForeColor = Color.White;
            };
            btnHeaderExit.MouseLeave += (s, e) => {
                btnHeaderExit.BackColor = Color.Transparent;
                btnHeaderExit.ForeColor = Color.FromArgb(200, 190, 210);
            };

            pnlControlBox.Controls.Add(btnHeaderMinimize);
            pnlControlBox.Controls.Add(btnHeaderExit);
            this.Controls.Add(pnlControlBox);
            pnlControlBox.BringToFront();
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

}