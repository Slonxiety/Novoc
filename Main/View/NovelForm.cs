using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Main.View
{
    public partial class NovelForm : Form
    {
        public NovelForm(string filepath)
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

        private void Update_Page()
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
}
