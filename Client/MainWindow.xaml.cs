using System;
using System.Text;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using Client.Classes;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Peer peer;
        public MainWindow()
        {
            InitializeComponent();
            peer = new Peer(this);
            this.DataContext = peer;
            lb_Users.MouseDoubleClick += ListBoxDoubleClick;
        }
        public void ListBoxDoubleClick(object sender, EventArgs e)
        {
            if(lb_Users.SelectedItem != null)
            {
                
            }
        }
    }
}
