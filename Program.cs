using System;
using System.IO;
using System.Xml;
using File = System.IO.File;

namespace CalMedUpdater
{
    public class CalMedUpdaterApp
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Cal-Med Updater v2.2");

            if (args.Length == 0)
            {
                Console.WriteLine("No configuration file specified.");
                return;
            }

            var updatesFile = args[0];

            if (!File.Exists(updatesFile))
            {
                Console.WriteLine("Failed to open: {0}", updatesFile);
                return;
            }

            var getHashMode = args.Length > 0 && args[0].ToLower() == "-hash" || args.Length > 1 && args[1].ToLower() == "-hash";

            var doc = new XmlDocument();
            doc.Load(updatesFile);

            XmlNode root = doc["CalMedUpdater"];

            if (root == null) { Console.WriteLine("Cannot find CalMedUpdater root node in XML configuration."); return; }

            var installPath = new SplitPath(root["InstallPath"]);

            var validationNode = root["Validation"];
            if (!getHashMode && validationNode != null)
            {
                var fileNameToVerifyNode = validationNode["FileToVerify"];
                var fileToVerify = Path.Combine(installPath, fileNameToVerifyNode!.InnerText);
                var hashTypeNode = validationNode["HashType"];
                var hashType = Enum.Parse<HashType>(hashTypeNode!.InnerText);
                var hashNode = validationNode["Hash"];
                var hashToCheck = hashNode!.InnerText.ToUpper();

                if (File.Exists(fileToVerify))
                {
                    var hashFound = Hashing.HashFile(hashType, new FileInfo(fileToVerify)).ToUpper();

                    if (hashFound == hashToCheck)
                    {
                        Console.WriteLine("Already installed and updated.");
                        return;
                    }
                }
            }

            XmlNode shortcutNode = root["DesktopShortcut"];
            DesktopShortcut desktopShortcut = new()
            {
                Name = shortcutNode["Name"]!.InnerText,
                Arguments = shortcutNode["Arguments"]!.InnerText
            };

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

            if (getHashMode)
            {
                if (validationNode == null)
                {
                    validationNode = doc.CreateElement("Validation");
                    root.AppendChild(validationNode);
                }

                var fileNameToVerifyNode = validationNode["FileToVerify"];
                if (fileNameToVerifyNode == null)
                {
                    fileNameToVerifyNode = doc.CreateElement("FileToVerify");
                    validationNode.AppendChild(fileNameToVerifyNode);
                }

                if (string.IsNullOrWhiteSpace(fileNameToVerifyNode.InnerText))
                {
                    fileNameToVerifyNode.InnerText ??= "maincds.exe";
                }

                var fileToVerify = fileNameToVerifyNode.InnerText;

                var hashTypeNode = validationNode["HashType"];
                if (hashTypeNode == null)
                {
                    hashTypeNode = doc.CreateElement("HashType");
                    validationNode.AppendChild(hashTypeNode);
                }

                HashType hashType;
                try
                {
                    hashType = Enum.Parse<HashType>(hashTypeNode.InnerText);
                }
                catch
                {
                    hashType = HashType.SHA256;
                    hashTypeNode.InnerText = hashType.ToString();
                }

                var hashNode = validationNode["Hash"];
                if (hashNode == null)
                {
                    hashNode = doc.CreateElement("Hash");
                    validationNode.AppendChild(hashNode);
                }

                var hash = hashNode.InnerText = Hashing.HashFile(hashType, new FileInfo(Path.Combine(installPath, fileToVerify)));

                Console.WriteLine("Updating hash for {0} in {1} to {2} ({3})", fileToVerify, updatesFile, hash, hashType.ToString());
                doc.Save(updatesFile);
            }
        }

        private static void Install(Install install, string installPath)
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
