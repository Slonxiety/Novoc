using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using VersOne.Epub;
using HtmlAgilityPack;
using System.Xml;

namespace Main.View
{
    public partial class NovelForm : Form
    {
        public NovelForm (string filepath)
        {
            InitializeComponent();
            file = Machine.NovelReaderFactory.Get(Path.GetExtension(filepath)).Read(filepath);
        }

        DataTypes.INovel_Entry file;
        int now_page = 0;

        private void NovelForm_Load(object sender, EventArgs e)
        {
            label1.Text = file.name;
            this.Text = Path.GetFileName(file.filepath);
            Update_Page();
        }

        private void Update_Page ()
        {
            if (file.Contents.Count == 1)
            {
                label2.Text = " - ";
                label3.Text = "";
            }
            else
            {
                label2.Text = (now_page + 1) + "/" + file.Contents.Count;
                label3.Text = file.Titles[now_page];
            }
            if (now_page == 0)
                button2.Visible = false;
            else
                button2.Visible = true;
            if (now_page == file.Contents.Count - 1)
                button1.Visible = false;
            else
                button1.Visible = true;

            richTextBox1.Text = "";
            richTextBox1.Text = file.Contents[now_page];
            richTextBox1.SelectionStart = 0;

            richTextBox1.SelectAll();
            richTextBox1.SelectionFont = new Font("Cambria", 20f);
            richTextBox1.Select(0, 0);
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            now_page++;
            Update_Page();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            now_page--;
            Update_Page();
        }
    }

    public class OpenedFile
    {
        public List<string> Titles { get; private set; } = new List<string>();
        public List<string> Contents { get; private set; } = new List<string>();
        public string filepath { get; private set; }
        public string name { get; private set; }

        static Dictionary<string, OpenedFile> pool = new Dictionary<string, OpenedFile>();
        public static OpenedFile Create (string filepath)
        {
            if (pool.ContainsKey(filepath))
                return pool[filepath];
            else
                return pool[filepath] = new OpenedFile(filepath);
        }
        public static OpenedFile Create (DataTypes.INovel novel)
        {
            return Create(novel.ReferencePath);
        }

        private OpenedFile (string filepath)
        {
            this.filepath = filepath;
            name = Path.GetFileNameWithoutExtension(filepath);
            switch (Path.GetExtension(filepath))
            {
                case ".txt":
                    Load_Txt();
                    break;
                case ".epub":
                    Load_Epub();
                    break;
                case ".ref":
                    Load_Ref();
                    break;
            }
        }

        private void Load_Txt()
        {
            using (StreamReader sr = new StreamReader(filepath))
            {
                string s = "";
                StringBuilder sb = new StringBuilder();
                while ((s = sr.ReadLine()) != null)
                {
                    sb.Append(s + "\n");
                }
                Contents.Add(sb.ToString());
            }
        }

        private void Load_Epub()
        {
            XmlReaderSettings set = new XmlReaderSettings()
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = new XmlUrlResolver(),
                IgnoreComments = true,
                IgnoreWhitespace = true
            };
            using (ZipArchive zip = ZipFile.OpenRead(filepath))
            {
                var v = zip.Entries.First(x => x.Name == "content.opf");

                List<string> entries = new List<string>();
                using (XmlReader xr = XmlReader.Create(v.Open(), set))
                {
                    xr.MoveToContent();
                    while (xr.NodeType != XmlNodeType.Element || xr.Name != "manifest")
                        xr.Read();

                    Dictionary<string, string> idToHref = new Dictionary<string, string>();
                    while (xr.Read())
                    {
                        if (xr.Name == "manifest")
                            break;
                        if (xr.Name == "item")
                            idToHref[xr.GetAttribute("id")] = xr.GetAttribute("href");
                        if (xr.Name == "dc:title" && xr.NodeType == XmlNodeType.Element)
                        {
                            xr.Read();
                            name = xr.Value;
                        }
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
                    /*StringBuilder ssb = new StringBuilder(); string ss;
                    using (StreamReader sr = new StreamReader(f.Open()))
                        while ((ss = sr.ReadLine()) != null)
                        ssb.AppendLine(ss);
                    Contents.Add(ssb.ToString());
                    Titles.Add("");
                    continue;*/
                    /*
                    StringBuilder sb = new StringBuilder();
                    using (XmlReader xr = XmlReader.Create(f.Open(), set))
                    {
                        
                        while (xr.Read())
                        {
                            if (xr.Name == "title")
                            {
                                xr.Read();
                                titles.Add(xr.Value);
                                xr.Read();
                            }
                            if (xr.Name == "p")
                            {
                                XmlReader pReader = xr.ReadSubtree();
                                while (pReader.Read())
                                {
                                    if (pReader.NodeType == XmlNodeType.Text)
                                    {
                                        sb.Append(pReader.Value);
                                    }
                                }
                            }
                        }
                    }*/

                    HtmlAgilityPack.HtmlDocument doct = new HtmlAgilityPack.HtmlDocument();
                    doct.Load(f.Open(), Encoding.UTF8);
                    StringBuilder sb = new StringBuilder();
                    /*foreach (HtmlNode node in doct.DocumentNode.SelectNodes("//text()"))
                    {
                        if (node.NodeType == HtmlNodeType.Comment)
                            continue;
                        string s = node.InnerText.Trim();
                        //s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
                        //s = System.Text.RegularExpressions.Regex.Replace(s, @"<[^>]*>", string.Empty);

                        sb.AppendLine(s);
                    }*/

                    foreach (string s in GetTextsFromNode(doct.DocumentNode.ChildNodes))
                        sb.AppendLine(s + "\n");

                    /*//richTextBox1.Text += sb.ToString() + "\n";
                    for (int ch = 0; ch < sb.Length; ch++)
                    {
                        if (sb[ch] == '\n')
                        {
                            int next = 0;
                            int k = ch;
                            while (k < sb.Length && sb[k++] == '\n')
                                next++;
                            if (k - ch == 1)
                                sb.Remove(ch, 1);
                            else
                                sb.Remove(ch, k - 2 - ch);
                        }
                    }*/

                    Titles.Add(doct.DocumentNode.SelectNodes("//title")[0].InnerText);
                    string rs = HtmlEntity.DeEntitize(sb.ToString());
                    //rs = System.Text.RegularExpressions.Regex.Replace(rs, "\n+", m => m.Value.Length == 1 ? "" : m.Value);
                    Contents.Add(rs);
                }
            }

            /*
            EpubBook book = EpubReader.ReadBook(filepath);

            foreach (EpubChapter chap in book.Chapters)
            {
                richTextBox1.Text += chap.HtmlContent;
            }
            HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(textContentFile.Content);
            StringBuilder sb = new StringBuilder();
            foreach (HtmlNode node in htmlDocument.DocumentNode.SelectNodes("//text()"))
            {
                sb.AppendLine(node.InnerText.Trim());
            }
            string contentText = sb.ToString();*/
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

        private void Load_Ref()
        {
            using (StreamReader sr = new StreamReader(filepath))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    //todo
                }
            }
        }
    }

}
