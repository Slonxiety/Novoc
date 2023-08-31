﻿using System;
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
        public DataTypes.IDictionary dictionary { get; set; }

        public NovelPage(DataTypes.IDictionary dict)
        {
            InitializeComponent();
            dictionary = dict;
        }
        bool complete_loading = false;
        
        private async void button1_Click(object sender, EventArgs e) //Add
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string s in openFileDialog1.FileNames)
                {
                    /*
                    Task<DataTypes.INovel_Entry> load_task = new Task<DataTypes.INovel_Entry>
                        (() => Machine.NovelReaderFactory.Get(Path.GetExtension(s)).Read(s));
                    var entry = await load_task.;

                    Task parse_task = new Task(() => dictionary.ParseNovel(entry, 
                        Machine.NovelLoader.GetNovelLoader(dictionary).FolderPath));

                    await parse_task;
                    */
                    await Task.Run(() =>
                    {
                        var entry = Machine.NovelReaderFactory.Get(Path.GetExtension(s)).Read(s);
                        dictionary.ParseNovel(entry,
                            Machine.NovelLoader.GetNovelLoader(dictionary).FolderPath);

                        Add_Novel(dictionary.Novels_List.Last());
                    });
                }
                treeView1.ExpandAll();
                MessageBox.Show("Complete!\n" + openFileDialog1.FileNames.Length + " novel(s) had been added.");
            }
        }
        
        private void Novel_Load(object sender, EventArgs e)
        {
            treeView1.BeginUpdate();
            
            treeView1.Nodes.Add("Novels");
            foreach (var novel in dictionary.Novels_List)
            {
                Add_Novel(novel);
            }
            treeView1.EndUpdate();
            treeView1.ExpandAll();

            Machine.NovelLoader.GetNovelLoader(dictionary).NovelLoadedEvent +=
                (_sender, _arg) =>
                {
                    Add_Novel(_arg.Novel);
                };
            //timer1.Start();
        }

        Dictionary<TreeNode, DataTypes.INovel> NovelLookup = new Dictionary<TreeNode, DataTypes.INovel>();

        private void Add_Novel (DataTypes.INovel novel)
        {
            TreeNode now = treeView1.Nodes[0];
            int cutlength = Properties.Settings.Default.Folder.Length;
            string path = novel.DictionaryPath.Substring(cutlength + 1);
            path = path.Remove(path.Length - 4);
            foreach (string p in path.Split('\\'))
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
                if (not_found)
                {
                    treeView1.Invoke((MethodInvoker)delegate
                    {
                        now = now.Nodes.Add(p);
                    });
                }
            }
            string name = now.Text;
            if (now.Nodes.Count > 0) //is directory
                now = now.Parent.Nodes.Add(name);
            NovelLookup[now] = novel;
            word_count += novel.WordCount;

            label1.Invoke((MethodInvoker)delegate
            {
                label1.Text = string.Format("Loaded: (Novel: {0}) (Word: {1})", NovelLookup.Count, word_count);
            });
            
        }

        int word_count;

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                NovelForm nf = new NovelForm(openFileDialog1.FileName);
                nf.Show();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.Visible == false) return;
            List<DataTypes.INovel> list = new List<DataTypes.INovel>(dictionary.Novels_List);
            foreach (var novel in list)
            {
                if (!NovelLookup.ContainsValue(novel))
                    Add_Novel(novel);
            }
            if (complete_loading) timer1.Stop();
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (NovelLookup.ContainsKey(e.Node))
            {
                var novel = NovelLookup[e.Node];
                Form page = new View.NovelForm(novel.ReferencePath);
                page.Show();
            }
        }
    }
}
