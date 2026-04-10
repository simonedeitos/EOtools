namespace FinCheck_0k
{
    partial class SettingsForm
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabFolder = new System.Windows.Forms.TabPage();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtFolderPath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtServerName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.numIntervalMinutes = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.tabEmail = new System.Windows.Forms.TabPage();
            this.btnTestEmail = new System.Windows.Forms.Button();
            this.txtEmailSubject = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtEmailTo = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtEmailFrom = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtSmtpPassword = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtSmtpUsername = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.chkUseSsl = new System.Windows.Forms.CheckBox();
            this.numSmtpPort = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.txtSmtpServer = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabFolder.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numIntervalMinutes)).BeginInit();
            this.tabEmail.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSmtpPort)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabFolder);
            this.tabControl1.Controls.Add(this.tabEmail);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(410, 280);
            this.tabControl1.TabIndex = 0;
            // 
            // tabFolder
            // 
            this.tabFolder.Controls.Add(this.btnBrowse);
            this.tabFolder.Controls.Add(this.label1);
            this.tabFolder.Controls.Add(this.txtFolderPath);
            this.tabFolder.Controls.Add(this.label2);
            this.tabFolder.Controls.Add(this.txtServerName);
            this.tabFolder.Controls.Add(this.label3);
            this.tabFolder.Controls.Add(this.numIntervalMinutes);
            this.tabFolder.Controls.Add(this.label4);
            this.tabFolder.Location = new System.Drawing.Point(4, 22);
            this.tabFolder.Name = "tabFolder";
            this.tabFolder.Padding = new System.Windows.Forms.Padding(3);
            this.tabFolder.Size = new System.Drawing.Size(402, 254);
            this.tabFolder.TabIndex = 0;
            this.tabFolder.Text = "Folder Settings";
            this.tabFolder.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(132, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Folder to Monitor:";
            // 
            // txtFolderPath
            // 
            this.txtFolderPath.Location = new System.Drawing.Point(9, 33);
            this.txtFolderPath.Name = "txtFolderPath";
            this.txtFolderPath.Size = new System.Drawing.Size(306, 20);
            this.txtFolderPath.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(321, 32);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 67);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Server Name:";
            // 
            // txtServerName
            // 
            this.txtServerName.Location = new System.Drawing.Point(9, 83);
            this.txtServerName.Name = "txtServerName";
            this.txtServerName.Size = new System.Drawing.Size(306, 20);
            this.txtServerName.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 117);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(148, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Check interval (minutes):";
            // 
            // numIntervalMinutes
            // 
            this.numIntervalMinutes.Location = new System.Drawing.Point(163, 115);
            this.numIntervalMinutes.Maximum = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            this.numIntervalMinutes.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numIntervalMinutes.Name = "numIntervalMinutes";
            this.numIntervalMinutes.Size = new System.Drawing.Size(70, 20);
            this.numIntervalMinutes.TabIndex = 6;
            this.numIntervalMinutes.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(239, 117);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(86, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "(min 1, max 1440)";
            // 
            // tabEmail
            // 
            this.tabEmail.Controls.Add(this.btnTestEmail);
            this.tabEmail.Controls.Add(this.txtEmailSubject);
            this.tabEmail.Controls.Add(this.label12);
            this.tabEmail.Controls.Add(this.txtEmailTo);
            this.tabEmail.Controls.Add(this.label11);
            this.tabEmail.Controls.Add(this.txtEmailFrom);
            this.tabEmail.Controls.Add(this.label10);
            this.tabEmail.Controls.Add(this.txtSmtpPassword);
            this.tabEmail.Controls.Add(this.label9);
            this.tabEmail.Controls.Add(this.txtSmtpUsername);
            this.tabEmail.Controls.Add(this.label8);
            this.tabEmail.Controls.Add(this.chkUseSsl);
            this.tabEmail.Controls.Add(this.numSmtpPort);
            this.tabEmail.Controls.Add(this.label7);
            this.tabEmail.Controls.Add(this.txtSmtpServer);
            this.tabEmail.Controls.Add(this.label6);
            this.tabEmail.Location = new System.Drawing.Point(4, 22);
            this.tabEmail.Name = "tabEmail";
            this.tabEmail.Padding = new System.Windows.Forms.Padding(3);
            this.tabEmail.Size = new System.Drawing.Size(402, 254);
            this.tabEmail.TabIndex = 1;
            this.tabEmail.Text = "Email Settings";
            this.tabEmail.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(19, 30);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(74, 13);
            this.label6.TabIndex = 0;
            this.label6.Text = "SMTP Server:";
            // 
            // txtSmtpServer
            // 
            this.txtSmtpServer.Location = new System.Drawing.Point(98, 27);
            this.txtSmtpServer.Name = "txtSmtpServer";
            this.txtSmtpServer.Size = new System.Drawing.Size(298, 20);
            this.txtSmtpServer.TabIndex = 1;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(28, 55);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 13);
            this.label7.TabIndex = 2;
            this.label7.Text = "SMTP Port:";
            // 
            // numSmtpPort
            // 
            this.numSmtpPort.Location = new System.Drawing.Point(98, 53);
            this.numSmtpPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numSmtpPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numSmtpPort.Name = "numSmtpPort";
            this.numSmtpPort.Size = new System.Drawing.Size(70, 20);
            this.numSmtpPort.TabIndex = 3;
            this.numSmtpPort.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
            // 
            // chkUseSsl
            // 
            this.chkUseSsl.AutoSize = true;
            this.chkUseSsl.Location = new System.Drawing.Point(199, 56);
            this.chkUseSsl.Name = "chkUseSsl";
            this.chkUseSsl.Size = new System.Drawing.Size(68, 17);
            this.chkUseSsl.TabIndex = 4;
            this.chkUseSsl.Text = "Use SSL";
            this.chkUseSsl.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(5, 82);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(91, 13);
            this.label8.TabIndex = 5;
            this.label8.Text = "SMTP Username:";
            // 
            // txtSmtpUsername
            // 
            this.txtSmtpUsername.Location = new System.Drawing.Point(98, 79);
            this.txtSmtpUsername.Name = "txtSmtpUsername";
            this.txtSmtpUsername.Size = new System.Drawing.Size(298, 20);
            this.txtSmtpUsername.TabIndex = 6;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(7, 108);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(89, 13);
            this.label9.TabIndex = 7;
            this.label9.Text = "SMTP Password:";
            // 
            // txtSmtpPassword
            // 
            this.txtSmtpPassword.Location = new System.Drawing.Point(98, 105);
            this.txtSmtpPassword.Name = "txtSmtpPassword";
            this.txtSmtpPassword.PasswordChar = '*';
            this.txtSmtpPassword.Size = new System.Drawing.Size(298, 20);
            this.txtSmtpPassword.TabIndex = 8;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(32, 134);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(61, 13);
            this.label10.TabIndex = 9;
            this.label10.Text = "Email From:";
            // 
            // txtEmailFrom
            // 
            this.txtEmailFrom.Location = new System.Drawing.Point(98, 131);
            this.txtEmailFrom.Name = "txtEmailFrom";
            this.txtEmailFrom.Size = new System.Drawing.Size(298, 20);
            this.txtEmailFrom.TabIndex = 10;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(42, 160);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(51, 13);
            this.label11.TabIndex = 11;
            this.label11.Text = "Email To:";
            // 
            // txtEmailTo
            // 
            this.txtEmailTo.Location = new System.Drawing.Point(98, 157);
            this.txtEmailTo.Name = "txtEmailTo";
            this.txtEmailTo.Size = new System.Drawing.Size(298, 20);
            this.txtEmailTo.TabIndex = 12;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(15, 186);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(74, 13);
            this.label12.TabIndex = 13;
            this.label12.Text = "Email Subject:";
            // 
            // txtEmailSubject
            // 
            this.txtEmailSubject.Location = new System.Drawing.Point(98, 183);
            this.txtEmailSubject.Name = "txtEmailSubject";
            this.txtEmailSubject.Size = new System.Drawing.Size(298, 20);
            this.txtEmailSubject.TabIndex = 14;
            this.txtEmailSubject.Text = "FinCheck 0k Alert";
            // 
            // btnTestEmail
            // 
            this.btnTestEmail.Location = new System.Drawing.Point(298, 225);
            this.btnTestEmail.Name = "btnTestEmail";
            this.btnTestEmail.Size = new System.Drawing.Size(98, 23);
            this.btnTestEmail.TabIndex = 15;
            this.btnTestEmail.Text = "Test Email";
            this.btnTestEmail.UseVisualStyleBackColor = true;
            this.btnTestEmail.Click += new System.EventHandler(this.btnTestEmail_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(266, 298);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(347, 298);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 333);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings - FinCheck 0k";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabFolder.ResumeLayout(false);
            this.tabFolder.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numIntervalMinutes)).EndInit();
            this.tabEmail.ResumeLayout(false);
            this.tabEmail.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSmtpPort)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabFolder;
        private System.Windows.Forms.TabPage tabEmail;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtFolderPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numIntervalMinutes;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnTestEmail;
        private System.Windows.Forms.TextBox txtEmailSubject;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtEmailTo;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtEmailFrom;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtSmtpPassword;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtSmtpUsername;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox chkUseSsl;
        private System.Windows.Forms.NumericUpDown numSmtpPort;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtSmtpServer;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
