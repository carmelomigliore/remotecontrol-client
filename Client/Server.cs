using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Client
{
    public class Server
    {
        
        public string Ip { get; set; }
        public int Port { get; set; }
        public short Side { get; set; } //0 = left - 1 = right
        public string Nickname { get; set; }

        public string Username { get; set; }
        public string Domain { get; set; }
        public string Password { get; set; }
        public bool Connected { get; set; }

        public MainWindow Window { get; set; }
        
        private Connection _connection;

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource,
            string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags,
            bool force);

        [StructLayout(LayoutKind.Sequential)]
        public class NetResource
        {
            public ResourceScope Scope;
            public ResourceType ResourceType;
            public ResourceDisplaytype DisplayType;
            public int Usage;
            public string LocalName;
            public string RemoteName;
            public string Comment;
            public string Provider;
        }

        public enum ResourceScope : int
        {
            Connected = 1,
            GlobalNetwork,
            Remembered,
            Recent,
            Context
        };

        public enum ResourceType : int
        {
            Any = 0,
            Disk = 1,
            Print = 2,
            Reserved = 8,
        }

        public enum ResourceDisplaytype : int
        {
            Generic = 0x0,
            Domain = 0x01,
            Server = 0x02,
            Share = 0x03,
            File = 0x04,
            Group = 0x05,
            Network = 0x06,
            Root = 0x07,
            Shareadmin = 0x08,
            Directory = 0x09,
            Tree = 0x0a,
            Ndscontainer = 0x0b
        }

        public Server(string ip, int port, string username, string domain, string password, MainWindow window)
        {
            Ip = ip;
            Port = port;
            Username = username;
            Domain = domain;
            Password = password;
            _connection = new Connection(Ip, Port);
            Window = window;
        }

        public void Send(bool keyboard, byte[] data)
        {
            _connection.Send(data,keyboard);
        }

        public void ConnectAndLogin()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += DoWorkConnect;
            bw.RunWorkerCompleted += ConnectionCompleted;
            bw.RunWorkerAsync();
        }

        private void DoWorkConnect(object sender, DoWorkEventArgs eventArgs)
        {
           
               // BackgroundWorker bw = (BackgroundWorker) sender;
               eventArgs.Result = _connection.TcpConnectAndLogin(Username, Domain, Password);

            if ((bool) eventArgs.Result)
            {
                Action act = new Action(() =>
                {
                    var netResource = new NetResource()
                    {
                        Scope = ResourceScope.GlobalNetwork,
                        ResourceType = ResourceType.Disk,
                        DisplayType = ResourceDisplaytype.Share,
                        RemoteName = "\\\\" + Ip + "\\C"
                    };

                    var result = WNetAddConnection2(
                        netResource,
                        Password,
                        Username,
                        0x00000004 | 0x00000008 | 0x00000010 | 0x1000);

                    if (result != 0)
                    {
                        Console.WriteLine("Result not zero: " + result);
                    }
                });
               Thread t = new Thread(() => act());
               t.Start();

            }
        }

        private void ConnectionCompleted(object sender, RunWorkerCompletedEventArgs eventArgs)
        {
            if (!eventArgs.Cancelled && eventArgs.Error == null)
            {
                Connected = (bool)eventArgs.Result;
              //  Window.SetServer(this);

                //TODO trayicon notification
               

            }
            else
            {
                Console.WriteLine(eventArgs.Error.Message);
                Connected = false;
            }
        
        
        }

        private byte[] RetrieveLocalClipboard()
        {
            try
            {
                IDataObject data = Clipboard.GetDataObject();
                ArrayList dataObjects = new ArrayList();
                if (data != null)
                {

                    string[] formats = data.GetFormats();
                    BinaryFormatter bf = new BinaryFormatter();
                    for (int i = 0; i < formats.Length; i++)
                    {
                        object clipboardItem;
                        try
                        {
                            clipboardItem = data.GetData(formats[i]);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        if (clipboardItem != null && clipboardItem.GetType().IsSerializable)
                        {
                            Console.WriteLine("sending {0}", formats[i]);
                            dataObjects.Add(formats[i]);
                            dataObjects.Add(clipboardItem);
                        }
                        else
                            Console.WriteLine("ignoring {0}", formats[i]);
                    }
                    using (var ms = new MemoryStream())
                    {
                        bf.Serialize(ms, dataObjects);
                        Console.WriteLine("count: " + dataObjects.Count);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return null;
        }

        public void SendLocalClipboard()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += DoWorkSendLocalClipboard;
            bw.RunWorkerAsync(RetrieveLocalClipboard());
        }

        private void DoWorkSendLocalClipboard(object sender, DoWorkEventArgs eventArgs)
        {
            _connection.SendClipboard((byte[])eventArgs.Argument);
        }

        public void GetRemoteClipboard()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += DoWorkGetClipboard;
            bw.RunWorkerCompleted += ClipboardReceived;
            bw.RunWorkerAsync();
        }

        private void DoWorkGetClipboard(object sender, DoWorkEventArgs eventArgs)
        {
            eventArgs.Result = _connection.GetClipboard();
        }

        private void ClipboardReceived(object sender, RunWorkerCompletedEventArgs eventArgs)
        {
            if (!eventArgs.Cancelled && eventArgs.Error == null)
            {
                ArrayList data = (ArrayList) eventArgs.Result;
                if (data != null)
                {
                    DataObject dataObj = new DataObject();
                    Console.WriteLine("Count: " + data.Count);
                    for (int i = 0; i < data.Count; i++)
                    {
                        string format = (string)data[i++];
                        Console.WriteLine(format);
                        dataObj.SetData(format, data[i]);
                    }

                    if (dataObj.ContainsFileDropList())
                    {
                        StringCollection files = dataObj.GetFileDropList();
                        dataObj = new DataObject();
                        StringCollection adjusted = new StringCollection();
                        foreach (string f in files)
                        {
                            if (!f.StartsWith("\\"))
                            {
                                string toadd = "\\\\" + Ip + "\\" + f.Replace(":", "");
                                Console.WriteLine(toadd);
                                adjusted.Add(toadd);
                            }
                            else
                            {
                                adjusted.Add(f);
                            }
                        }
                        dataObj.SetFileDropList(adjusted);
                    }
                    Clipboard.SetDataObject(dataObj);
                   
                }
            }
        }

    }
}
