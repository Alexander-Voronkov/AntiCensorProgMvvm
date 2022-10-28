using MVVM_Ex.ViewModels;
using MVVM_Ex.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace MVVM_Ex.Dialogs
{
    public static class DialogService
    {

        public static List<string> SelectExceptions(List<string> exceptions)
        {
            var select = new ExceptionFiles();
            foreach (var item in exceptions)
            {
                (select.DataContext as ExceptionFilesViewModel).Exceptions.Add(item);
            }
            select.ShowDialog();
            return (select.DataContext as ExceptionFilesViewModel).Exceptions.ToList();
        }

        public static string AddWord(List<string> words)
        {
            var add = new AddWordView();
            (add.DataContext as AddWordViewModel).Words.AddRange(words);
            if (add.ShowDialog() != false)
            {
                return (add.DataContext as AddWordViewModel).Word;
            }
            return null;
        }

        public static List<string> LoadForbiddenWords()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files|*.txt";
            if(openFileDialog.ShowDialog()==DialogResult.OK)
            {
                if (!File.Exists(openFileDialog.FileName))
                    return null;
                List<string> words = new List<string>();
                using (var stream=new FileStream(openFileDialog.FileName,FileMode.Open))
                {
                    using (var sr = new StreamReader(stream))
                    {
                        return sr.ReadToEnd().Split('\n',' ','.',',','-',':',';','?','!','\\','/').ToList();
                    }
                }
            }
            return null;
        }

        public static string SelectDestinationFolder()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if(dialog.ShowDialog()==DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            return null;
        }
    }
}
