namespace BeanModManager
{
    partial class Main
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabInstalled = new System.Windows.Forms.TabPage();
            this.lblEmptyInstalled = new System.Windows.Forms.Label();
            this.panelInstalled = new System.Windows.Forms.FlowLayoutPanel();
            this.btnLaunchVanilla = new System.Windows.Forms.Button();
            this.tabStore = new System.Windows.Forms.TabPage();
            this.lblEmptyStore = new System.Windows.Forms.Label();
            this.panelStore = new System.Windows.Forms.FlowLayoutPanel();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.btnRestoreAmongUsData = new System.Windows.Forms.Button();
            this.btnBackupAmongUsData = new System.Windows.Forms.Button();
            this.btnUpdateAllMods = new System.Windows.Forms.Button();
            this.chkShowBetaVersions = new System.Windows.Forms.CheckBox();
            this.chkAutoUpdateMods = new System.Windows.Forms.CheckBox();
            this.lblMods = new System.Windows.Forms.Label();
            this.btnOpenDataFolder = new System.Windows.Forms.Button();
            this.btnOpenBepInExFolder = new System.Windows.Forms.Button();
            this.btnOpenModsFolder = new System.Windows.Forms.Button();
            this.btnOpenPluginsFolder = new System.Windows.Forms.Button();
            this.btnInstallBepInEx = new System.Windows.Forms.Button();
            this.btnDetectPath = new System.Windows.Forms.Button();
            this.btnBrowsePath = new System.Windows.Forms.Button();
            this.txtAmongUsPath = new System.Windows.Forms.TextBox();
            this.lblAmongUsPath = new System.Windows.Forms.Label();
            this.lblFolders = new System.Windows.Forms.Label();
            this.lblPaths = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.tabControl.SuspendLayout();
            this.tabInstalled.SuspendLayout();
            this.tabStore.SuspendLayout();
            this.tabSettings.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabInstalled);
            this.tabControl.Controls.Add(this.tabStore);
            this.tabControl.Controls.Add(this.tabSettings);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1072, 626);
            this.tabControl.TabIndex = 0;
            // 
            // tabInstalled
            // 
            this.tabInstalled.Controls.Add(this.lblEmptyInstalled);
            this.tabInstalled.Controls.Add(this.panelInstalled);
            this.tabInstalled.Controls.Add(this.btnLaunchVanilla);
            this.tabInstalled.Location = new System.Drawing.Point(4, 24);
            this.tabInstalled.Name = "tabInstalled";
            this.tabInstalled.Padding = new System.Windows.Forms.Padding(3);
            this.tabInstalled.Size = new System.Drawing.Size(1064, 598);
            this.tabInstalled.TabIndex = 0;
            this.tabInstalled.Text = "Installed Mods";
            this.tabInstalled.UseVisualStyleBackColor = true;
            // 
            // lblEmptyInstalled
            // 
            this.lblEmptyInstalled.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblEmptyInstalled.AutoSize = true;
            this.lblEmptyInstalled.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEmptyInstalled.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(130)))));
            this.lblEmptyInstalled.Location = new System.Drawing.Point(436, 263);
            this.lblEmptyInstalled.Name = "lblEmptyInstalled";
            this.lblEmptyInstalled.Size = new System.Drawing.Size(190, 25);
            this.lblEmptyInstalled.TabIndex = 2;
            this.lblEmptyInstalled.Text = "😢 No mods installed";
            this.lblEmptyInstalled.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEmptyInstalled.Visible = false;
            // 
            // panelInstalled
            // 
            this.panelInstalled.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelInstalled.AutoScroll = true;
            this.panelInstalled.Location = new System.Drawing.Point(3, 3);
            this.panelInstalled.Name = "panelInstalled";
            this.panelInstalled.Padding = new System.Windows.Forms.Padding(10);
            this.panelInstalled.Size = new System.Drawing.Size(1058, 546);
            this.panelInstalled.TabIndex = 1;
            // 
            // btnLaunchVanilla
            // 
            this.btnLaunchVanilla.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLaunchVanilla.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnLaunchVanilla.FlatAppearance.BorderSize = 0;
            this.btnLaunchVanilla.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(180)))));
            this.btnLaunchVanilla.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLaunchVanilla.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLaunchVanilla.ForeColor = System.Drawing.Color.White;
            this.btnLaunchVanilla.Location = new System.Drawing.Point(10, 556);
            this.btnLaunchVanilla.Name = "btnLaunchVanilla";
            this.btnLaunchVanilla.Size = new System.Drawing.Size(1044, 40);
            this.btnLaunchVanilla.TabIndex = 0;
            this.btnLaunchVanilla.Text = "🚀 Launch Vanilla Among Us";
            this.btnLaunchVanilla.UseVisualStyleBackColor = false;
            this.btnLaunchVanilla.Click += new System.EventHandler(this.btnLaunchVanilla_Click);
            // 
            // tabStore
            // 
            this.tabStore.Controls.Add(this.lblEmptyStore);
            this.tabStore.Controls.Add(this.panelStore);
            this.tabStore.Location = new System.Drawing.Point(4, 24);
            this.tabStore.Name = "tabStore";
            this.tabStore.Padding = new System.Windows.Forms.Padding(3);
            this.tabStore.Size = new System.Drawing.Size(992, 572);
            this.tabStore.TabIndex = 1;
            this.tabStore.Text = "Mod Store";
            this.tabStore.UseVisualStyleBackColor = true;
            // 
            // lblEmptyStore
            // 
            this.lblEmptyStore.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblEmptyStore.AutoSize = true;
            this.lblEmptyStore.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEmptyStore.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(130)))));
            this.lblEmptyStore.Location = new System.Drawing.Point(400, 280);
            this.lblEmptyStore.Name = "lblEmptyStore";
            this.lblEmptyStore.Size = new System.Drawing.Size(240, 25);
            this.lblEmptyStore.TabIndex = 1;
            this.lblEmptyStore.Text = "😢 No more mods to install";
            this.lblEmptyStore.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEmptyStore.Visible = false;
            // 
            // panelStore
            // 
            this.panelStore.AutoScroll = true;
            this.panelStore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStore.Location = new System.Drawing.Point(3, 3);
            this.panelStore.Name = "panelStore";
            this.panelStore.Padding = new System.Windows.Forms.Padding(10);
            this.panelStore.Size = new System.Drawing.Size(986, 566);
            this.panelStore.TabIndex = 0;
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.btnRestoreAmongUsData);
            this.tabSettings.Controls.Add(this.btnBackupAmongUsData);
            this.tabSettings.Controls.Add(this.btnUpdateAllMods);
            this.tabSettings.Controls.Add(this.chkShowBetaVersions);
            this.tabSettings.Controls.Add(this.chkAutoUpdateMods);
            this.tabSettings.Controls.Add(this.lblMods);
            this.tabSettings.Controls.Add(this.btnOpenDataFolder);
            this.tabSettings.Controls.Add(this.btnOpenBepInExFolder);
            this.tabSettings.Controls.Add(this.btnOpenModsFolder);
            this.tabSettings.Controls.Add(this.btnOpenPluginsFolder);
            this.tabSettings.Controls.Add(this.btnInstallBepInEx);
            this.tabSettings.Controls.Add(this.btnDetectPath);
            this.tabSettings.Controls.Add(this.btnBrowsePath);
            this.tabSettings.Controls.Add(this.txtAmongUsPath);
            this.tabSettings.Controls.Add(this.lblAmongUsPath);
            this.tabSettings.Controls.Add(this.lblFolders);
            this.tabSettings.Controls.Add(this.lblPaths);
            this.tabSettings.Location = new System.Drawing.Point(4, 24);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabSettings.Size = new System.Drawing.Size(992, 572);
            this.tabSettings.TabIndex = 2;
            this.tabSettings.Text = "Settings";
            this.tabSettings.UseVisualStyleBackColor = true;
            // 
            // btnRestoreAmongUsData
            // 
            this.btnRestoreAmongUsData.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRestoreAmongUsData.Location = new System.Drawing.Point(190, 385);
            this.btnRestoreAmongUsData.Name = "btnRestoreAmongUsData";
            this.btnRestoreAmongUsData.Size = new System.Drawing.Size(150, 35);
            this.btnRestoreAmongUsData.TabIndex = 15;
            this.btnRestoreAmongUsData.Text = "Restore Among Us Data";
            this.btnRestoreAmongUsData.UseVisualStyleBackColor = true;
            this.btnRestoreAmongUsData.Click += new System.EventHandler(this.btnRestoreAmongUsData_Click);
            // 
            // btnBackupAmongUsData
            // 
            this.btnBackupAmongUsData.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBackupAmongUsData.Location = new System.Drawing.Point(20, 385);
            this.btnBackupAmongUsData.Name = "btnBackupAmongUsData";
            this.btnBackupAmongUsData.Size = new System.Drawing.Size(150, 35);
            this.btnBackupAmongUsData.TabIndex = 14;
            this.btnBackupAmongUsData.Text = "Backup Among Us Data";
            this.btnBackupAmongUsData.UseVisualStyleBackColor = true;
            this.btnBackupAmongUsData.Click += new System.EventHandler(this.btnBackupAmongUsData_Click);
            // 
            // btnUpdateAllMods
            // 
            this.btnUpdateAllMods.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnUpdateAllMods.Location = new System.Drawing.Point(20, 345);
            this.btnUpdateAllMods.Name = "btnUpdateAllMods";
            this.btnUpdateAllMods.Size = new System.Drawing.Size(150, 35);
            this.btnUpdateAllMods.TabIndex = 13;
            this.btnUpdateAllMods.Text = "Update All Mods";
            this.btnUpdateAllMods.UseVisualStyleBackColor = true;
            this.btnUpdateAllMods.Click += new System.EventHandler(this.btnUpdateAllMods_Click);
            // 
            // chkShowBetaVersions
            // 
            this.chkShowBetaVersions.AutoSize = true;
            this.chkShowBetaVersions.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkShowBetaVersions.Location = new System.Drawing.Point(20, 315);
            this.chkShowBetaVersions.Name = "chkShowBetaVersions";
            this.chkShowBetaVersions.Size = new System.Drawing.Size(127, 19);
            this.chkShowBetaVersions.TabIndex = 14;
            this.chkShowBetaVersions.Text = "Show beta versions";
            this.chkShowBetaVersions.UseVisualStyleBackColor = true;
            this.chkShowBetaVersions.CheckedChanged += new System.EventHandler(this.chkShowBetaVersions_CheckedChanged);
            // 
            // chkAutoUpdateMods
            // 
            this.chkAutoUpdateMods.AutoSize = true;
            this.chkAutoUpdateMods.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkAutoUpdateMods.Location = new System.Drawing.Point(20, 290);
            this.chkAutoUpdateMods.Name = "chkAutoUpdateMods";
            this.chkAutoUpdateMods.Size = new System.Drawing.Size(127, 19);
            this.chkAutoUpdateMods.TabIndex = 12;
            this.chkAutoUpdateMods.Text = "Auto-update mods";
            this.chkAutoUpdateMods.UseVisualStyleBackColor = true;
            this.chkAutoUpdateMods.CheckedChanged += new System.EventHandler(this.chkAutoUpdateMods_CheckedChanged);
            // 
            // lblMods
            // 
            this.lblMods.AutoSize = true;
            this.lblMods.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMods.Location = new System.Drawing.Point(20, 260);
            this.lblMods.Name = "lblMods";
            this.lblMods.Size = new System.Drawing.Size(46, 19);
            this.lblMods.TabIndex = 11;
            this.lblMods.Text = "Mods";
            // 
            // btnOpenDataFolder
            // 
            this.btnOpenDataFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenDataFolder.Location = new System.Drawing.Point(530, 210);
            this.btnOpenDataFolder.Name = "btnOpenDataFolder";
            this.btnOpenDataFolder.Size = new System.Drawing.Size(150, 35);
            this.btnOpenDataFolder.TabIndex = 10;
            this.btnOpenDataFolder.Text = "Open Data Folder";
            this.btnOpenDataFolder.UseVisualStyleBackColor = true;
            this.btnOpenDataFolder.Click += new System.EventHandler(this.btnOpenDataFolder_Click);
            // 
            // btnOpenBepInExFolder
            // 
            this.btnOpenBepInExFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenBepInExFolder.Location = new System.Drawing.Point(360, 210);
            this.btnOpenBepInExFolder.Name = "btnOpenBepInExFolder";
            this.btnOpenBepInExFolder.Size = new System.Drawing.Size(150, 35);
            this.btnOpenBepInExFolder.TabIndex = 9;
            this.btnOpenBepInExFolder.Text = "Open BepInEx Folder";
            this.btnOpenBepInExFolder.UseVisualStyleBackColor = true;
            this.btnOpenBepInExFolder.Click += new System.EventHandler(this.btnOpenBepInExFolder_Click);
            // 
            // btnOpenModsFolder
            // 
            this.btnOpenModsFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenModsFolder.Location = new System.Drawing.Point(190, 210);
            this.btnOpenModsFolder.Name = "btnOpenModsFolder";
            this.btnOpenModsFolder.Size = new System.Drawing.Size(150, 35);
            this.btnOpenModsFolder.TabIndex = 8;
            this.btnOpenModsFolder.Text = "Open Mods Folder";
            this.btnOpenModsFolder.UseVisualStyleBackColor = true;
            this.btnOpenModsFolder.Click += new System.EventHandler(this.btnOpenModsFolder_Click);
            // 
            // btnOpenPluginsFolder
            // 
            this.btnOpenPluginsFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenPluginsFolder.Location = new System.Drawing.Point(20, 210);
            this.btnOpenPluginsFolder.Name = "btnOpenPluginsFolder";
            this.btnOpenPluginsFolder.Size = new System.Drawing.Size(150, 35);
            this.btnOpenPluginsFolder.TabIndex = 7;
            this.btnOpenPluginsFolder.Text = "Open Plugins Folder";
            this.btnOpenPluginsFolder.UseVisualStyleBackColor = true;
            this.btnOpenPluginsFolder.Click += new System.EventHandler(this.btnOpenPluginsFolder_Click);
            // 
            // btnInstallBepInEx
            // 
            this.btnInstallBepInEx.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInstallBepInEx.Location = new System.Drawing.Point(20, 120);
            this.btnInstallBepInEx.Name = "btnInstallBepInEx";
            this.btnInstallBepInEx.Size = new System.Drawing.Size(150, 35);
            this.btnInstallBepInEx.TabIndex = 5;
            this.btnInstallBepInEx.Text = "Install BepInEx";
            this.btnInstallBepInEx.UseVisualStyleBackColor = true;
            this.btnInstallBepInEx.Click += new System.EventHandler(this.btnInstallBepInEx_Click);
            // 
            // btnDetectPath
            // 
            this.btnDetectPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDetectPath.Location = new System.Drawing.Point(850, 74);
            this.btnDetectPath.Name = "btnDetectPath";
            this.btnDetectPath.Size = new System.Drawing.Size(100, 25);
            this.btnDetectPath.TabIndex = 4;
            this.btnDetectPath.Text = "Auto-Detect";
            this.btnDetectPath.UseVisualStyleBackColor = true;
            this.btnDetectPath.Click += new System.EventHandler(this.btnDetectPath_Click);
            // 
            // btnBrowsePath
            // 
            this.btnBrowsePath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowsePath.Location = new System.Drawing.Point(740, 74);
            this.btnBrowsePath.Name = "btnBrowsePath";
            this.btnBrowsePath.Size = new System.Drawing.Size(100, 25);
            this.btnBrowsePath.TabIndex = 3;
            this.btnBrowsePath.Text = "Browse...";
            this.btnBrowsePath.UseVisualStyleBackColor = true;
            this.btnBrowsePath.Click += new System.EventHandler(this.btnBrowsePath_Click);
            // 
            // txtAmongUsPath
            // 
            this.txtAmongUsPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAmongUsPath.Location = new System.Drawing.Point(20, 75);
            this.txtAmongUsPath.Name = "txtAmongUsPath";
            this.txtAmongUsPath.ReadOnly = true;
            this.txtAmongUsPath.Size = new System.Drawing.Size(700, 23);
            this.txtAmongUsPath.TabIndex = 2;
            // 
            // lblAmongUsPath
            // 
            this.lblAmongUsPath.AutoSize = true;
            this.lblAmongUsPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAmongUsPath.Location = new System.Drawing.Point(20, 50);
            this.lblAmongUsPath.Name = "lblAmongUsPath";
            this.lblAmongUsPath.Size = new System.Drawing.Size(93, 15);
            this.lblAmongUsPath.TabIndex = 1;
            this.lblAmongUsPath.Text = "Among Us Path:";
            // 
            // lblFolders
            // 
            this.lblFolders.AutoSize = true;
            this.lblFolders.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFolders.Location = new System.Drawing.Point(20, 180);
            this.lblFolders.Name = "lblFolders";
            this.lblFolders.Size = new System.Drawing.Size(58, 19);
            this.lblFolders.TabIndex = 6;
            this.lblFolders.Text = "Folders";
            // 
            // lblPaths
            // 
            this.lblPaths.AutoSize = true;
            this.lblPaths.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPaths.Location = new System.Drawing.Point(20, 20);
            this.lblPaths.Name = "lblPaths";
            this.lblPaths.Size = new System.Drawing.Size(45, 19);
            this.lblPaths.TabIndex = 0;
            this.lblPaths.Text = "Paths";
            // 
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.progressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 626);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1072, 22);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(39, 17);
            this.lblStatus.Text = "Ready";
            // 
            // progressBar
            // 
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(200, 16);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.Visible = false;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(1072, 648);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.statusStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bean Mod Manager";
            this.tabControl.ResumeLayout(false);
            this.tabInstalled.ResumeLayout(false);
            this.tabInstalled.PerformLayout();
            this.tabStore.ResumeLayout(false);
            this.tabStore.PerformLayout();
            this.tabSettings.ResumeLayout(false);
            this.tabSettings.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabInstalled;
        private System.Windows.Forms.TabPage tabStore;
        private System.Windows.Forms.TabPage tabSettings;
        private System.Windows.Forms.FlowLayoutPanel panelInstalled;
        private System.Windows.Forms.FlowLayoutPanel panelStore;
        private System.Windows.Forms.Button btnLaunchVanilla;
        private System.Windows.Forms.Label lblAmongUsPath;
        private System.Windows.Forms.TextBox txtAmongUsPath;
        private System.Windows.Forms.Button btnBrowsePath;
        private System.Windows.Forms.Button btnDetectPath;
        private System.Windows.Forms.Button btnInstallBepInEx;
        private System.Windows.Forms.Button btnOpenPluginsFolder;
        private System.Windows.Forms.Button btnOpenModsFolder;
        private System.Windows.Forms.Button btnOpenBepInExFolder;
        private System.Windows.Forms.Button btnOpenDataFolder;
        private System.Windows.Forms.Label lblFolders;
        private System.Windows.Forms.Label lblPaths;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.Label lblEmptyStore;
        private System.Windows.Forms.Label lblEmptyInstalled;
        private System.Windows.Forms.CheckBox chkAutoUpdateMods;
        private System.Windows.Forms.CheckBox chkShowBetaVersions;
        private System.Windows.Forms.Button btnUpdateAllMods;
        private System.Windows.Forms.Button btnBackupAmongUsData;
        private System.Windows.Forms.Button btnRestoreAmongUsData;
        private System.Windows.Forms.Label lblMods;
    }
}

