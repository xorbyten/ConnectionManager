using System;
using System.Windows;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ConnectionManager.Handlers;

namespace ConnectionManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NetworkHandler networkHandler;
        public MainWindow()
        {
            InitializeComponent();
            networkHandler = new NetworkHandler(this);
            this.Loaded += networkHandler.StartNetworkManager;
            this.Closing += new CancelEventHandler(TerminateApp);
            this.DataContext = networkHandler;
        }
        void TerminateApp(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }
    }
}
