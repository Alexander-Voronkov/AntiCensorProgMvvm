using MVVM_Ex.Commands;
using MVVM_Ex.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MVVM_Ex.ViewModels
{
    internal class ExceptionFilesViewModel:BaseViewModel
    {
        public ObservableCollection<string> Exceptions { get; set; }
        public AddExceptionCommand AddExceptionCommand { get; set; }
        public RemoveExceptionCommand RemoveExceptionCommand { get; set; }
        private string _ChosenException = null;
        public string ChosenException { get { return _ChosenException; } set { Set(ref _ChosenException, value); } }
        public ExceptionFilesViewModel()
        {
            Exceptions = new ObservableCollection<string>();
            AddExceptionCommand = new AddExceptionCommand(AddException);
            RemoveExceptionCommand = new RemoveExceptionCommand(RemoveException, CanRemoveException);
        }
        private void AddException(object param)
        {
            var fbd = new OpenFileDialog();
            fbd.Filter = "Text files|*.txt";
            if(fbd.ShowDialog()==DialogResult.OK)
            {
                if (File.Exists(fbd.FileName))
                {
                    if (!Exceptions.Contains(fbd.FileName))
                    {
                        Exceptions.Add(fbd.FileName);
                    }
                }
            }
        }

        private void RemoveException(object param)
        {
            Exceptions.Remove(ChosenException);
        }

        private bool CanRemoveException(object param)
        {
            return Exceptions.Count > 0;
        }
    }
}
