using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HtmlAgilityPack;
using System.Xml;
using System.IO.Compression;

using Main.DataTypes;

namespace Main.Machine
{
    public static class NovelReaderFactory
    {
        public static INovelReader Get(string type)
        {
            if (type.Contains("txt"))
                return (new TXTReader());
            else if (type.Contains("epub"))
                return (new EpubReader());
            else
                return (new RefReader());
        }
    }

    public interface INovelReader
    {
        INovel_Entry Read(string filepath);
    }

    class TXTReader : INovelReader
    {
        public INovel_Entry Read(string filepath)
        {
            Novel_Entry entry = new Novel_Entry()
            {
                filepath = filepath,
            };
            using (StreamReader sr = new StreamReader(filepath))
            {
                string s = "";
                StringBuilder sb = new StringBuilder();
                while ((s = sr.ReadLine()) != null)
                {
                    sb.Append(s + "\n");
                }
                entry.Contents.Add(sb.ToString());
            }
            return entry;
        }
    }
    
    class EpubReader : INovelReader
    {
        public INovel_Entry Read(string filepath)
        {
            Novel_Entry entry = new Novel_Entry()
            {
                filepath = filepath,
            };
            XmlReaderSettings set = new XmlReaderSettings()
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = new XmlUrlResolver(),
                IgnoreComments = true,
                IgnoreWhitespace = true,
            };
            using (ZipArchive zip = ZipFile.OpenRead(filepath))
            {
                var v = zip.Entries.First(x => x.Name == "content.opf");
                

                List<string> entries = new List<string>();
                StreamReader stream = new StreamReader(v.Open(), Encoding.UTF8);
                using (XmlReader xr = XmlReader.Create(stream, set))
                {
                    xr.MoveToContent();
                    while (xr.NodeType != XmlNodeType.Element || xr.Name != "manifest")
                    {
                        if (xr.Name == "dc:title" && xr.NodeType == XmlNodeType.Element)
                        {
                            xr.Read();
                            entry.name = xr.Value;
                            entry.name = string.Join("_", entry.name.Split(Path.GetInvalidFileNameChars()));
                        }
                        xr.Read();
                    }

                    Dictionary<string, string> idToHref = new Dictionary<string, string>();
                    while (xr.Read())
                    {
                        if (xr.Name == "manifest")
                            break;
                        if (xr.Name == "item")
                            idToHref[xr.GetAttribute("id")] = xr.GetAttribute("href");
                    }

                    while (xr.NodeType != XmlNodeType.Element || xr.Name != "spine")
                        xr.Read();

                    while (xr.Read())
                    {
                        if (xr.Name == "spine")
                            break;
                        if (xr.Name == "itemref")
                        {
                            string s = Path.GetDirectoryName(v.FullName);
                            entries.Add((s == "" ? "" : (s + "/")) + idToHref[xr.GetAttribute("idref")]);
                        }
                    }
                }

                for (int i = 0; i < entries.Count(); i++)
                {
                    var f = zip.GetEntry(entries[i]);

                    HtmlAgilityPack.HtmlDocument doct = new HtmlAgilityPack.HtmlDocument();
                    doct.Load(f.Open(), Encoding.UTF8);
                    StringBuilder sb = new StringBuilder();

                    foreach (string s in GetTextsFromNode(doct.DocumentNode.ChildNodes))
                        sb.AppendLine(s + "\n");

                    

                    entry.Titles.Add(doct.DocumentNode.SelectNodes("//title")[0].InnerText);
                    string rs = HtmlEntity.DeEntitize(sb.ToString());
                    entry.Contents.Add(rs);
                }
            }

            
            return entry;
        }
        private void Load_Epub()
        {
            
        }
        private static ICollection<string> GetTextsFromNode(HtmlNodeCollection nodes)
        {
            var texts = new List<string>();
            bool contain_text = false;
            foreach (var node in nodes)
            {
                if (node.Name.ToLowerInvariant() == "style")
                    continue;
                if (node.HasChildNodes)
                {
                    texts.AddRange(GetTextsFromNode(node.ChildNodes));
                }
                else
                {

                    var innerText = node.InnerText;
                    if (node.NodeType == HtmlNodeType.Element && node.OuterHtml == "<br />")
                        texts.Add("\n");
                    else if (!string.IsNullOrWhiteSpace(innerText))
                    {
                        innerText.Replace(Environment.NewLine, "");
                        innerText = System.Text.RegularExpressions.Regex.Replace(innerText, @"\s+", " ");
                        texts.Add(innerText);
                        contain_text = true;
                    }
                }
            }

            if (contain_text)
                return new string[] { string.Join(" ", texts) };

            return texts;
        }
    }

    class RefReader : INovelReader
    {
        public INovel_Entry Read(string filepath)
        {
            Novel_Entry entry = new Novel_Entry()
            {
                filepath = filepath,
                name = Path.GetFileName(filepath)
            };
            using (StreamReader sr = new StreamReader(filepath))
            {
                int chapter = 0, paragraph = 0;

                StringBuilder sb = new StringBuilder();
                
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    var para = s.Split(new char[] { ' ' }, 2);
                    DataTypes.Position pos = new DataTypes.Position(para[0]);

                    if (pos.Chapter != chapter)
                    {
                        entry.Contents.Add(sb.ToString());
                        entry.Titles.Add("Chapter " + chapter);
                        chapter = pos.Chapter;
                        paragraph = pos.Paragraph;
                        sb.Clear();
                    }
                    else if (pos.Paragraph != paragraph)
                    {
                        paragraph = pos.Paragraph;
                        sb.Append("\n\n");
                    }

                    sb.Append(para[1]);
                }
                entry.Contents.Add(sb.ToString());
                entry.Titles.Add("Chapter " + chapter);
            }

            return entry;
        }
    }
}
