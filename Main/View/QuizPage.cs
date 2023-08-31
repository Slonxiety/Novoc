using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Main.View
{
    public partial class QuizPage : Form
    {
        DataTypes.IDictionary dictionary;
        public QuizPage(DataTypes.IDictionary dictionary)
        {
            InitializeComponent();
            this.dictionary = dictionary;
        }

        string guess;
        int dif;

        private void button1_Click(object sender, EventArgs e)
        {
            if (valid_words.Count < 400)
                Evaluate_All_Word();
            else
            {
                MessageBox.Show("Correct answer is: " + guess);
                textBox1.Text = (int.Parse(textBox1.Text) - (40 - dif) / 2).ToString();
            }

            New_Word(int.Parse(textBox1.Text));
        }

        List<Tuple<string, int>> valid_words = new List<Tuple<string, int>>();

        private void Evaluate_All_Word()
        {

            using (StringReader sr = new StringReader(Properties.Resources.English_Word_List))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    if (dictionary.ContainWord(s))
                    {
                        valid_words.Add(new Tuple<string, int>(s, dictionary.Get_Positions_Records(s).Sum(x => x.Positions.Count)));
                    }
                }
            }
            valid_words.Sort((x, y) => y.Item2.CompareTo(x.Item2));
        }

        Random r = new Random();
        private void New_Word(int value)
        {
            if (valid_words.Count < 400)
            {
                MessageBox.Show("Novels not enought. Required more language data to generate.");
                return;
            }
            if (value + 200 > valid_words.Count)
                value = valid_words.Count - 200;
            if (value < 200)
                value = 200;
            textBox1.Text = value.ToString();

            guess = valid_words[value - 200 + (dif = r.Next(400))].Item1;
            dif /= 10;


            richTextBox1.Text = "";
            richTextBox1.SelectionIndent = 0;


            var record_list = dictionary.Get_Positions_Records(guess);
            richTextBox1.SelectionIndent = 4;
            var rep = record_list.ToList();


            int i = rep.Sum(x => x.Positions.Count());

            bool[] select = new bool[i]; //let the hints be at most 20 sentences
            for (int k = 0; k < i; k++) select[k] = k < 20 ? true : false;
            select = select.OrderBy(x => r.Next()).ToArray();

            int n = 0;
            foreach (var records in rep)
            {
                List<DataTypes.Position> pos_list = new List<DataTypes.Position>();
                foreach (var pos in records.Positions)
                    if (select[n++])
                        pos_list.Add(pos);

                foreach (var refline in records.Novel.Get_Reference_Sentences(pos_list))
                {
                    string s = refline.Item2;
                    s = s.Replace(guess, guess[0] + "____" + guess[guess.Count() - 1]);
                    s = s.Replace(guess[0].ToString().ToUpper() + guess.Substring(1), guess[0].ToString().ToUpper() + "____" + guess[guess.Count() - 1]);
                    richTextBox1.AppendText("  " + s + "\n");
                    richTextBox1.SelectionAlignment = HorizontalAlignment.Right;
                    richTextBox1.AppendText(string.Format("-{0}, Chapter {1}, Paragraph {2}\n\n",
                                                        records.Novel.Name, refline.Item1.Chapter,
                                                        refline.Item1.Paragraph));
                    richTextBox1.SelectionAlignment = HorizontalAlignment.Left;
                }
            }

            richTextBox1.SelectAll();
            richTextBox1.SelectionFont = new Font("Cambria", 16f);
            richTextBox1.Select(0, 0);
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                Score();
        }

        private void Score()
        {
            if (textBox2.Text == guess)
            {
                textBox1.Text = (int.Parse(textBox1.Text) + dif).ToString();
                New_Word(int.Parse(textBox1.Text));
            }
            else
            {
                textBox1.Text = (int.Parse(textBox1.Text) - 1).ToString();
            }
            textBox2.Text = "";

        }
    }
}
