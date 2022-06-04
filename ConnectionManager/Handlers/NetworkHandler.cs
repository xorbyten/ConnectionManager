using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace ConnectionManager.Handlers
{
    internal class NetworkHandler
    {
        // Переменная для хранения имени хоста.
        string hostName;
        // Переменная для хранения информации о хосте.
        IPHostEntry hostInfo;
        // Переменная для хранения IP хоста.
        IPAddress hostIp;
        // Переменная для хранения серверного сокета.
        Socket socket = null;
        // Переменная в которую копируется подключенный сокет клиента.
        Socket handleClient = null;
        // Конечная точка сервера.
        IPEndPoint endPoint;
        // Объект главного окна программы.
        MainWindow mainWindow;
        public string HostIp { get; private set; }
        // Свойство которого привязано к ListBox
        public string Hostname { get; private set; }
        // Коллекция, которая привязана к ListBox и из нее будет отображаться
        // и обновляться свойство Hostname.
        public ObservableCollection<Users> users { get; set; }
        // Список, который держит подключеные сокеты пользователей.
        List<Socket> userSockets;
        public NetworkHandler(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            users = new ObservableCollection<Users>();
            userSockets = new List<Socket>();
            mainWindow.lbl_Users.ItemsSource = users;
            hostName = Dns.GetHostName();
            /*hostInfo = Dns.GetHostEntry(hostName);
            hostIp = hostInfo.AddressList[1];*/
            string hostIpStr = GetIp(NetworkInterfaceType.Ethernet);
            hostIp = IPAddress.Parse(hostIpStr);
            endPoint = new IPEndPoint(hostIp, 13000);
            HostIp = hostIp.ToString();
            Hostname = hostName;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            handleClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        private string GetIp(NetworkInterfaceType _type)
        {
            string output = "";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output = ip.Address.ToString();
                        }
                    }
                }
            }
            return output;
        }
        public void StartNetworkManager(object sender, RoutedEventArgs e)
        {
            socket.Bind(endPoint);
            socket.Listen(10);

            Thread acceptThread = new Thread(() =>
            {
                while (true)
                {
                    Socket listen = socket.Accept();
                    handleClient = listen;
                    Thread handleClientThread = new Thread(HandleClient);
                    handleClientThread.Name = "HandleClientThread";
                    handleClientThread.Start();
                }
            });
            acceptThread.Start();
        }

        private void HandleClient()
        {
            Socket client = handleClient;
            bool flag = true;
            if (client.Connected)
            {
                mainWindow.lbl_Users.Dispatcher.Invoke(new Action(() =>
                {
                    // Получаем endPoint с сокета, который только что подключился
                    IPEndPoint clientEndPoint = (IPEndPoint)client.RemoteEndPoint;
                    // Получаем из endPoint адрес покдлюченного клиента
                    string ip = clientEndPoint.Address.ToString();
                    // Получаем hostEntry по адресу
                    IPHostEntry hostEntry = Dns.GetHostEntry(ip);
                    // Добавляем новый объект типа Users с именем хоста и ip адресом
                    // в список подключенных пользователей.
                    users.Add(new Users( hostname: hostEntry.HostName, ip: ip));
                    userSockets.Add(client);
                }));
                while (flag)
                {
                    // Создаем массив байтов для получения данных от клиента
                    byte[] buffer = new byte[33];
                    // Получаем данные от клиента в массив байтов
                    client.Receive(buffer);
                    // Кодируем байты в строку
                    string str = Encoding.ASCII.GetString(buffer);
                    if (str.Contains("DISCONNECT_SIGNAL"))
                    {
                        // Находим индекс символа ':'
                        int charIndex = str.IndexOf(':');
                        // Отрезаем в строке все что находится до charIndex+1
                        str = str.Substring(charIndex + 1);
                        // Убираем лишние символы.
                        str = str.Trim('\0');
                        // Передаем строку с IP адресом в метод
                        RemoveUser(str);
                        // Удаляем пользовательский сокет если он не подключен.
                        RemoveSocket();

                        client.Close();
                        client.Dispose();
                        flag = false;
                    }
                    else
                    {
                        // Отправляем список пользователей.
                        SendUser();
                    }
                }
            }
        }

        private void RemoveUser(string clientIP)
        {
            /*
             * Если пробегать по списку users в цикле и там же
             * пытаться его изменить, то выскочит исключение что
             * нельзя менять список пока итерируешься по нему.
             * Для этого создан метод CloneList чтобы клонировать список
             * во временный tmp. Затем пробегаемся по списку users но
             * удаляем пользователя из временного списка tmp. Потом,
             * после того, как цикл завершится, удаляем пользователя из
             * нашего оригинального списка users.
             */
            ObservableCollection<Users> tmp = CloneList(users);
            Users tmpUser = null;
            foreach (var usr in users)
            {
                if (usr.IP == clientIP)
                {
                    mainWindow.lbl_Users.Dispatcher.Invoke(new Action(() =>
                    {
                        // Удаляем из списка
                        tmp.Remove(usr);
                    }));
                    // Сохраняем удаленого пользователя в переменную tmpUser
                    // чтобы вынести его за цикл.
                    tmpUser = usr;
                }
            }
            if (tmpUser != null)
            {
                mainWindow.lbl_Users.Dispatcher.Invoke(new Action(() =>
                {
                    users.Remove(tmpUser);
                }));
            }
        }

        private void RemoveSocket()
        {
            for (int i = 0; i < userSockets.Count(); i++)
            {
                if (!(userSockets[i].Connected))
                    userSockets.Remove(userSockets[i]);
            }
        }

        private ObservableCollection<Users> CloneList(ObservableCollection<Users> source)
        {
            ObservableCollection<Users> result = new ObservableCollection<Users>();

            for (int i = 0; i < source.Count; i++)
            {
                result.Add(source[i]);
            }

            return result;
        }

        private void SendUser()
        {
            foreach(var obj in userSockets)
            {
                if(obj.Connected)
                {
                    foreach(var obj2 in users)
                    {
                        string jsonString = JsonSerializer.Serialize<Users>(obj2);
                        byte[] buffer = Encoding.UTF8.GetBytes(jsonString);
                        obj.Send(buffer);
                        Thread.Sleep(50);
                    }
                }
            }
        }
    }
}
