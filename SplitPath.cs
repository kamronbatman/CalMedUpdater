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

        public SplitPath(string path)
        {
            Path = path;
        }

        public SplitPath(string x86, string x64) : this(Utility.Is64Win() ? x64 : x86)
        {
        }

        public SplitPath(XmlNode node) : this(node?["x86"]?.InnerText ?? node?.InnerText, node?["x64"]?.InnerText ?? node?.InnerText)
        {
        }

        public static implicit operator String(SplitPath path)
        {
            return path.Path;
        }

        public static implicit operator SplitPath(string path)
        {
            return new SplitPath(path);
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
