using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Progress : Window
    {
        public MainWindow Main { get; set; }
        public StartWindow Start { get; set; }
        public Progress()
        {
            InitializeComponent();
        }
        
        public void ConnectionCompleted()
        {
            Main.Show();
            this.Close();
        }
        public void AuthFailed()
        {
            MessageBox.Show("Authentication failed");
            Start.Show();
            this.Close();
        }
      
    }
}
