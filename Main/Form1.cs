using System.Net;
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

namespace Main
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        DataTypes.IDictionary dictionary;

        //const
        Color butselect = Color.Red;
        Color butnormal = Color.FromArgb(128, 128, 255);

        //UI
        Button selected = null;
        Dictionary<Button, Form> lookup; //init in load
        private void Button_Click (object sender, EventArgs e)
        {
            //deselect
            if (selected != null)
            {
                selected.BackColor = butnormal;
                lookup[selected].Visible = false;
            }

            //select
            if (selected != sender)
            {
                selected = (Button)sender;
                lookup[selected].BringToFront();
                lookup[selected].Visible = true;
                selected.BackColor = butselect;
            }
            else
                selected = null;

            if (selected != null)
                label2.Visible = false;
            else
                label2.Visible = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //create dictionary
            dictionary = new DataTypes.Dictionary();
            
            //initialize Interface
            lookup = new Dictionary<Button, Form>()
            {
                [button4] = new View.SearchPage(dictionary),
                [button5] = new View.NovelPage(dictionary),
                [button2] = new View.SettingPage(dictionary),
                [button1] = new View.QuizPage(dictionary)
            };

            foreach (var pair in lookup)
            {
                pair.Key.Click += new EventHandler(Button_Click);
                pair.Key.BackColor = butnormal;

                Form f = pair.Value;
                f.Visible = false;
                f.TopLevel = false;
                f.FormBorderStyle = FormBorderStyle.None;
                f.Dock = DockStyle.Fill;
                panel1.Controls.Add(f);
            }

        }
    }

}