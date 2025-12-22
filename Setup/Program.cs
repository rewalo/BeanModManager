using Setup.Dialogs;
using System;
using WixSharp;
using WixSharp.CommonTasks;

namespace Setup
{
    public class Program
    {
        static void Main()
        {
            var project = new ManagedProject(
                "Bean Mod Manager",
                new Dir(@"%ProgramFiles%\Bean Mod Manager",
                    new Files(@"*.exe")),

                                new Dir(@"%Desktop%",
                    new ExeFileShortcut("Bean Mod Manager", "[INSTALLDIR]BeanModManager.exe", "")
                    {
                        Condition = new Condition("DESKTOP_SHORTCUT=\"1\"")
                    }),

                                new Dir(@"%ProgramMenu%\Bean Mod Manager",
                    new ExeFileShortcut("Bean Mod Manager", "[INSTALLDIR]BeanModManager.exe", "")
                    {
                        Condition = new Condition("START_MENU_ENTRY=\"1\"")
                    },
                    new ExeFileShortcut("Uninstall Bean Mod Manager", "[System64Folder]msiexec.exe", "/x [ProductCode]")
                    {
                        Condition = new Condition("START_MENU_ENTRY=\"1\"")
                    })
            );

            project.SourceBaseDir = System.IO.Path.GetFullPath(@"..\bin\Release");


            project.GUID = new Guid("5939155f-c7e1-43ee-aad9-9bc67a35d9c5");
            
            // Get version from environment variable (set by MSBuild or CI), fallback to default
            var versionString = Environment.GetEnvironmentVariable("VERSION") ?? "1.5.9";
            
            // Remove 'v' prefix if present (e.g., "v1.5.9" -> "1.5.9")
            versionString = versionString.TrimStart('v', 'V');
            
            if (!Version.TryParse(versionString, out var version))
            {
                Console.WriteLine($"Warning: Invalid version string '{versionString}', using default 1.5.9");
                version = new Version("1.5.9");
            }
            
            project.Version = version;
            Console.WriteLine($"Building MSI with version: {version}");

                        project.ControlPanelInfo.ProductIcon = @"..\mod.ico";
            project.ControlPanelInfo.Manufacturer = "rewalo";
            project.ControlPanelInfo.Comments = "A simple mod manager for Among Us. Install and manage mods like TOHE, Town of Us Mira, Better CrewLink, and The Other Roles without the hassle.";
            project.ControlPanelInfo.Readme = "https://github.com/rewalo/BeanModManager/blob/master/README.md";
            project.ControlPanelInfo.HelpLink = "https://github.com/rewalo/BeanModManager";
            project.ControlPanelInfo.UrlInfoAbout = "https://github.com/rewalo/BeanModManager";
            project.ControlPanelInfo.UrlUpdateInfo = "https://github.com/rewalo/BeanModManager/releases";

                        project.Scope = InstallScope.perMachine;

                        project.AddProperty(new Property("DESKTOP_SHORTCUT", "1"));
            project.AddProperty(new Property("START_MENU_ENTRY", "1"));

                        project.ManagedUI = new ManagedUI();

            project.ManagedUI.InstallDialogs.Add<WelcomeDialog>()
                                            .Add<LicenceDialog>()
                                            .Add<InstallDirDialog>()
                                            .Add<OptionsDialog>()
                                            .Add<ProgressDialog>()
                                            .Add<ExitDialog>();

            project.ManagedUI.ModifyDialogs.Add<MaintenanceTypeDialog>()
                                           .Add<ProgressDialog>()
                                           .Add<ExitDialog>();

                        
            ValidateAssemblyCompatibility();

            project.BuildMsi();
        }

        static void ValidateAssemblyCompatibility()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            if (!assembly.ImageRuntimeVersion.StartsWith("v2."))
            {
                Console.WriteLine("Warning: assembly '{0}' is compiled for {1} runtime, which may not be compatible with the CLR version hosted by MSI. " +
                                  "The incompatibility is particularly possible for the EmbeddedUI scenarios. " +
                                   "The safest way to solve the problem is to compile the assembly for v3.5 Target Framework.",
                                   assembly.GetName().Name, assembly.ImageRuntimeVersion);
            }
        }
    }
}