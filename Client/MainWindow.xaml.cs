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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Hooker hook;
        private Server serv;
       // private StubServer s;
        public MainWindow()
        {
            InitializeComponent();
            hook = new Hooker();
            hook.Win = this;
            //s = new StubServer();
            serv = new Server("192.168.168.132",3000,"Administrator","WORKGROUP","admin",this);
            serv.ConnectAndLogin();
            hook.SetHook();
        }

        public void SetServer(Server connected)
        {
            hook.RightServer = connected;
        }
    }
}
