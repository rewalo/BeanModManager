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
            this.installedLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblInstalledHeader = new System.Windows.Forms.Label();
            this.panelInstalledHost = new System.Windows.Forms.Panel();
            this.storeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblStoreHeader = new System.Windows.Forms.Label();
            this.panelStoreHost = new System.Windows.Forms.Panel();
            this.settingsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpPath = new System.Windows.Forms.GroupBox();
            this.pathLayout = new System.Windows.Forms.TableLayoutPanel();
            this.txtAmongUsPath = new System.Windows.Forms.TextBox();
            this.btnBrowsePath = new System.Windows.Forms.Button();
            this.btnDetectPath = new System.Windows.Forms.Button();
            this.lblAmongUsPath = new System.Windows.Forms.Label();
            this.grpBepInEx = new System.Windows.Forms.GroupBox();
            this.flowBepInEx = new System.Windows.Forms.FlowLayoutPanel();
            this.btnInstallBepInEx = new System.Windows.Forms.Button();
            this.btnOpenBepInExFolder = new System.Windows.Forms.Button();
            this.btnOpenPluginsFolder = new System.Windows.Forms.Button();
            this.grpFolders = new System.Windows.Forms.GroupBox();
            this.flowFolders = new System.Windows.Forms.FlowLayoutPanel();
            this.btnOpenModsFolder = new System.Windows.Forms.Button();
            this.btnOpenAmongUsFolder = new System.Windows.Forms.Button();
            this.grpMods = new System.Windows.Forms.GroupBox();
            this.flowMods = new System.Windows.Forms.FlowLayoutPanel();
            this.chkAutoUpdateMods = new System.Windows.Forms.CheckBox();
            this.chkShowBetaVersions = new System.Windows.Forms.CheckBox();
            this.btnUpdateAllMods = new System.Windows.Forms.Button();
            this.grpData = new System.Windows.Forms.GroupBox();
            this.flowData = new System.Windows.Forms.FlowLayoutPanel();
            this.btnBackupAmongUsData = new System.Windows.Forms.Button();
            this.btnRestoreAmongUsData = new System.Windows.Forms.Button();
            this.btnOpenDataFolder = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.tabControl.SuspendLayout();
            this.tabInstalled.SuspendLayout();
            this.tabStore.SuspendLayout();
            this.tabSettings.SuspendLayout();
            this.grpPath.SuspendLayout();
            this.pathLayout.SuspendLayout();
            this.grpBepInEx.SuspendLayout();
            this.flowBepInEx.SuspendLayout();
            this.grpFolders.SuspendLayout();
            this.flowFolders.SuspendLayout();
            this.grpMods.SuspendLayout();
            this.flowMods.SuspendLayout();
            this.grpData.SuspendLayout();
            this.flowData.SuspendLayout();
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
            this.tabControl.ItemSize = new System.Drawing.Size(120, 30);
            this.tabControl.Padding = new System.Drawing.Point(12, 4);
            this.tabControl.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControl.Size = new System.Drawing.Size(1072, 626);
            this.tabControl.TabIndex = 0;
            // 
            // tabInstalled
            // 
            this.tabInstalled.Controls.Add(this.installedLayout);
            this.tabInstalled.Location = new System.Drawing.Point(4, 34);
            this.tabInstalled.Name = "tabInstalled";
            this.tabInstalled.Padding = new System.Windows.Forms.Padding(10);
            this.tabInstalled.Size = new System.Drawing.Size(1064, 588);
            this.tabInstalled.TabIndex = 0;
            this.tabInstalled.Text = "Installed Mods";
            this.tabInstalled.UseVisualStyleBackColor = true;
            // 
            // installedLayout
            // 
            this.installedLayout.ColumnCount = 1;
            this.installedLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.installedLayout.Controls.Add(this.lblInstalledHeader, 0, 0);
            this.installedLayout.Controls.Add(this.panelInstalledHost, 0, 1);
            this.installedLayout.Controls.Add(this.btnLaunchVanilla, 0, 2);
            this.installedLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.installedLayout.Location = new System.Drawing.Point(10, 10);
            this.installedLayout.Name = "installedLayout";
            this.installedLayout.RowCount = 3;
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.installedLayout.Size = new System.Drawing.Size(1044, 568);
            this.installedLayout.TabIndex = 3;
            // 
            // lblInstalledHeader
            // 
            this.lblInstalledHeader.AutoSize = true;
            this.lblInstalledHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInstalledHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(90)))));
            this.lblInstalledHeader.Location = new System.Drawing.Point(3, 0);
            this.lblInstalledHeader.Name = "lblInstalledHeader";
            this.lblInstalledHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblInstalledHeader.Size = new System.Drawing.Size(104, 24);
            this.lblInstalledHeader.TabIndex = 0;
            this.lblInstalledHeader.Text = "Installed Mods";
            // 
            // lblEmptyInstalled
            // 
            this.lblEmptyInstalled.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmptyInstalled.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEmptyInstalled.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(150)))));
            this.lblEmptyInstalled.Name = "lblEmptyInstalled";
            this.lblEmptyInstalled.TabIndex = 2;
            this.lblEmptyInstalled.Text = "😢 No mods installed";
            this.lblEmptyInstalled.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEmptyInstalled.Visible = false;
            // 
            // panelInstalledHost
            // 
            this.panelInstalledHost.BackColor = System.Drawing.Color.Transparent;
            this.panelInstalledHost.Controls.Add(this.lblEmptyInstalled);
            this.panelInstalledHost.Controls.Add(this.panelInstalled);
            this.panelInstalledHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelInstalledHost.Location = new System.Drawing.Point(0, 27);
            this.panelInstalledHost.Margin = new System.Windows.Forms.Padding(0);
            this.panelInstalledHost.Name = "panelInstalledHost";
            this.panelInstalledHost.Size = new System.Drawing.Size(1044, 501);
            this.panelInstalledHost.TabIndex = 3;
            // 
            // panelInstalled
            // 
            this.panelInstalled.AutoScroll = true;
            this.panelInstalled.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelInstalled.Margin = new System.Windows.Forms.Padding(0);
            this.panelInstalled.Name = "panelInstalled";
            this.panelInstalled.Padding = new System.Windows.Forms.Padding(10);
            this.panelInstalled.Size = new System.Drawing.Size(1044, 501);
            this.panelInstalled.TabIndex = 1;
            // 
            // btnLaunchVanilla
            // 
            this.btnLaunchVanilla.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnLaunchVanilla.FlatAppearance.BorderSize = 0;
            this.btnLaunchVanilla.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(180)))));
            this.btnLaunchVanilla.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLaunchVanilla.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLaunchVanilla.ForeColor = System.Drawing.Color.White;
            this.btnLaunchVanilla.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLaunchVanilla.Location = new System.Drawing.Point(10, 520);
            this.btnLaunchVanilla.Margin = new System.Windows.Forms.Padding(10, 10, 10, 0);
            this.btnLaunchVanilla.Name = "btnLaunchVanilla";
            this.btnLaunchVanilla.Size = new System.Drawing.Size(1024, 40);
            this.btnLaunchVanilla.TabIndex = 0;
            this.btnLaunchVanilla.Text = "🚀 Launch Vanilla Among Us";
            this.btnLaunchVanilla.UseVisualStyleBackColor = false;
            this.btnLaunchVanilla.Click += new System.EventHandler(this.btnLaunchVanilla_Click);
            // 
            // tabStore
            // 
            this.tabStore.Controls.Add(this.storeLayout);
            this.tabStore.Controls.Add(this.lblEmptyStore);
            this.tabStore.Location = new System.Drawing.Point(4, 34);
            this.tabStore.Name = "tabStore";
            this.tabStore.Padding = new System.Windows.Forms.Padding(10);
            this.tabStore.Size = new System.Drawing.Size(1064, 588);
            this.tabStore.TabIndex = 1;
            this.tabStore.Text = "Mod Store";
            this.tabStore.UseVisualStyleBackColor = true;
            // 
            // lblEmptyStore
            // 
            this.lblEmptyStore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmptyStore.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEmptyStore.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(150)))));
            this.lblEmptyStore.Name = "lblEmptyStore";
            this.lblEmptyStore.TabIndex = 1;
            this.lblEmptyStore.Text = "😢 No more mods to install";
            this.lblEmptyStore.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEmptyStore.Visible = false;
            // 
            // panelStore
            // 
            this.panelStore.AutoScroll = true;
            this.panelStore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStore.Location = new System.Drawing.Point(0, 0);
            this.panelStore.Name = "panelStore";
            this.panelStore.Padding = new System.Windows.Forms.Padding(10);
            this.panelStore.Size = new System.Drawing.Size(1044, 541);
            this.panelStore.TabIndex = 0;
            // 
            // storeLayout
            // 
            this.storeLayout.ColumnCount = 1;
            this.storeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.storeLayout.Controls.Add(this.lblStoreHeader, 0, 0);
            this.storeLayout.Controls.Add(this.panelStoreHost, 0, 1);
            this.storeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.storeLayout.Location = new System.Drawing.Point(10, 10);
            this.storeLayout.Name = "storeLayout";
            this.storeLayout.RowCount = 2;
            this.storeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.storeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.storeLayout.Size = new System.Drawing.Size(1044, 568);
            this.storeLayout.TabIndex = 2;
            // 
            // lblStoreHeader
            // 
            this.lblStoreHeader.AutoSize = true;
            this.lblStoreHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStoreHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(90)))));
            this.lblStoreHeader.Location = new System.Drawing.Point(3, 0);
            this.lblStoreHeader.Name = "lblStoreHeader";
            this.lblStoreHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblStoreHeader.Size = new System.Drawing.Size(70, 24);
            this.lblStoreHeader.TabIndex = 0;
            this.lblStoreHeader.Text = "Mod Store";
            // 
            // panelStoreHost
            // 
            this.panelStoreHost.BackColor = System.Drawing.Color.Transparent;
            this.panelStoreHost.Controls.Add(this.lblEmptyStore);
            this.panelStoreHost.Controls.Add(this.panelStore);
            this.panelStoreHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStoreHost.Location = new System.Drawing.Point(0, 24);
            this.panelStoreHost.Margin = new System.Windows.Forms.Padding(0);
            this.panelStoreHost.Name = "panelStoreHost";
            this.panelStoreHost.Size = new System.Drawing.Size(1044, 544);
            this.panelStoreHost.TabIndex = 2;
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.settingsLayout);
            this.tabSettings.Location = new System.Drawing.Point(4, 24);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabSettings.Size = new System.Drawing.Size(1064, 598);
            this.tabSettings.TabIndex = 2;
            this.tabSettings.Text = "Settings";
            this.tabSettings.UseVisualStyleBackColor = true;
            // 
            // settingsLayout
            // 
            this.settingsLayout.ColumnCount = 2;
            this.settingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.settingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.settingsLayout.Controls.Add(this.grpPath, 0, 0);
            this.settingsLayout.Controls.Add(this.grpBepInEx, 0, 1);
            this.settingsLayout.Controls.Add(this.grpFolders, 1, 1);
            this.settingsLayout.Controls.Add(this.grpMods, 0, 2);
            this.settingsLayout.Controls.Add(this.grpData, 1, 2);
            this.settingsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsLayout.Location = new System.Drawing.Point(3, 3);
            this.settingsLayout.Name = "settingsLayout";
            this.settingsLayout.Padding = new System.Windows.Forms.Padding(10);
            this.settingsLayout.RowCount = 3;
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingsLayout.Size = new System.Drawing.Size(1058, 592);
            this.settingsLayout.TabIndex = 0;
            this.settingsLayout.SetColumnSpan(this.grpPath, 2);
            // 
            // grpPath
            // 
            this.grpPath.Controls.Add(this.pathLayout);
            this.grpPath.Controls.Add(this.lblAmongUsPath);
            this.grpPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPath.Location = new System.Drawing.Point(13, 13);
            this.grpPath.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
            this.grpPath.Name = "grpPath";
            this.grpPath.Padding = new System.Windows.Forms.Padding(12);
            this.grpPath.Size = new System.Drawing.Size(1032, 114);
            this.grpPath.TabIndex = 0;
            this.grpPath.TabStop = false;
            this.grpPath.Text = "Among Us Path";
            // 
            // pathLayout
            // 
            this.pathLayout.ColumnCount = 3;
            this.pathLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pathLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.pathLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.pathLayout.Controls.Add(this.txtAmongUsPath, 0, 0);
            this.pathLayout.Controls.Add(this.btnBrowsePath, 1, 0);
            this.pathLayout.Controls.Add(this.btnDetectPath, 2, 0);
            this.pathLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.pathLayout.Location = new System.Drawing.Point(12, 46);
            this.pathLayout.Margin = new System.Windows.Forms.Padding(0);
            this.pathLayout.Name = "pathLayout";
            this.pathLayout.RowCount = 1;
            this.pathLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pathLayout.Size = new System.Drawing.Size(1008, 35);
            this.pathLayout.TabIndex = 2;
            // 
            // txtAmongUsPath
            // 
            this.txtAmongUsPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAmongUsPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAmongUsPath.Location = new System.Drawing.Point(3, 3);
            this.txtAmongUsPath.Name = "txtAmongUsPath";
            this.txtAmongUsPath.ReadOnly = true;
            this.txtAmongUsPath.Size = new System.Drawing.Size(762, 23);
            this.txtAmongUsPath.TabIndex = 0;
            // 
            // btnBrowsePath
            // 
            this.btnBrowsePath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBrowsePath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowsePath.Location = new System.Drawing.Point(771, 3);
            this.btnBrowsePath.Name = "btnBrowsePath";
            this.btnBrowsePath.Size = new System.Drawing.Size(114, 29);
            this.btnBrowsePath.TabIndex = 1;
            this.btnBrowsePath.Text = "Browse...";
            this.btnBrowsePath.UseVisualStyleBackColor = true;
            this.btnBrowsePath.Click += new System.EventHandler(this.btnBrowsePath_Click);
            // 
            // btnDetectPath
            // 
            this.btnDetectPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDetectPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDetectPath.Location = new System.Drawing.Point(891, 3);
            this.btnDetectPath.Name = "btnDetectPath";
            this.btnDetectPath.Size = new System.Drawing.Size(114, 29);
            this.btnDetectPath.TabIndex = 2;
            this.btnDetectPath.Text = "Auto-Detect";
            this.btnDetectPath.UseVisualStyleBackColor = true;
            this.btnDetectPath.Click += new System.EventHandler(this.btnDetectPath_Click);
            // 
            // lblAmongUsPath
            // 
            this.lblAmongUsPath.AutoSize = true;
            this.lblAmongUsPath.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAmongUsPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAmongUsPath.Location = new System.Drawing.Point(12, 24);
            this.lblAmongUsPath.Name = "lblAmongUsPath";
            this.lblAmongUsPath.Size = new System.Drawing.Size(93, 15);
            this.lblAmongUsPath.TabIndex = 1;
            this.lblAmongUsPath.Text = "Among Us Path:";
            // 
            // grpBepInEx
            // 
            this.grpBepInEx.AutoSize = true;
            this.grpBepInEx.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.grpBepInEx.Controls.Add(this.flowBepInEx);
            this.grpBepInEx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpBepInEx.Location = new System.Drawing.Point(13, 140);
            this.grpBepInEx.Margin = new System.Windows.Forms.Padding(3, 3, 10, 10);
            this.grpBepInEx.Name = "grpBepInEx";
            this.grpBepInEx.Padding = new System.Windows.Forms.Padding(12);
            this.grpBepInEx.Size = new System.Drawing.Size(509, 164);
            this.grpBepInEx.TabIndex = 1;
            this.grpBepInEx.TabStop = false;
            this.grpBepInEx.Text = "BepInEx";
            // 
            // flowBepInEx
            // 
            this.flowBepInEx.AutoScroll = false;
            this.flowBepInEx.AutoSize = true;
            this.flowBepInEx.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowBepInEx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowBepInEx.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowBepInEx.Location = new System.Drawing.Point(12, 24);
            this.flowBepInEx.Name = "flowBepInEx";
            this.flowBepInEx.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.flowBepInEx.Size = new System.Drawing.Size(485, 128);
            this.flowBepInEx.TabIndex = 0;
            this.flowBepInEx.WrapContents = false;
            this.flowBepInEx.Controls.Add(this.btnInstallBepInEx);
            this.flowBepInEx.Controls.Add(this.btnOpenBepInExFolder);
            this.flowBepInEx.Controls.Add(this.btnOpenPluginsFolder);
            // 
            // btnInstallBepInEx
            // 
            this.btnInstallBepInEx.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInstallBepInEx.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnInstallBepInEx.Name = "btnInstallBepInEx";
            this.btnInstallBepInEx.Size = new System.Drawing.Size(230, 35);
            this.btnInstallBepInEx.TabIndex = 0;
            this.btnInstallBepInEx.Text = "Install BepInEx";
            this.btnInstallBepInEx.UseVisualStyleBackColor = true;
            this.btnInstallBepInEx.Click += new System.EventHandler(this.btnInstallBepInEx_Click);
            // 
            // btnOpenBepInExFolder
            // 
            this.btnOpenBepInExFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenBepInExFolder.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnOpenBepInExFolder.Name = "btnOpenBepInExFolder";
            this.btnOpenBepInExFolder.Size = new System.Drawing.Size(230, 35);
            this.btnOpenBepInExFolder.TabIndex = 1;
            this.btnOpenBepInExFolder.Text = "Open BepInEx Folder";
            this.btnOpenBepInExFolder.UseVisualStyleBackColor = true;
            this.btnOpenBepInExFolder.Click += new System.EventHandler(this.btnOpenBepInExFolder_Click);
            // 
            // btnOpenPluginsFolder
            // 
            this.btnOpenPluginsFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenPluginsFolder.Margin = new System.Windows.Forms.Padding(0);
            this.btnOpenPluginsFolder.Name = "btnOpenPluginsFolder";
            this.btnOpenPluginsFolder.Size = new System.Drawing.Size(230, 35);
            this.btnOpenPluginsFolder.TabIndex = 2;
            this.btnOpenPluginsFolder.Text = "Open Plugins Folder";
            this.btnOpenPluginsFolder.UseVisualStyleBackColor = true;
            this.btnOpenPluginsFolder.Click += new System.EventHandler(this.btnOpenPluginsFolder_Click);
            // 
            // grpFolders
            // 
            this.grpFolders.AutoSize = true;
            this.grpFolders.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.grpFolders.Controls.Add(this.flowFolders);
            this.grpFolders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpFolders.Location = new System.Drawing.Point(535, 140);
            this.grpFolders.Margin = new System.Windows.Forms.Padding(3, 3, 13, 10);
            this.grpFolders.Name = "grpFolders";
            this.grpFolders.Padding = new System.Windows.Forms.Padding(12);
            this.grpFolders.Size = new System.Drawing.Size(510, 164);
            this.grpFolders.TabIndex = 2;
            this.grpFolders.TabStop = false;
            this.grpFolders.Text = "Quick Folders";
            // 
            // flowFolders
            // 
            this.flowFolders.AutoScroll = false;
            this.flowFolders.AutoSize = true;
            this.flowFolders.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowFolders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowFolders.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowFolders.Location = new System.Drawing.Point(12, 24);
            this.flowFolders.Name = "flowFolders";
            this.flowFolders.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.flowFolders.Size = new System.Drawing.Size(486, 128);
            this.flowFolders.TabIndex = 0;
            this.flowFolders.WrapContents = false;
            this.flowFolders.Controls.Add(this.btnOpenModsFolder);
            this.flowFolders.Controls.Add(this.btnOpenAmongUsFolder);
            this.flowFolders.Controls.Add(this.btnOpenDataFolder);
            // 
            // btnOpenModsFolder
            // 
            this.btnOpenModsFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenModsFolder.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnOpenModsFolder.Name = "btnOpenModsFolder";
            this.btnOpenModsFolder.Size = new System.Drawing.Size(230, 35);
            this.btnOpenModsFolder.TabIndex = 0;
            this.btnOpenModsFolder.Text = "Open Mods Folder";
            this.btnOpenModsFolder.UseVisualStyleBackColor = true;
            this.btnOpenModsFolder.Click += new System.EventHandler(this.btnOpenModsFolder_Click);
            // 
            // btnOpenAmongUsFolder
            // 
            this.btnOpenAmongUsFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenAmongUsFolder.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnOpenAmongUsFolder.Name = "btnOpenAmongUsFolder";
            this.btnOpenAmongUsFolder.Size = new System.Drawing.Size(230, 35);
            this.btnOpenAmongUsFolder.TabIndex = 2;
            this.btnOpenAmongUsFolder.Text = "Open Among Us Folder";
            this.btnOpenAmongUsFolder.UseVisualStyleBackColor = true;
            this.btnOpenAmongUsFolder.Click += new System.EventHandler(this.btnOpenAmongUsFolder_Click);
            // 
            // btnOpenDataFolder
            // 
            this.btnOpenDataFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenDataFolder.Margin = new System.Windows.Forms.Padding(0);
            this.btnOpenDataFolder.Name = "btnOpenDataFolder";
            this.btnOpenDataFolder.Size = new System.Drawing.Size(230, 35);
            this.btnOpenDataFolder.TabIndex = 1;
            this.btnOpenDataFolder.Text = "Open Data Folder";
            this.btnOpenDataFolder.UseVisualStyleBackColor = true;
            this.btnOpenDataFolder.Click += new System.EventHandler(this.btnOpenDataFolder_Click);
            // 
            // grpMods
            // 
            this.grpMods.Controls.Add(this.flowMods);
            this.grpMods.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMods.Location = new System.Drawing.Point(13, 317);
            this.grpMods.Margin = new System.Windows.Forms.Padding(3, 3, 10, 10);
            this.grpMods.Name = "grpMods";
            this.grpMods.Padding = new System.Windows.Forms.Padding(12);
            this.grpMods.Size = new System.Drawing.Size(509, 119);
            this.grpMods.TabIndex = 3;
            this.grpMods.TabStop = false;
            this.grpMods.Text = "Mod Management";
            // 
            // flowMods
            // 
            this.flowMods.AutoScroll = true;
            this.flowMods.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowMods.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowMods.Location = new System.Drawing.Point(12, 24);
            this.flowMods.Name = "flowMods";
            this.flowMods.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.flowMods.Size = new System.Drawing.Size(485, 83);
            this.flowMods.TabIndex = 0;
            this.flowMods.WrapContents = false;
            this.flowMods.Controls.Add(this.chkAutoUpdateMods);
            this.flowMods.Controls.Add(this.chkShowBetaVersions);
            this.flowMods.Controls.Add(this.btnUpdateAllMods);
            // 
            // chkAutoUpdateMods
            // 
            this.chkAutoUpdateMods.AutoSize = true;
            this.chkAutoUpdateMods.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkAutoUpdateMods.Margin = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.chkAutoUpdateMods.Name = "chkAutoUpdateMods";
            this.chkAutoUpdateMods.Size = new System.Drawing.Size(127, 19);
            this.chkAutoUpdateMods.TabIndex = 0;
            this.chkAutoUpdateMods.Text = "Auto-update mods";
            this.chkAutoUpdateMods.UseVisualStyleBackColor = true;
            this.chkAutoUpdateMods.CheckedChanged += new System.EventHandler(this.chkAutoUpdateMods_CheckedChanged);
            // 
            // chkShowBetaVersions
            // 
            this.chkShowBetaVersions.AutoSize = true;
            this.chkShowBetaVersions.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkShowBetaVersions.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.chkShowBetaVersions.Name = "chkShowBetaVersions";
            this.chkShowBetaVersions.Size = new System.Drawing.Size(127, 19);
            this.chkShowBetaVersions.TabIndex = 1;
            this.chkShowBetaVersions.Text = "Show beta versions";
            this.chkShowBetaVersions.UseVisualStyleBackColor = true;
            this.chkShowBetaVersions.CheckedChanged += new System.EventHandler(this.chkShowBetaVersions_CheckedChanged);
            // 
            // btnUpdateAllMods
            // 
            this.btnUpdateAllMods.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnUpdateAllMods.Margin = new System.Windows.Forms.Padding(0);
            this.btnUpdateAllMods.Name = "btnUpdateAllMods";
            this.btnUpdateAllMods.Size = new System.Drawing.Size(230, 35);
            this.btnUpdateAllMods.TabIndex = 2;
            this.btnUpdateAllMods.Text = "Update All Mods";
            this.btnUpdateAllMods.UseVisualStyleBackColor = true;
            this.btnUpdateAllMods.Click += new System.EventHandler(this.btnUpdateAllMods_Click);
            // 
            // grpData
            // 
            this.grpData.Controls.Add(this.flowData);
            this.grpData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpData.Location = new System.Drawing.Point(535, 317);
            this.grpData.Margin = new System.Windows.Forms.Padding(3, 3, 13, 10);
            this.grpData.Name = "grpData";
            this.grpData.Padding = new System.Windows.Forms.Padding(12);
            this.grpData.Size = new System.Drawing.Size(510, 119);
            this.grpData.TabIndex = 4;
            this.grpData.TabStop = false;
            this.grpData.Text = "Save Data";
            // 
            // flowData
            // 
            this.flowData.AutoScroll = true;
            this.flowData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowData.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowData.Location = new System.Drawing.Point(12, 24);
            this.flowData.Name = "flowData";
            this.flowData.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.flowData.Size = new System.Drawing.Size(486, 83);
            this.flowData.TabIndex = 0;
            this.flowData.WrapContents = false;
            this.flowData.Controls.Add(this.btnBackupAmongUsData);
            this.flowData.Controls.Add(this.btnRestoreAmongUsData);
            // 
            // btnBackupAmongUsData
            // 
            this.btnBackupAmongUsData.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBackupAmongUsData.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnBackupAmongUsData.Name = "btnBackupAmongUsData";
            this.btnBackupAmongUsData.Size = new System.Drawing.Size(230, 35);
            this.btnBackupAmongUsData.TabIndex = 0;
            this.btnBackupAmongUsData.Text = "Backup Among Us Data";
            this.btnBackupAmongUsData.UseVisualStyleBackColor = true;
            this.btnBackupAmongUsData.Click += new System.EventHandler(this.btnBackupAmongUsData_Click);
            // 
            // btnRestoreAmongUsData
            // 
            this.btnRestoreAmongUsData.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRestoreAmongUsData.Margin = new System.Windows.Forms.Padding(0);
            this.btnRestoreAmongUsData.Name = "btnRestoreAmongUsData";
            this.btnRestoreAmongUsData.Size = new System.Drawing.Size(230, 35);
            this.btnRestoreAmongUsData.TabIndex = 1;
            this.btnRestoreAmongUsData.Text = "Restore Among Us Data";
            this.btnRestoreAmongUsData.UseVisualStyleBackColor = true;
            this.btnRestoreAmongUsData.Click += new System.EventHandler(this.btnRestoreAmongUsData_Click);
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
            this.grpData.ResumeLayout(false);
            this.flowData.ResumeLayout(false);
            this.grpMods.ResumeLayout(false);
            this.flowMods.ResumeLayout(false);
            this.flowMods.PerformLayout();
            this.grpFolders.ResumeLayout(false);
            this.flowFolders.ResumeLayout(false);
            this.grpBepInEx.ResumeLayout(false);
            this.flowBepInEx.ResumeLayout(false);
            this.grpPath.ResumeLayout(false);
            this.grpPath.PerformLayout();
            this.pathLayout.ResumeLayout(false);
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
        private System.Windows.Forms.TableLayoutPanel installedLayout;
        private System.Windows.Forms.Label lblInstalledHeader;
        private System.Windows.Forms.FlowLayoutPanel panelInstalled;
        private System.Windows.Forms.Panel panelInstalledHost;
        private System.Windows.Forms.FlowLayoutPanel panelStore;
        private System.Windows.Forms.Panel panelStoreHost;
        private System.Windows.Forms.Button btnLaunchVanilla;
        private System.Windows.Forms.TableLayoutPanel storeLayout;
        private System.Windows.Forms.Label lblStoreHeader;
        private System.Windows.Forms.TableLayoutPanel settingsLayout;
        private System.Windows.Forms.GroupBox grpPath;
        private System.Windows.Forms.Label lblAmongUsPath;
        private System.Windows.Forms.TableLayoutPanel pathLayout;
        private System.Windows.Forms.TextBox txtAmongUsPath;
        private System.Windows.Forms.Button btnBrowsePath;
        private System.Windows.Forms.Button btnDetectPath;
        private System.Windows.Forms.GroupBox grpBepInEx;
        private System.Windows.Forms.FlowLayoutPanel flowBepInEx;
        private System.Windows.Forms.Button btnInstallBepInEx;
        private System.Windows.Forms.Button btnOpenBepInExFolder;
        private System.Windows.Forms.Button btnOpenPluginsFolder;
        private System.Windows.Forms.GroupBox grpFolders;
        private System.Windows.Forms.FlowLayoutPanel flowFolders;
        private System.Windows.Forms.Button btnOpenModsFolder;
        private System.Windows.Forms.Button btnOpenDataFolder;
        private System.Windows.Forms.Button btnOpenAmongUsFolder;
        private System.Windows.Forms.GroupBox grpMods;
        private System.Windows.Forms.FlowLayoutPanel flowMods;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.Label lblEmptyStore;
        private System.Windows.Forms.Label lblEmptyInstalled;
        private System.Windows.Forms.CheckBox chkAutoUpdateMods;
        private System.Windows.Forms.CheckBox chkShowBetaVersions;
        private System.Windows.Forms.Button btnUpdateAllMods;
        private System.Windows.Forms.GroupBox grpData;
        private System.Windows.Forms.FlowLayoutPanel flowData;
        private System.Windows.Forms.Button btnBackupAmongUsData;
        private System.Windows.Forms.Button btnRestoreAmongUsData;
    }
}

