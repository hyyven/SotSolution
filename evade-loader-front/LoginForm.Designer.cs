namespace ProxyLoader
{
    partial class LoginForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            titleBar = new Panel();
            lblTitle = new Label();
            closeButton = new Button();
            welcomeLabel = new Label();
            mainPanel = new Panel();
            lblUsername = new Label();
            txtUsername = new TextBox();
            lblPassword = new Label();
            txtPassword = new TextBox();
            btnLogin = new Button();
            lblError = new Label();
            downloadProgress = new ProgressBar();
            statusLabel = new Label();
            productComboBox = new ComboBox();
            lblProduct = new Label();
            animationTimer = new System.Windows.Forms.Timer(components);
            titleBar.SuspendLayout();
            mainPanel.SuspendLayout();
            SuspendLayout();
            // 
            // titleBar
            // 
            titleBar.BackColor = Color.FromArgb(20, 20, 20);
            titleBar.Controls.Add(lblTitle);
            titleBar.Controls.Add(closeButton);
            titleBar.Dock = DockStyle.Top;
            titleBar.Location = new Point(0, 0);
            titleBar.Name = "titleBar";
            titleBar.Size = new Size(400, 30);
            titleBar.TabIndex = 0;
            titleBar.Paint += titleBar_Paint;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.ForeColor = Color.FromArgb(150, 150, 150);
            lblTitle.Location = new Point(10, 8);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(142, 15);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "SOT SOLUTION LOADER";
            // 
            // closeButton
            // 
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.Font = new Font("Consolas", 20F, FontStyle.Regular, GraphicsUnit.Point);
            closeButton.ForeColor = Color.FromArgb(150, 150, 150);
            closeButton.Location = new Point(370, 0);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(30, 30);
            closeButton.AutoSize = false;
            closeButton.Padding = new Padding(0, 0, 0, 0);
            closeButton.Margin = new Padding(0);
            closeButton.TabIndex = 1;
            closeButton.Text = "×";
            closeButton.TextAlign = ContentAlignment.MiddleCenter;
            closeButton.UseVisualStyleBackColor = false;
            closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(241, 112, 122);
            // 
            // welcomeLabel
            // 
            welcomeLabel.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
            welcomeLabel.ForeColor = Color.FromArgb(0, 120, 215);
            welcomeLabel.Location = new Point(0, 150);
            welcomeLabel.Name = "welcomeLabel";
            welcomeLabel.Size = new Size(400, 45);
            welcomeLabel.TabIndex = 0;
            welcomeLabel.Text = "SOT SOLUTION";
            welcomeLabel.TextAlign = ContentAlignment.MiddleCenter;
            welcomeLabel.Click += welcomeLabel_Click;
            // 
            // mainPanel
            // 
            mainPanel.BackColor = Color.FromArgb(25, 25, 25);
            mainPanel.Controls.Add(welcomeLabel);
            mainPanel.Controls.Add(lblUsername);
            mainPanel.Controls.Add(txtUsername);
            mainPanel.Controls.Add(lblPassword);
            mainPanel.Controls.Add(txtPassword);
            mainPanel.Controls.Add(btnLogin);
            mainPanel.Controls.Add(lblError);
            mainPanel.Controls.Add(downloadProgress);
            mainPanel.Controls.Add(statusLabel);
            mainPanel.Controls.Add(productComboBox);
            mainPanel.Controls.Add(lblProduct);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 30);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new Size(400, 470);
            mainPanel.TabIndex = 1;
            mainPanel.Paint += mainPanel_Paint;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblUsername.ForeColor = Color.FromArgb(200, 200, 200);
            lblUsername.Location = new Point(30, 270);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(71, 15);
            lblUsername.TabIndex = 3;
            lblUsername.Text = "USERNAME";
            // 
            // txtUsername
            // 
            txtUsername.BackColor = Color.FromArgb(30, 30, 30);
            txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            txtUsername.ForeColor = Color.White;
            txtUsername.Location = new Point(30, 290);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(340, 18);
            txtUsername.TabIndex = 4;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblPassword.ForeColor = Color.FromArgb(200, 200, 200);
            lblPassword.Location = new Point(30, 330);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(73, 15);
            lblPassword.TabIndex = 5;
            lblPassword.Text = "PASSWORD";
            // 
            // txtPassword
            // 
            txtPassword.BackColor = Color.FromArgb(30, 30, 30);
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            txtPassword.ForeColor = Color.White;
            txtPassword.Location = new Point(30, 350);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '●';
            txtPassword.Size = new Size(340, 18);
            txtPassword.TabIndex = 6;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.FromArgb(0, 120, 215);
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(30, 390);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(340, 35);
            btnLogin.TabIndex = 7;
            btnLogin.Text = "CHECK UPDATE";
            btnLogin.UseVisualStyleBackColor = false;
            // 
            // lblError
            // 
            lblError.AutoSize = true;
            lblError.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblError.ForeColor = Color.Red;
            lblError.Location = new Point(30, 435);
            lblError.Name = "lblError";
            lblError.Size = new Size(0, 15);
            lblError.TabIndex = 10;
            lblError.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // downloadProgress
            // 
            downloadProgress.Location = new Point(30, 390);
            downloadProgress.Name = "downloadProgress";
            downloadProgress.Size = new Size(340, 35);
            downloadProgress.Style = ProgressBarStyle.Continuous;
            downloadProgress.TabIndex = 8;
            downloadProgress.Visible = false;
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            statusLabel.ForeColor = Color.White;
            statusLabel.Location = new Point(30, 435);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(0, 15);
            statusLabel.TabIndex = 9;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Visible = false;
            // 
            // productComboBox
            // 
            productComboBox.BackColor = Color.FromArgb(30, 30, 30);
            productComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            productComboBox.FlatStyle = FlatStyle.Flat;
            productComboBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            productComboBox.ForeColor = Color.White;
            productComboBox.FormattingEnabled = true;
            productComboBox.Items.AddRange(new object[] { "Evade", "PlayerList" });
            productComboBox.Location = new Point(30, 230);
            productComboBox.Name = "productComboBox";
            productComboBox.Size = new Size(340, 25);
            productComboBox.TabIndex = 2;
            // 
            // lblProduct
            // 
            lblProduct.AutoSize = true;
            lblProduct.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblProduct.ForeColor = Color.FromArgb(200, 200, 200);
            lblProduct.Location = new Point(30, 210);
            lblProduct.Name = "lblProduct";
            lblProduct.Size = new Size(63, 15);
            lblProduct.TabIndex = 1;
            lblProduct.Text = "PRODUCT";
            // 
            // animationTimer
            // 
            animationTimer.Interval = 50;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 15, 15);
            ClientSize = new Size(400, 500);
            Controls.Add(mainPanel);
            Controls.Add(titleBar);
            FormBorderStyle = FormBorderStyle.None;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SOT SOLUTION LOADER";
            titleBar.ResumeLayout(false);
            titleBar.PerformLayout();
            mainPanel.ResumeLayout(false);
            mainPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel titleBar;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Label welcomeLabel;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ProgressBar downloadProgress;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Timer animationTimer;
        private System.Windows.Forms.ComboBox productComboBox;
        private System.Windows.Forms.Label lblProduct;
    }
} 