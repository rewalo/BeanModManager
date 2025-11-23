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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabInstalled = new System.Windows.Forms.TabPage();
            this.installedLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblInstalledHeader = new System.Windows.Forms.Label();
            this.flowInstalledFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.lblInstalledSearch = new System.Windows.Forms.Label();
            this.txtInstalledSearch = new System.Windows.Forms.TextBox();
            this.lblInstalledCategory = new System.Windows.Forms.Label();
            this.cmbInstalledCategory = new System.Windows.Forms.ComboBox();
            this.panelInstalledHost = new System.Windows.Forms.Panel();
            this.lblEmptyInstalled = new System.Windows.Forms.Label();
            this.panelInstalled = new System.Windows.Forms.FlowLayoutPanel();
            this.btnLaunchSelected = new System.Windows.Forms.Button();
            this.btnLaunchVanilla = new System.Windows.Forms.Button();
            this.tabStore = new System.Windows.Forms.TabPage();
            this.storeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblStoreHeader = new System.Windows.Forms.Label();
            this.flowStoreFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.lblStoreSearch = new System.Windows.Forms.Label();
            this.txtStoreSearch = new System.Windows.Forms.TextBox();
            this.lblStoreCategory = new System.Windows.Forms.Label();
            this.cmbStoreCategory = new System.Windows.Forms.ComboBox();
            this.panelStoreHost = new System.Windows.Forms.Panel();
            this.lblEmptyStore = new System.Windows.Forms.Label();
            this.panelStore = new System.Windows.Forms.FlowLayoutPanel();
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
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.tabControl.SuspendLayout();
            this.tabInstalled.SuspendLayout();
            this.installedLayout.SuspendLayout();
            this.flowInstalledFilters.SuspendLayout();
            this.panelInstalledHost.SuspendLayout();
            this.tabStore.SuspendLayout();
            this.storeLayout.SuspendLayout();
            this.flowStoreFilters.SuspendLayout();
            this.panelStoreHost.SuspendLayout();
            this.tabSettings.SuspendLayout();
            this.settingsLayout.SuspendLayout();
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
            this.tabControl.ItemSize = new System.Drawing.Size(120, 30);
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(12, 4);
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1115, 730);
            this.tabControl.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControl.TabIndex = 0;
            // 
            // tabInstalled
            // 
            this.tabInstalled.Controls.Add(this.installedLayout);
            this.tabInstalled.Location = new System.Drawing.Point(4, 34);
            this.tabInstalled.Name = "tabInstalled";
            this.tabInstalled.Padding = new System.Windows.Forms.Padding(10);
            this.tabInstalled.Size = new System.Drawing.Size(1107, 692);
            this.tabInstalled.TabIndex = 0;
            this.tabInstalled.Text = "Installed Mods";
            this.tabInstalled.UseVisualStyleBackColor = true;
            // 
            // installedLayout
            // 
            this.installedLayout.ColumnCount = 1;
            this.installedLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.installedLayout.Controls.Add(this.lblInstalledHeader, 0, 0);
            this.installedLayout.Controls.Add(this.flowInstalledFilters, 0, 1);
            this.installedLayout.Controls.Add(this.panelInstalledHost, 0, 2);
            this.installedLayout.Controls.Add(this.btnLaunchSelected, 0, 3);
            this.installedLayout.Controls.Add(this.btnLaunchVanilla, 0, 4);
            this.installedLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.installedLayout.Location = new System.Drawing.Point(10, 10);
            this.installedLayout.Name = "installedLayout";
            this.installedLayout.RowCount = 5;
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.installedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.installedLayout.Size = new System.Drawing.Size(1087, 672);
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
            this.flowInstalledFilters.BackColor = System.Drawing.Color.WhiteSmoke;
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
            // panelInstalledHost
            // 
            this.panelInstalledHost.BackColor = System.Drawing.Color.Transparent;
            this.panelInstalledHost.Controls.Add(this.lblEmptyInstalled);
            this.panelInstalledHost.Controls.Add(this.panelInstalled);
            this.panelInstalledHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelInstalledHost.Location = new System.Drawing.Point(0, 60);
            this.panelInstalledHost.Margin = new System.Windows.Forms.Padding(0);
            this.panelInstalledHost.Name = "panelInstalledHost";
            this.panelInstalledHost.Size = new System.Drawing.Size(1087, 512);
            this.panelInstalledHost.TabIndex = 3;
            // 
            // lblEmptyInstalled
            // 
            this.lblEmptyInstalled.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmptyInstalled.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEmptyInstalled.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(150)))));
            this.lblEmptyInstalled.Location = new System.Drawing.Point(0, 0);
            this.lblEmptyInstalled.Name = "lblEmptyInstalled";
            this.lblEmptyInstalled.Size = new System.Drawing.Size(1087, 512);
            this.lblEmptyInstalled.TabIndex = 2;
            this.lblEmptyInstalled.Text = "😢 No mods installed";
            this.lblEmptyInstalled.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEmptyInstalled.Visible = false;
            // 
            // panelInstalled
            // 
            this.panelInstalled.AutoScroll = true;
            this.panelInstalled.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelInstalled.Location = new System.Drawing.Point(0, 0);
            this.panelInstalled.Margin = new System.Windows.Forms.Padding(0);
            this.panelInstalled.Name = "panelInstalled";
            this.panelInstalled.Padding = new System.Windows.Forms.Padding(10);
            this.panelInstalled.Size = new System.Drawing.Size(1087, 512);
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
            this.btnLaunchSelected.Location = new System.Drawing.Point(10, 582);
            this.btnLaunchSelected.Margin = new System.Windows.Forms.Padding(10, 10, 10, 0);
            this.btnLaunchSelected.Name = "btnLaunchSelected";
            this.btnLaunchSelected.Size = new System.Drawing.Size(1067, 40);
            this.btnLaunchSelected.TabIndex = 4;
            this.btnLaunchSelected.Text = "Launch Selected Mods";
            this.btnLaunchSelected.UseVisualStyleBackColor = false;
            this.btnLaunchSelected.Click += new System.EventHandler(this.btnLaunchSelected_Click);
            // 
            // btnLaunchVanilla
            // 
            this.btnLaunchVanilla.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(122)))), ((int)(((byte)(189)))));
            this.btnLaunchVanilla.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLaunchVanilla.FlatAppearance.BorderSize = 0;
            this.btnLaunchVanilla.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(105)))), ((int)(((byte)(166)))));
            this.btnLaunchVanilla.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLaunchVanilla.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLaunchVanilla.ForeColor = System.Drawing.Color.White;
            this.btnLaunchVanilla.Location = new System.Drawing.Point(10, 632);
            this.btnLaunchVanilla.Margin = new System.Windows.Forms.Padding(10, 10, 10, 0);
            this.btnLaunchVanilla.Name = "btnLaunchVanilla";
            this.btnLaunchVanilla.Size = new System.Drawing.Size(1067, 40);
            this.btnLaunchVanilla.TabIndex = 0;
            this.btnLaunchVanilla.Text = "Launch Vanilla Among Us";
            this.btnLaunchVanilla.UseVisualStyleBackColor = false;
            this.btnLaunchVanilla.Click += new System.EventHandler(this.btnLaunchVanilla_Click);
            // 
            // tabStore
            // 
            this.tabStore.Controls.Add(this.storeLayout);
            this.tabStore.Location = new System.Drawing.Point(4, 34);
            this.tabStore.Name = "tabStore";
            this.tabStore.Padding = new System.Windows.Forms.Padding(10);
            this.tabStore.Size = new System.Drawing.Size(1107, 692);
            this.tabStore.TabIndex = 1;
            this.tabStore.Text = "Mod Store";
            this.tabStore.UseVisualStyleBackColor = true;
            // 
            // storeLayout
            // 
            this.storeLayout.ColumnCount = 1;
            this.storeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.storeLayout.Controls.Add(this.lblStoreHeader, 0, 0);
            this.storeLayout.Controls.Add(this.flowStoreFilters, 0, 1);
            this.storeLayout.Controls.Add(this.panelStoreHost, 0, 2);
            this.storeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.storeLayout.Location = new System.Drawing.Point(10, 10);
            this.storeLayout.Name = "storeLayout";
            this.storeLayout.RowCount = 3;
            this.storeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.storeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.storeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.storeLayout.Size = new System.Drawing.Size(1087, 672);
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
            this.flowStoreFilters.BackColor = System.Drawing.Color.WhiteSmoke;
            this.flowStoreFilters.Controls.Add(this.lblStoreSearch);
            this.flowStoreFilters.Controls.Add(this.txtStoreSearch);
            this.flowStoreFilters.Controls.Add(this.lblStoreCategory);
            this.flowStoreFilters.Controls.Add(this.cmbStoreCategory);
            this.flowStoreFilters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowStoreFilters.Location = new System.Drawing.Point(0, 23);
            this.flowStoreFilters.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.flowStoreFilters.Name = "flowStoreFilters";
            this.flowStoreFilters.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.flowStoreFilters.Size = new System.Drawing.Size(1087, 29);
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
            // panelStoreHost
            // 
            this.panelStoreHost.BackColor = System.Drawing.Color.Transparent;
            this.panelStoreHost.Controls.Add(this.lblEmptyStore);
            this.panelStoreHost.Controls.Add(this.panelStore);
            this.panelStoreHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStoreHost.Location = new System.Drawing.Point(0, 60);
            this.panelStoreHost.Margin = new System.Windows.Forms.Padding(0);
            this.panelStoreHost.Name = "panelStoreHost";
            this.panelStoreHost.Size = new System.Drawing.Size(1087, 612);
            this.panelStoreHost.TabIndex = 2;
            // 
            // lblEmptyStore
            // 
            this.lblEmptyStore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmptyStore.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEmptyStore.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(150)))));
            this.lblEmptyStore.Location = new System.Drawing.Point(0, 0);
            this.lblEmptyStore.Name = "lblEmptyStore";
            this.lblEmptyStore.Size = new System.Drawing.Size(1087, 612);
            this.lblEmptyStore.TabIndex = 5;
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
            this.panelStore.Size = new System.Drawing.Size(1087, 612);
            this.panelStore.TabIndex = 0;
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.settingsLayout);
            this.tabSettings.Location = new System.Drawing.Point(4, 34);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabSettings.Size = new System.Drawing.Size(1107, 692);
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
            this.settingsLayout.Size = new System.Drawing.Size(1101, 686);
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
            this.grpPath.Size = new System.Drawing.Size(1075, 114);
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
            this.pathLayout.Size = new System.Drawing.Size(1051, 35);
            this.pathLayout.TabIndex = 2;
            // 
            // txtAmongUsPath
            // 
            this.txtAmongUsPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAmongUsPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAmongUsPath.Location = new System.Drawing.Point(3, 3);
            this.txtAmongUsPath.Name = "txtAmongUsPath";
            this.txtAmongUsPath.ReadOnly = true;
            this.txtAmongUsPath.Size = new System.Drawing.Size(805, 23);
            this.txtAmongUsPath.TabIndex = 0;
            // 
            // btnBrowsePath
            // 
            this.btnBrowsePath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBrowsePath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowsePath.Location = new System.Drawing.Point(814, 3);
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
            this.btnDetectPath.Location = new System.Drawing.Point(934, 3);
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
            this.grpBepInEx.Size = new System.Drawing.Size(527, 170);
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
            this.flowBepInEx.Size = new System.Drawing.Size(503, 130);
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
            this.grpFolders.Location = new System.Drawing.Point(553, 140);
            this.grpFolders.Margin = new System.Windows.Forms.Padding(3, 3, 13, 10);
            this.grpFolders.Name = "grpFolders";
            this.grpFolders.Padding = new System.Windows.Forms.Padding(12);
            this.grpFolders.Size = new System.Drawing.Size(525, 170);
            this.grpFolders.TabIndex = 2;
            this.grpFolders.TabStop = false;
            this.grpFolders.Text = "Quick Folders";
            // 
            // flowFolders
            // 
            this.flowFolders.AutoSize = true;
            this.flowFolders.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowFolders.Controls.Add(this.btnOpenModsFolder);
            this.flowFolders.Controls.Add(this.btnOpenAmongUsFolder);
            this.flowFolders.Controls.Add(this.btnOpenDataFolder);
            this.flowFolders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowFolders.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowFolders.Location = new System.Drawing.Point(12, 28);
            this.flowFolders.Name = "flowFolders";
            this.flowFolders.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.flowFolders.Size = new System.Drawing.Size(501, 130);
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
            this.grpMods.Location = new System.Drawing.Point(13, 323);
            this.grpMods.Margin = new System.Windows.Forms.Padding(3, 3, 10, 10);
            this.grpMods.Name = "grpMods";
            this.grpMods.Padding = new System.Windows.Forms.Padding(12);
            this.grpMods.Size = new System.Drawing.Size(527, 343);
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
            this.flowMods.Size = new System.Drawing.Size(503, 303);
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
            this.grpData.Location = new System.Drawing.Point(553, 323);
            this.grpData.Margin = new System.Windows.Forms.Padding(3, 3, 13, 10);
            this.grpData.Name = "grpData";
            this.grpData.Padding = new System.Windows.Forms.Padding(12);
            this.grpData.Size = new System.Drawing.Size(525, 343);
            this.grpData.TabIndex = 4;
            this.grpData.TabStop = false;
            this.grpData.Text = "Save Data";
            // 
            // flowData
            // 
            this.flowData.AutoScroll = true;
            this.flowData.Controls.Add(this.btnBackupAmongUsData);
            this.flowData.Controls.Add(this.btnRestoreAmongUsData);
            this.flowData.Controls.Add(this.btnClearBackup);
            this.flowData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowData.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowData.Location = new System.Drawing.Point(12, 28);
            this.flowData.Name = "flowData";
            this.flowData.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.flowData.Size = new System.Drawing.Size(501, 303);
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
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.progressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 730);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1115, 22);
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
            this.ClientSize = new System.Drawing.Size(1115, 752);
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
            this.installedLayout.ResumeLayout(false);
            this.installedLayout.PerformLayout();
            this.flowInstalledFilters.ResumeLayout(false);
            this.flowInstalledFilters.PerformLayout();
            this.panelInstalledHost.ResumeLayout(false);
            this.tabStore.ResumeLayout(false);
            this.storeLayout.ResumeLayout(false);
            this.storeLayout.PerformLayout();
            this.flowStoreFilters.ResumeLayout(false);
            this.flowStoreFilters.PerformLayout();
            this.panelStoreHost.ResumeLayout(false);
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
            this.grpMods.ResumeLayout(false);
            this.flowMods.ResumeLayout(false);
            this.flowMods.PerformLayout();
            this.grpData.ResumeLayout(false);
            this.flowData.ResumeLayout(false);
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
        private System.Windows.Forms.Button btnLaunchSelected;
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
    }
}

