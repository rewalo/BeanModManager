using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using WixSharp;

using WixSharp.UI.Forms;

namespace Setup.Dialogs
{
                public partial class ExitDialog : ManagedForm, IManagedDialog     {
                                public ExitDialog()
        {
            InitializeComponent();
        }

        void ExitDialog_Load(object sender, System.EventArgs e)
        {
            image.Image = Runtime.Session.GetResourceBitmap("WixUI_Bmp_Dialog") ??
                          Runtime.Session.GetResourceBitmap("WixSharpUI_Bmp_Dialog");

                        try
            {
                if (string.IsNullOrEmpty(Globals.InstallDir))
                {
                    Globals.InstallDir = Runtime.Session.Property("INSTALLDIR");
                }
            }
            catch
            {
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
                                chkStartBeanModManager.Visible = true;
            }

            if (image.Image != null)
                ResetLayout();

                                                                                }

        void ResetLayout()
        {
                                    
            
            var bHeight = (int)(next.Height * 2.3);

            var upShift = bHeight - bottomPanel.Height;
            bottomPanel.Top -= upShift;
            bottomPanel.Height = bHeight;

            imgPanel.Height = this.ClientRectangle.Height - bottomPanel.Height;
            float ratio = (float)image.Image.Width / (float)image.Image.Height;
            image.Width = (int)(image.Height * ratio);

                    }

        void finish_Click(object sender, System.EventArgs e)
        {
                                    if (chkStartBeanModManager.Visible && chkStartBeanModManager.Checked)
            {
                try
                {
                    var installPath = Globals.InstallDir;
                    if (string.IsNullOrEmpty(installPath))
                    {
                                                try
                        {
                            installPath = Runtime.Session.Property("INSTALLDIR");
                        }
                        catch { }
                    }

                    if (!string.IsNullOrEmpty(installPath))
                    {
                        var exePath = System.IO.Path.Combine(installPath, "BeanModManager.exe");
                        
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
                                            }
        }
    }
}