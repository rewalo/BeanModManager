using System;
using System.Diagnostics;
using System.Windows.Forms;

using WixSharp;

using WixSharp.UI.Forms;

namespace Setup.Dialogs
{
                public partial class WelcomeDialog : ManagedForm, IManagedDialog     {
                                public WelcomeDialog()
        {
            InitializeComponent();
        }

        void WelcomeDialog_Load(object sender, EventArgs e)
        {
            image.Image = Runtime.Session.GetResourceBitmap("WixUI_Bmp_Dialog") ??
                          Runtime.Session.GetResourceBitmap("WixSharpUI_Bmp_Dialog");

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

        void cancel_Click(object sender, EventArgs e)
        {
            Shell.Cancel();
        }

        void next_Click(object sender, EventArgs e)
        {
            Shell.GoNext();
        }

        void back_Click(object sender, EventArgs e)
        {
            Shell.GoPrev();
        }
    }
}