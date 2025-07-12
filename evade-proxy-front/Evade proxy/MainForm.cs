using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Evade_proxy.src.proxy;
using LoaderV2;
using System.Text;
using System.Text.Json;

namespace Evade_proxy
{
    public partial class MainForm : Form
    {
        private AuthClient AuthClient;
        private ProxyManager ProxyManager;
        private ProxySetup ProxySetup;
        private System.Windows.Forms.Timer infoCheckTimer;
        private bool evadeRunning = false;
        private static bool lastEvadeState = false;

        private RoundedButton btnStartStop;
        private Label lblNickV;
        private Label lblSubV;
        private Label lblEvadeV;
        private Label lblPlayerListV;
        private Label lblServerIP;
        private Label lblStampID;
        private Label lblXUIDCount;

        // Rendre la form arrondie
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        // Pour déplacer la fenêtre via la barre titre custom
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public MainForm()
        {
            ProxyManager = new ProxyManager();
            ProxySetup = new ProxySetup();
            AuthClient = new AuthClient();

            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(30, 35, 45);
            this.ClientSize = new Size(650, 375);
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 25, 25));

            // ----- BARRE TITRE CUSTOM -----
            Panel titleBar = new Panel()
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(35, 40, 55)
            };
            titleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); };
            this.Controls.Add(titleBar);

            Label lblTitle = new Label()
            {
                Text = "SOT Solution - Evade",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(20, 10)
            };
            titleBar.Controls.Add(lblTitle);

            // Minimize btn
            Button btnMin = new Button()
            {
                Text = "–",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Size = new Size(36, 32),
                Location = new Point(this.Width - 90, 4),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                TabStop = false
            };
            btnMin.FlatAppearance.BorderSize = 0;
            btnMin.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnMin.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            titleBar.Controls.Add(btnMin);

            // Close btn
            Button btnClose = new Button()
            {
                Text = "✕",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Size = new Size(36, 32),
                Location = new Point(this.Width - 50, 4),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                TabStop = false
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnClose.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnClose.Click += (s, e) => this.Close();
            titleBar.Controls.Add(btnClose);

            // Ajuste position boutons si redim...
            this.Resize += (s, e) =>
            {
                btnMin.Left = this.Width - 90;
                btnClose.Left = this.Width - 50;
                this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 25, 25));
            };

            // ----------------------------------------
            // COLONNE GAUCHE (logo + bouton)
            int blocGaucheX = 30;
            int blocCentre = this.ClientSize.Width / 2; // 325

            PictureBox logoBox = new PictureBox()
            {
                Image = Properties.Resources.sotsolution_logo,
                Location = new Point(blocGaucheX + 55, 50),
                Size = new Size(120, 100),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            this.Controls.Add(logoBox);

            btnStartStop = new RoundedButton()
            {
                Text = "START EVADE",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Size = new Size(220, 40),
                Location = new Point(blocGaucheX, 160),
                BackColor = Color.FromArgb(54, 119, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnStartStop.FlatAppearance.BorderSize = 0;
            btnStartStop.Click += Start_Click;
            this.Controls.Add(btnStartStop);

            // ----------------------------------------
            // COLONNE DROITE (infos, commence à 325)
            int infosX = 325;

            Label lblClientInfo = new Label()
            {
                Text = "Client Information",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(infosX, 60),
                AutoSize = true
            };
            this.Controls.Add(lblClientInfo);

            Label lblNick = new Label()
            {
                Text = "Nickname:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(infosX + 10, 80),
                AutoSize = true
            };
            this.Controls.Add(lblNick);

            lblNickV = new Label()
            {
                Text = "...",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.Gainsboro,
                Location = new Point(infosX + 200, 80),
                AutoSize = true
            };
            this.Controls.Add(lblNickV);

            Label lblSub = new Label()
            {
                Text = "Subscription End Date:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(infosX + 10, 100),
                AutoSize = true
            };
            this.Controls.Add(lblSub);

            lblSubV = new Label()
            {
                Text = "...",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.Gainsboro,
                Location = new Point(infosX + 200, 100),
                AutoSize = true
            };
            this.Controls.Add(lblSubV);

            // ---- Séparateur horizontal client/evade ----
            Panel sep1 = new Panel()
            {
                BackColor = Color.FromArgb(60, 60, 70),
                Size = new Size(290, 2),
                Location = new Point(infosX, 130)
            };
            this.Controls.Add(sep1);

            // ----- INFOS EVADE -----
            Label lblEvadeInfo = new Label()
            {
                Text = "Evade Information",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(infosX, 140),
                AutoSize = true
            };
            this.Controls.Add(lblEvadeInfo);

            Label lblEvade = new Label()
            {
                Text = "Evade State:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(infosX + 10, 160),
                AutoSize = true
            };
            this.Controls.Add(lblEvade);

            lblEvadeV = new Label()
            {
                Text = "...",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.Gainsboro,
                Location = new Point(infosX + 200, 160),
                AutoSize = true
            };
            this.Controls.Add(lblEvadeV);

            Label lblPlayerList = new Label()
            {
                Text = "Player List State:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(infosX + 10, 180),
                AutoSize = true
            };
            this.Controls.Add(lblPlayerList);

            lblPlayerListV = new Label()
            {
                Text = "Coming Soon",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.Gainsboro,
                Location = new Point(infosX + 200, 180),
                AutoSize = true
            };
            this.Controls.Add(lblPlayerListV);

            // ---- Séparateur horizontal bas (10px plus bas que le bouton)
            Panel sep2 = new Panel()
            {
                BackColor = Color.FromArgb(60, 60, 70),
                Size = new Size(580, 2),
                Location = new Point(30, 215)
            };
            this.Controls.Add(sep2);

            // ----- INFOS SERVER MONITOR -----
            Label lblServer = new Label()
            {
                Text = "Server Monitor",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 235),
                AutoSize = true
            };
            this.Controls.Add(lblServer);

            AddServerRow("Server IP:", "unknown", 40, 265, "ip");
            AddServerRow("Stamp ID:", "unknown", 40, 290, "stamp");
            AddServerRow("XUID Count:", "0", 40, 315, "xuid");

            // ---- Mentions légales et version ----
            Label lblLegal = new Label()
            {
                Text = "Evade 2.0  |  SOT Solution 2025  |  All rights reserved",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true
            };
            lblLegal.Location = new Point(this.ClientSize.Width - lblLegal.PreferredWidth - 100, this.ClientSize.Height - 30);
            this.Controls.Add(lblLegal);

            // ---- Join Discord (cliquable) ----
            Label lblDiscord = new Label()
            {
                Text = "Join Discord",
                Font = new Font("Segoe UI", 8, FontStyle.Underline),
                ForeColor = Color.DeepSkyBlue,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lblDiscord.Location = new Point(this.ClientSize.Width - lblDiscord.PreferredWidth - 25, this.ClientSize.Height - 30);
            lblDiscord.Click += (s, e) =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://discord.gg/7pcX9SmBks",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            };
            this.Controls.Add(lblDiscord);
        }

        private void AddServerRow(string left, string right, int x, int y, string key)
        {
            var l1 = new Label()
            {
                Text = left,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.White,
                Location = new Point(x, y),
                AutoSize = true
            };

            var l2 = new Label()
            {
                Text = right,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.Gainsboro,
                Location = new Point(x + 130, y),
                AutoSize = true
            };

            this.Controls.Add(l1);
            this.Controls.Add(l2);

            // Store references
            switch (key)
            {
                case "ip":
                    lblServerIP = l2;
                    break;
                case "stamp":
                    lblStampID = l2;
                    break;
                case "xuid":
                    lblXUIDCount = l2;
                    break;
            }
        }

        // --- BOUTON ARRONDI ---
        public class RoundedButton : Button
        {
            protected override void OnPaint(PaintEventArgs pevent)
            {
                Graphics g = pevent.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = this.ClientRectangle;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, 32, 32, 180, 90);
                    path.AddArc(rect.Right - 32, rect.Y, 32, 32, 270, 90);
                    path.AddArc(rect.Right - 32, rect.Bottom - 32, 32, 32, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - 32, 32, 32, 90, 90);
                    path.CloseAllFigures();
                    this.Region = new Region(path);
                    using (SolidBrush brush = new SolidBrush(this.BackColor))
                        g.FillPath(brush, path);
                    TextRenderer.DrawText(g, this.Text, this.Font, rect, this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        private async void Start_Click(object sender, EventArgs e)
        {
            if (!evadeRunning)
            {
                // Apply changes on ui
                btnStartStop.Text = "STOP EVADE";
                evadeRunning = true;

                ProxyManager.evadeEnabled = true;
            }
            else
            {
                //Apply ui change
                btnStartStop.Text = "START EVADE";
                evadeRunning = false;

                ProxyManager.evadeEnabled = false;
            }
        }
        private bool ft_has_evade_state_changed()
        {
            if (evadeRunning != lastEvadeState)
            {
                lastEvadeState = evadeRunning;
                return true;
            }
            return false;
        }
        private async void MainForm_Load(object sender, EventArgs e)
        {
            var (response, username) = await AuthClient.ft_auth_login();
            string json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            string rawDate = doc.RootElement.GetProperty("expiration_date").GetString();

            DateTime expirationDate = DateTime.Parse(rawDate);
            string formattedDate = expirationDate.ToString("dd/MM/yyyy");

            ProxySetup.ft_setup_all();

            lblNickV.Text = username;
            lblSubV.Text = formattedDate;

            infoCheckTimer = new System.Windows.Forms.Timer();
            infoCheckTimer.Interval = 100;
            infoCheckTimer.Tick += (s, e) =>
            {
                if (ProxySetup.ft_has_data_changed() || ft_has_evade_state_changed())
                {
                    lblEvadeV.Text = evadeRunning ? "ON" : "OFF";
                    lblServerIP.Text = Utils.ft_get_server_ip();
                    lblStampID.Text = Utils.ft_get_stamp_id();
                    lblXUIDCount.Text = Utils.ft_get_xuid_count().ToString();
                }
            };
            infoCheckTimer.Start();
        }
    }
}