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
using System.IO;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using System.Data.SQLite;
using ServerTCP;
using System.Runtime.InteropServices;

namespace serverTcp
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        

        private Server server;
        private volatile Boolean _isRunning = false;
        private Thread t;
        private TcpClient clients;
        private List<Network.HandleClient> list;
        private Database.SQLiteDatabase dbConn;

        public void CreateDBAndTable()
        {
            string inputFile = "testDB.s3db";
            string dbConnection = String.Format("Data Source={0}", inputFile);  
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);  
            cnn.Open();
           
            string createLogTableSQL = "CREATE TABLE [USERS] (" +  
                "[Username] TEXT PRIMARY KEY," +  
                "[Password] TEXT NULL," +
                "[Folder_ID] TEXT NULL" +
                ")";  
            using (SQLiteTransaction sqlTransaction = cnn.BeginTransaction())  
            {  
                // Create the table  
                SQLiteCommand createCommand = new SQLiteCommand(createLogTableSQL, cnn);  
                createCommand.ExecuteNonQuery();  
                createCommand.Dispose();
                createLogTableSQL = "INSERT INTO USERS (Username, Password) values ('gianpiero', 'delconte')";
                createCommand = new SQLiteCommand(createLogTableSQL, cnn);  
                createCommand.ExecuteNonQuery();  
                createCommand.Dispose();
                // Commit the changes into the database  
                sqlTransaction.Commit();  
            } // end using 
            cnn.Close();
        }


        public MainWindow()
        {
            try
            {
                string ip = getLocalIp();
                //string ip = "127.0.0.1";
                Console.WriteLine(ip);
                string port = "3000";
                server = new Server(Utils.Function.checkIPAddress(ip), Int32.Parse(port));
                //clients = new List<Thread>();

                list = new List<Network.HandleClient>();
            }
            catch (Exception e)
            {
                return;
            }
            //CreateDBAndTable();
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (server != null) this.server.stopServer();
            _isRunning = false;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
              
            IPServer.Dispatcher.Invoke(new Action(() =>
            {
                //ip = IPServer.Text;
                IPServer.Visibility = Visibility.Hidden;
            }), DispatcherPriority.ContextIdle);

            Port.Dispatcher.Invoke(new Action(() =>
            {
                //port = Port.Text;
                Port.Visibility = Visibility.Hidden;
            }), DispatcherPriority.ContextIdle);
            
            // creazione della connessione e start del server 
  
            server.StartServer();
            dbConn = new Database.SQLiteDatabase(Utils.Function.checkOrCreateFileDb());
                
            _isRunning = true;
            btnS.Dispatcher.Invoke(new Action(() =>
            {
                btnS.Visibility = Visibility.Hidden;
                eventLog.Text += "Server Online\n";
            }), DispatcherPriority.ContextIdle);

            disconnect.Dispatcher.Invoke(new Action(() =>
            {
                disconnect.Visibility = Visibility.Visible;
            }), DispatcherPriority.ContextIdle);
                
            t = new Thread(new ThreadStart(beginConnection));
            t.Start();
            t.IsBackground = true;
        }


        private void disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (list.Count > 0)
            {
                foreach (Network.HandleClient tcp in list)
                {
                    tcp.SendData("++CLOSE");
                    tcp.Close();
                }
                list.Clear();
            }

            _isRunning = false;

            server.stopServer();

            btnS.Dispatcher.Invoke(new Action(() =>
            {
                btnS.Visibility = Visibility.Visible;
                eventLog.Text += "Close Connection\n";
            }), DispatcherPriority.ContextIdle);

            disconnect.Dispatcher.Invoke(new Action(() =>
            {
                disconnect.Visibility = Visibility.Hidden;
            }), DispatcherPriority.ContextIdle);
            return;
        }


        public void beginConnection()
        {
            try
            {
                TcpClient client = null;
                while (_isRunning)
                {
                    Network.HandleClient hc = null;
                    Console.Write("Waiting for a connection... ");
                    eventLog.Dispatcher.Invoke(new Action(() =>
                    {
                        eventLog.Text += "Waiting for a connection... \n";
                    }), DispatcherPriority.ContextIdle);
                    client = server.waitForConnection();
                    hc = new Network.HandleClient(client, eventLog, dbConn);
                    list.Add(hc);
                    
                   
                }
                if (client != null)
                    client.Close();
                return;
                
            }
            catch (Exception e4)
            {
                Console.WriteLine(e4.Message);
                return;              
            }
        }

        public static string getLocalIp()
        {
            string localIp = "127.0.0.1";

            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIp = ip.ToString();
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Sorry, a network error occured.");
                return null;
            }
            return localIp;
        }


    }
}
