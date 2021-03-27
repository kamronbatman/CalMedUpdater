using System;
using System.IO;
using System.Xml;
using File = System.IO.File;

namespace CalMedUpdater
{
    public class MainProgram
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No configuration file specified.");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Failed to open: {0}", args[0]);
                return;
            }

            var is64Win = Utility.Is64Win();

            XmlDocument doc = new XmlDocument();
            doc.Load(args[0]);

            XmlNode root = doc["CalMedUpdater"];

            if (root == null) { Console.WriteLine("Cannot find CalMedUpdater root node in XML configuration."); return; }

            var installPath = is64Win ? root["InstallPath"]["x64"].InnerText : root["InstallPath"]["x86"].InnerText;

            var validationNode = root["Validation"];
            if (validationNode != null)
            {
                var fileNameToVerify = validationNode["FileToVerify"].InnerText;
                var fileToVerify = Path.Combine(installPath, fileNameToVerify);
                var hashType = (HashType)Enum.Parse(typeof(HashType), validationNode["HashType"].InnerText);
                var hashToCheck = validationNode["Hash"].InnerText.ToUpper();

                if (File.Exists(fileToVerify))
                {
                    var hashFound = Hashing.HashFile(hashType, new FileInfo(fileToVerify)).ToUpper();
                    if (args.Length > 1 && args[0].ToLower() == "-hash" || args[1].ToLower() == "-hash")
                    {
                        Console.WriteLine("Hash ({2}) for {0} is {1}", fileNameToVerify, hashFound, hashType.ToString());
                        return;
                    }

                    if (hashFound == hashToCheck)
                    {
                        Console.WriteLine("Already installed and updated.");
                        return;
                    }
                }
            }

            XmlNode shortcutNode = root["DesktopShortcut"];
            var desktopShortcut = new DesktopShortcut() { Name = shortcutNode["Name"].InnerText, Arguments = shortcutNode["Arguments"].InnerText };

            WindowsShortcuts.DeleteDesktopShortcut(desktopShortcut);

            Install(new CalMedInstall(root["CalMedInstall"]), installPath);
            var updateNode = root["CalMedUpdate"];
            if (updateNode != null)
            {
                Install(new CalMedInstall(updateNode), installPath);
            }

            XmlNode itemizedStsNode = root["ItemizedSts"];

            if (itemizedStsNode != null)
            {
                var destPath = Path.Combine(installPath, @"rpt\ItemizedSt");
                foreach (XmlNode itemizedStNode in itemizedStsNode.ChildNodes)
                {
                    var fileName = itemizedStNode["File"].InnerText;
                    var source = itemizedStNode["Source"].InnerText;
                    File.Delete(Path.Combine(destPath, fileName));
                    File.Copy(Path.Combine(source, fileName), Path.Combine(destPath, fileName));
                }

                Console.WriteLine("Copied ItemizedSts");
            }

            WindowsShortcuts.CreateDesktopShortcut(installPath, "maincds.exe", desktopShortcut);
            Console.WriteLine("Created shortcut");
        }

        private static void Install(CalMedInstall install, string installPath)
        {
            Console.WriteLine("Starting Install {0}", install.FilePath);
            install.PerformInstall(installPath);
            Console.WriteLine("Installation Finished");
            Console.WriteLine("Starting Post Install");
            install.PerformPostInstall(installPath);
            Console.WriteLine("Post Install Finished");
        }
    }
}
