using System;
using System.Text;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Net.NetworkInformation;

namespace Client.Classes
{
    internal class Peer
    {
        Socket client;
        IPAddress iPAddress;
        IPEndPoint serverEndPoint;
        IPHostEntry hostEntry;
        string serverIP = string.Empty;
        string clientIP = string.Empty;
        MainWindow mainWindow;
        public ObservableCollection<Users> users { get; set; }
        public Peer(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            InitializePeer();
            users = new ObservableCollection<Users>();
        }
        void InitializePeer()
        {
            // IP адрес сервера
            serverIP = "192.168.6.66";
            // Конвертируем строку с ip сервера в IPAddress
            iPAddress = IPAddress.Parse(serverIP);
            // Создаем IPEndPoint с адресом сервера и портом.
            // То есть конечная точка это сервер.
            serverEndPoint = new IPEndPoint(iPAddress, 13000);
            // Получаем имя хоста (компьютера, на котором запущен клиент).
            // Сразу же получаем информацию о хосте.
            hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            // Из из списка адресов AddressList получаем ip адрес клиента.
            clientIP = hostEntry.AddressList[1].ToString();
            // Создаем сокет этого клиента, через который будем подключаться к серверу.
            client = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            // Событие, происходящее при загрузке окна.
            mainWindow.Loaded += StartConnection;
            // Событие, происходящее при закрытии окна.
            mainWindow.Closing += new CancelEventHandler(TerminateApp);
        }

        private void StartConnection(object sender, RoutedEventArgs e)
        {
            // Подключаемся к серверу и в качестве параметра
            // указываем конечную точку сервера.
            try
            {
                client.Connect(serverEndPoint);
                Thread.Sleep(500);
                byte[] msg = new byte[1];
                client.Send(msg);
                Thread getUsersThread = new Thread(GetListOfUsers);
                getUsersThread.Name = "GetListOfUsersName";
                getUsersThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " Or server not available.");
            }
        }

        private void GetListOfUsers()
        {
            while (true)
            {
                byte[] buffer = new byte[1024];
                client.Receive(buffer);
                string jsonString = Encoding.UTF8.GetString(buffer);
                jsonString = jsonString.TrimEnd('\0');
                Users user = JsonSerializer.Deserialize<Users>(jsonString);
                if(clientIP != user.IP)
                {
                    mainWindow.lb_Users.Dispatcher.Invoke(new Action(() =>
                    {
                        users.Add(user);
                    }));
                }
            }
        }

        private void TerminateApp(object sender, CancelEventArgs e)
        {
            // При закрытии приложения, серверу будет отправлен сигнал
            // говорящий что клиент отключается. Отправляем ip адрес клиента
            // чтобы сервер знал какой именно клиент отключился.
            string str = $"DISCONNECT_SIGNAL:{clientIP}";
            // Создаем массив байтов для отправки сообщения серверу и
            // получаем байты из строки.
            byte[] message = Encoding.ASCII.GetBytes(str);
            // Отправляем сообщение серверу.
            client.Send(message);
            // Отключаемся от сервера и ставим false чтобы данный сокет
            // нельзя было снова использовать.
            client.Disconnect(false);
            // Закрываем сокет.
            client.Close();
            // Закрываем прииложение.
            Application.Current.Shutdown();
            // Убиваем текущий процесс приложения.
            Process.GetCurrentProcess().Kill();
        }
    }
}
