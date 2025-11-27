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
            if (disposing)
            {
                _installedSearchDebounceTimer?.Stop();
                _installedSearchDebounceTimer?.Dispose();
                _storeSearchDebounceTimer?.Stop();
                _storeSearchDebounceTimer?.Dispose();
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
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.headerStrip = new System.Windows.Forms.Panel();
            this.lblHeaderInfo = new System.Windows.Forms.Label();
            this.leftSidebar = new System.Windows.Forms.Panel();
            this.sidebarButtons = new System.Windows.Forms.Panel();
            this.btnSidebarLaunchVanilla = new System.Windows.Forms.Button();
            this.btnSidebarSettings = new System.Windows.Forms.Button();
            this.btnSidebarStore = new System.Windows.Forms.Button();
            this.btnSidebarInstalled = new System.Windows.Forms.Button();
            this.sidebarDivider = new System.Windows.Forms.Panel();
            this.sidebarStats = new System.Windows.Forms.Panel();
            this.lblPendingUpdates = new System.Windows.Forms.Label();
            this.lblInstalledCount = new System.Windows.Forms.Label();
            this.sidebarHeader = new System.Windows.Forms.Panel();
            this.lblSidebarTitle = new System.Windows.Forms.Label();
            this.sidebarBorder = new System.Windows.Forms.Panel();
            this.tabControl = new BeanModManager.Controls.ThemedTabControl();
            this.tabInstalled = new System.Windows.Forms.TabPage();
            this.installedLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblInstalledHeader = new System.Windows.Forms.Label();
            this.flowInstalledFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.lblInstalledSearch = new System.Windows.Forms.Label();
            this.txtInstalledSearch = new System.Windows.Forms.TextBox();
            this.lblInstalledCategory = new System.Windows.Forms.Label();
            this.cmbInstalledCategory = new System.Windows.Forms.ComboBox();
            this.panelInstalledHost = new System.Windows.Forms.Panel();
            this.panelEmptyInstalled = new System.Windows.Forms.Panel();
            this.lblEmptyInstalled = new System.Windows.Forms.Label();
            this.btnEmptyInstalledBrowseFeatured = new System.Windows.Forms.Button();
            this.btnEmptyInstalledBrowseStore = new System.Windows.Forms.Button();
            this.panelInstalled = new BeanModManager.Controls.ThemedFlowLayoutPanel();
            this.btnLaunchSelected = new System.Windows.Forms.Button();
            this.tabStore = new System.Windows.Forms.TabPage();
            this.storeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblStoreHeader = new System.Windows.Forms.Label();
            this.flowStoreFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.lblStoreSearch = new System.Windows.Forms.Label();
            this.txtStoreSearch = new System.Windows.Forms.TextBox();
            this.lblStoreCategory = new System.Windows.Forms.Label();
            this.cmbStoreCategory = new System.Windows.Forms.ComboBox();
            this.panelStoreHost = new System.Windows.Forms.Panel();
            this.panelEmptyStore = new System.Windows.Forms.Panel();
            this.lblEmptyStore = new System.Windows.Forms.Label();
            this.btnEmptyStoreClearFilters = new System.Windows.Forms.Button();
            this.btnEmptyStoreBrowseFeatured = new System.Windows.Forms.Button();
            this.panelStore = new BeanModManager.Controls.ThemedFlowLayoutPanel();
            this.panelBulkActionsInstalled = new System.Windows.Forms.Panel();
            this.lblBulkSelectedCountInstalled = new System.Windows.Forms.Label();
            this.btnBulkUninstallInstalled = new System.Windows.Forms.Button();
            this.btnBulkDeselectAllInstalled = new System.Windows.Forms.Button();
            this.panelBulkActionsStore = new System.Windows.Forms.Panel();
            this.lblBulkSelectedCountStore = new System.Windows.Forms.Label();
            this.btnBulkInstallStore = new System.Windows.Forms.Button();
            this.btnBulkDeselectAllStore = new System.Windows.Forms.Button();
            this.tabSettings = new System.Windows.Forms.TabPage();
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
            this.btnOpenDataFolder = new System.Windows.Forms.Button();
            this.btnClearCache = new System.Windows.Forms.Button();
            this.grpAppearance = new System.Windows.Forms.GroupBox();
            this.appearanceLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTheme = new System.Windows.Forms.Label();
            this.cmbTheme = new System.Windows.Forms.ComboBox();
            this.grpMods = new System.Windows.Forms.GroupBox();
            this.flowMods = new System.Windows.Forms.FlowLayoutPanel();
            this.chkAutoUpdateMods = new System.Windows.Forms.CheckBox();
            this.chkShowBetaVersions = new System.Windows.Forms.CheckBox();
            this.btnUpdateAllMods = new System.Windows.Forms.Button();
            this.grpData = new System.Windows.Forms.GroupBox();
            this.flowData = new System.Windows.Forms.FlowLayoutPanel();
            this.btnBackupAmongUsData = new System.Windows.Forms.Button();
            this.btnRestoreAmongUsData = new System.Windows.Forms.Button();
            this.btnClearBackup = new System.Windows.Forms.Button();
            this.statusStrip.SuspendLayout();
            this.headerStrip.SuspendLayout();
            this.leftSidebar.SuspendLayout();
            this.sidebarButtons.SuspendLayout();
            this.sidebarStats.SuspendLayout();
            this.sidebarHeader.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabInstalled.SuspendLayout();
            this.installedLayout.SuspendLayout();
            this.flowInstalledFilters.SuspendLayout();
            this.panelInstalledHost.SuspendLayout();
            this.panelEmptyInstalled.SuspendLayout();
            this.tabStore.SuspendLayout();
            this.storeLayout.SuspendLayout();
            this.flowStoreFilters.SuspendLayout();
            this.panelStoreHost.SuspendLayout();
            this.panelEmptyStore.SuspendLayout();
            this.panelBulkActionsInstalled.SuspendLayout();
            this.panelBulkActionsStore.SuspendLayout();
            this.tabSettings.SuspendLayout();
            this.settingsLayout.SuspendLayout();
            this.grpPath.SuspendLayout();
            this.pathLayout.SuspendLayout();
            this.grpBepInEx.SuspendLayout();
            this.flowBepInEx.SuspendLayout();
            this.grpFolders.SuspendLayout();
            this.flowFolders.SuspendLayout();
            this.grpAppearance.SuspendLayout();
            this.appearanceLayout.SuspendLayout();
            this.grpMods.SuspendLayout();
            this.flowMods.SuspendLayout();
            this.grpData.SuspendLayout();
            this.flowData.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.progressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 742);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 0, 0);
            this.statusStrip.Size = new System.Drawing.Size(1335, 22);
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
            // headerStrip
            // 
            this.headerStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.headerStrip.Controls.Add(this.lblHeaderInfo);
            this.headerStrip.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerStrip.Location = new System.Drawing.Point(0, 0);
            this.headerStrip.Name = "headerStrip";
            this.headerStrip.Padding = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.headerStrip.Size = new System.Drawing.Size(1335, 32);
            this.headerStrip.TabIndex = 2;
            this.headerStrip.Paint += new System.Windows.Forms.PaintEventHandler(this.HeaderStrip_Paint);
            // 
            // lblHeaderInfo
            // 
            this.lblHeaderInfo.AutoSize = true;
            this.lblHeaderInfo.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHeaderInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(110)))));
            this.lblHeaderInfo.Location = new System.Drawing.Point(16, 9);
            this.lblHeaderInfo.Name = "lblHeaderInfo";
            this.lblHeaderInfo.Size = new System.Drawing.Size(39, 15);
            this.lblHeaderInfo.TabIndex = 0;
            this.lblHeaderInfo.Text = "Ready";
            // 
            // leftSidebar
            // 
            this.leftSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.leftSidebar.Controls.Add(this.sidebarButtons);
            this.leftSidebar.Controls.Add(this.sidebarDivider);
            this.leftSidebar.Controls.Add(this.sidebarStats);
            this.leftSidebar.Controls.Add(this.sidebarHeader);
            this.leftSidebar.Controls.Add(this.sidebarBorder);
            this.leftSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.leftSidebar.Location = new System.Drawing.Point(0, 32);
            this.leftSidebar.Name = "leftSidebar";
            this.leftSidebar.Size = new System.Drawing.Size(220, 710);
            this.leftSidebar.TabIndex = 3;
            // 
            // sidebarButtons
            // 
            this.sidebarButtons.Controls.Add(this.btnSidebarLaunchVanilla);
            this.sidebarButtons.Controls.Add(this.btnSidebarSettings);
            this.sidebarButtons.Controls.Add(this.btnSidebarStore);
            this.sidebarButtons.Controls.Add(this.btnSidebarInstalled);
            this.sidebarButtons.Dock = System.Windows.Forms.DockStyle.Top;
            this.sidebarButtons.Location = new System.Drawing.Point(0, 129);
            this.sidebarButtons.Name = "sidebarButtons";
            this.sidebarButtons.Padding = new System.Windows.Forms.Padding(8, 12, 8, 12);
            this.sidebarButtons.Size = new System.Drawing.Size(219, 172);
            this.sidebarButtons.TabIndex = 0;
            // 
            // btnSidebarLaunchVanilla
            // 
            this.btnSidebarLaunchVanilla.BackColor = System.Drawing.Color.Transparent;
            this.btnSidebarLaunchVanilla.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSidebarLaunchVanilla.FlatAppearance.BorderSize = 0;
            this.btnSidebarLaunchVanilla.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(247)))));
            this.btnSidebarLaunchVanilla.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSidebarLaunchVanilla.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSidebarLaunchVanilla.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(122)))), ((int)(((byte)(189)))));
            this.btnSidebarLaunchVanilla.Location = new System.Drawing.Point(8, 132);
            this.btnSidebarLaunchVanilla.Margin = new System.Windows.Forms.Padding(0);
            this.btnSidebarLaunchVanilla.Name = "btnSidebarLaunchVanilla";
            this.btnSidebarLaunchVanilla.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnSidebarLaunchVanilla.Size = new System.Drawing.Size(203, 40);
            this.btnSidebarLaunchVanilla.TabIndex = 3;
            this.btnSidebarLaunchVanilla.Text = "Launch Vanilla";
            this.btnSidebarLaunchVanilla.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSidebarLaunchVanilla.UseVisualStyleBackColor = false;
            this.btnSidebarLaunchVanilla.Click += new System.EventHandler(this.btnSidebarLaunchVanilla_Click);
            // 
            // btnSidebarSettings
            // 
            this.btnSidebarSettings.BackColor = System.Drawing.Color.Transparent;
            this.btnSidebarSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSidebarSettings.FlatAppearance.BorderSize = 0;
            this.btnSidebarSettings.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(247)))));
            this.btnSidebarSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSidebarSettings.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSidebarSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(110)))));
            this.btnSidebarSettings.Location = new System.Drawing.Point(8, 92);
            this.btnSidebarSettings.Margin = new System.Windows.Forms.Padding(0);
            this.btnSidebarSettings.Name = "btnSidebarSettings";
            this.btnSidebarSettings.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnSidebarSettings.Size = new System.Drawing.Size(203, 40);
            this.btnSidebarSettings.TabIndex = 2;
            this.btnSidebarSettings.Text = "Settings";
            this.btnSidebarSettings.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSidebarSettings.UseVisualStyleBackColor = false;
            this.btnSidebarSettings.Click += new System.EventHandler(this.btnSidebarSettings_Click);
            // 
            // btnSidebarStore
            // 
            this.btnSidebarStore.BackColor = System.Drawing.Color.Transparent;
            this.btnSidebarStore.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSidebarStore.FlatAppearance.BorderSize = 0;
            this.btnSidebarStore.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(247)))));
            this.btnSidebarStore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSidebarStore.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSidebarStore.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(110)))));
            this.btnSidebarStore.Location = new System.Drawing.Point(8, 52);
            this.btnSidebarStore.Margin = new System.Windows.Forms.Padding(0);
            this.btnSidebarStore.Name = "btnSidebarStore";
            this.btnSidebarStore.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnSidebarStore.Size = new System.Drawing.Size(203, 40);
            this.btnSidebarStore.TabIndex = 1;
            this.btnSidebarStore.Text = "Mod Store";
            this.btnSidebarStore.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSidebarStore.UseVisualStyleBackColor = false;
            this.btnSidebarStore.Click += new System.EventHandler(this.btnSidebarStore_Click);
            // 
            // btnSidebarInstalled
            // 
            this.btnSidebarInstalled.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(247)))));
            this.btnSidebarInstalled.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSidebarInstalled.FlatAppearance.BorderSize = 0;
            this.btnSidebarInstalled.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(247)))));
            this.btnSidebarInstalled.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSidebarInstalled.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSidebarInstalled.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(155)))), ((int)(((byte)(105)))));
            this.btnSidebarInstalled.Location = new System.Drawing.Point(8, 12);
            this.btnSidebarInstalled.Margin = new System.Windows.Forms.Padding(0);
            this.btnSidebarInstalled.Name = "btnSidebarInstalled";
            this.btnSidebarInstalled.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnSidebarInstalled.Size = new System.Drawing.Size(203, 40);
            this.btnSidebarInstalled.TabIndex = 0;
            this.btnSidebarInstalled.Text = "Installed Mods";
            this.btnSidebarInstalled.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSidebarInstalled.UseVisualStyleBackColor = false;
            this.btnSidebarInstalled.Click += new System.EventHandler(this.btnSidebarInstalled_Click);
            // 
            // sidebarDivider
            // 
            this.sidebarDivider.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(225)))), ((int)(((byte)(235)))));
            this.sidebarDivider.Dock = System.Windows.Forms.DockStyle.Top;
            this.sidebarDivider.Location = new System.Drawing.Point(0, 128);
            this.sidebarDivider.Name = "sidebarDivider";
            this.sidebarDivider.Size = new System.Drawing.Size(219, 1);
            this.sidebarDivider.TabIndex = 2;
            // 
            // sidebarStats
            // 
            this.sidebarStats.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.sidebarStats.Controls.Add(this.lblPendingUpdates);
            this.sidebarStats.Controls.Add(this.lblInstalledCount);
            this.sidebarStats.Dock = System.Windows.Forms.DockStyle.Top;
            this.sidebarStats.Location = new System.Drawing.Point(0, 48);
            this.sidebarStats.Name = "sidebarStats";
            this.sidebarStats.Padding = new System.Windows.Forms.Padding(16, 8, 16, 12);
            this.sidebarStats.Size = new System.Drawing.Size(219, 80);
            this.sidebarStats.TabIndex = 1;
            // 
            // lblPendingUpdates
            // 
            this.lblPendingUpdates.AutoSize = true;
            this.lblPendingUpdates.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPendingUpdates.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(110)))));
            this.lblPendingUpdates.Location = new System.Drawing.Point(16, 48);
            this.lblPendingUpdates.Name = "lblPendingUpdates";
            this.lblPendingUpdates.Size = new System.Drawing.Size(109, 15);
            this.lblPendingUpdates.TabIndex = 1;
            this.lblPendingUpdates.Text = "Pending Updates: 0";
            // 
            // lblInstalledCount
            // 
            this.lblInstalledCount.AutoSize = true;
            this.lblInstalledCount.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInstalledCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(55)))), ((int)(((byte)(85)))));
            this.lblInstalledCount.Location = new System.Drawing.Point(16, 16);
            this.lblInstalledCount.Name = "lblInstalledCount";
            this.lblInstalledCount.Size = new System.Drawing.Size(115, 19);
            this.lblInstalledCount.TabIndex = 0;
            this.lblInstalledCount.Text = "Installed: 0 mods";
            // 
            // sidebarHeader
            // 
            this.sidebarHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.sidebarHeader.Controls.Add(this.lblSidebarTitle);
            this.sidebarHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.sidebarHeader.Location = new System.Drawing.Point(0, 0);
            this.sidebarHeader.Name = "sidebarHeader";
            this.sidebarHeader.Padding = new System.Windows.Forms.Padding(16, 14, 16, 8);
            this.sidebarHeader.Size = new System.Drawing.Size(219, 48);
            this.sidebarHeader.TabIndex = 3;
            // 
            // lblSidebarTitle
            // 
            this.lblSidebarTitle.AutoSize = true;
            this.lblSidebarTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSidebarTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(55)))), ((int)(((byte)(85)))));
            this.lblSidebarTitle.Location = new System.Drawing.Point(16, 14);
            this.lblSidebarTitle.Name = "lblSidebarTitle";
            this.lblSidebarTitle.Size = new System.Drawing.Size(145, 20);
            this.lblSidebarTitle.TabIndex = 0;
            this.lblSidebarTitle.Text = "Bean Mod Manager";
            // 
            // sidebarBorder
            // 
            this.sidebarBorder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(225)))), ((int)(((byte)(235)))));
            this.sidebarBorder.Dock = System.Windows.Forms.DockStyle.Right;
            this.sidebarBorder.Location = new System.Drawing.Point(219, 0);
            this.sidebarBorder.Name = "sidebarBorder";
            this.sidebarBorder.Size = new System.Drawing.Size(1, 710);
            this.sidebarBorder.TabIndex = 4;
            // 
            // tabControl
            // 
            this.tabControl.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tabControl.Controls.Add(this.tabInstalled);
            this.tabControl.Controls.Add(this.tabStore);
            this.tabControl.Controls.Add(this.tabSettings);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabControl.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(212)))), ((int)(((byte)(219)))), ((int)(((byte)(238)))));
            this.tabControl.ItemSize = new System.Drawing.Size(0, 1);
            this.tabControl.Location = new System.Drawing.Point(220, 32);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(0, 0);
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1115, 710);
            this.tabControl.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControl.TabIndex = 0;
            this.tabControl.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.tabControl_DrawItem);
            // 
            // tabInstalled
            // 
            this.tabInstalled.Controls.Add(this.installedLayout);
            this.tabInstalled.Location = new System.Drawing.Point(4, 5);
            this.tabInstalled.Name = "tabInstalled";
            this.tabInstalled.Padding = new System.Windows.Forms.Padding(10);
            this.tabInstalled.Size = new System.Drawing.Size(1107, 701);
            this.tabInstalled.TabIndex = 0;
            this.tabInstalled.Text = "Installed Mods";
            // 
            // installedLayout
            // 
            this.installedLayout.ColumnCount = 1;
            this.installedLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.installedLayout.Controls.Add(this.lblInstalledHeader, 0, 0);
            this.installedLayout.Controls.Add(this.flowInstalledFilters, 0, 1);
            this.installedLayout.Controls.Add(this.panelBulkActionsInstalled, 0, 2);
            this.installedLayout.Controls.Add(this.panelInstalledHost, 0, 3);
            this.installedLayout.Controls.Add(this.btnLaunchSelected, 0, 4);
            this.installedLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.installedLayout.Location = new System.Drawing.Point(10, 10);
            this.installedLayout.Name = "installedLayout";
            this.installedLayout.RowCount = 5;
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.installedLayout.Size = new System.Drawing.Size(1087, 681);
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
            this.lblInstalledHeader.Size = new System.Drawing.Size(101, 23);
            this.lblInstalledHeader.TabIndex = 0;
            this.lblInstalledHeader.Text = "Installed Mods";
            // 
            // flowInstalledFilters
            // 
            this.flowInstalledFilters.AutoSize = true;
            this.flowInstalledFilters.BackColor = System.Drawing.Color.Transparent;
            this.flowInstalledFilters.Controls.Add(this.lblInstalledSearch);
            this.flowInstalledFilters.Controls.Add(this.txtInstalledSearch);
            this.flowInstalledFilters.Controls.Add(this.lblInstalledCategory);
            this.flowInstalledFilters.Controls.Add(this.cmbInstalledCategory);
            this.flowInstalledFilters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowInstalledFilters.Location = new System.Drawing.Point(0, 23);
            this.flowInstalledFilters.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.flowInstalledFilters.Name = "flowInstalledFilters";
            this.flowInstalledFilters.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.flowInstalledFilters.Size = new System.Drawing.Size(1087, 29);
            this.flowInstalledFilters.TabIndex = 4;
            this.flowInstalledFilters.WrapContents = false;
            // 
            // lblInstalledSearch
            // 
            this.lblInstalledSearch.AutoSize = true;
            this.lblInstalledSearch.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblInstalledSearch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(110)))));
            this.lblInstalledSearch.Location = new System.Drawing.Point(3, 7);
            this.lblInstalledSearch.Margin = new System.Windows.Forms.Padding(3, 4, 6, 0);
            this.lblInstalledSearch.Name = "lblInstalledSearch";
            this.lblInstalledSearch.Size = new System.Drawing.Size(48, 15);
            this.lblInstalledSearch.TabIndex = 0;
            this.lblInstalledSearch.Text = "Search:";
            this.lblInstalledSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtInstalledSearch
            // 
            this.txtInstalledSearch.BackColor = System.Drawing.Color.White;
            this.txtInstalledSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtInstalledSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtInstalledSearch.Location = new System.Drawing.Point(57, 3);
            this.txtInstalledSearch.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.txtInstalledSearch.MaxLength = 200;
            this.txtInstalledSearch.Name = "txtInstalledSearch";
            this.txtInstalledSearch.Size = new System.Drawing.Size(240, 23);
            this.txtInstalledSearch.TabIndex = 1;
            this.txtInstalledSearch.TextChanged += new System.EventHandler(this.txtInstalledSearch_TextChanged);
            // 
            // lblInstalledCategory
            // 
            this.lblInstalledCategory.AutoSize = true;
            this.lblInstalledCategory.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblInstalledCategory.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(110)))));
            this.lblInstalledCategory.Location = new System.Drawing.Point(315, 7);
            this.lblInstalledCategory.Margin = new System.Windows.Forms.Padding(3, 4, 6, 0);
            this.lblInstalledCategory.Name = "lblInstalledCategory";
            this.lblInstalledCategory.Size = new System.Drawing.Size(60, 15);
            this.lblInstalledCategory.TabIndex = 2;
            this.lblInstalledCategory.Text = "Category:";
            // 
            // cmbInstalledCategory
            // 
            this.cmbInstalledCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbInstalledCategory.FormattingEnabled = true;
            this.cmbInstalledCategory.Items.AddRange(new object[] {
            "All"});
            this.cmbInstalledCategory.Location = new System.Drawing.Point(381, 3);
            this.cmbInstalledCategory.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.cmbInstalledCategory.Name = "cmbInstalledCategory";
            this.cmbInstalledCategory.Size = new System.Drawing.Size(180, 23);
            this.cmbInstalledCategory.TabIndex = 3;
            this.cmbInstalledCategory.SelectedIndexChanged += new System.EventHandler(this.cmbInstalledCategory_SelectedIndexChanged);
            // 
            // panelBulkActionsInstalled
            // 
            this.panelBulkActionsInstalled.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(247)))));
            this.panelBulkActionsInstalled.Controls.Add(this.lblBulkSelectedCountInstalled);
            this.panelBulkActionsInstalled.Controls.Add(this.btnBulkUninstallInstalled);
            this.panelBulkActionsInstalled.Controls.Add(this.btnBulkDeselectAllInstalled);
            this.panelBulkActionsInstalled.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelBulkActionsInstalled.Location = new System.Drawing.Point(0, 52);
            this.panelBulkActionsInstalled.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.panelBulkActionsInstalled.Name = "panelBulkActionsInstalled";
            this.panelBulkActionsInstalled.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.panelBulkActionsInstalled.Size = new System.Drawing.Size(1087, 44);
            this.panelBulkActionsInstalled.TabIndex = 5;
            this.panelBulkActionsInstalled.Visible = false;
            // 
            // lblBulkSelectedCountInstalled
            // 
            this.lblBulkSelectedCountInstalled.AutoSize = true;
            this.lblBulkSelectedCountInstalled.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblBulkSelectedCountInstalled.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(90)))));
            this.lblBulkSelectedCountInstalled.Location = new System.Drawing.Point(12, 11);
            this.lblBulkSelectedCountInstalled.Name = "lblBulkSelectedCountInstalled";
            this.lblBulkSelectedCountInstalled.Size = new System.Drawing.Size(95, 15);
            this.lblBulkSelectedCountInstalled.TabIndex = 0;
            this.lblBulkSelectedCountInstalled.Text = "0 mods selected";
            // 
            // btnBulkUninstallInstalled
            // 
            this.btnBulkUninstallInstalled.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBulkUninstallInstalled.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(94)))));
            this.btnBulkUninstallInstalled.FlatAppearance.BorderSize = 0;
            this.btnBulkUninstallInstalled.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(80)))), ((int)(((byte)(81)))));
            this.btnBulkUninstallInstalled.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBulkUninstallInstalled.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnBulkUninstallInstalled.ForeColor = System.Drawing.Color.White;
            this.btnBulkUninstallInstalled.Location = new System.Drawing.Point(800, 6);
            this.btnBulkUninstallInstalled.Name = "btnBulkUninstallInstalled";
            this.btnBulkUninstallInstalled.Size = new System.Drawing.Size(130, 28);
            this.btnBulkUninstallInstalled.TabIndex = 1;
            this.btnBulkUninstallInstalled.Text = "Uninstall Selected";
            this.btnBulkUninstallInstalled.UseVisualStyleBackColor = false;
            this.btnBulkUninstallInstalled.Click += new System.EventHandler(this.btnBulkUninstallInstalled_Click);
            // 
            // btnBulkDeselectAllInstalled
            // 
            this.btnBulkDeselectAllInstalled.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBulkDeselectAllInstalled.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnBulkDeselectAllInstalled.FlatAppearance.BorderSize = 0;
            this.btnBulkDeselectAllInstalled.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(98)))), ((int)(((byte)(105)))));
            this.btnBulkDeselectAllInstalled.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBulkDeselectAllInstalled.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnBulkDeselectAllInstalled.ForeColor = System.Drawing.Color.White;
            this.btnBulkDeselectAllInstalled.Location = new System.Drawing.Point(936, 6);
            this.btnBulkDeselectAllInstalled.Name = "btnBulkDeselectAllInstalled";
            this.btnBulkDeselectAllInstalled.Size = new System.Drawing.Size(139, 28);
            this.btnBulkDeselectAllInstalled.TabIndex = 2;
            this.btnBulkDeselectAllInstalled.Text = "Deselect All";
            this.btnBulkDeselectAllInstalled.UseVisualStyleBackColor = false;
            this.btnBulkDeselectAllInstalled.Click += new System.EventHandler(this.btnBulkDeselectAllInstalled_Click);
            // 
            // panelInstalledHost
            // 
            this.panelInstalledHost.BackColor = System.Drawing.Color.Transparent;
            this.panelInstalledHost.Controls.Add(this.panelEmptyInstalled);
            this.panelInstalledHost.Controls.Add(this.panelInstalled);
            this.panelInstalledHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelInstalledHost.Location = new System.Drawing.Point(0, 68);
            this.panelInstalledHost.Margin = new System.Windows.Forms.Padding(0);
            this.panelInstalledHost.Name = "panelInstalledHost";
            this.panelInstalledHost.Size = new System.Drawing.Size(1087, 571);
            this.panelInstalledHost.TabIndex = 3;
            // 
            // panelEmptyInstalled
            // 
            this.panelEmptyInstalled.Controls.Add(this.lblEmptyInstalled);
            this.panelEmptyInstalled.Controls.Add(this.btnEmptyInstalledBrowseFeatured);
            this.panelEmptyInstalled.Controls.Add(this.btnEmptyInstalledBrowseStore);
            this.panelEmptyInstalled.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelEmptyInstalled.Location = new System.Drawing.Point(0, 0);
            this.panelEmptyInstalled.Name = "panelEmptyInstalled";
            this.panelEmptyInstalled.Size = new System.Drawing.Size(1087, 571);
            this.panelEmptyInstalled.TabIndex = 2;
            this.panelEmptyInstalled.Visible = false;
            // 
            // lblEmptyInstalled
            // 
            this.lblEmptyInstalled.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblEmptyInstalled.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEmptyInstalled.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(150)))));
            this.lblEmptyInstalled.Location = new System.Drawing.Point(0, 200);
            this.lblEmptyInstalled.Name = "lblEmptyInstalled";
            this.lblEmptyInstalled.Size = new System.Drawing.Size(1087, 30);
            this.lblEmptyInstalled.TabIndex = 0;
            this.lblEmptyInstalled.Text = "No mods installed";
            this.lblEmptyInstalled.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnEmptyInstalledBrowseFeatured
            // 
            this.btnEmptyInstalledBrowseFeatured.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnEmptyInstalledBrowseFeatured.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(155)))), ((int)(((byte)(105)))));
            this.btnEmptyInstalledBrowseFeatured.FlatAppearance.BorderSize = 0;
            this.btnEmptyInstalledBrowseFeatured.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(134)))), ((int)(((byte)(91)))));
            this.btnEmptyInstalledBrowseFeatured.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEmptyInstalledBrowseFeatured.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEmptyInstalledBrowseFeatured.ForeColor = System.Drawing.Color.White;
            this.btnEmptyInstalledBrowseFeatured.Location = new System.Drawing.Point(443, 250);
            this.btnEmptyInstalledBrowseFeatured.Name = "btnEmptyInstalledBrowseFeatured";
            this.btnEmptyInstalledBrowseFeatured.Size = new System.Drawing.Size(200, 36);
            this.btnEmptyInstalledBrowseFeatured.TabIndex = 1;
            this.btnEmptyInstalledBrowseFeatured.Text = "Browse Featured Mods";
            this.btnEmptyInstalledBrowseFeatured.UseVisualStyleBackColor = false;
            this.btnEmptyInstalledBrowseFeatured.Click += new System.EventHandler(this.btnEmptyInstalledBrowseFeatured_Click);
            // 
            // btnEmptyInstalledBrowseStore
            // 
            this.btnEmptyInstalledBrowseStore.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnEmptyInstalledBrowseStore.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(80)))));
            this.btnEmptyInstalledBrowseStore.FlatAppearance.BorderSize = 0;
            this.btnEmptyInstalledBrowseStore.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(100)))));
            this.btnEmptyInstalledBrowseStore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEmptyInstalledBrowseStore.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEmptyInstalledBrowseStore.ForeColor = System.Drawing.Color.White;
            this.btnEmptyInstalledBrowseStore.Location = new System.Drawing.Point(443, 296);
            this.btnEmptyInstalledBrowseStore.Name = "btnEmptyInstalledBrowseStore";
            this.btnEmptyInstalledBrowseStore.Size = new System.Drawing.Size(200, 36);
            this.btnEmptyInstalledBrowseStore.TabIndex = 2;
            this.btnEmptyInstalledBrowseStore.Text = "Browse All Mods";
            this.btnEmptyInstalledBrowseStore.UseVisualStyleBackColor = false;
            this.btnEmptyInstalledBrowseStore.Click += new System.EventHandler(this.btnEmptyInstalledBrowseStore_Click);
            // 
            // panelInstalled
            // 
            this.panelInstalled.AutoScroll = true;
            this.panelInstalled.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(32)))), ((int)(((byte)(45)))));
            this.panelInstalled.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelInstalled.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(212)))), ((int)(((byte)(219)))), ((int)(((byte)(238)))));
            this.panelInstalled.Location = new System.Drawing.Point(0, 0);
            this.panelInstalled.Margin = new System.Windows.Forms.Padding(0);
            this.panelInstalled.Name = "panelInstalled";
            this.panelInstalled.Padding = new System.Windows.Forms.Padding(10);
            this.panelInstalled.Size = new System.Drawing.Size(1087, 571);
            this.panelInstalled.TabIndex = 1;
            // 
            // btnLaunchSelected
            // 
            this.btnLaunchSelected.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(155)))), ((int)(((byte)(105)))));
            this.btnLaunchSelected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLaunchSelected.FlatAppearance.BorderSize = 0;
            this.btnLaunchSelected.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(134)))), ((int)(((byte)(91)))));
            this.btnLaunchSelected.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLaunchSelected.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLaunchSelected.ForeColor = System.Drawing.Color.White;
            this.btnLaunchSelected.Location = new System.Drawing.Point(10, 641);
            this.btnLaunchSelected.Margin = new System.Windows.Forms.Padding(10, 10, 10, 0);
            this.btnLaunchSelected.Name = "btnLaunchSelected";
            this.btnLaunchSelected.Size = new System.Drawing.Size(1067, 40);
            this.btnLaunchSelected.TabIndex = 4;
            this.btnLaunchSelected.Text = "Launch Selected Mods";
            this.btnLaunchSelected.UseVisualStyleBackColor = false;
            this.btnLaunchSelected.Click += new System.EventHandler(this.btnLaunchSelected_Click);
            // 
            // tabStore
            // 
            this.tabStore.Controls.Add(this.storeLayout);
            this.tabStore.Location = new System.Drawing.Point(4, 5);
            this.tabStore.Name = "tabStore";
            this.tabStore.Padding = new System.Windows.Forms.Padding(10);
            this.tabStore.Size = new System.Drawing.Size(1110, 701);
            this.tabStore.TabIndex = 1;
            this.tabStore.Text = "Mod Store";
            // 
            // storeLayout
            // 
            this.storeLayout.ColumnCount = 1;
            this.storeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.storeLayout.Controls.Add(this.lblStoreHeader, 0, 0);
            this.storeLayout.Controls.Add(this.flowStoreFilters, 0, 1);
            this.storeLayout.Controls.Add(this.panelBulkActionsStore, 0, 2);
            this.storeLayout.Controls.Add(this.panelStoreHost, 0, 3);
            this.storeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.storeLayout.Location = new System.Drawing.Point(10, 10);
            this.storeLayout.Name = "storeLayout";
            this.storeLayout.RowCount = 4;
            this.storeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.storeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.storeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.storeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.storeLayout.Size = new System.Drawing.Size(1090, 681);
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
            this.lblStoreHeader.Size = new System.Drawing.Size(75, 23);
            this.lblStoreHeader.TabIndex = 0;
            this.lblStoreHeader.Text = "Mod Store";
            // 
            // flowStoreFilters
            // 
            this.flowStoreFilters.AutoSize = true;
            this.flowStoreFilters.BackColor = System.Drawing.Color.Transparent;
            this.flowStoreFilters.Controls.Add(this.lblStoreSearch);
            this.flowStoreFilters.Controls.Add(this.txtStoreSearch);
            this.flowStoreFilters.Controls.Add(this.lblStoreCategory);
            this.flowStoreFilters.Controls.Add(this.cmbStoreCategory);
            this.flowStoreFilters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowStoreFilters.Location = new System.Drawing.Point(0, 23);
            this.flowStoreFilters.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.flowStoreFilters.Name = "flowStoreFilters";
            this.flowStoreFilters.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.flowStoreFilters.Size = new System.Drawing.Size(1090, 29);
            this.flowStoreFilters.TabIndex = 4;
            this.flowStoreFilters.WrapContents = false;
            // 
            // lblStoreSearch
            // 
            this.lblStoreSearch.AutoSize = true;
            this.lblStoreSearch.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblStoreSearch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(110)))));
            this.lblStoreSearch.Location = new System.Drawing.Point(3, 7);
            this.lblStoreSearch.Margin = new System.Windows.Forms.Padding(3, 4, 6, 0);
            this.lblStoreSearch.Name = "lblStoreSearch";
            this.lblStoreSearch.Size = new System.Drawing.Size(48, 15);
            this.lblStoreSearch.TabIndex = 0;
            this.lblStoreSearch.Text = "Search:";
            // 
            // txtStoreSearch
            // 
            this.txtStoreSearch.BackColor = System.Drawing.Color.White;
            this.txtStoreSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtStoreSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtStoreSearch.Location = new System.Drawing.Point(57, 3);
            this.txtStoreSearch.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.txtStoreSearch.MaxLength = 200;
            this.txtStoreSearch.Name = "txtStoreSearch";
            this.txtStoreSearch.Size = new System.Drawing.Size(240, 23);
            this.txtStoreSearch.TabIndex = 1;
            this.txtStoreSearch.TextChanged += new System.EventHandler(this.txtStoreSearch_TextChanged);
            // 
            // lblStoreCategory
            // 
            this.lblStoreCategory.AutoSize = true;
            this.lblStoreCategory.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblStoreCategory.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(110)))));
            this.lblStoreCategory.Location = new System.Drawing.Point(315, 7);
            this.lblStoreCategory.Margin = new System.Windows.Forms.Padding(3, 4, 6, 0);
            this.lblStoreCategory.Name = "lblStoreCategory";
            this.lblStoreCategory.Size = new System.Drawing.Size(60, 15);
            this.lblStoreCategory.TabIndex = 2;
            this.lblStoreCategory.Text = "Category:";
            // 
            // cmbStoreCategory
            // 
            this.cmbStoreCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStoreCategory.FormattingEnabled = true;
            this.cmbStoreCategory.Items.AddRange(new object[] {
            "All"});
            this.cmbStoreCategory.Location = new System.Drawing.Point(381, 3);
            this.cmbStoreCategory.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.cmbStoreCategory.Name = "cmbStoreCategory";
            this.cmbStoreCategory.Size = new System.Drawing.Size(180, 23);
            this.cmbStoreCategory.TabIndex = 3;
            this.cmbStoreCategory.SelectedIndexChanged += new System.EventHandler(this.cmbStoreCategory_SelectedIndexChanged);
            // 
            // panelBulkActionsStore
            // 
            this.panelBulkActionsStore.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(247)))));
            this.panelBulkActionsStore.Controls.Add(this.lblBulkSelectedCountStore);
            this.panelBulkActionsStore.Controls.Add(this.btnBulkInstallStore);
            this.panelBulkActionsStore.Controls.Add(this.btnBulkDeselectAllStore);
            this.panelBulkActionsStore.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelBulkActionsStore.Location = new System.Drawing.Point(0, 52);
            this.panelBulkActionsStore.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.panelBulkActionsStore.Name = "panelBulkActionsStore";
            this.panelBulkActionsStore.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.panelBulkActionsStore.Size = new System.Drawing.Size(1090, 44);
            this.panelBulkActionsStore.TabIndex = 6;
            this.panelBulkActionsStore.Visible = false;
            // 
            // lblBulkSelectedCountStore
            // 
            this.lblBulkSelectedCountStore.AutoSize = true;
            this.lblBulkSelectedCountStore.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblBulkSelectedCountStore.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(90)))));
            this.lblBulkSelectedCountStore.Location = new System.Drawing.Point(12, 11);
            this.lblBulkSelectedCountStore.Name = "lblBulkSelectedCountStore";
            this.lblBulkSelectedCountStore.Size = new System.Drawing.Size(95, 15);
            this.lblBulkSelectedCountStore.TabIndex = 0;
            this.lblBulkSelectedCountStore.Text = "0 mods selected";
            // 
            // btnBulkInstallStore
            // 
            this.btnBulkInstallStore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBulkInstallStore.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnBulkInstallStore.FlatAppearance.BorderSize = 0;
            this.btnBulkInstallStore.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(170)))));
            this.btnBulkInstallStore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBulkInstallStore.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnBulkInstallStore.ForeColor = System.Drawing.Color.White;
            this.btnBulkInstallStore.Location = new System.Drawing.Point(800, 6);
            this.btnBulkInstallStore.Name = "btnBulkInstallStore";
            this.btnBulkInstallStore.Size = new System.Drawing.Size(130, 28);
            this.btnBulkInstallStore.TabIndex = 1;
            this.btnBulkInstallStore.Text = "Install Selected";
            this.btnBulkInstallStore.UseVisualStyleBackColor = false;
            this.btnBulkInstallStore.Click += new System.EventHandler(this.btnBulkInstallStore_Click);
            // 
            // btnBulkDeselectAllStore
            // 
            this.btnBulkDeselectAllStore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBulkDeselectAllStore.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnBulkDeselectAllStore.FlatAppearance.BorderSize = 0;
            this.btnBulkDeselectAllStore.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(98)))), ((int)(((byte)(105)))));
            this.btnBulkDeselectAllStore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBulkDeselectAllStore.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnBulkDeselectAllStore.ForeColor = System.Drawing.Color.White;
            this.btnBulkDeselectAllStore.Location = new System.Drawing.Point(936, 6);
            this.btnBulkDeselectAllStore.Name = "btnBulkDeselectAllStore";
            this.btnBulkDeselectAllStore.Size = new System.Drawing.Size(139, 28);
            this.btnBulkDeselectAllStore.TabIndex = 2;
            this.btnBulkDeselectAllStore.Text = "Deselect All";
            this.btnBulkDeselectAllStore.UseVisualStyleBackColor = false;
            this.btnBulkDeselectAllStore.Click += new System.EventHandler(this.btnBulkDeselectAllStore_Click);
            // 
            // panelStoreHost
            // 
            this.panelStoreHost.BackColor = System.Drawing.Color.Transparent;
            this.panelStoreHost.Controls.Add(this.panelEmptyStore);
            this.panelStoreHost.Controls.Add(this.panelStore);
            this.panelStoreHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStoreHost.Location = new System.Drawing.Point(0, 68);
            this.panelStoreHost.Margin = new System.Windows.Forms.Padding(0);
            this.panelStoreHost.Name = "panelStoreHost";
            this.panelStoreHost.Size = new System.Drawing.Size(1090, 621);
            this.panelStoreHost.TabIndex = 2;
            // 
            // panelEmptyStore
            // 
            this.panelEmptyStore.Controls.Add(this.lblEmptyStore);
            this.panelEmptyStore.Controls.Add(this.btnEmptyStoreClearFilters);
            this.panelEmptyStore.Controls.Add(this.btnEmptyStoreBrowseFeatured);
            this.panelEmptyStore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelEmptyStore.Location = new System.Drawing.Point(0, 0);
            this.panelEmptyStore.Name = "panelEmptyStore";
            this.panelEmptyStore.Size = new System.Drawing.Size(1090, 621);
            this.panelEmptyStore.TabIndex = 5;
            this.panelEmptyStore.Visible = false;
            // 
            // lblEmptyStore
            // 
            this.lblEmptyStore.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblEmptyStore.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEmptyStore.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(150)))));
            this.lblEmptyStore.Location = new System.Drawing.Point(0, 200);
            this.lblEmptyStore.Name = "lblEmptyStore";
            this.lblEmptyStore.Size = new System.Drawing.Size(1090, 30);
            this.lblEmptyStore.TabIndex = 0;
            this.lblEmptyStore.Text = "No mods match your filters";
            this.lblEmptyStore.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnEmptyStoreClearFilters
            // 
            this.btnEmptyStoreClearFilters.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnEmptyStoreClearFilters.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(155)))), ((int)(((byte)(105)))));
            this.btnEmptyStoreClearFilters.FlatAppearance.BorderSize = 0;
            this.btnEmptyStoreClearFilters.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(134)))), ((int)(((byte)(91)))));
            this.btnEmptyStoreClearFilters.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEmptyStoreClearFilters.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEmptyStoreClearFilters.ForeColor = System.Drawing.Color.White;
            this.btnEmptyStoreClearFilters.Location = new System.Drawing.Point(445, 250);
            this.btnEmptyStoreClearFilters.Name = "btnEmptyStoreClearFilters";
            this.btnEmptyStoreClearFilters.Size = new System.Drawing.Size(200, 36);
            this.btnEmptyStoreClearFilters.TabIndex = 1;
            this.btnEmptyStoreClearFilters.Text = "Clear Filters";
            this.btnEmptyStoreClearFilters.UseVisualStyleBackColor = false;
            this.btnEmptyStoreClearFilters.Click += new System.EventHandler(this.btnEmptyStoreClearFilters_Click);
            // 
            // btnEmptyStoreBrowseFeatured
            // 
            this.btnEmptyStoreBrowseFeatured.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnEmptyStoreBrowseFeatured.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(80)))));
            this.btnEmptyStoreBrowseFeatured.FlatAppearance.BorderSize = 0;
            this.btnEmptyStoreBrowseFeatured.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(100)))));
            this.btnEmptyStoreBrowseFeatured.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEmptyStoreBrowseFeatured.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEmptyStoreBrowseFeatured.ForeColor = System.Drawing.Color.White;
            this.btnEmptyStoreBrowseFeatured.Location = new System.Drawing.Point(445, 296);
            this.btnEmptyStoreBrowseFeatured.Name = "btnEmptyStoreBrowseFeatured";
            this.btnEmptyStoreBrowseFeatured.Size = new System.Drawing.Size(200, 36);
            this.btnEmptyStoreBrowseFeatured.TabIndex = 2;
            this.btnEmptyStoreBrowseFeatured.Text = "Browse Featured Mods";
            this.btnEmptyStoreBrowseFeatured.UseVisualStyleBackColor = false;
            this.btnEmptyStoreBrowseFeatured.Click += new System.EventHandler(this.btnEmptyStoreBrowseFeatured_Click);
            // 
            // panelStore
            // 
            this.panelStore.AutoScroll = true;
            this.panelStore.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(32)))), ((int)(((byte)(45)))));
            this.panelStore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStore.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(212)))), ((int)(((byte)(219)))), ((int)(((byte)(238)))));
            this.panelStore.Location = new System.Drawing.Point(0, 0);
            this.panelStore.Name = "panelStore";
            this.panelStore.Padding = new System.Windows.Forms.Padding(10);
            this.panelStore.Size = new System.Drawing.Size(1090, 621);
            this.panelStore.TabIndex = 0;
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.settingsLayout);
            this.tabSettings.Location = new System.Drawing.Point(4, 5);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabSettings.Size = new System.Drawing.Size(1110, 701);
            this.tabSettings.TabIndex = 2;
            this.tabSettings.Text = "Settings";
            // 
            // settingsLayout
            // 
            this.settingsLayout.ColumnCount = 2;
            this.settingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.settingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.settingsLayout.Controls.Add(this.grpPath, 0, 0);
            this.settingsLayout.Controls.Add(this.grpBepInEx, 0, 1);
            this.settingsLayout.Controls.Add(this.grpFolders, 1, 1);
            this.settingsLayout.Controls.Add(this.grpAppearance, 0, 2);
            this.settingsLayout.Controls.Add(this.grpMods, 0, 3);
            this.settingsLayout.Controls.Add(this.grpData, 1, 3);
            this.settingsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsLayout.Location = new System.Drawing.Point(3, 3);
            this.settingsLayout.Name = "settingsLayout";
            this.settingsLayout.Padding = new System.Windows.Forms.Padding(10);
            this.settingsLayout.RowCount = 4;
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingsLayout.Size = new System.Drawing.Size(1104, 695);
            this.settingsLayout.TabIndex = 0;
            // 
            // grpPath
            // 
            this.settingsLayout.SetColumnSpan(this.grpPath, 2);
            this.grpPath.Controls.Add(this.pathLayout);
            this.grpPath.Controls.Add(this.lblAmongUsPath);
            this.grpPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPath.Location = new System.Drawing.Point(13, 13);
            this.grpPath.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
            this.grpPath.Name = "grpPath";
            this.grpPath.Padding = new System.Windows.Forms.Padding(12);
            this.grpPath.Size = new System.Drawing.Size(1078, 114);
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
            this.pathLayout.Location = new System.Drawing.Point(12, 43);
            this.pathLayout.Margin = new System.Windows.Forms.Padding(0);
            this.pathLayout.Name = "pathLayout";
            this.pathLayout.RowCount = 1;
            this.pathLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pathLayout.Size = new System.Drawing.Size(1054, 35);
            this.pathLayout.TabIndex = 2;
            // 
            // txtAmongUsPath
            // 
            this.txtAmongUsPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAmongUsPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAmongUsPath.Location = new System.Drawing.Point(3, 3);
            this.txtAmongUsPath.Name = "txtAmongUsPath";
            this.txtAmongUsPath.ReadOnly = true;
            this.txtAmongUsPath.Size = new System.Drawing.Size(808, 23);
            this.txtAmongUsPath.TabIndex = 0;
            // 
            // btnBrowsePath
            // 
            this.btnBrowsePath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBrowsePath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowsePath.Location = new System.Drawing.Point(817, 3);
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
            this.btnDetectPath.Location = new System.Drawing.Point(937, 3);
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
            this.lblAmongUsPath.Location = new System.Drawing.Point(12, 28);
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
            this.grpBepInEx.Size = new System.Drawing.Size(529, 170);
            this.grpBepInEx.TabIndex = 1;
            this.grpBepInEx.TabStop = false;
            this.grpBepInEx.Text = "BepInEx";
            // 
            // flowBepInEx
            // 
            this.flowBepInEx.AutoSize = true;
            this.flowBepInEx.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowBepInEx.Controls.Add(this.btnInstallBepInEx);
            this.flowBepInEx.Controls.Add(this.btnOpenBepInExFolder);
            this.flowBepInEx.Controls.Add(this.btnOpenPluginsFolder);
            this.flowBepInEx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowBepInEx.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowBepInEx.Location = new System.Drawing.Point(12, 28);
            this.flowBepInEx.Name = "flowBepInEx";
            this.flowBepInEx.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.flowBepInEx.Size = new System.Drawing.Size(505, 130);
            this.flowBepInEx.TabIndex = 0;
            this.flowBepInEx.WrapContents = false;
            // 
            // btnInstallBepInEx
            // 
            this.btnInstallBepInEx.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInstallBepInEx.Location = new System.Drawing.Point(0, 0);
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
            this.btnOpenBepInExFolder.Location = new System.Drawing.Point(0, 45);
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
            this.btnOpenPluginsFolder.Location = new System.Drawing.Point(0, 90);
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
            this.grpFolders.Location = new System.Drawing.Point(555, 140);
            this.grpFolders.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
            this.grpFolders.Name = "grpFolders";
            this.grpFolders.Padding = new System.Windows.Forms.Padding(12);
            this.grpFolders.Size = new System.Drawing.Size(536, 170);
            this.grpFolders.TabIndex = 2;
            this.grpFolders.TabStop = false;
            this.grpFolders.Text = "Quick Actions";
            // 
            // flowFolders
            // 
            this.flowFolders.AutoSize = true;
            this.flowFolders.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowFolders.Controls.Add(this.btnOpenModsFolder);
            this.flowFolders.Controls.Add(this.btnOpenAmongUsFolder);
            this.flowFolders.Controls.Add(this.btnOpenDataFolder);
            this.flowFolders.Controls.Add(this.btnClearCache);
            this.flowFolders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowFolders.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowFolders.Location = new System.Drawing.Point(12, 28);
            this.flowFolders.Name = "flowFolders";
            this.flowFolders.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.flowFolders.Size = new System.Drawing.Size(512, 130);
            this.flowFolders.TabIndex = 0;
            this.flowFolders.WrapContents = false;
            // 
            // btnOpenModsFolder
            // 
            this.btnOpenModsFolder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOpenModsFolder.Location = new System.Drawing.Point(0, 0);
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
            this.btnOpenAmongUsFolder.Location = new System.Drawing.Point(0, 45);
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
            this.btnOpenDataFolder.Location = new System.Drawing.Point(0, 90);
            this.btnOpenDataFolder.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnOpenDataFolder.Name = "btnOpenDataFolder";
            this.btnOpenDataFolder.Size = new System.Drawing.Size(230, 35);
            this.btnOpenDataFolder.TabIndex = 1;
            this.btnOpenDataFolder.Text = "Open Data Folder";
            this.btnOpenDataFolder.UseVisualStyleBackColor = true;
            this.btnOpenDataFolder.Click += new System.EventHandler(this.btnOpenDataFolder_Click);
            // 
            // btnClearCache
            // 
            this.btnClearCache.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClearCache.Location = new System.Drawing.Point(0, 135);
            this.btnClearCache.Margin = new System.Windows.Forms.Padding(0);
            this.btnClearCache.Name = "btnClearCache";
            this.btnClearCache.Size = new System.Drawing.Size(230, 35);
            this.btnClearCache.TabIndex = 3;
            this.btnClearCache.Text = "Clear Cache";
            this.btnClearCache.UseVisualStyleBackColor = true;
            this.btnClearCache.Click += new System.EventHandler(this.btnClearCache_Click);
            // 
            // grpAppearance
            // 
            this.settingsLayout.SetColumnSpan(this.grpAppearance, 2);
            this.grpAppearance.Controls.Add(this.appearanceLayout);
            this.grpAppearance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAppearance.Location = new System.Drawing.Point(13, 323);
            this.grpAppearance.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
            this.grpAppearance.Name = "grpAppearance";
            this.grpAppearance.Padding = new System.Windows.Forms.Padding(12);
            this.grpAppearance.Size = new System.Drawing.Size(1078, 110);
            this.grpAppearance.TabIndex = 5;
            this.grpAppearance.TabStop = false;
            this.grpAppearance.Text = "Appearance";
            // 
            // appearanceLayout
            // 
            this.appearanceLayout.ColumnCount = 2;
            this.appearanceLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.appearanceLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.appearanceLayout.Controls.Add(this.lblTheme, 0, 0);
            this.appearanceLayout.Controls.Add(this.cmbTheme, 1, 0);
            this.appearanceLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.appearanceLayout.Location = new System.Drawing.Point(12, 28);
            this.appearanceLayout.Name = "appearanceLayout";
            this.appearanceLayout.RowCount = 1;
            this.appearanceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.appearanceLayout.Size = new System.Drawing.Size(1054, 70);
            this.appearanceLayout.TabIndex = 0;
            // 
            // lblTheme
            // 
            this.lblTheme.AutoSize = true;
            this.lblTheme.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblTheme.Location = new System.Drawing.Point(3, 8);
            this.lblTheme.Margin = new System.Windows.Forms.Padding(3, 8, 10, 0);
            this.lblTheme.Name = "lblTheme";
            this.lblTheme.Size = new System.Drawing.Size(49, 15);
            this.lblTheme.TabIndex = 0;
            this.lblTheme.Text = "Theme:";
            // 
            // cmbTheme
            // 
            this.cmbTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTheme.FormattingEnabled = true;
            this.cmbTheme.Items.AddRange(new object[] {
            "Light",
            "Dark"});
            this.cmbTheme.Location = new System.Drawing.Point(65, 4);
            this.cmbTheme.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this.cmbTheme.Name = "cmbTheme";
            this.cmbTheme.Size = new System.Drawing.Size(200, 23);
            this.cmbTheme.TabIndex = 1;
            this.cmbTheme.SelectedIndexChanged += new System.EventHandler(this.cmbTheme_SelectedIndexChanged);
            // 
            // grpMods
            // 
            this.grpMods.Controls.Add(this.flowMods);
            this.grpMods.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMods.Location = new System.Drawing.Point(13, 446);
            this.grpMods.Margin = new System.Windows.Forms.Padding(3, 3, 10, 10);
            this.grpMods.Name = "grpMods";
            this.grpMods.Padding = new System.Windows.Forms.Padding(12);
            this.grpMods.Size = new System.Drawing.Size(529, 229);
            this.grpMods.TabIndex = 3;
            this.grpMods.TabStop = false;
            this.grpMods.Text = "Mod Management";
            // 
            // flowMods
            // 
            this.flowMods.AutoScroll = true;
            this.flowMods.Controls.Add(this.chkAutoUpdateMods);
            this.flowMods.Controls.Add(this.chkShowBetaVersions);
            this.flowMods.Controls.Add(this.btnUpdateAllMods);
            this.flowMods.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowMods.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowMods.Location = new System.Drawing.Point(12, 28);
            this.flowMods.Name = "flowMods";
            this.flowMods.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.flowMods.Size = new System.Drawing.Size(505, 189);
            this.flowMods.TabIndex = 0;
            this.flowMods.WrapContents = false;
            // 
            // chkAutoUpdateMods
            // 
            this.chkAutoUpdateMods.AutoSize = true;
            this.chkAutoUpdateMods.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkAutoUpdateMods.Location = new System.Drawing.Point(0, 0);
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
            this.chkShowBetaVersions.Location = new System.Drawing.Point(0, 24);
            this.chkShowBetaVersions.Margin = new System.Windows.Forms.Padding(0, 0, 0, 5);
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
            this.btnUpdateAllMods.Location = new System.Drawing.Point(0, 48);
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
            this.grpData.Location = new System.Drawing.Point(555, 446);
            this.grpData.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
            this.grpData.Name = "grpData";
            this.grpData.Padding = new System.Windows.Forms.Padding(12);
            this.grpData.Size = new System.Drawing.Size(536, 229);
            this.grpData.TabIndex = 4;
            this.grpData.TabStop = false;
            this.grpData.Text = "Save Data";
            // 
            // flowData
            // 
            this.flowData.Controls.Add(this.btnBackupAmongUsData);
            this.flowData.Controls.Add(this.btnRestoreAmongUsData);
            this.flowData.Controls.Add(this.btnClearBackup);
            this.flowData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowData.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowData.Location = new System.Drawing.Point(12, 28);
            this.flowData.Name = "flowData";
            this.flowData.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.flowData.Size = new System.Drawing.Size(512, 189);
            this.flowData.TabIndex = 0;
            this.flowData.WrapContents = false;
            // 
            // btnBackupAmongUsData
            // 
            this.btnBackupAmongUsData.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBackupAmongUsData.Location = new System.Drawing.Point(0, 0);
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
            this.btnRestoreAmongUsData.Location = new System.Drawing.Point(0, 45);
            this.btnRestoreAmongUsData.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnRestoreAmongUsData.Name = "btnRestoreAmongUsData";
            this.btnRestoreAmongUsData.Size = new System.Drawing.Size(230, 35);
            this.btnRestoreAmongUsData.TabIndex = 1;
            this.btnRestoreAmongUsData.Text = "Restore Among Us Data";
            this.btnRestoreAmongUsData.UseVisualStyleBackColor = true;
            this.btnRestoreAmongUsData.Click += new System.EventHandler(this.btnRestoreAmongUsData_Click);
            // 
            // btnClearBackup
            // 
            this.btnClearBackup.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClearBackup.Location = new System.Drawing.Point(0, 90);
            this.btnClearBackup.Margin = new System.Windows.Forms.Padding(0);
            this.btnClearBackup.Name = "btnClearBackup";
            this.btnClearBackup.Size = new System.Drawing.Size(230, 35);
            this.btnClearBackup.TabIndex = 2;
            this.btnClearBackup.Text = "Clear Backup";
            this.btnClearBackup.UseVisualStyleBackColor = true;
            this.btnClearBackup.Click += new System.EventHandler(this.btnClearBackup_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(1335, 764);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.leftSidebar);
            this.Controls.Add(this.headerStrip);
            this.Controls.Add(this.statusStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bean Mod Manager";
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.headerStrip.ResumeLayout(false);
            this.headerStrip.PerformLayout();
            this.leftSidebar.ResumeLayout(false);
            this.sidebarButtons.ResumeLayout(false);
            this.sidebarStats.ResumeLayout(false);
            this.sidebarStats.PerformLayout();
            this.sidebarHeader.ResumeLayout(false);
            this.sidebarHeader.PerformLayout();
            this.tabControl.ResumeLayout(false);
            this.tabInstalled.ResumeLayout(false);
            this.installedLayout.ResumeLayout(false);
            this.installedLayout.PerformLayout();
            this.flowInstalledFilters.ResumeLayout(false);
            this.flowInstalledFilters.PerformLayout();
            this.panelBulkActionsInstalled.ResumeLayout(false);
            this.panelBulkActionsInstalled.PerformLayout();
            this.panelInstalledHost.ResumeLayout(false);
            this.panelEmptyInstalled.ResumeLayout(false);
            this.tabStore.ResumeLayout(false);
            this.storeLayout.ResumeLayout(false);
            this.storeLayout.PerformLayout();
            this.flowStoreFilters.ResumeLayout(false);
            this.flowStoreFilters.PerformLayout();
            this.panelBulkActionsStore.ResumeLayout(false);
            this.panelBulkActionsStore.PerformLayout();
            this.panelStoreHost.ResumeLayout(false);
            this.panelEmptyStore.ResumeLayout(false);
            this.tabSettings.ResumeLayout(false);
            this.settingsLayout.ResumeLayout(false);
            this.settingsLayout.PerformLayout();
            this.grpPath.ResumeLayout(false);
            this.grpPath.PerformLayout();
            this.pathLayout.ResumeLayout(false);
            this.pathLayout.PerformLayout();
            this.grpBepInEx.ResumeLayout(false);
            this.grpBepInEx.PerformLayout();
            this.flowBepInEx.ResumeLayout(false);
            this.grpFolders.ResumeLayout(false);
            this.grpFolders.PerformLayout();
            this.flowFolders.ResumeLayout(false);
            this.grpAppearance.ResumeLayout(false);
            this.appearanceLayout.ResumeLayout(false);
            this.appearanceLayout.PerformLayout();
            this.grpMods.ResumeLayout(false);
            this.flowMods.ResumeLayout(false);
            this.flowMods.PerformLayout();
            this.grpData.ResumeLayout(false);
            this.flowData.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private BeanModManager.Controls.ThemedTabControl tabControl;
        private System.Windows.Forms.TabPage tabInstalled;
        private System.Windows.Forms.TabPage tabStore;
        private System.Windows.Forms.TabPage tabSettings;
        private System.Windows.Forms.Panel headerStrip;
        private System.Windows.Forms.Label lblHeaderInfo;
        private System.Windows.Forms.Panel leftSidebar;
        private System.Windows.Forms.Panel sidebarHeader;
        private System.Windows.Forms.Label lblSidebarTitle;
        private System.Windows.Forms.Panel sidebarStats;
        private System.Windows.Forms.Label lblInstalledCount;
        private System.Windows.Forms.Label lblPendingUpdates;
        private System.Windows.Forms.Panel sidebarDivider;
        private System.Windows.Forms.Panel sidebarBorder;
        private System.Windows.Forms.Panel sidebarButtons;
        private System.Windows.Forms.Button btnSidebarInstalled;
        private System.Windows.Forms.Button btnSidebarStore;
        private System.Windows.Forms.Button btnSidebarSettings;
        private System.Windows.Forms.TableLayoutPanel installedLayout;
        private System.Windows.Forms.Label lblInstalledHeader;
        private BeanModManager.Controls.ThemedFlowLayoutPanel panelInstalled;
        private System.Windows.Forms.Panel panelInstalledHost;
        private BeanModManager.Controls.ThemedFlowLayoutPanel panelStore;
        private System.Windows.Forms.Panel panelStoreHost;
        private System.Windows.Forms.Button btnLaunchSelected;
        private System.Windows.Forms.Button btnSidebarLaunchVanilla;
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
        private System.Windows.Forms.Button btnClearCache;
        private System.Windows.Forms.GroupBox grpMods;
        private System.Windows.Forms.FlowLayoutPanel flowMods;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.Panel panelEmptyInstalled;
        private System.Windows.Forms.Label lblEmptyInstalled;
        private System.Windows.Forms.Button btnEmptyInstalledBrowseFeatured;
        private System.Windows.Forms.Button btnEmptyInstalledBrowseStore;
        private System.Windows.Forms.Panel panelEmptyStore;
        private System.Windows.Forms.Label lblEmptyStore;
        private System.Windows.Forms.Button btnEmptyStoreClearFilters;
        private System.Windows.Forms.Button btnEmptyStoreBrowseFeatured;
        private System.Windows.Forms.CheckBox chkAutoUpdateMods;
        private System.Windows.Forms.CheckBox chkShowBetaVersions;
        private System.Windows.Forms.Button btnUpdateAllMods;
        private System.Windows.Forms.GroupBox grpData;
        private System.Windows.Forms.FlowLayoutPanel flowData;
        private System.Windows.Forms.Button btnBackupAmongUsData;
        private System.Windows.Forms.Button btnRestoreAmongUsData;
        private System.Windows.Forms.Button btnClearBackup;
        private System.Windows.Forms.FlowLayoutPanel flowInstalledFilters;
        private System.Windows.Forms.Label lblInstalledSearch;
        private System.Windows.Forms.TextBox txtInstalledSearch;
        private System.Windows.Forms.Label lblInstalledCategory;
        private System.Windows.Forms.ComboBox cmbInstalledCategory;
        private System.Windows.Forms.FlowLayoutPanel flowStoreFilters;
        private System.Windows.Forms.Label lblStoreSearch;
        private System.Windows.Forms.TextBox txtStoreSearch;
        private System.Windows.Forms.Label lblStoreCategory;
        private System.Windows.Forms.ComboBox cmbStoreCategory;
        private System.Windows.Forms.Panel panelBulkActionsInstalled;
        private System.Windows.Forms.Label lblBulkSelectedCountInstalled;
        private System.Windows.Forms.Button btnBulkUninstallInstalled;
        private System.Windows.Forms.Button btnBulkDeselectAllInstalled;
        private System.Windows.Forms.Panel panelBulkActionsStore;
        private System.Windows.Forms.Label lblBulkSelectedCountStore;
        private System.Windows.Forms.Button btnBulkInstallStore;
        private System.Windows.Forms.Button btnBulkDeselectAllStore;
        private System.Windows.Forms.GroupBox grpAppearance;
        private System.Windows.Forms.TableLayoutPanel appearanceLayout;
        private System.Windows.Forms.Label lblTheme;
        private System.Windows.Forms.ComboBox cmbTheme;
    }
}

