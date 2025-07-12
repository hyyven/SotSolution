using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;
using System.IO;
using System.Windows.Forms.VisualStyles;
using Timer = System.Windows.Forms.Timer;
using System.Collections.Generic;
using System.Text.Json;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ProxyLoader
{
    public partial class LoginForm : Form
    {
        private readonly AuthClient _authClient;
        private bool _loginSuccessful;
        private bool isDragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;
        private WebClient webClient;
        private bool isDownloading = false;
        private float glowOpacity = 0;
        private bool increasing = true;
        private PictureBox logoPictureBox;
        private Icon appIcon;
        private CustomComboBox customProductComboBox;

        // Constantes Win32 API
        private const int WM_SETICON = 0x80;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        public LoginForm(AuthClient authClient)
        {
            InitializeComponent();
            _authClient = authClient;
            _loginSuccessful = false;
            this.Opacity = 0;
            this.ShowInTaskbar = true;

            // Configurer correctement le bouton de fermeture
            SetupCloseButton();

            // Essayer de charger l'icône depuis le cache
            string iconPath = Path.Combine(Path.GetTempPath(), "sotsolution_app.ico");
            if (File.Exists(iconPath))
            {
                try
                {
                    appIcon = new Icon(iconPath);
                    this.Icon = appIcon;
                }
                catch { }
            }

            // Créer un label de chargement en premier
            var loadingLabel = new Label
            {
                Text = "Loading...",
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                Location = new Point((mainPanel.Width - 60) / 2, 80),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            mainPanel.Controls.Add(loadingLabel);
            loadingLabel.BringToFront();

            // Créer le PictureBox pour le logo
            logoPictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(120, 120),
                Location = new Point((mainPanel.Width - 120) / 2, 20),
                BackColor = Color.Transparent
            };
            mainPanel.Controls.Add(logoPictureBox);

            // Create and replace the default combobox with custom one
            customProductComboBox = new CustomComboBox
            {
                Location = productComboBox.Location,
                Size = productComboBox.Size,
                Name = "customProductComboBox",
                TabIndex = productComboBox.TabIndex
            };

            // Add the same items
            customProductComboBox.Items.AddRange(new object[] { "Evade", "PlayerList" });

            // Remove old combobox and add new one
            mainPanel.Controls.Remove(productComboBox);
            mainPanel.Controls.Add(customProductComboBox);

            SetupEventHandlers();
            LoadSavedCredentials();

            this.Load += async (s, e) =>
            {
                try
                {
                    await LoadLogoImageAsync();
                    loadingLabel.Visible = false;
                    mainPanel.Refresh();
                }
                catch (Exception ex)
                {
                    loadingLabel.Text = "Failed to load images";
                    loadingLabel.ForeColor = Color.Red;
                    File.WriteAllText(
                        Path.Combine(Path.GetTempPath(), "logo_error.log"),
                        $"Error loading images: {ex.Message}\n{ex.StackTrace}"
                    );
                }
                await FadeInAsync();
                StartGlowAnimation();
            };

            welcomeLabel.Text = "SOT SOLUTION";
            welcomeLabel.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            welcomeLabel.ForeColor = Color.FromArgb(0, 120, 215);
            welcomeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            welcomeLabel.AutoSize = false;
            welcomeLabel.Size = new Size(mainPanel.Width, 45);
            welcomeLabel.Location = new Point(0, 150);
        }

        private async Task LoadLogoImageAsync()
        {
            string tempPath = Path.GetTempPath();
            string iconPath = Path.Combine(tempPath, "sotsolution_app.ico");
            string logoPath = Path.Combine(tempPath, "sotsolution_logo.png");

            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                    // Télécharger et sauvegarder l'icône en premier
                    try
                    {
                        byte[] iconData = await webClient.DownloadDataTaskAsync("https://XXXXXXXXXXXX/XXXXXXXXXXXXXXXXX/eac.ico");
                        File.WriteAllBytes(iconPath, iconData);

                        this.Invoke((MethodInvoker)delegate
                        {
                            try
                            {
                                if (appIcon != null)
                                {
                                    appIcon.Dispose();
                                }

                                appIcon = new Icon(iconPath);
                                this.Icon = appIcon;

                                // Force l'icône dans la barre des tâches
                                var handle = this.Handle;
                                SendMessage(handle, WM_SETICON, ICON_BIG, appIcon.Handle);
                                SendMessage(handle, WM_SETICON, ICON_SMALL, appIcon.Handle);

                                // Rafraîchir la barre des tâches
                                RefreshTrayIcon();
                            }
                            catch (Exception ex)
                            {
                                File.WriteAllText(
                                    Path.Combine(tempPath, "icon_load_error.log"),
                                    $"Error loading icon: {ex.Message}\n{ex.StackTrace}"
                                );
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText(
                            Path.Combine(tempPath, "icon_download_error.log"),
                            $"Error downloading icon: {ex.Message}\n{ex.StackTrace}"
                        );
                    }

                    // Télécharger et sauvegarder le logo
                    try
                    {
                        byte[] logoData = await webClient.DownloadDataTaskAsync("https://XXXXXXXXXXXXXXXXXX/XXXXXXXXXXXXXXXXXX/logo.png");
                        File.WriteAllBytes(logoPath, logoData);

                        using (var logoStream = new MemoryStream(logoData))
                        {
                            var image = Image.FromStream(logoStream);
                            this.Invoke((MethodInvoker)delegate
                            {
                                if (logoPictureBox != null && !logoPictureBox.IsDisposed)
                                {
                                    logoPictureBox.Image = image;
                                    logoPictureBox.Refresh();
                                    mainPanel.Refresh();
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText(
                            Path.Combine(tempPath, "logo_download_error.log"),
                            $"Error downloading logo: {ex.Message}\n{ex.StackTrace}"
                        );

                        // Essayer de charger le logo depuis le fichier local si disponible
                        if (File.Exists(logoPath))
                        {
                            using (var fileStream = File.OpenRead(logoPath))
                            {
                                var image = Image.FromStream(fileStream);
                                this.Invoke((MethodInvoker)delegate
                                {
                                    if (logoPictureBox != null && !logoPictureBox.IsDisposed)
                                    {
                                        logoPictureBox.Image = image;
                                        logoPictureBox.Refresh();
                                        mainPanel.Refresh();
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(
                    Path.Combine(tempPath, "general_error.log"),
                    $"General error: {ex.Message}\n{ex.StackTrace}"
                );
                throw;
            }
        }

        private void LoadLogoImage()
        {
            // Cette méthode n'est plus utilisée, tout est géré dans LoadLogoImageAsync
        }

        private void SetupEventHandlers()
        {
            this.titleBar.MouseDown += TitleBar_MouseDown;
            this.titleBar.MouseMove += TitleBar_MouseMove;
            this.titleBar.MouseUp += TitleBar_MouseUp;

            this.closeButton.Click += (s, e) => this.Close();

            this.txtUsername.TextChanged += TextBox_TextChanged;
            this.txtPassword.TextChanged += TextBox_TextChanged;

            this.btnLogin.Click += btnLogin_Click;

            this.animationTimer.Tick += (s, e) =>
            {
                if (increasing)
                {
                    glowOpacity = Math.Min(1.0f, glowOpacity + 0.1f);
                    if (glowOpacity >= 1.0f) increasing = false;
                }
                else
                {
                    glowOpacity = Math.Max(0.0f, glowOpacity - 0.1f);
                    if (glowOpacity <= 0.0f) increasing = true;
                }
                this.Invalidate();
            };

            customProductComboBox.SelectedIndexChanged += customProductComboBox_SelectedIndexChanged;
        }

        private void StartGlowAnimation()
        {
            animationTimer.Start();
        }

        private async Task FadeInAsync()
        {
            for (double i = 0; i <= 1; i += 0.1)
            {
                if (this.IsDisposed) return;
                this.Opacity = i;
                await Task.Delay(20);
            }
        }

        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {
        }

        private void titleBar_Paint(object sender, PaintEventArgs e)
        {
        }

        public bool LoginSuccessful => _loginSuccessful;

        private void LoadSavedCredentials()
        {
            try
            {
                string configPath = @"C:\SotSolution";
                string selectedProduct = customProductComboBox.SelectedItem?.ToString() ?? "Evade";
                string loginFile = Path.Combine(configPath, selectedProduct == "Evade" ? "authBanEvade.json" : "authPlayerlist.json");

                if (File.Exists(loginFile))
                {
                    string jsonContent = File.ReadAllText(loginFile);
                    var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);

                    if (credentials != null && credentials.ContainsKey("username") && credentials.ContainsKey("password"))
                    {
                        txtUsername.Text = credentials["username"];
                        txtPassword.Text = credentials["password"];
                    }
                }
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw form border with rounded corners
            using (GraphicsPath formPath = new GraphicsPath())
            {
                int formRadius = 15;
                Rectangle topLeft = new Rectangle(0, 0, formRadius * 2, formRadius * 2);
                Rectangle topRight = new Rectangle(this.Width - formRadius * 2, 0, formRadius * 2, formRadius * 2);
                Rectangle bottomRight = new Rectangle(this.Width - formRadius * 2, this.Height - formRadius * 2, formRadius * 2, formRadius * 2);
                Rectangle bottomLeft = new Rectangle(0, this.Height - formRadius * 2, formRadius * 2, formRadius * 2);

                formPath.AddArc(topLeft, 180, 90);
                formPath.AddArc(topRight, 270, 90);
                formPath.AddArc(bottomRight, 0, 90);
                formPath.AddArc(bottomLeft, 90, 90);
                formPath.CloseFigure();

                this.Region = new Region(formPath);
            }

            // Draw main panel with glass effect
            if (this.mainPanel != null)
            {
                using (GraphicsPath mainPanelPath = new GraphicsPath())
                {
                    int radius = 15;
                    Rectangle rect = new Rectangle(
                        this.mainPanel.Left,
                        0,
                        this.Width - this.mainPanel.Left,
                        this.Height
                    );

                    Rectangle topRight = new Rectangle(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2);
                    Rectangle bottomRight = new Rectangle(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2);
                    Rectangle bottomLeft = new Rectangle(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2);

                    mainPanelPath.AddArc(topRight, 270, 90);
                    mainPanelPath.AddArc(bottomRight, 0, 90);
                    mainPanelPath.AddArc(bottomLeft, 90, 90);
                    mainPanelPath.AddLine(new Point(rect.X, rect.Y + radius), new Point(rect.X, rect.Y + radius));
                    mainPanelPath.CloseFigure();

                    // Background gradient
                    using (LinearGradientBrush backgroundBrush = new LinearGradientBrush(
                        new Point(rect.Left, rect.Top),
                        new Point(rect.Right, rect.Bottom),
                        Color.FromArgb(25, 25, 25),
                        Color.FromArgb(15, 15, 15)))
                    {
                        e.Graphics.FillPath(backgroundBrush, mainPanelPath);
                    }

                    // Glass effect
                    using (PathGradientBrush glassEffect = new PathGradientBrush(mainPanelPath))
                    {
                        glassEffect.CenterColor = Color.FromArgb(30, 255, 255, 255);
                        glassEffect.SurroundColors = new Color[] { Color.FromArgb(5, 255, 255, 255) };
                        e.Graphics.FillPath(glassEffect, mainPanelPath);
                    }

                    // Subtle border
                    using (Pen borderPen = new Pen(Color.FromArgb(35, 35, 35), 1))
                    {
                        e.Graphics.DrawPath(borderPen, mainPanelPath);
                    }
                }
            }
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(dif));
            }
        }

        private void TitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            // Ne pas sauvegarder automatiquement pour permettre de choisir le bon fichier selon le produit
            btnLogin.Enabled = !string.IsNullOrWhiteSpace(txtUsername.Text) && !string.IsNullOrWhiteSpace(txtPassword.Text);
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            if (isDownloading) return;

            lblError.Text = "";
            if (customProductComboBox.SelectedIndex == -1)
            {
                lblError.Text = "Please select a product";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblError.Text = "Please enter both username and password";
                return;
            }

            // Disable controls during login
            btnLogin.Enabled = false;
            btnLogin.Text = "Logging in...";
            btnLogin.BackColor = Color.FromArgb(70, 70, 70);

            try
            {
                var selectedProduct = customProductComboBox.SelectedItem.ToString();
                var authClient = new AuthClient(selectedProduct);
                var loginResult = await authClient.Login(txtUsername.Text, txtPassword.Text);

                if (loginResult.success)
                {
                    // Login successful
                    _loginSuccessful = true;
                    btnLogin.Text = "SUCCESS";
                    btnLogin.BackColor = Color.FromArgb(0, 130, 0);
                    
                    authClient.SaveCredentials(txtUsername.Text, txtPassword.Text);

                    // Continuer avec la logique de téléchargement
                    StartDownloadProcess(selectedProduct);
                }
                else if (loginResult.requires2FA)
                {
                    // Afficher l'interface de vérification 2FA
                    Show2FAInterface();
                }
                else
                {
                    // Login failed
                    lblError.Text = "Invalid username or password";
                    btnLogin.Text = "Login";
                    btnLogin.BackColor = Color.FromArgb(0, 120, 215);
                    btnLogin.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "evade_error.log"),
                    $"Error: {ex.Message}\n{ex.StackTrace}");
                lblError.Text = "Connection error - Please check your internet connection";
                btnLogin.Text = "Login";
                btnLogin.BackColor = Color.FromArgb(0, 120, 215);
                btnLogin.Enabled = true;
            }
        }

        // Nouvelle méthode pour afficher l'interface de vérification 2FA
        private void Show2FAInterface()
        {
            // Conserver le titre principal mais changer sa couleur pour indiquer le mode 2FA
            welcomeLabel.Text = "SOT SOLUTION";
            welcomeLabel.ForeColor = Color.FromArgb(255, 130, 0); // Orange pour indiquer le mode de vérification
            lblError.Text = "";

            // Stocker les contrôles à cacher pour pouvoir les réafficher si besoin
            var hiddenControls = new Control[] { txtUsername, txtPassword, customProductComboBox };
            foreach (var control in hiddenControls)
            {
                control.Visible = false;
            }

            // Créer un panel pour contenir les contrôles 2FA
            Panel verificationPanel = new Panel
            {
                Size = new Size(txtUsername.Width, 100),
                Location = new Point(txtUsername.Location.X, txtUsername.Location.Y - 10),
                BackColor = Color.Transparent
            };
            mainPanel.Controls.Add(verificationPanel);

            // Label principal pour la vérification avec icône
            Label verificationLabel = new Label
            {
                Text = "Verification Required",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 220),
                AutoSize = true,
                Location = new Point((verificationPanel.Width - 170) / 2, 0),
                BackColor = Color.Transparent
            };
            verificationPanel.Controls.Add(verificationLabel);

            // Ajouter une icône séparée (emoji verrou)
            Label iconLabel = new Label
            {
                Text = "🔒",
                Font = new Font("Segoe UI", 15F, FontStyle.Regular),
                ForeColor = Color.FromArgb(255, 130, 0),
                AutoSize = true,
                Location = new Point(verificationLabel.Location.X - 30, 0),
                BackColor = Color.Transparent
            };
            verificationPanel.Controls.Add(iconLabel);
            
            // Label d'instructions plus épuré
            Label instructionsLabel = new Label
            {
                Text = "A verification code has been sent to your Discord.\nEnter the code below to continue:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 180, 180),
                TextAlign = System.Drawing.ContentAlignment.TopCenter,
                AutoSize = false,
                Size = new Size(verificationPanel.Width, 40),
                Location = new Point(0, 30),
                BackColor = Color.Transparent
            };
            verificationPanel.Controls.Add(instructionsLabel);
            
            // TextBox pour le code de vérification (avec style amélioré)
            TextBox tbVerificationCode = new TextBox
            {
                Name = "tbVerificationCode",
                Font = new Font("Consolas", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 255, 255),
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center,
                MaxLength = 6,
                Size = new Size(120, 30),
                Location = new Point((verificationPanel.Width - 120) / 2, 70)
            };
            
            // Ajouter un bord discret au TextBox
            Label textBoxBorder = new Label
            {
                AutoSize = false,
                Size = new Size(tbVerificationCode.Width + 10, tbVerificationCode.Height + 10),
                Location = new Point(tbVerificationCode.Location.X - 5, tbVerificationCode.Location.Y - 5),
                BackColor = Color.FromArgb(50, 50, 50)
            };
            verificationPanel.Controls.Add(textBoxBorder);
            verificationPanel.Controls.Add(tbVerificationCode);
            
            // Amener tous les contrôles au premier plan
            textBoxBorder.BringToFront();
            tbVerificationCode.BringToFront();
            
            // Modifier le bouton de login pour la vérification
            btnLogin.Text = "Verify";
            btnLogin.BackColor = Color.FromArgb(255, 130, 0); // Orange pour indiquer le mode de vérification
            btnLogin.ForeColor = Color.White;
            btnLogin.Enabled = true;
            
            // Remplacer l'événement du bouton de login avec une nouvelle logique
            btnLogin.Click -= btnLogin_Click;
            btnLogin.Click += async (s, e) => {
                if (string.IsNullOrWhiteSpace(tbVerificationCode.Text))
                {
                    lblError.Text = "Please enter the verification code";
                    return;
                }
                
                // Animation du bouton pendant la vérification
                btnLogin.Enabled = false;
                btnLogin.Text = "Verifying...";
                btnLogin.BackColor = Color.FromArgb(70, 70, 70);
                
                // Exécuter la vérification de manière asynchrone
                try
                {
                    // Log pour débogage
                    File.WriteAllText(
                        Path.Combine(Path.GetTempPath(), "verification_start.log"),
                        $"Attempting to verify code: {tbVerificationCode.Text}"
                    );
                    
                    bool verified = await _authClient.Verify2FA(tbVerificationCode.Text);
                    
                    // Log le résultat
                    File.WriteAllText(
                        Path.Combine(Path.GetTempPath(), "verification_result.log"),
                        $"Verification result: {verified}"
                    );
                    
                    if (verified)
                    {
                        // Animation de succès
                        btnLogin.Text = "SUCCESS";
                        btnLogin.BackColor = Color.FromArgb(46, 204, 113); // Vert pour le succès
                        lblError.Text = "";
                        _loginSuccessful = true;
                        
                        // Attendre un peu pour l'effet visuel
                        await Task.Delay(500);
                        
                        // Vérifier si un produit a été sélectionné
                        if (customProductComboBox.SelectedIndex == -1)
                        {
                            // Sélectionner le premier produit par défaut si aucun n'est sélectionné
                            customProductComboBox.SelectedIndex = 0;
                        }
                        
                        // Continuer avec le téléchargement
                        StartDownloadProcess(customProductComboBox.SelectedItem.ToString());
                    }
                    else
                    {
                        // Animation d'échec
                        lblError.Text = "Invalid verification code";
                        btnLogin.Text = "Try Again";
                        btnLogin.BackColor = Color.FromArgb(255, 130, 0);
                        btnLogin.Enabled = true;
                        
                        // Animation de secousse du champ de texte pour indiquer l'erreur
                        var originalLocation = tbVerificationCode.Location;
                        for (int i = 0; i < 5; i++)
                        {
                            tbVerificationCode.Left += (i % 2 == 0) ? 5 : -5;
                            await Task.Delay(50);
                        }
                        tbVerificationCode.Location = originalLocation;
                        
                        // Vider et mettre le focus sur le champ de texte
                        tbVerificationCode.Text = "";
                        tbVerificationCode.Focus();
                    }
                }
                catch (Exception ex)
                {
                    lblError.Text = "Verification failed. Please try again.";
                    btnLogin.Text = "Verify";
                    btnLogin.BackColor = Color.FromArgb(255, 130, 0);
                    btnLogin.Enabled = true;
                    
                    // Log l'erreur pour débogage
                    File.WriteAllText(
                        Path.Combine(Path.GetTempPath(), "verification_ui_error.log"),
                        $"Verification UI Error: {ex.Message}\n{ex.StackTrace}"
                    );
                }
            };
            
            // Configurer le comportement de la touche Entrée
            tbVerificationCode.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter && btnLogin.Enabled)
                {
                    btnLogin.PerformClick();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            
            // Donner le focus au TextBox
            tbVerificationCode.Focus();
        }

        // Méthode pour activer/désactiver les contrôles
        private void SetControlsEnabled(bool enabled)
        {
            txtUsername.Enabled = enabled;
            txtPassword.Enabled = enabled;
            customProductComboBox.Enabled = enabled;
            btnLogin.Enabled = enabled;
        }

        // Méthode pour démarrer le téléchargement
        private void StartDownloadProcess(string selectedProduct)
        {
            downloadProgress.Value = 0;
            downloadProgress.Visible = true;
            btnLogin.Visible = false;
            statusLabel.Visible = true;
            statusLabel.Text = "Downloading...";
            this.Invalidate();

            string tempPath = Path.GetTempPath();
            string exePath = Path.Combine(tempPath, selectedProduct == "Evade" ? "evade.exe" : "playerlist.exe");

            if (File.Exists(exePath))
            {
                try { File.Delete(exePath); }
                catch { }
            }

            using (var downloadClient = new WebClient())
            {
                downloadClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                downloadClient.DownloadProgressChanged += (s, e) =>
                {
                    downloadProgress.Value = e.ProgressPercentage;
                    statusLabel.Text = $"Downloading... {e.ProgressPercentage}%";
                    this.Invalidate();
                };
                downloadClient.DownloadFileCompleted += (s, e) =>
                {
                    if (e.Error != null)
                    {
                        lblError.Text = "Failed to download file";
                        _loginSuccessful = false;
                        downloadProgress.Visible = false;
                        btnLogin.Visible = true;
                        btnLogin.Enabled = true;
                        statusLabel.Visible = false;
                        File.WriteAllText(Path.Combine(tempPath, "download_error.log"),
                            $"Download Error: {e.Error.Message}");
                        return;
                    }

                    if (File.Exists(exePath))
                    {
                        btnLogin.Text = "LAUNCH";
                        btnLogin.Visible = true;
                        btnLogin.Enabled = true;
                        downloadProgress.Visible = false;
                        statusLabel.Visible = false;

                        btnLogin.Click -= new EventHandler(btnLogin_Click);
                        btnLogin.Click += (s, ev) =>
                        {
                            try
                            {
                                if (selectedProduct == "PlayerList")
                                {
                                    var startInfo = new ProcessStartInfo
                                    {
                                        FileName = exePath,
                                        Arguments = "--port 8204",
                                        UseShellExecute = true
                                    };
                                    Process.Start(startInfo);
                                }
                                else
                                {
                                    Process.Start(exePath);
                                }
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                            catch (Exception ex)
                            {
                                lblError.Text = "Failed to launch application";
                                File.WriteAllText(Path.Combine(tempPath, "launch_error.log"),
                                    $"Launch Error: {ex.Message}\nPath: {exePath}");
                            }
                        };
                    }
                };

                try
                {
                    string downloadUrl = selectedProduct == "Evade"
                        ? "https://XXXXXXXXXXXXx/XXXXXXXXXXXXXXXXXXXX/evade.exe"
                        : "https://XXXXXXXXXXXXx/XXXXXXXXXXXXXXXXXXXX/playerlist.exe";

                    downloadClient.DownloadFileAsync(
                        new Uri(downloadUrl),
                        exePath
                    );
                }
                catch (Exception ex)
                {
                    lblError.Text = "Failed to start download";
                    File.WriteAllText(Path.Combine(tempPath, "download_error.log"),
                        $"Download Start Error: {ex.Message}");
                    _loginSuccessful = false;
                    downloadProgress.Visible = false;
                    btnLogin.Visible = true;
                    btnLogin.Enabled = true;
                    statusLabel.Visible = false;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (webClient != null)
            {
                webClient.CancelAsync();
                webClient.Dispose();
            }

            if (appIcon != null)
            {
                appIcon.Dispose();
            }
        }

        private void welcomeLabel_Click(object sender, EventArgs e)
        {

        }

        private void RefreshTrayIcon()
        {
            // Force le rafraîchissement de l'icône dans la barre des tâches
            var placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(this.Handle, ref placement);

            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;
        }

        private void SetupCloseButton()
        {
            // Remplacer le bouton standard par un bouton personnalisé pour garantir un centrage parfait
            if (closeButton != null)
            {
                // Supprimer l'ancien bouton
                titleBar.Controls.Remove(closeButton);

                // Créer un nouveau label qui servira de bouton de fermeture
                Label customCloseButton = new Label
                {
                    Text = "×",
                    Font = new Font("Arial", 18, FontStyle.Regular),
                    ForeColor = Color.FromArgb(150, 150, 150),
                    Location = new Point(370, 0),
                    Size = new Size(30, 30),
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };

                // Ajouter les événements
                customCloseButton.MouseEnter += (s, e) =>
                {
                    customCloseButton.ForeColor = Color.FromArgb(232, 17, 35);
                    customCloseButton.BackColor = Color.FromArgb(40, 40, 40);
                };

                customCloseButton.MouseLeave += (s, e) =>
                {
                    customCloseButton.ForeColor = Color.FromArgb(150, 150, 150);
                    customCloseButton.BackColor = Color.Transparent;
                };

                customCloseButton.Click += (s, e) => this.Close();

                // Ajouter le nouveau bouton au titleBar
                titleBar.Controls.Add(customCloseButton);
                customCloseButton.BringToFront();
            }
        }

        private void customProductComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSavedCredentials();
        }
    }

    // Minimal ComboBox without blue highlight
    public class CustomComboBox : ComboBox
    {
        private Color _borderColor = Color.FromArgb(60, 60, 60);
        private Color _textColor = Color.FromArgb(220, 220, 220);
        private Color _backColor = Color.FromArgb(30, 30, 30);
        private bool _isDroppedDown = false;
        private readonly int _cornerRadius = 3;

        public CustomComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.FromArgb(220, 220, 220);
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            ItemHeight = 24;
            DropDownHeight = 160;

            DropDown += (s, e) => { _isDroppedDown = true; Invalidate(); };
            DropDownClosed += (s, e) => { _isDroppedDown = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            
            // Background
            using (GraphicsPath path = RoundedRect(rect, _cornerRadius))
            using (SolidBrush backBrush = new SolidBrush(_backColor))
            using (Pen borderPen = new Pen(_borderColor, 1))
            {
                e.Graphics.FillPath(backBrush, path);
                e.Graphics.DrawPath(borderPen, path);
            }
            
            // Text
            TextRenderer.DrawText(
                e.Graphics, 
                Text, 
                Font, 
                new Rectangle(10, 0, Width - 25, Height), 
                _textColor, 
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            );
            
            // Arrow - blue color matching SOT SOLUTION text
            int arrowX = Width - 15;
            int arrowY = Height / 2;
            
            using (Pen arrowPen = new Pen(Color.FromArgb(0, 120, 215), 1.5f))
            {
                // Draw a simple triangle
                e.Graphics.DrawLine(arrowPen, arrowX - 3, arrowY - 1, arrowX + 3, arrowY - 1);
                e.Graphics.DrawLine(arrowPen, arrowX - 2, arrowY, arrowX + 2, arrowY);
                e.Graphics.DrawLine(arrowPen, arrowX - 1, arrowY + 1, arrowX + 1, arrowY + 1);
                e.Graphics.DrawLine(arrowPen, arrowX, arrowY + 2, arrowX, arrowY + 2);
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            
            // Background
            Rectangle itemRect = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            
            using (SolidBrush backBrush = new SolidBrush(isSelected ? Color.FromArgb(50, 50, 50) : Color.FromArgb(35, 35, 35)))
            {
                e.Graphics.FillRectangle(backBrush, itemRect);
            }
            
            // Item text
            using (SolidBrush textBrush = new SolidBrush(_textColor))
            {
                StringFormat sf = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                
                e.Graphics.DrawString(
                    Items[e.Index].ToString(), 
                    Font, 
                    textBrush, 
                    new Rectangle(itemRect.X + 8, itemRect.Y, itemRect.Width - 10, itemRect.Height),
                    sf
                );
            }
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
} 