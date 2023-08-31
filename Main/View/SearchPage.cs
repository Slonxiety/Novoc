using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Main.View
{
    public partial class SearchPage : Form
    {
        DataTypes.IDictionary dictionary;
        HashSet<string> varified_words = new HashSet<string>();

        public SearchPage(DataTypes.IDictionary dict)
        {
            dictionary = dict;
            InitializeComponent();

            using (StringReader sr = new StringReader(Properties.Resources.English_Word_List))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                    varified_words.Add(s);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string voc = textBox1.Text.ToLower();

            await Search(voc);
        }
        private Task Search(string voc)
        {
            var tsk = new TaskCompletionSource<bool>();
            richTextBox1.Text = "";
            richTextBox1.SelectionIndent = 0;

            if (varified_words.Contains(voc))
                richTextBox1.AppendText("This is a valid word\n\n");

            if (!dictionary.ContainWord(voc))
            {
                richTextBox1.AppendText("No References found");
                return tsk.Task;
            }

            var records_list = dictionary.Get_Positions_Records(voc);
            int load_count = 0;
            richTextBox1.AppendText("References:\n");
            richTextBox1.SelectionIndent = 4;
            foreach (var record in records_list)
            {
                foreach (var refline in record.Novel.Get_Reference_Sentences(record.Positions.ToList()))
                {
                    richTextBox1.AppendText("  " + refline.Item2 + "\n");
                    richTextBox1.SelectionAlignment = HorizontalAlignment.Right;
                    richTextBox1.AppendText(string.Format("-{0}, Chapter {1}, Paragraph {2}\n\n",
                                                        record.Novel.Name, refline.Item1.Chapter,
                                                        refline.Item1.Paragraph));
                    richTextBox1.SelectionAlignment = HorizontalAlignment.Left;
                    if (load_count++ == 300)
                    {
                        richTextBox1.AppendText("There are more than 300 references, the process was cut short");
                        tsk.SetResult(true);
                        return tsk.Task;
                    }
                }
            }


            richTextBox1.SelectAll();
            richTextBox1.SelectionFont = new Font("Cambria", 18f);
            richTextBox1.Select(0, 0);
            tsk.SetResult(true);
            return tsk.Task;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                button1_Click(null, null);
        }
    }
}
