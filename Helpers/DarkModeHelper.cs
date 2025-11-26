using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BeanModManager.Helpers
{
    public static class DarkModeHelper
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_MICA_EFFECT = 1029;
        private const int WM_THEMECHANGED = 0x031A;
        private const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern bool IsThemeActive();

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetPreferredAppMode(int mode);

        private const int PreferredAppModeAllowDark = 1;
        private const int PreferredAppModeForceDark = 2;
        private const int PreferredAppModeForceLight = 0;

        public static void EnableDarkMode(Form form, bool enable)
        {
            if (form == null || !form.IsHandleCreated)
                return;

            try
            {
                // Enable dark mode for window chrome (title bar, borders)
                int darkMode = enable ? 1 : 0;
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                // Set window theme for all child controls
                ApplyThemeToControl(form, enable);
            }
            catch
            {
                // Silently fail if API not available
            }
        }

        public static void ApplyThemeToControl(Control control, bool darkMode)
        {
            if (control == null)
                return;

            try
            {
                if (control.IsHandleCreated)
                {
                    SetWindowTheme(control.Handle, darkMode ? "DarkMode_Explorer" : "", null);
                }
            }
            catch
            {
                // Silently fail
            }

            // Recursively apply to children
            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, darkMode);
            }
        }

        public static void InitializeDarkMode()
        {
            try
            {
                // Enable dark mode support at app level
                SetPreferredAppMode(PreferredAppModeAllowDark);
            }
            catch
            {
                // Silently fail if API not available
            }
        }
    }
}

