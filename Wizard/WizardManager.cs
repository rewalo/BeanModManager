using System;
using System.Windows.Forms;
using BeanModManager.Models;
using BeanModManager.Services;

namespace BeanModManager.Wizard
{
    public class WizardManager
    {
        private readonly Config _config;

        public WizardManager(Config config)
        {
            _config = config;
        }

        public bool RunWizardAsync(IWin32Window owner)
        {
            try
            {
                int stepIndex = 0;
                string amongUsPath = _config.AmongUsPath;
                string detectedChannel = null;
                string selectedChannel = Helpers.GameChannels.Steam;

                while (true)
                {
                    switch (stepIndex)
                    {
                        case 0:
                            using (WizardWelcomeDialog welcomeDialog = new WizardWelcomeDialog())
                            {
                                DialogResult result = welcomeDialog.ShowDialog(owner);
                                if (result == DialogResult.OK)
                                {
                                    stepIndex++;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            break;

                        case 1:
                            using (WizardDetectPathDialog detectDialog = new WizardDetectPathDialog())
                            {
                                DialogResult result = detectDialog.ShowDialog(owner);
                                if (result == DialogResult.OK)
                                {
                                    amongUsPath = detectDialog.SelectedPath;
                                    detectedChannel = detectDialog.DetectedChannel;

                                    if (string.IsNullOrEmpty(amongUsPath) || !AmongUsDetector.ValidateAmongUsPath(amongUsPath))
                                    {
#if DEBUG
                                        // The dialog only returns OK with an empty path when the
                                        // user confirmed the debug skip, so just advance.
                                        if (string.IsNullOrEmpty(amongUsPath))
                                        {
                                            stepIndex++;
                                            break;
                                        }
#endif
                                        MessageBox.Show("Invalid Among Us path. Please try again.", "Error",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        continue;
                                    }

                                    _config.AmongUsPath = amongUsPath;
                                    _config.WizardDetectedAmongUs = true;
                                    _config.Save();

                                    stepIndex++;
                                }
                                else if (result == DialogResult.Retry)
                                {
                                    stepIndex = Math.Max(0, stepIndex - 1);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            break;

                        case 2:
                            using (WizardSelectChannelDialog channelDialog = new WizardSelectChannelDialog(detectedChannel, selectedChannel))
                            {
                                DialogResult result = channelDialog.ShowDialog(owner);
                                if (result == DialogResult.OK)
                                {
                                    selectedChannel = channelDialog.SelectedChannel;
                                    _config.WizardSelectedChannel = true;
                                    _config.GameChannel = selectedChannel;
                                    _config.Save();

                                    stepIndex++;
                                }
                                else if (result == DialogResult.Retry)
                                {
                                    stepIndex = Math.Max(1, stepIndex - 1);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            break;

                        case 3:
                            using (WizardInstallBepInExDialog bepInExDialog = new WizardInstallBepInExDialog(amongUsPath, selectedChannel))
                            {
                                DialogResult result = bepInExDialog.ShowDialog(owner);
                                if (result == DialogResult.OK)
                                {
                                    bool bepInExInstalled = bepInExDialog.InstallationSuccess;
                                    if (!bepInExInstalled && !bepInExDialog.SkipInstallation)
                                    {
                                        DialogResult prompt = MessageBox.Show(
                                            "BepInEx installation failed or was skipped.\n\n" +
                                            "You can install it later from the Settings tab.\n\n" +
                                            "Continue with setup?",
                                            "BepInEx Not Installed",
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question);

                                        if (prompt != DialogResult.Yes)
                                        {
                                            continue;
                                        }
                                    }

                                    if (bepInExInstalled)
                                    {
                                        _config.WizardInstalledBepInEx = true;
                                        _config.Save();
                                    }

                                    stepIndex++;
                                }
                                else if (result == DialogResult.Retry)
                                {
                                    stepIndex = Math.Max(2, stepIndex - 1);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            break;

                        case 4:
                            using (WizardCompleteDialog completeDialog = new WizardCompleteDialog())
                            {
                                DialogResult result = completeDialog.ShowDialog(owner);
                                if (result == DialogResult.OK)
                                {
                                    _config.FirstLaunchWizardCompleted = true;
                                    _config.SaveSync();
                                    return true;
                                }
                                else if (result == DialogResult.Retry)
                                {
                                    stepIndex = Math.Max(3, stepIndex - 1);
                                }
                                else
                                {
                                    return false;
                                }
                            }

                            break;

                        default:
                            return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during setup: {ex.Message}", "Setup Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}

