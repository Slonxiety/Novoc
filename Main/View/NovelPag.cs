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
using System.Security.Cryptography;

namespace Main.View
{
    public partial class NovelPage : Form
    {
        public DataTypes.Dictionary dictionary { get; set; }

        public NovelPage(DataTypes.Dictionary dict)
        {
            InitializeComponent();
            dictionary = dict;
            //novelrecorder = new Controller.NovelRecorder(Dictionary);
        }
        
        
        Controller.FileParser fileparser = new Controller.FileParser(Constant.NovelFolder);
        Controller.NovelRecorder novelrecorder;

        private void button1_Click(object sender, EventArgs e) //Add
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string s in openFileDialog1.FileNames)
                {
                    DataTypes.Novel novel = DataTypes.Novel.Create(s, dictionary);

                    if (!Directory.Exists(Constant.NovelFolder)) Directory.CreateDirectory(Constant.NovelFolder);

                    fileparser.ParseToXml(s);

                    string resultfile = fileparser.GetResultFilePath(s);

                    novelrecorder.Register(resultfile);

                    MessageBox.Show("File save at: " + resultfile);
                    //ADD WORD TO DICTIONARY; COUNT LINE, PARAGRAPH, WORD
                    return;
                    
                }
            }
        }

        private void Novel_Load(object sender, EventArgs e)
        {
            treeView1.BeginUpdate();
            
            treeView1.Nodes.Add("Novels");
            foreach (var novel in dictionary.Novels)
            {
                Add_Novel(novel);
            }
            treeView1.EndUpdate();
        }

        Dictionary<TreeNode, DataTypes.Novel> NovelLookup = new Dictionary<TreeNode, DataTypes.Novel>();

        private void Add_Novel (DataTypes.Novel novel)
        {
            TreeNode now = treeView1.Nodes[0];
            foreach (string p in novel.FilePath.Split('\\'))
            {
                bool not_found = true;
                foreach (TreeNode n in now.Nodes)
                {
                    if (NovelLookup.ContainsKey(n)) continue; //end_node
                    if (n.Text == p)
                    {
                        not_found = false;
                        now = n;
                        break;
                    }
                }
                if (not_found) now = now.Nodes.Add(p);
            }
            string name = now.Text;
            if (now.Nodes.Count > 0) //is directory
                now = now.Parent.Nodes.Add(name);
            NovelLookup[now] = novel;
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }
}
