using BeanModManager.Helpers;
using BeanModManager.Services;
using BeanModManager.Themes;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeanModManager.Dialogs
{
    public class EpicLoginDialog : Form
    {
        private readonly EpicApiService _epicService;
        private TextBox _txtAuthCode;
        private Button _btnLogin;
        private Button _btnOpenBrowser;
        private Button _btnLogout;
        private Label _lblStatus;
        private Label _lblDescription;
        private Panel _pnlLoggedIn;
        private Panel _pnlLoginForm;
        private bool _isLoggedIn = false;
        private Timer _clipboardMonitor;
        private string _lastClipboardText = "";

        public bool IsLoggedIn => _isLoggedIn;
        public bool LoginSuccessful { get; private set; }

        public EpicLoginDialog()
        {
            _epicService = new EpicApiService();
            InitializeComponent();
            ApplyTheme();
            CheckLoginStatus();
            StartClipboardMonitoring();
            this.HandleCreated += EpicLoginDialog_HandleCreated;
            this.FormClosing += EpicLoginDialog_FormClosing;
        }

        private void EpicLoginDialog_HandleCreated(object sender, EventArgs e)
        {
            ApplyDarkMode();
            this.BeginInvoke(new Action(() =>
            {
                ApplyTheme();
                this.Invalidate(true);
            }));
        }

        private void EpicLoginDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopClipboardMonitoring();
        }

        private void StartClipboardMonitoring()
        {
            _clipboardMonitor = new Timer();
            _clipboardMonitor.Interval = 500; // Check every 500ms
            _clipboardMonitor.Tick += ClipboardMonitor_Tick;
            _clipboardMonitor.Start();
        }

        private void StopClipboardMonitoring()
        {
            if (_clipboardMonitor != null)
            {
                _clipboardMonitor.Stop();
                _clipboardMonitor.Dispose();
                _clipboardMonitor = null;
            }
        }

        private void ClipboardMonitor_Tick(object sender, EventArgs e)
        {
            // Only monitor if we're not logged in
            if (_isLoggedIn || _txtAuthCode == null)
                return;

            try
            {
                if (Clipboard.ContainsText())
                {
                    var clipboardText = Clipboard.GetText();
                    
                    // Skip if it's the same as last time
                    if (clipboardText == _lastClipboardText)
                        return;

                    _lastClipboardText = clipboardText;

                    // Try to extract authorization code from JSON
                    string authCode = null;

                    // Check if clipboard contains JSON with authorizationCode
                    if (clipboardText.Contains("authorizationCode"))
                    {
                        // Try to parse as JSON and extract the code
                        var jsonMatch = Regex.Match(clipboardText, @"""authorizationCode""\s*:\s*""([^""]+)""");
                        if (jsonMatch.Success)
                        {
                            authCode = jsonMatch.Groups[1].Value;
                        }
                        else
                        {
                            // Try alternative pattern
                            var altMatch = Regex.Match(clipboardText, @"authorizationCode[""']?\s*:\s*[""']([^""']+)");
                            if (altMatch.Success)
                            {
                                authCode = altMatch.Groups[1].Value;
                            }
                        }
                    }
                    // Check if clipboard is just the code itself (32 character hex string)
                    else if (Regex.IsMatch(clipboardText.Trim(), @"^[a-fA-F0-9]{32}$"))
                    {
                        authCode = clipboardText.Trim();
                    }

                    // If we found a code and the text box is empty, auto-fill it
                    if (!string.IsNullOrEmpty(authCode) && string.IsNullOrWhiteSpace(_txtAuthCode.Text))
                    {
                        _txtAuthCode.Text = authCode;
                        _lblStatus.Text = "Authorization code detected! Click Login to continue.";
                        _lblStatus.ForeColor = ThemeManager.Current.SuccessButtonColor;
                    }
                }
            }
            catch
            {
                // Ignore clipboard errors (e.g., if another app is using clipboard)
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Epic Games Login";
            this.Size = new Size(550, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;

            var palette = ThemeManager.Current;

            var lblTitle = new Label
            {
                Text = "Epic Games Login",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            this.Controls.Add(lblTitle);

            _lblDescription = new Label
            {
                Text = "Login to launch the Epic Games version of Among Us.",
                Font = new Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new Size(510, 40),
                Location = new Point(20, 55)
            };
            this.Controls.Add(_lblDescription);

            // Logged in panel
            _pnlLoggedIn = new Panel
            {
                Location = new Point(20, 110),
                Size = new Size(510, 120),
                Visible = false
            };

            var lblLoggedIn = new Label
            {
                Text = "Logged In",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            _pnlLoggedIn.Controls.Add(lblLoggedIn);

            var lblSessionActive = new Label
            {
                Text = "Your session is active",
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(10, 35)
            };
            _pnlLoggedIn.Controls.Add(lblSessionActive);

            _btnLogout = new Button
            {
                Text = "Logout",
                Size = new Size(100, 30),
                Location = new Point(400, 10),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = palette.SecondaryButtonColor,
                ForeColor = palette.SecondaryButtonTextColor
            };
            _btnLogout.FlatAppearance.BorderSize = 0;
            _btnLogout.Click += BtnLogout_Click;
            _pnlLoggedIn.Controls.Add(_btnLogout);

            this.Controls.Add(_pnlLoggedIn);

            // Login form panel
            _pnlLoginForm = new Panel
            {
                Location = new Point(20, 110),
                Size = new Size(510, 280),
                Visible = true
            };

            var pnlManual = new Panel
            {
                Location = new Point(0, 10),
                Size = new Size(510, 250),
                Visible = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlManual.Tag = "manualPanel";

            var lblStep1 = new Label
            {
                Text = "Step 1: Open Epic Games login page",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 15)
            };
            pnlManual.Controls.Add(lblStep1);

            _btnOpenBrowser = new Button
            {
                Text = "Open Login Page",
                Size = new Size(490, 35),
                Location = new Point(10, 35),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = palette.PrimaryButtonColor,
                ForeColor = palette.PrimaryButtonTextColor,
                Font = new Font("Segoe UI", 9F)
            };
            _btnOpenBrowser.FlatAppearance.BorderSize = 0;
            _btnOpenBrowser.Click += BtnOpenBrowser_Click;
            pnlManual.Controls.Add(_btnOpenBrowser);

            var lblStep2 = new Label
            {
                Text = "Step 2: After logging in, copy the JSON from the page",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 85)
            };
            pnlManual.Controls.Add(lblStep2);

            var lblHint = new Label
            {
                Text = "The authorization code will be detected automatically when you copy the JSON.",
                Font = new Font("Segoe UI", 8F),
                ForeColor = palette.SecondaryTextColor,
                AutoSize = false,
                Size = new Size(490, 30),
                Location = new Point(10, 110)
            };
            pnlManual.Controls.Add(lblHint);

            var lblStep3 = new Label
            {
                Text = "Step 3: Enter or paste the authorization code",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 145)
            };
            pnlManual.Controls.Add(lblStep3);

            _txtAuthCode = new TextBox
            {
                Size = new Size(380, 30),
                Location = new Point(10, 170)
            };
            // Set placeholder text if available (requires .NET 4.7.2+)
            try
            {
                _txtAuthCode.GetType().GetProperty("PlaceholderText")?.SetValue(_txtAuthCode, "Authorization code will be auto-filled");
            }
            catch { }
            _txtAuthCode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(_txtAuthCode.Text))
                {
                    BtnLogin_Click(s, e);
                }
            };
            pnlManual.Controls.Add(_txtAuthCode);

            var btnPaste = new Button
            {
                Text = "Paste",
                Size = new Size(100, 30),
                Location = new Point(400, 170),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = palette.SecondaryButtonColor,
                ForeColor = palette.SecondaryButtonTextColor
            };
            btnPaste.FlatAppearance.BorderSize = 0;
            btnPaste.Click += BtnPaste_Click;
            pnlManual.Controls.Add(btnPaste);

            _btnLogin = new Button
            {
                Text = "Login",
                Size = new Size(490, 40),
                Location = new Point(10, 210),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = palette.PrimaryButtonColor,
                ForeColor = palette.PrimaryButtonTextColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Click += BtnLogin_Click;
            pnlManual.Controls.Add(_btnLogin);

            _pnlLoginForm.Controls.Add(pnlManual);

            this.Controls.Add(_pnlLoginForm);

            _lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8F),
                AutoSize = true,
                Location = new Point(20, 400),
                ForeColor = palette.SuccessButtonColor
            };
            this.Controls.Add(_lblStatus);

            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10, 10, 10, 10)
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var btnClose = new Button
            {
                Text = "Close",
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 5, 5, 5),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = palette.SecondaryButtonColor,
                ForeColor = palette.SecondaryButtonTextColor
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };
            buttonPanel.Controls.Add(new Panel(), 0, 0);
            buttonPanel.Controls.Add(btnClose, 1, 0);
            this.Controls.Add(buttonPanel);

            this.AcceptButton = _btnLogin;
            this.CancelButton = btnClose;

            this.ResumeLayout(true);
            this.PerformLayout();
        }

        private async void CheckLoginStatus()
        {
            try
            {
                var sessionJson = CredentialHelper.LoadEpicSession();
                if (!string.IsNullOrEmpty(sessionJson))
                {
                    var session = JsonHelper.Deserialize<EpicSession>(sessionJson);
                    if (session != null && !string.IsNullOrEmpty(session.RefreshToken))
                    {
                        // Try to refresh to verify it's still valid
                        try
                        {
                            var refreshed = await _epicService.RefreshSessionAsync(session.RefreshToken);
                            SaveSession(refreshed);
                            _isLoggedIn = true;
                            UpdateUI();
                            return;
                        }
                        catch
                        {
                            // Session invalid, clear it
                            CredentialHelper.ClearEpicSession();
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            _isLoggedIn = false;
            UpdateUI();
        }

        private void UpdateUI()
        {
            _pnlLoggedIn.Visible = _isLoggedIn;
            _pnlLoginForm.Visible = !_isLoggedIn;

            if (_isLoggedIn)
            {
                _lblDescription.Text = "Your Epic Games account is connected.";
                _lblStatus.Text = "Logged in successfully";
                _lblStatus.ForeColor = ThemeManager.Current.SuccessButtonColor;
            }
            else
            {
                _lblDescription.Text = "Login to launch the Epic Games version of Among Us.";
                _lblStatus.Text = "";
            }
        }


        private void BtnOpenBrowser_Click(object sender, EventArgs e)
        {
            try
            {
                var authUrl = EpicApiService.GetAuthUrl();
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Error opening browser: {ex.Message}";
                _lblStatus.ForeColor = ThemeManager.Current.DangerButtonColor;
            }
        }

        private void BtnPaste_Click(object sender, EventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var clipboardText = Clipboard.GetText();
                    string authCode = null;

                    // Try to extract authorization code from JSON
                    if (clipboardText.Contains("authorizationCode"))
                    {
                        var jsonMatch = Regex.Match(clipboardText, @"""authorizationCode""\s*:\s*""([^""]+)""");
                        if (jsonMatch.Success)
                        {
                            authCode = jsonMatch.Groups[1].Value;
                        }
                        else
                        {
                            var altMatch = Regex.Match(clipboardText, @"authorizationCode[""']?\s*:\s*[""']([^""']+)");
                            if (altMatch.Success)
                            {
                                authCode = altMatch.Groups[1].Value;
                            }
                        }
                    }
                    // Check if clipboard is just the code itself
                    else if (Regex.IsMatch(clipboardText.Trim(), @"^[a-fA-F0-9]{32}$"))
                    {
                        authCode = clipboardText.Trim();
                    }

                    if (!string.IsNullOrEmpty(authCode))
                    {
                        _txtAuthCode.Text = authCode;
                        _lblStatus.Text = "Code pasted successfully!";
                        _lblStatus.ForeColor = ThemeManager.Current.SuccessButtonColor;
                    }
                    else
                    {
                        // Just paste the raw text
                        _txtAuthCode.Text = clipboardText;
                        _lblStatus.Text = "Text pasted. If it's JSON, the code will be extracted automatically.";
                        _lblStatus.ForeColor = ThemeManager.Current.PrimaryTextColor;
                    }
                }
                else
                {
                    _lblStatus.Text = "Clipboard is empty or doesn't contain text.";
                    _lblStatus.ForeColor = ThemeManager.Current.WarningButtonColor;
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Error pasting: {ex.Message}";
                _lblStatus.ForeColor = ThemeManager.Current.DangerButtonColor;
            }
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtAuthCode.Text))
            {
                _lblStatus.Text = "Please enter an authorization code";
                _lblStatus.ForeColor = ThemeManager.Current.WarningButtonColor;
                return;
            }

            _btnLogin.Enabled = false;
            _lblStatus.Text = "Logging in...";
            _lblStatus.ForeColor = ThemeManager.Current.PrimaryTextColor;

            try
            {
                var session = await _epicService.LoginWithAuthCodeAsync(_txtAuthCode.Text);
                SaveSession(session);
                _isLoggedIn = true;
                LoginSuccessful = true;
                UpdateUI();
                _lblStatus.Text = "Login successful!";
                _lblStatus.ForeColor = ThemeManager.Current.SuccessButtonColor;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Login failed: {ex.Message}";
                _lblStatus.ForeColor = ThemeManager.Current.DangerButtonColor;
            }
            finally
            {
                _btnLogin.Enabled = true;
            }
        }

        private async void BtnLogout_Click(object sender, EventArgs e)
        {
            try
            {
                CredentialHelper.ClearEpicSession();
                _isLoggedIn = false;
                LoginSuccessful = false;
                UpdateUI();
                _lblStatus.Text = "Logged out successfully";
                _lblStatus.ForeColor = ThemeManager.Current.SuccessButtonColor;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Logout error: {ex.Message}";
                _lblStatus.ForeColor = ThemeManager.Current.DangerButtonColor;
            }
        }

        private void SaveSession(EpicSession session)
        {
            var sessionJson = JsonHelper.Serialize(session);
            CredentialHelper.SaveEpicSession(sessionJson);
        }

        private void ApplyTheme()
        {
            var palette = ThemeManager.Current;
            this.BackColor = palette.WindowBackColor;
            this.ForeColor = palette.PrimaryTextColor;

            var labels = this.Controls.OfType<Label>().ToList();
            foreach (var lbl in labels)
            {
                if (lbl.Font.Bold && lbl.Text.Contains("Epic Games Login"))
                {
                    lbl.ForeColor = palette.HeadingTextColor;
                }
                else if (lbl == _lblDescription)
                {
                    lbl.ForeColor = palette.SecondaryTextColor;
                }
            }

            var textBoxes = this.Controls.OfType<TextBox>().ToList();
            foreach (var txt in textBoxes)
            {
                txt.BackColor = palette.InputBackColor;
                txt.ForeColor = palette.InputTextColor;
                txt.BorderStyle = BorderStyle.FixedSingle;
            }

            var panels = this.Controls.OfType<Panel>().ToList();
            foreach (var pnl in panels)
            {
                if (pnl == _pnlLoggedIn)
                {
                    pnl.BackColor = palette.SurfaceColor;
                }
                else if (pnl.Tag?.ToString() == "manualPanel")
                {
                    pnl.BackColor = palette.SurfaceAltColor;
                }
            }

            var buttons = this.Controls.OfType<Button>().ToList();
            foreach (var btn in buttons)
            {
                btn.UseVisualStyleBackColor = false;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
            }
        }

        private void ApplyDarkMode()
        {
            if (!IsHandleCreated)
                return;

            bool isDark = ThemeManager.CurrentVariant == ThemeVariant.Dark;
            DarkModeHelper.EnableDarkMode(this, isDark);
            DarkModeHelper.ApplyThemeToControl(this, isDark);
        }
    }
}