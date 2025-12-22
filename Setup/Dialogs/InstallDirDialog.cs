using System;
using System.Windows.Forms;

using WixSharp;

using WixSharp.UI.Forms;

namespace Setup.Dialogs
{
                public partial class InstallDirDialog : ManagedForm, IManagedDialog      {
                                public InstallDirDialog()
        {
            InitializeComponent();
            label1.MakeTransparentOn(banner);
            label2.MakeTransparentOn(banner);
        }

        string installDirProperty;

        void InstallDirDialog_Load(object sender, EventArgs e)
        {
            banner.Image = Runtime.Session.GetResourceBitmap("WixUI_Bmp_Banner") ??
                           Runtime.Session.GetResourceBitmap("WixSharpUI_Bmp_Banner");

            installDirProperty = Runtime.Session.Property("WixSharp_UI_INSTALLDIR");

            string installDirPropertyValue = Runtime.Session.Property(installDirProperty);

            if (installDirPropertyValue.IsEmpty())
            {
                                                installDir.Text = Runtime.Session.GetDirectoryPath(installDirProperty);

                if (installDir.Text == "ABSOLUTEPATH")
                    installDir.Text = Runtime.Session.Property("INSTALLDIR_ABSOLUTEPATH");
            }
            else
            {
                                installDir.Text = installDirPropertyValue;
            }

            if (banner.Image != null)
                ResetLayout();
        }

        void ResetLayout()
        {
                                                float ratio = (float)banner.Image.Width / (float)banner.Image.Height;
            topPanel.Height = (int)(banner.Width / ratio);
            topBorder.Top = topPanel.Height + 1;

            middlePanel.Top = topBorder.Bottom + 10;

            var upShift = (int)(next.Height * 2.3) - bottomPanel.Height;
            bottomPanel.Top -= upShift;
            bottomPanel.Height += upShift;
        }

        void back_Click(object sender, EventArgs e)
        {
            Shell.GoPrev();
        }

        void next_Click(object sender, EventArgs e)
        {
            if (!installDirProperty.IsEmpty())
                Runtime.Session[installDirProperty] = installDir.Text;
            Shell.GoNext();
        }

        void cancel_Click(object sender, EventArgs e)
        {
            Shell.Cancel();
        }

        void change_Click(object sender, EventArgs e)
        {
            if (this.Session().UseModernFolderBrowserDialog())
            {
                try
                {
                    var newFoledrPath = WixSharp.FolderBrowserDialog.ShowDialog(this.Handle, "Select Folder", installDir.Text);
                    if (newFoledrPath != null)
                        installDir.Text = newFoledrPath;
                    return;
                }
                catch
                {
                }
            }

            using (var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = installDir.Text })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    installDir.Text = dialog.SelectedPath;
                }
            }
        }
    }
}