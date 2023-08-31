using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Main.Machine
{
    public class NovelLoadedArgs
    {
        public DataTypes.INovel Novel { get; }
        public NovelLoadedArgs(DataTypes.INovel novel) { Novel = novel; }
    }
    public class NovelLoader
    {
        public event EventHandler<NovelLoadedArgs> NovelLoadedEvent;

        private static Dictionary<DataTypes.IDictionary, NovelLoader> pool = new Dictionary<DataTypes.IDictionary, NovelLoader>();
        public static NovelLoader GetNovelLoader(DataTypes.IDictionary dictionary)
        {
            if (pool.ContainsKey(dictionary)) return pool[dictionary];
            else return pool[dictionary] = new NovelLoader(dictionary);
        }
        public string FolderPath { get; private set; }
        private DataTypes.IDictionary dictionary;
        private NovelLoader(DataTypes.IDictionary dictionary)
        {
            this.dictionary = dictionary;
        }
        public void Clear_Dictionary()
        {
            Stop_Loading();

            dictionary.RemoveAllNovel();
        }
        public void Stop_Loading()
        {
            cancellationTokenSource?.Cancel();
        }

        CancellationTokenSource cancellationTokenSource = null;
        public void Load_Dictionary(string path)
        {
            Stop_Loading();
            var tokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(() => Async_Load(path, tokenSource.Token), tokenSource.Token);
            cancellationTokenSource = tokenSource;
        }

        private void Async_Load(string path, CancellationToken canceltoken)
        {
            List<Tuple<string, string>> filepairs = new List<Tuple<string, string>>();
            Dictionary<string, string> pairmatcher = new Dictionary<string, string>();
            if (Directory.Exists(path))
                foreach (var v in Directory.GetFiles(path))
                {
                    string name = Path.GetFileNameWithoutExtension(v);
                    if (!pairmatcher.ContainsKey(name))
                    {
                        pairmatcher.Add(name, v);
                        continue;
                    }

                    if (Path.GetExtension(v) == ".dic")
                    {
                        filepairs.Add(new Tuple<string, string>(v, pairmatcher[name]));
                        pairmatcher.Remove(name);
                    }
                    if (Path.GetExtension(v) == ".ref")
                    {
                        filepairs.Add(new Tuple<string, string>(pairmatcher[name], v));
                        pairmatcher.Remove(name);
                    }
                }

            FolderPath = path;
            foreach (var pair in filepairs)
            {
                if (dictionary.Novels_List.Any(x => x.DictionaryPath == pair.Item1))
                    continue;

                if (canceltoken.IsCancellationRequested)
                    return;

                dictionary.LoadNovel(pair.Item1, pair.Item2);
                NovelLoadedEvent?.Invoke(this, new NovelLoadedArgs(dictionary.Novels_List.Last()));
            }
        }
    }
}
