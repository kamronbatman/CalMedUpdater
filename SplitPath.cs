using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace CalMedUpdater
{
    public class SplitPath
    {
        public string Path { get; set; }

        public SplitPath(string x86, string x64)
        {
            Path = Utility.Is64Win() ? x64 : x86;
        }

        public SplitPath(XmlNode node) : this(node?["x86"].InnerText, node?["x64"].InnerText)
        {
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
