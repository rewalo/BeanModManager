using BeanModManager.Services;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace BeanModManager
{
    public partial class Main : Form
    {
        private void HandleUpdateCheckerProgress(string message)
        {
            if (_isInitialLoadInProgress)
                return;

            UpdateStatus(message);
        }

        private async System.Threading.Tasks.Task CheckForAppUpdatesAsync()
        {
            try
            {
                await _updateChecker.CheckForUpdatesAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private void UpdateChecker_UpdateAvailable(object sender, UpdateChecker.UpdateAvailableEventArgs e)
        {
            SafeInvoke(() =>
            {
                var result = MessageBox.Show(
                    $"A new version of Bean Mod Manager is available!\n\n" +
                    $"Current version: {e.CurrentVersion}\n" +
                    $"Latest version: {e.LatestVersion}\n\n" +
                    $"{(!string.IsNullOrEmpty(e.ReleaseNotes) ? $"Release notes:\n{e.ReleaseNotes.Substring(0, Math.Min(200, e.ReleaseNotes.Length))}...\n\n" : "")}" +
                    $"Would you like to open the download page?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = e.ReleaseUrl,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        MessageBox.Show(
                            $"Failed to open the download page.\n\nPlease visit: {e.ReleaseUrl}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            });
        }
    }
}


