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
    public class Constant
    {
        public static readonly string ExeFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        public static readonly string NovocFolder = Path.Combine(ExeFolder, @"Novoc\");
        public static readonly string NovelFolder = Path.Combine(NovocFolder, @"Novels\");
        public static readonly string DictionaryFile = Path.Combine(NovocFolder, @"Dictionary.txt");
    }
}
