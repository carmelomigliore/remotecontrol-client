using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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
using Point = System.Windows.Point;

namespace Client
{
    /// <summary>
    /// Interaction logic for StartWindow.xaml
    /// </summary>
    public partial class StartWindow : Window
    {

        public ObservableCollection<Server> List { get; set; }
        public MainWindow mainWindow { get; set; }
        private System.Windows.Forms.NotifyIcon _trayIcon;
        public Server RightServer { get; set; }
        public Server LeftServer { get; set; }

        private bool _started = false;

        public Hooker Hook { get; set; }
        private Point startPosition;
        public StartWindow()
        {
            InitializeComponent();
            List = new ObservableCollection<Server>();
            this.DataContext = this;
             Hook = new Hooker();
            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.Icon = new System.Drawing.Icon("Resources/Icon.ico");
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate(object sender, EventArgs args)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
            _trayIcon.Click += delegate(object sender, EventArgs args)
            {
                this.Hide();
            };
        }
 
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddServer addServer = new AddServer();
            addServer.mainWindow = mainWindow;
            addServer.startWindow = this;
            addServer.Show();
        }

        private void serverlist_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPosition = e.GetPosition(null);
        }

        private void serverlist_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Vector diff = startPosition - e.GetPosition(null);

            if (e.LeftButton == MouseButtonState.Pressed &&
                Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                ListView listView = sender as ListView;
                ListViewItem listViewItem = FindAnchestor<ListViewItem>((DependencyObject) e.OriginalSource);
                if (listView != null && listViewItem != null)
                {
                    Server server = (Server)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
                    DataObject dragData = new DataObject("server", server);
                    DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
                }
             
            }
        }

        private T FindAnchestor<T>(DependencyObject current) where T: DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T) current;
                }
                current = VisualTreeHelper.GetParent(current);

            } while (current != null);
            return null;
        }

        private void Rectangle_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("server") || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Rectangle_Right_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("server"))
            {
                Server server = e.Data.GetData("server") as Server;
                RightServer = server;
                RightServer.PropertyChanged += ConnectionHandler;
                RightLabel.Content = server.Nickname;
            }
        }


        private void Rectangle_Left_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("server"))
            {
                if (e.Data.GetDataPresent("server"))
                {
                    Server server = e.Data.GetData("server") as Server;
                    LeftServer = server;
                    LeftLabel.Content = server.Nickname;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_started)
            {
                mainWindow = new MainWindow();
                mainWindow.Hook = Hook;
                Hook.Win = mainWindow;
                bool serverIsPresent = false;
                if (RightServer != null && !RightServer.Connected)
                {
                    mainWindow.RightServer = RightServer;
                    mainWindow.RightServer.Window = this;
                    mainWindow.RightServer.ConnectAndLogin();
                    serverIsPresent = true;
                }

                if (LeftServer != null && !LeftServer.Connected)
                {
                    mainWindow.LeftServer = LeftServer;
                    mainWindow.LeftServer.Window = this;
                    mainWindow.LeftServer.ConnectAndLogin();
                    serverIsPresent = true;
                }
                if (serverIsPresent)
                {
                    mainWindow.SetHook();
                    _started = true;
                    Mouse.OverrideCursor = Cursors.Wait;
                    Start.Content = "Stop";
                    // this.Hide();
                    //Prog.Show();
                    //mainWindow.Show();
                }
            }
            else
            {
                if (RightServer != null)
                {
                    RightServer.Disconnect();
                    Start.Content = "Start";
                    _started = false;
                    Mouse.OverrideCursor = Cursors.Arrow;
                    Hook.UnHook();
                }

                if (LeftServer != null)
                {
                    LeftServer.Disconnect();
                    Start.Content = "Start";
                    _started = false;
                    Mouse.OverrideCursor = Cursors.Arrow;
                    Hook.UnHook();
                }
            }
        }

        public void ConnectionHandler(object sender, PropertyChangedExtendedEventArgs<bool> args)
        {
            if (args.PropertyName == "connected")
            {

                if (sender == RightServer)
                {
                    if (args.NewValue)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            RightRectangle.Stroke = new SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                            this.Hide();
                            mainWindow.Show();
                            Mouse.OverrideCursor = Cursors.None;
                        }));
                    }
                    else
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            RightRectangle.Stroke = new SolidColorBrush(System.Windows.Media.Colors.Black);
                        }));
                    }
                }
                else if (sender == LeftServer)
                {
                    if (args.NewValue)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            LeftRectangle.Stroke = new SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                            this.Hide();
                            mainWindow.Show();
                            Mouse.OverrideCursor = Cursors.None;
                        }));
                    }
                    else
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            LeftRectangle.Stroke = new SolidColorBrush(System.Windows.Media.Colors.Black);
                        }));
                    }
                }
            }
        }

        public void AuthFailed()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                MessageBox.Show("Authentication failed");
                Hook.StopCapture();
                mainWindow.Close();
                this.Show();
                Mouse.OverrideCursor = Cursors.Arrow;
            }));
            //this.Close();
        }
    }
}
