using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Main.DataTypes
{
    public interface IDictionary
    {
        IReadOnlyList<string> Words_List { get; }
        IReadOnlyList<INovel> Novels_List { get; }
        IReadOnlyList<IRecords> Get_Positions_Records(string volc);
        IReadOnlyList<IRecords> Get_Positions_Records(INovel novel);
        IRecords Get_Positions_Records(string volc, INovel novel);
        void LoadNovel(string dic_path, string ref_path);
        void ParseNovel(INovel_Entry entry, string output_path);
        bool ContainWord(string word);
        void RemoveAllNovel();
    }
    public interface INovel
    {
        string DictionaryPath { get; }
        string ReferencePath { get; }
        int ID { get; }
        string Name { get; }
        int ParagraphCount { get; }
        int LineCount { get; }
        int WordCount { get; }

        IReadOnlyList<string> Words_List { get; }
        IReadOnlyList<IRecords> Records_List { get; }
        IRecords Get_Positions_Records(string volc);
        IEnumerable<Tuple<Position, string>> Get_Reference_Sentences(IEnumerable<Position> positions);
    }
    public interface IRecords
    {
        string Word { get; }
        INovel Novel { get; }
        IReadOnlyList<Position> Positions { get; }
    }


    public struct Position
    {
        public static Machine.IPositionEncoder Encoder = Machine.PositionEncoding.Encoder;

        private byte[] data;
        public int Chapter
        {
            get { return Encoder.GetChapter(data); }
        }
        public int Paragraph
        {
            get { return Encoder.GetParagraph(data); }
        }
        public int Line
        {
            get { return Encoder.GetLine(data); }
        }
        public string DataString
        {
            get { return Encoder.GetDataString(data); }
        }
        public Position(int chap, int para, int line)
        {
            data = new byte[Encoder.ByteSize];
            Encoder.SetChapter(ref data, chap);
            Encoder.SetParagraph(ref data, para);
            Encoder.SetLine(ref data, line);
        }
        public Position(string datastring)
        {
            data = new byte[Encoder.ByteSize];
            Encoder.SetDataString(ref data, datastring);
        }

        public static int Compare(Position p1, Position p2)
        {
            if (p1.Chapter > p2.Chapter) return 1;
            else if (p1.Chapter < p2.Chapter) return -1;

            if (p1.Paragraph > p2.Paragraph) return 1;
            else if (p1.Paragraph < p2.Paragraph) return -1;

            if (p1.Line > p2.Line) return 1;
            else if (p1.Line < p2.Line) return -1;

            return 0;
        }

        public static bool IsLargerOrEqual(Position p1, Position p2)
        {
            return Compare(p1, p2) >= 0;
        }
    }


    public class Dictionary : IDictionary
    {
        private List<Novel> novels_list = new List<Novel>();
        private List<string> words_list = new List<string>();

        private Dictionary<string, List<Records>> Word_Positions { get; set; } = new Dictionary<string, List<Records>>();


        IReadOnlyList<string> IDictionary.Words_List => words_list.AsReadOnly();

        IReadOnlyList<INovel> IDictionary.Novels_List => novels_list.AsReadOnly();

        IReadOnlyList<IRecords> IDictionary.Get_Positions_Records(string volc)
        {
            return Word_Positions[volc].AsReadOnly();
        }

        IReadOnlyList<IRecords> IDictionary.Get_Positions_Records(INovel novel)
        {
            return novel.Records_List;
        }

        IRecords IDictionary.Get_Positions_Records(string volc, INovel novel)
        {
            return novel.Get_Positions_Records(volc);
        }

        void IDictionary.LoadNovel(string dic_path, string ref_path)
        {
            Novel novel = new Novel()
            {
                ReferencePath = ref_path,
                DictionaryPath = dic_path
            };

            novels_list.Add(novel);

            string s;
            using (StreamReader sr = new StreamReader(dic_path))
                while ((s = sr.ReadLine()) != null)
                {
                    string[] para = s.Split();
                    switch (para[0])
                    {
                        case "W": //input sets of word position //W <word> (Chap-Para-Sent)
                            if (!novel.Words_Lookup.ContainsKey(para[1]))
                                novel.Words_Lookup[para[1]] = new Records(para[1], novel);

                            for (int i = 2; i < para.Count(); i++)
                            {
                                Position pos = new Position(para[i]);
                                novel.Words_Lookup[para[1]].positions.Add(pos);
                            }

                            if (!Word_Positions.ContainsKey(para[1]))
                            {
                                words_list.Add(para[1]);
                                Word_Positions[para[1]] = new List<Records>();
                            }
                            Word_Positions[para[1]].Add(novel.Words_Lookup[para[1]]);

                            break;

                        case "F": //input sets of file hash
                            novel.Filehashes[para[1]] = para[2];
                            break;
                        case "T": //total words
                            novel.WordCount = int.Parse(para[1]);
                            break;
                        case "N":
                            novel.Name = s.Substring(para[0].Length + 1);
                            break;
                    }
                }
        }

        void IDictionary.ParseNovel(INovel_Entry entry, string output_path)
        {
            if (output_path == null) output_path = Properties.Settings.Default.Folder;
            string target_path = Path.Combine(output_path, entry.name);

            Novel novel = new Novel()
            {
                ReferencePath = target_path + ".ref",
                DictionaryPath = target_path + ".dic",
                Name = entry.name,
            };
            novels_list.Add(novel);

            using (StreamWriter swref = new StreamWriter(novel.ReferencePath))
            using (StreamWriter swpos = new StreamWriter(novel.DictionaryPath))
            {
                for (int chap = 0; chap < entry.Contents.Count; chap++)
                {
                    string[] splits = entry.Contents[chap].Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int para = 0; para < splits.Length; para++)
                    {
                        if (string.IsNullOrWhiteSpace(splits[para]))
                            continue;
                        string[] sents = splits[para].Split(new char[] { '?', '.', '!' });
                        int i = 0;
                        foreach (char c in splits[para])
                            if (c == '?' || c == '.' || c == '!')
                                sents[i++] += c;
                        for (int line = 0; line < sents.Length; line++)
                        {
                            Position p = new Position(chap, para, line);
                            foreach (string word in ParseSentence(sents[line]))
                            {
                                if (!novel.Words_Lookup.ContainsKey(word))
                                {
                                    novel.Words_Lookup[word] = new Records(word, novel);
                                    novel.words_list.Add(word);
                                }
                                novel.Words_Lookup[word].positions.Add(p);
                            }
                            swref.WriteLine(p.DataString + " " + sents[line]);
                        }
                    }
                }
                int total = 0;
                foreach (string word in novel.words_list)
                {
                    swpos.WriteLine("W {0} {1}", word, string.Join(" ",
                        novel.Words_Lookup[word].positions.ConvertAll(x => x.DataString)));
                    if (!Word_Positions.ContainsKey(word))
                    {
                        words_list.Add(word);
                        Word_Positions[word] = new List<Records>();
                    }
                    Word_Positions[word].Add(novel.Words_Lookup[word]);
                    total += novel.Words_Lookup[word].Positions.Count;
                }
                swpos.WriteLine("T " + total);
                swpos.WriteLine("N " + novel.Name);

                novel.WordCount = total;
            }

        }
        private static IEnumerable<string> ParseSentence(string s)
        {
            string temp = "";
            HashSet<string> set = new HashSet<string>();
            foreach (char c in s)
            {
                if (c == ' ')
                {
                    if (temp.Length == 0)
                        continue;
                    if (temp.Length > 1 && temp[0] == '\'' && temp[temp.Length - 1] == '\'') //'quote' -> quote
                        temp.Substring(1, temp.Length - 2);
                    set.Add(temp.ToLower());
                    temp = "";
                }
                else if (char.IsLetterOrDigit(c) || c == '\'')
                    temp += c;
            }
            if (temp.Length != 0)
            {
                if (temp.Length > 1 && temp[0] == '\'' && temp[temp.Length - 1] == '\'') //'quote' -> quote
                    temp.Substring(1, temp.Length - 2);
                set.Add(temp.ToLower());
            }
            return set;
        }

        bool IDictionary.ContainWord(string word)
        {
            return Word_Positions.ContainsKey(word);
        }

        void IDictionary.RemoveAllNovel()
        {
            novels_list.Clear();
            words_list.Clear();
            Word_Positions.Clear();
        }
    }

    internal class Novel : INovel
    {
        public string DictionaryPath { get; set; }
        public string ReferencePath { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public int ParagraphCount { get; set; }
        public int LineCount { get; set; }
        public int WordCount { get; set; }

        //Data that will be saved
        public List<string> words_list = new List<string>();
        public List<Records> record_list = new List<Records>();
        public Dictionary<string, string> Filehashes { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, Records> Words_Lookup { get; set; } = new Dictionary<string, Records>();

        IReadOnlyList<string> INovel.Words_List => words_list.AsReadOnly();

        IReadOnlyList<IRecords> INovel.Records_List => record_list.AsReadOnly();

        //func
        IEnumerable<Tuple<Position, string>> INovel.Get_Reference_Sentences(IEnumerable<Position> positions)
        {
            //positions = positions.OrderByDescending(x => x.DataString);
            Func<Position, Position> LeftBound, RightBound;
            switch (Properties.Settings.Default.Search_option)
            {
                case 1:
                    LeftBound = (x) => x;
                    RightBound = (x) => x;
                    break;
                case 2:
                    LeftBound = (x) => new Position(x.Chapter, x.Paragraph, x.Line - 1);
                    RightBound = (x) => new Position(x.Chapter, x.Paragraph, x.Line + 1);
                    break;
                case 3:
                    LeftBound = (x) => new Position(x.Chapter, x.Paragraph, 0);
                    RightBound = (x) => new Position(x.Chapter, x.Paragraph, 1 << 20);
                    break;
                default:
                    LeftBound = (x) => x;
                    RightBound = (x) => x;
                    break;
            }
            string s;
            if (ReferencePath != null)
                using (StreamReader sr = new StreamReader(ReferencePath))
                    foreach (Position p in positions)
                    {
                        Position left = LeftBound(p), right = RightBound(p); //todo
                        string yields = "";
                        while ((s = sr.ReadLine()) != null)
                        {
                            string[] para = s.Split(new char[] { ' ' }, 2);
                            Position val = new Position(para[0]);

                            if (Position.IsLargerOrEqual(val, left)) //left <= val
                            {
                                if (Position.IsLargerOrEqual(right, val)) //left <= val <= right
                                    yields += para[1];
                                else //right < val
                                {
                                    if (yields != "")
                                        yield return new Tuple<Position, string>(p, yields);
                                    break;
                                }
                            }
                        }
                    }
        }
        IRecords INovel.Get_Positions_Records(string volc)
        {
            if (Words_Lookup.ContainsKey(volc)) return Words_Lookup[volc];
            else return null;
        }

    }

    internal class Records : IRecords
    {
        public List<Position> positions;

        public string Word { get; set; }
        public INovel Novel { get; set; }
        public IReadOnlyList<Position> Positions { get { return positions; } }

        public Records(string word, INovel novel)
        {
            Word = word;
            Novel = novel;
            positions = new List<Position>();
        }
    }


    public interface INovel_Entry
    {
        List<string> Titles { get; }
        List<string> Contents { get; }
        string filepath { get; }
        string name { get; }
    }

    public class Novel_Entry : INovel_Entry
    {
        public List<string> Titles { get; set; } = new List<string>();
        public List<string> Contents { get; set; } = new List<string>();
        public string filepath { get; set; }
        public string name { get; set; }
    }
}
