using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVM_Ex.Commands;

namespace MVVM_Ex.ViewModels
{
    internal class AddWordViewModel:BaseViewModel
    {
        public DialogAddWordCommand DialogAddWordCommand { get; set; }
        public List<string> Words { get; set; }
        private string _Word;
        public string Word { get { return _Word; } set { Set(ref _Word, value); } }

        private bool _Closed = false;
        public bool Closed { get { return _Closed; } set { Set(ref _Closed, value); } }
        public AddWordViewModel()
        {
            DialogAddWordCommand = new DialogAddWordCommand(AddWord, CanAddWord);
            Words = new List<string>();
        }

        private void AddWord(object param)
        {
            Closed = true;
        }

        private bool CanAddWord(object param)
        {
            return !string.IsNullOrWhiteSpace(Word) && !string.IsNullOrEmpty(Word)&& !Words.Contains(Word);
        }
    }
}
