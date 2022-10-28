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
        public List<string> Words { get; set; }
        private string _Word;
        public string Word { get { return _Word; } set { Set(ref _Word, value); Check(); } }
        private bool _CheckWord;
        public bool CheckWord { get { return _CheckWord; } set { Set(ref _CheckWord, value); } }
        private bool _Closed = false;
        public bool Closed { get { return _Closed; } set { Set(ref _Closed, value); } }
        public AddWordViewModel()
        {
            Words = new List<string>();
        }

        private void Check()
        {
            CheckWord = !string.IsNullOrWhiteSpace(Word) && !string.IsNullOrEmpty(Word) && !Words.Contains(Word);
        }
    }
}
