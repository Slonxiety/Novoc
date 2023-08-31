using System;
using System.IO;
using System.Windows.Forms;

namespace Main.View
{
    public partial class SettingPage : Form
    {
        private DataTypes.IDictionary dictionary;
        public SettingPage(DataTypes.IDictionary dictionary)
        {
            InitializeComponent();
            this.dictionary = dictionary;
            Set_Default_Folder_Name();
        }
        private void Set_Default_Folder_Name()
        {
            if (Properties.Settings.Default.Folder == "")
            {
                Properties.Settings.Default.Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Novoc");
                Properties.Settings.Default.Save();
            }
            try
            {
                if (!Directory.Exists(Properties.Settings.Default.Folder))
                    Directory.CreateDirectory(Properties.Settings.Default.Folder);
            }
            catch
            {
                Properties.Settings.Default.Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Novoc");
                Properties.Settings.Default.Save();
                if (!Directory.Exists(Properties.Settings.Default.Folder))
                    Directory.CreateDirectory(Properties.Settings.Default.Folder);
            }

            textBox1.Text = Properties.Settings.Default.Folder;
            Set_Folder_Path();
        }
        private void CheckChange(object sender, EventArgs e)
        {
            int result;
            if (radioButton1.Checked) result = 1;
            else if (radioButton2.Checked) result = 2;
            else result = 3;
            Properties.Settings.Default.Search_option = result;
            Properties.Settings.Default.Save();
        }
        private void SettingPage_Load(object sender, EventArgs e)
        {
            switch (Properties.Settings.Default.Search_option)
            {
                case 1:
                    radioButton1.Checked = true;
                    break;
                case 2:
                    radioButton2.Checked = true;
                    break;
                case 3:
                    radioButton3.Checked = true;
                    break;
            }
            radioButton1.CheckedChanged += new EventHandler(CheckChange);
            radioButton2.CheckedChanged += new EventHandler(CheckChange);
            radioButton3.CheckedChanged += new EventHandler(CheckChange);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.Folder = textBox1.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (!Directory.Exists(textBox1.Text))
                if (MessageBox.Show("Folder not existed, create one?\n" + textBox1.Text,
                                    "Folder not existed",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(textBox1.Text);
                        Properties.Settings.Default.Folder = textBox1.Text;
                        Properties.Settings.Default.Save();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                else
                    textBox1.Text = Properties.Settings.Default.Folder;


            Set_Folder_Path();
        }


        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                textBox1_Leave(null, null);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Set_Default_Folder_Name();
            SettingPage_Load(sender, e);
        }

        private void Set_Folder_Path()
        {
            var loader = Machine.NovelLoader.GetNovelLoader(dictionary);

            if (dictionary.Novels_List.Count == 0)
                loader.Load_Dictionary(textBox1.Text);
            else if (textBox1.Text != loader.FolderPath)
            {
                string msg = "The folder has been changed.\nLoad files from the new folder?";
                if (MessageBox.Show(msg, "Load", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                    == DialogResult.Yes)
                {
                    loader.Load_Dictionary(textBox1.Text);

                }
                else
                    textBox1.Text = loader.FolderPath;
            }

        }
    }

}
