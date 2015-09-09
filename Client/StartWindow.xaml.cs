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
using Color = System.Windows.Media.Color;
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

        private bool _right_started = false;
        private bool _left_started = false;

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
                this.Activate();
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
            if (e.Data.GetDataPresent("server") && !_right_started)
            {
                Server server = e.Data.GetData("server") as Server;
                if ((RightServer == null || !server.Ip.Equals(RightServer.Ip)) && (LeftServer == null || !server.Ip.Equals(LeftServer.Ip)))
                {
                    RightServer = server;
                    RightServer.PropertyChanged += ConnectionHandler;
                    RightLabel.Text = server.Nickname;
                    Console.WriteLine("bastardo di cristino");
                }
            }
        }


        private void Rectangle_Left_Drop(object sender, DragEventArgs e)
        {
                if (e.Data.GetDataPresent("server") && !_left_started)
                {
                    Server server = e.Data.GetData("server") as Server;
                    if ((LeftServer == null || !server.Ip.Equals(LeftServer.Ip)) && (RightServer==null || !server.Ip.Equals(RightServer.Ip)))
                    {
                        LeftServer = server;
                        LeftServer.PropertyChanged += ConnectionHandler;
                        LeftLabel.Text = server.Nickname;
                        Console.WriteLine("Bastardo di erick toir");
                    }
                }
        }

        private void Button_Click_Right(object sender, RoutedEventArgs e)
        {
            if (!_right_started)
            {
                if (mainWindow == null)
                {
                    mainWindow = new MainWindow();
                    mainWindow.Hook = Hook;
                    Hook.Win = mainWindow;
                }
                if (RightServer != null && !RightServer.Connected)
                {
                    mainWindow.RightServer = RightServer;
                    mainWindow.RightServer.Window = this;
                    mainWindow.RightServer.ConnectAndLogin();
                    _right_started = true;
                    Mouse.OverrideCursor = Cursors.Wait;
                    Right_Start.Content = "Stop";
                    if(!_left_started)
                         mainWindow.SetHook();
                }
            }
            else
            {
                if (RightServer != null)
                {
                    RightServer.Disconnect();
                    Right_Start.Content = "Start";
                    _right_started = false;
                    Mouse.OverrideCursor = Cursors.Arrow;
                    if (!_left_started)
                    {
                        Hook.UnHook();
                        mainWindow = null;
                    }
                }

            }
        }

        private void Button_Click_Left(object sender, RoutedEventArgs e)
        {
            if (!_left_started)
            {
                if (mainWindow == null)
                {
                    mainWindow = new MainWindow();
                    mainWindow.Hook = Hook;
                    Hook.Win = mainWindow;
                }
                if (LeftServer != null && !LeftServer.Connected)
                {
                    mainWindow.LeftServer = LeftServer;
                    mainWindow.LeftServer.Window = this;
                    mainWindow.LeftServer.ConnectAndLogin();
                    _left_started = true;
                    Mouse.OverrideCursor = Cursors.Wait;
                    Left_Start.Content = "Stop";
                    if (!_right_started)
                        mainWindow.SetHook();
                }
            }
            else
            {
                if (LeftServer != null)
                {
                    LeftServer.Disconnect();
                    Left_Start.Content = "Start";
                    _left_started = false;
                    Mouse.OverrideCursor = Cursors.Arrow;
                    if (!_right_started)
                    {
                        Hook.UnHook();
                        mainWindow = null;
                    }
                }

            }
        }

        public void ConnectionHandler(object sender, PropertyChangedExtendedEventArgs<bool> args)
        {
            Console.WriteLine("padreterno mannaggia");
            if (args.PropertyName == "connected")
            {
                Console.WriteLine("ballone");
                if (sender == RightServer)
                {
                    if (args.NewValue)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            RightRectangle.Background = new SolidColorBrush(System.Windows.Media.Colors.LemonChiffon);
                          //  this.Hide();
                            mainWindow.Show();
                            Mouse.OverrideCursor = Cursors.None;
                        }));
                    }
                    else
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            RightRectangle.Background = new SolidColorBrush(Color.FromArgb(255, 244, 244, 245));
                        }));
                    }
                }
                else if (sender == LeftServer)
                {
                    Console.WriteLine("Bastardo del signoruzzo");
                    if (args.NewValue)
                    {
                        Console.WriteLine("frocio di gesù");
                        Dispatcher.Invoke(new Action(() =>
                        {
                            LeftRectangle.Background = new SolidColorBrush(System.Windows.Media.Colors.LemonChiffon);
                           // this.Hide();
                            mainWindow.Show();
                            Mouse.OverrideCursor = Cursors.None;
                        }));
                    }
                    else
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            LeftRectangle.Background = new SolidColorBrush(Color.FromArgb(255, 244, 244, 245));
                        }));
                    }
                }
            }
        }


        public void AuthFailed(Server sender)
        {
            bool anotherServer = false;
            if ((sender == RightServer && _left_started) || (sender == LeftServer && _right_started))
            {
                anotherServer = true;
            }
            if (!anotherServer)
            {
                Dispatcher.Invoke(new Action(() =>
                {
               
                        Hook.StopCapture();
                    if (mainWindow != null)
                    {
                        mainWindow.Close();
                    }
                    this.Show();
                        if ((RightServer == null || RightServer.Connected == false) &&
                            (LeftServer == null || LeftServer.Connected == false))
                        {
                            Console.WriteLine("bastogne di gesuzzo");
                            Mouse.OverrideCursor = Cursors.Arrow;
                        }
                        MessageBox.Show("Connection failed");
                    }));
            }
            if (sender == LeftServer)
            {
                 Left_Start.Content = "Start";
                _left_started = false;
                if (!_right_started)
                {
                    Mouse.OverrideCursor = Cursors.Arrow;
                    Hook.UnHook();
                    mainWindow = null;
                }
            }
            else if (sender == RightServer)
            {
                 Right_Start.Content = "Start";
                    _right_started = false;
                    if (!_left_started)
                    {
                        Mouse.OverrideCursor = Cursors.Arrow;
                        Hook.UnHook();
                        mainWindow = null;
                    }
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.None;
            Console.WriteLine("diazzo di dio");
        }

        public void EditContextMenu_OnClick(object sender, RoutedEventArgs e)
        {
            if (serverlist.SelectedItem != null)
            {
                Server old = serverlist.SelectedItem as Server;
                AddServer add = new AddServer();
                add.ToModify = old;
                add.Show();
                add.startWindow = this;
            }
        }

        public void RemoveContextMenu_OnClick(object sender, RoutedEventArgs e)
        {
            if (serverlist.SelectedItem != null)
            {
                List.Remove(serverlist.SelectedItem as Server);
            }
        }

        private void serverlist_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (serverlist.SelectedItem == null)
            {
                EditContextMenu.IsEnabled = false;
                RemoveontextMenu.IsEnabled = false;
            }
            else
            {
                EditContextMenu.IsEnabled = true;
                RemoveontextMenu.IsEnabled = true;
            }
        }

        private void serverlist_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }


        private void RemoveLeftRectContextMenu_OnClick(object sender, RoutedEventArgs e)
        {
            if (LeftServer != null)
            {
                LeftServer = null;
                LeftLabel.Text = "";
            }
        }

        private void RemoveRightRectContextMenu_OnClick(object sender, RoutedEventArgs e)
        {
            if (RightServer != null)
            {
                RightServer = null;
                RightLabel.Text = "";
            }
        }

        private void LeftRectContextEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
            if (LeftServer == null || _left_started)
            {
                RemoveFromRectLeft.IsEnabled = false;
            }
            else
            {
                RemoveFromRectLeft.IsEnabled = true;
            }
        }

        private void RightRectContextEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
            if (RightServer == null || _right_started)
            {
                RemoveFromRectRight.IsEnabled = false;
            }
            else
            {
                RemoveFromRectRight.IsEnabled = true;
            }
        }
    }
}
