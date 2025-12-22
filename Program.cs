using System;
using System.Linq;
using System.Windows.Forms;

namespace BeanModManager
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && (args[0] == "--populate-cache" || args[0] == "-c"))
            {
                PopulateRegistryCache.Main(args.Skip(1).ToArray()).GetAwaiter().GetResult();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}
