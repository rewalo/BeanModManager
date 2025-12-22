using System;
using System.Windows.Forms;

using WixSharp;

using WixSharp.UI.Forms;

namespace Setup.Dialogs
{
                public partial class OptionsDialog : ManagedForm, IManagedDialog
    {
                                public OptionsDialog()
        {
            InitializeComponent();
            label1.MakeTransparentOn(banner);
            label2.MakeTransparentOn(banner);
        }

        void OptionsDialog_Load(object sender, EventArgs e)
        {
            banner.Image = Runtime.Session.GetResourceBitmap("WixUI_Bmp_Banner") ??
                           Runtime.Session.GetResourceBitmap("WixSharpUI_Bmp_Banner");

                        string desktopShortcut = Runtime.Session.Property("DESKTOP_SHORTCUT");
            string startMenuEntry = Runtime.Session.Property("START_MENU_ENTRY");

            chkDesktopShortcut.Checked = desktopShortcut.IsEmpty() || desktopShortcut == "1";
            chkStartMenuEntry.Checked = startMenuEntry.IsEmpty() || startMenuEntry == "1";
            
                        chkOpenAfterInstall.Visible = false;

            if (banner.Image != null)
                ResetLayout();
        }

        void ResetLayout()
        {
            float ratio = (float)banner.Image.Width / (float)banner.Image.Height;
            topPanel.Height = (int)(banner.Width / ratio);
            topBorder.Top = topPanel.Height + 1;

            var upShift = (int)(next.Height * 2.3) - bottomPanel.Height;
            bottomPanel.Top -= upShift;
            bottomPanel.Height += upShift;

            middlePanel.Top = topBorder.Bottom + 10;
            middlePanel.Height = (bottomPanel.Top - 10) - middlePanel.Top;
        }

        void back_Click(object sender, EventArgs e)
        {
            Shell.GoPrev();
        }

        void next_Click(object sender, EventArgs e)
        {
                        Runtime.Session["DESKTOP_SHORTCUT"] = chkDesktopShortcut.Checked ? "1" : "0";
            Runtime.Session["START_MENU_ENTRY"] = chkStartMenuEntry.Checked ? "1" : "0";

            Shell.GoNext();
        }

        void cancel_Click(object sender, EventArgs e)
        {
            Shell.Cancel();
        }
    }
}

