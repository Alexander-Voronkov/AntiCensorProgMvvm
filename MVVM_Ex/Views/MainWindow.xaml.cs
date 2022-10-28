using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MVVM_Ex
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Mutex mutex=null;
        public MainWindow()
        {
            InitializeComponent();
            if (Mutex.TryOpenExisting("AntiCensor", out mutex) == true)
            {
                this.Close();
                return;
            }
            else
            {
                mutex = new Mutex(false,"AntiCensor");
            }
        }
    }
}
