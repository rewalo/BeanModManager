namespace Setup.Dialogs
{
    /// <summary>
    /// Global state storage for installer dialogs
    /// Used to store values that may not be accessible after installation completes
    /// </summary>
    public static class Globals
    {
        public static string InstallDir { get; set; }
    }
}

