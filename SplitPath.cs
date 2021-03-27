using System;
using System.Xml;

namespace CalMedUpdater
{
    public class SplitPath
    {
        public string Path { get; set; }

        public SplitPath(string path) => Path = path;

        public SplitPath(string x86, string x64) : this(Utility.Is64Win() ? x64 : x86)
        {
        }

        public SplitPath(XmlNode node) : this(node?["x86"]?.InnerText ?? node?.InnerText, node?["x64"]?.InnerText ?? node?.InnerText)
        {
        }

        public static implicit operator String(SplitPath path) => path.Path;

        public static implicit operator SplitPath(string path) => new SplitPath(path);

        public override string ToString() => Path;
    }
}
