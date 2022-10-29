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
        private System.Timers.Timer timer=null;
        private Semaphore semaphore;
        public List<string> Drives { get; private set; }
        public List<string> ForbiddenWords { get; private set; }
        public List<ForbiddenFile> ForbiddenFiles { get; private set; }
        public List<string> Exceptions { get; private set; }
        public Dictionary<string, int> WordsStats { get; private set; }
        private bool _Cancel;
        public bool Cancel { get { return _Cancel; } set { _Cancel = value; if(value) Truncate(); } }
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
        public int Progress { get { return _Progress; } private set { _Progress = value; ProgressChanged?.Invoke(); if (value == 100) { timer.Stop(); if (CreateLog) GenerateLog(); Cancel = true;  } } }
        private int _ObservedFiles;
        public int ObservedFiles 
        { 
            get 
            { 
                return _ObservedFiles;
            } 
            set
            {
                _ObservedFiles = value; 
                if(FilePaths.Count!=0)
                    Progress = (int)((_ObservedFiles*1.0 / FilePaths.Count)*100); 
            } 
        }
        
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
            if (timer != null)
            {
                timer.Dispose();
            }
            Exceptions = new List<string>();
            WorkTime = 0;
            Progress = 0;
            Cancel = false;
            semaphore = new Semaphore(1,2);
            CreateLog = false;
            DestinationDirectory = String.Empty;
        }

        private void CountAllFiles()
        {
            CountingFiles = true;
            ObservedFiles = 0;
            foreach (var drive in Drives)
            {
                if (!Pause&&!Cancel)
                {
                    Count(drive);
                }
                else if(Pause&&!Cancel)
                {
                    while(Pause)
                    {

                    }
                    if (Cancel)
                        return;
                }
                else if(Cancel&&Pause)
                {
                    return;
                }
            }
        }

        private void Count(object path)
        {
            try
            {
                foreach (var item in Directory.GetFiles(path.ToString()).Where(x => Path.GetExtension(x) == ".txt"))
                {
                    if (!Pause && !Cancel)
                    {
                        FilePaths.Add(item);
                    }
                    else if (Pause && !Cancel)
                    {
                        while (Pause)
                        {

                        }
                        if (Cancel)
                            return;
                    }
                    else if (Cancel && Pause)
                    {
                        return;
                    }
                }                
                foreach (var directory in Directory.GetDirectories(path.ToString()))
                {
                    if (!Pause && !Cancel)
                    {
                        Count(directory);
                    }
                    else if (Pause && !Cancel)
                    {
                        while (Pause)
                        {

                        }
                        if (Cancel)
                            return;
                    }
                    else if (Cancel && Pause)
                    {
                        return;
                    }
                }
            }
            catch 
            {
                return;
            }
        }

        public async void StartSearching()
        {
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += (obj,ea) =>
            {
                WorkTime++;
                TimeChanged?.Invoke();
            };
            timer.Start();
            if (ForbiddenWords.Count == 0||string.IsNullOrWhiteSpace(DestinationDirectory))
                return;
            await Task.Run(CountAllFiles);
            ThreadPool.SetMaxThreads(10,10);
            CountingFiles = false;
            foreach (var file in FilePaths)
            {
                if(!Pause && !Cancel)
                    ThreadPool.QueueUserWorkItem(FindWordsInFile, file);
                else if (Pause && !Cancel)
                {
                    while (Pause)
                    {

                    }
                    if (Cancel)
                        return;
                }
                else if (Cancel && Pause)
                {
                    return;
                }
            }
        }

        private void FindWordsInFile(object path)
        {
            try
            {
                if (!File.Exists(path.ToString()))
                    return;
                string result = "";
                using (Stream s = new FileStream(path.ToString(), FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        while (!sr.EndOfStream)
                        {
                            if (!Pause && !Cancel)
                                result += sr.ReadLine();
                            else if (Pause && !Cancel)
                            {
                                while (Pause)
                                {

                                }
                                if (Cancel)
                                    return;
                            }
                            else if (Cancel && Pause)
                            {
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
                            if (WordsStats.ContainsKey(word))
                                WordsStats[word]++;
                            else
                                WordsStats[word] = 1;
                            ThreadPool.QueueUserWorkItem(CopyFileWithForbiddenWord, path);
                            return;
                        }
                    }
                    else if (Pause && !Cancel)
                    {
                        while (Pause)
                        {

                        }
                        if (Cancel)
                            return;
                    }
                    else if (Cancel && Pause)
                    {
                        return;
                    }
                }
                semaphore.WaitOne();
                ObservedFiles++;
                semaphore.Release();
            }
            catch
            {
                semaphore.WaitOne();
                ObservedFiles++;
                semaphore.Release();
            }
        }

        private void CopyFileWithForbiddenWord(object path)
        {
            string dest = DestinationDirectory + "\\" +Path.GetFileName(path.ToString());
            File.Copy(path.ToString(),dest,true);
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
                        else if (Pause && !Cancel)
                        {
                            while (Pause)
                            {

                            }
                            if (Cancel)
                                return;
                        }
                        else if (Cancel && Pause)
                        {
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
                else if (Pause && !Cancel)
                {
                    while (Pause)
                    {

                    }
                    if (Cancel)
                        return;
                }
                else if (Cancel && Pause)
                {
                    return;
                }
            }
            using (var stream=new FileStream(DestinationDirectory+"\\"+Path.GetFileNameWithoutExtension(path.ToString())+" - observed" + Path.GetExtension(path.ToString()),FileMode.Create))
            {
                using (var sw=new StreamWriter(stream))
                {
                    sw.Write(content);
                }
            }
            semaphore.WaitOne();
            ObservedFiles++;
            semaphore.Release();
        }

        private void GenerateLog()
        {
            using (Stream s = new FileStream(DestinationDirectory+"\\log.txt",FileMode.Create))
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
                        sw.WriteLine($"\t\t\t{item.Key} --- {item.Value}");
                    }
                    sw.WriteLine($"Searching lasted {(WorkTime*1.0/60)} minutes");
                }
            }
        }

    }
}
