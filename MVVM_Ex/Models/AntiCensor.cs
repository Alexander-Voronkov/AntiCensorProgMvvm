using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MVVM_Ex.Models
{
    internal class AntiCensorModel
    {
        private System.Threading.Timer timer;
        private Mutex mutex;
        public List<string> Drives { get; private set; }
        public List<string> ForbiddenWords { get; private set; }
        public List<ForbiddenFile> ForbiddenFiles { get; private set; }
        public List<string> Exceptions { get; private set; }
        public Dictionary<string, int> WordsStats { get; private set; }
        private bool _Cancel;
        public bool Cancel { get { return _Cancel; } set { _Cancel = value; Pause = value; if(_Cancel) Truncate(); } }
        private bool _Pause;
        public bool Pause { get { return _Pause; } set { _Pause = value; } }
        private bool _CreateLog;
        public bool CreateLog { get { return _CreateLog; } set { _CreateLog = value; } }
        public event Action ProgressChanged;
        public event Action TimeChanged;
        public event Action CountingChanged;
        public List<string> FilePaths { get; set; }
        private int _Progress;
        private bool _CountingFiles=false;
        public bool CountingFiles { get { return _CountingFiles; } set { _CountingFiles = value; CountingChanged?.Invoke(); } }
        public int Progress { get { return _Progress; } private set { _Progress = value; ProgressChanged?.Invoke(); if (value == 100) { timer.Dispose(); if (CreateLog) GenerateLog(); Truncate();  } } }
        private int _ObservedFiles;
        public int ObservedFiles { get { return _ObservedFiles; } set { _ObservedFiles = value; if(FilePaths.Count!=0)Progress = _ObservedFiles / FilePaths.Count; } }
        
        public int WorkTime { get; set; }
        public string DestinationDirectory { get; set; }

        public AntiCensorModel()
        {
            Truncate();
            LoadDrives();   
        }

        private void LoadDrives()
        {
            Drives = DriveInfo.GetDrives().Select(x => x.Name).ToList(); 
        }

        private void Truncate()
        {
            FilePaths = new List<string>();
            ForbiddenWords = new List<string>();
            WordsStats = new Dictionary<string, int>();
            ForbiddenFiles = new List<ForbiddenFile>();
            Exceptions = new List<string>();
            WorkTime = 0;
            Progress = 0;
            Cancel = false;
            mutex = new Mutex(false);
            CreateLog = false;
            DestinationDirectory = String.Empty;
        }

        private void CountAllFiles()
        {
            CountingFiles = true;
            ObservedFiles = 0;
            LoadDrives();
            foreach (var drive in Drives)
            {
                Count(drive);
            }
        }

        private void Count(object path)
        {
            try
            {
                foreach (var item in Directory.GetFiles(path.ToString()).Where(x => Path.GetExtension(x) == ".txt"))
                {
                    FilePaths.Add(item);
                }                
                foreach (var directory in Directory.GetDirectories(path.ToString()))
                {
                    Count(directory);
                }
            }
            catch 
            {
                return;
            }
        }

        public async void StartSearching()
        {
            timer = new Timer((obj) => { WorkTime++;TimeChanged?.Invoke(); },null, TimeSpan.FromSeconds(1),TimeSpan.FromSeconds(1)) ;
            if (ForbiddenWords.Count == 0||string.IsNullOrWhiteSpace(DestinationDirectory))
                return;
            await Task.Run(CountAllFiles);
            CountingFiles = false;
            foreach (var file in FilePaths)
            {
                if(!Pause && !Cancel)
                    ThreadPool.QueueUserWorkItem(FindWordsInFile, file);
                else
                {
                    while(Pause)
                    {

                    }
                    if (Cancel)
                        return;
                }
            }
        }

        private void FindWordsInFile(object path)
        {
            if (!File.Exists(path.ToString()))
                return;
            string result = "";
            using (Stream s=new FileStream(path.ToString(),FileMode.Open))
            {
                using (StreamReader sr=new StreamReader(s))
                {
                    while (!sr.EndOfStream)
                    {
                        if(!Pause && !Cancel)
                        result += sr.ReadLine();
                        else
                        {
                            while(Pause)
                            {

                            }
                            if (Cancel)
                                return;
                        }
                    }
                }
            }
            var words = result.Split('\n', ' ', '.', ',', '-', ':', ';', '?', '!', '\\', '/');
            foreach (var word in words)
            {
                if (!Pause && !Cancel)
                {
                    if (ForbiddenWords.Contains(word))
                    {
                        ThreadPool.QueueUserWorkItem(CopyFileWithForbiddenWord, path);
                        return;
                    }
                }
                else
                {
                    while(Pause)
                    {

                    }
                    if (Cancel)
                        return;
                }
            }
            mutex.WaitOne();
            ObservedFiles++;
            mutex.ReleaseMutex();
        }

        private void CopyFileWithForbiddenWord(object path)
        {
            string dest = DestinationDirectory + Path.GetFileName(path.ToString());
            File.Copy(path.ToString(),DestinationDirectory+Path.GetFileName(path.ToString()));
            string content = "";
            using (var stream=new FileStream(dest,FileMode.Open))
            {
                using (var sr = new StreamReader(stream))
                {
                    while (!sr.EndOfStream)
                    {
                        if (!Pause && !Cancel)
                        {
                            content += sr.ReadLine();
                        }
                        else
                        {
                            while(Pause)
                            {

                            }
                            if (Cancel)
                                return;
                        }
                    }
                }
            }
            foreach (var fword in ForbiddenWords)
            {
                if (!Pause && !Cancel)
                {
                    content = content.Replace(fword, "*******");
                }
                else
                {
                    while (Pause)
                    {

                    }
                    if (Cancel)
                        return;
                }
            }
            using (var stream=new FileStream(DestinationDirectory+Path.GetFileNameWithoutExtension(path.ToString())+" - observed" + Path.GetExtension(path.ToString()),FileMode.Create))
            {
                using (var sw=new StreamWriter(stream))
                {
                    sw.Write(content);
                }
            }
            mutex.WaitOne();
            ObservedFiles++;
            mutex.ReleaseMutex();
        }

        private void GenerateLog()
        {
            using (Stream s = new FileStream("log.txt",FileMode.Create))
            {
                using (StreamWriter sw=new StreamWriter(s))
                {
                    sw.WriteLine("Forbidden files info:");
                    foreach (var item in ForbiddenFiles)
                    {
                        sw.WriteLine(item);
                    }
                    sw.WriteLine("\n\t\tTOP 10 FORBIDDEN WORDS:\n");
                    foreach (var item in WordsStats.OrderByDescending(x=>x.Value).Take(10))
                    {
                        sw.WriteLine($"{item.Key} --- {item.Value}");
                    }
                    sw.WriteLine($"Searching lasted {WorkTime/60} minutes");
                }
            }
        }

    }
}
