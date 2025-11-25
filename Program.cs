using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeanModManager
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Check if running in cache population mode
            if (args.Length > 0 && (args[0] == "--populate-cache" || args[0] == "-c"))
            {
                // Run cache populator
                PopulateRegistryCache.Main(args.Skip(1).ToArray()).GetAwaiter().GetResult();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}
