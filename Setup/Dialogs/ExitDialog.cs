using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using WixSharp;

using WixSharp.UI.Forms;

namespace Setup.Dialogs
{
    /// <summary>
    /// The standard Exit dialog
    /// </summary>
    public partial class ExitDialog : ManagedForm, IManagedDialog // change ManagedForm->Form if you want to show it in designer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExitDialog"/> class.
        /// </summary>
        public ExitDialog()
        {
            InitializeComponent();
        }

        void ExitDialog_Load(object sender, System.EventArgs e)
        {
            image.Image = Runtime.Session.GetResourceBitmap("WixUI_Bmp_Dialog") ??
                          Runtime.Session.GetResourceBitmap("WixSharpUI_Bmp_Dialog");

            // Try to capture INSTALLDIR as fallback if not already captured
            try
            {
                if (string.IsNullOrEmpty(Globals.InstallDir))
                {
                    Globals.InstallDir = Runtime.Session.Property("INSTALLDIR");
                }
            }
            catch
            {
                // Session may not be available, that's okay
            }

            if (Shell.UserInterrupted || Shell.Log.Contains("User cancelled installation."))
            {
                title.Text = "[UserExitTitle]";
                description.Text = "[UserExitDescription1]";
                this.Localize();
            }
            else if (Shell.ErrorDetected)
            {
                title.Text = "[FatalErrorTitle]";
                description.Text = Shell.CustomErrorDescription ?? "[FatalErrorDescription1]";
                this.Localize();
            }
            else
            {
                // Show checkbox only on successful installation
                chkStartBeanModManager.Visible = true;
            }

            if (image.Image != null)
                ResetLayout();

            // show error message if required
            // if (Shell.Errors.Any())
            // {
            //     string lastError = Shell.Errors.LastOrDefault();
            //     MessageBox.Show(lastError);
            // }
        }

        void ResetLayout()
        {
            // The form controls are properly anchored and will be correctly resized on parent form
            // resizing. However the initial sizing by WinForm runtime doesn't do a good job with DPI
            // other than 96. Thus manual resizing is the only reliable option apart from going WPF.

            // MessageBox.Show($"w:{image.Width}, h:{image.Height}");

            var bHeight = (int)(next.Height * 2.3);

            var upShift = bHeight - bottomPanel.Height;
            bottomPanel.Top -= upShift;
            bottomPanel.Height = bHeight;

            imgPanel.Height = this.ClientRectangle.Height - bottomPanel.Height;
            float ratio = (float)image.Image.Width / (float)image.Image.Height;
            image.Width = (int)(image.Height * ratio);

            // MessageBox.Show($"w:{image.Width}, h:{image.Height}");
        }

        void finish_Click(object sender, System.EventArgs e)
        {
            // Launch BeanModManager if checkbox is checked (after user clicks Finish)
            // Use Globals.InstallDir instead of Runtime.Session.Property() which may not be available
            if (chkStartBeanModManager.Visible && chkStartBeanModManager.Checked)
            {
                try
                {
                    var installPath = Globals.InstallDir;
                    if (string.IsNullOrEmpty(installPath))
                    {
                        // Fallback: try to get from session one more time
                        try
                        {
                            installPath = Runtime.Session.Property("INSTALLDIR");
                        }
                        catch { }
                    }

                    if (!string.IsNullOrEmpty(installPath))
                    {
                        var exePath = System.IO.Path.Combine(installPath, "BeanModManager.exe");
                        
                        // Create a temporary batch file that will launch the app after MSI closes
                        // This is the most reliable method for post-install launches
                        string tempBatch = System.IO.Path.Combine(System.IO.Path.GetTempPath(), 
                            $"BeanModManager_Launch_{System.Guid.NewGuid():N}.bat");
                        
                        string batchContent = $@"@echo off
timeout /t 2 /nobreak >nul
if exist ""{exePath}"" (
    cd /d ""{installPath}""
    start """" ""{exePath}""
)
del ""%~f0""";
                        
                        System.IO.File.WriteAllText(tempBatch, batchContent);
                        
                        // Launch the batch file in a detached process
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = tempBatch,
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true
                        });
                    }
                }
                catch
                {
                    // Silently fail - don't prevent installer from closing
                }
            }

            Shell.Exit();
        }

        void viewLog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string logFile = Runtime.Session.LogFile;

                if (logFile.IsEmpty())
                {
                    string wixSharpDir = Path.GetTempPath().PathCombine("WixSharp");

                    if (!Directory.Exists(wixSharpDir))
                        Directory.CreateDirectory(wixSharpDir);

                    logFile = wixSharpDir.PathCombine(Runtime.ProductName + ".log");
                    System.IO.File.WriteAllText(logFile, Shell.Log);
                }
                Process.Start("notepad.exe", logFile);
            }
            catch
            {
                //Catch all, we don't want the installer to crash in an
                //attempt to view the log.
            }
        }
    }
}