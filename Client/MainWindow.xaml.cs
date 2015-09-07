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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Hooker Hook { get; set; }
        private Server serv;
        public Server RightServer {
            get { return Hook.RightServer; }
            set { Hook.RightServer = value; } 
        }
        public Server LeftServer
        {
            get { return Hook.LeftServer; }
            set { Hook.LeftServer = value; }
        }


        public MainWindow()
        {
            InitializeComponent();
            Thread t = new Thread(ShareManager.InitializeShare);
            t.Start();
           // Hook = new Hooker();
           // Hook.Win = this;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            //s = new StubServer();
            //serv = new Server("192.168.168.133",3000,"Administrator","WORKGROUP","admin",this);
            //serv.ConnectAndLogin();
            //hook.SetHook();
        }

        public void SetHook()
        {
            Hook.SetHook();
        }

    }
}
