using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    /// Interaction logic for AddServer.xaml
    /// </summary>
    public partial class AddServer : Window
    {
        public StartWindow startWindow { get; set; }
        public Server ToModify { get; set; }

        public AddServer()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (CheckData())
            {
                Server s = new Server(this.ip.Text, Int32.Parse(this.port.Text), this.user.Text, "WORKGROUP",
                    this.password.Password);
                s.Nickname = this.nick.Text;
                if (ToModify != null)
                {
                    startWindow.List.Remove(ToModify);
                }
                startWindow.List.Add(s);
                this.Close();
            }
            else
            {
                MessageBox.Show("Campi non compilati correttamente");
            }
        }

        private bool CheckData()
        {
            IPAddress ipadd;
            if (System.Net.IPAddress.TryParse(this.ip.Text, out ipadd) && this.port.Text.All(char.IsDigit) &&
                !String.IsNullOrWhiteSpace(this.user.Text) && !String.IsNullOrEmpty(this.password.Password) && !String.IsNullOrWhiteSpace(this.nick.Text))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.None;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ToModify != null)
            {
                ip.Text = ToModify.Ip;
                nick.Text = ToModify.Nickname;
                port.Text = ToModify.Port.ToString();
                user.Text = ToModify.Username;
            }
        }
    }
}