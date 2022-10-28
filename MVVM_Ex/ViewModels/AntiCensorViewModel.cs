using MVVM_Ex.Models;
using MVVM_Ex.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using MVVM_Ex.Views;
using MVVM_Ex.Dialogs;
using System.Collections.Specialized;
using System.Windows.Forms;

namespace MVVM_Ex.ViewModels
{
    internal class AntiCensorViewModel:BaseViewModel
    {
        AntiCensorModel application;
        private bool _IsStarted=false;
        public bool IsStarted { get{return _IsStarted;}set { _IsStarted = value; IsNotStarted = !value; } }

        private bool _IsPaused = false;
        public bool IsPaused { get { return _IsPaused; } set { _IsPaused = value; application.Pause = value; } }
        private int _Progress;
        public int Progress { get { return _Progress; } set { Set(ref _Progress, value); if (value == 100) { IsStarted = false; } } }
        public ObservableCollection<DriveModel> SelectedDrives { get; set; }
        public ObservableCollection<string> ForbiddenWords { get; set; }
        private int _WorkTime;
        public int WorkTime { get { return _WorkTime; } set { Set(ref _WorkTime, value); } }
        public StopAppCommand StopCommand { get; set; }
        public PauseAppCommand PauseCommand { get; set; }
        public ResumeAppCommand ResumeCommand { get; set; }
        private bool _Marquee;
        public bool Marquee { get { return _Marquee; } set { Set(ref _Marquee, value); } }
        public StartAppCommand StartCommand { get; set; }
        public ExceptionFilesCommand SelectExceptionsCommand { get; set; }
        public LoadWordsFromFileCommand LoadFromFileCommand { get; set; }
        public DestinationFolderCommand SelectDestinationCommand { get; set; }
        private bool _IsNotStarted=true;
        public bool IsNotStarted { get { return _IsNotStarted; }set { Set(ref _IsNotStarted, value); } }

        private string _SelectedForbiddenWord;
        public string SelectedForbiddenWord { get { return _SelectedForbiddenWord; } set { Set(ref _SelectedForbiddenWord, value); } }
        public AddWordCommand AddWordCommand { get; set; }
        public RemoveWordCommand RemoveWordCommand { get; set; }
        public ObservableCollection<string> ExceptionFiles { get; set; }
        private bool _GenerateLog;
        public bool GenerateLog { get { return _GenerateLog; } set { Set(ref _GenerateLog, value); application.CreateLog = value; } }
        public AntiCensorViewModel()
        {
            application = new AntiCensorModel();
            SelectedDrives = new ObservableCollection<DriveModel>(application.Drives.Select(x=>new DriveModel() { DrivePath=x,IsSelected=false}));
            ForbiddenWords = new ObservableCollection<string>();
            ForbiddenWords.CollectionChanged += ForbiddenWordsChanged;
            application.ProgressChanged += OnProgressChanged;
            application.TimeChanged += OnTimerChange;
            ExceptionFiles = new ObservableCollection<string>();
            ExceptionFiles.CollectionChanged += ExceptionsChanged;
            StopCommand = new StopAppCommand(StopApp,CanStopExecute);
            PauseCommand = new PauseAppCommand(PauseApp,CanPauseExecute);
            StartCommand = new StartAppCommand(StartApp, CanStartExecute);
            AddWordCommand = new AddWordCommand(AddWord, CanAddWord);
            SelectExceptionsCommand = new ExceptionFilesCommand(SelectExceptionFiles,CanSelectExceptionFiles);
            RemoveWordCommand = new RemoveWordCommand(RemoveWord,CanRemoveWord);
            LoadFromFileCommand = new LoadWordsFromFileCommand(LoadForbiddenWordsFromFile, CanLoadWords);
            SelectDestinationCommand = new DestinationFolderCommand(SelectDestinationFolder, CanSelectDestinationFolder);
            ResumeCommand = new ResumeAppCommand(ResumeApp, CanResumeApp);
            application.CountingChanged += OnCountingChange;
        }

        private void ResumeApp(object param)
        {
            IsPaused = false;
        }

        private bool CanResumeApp(object param)
        {
            return IsPaused;
        }

        private void OnCountingChange()
        {
            Marquee = application.CountingFiles;
        }

        private void StopApp(object param)
        {
            application.Cancel = true;
            IsStarted = false;
            IsPaused = false;
        }

        private void PauseApp(object param)
        {
            IsPaused = true;
        }

        private void OnProgressChanged()
        {
            Progress = application.Progress;
        }
        private void StartApp(object param)
        {
            application.Drives.Clear();
            application.Drives.AddRange(SelectedDrives.Where(x => x.IsSelected).Select(x => x.DrivePath));
            Task.Run(application.StartSearching);
            IsStarted = true;
        }

        private bool CanStartExecute(object param)
        {
            return ForbiddenWords.Count>0&&SelectedDrives.Where(x=>x.IsSelected).Count()>0&&!string.IsNullOrEmpty(application.DestinationDirectory)&&!IsStarted;
        }

        private bool CanStopExecute(object param)
        {
            return IsStarted;
        }

        private bool CanPauseExecute(object param)
        {
            return IsStarted&&!IsPaused;
        }

        private void OnTimerChange()
        {
            WorkTime = application.WorkTime;
        }

        private void SelectExceptionFiles(object param)
        {
            ExceptionFiles=new ObservableCollection<string>(DialogService.SelectExceptions(ExceptionFiles.ToList()));
            ExceptionFiles.CollectionChanged += ExceptionsChanged;
        }

        private void ExceptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            application.Exceptions.Clear();
            application.Exceptions.AddRange(ExceptionFiles);
        }

        private void ForbiddenWordsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            application.ForbiddenWords.Clear();
            application.ForbiddenWords.AddRange(ForbiddenWords);
        }

        private bool CanSelectExceptionFiles(object param)
        {
            return !IsStarted;
        }

        private void AddWord(object param)
        {
            var res = DialogService.AddWord(ForbiddenWords.ToList());
            ForbiddenWords.Add(res);
        }

        private bool CanAddWord(object param)
        {
            return !IsStarted;
        }

        private void RemoveWord(object param)
        {
            ForbiddenWords.Remove(SelectedForbiddenWord);
        }

        private bool CanRemoveWord(object param)
        {
            return !string.IsNullOrEmpty(SelectedForbiddenWord);
        }

        private void LoadForbiddenWordsFromFile(object param)
        {
            var qq = DialogService.LoadForbiddenWords();
            if (qq == null)
                return;
            foreach (var item in qq)
            {
                ForbiddenWords.Add(item);
            }            
        }

        private bool CanLoadWords(object param)
        {
            return !IsStarted;
        }

        private void SelectDestinationFolder(object param)
        {
            string res= DialogService.SelectDestinationFolder();
            if (res == null)
                return;
            application.DestinationDirectory = res;
        }

        private bool CanSelectDestinationFolder(object param)
        {
            return !IsStarted;
        }
    }
}
