using System;
using System.Drawing;
using System.Security.Principal;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.UI.Forms;
using WixToolset.Dtf.WindowsInstaller;

namespace Setup.Dialogs
{
                public partial class ProgressDialog : ManagedForm, IManagedDialog, IProgressDialog     {
                                public ProgressDialog()
        {
            InitializeComponent();
            dialogText.MakeTransparentOn(banner);

            showWaitPromptTimer = new System.Windows.Forms.Timer() { Interval = 4000 };
            showWaitPromptTimer.Tick += (s, e) =>
            {
                this.waitPrompt.Visible = true;
                showWaitPromptTimer.Stop();
            };
        }

        System.Windows.Forms.Timer showWaitPromptTimer;

        void ProgressDialog_Load(object sender, EventArgs e)
        {
            banner.Image = Runtime.Session.GetResourceBitmap("WixUI_Bmp_Banner") ??
                           Runtime.Session.GetResourceBitmap("WixSharpUI_Bmp_Banner");

                        Globals.InstallDir = Runtime.Session.Property("INSTALLDIR");

            if (!WindowsIdentity.GetCurrent().IsAdmin() && Uac.IsEnabled())
            {
                this.waitPrompt.Text = Runtime.Session.Property("UAC_WARNING");

                showWaitPromptTimer.Start();
            }

            if (banner.Image != null)
                ResetLayout();

            Shell.StartExecute();
        }

        void ResetLayout()
        {
                                                float ratio = (float)banner.Image.Width / (float)banner.Image.Height;
            topPanel.Height = (int)(banner.Width / ratio);
            topBorder.Top = topPanel.Height + 1;

            var upShift = (int)(next.Height * 2.3) - bottomPanel.Height;
            bottomPanel.Top -= upShift;
            bottomPanel.Height += upShift;

            var fontSize = waitPrompt.Font.Size;
            float scaling = 1;
            waitPrompt.Font = new Font(waitPrompt.Font.Name, fontSize * scaling, FontStyle.Italic);
        }

                                        protected override void OnShellChanged()
        {
            if (Runtime.Session.IsUninstalling())
            {
                dialogText.Text =
                Text = "[ProgressDlgTitleRemoving]";
                description.Text = "[ProgressDlgTextRemoving]";
            }
            else if (Runtime.Session.IsRepairing())
            {
                dialogText.Text =
                Text = "[ProgressDlgTextRepairing]";
                description.Text = "[ProgressDlgTitleRepairing]";
            }
            else if (Runtime.Session.IsInstalling())
            {
                dialogText.Text =
                Text = "[ProgressDlgTitleInstalling]";
                description.Text = "[ProgressDlgTextInstalling]";
            }

            this.Localize();
        }

                                                                                public override MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord, MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
        {
            switch (messageType)
            {
                case InstallMessage.InstallStart:
                case InstallMessage.InstallEnd:
                    {
                        showWaitPromptTimer.Stop();
                        waitPrompt.Visible = false;
                    }
                    break;

                case InstallMessage.ActionStart:
                    {
                        try
                        {
                                                        
                            string message = null;

                            bool simple = true;
                            if (simple)
                            {
                                /*
                                messageRecord[2] unconditionally contains the string to display

                                Examples:

                                   messageRecord[0]    "Action 23:14:50: [1]. [2]"
                                   messageRecord[1]    "InstallFiles"
                                   messageRecord[2]    "Copying new files"
                                   messageRecord[3]    "File: [1],  Directory: [9],  Size: [6]"

                                   messageRecord[0]    "Action 23:15:21: [1]. [2]"
                                   messageRecord[1]    "RegisterUser"
                                   messageRecord[2]    "Registering user"
                                   messageRecord[3]    "[1]"

                                */
                                if (messageRecord.FieldCount >= 3)
                                {
                                    message = messageRecord[2].ToString();
                                }
                            }
                            else
                            {
                                message = messageRecord.FormatString;
                                if (message.IsNotEmpty())
                                {
                                    for (int i = 1; i < messageRecord.FieldCount; i++)
                                    {
                                        message = message.Replace("[" + i + "]", messageRecord[i].ToString());
                                    }
                                }
                                else
                                {
                                    message = messageRecord[messageRecord.FieldCount - 1].ToString();
                                }
                            }

                            if (message.IsNotEmpty())
                                currentAction.Text = "{0} {1}".FormatWith(currentActionLabel.Text, message);
                        }
                        catch
                        {
                                                                                }
                    }
                    break;
            }
            return MessageResult.OK;
        }

                                        public override void OnProgress(int progressPercentage)
        {
            progress.Value = progressPercentage;

            if (progressPercentage > 0)
            {
                waitPrompt.Visible = false;
            }
        }

                                public override void OnExecuteComplete()
        {
            currentAction.Text = null;
            Shell.GoNext();
        }

                                                void cancel_Click(object sender, EventArgs e)
        {
            if (Shell.IsDemoMode)
                Shell.GoNext();
            else
                Shell.Cancel();
        }
    }
}