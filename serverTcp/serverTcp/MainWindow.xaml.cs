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
        private Utils.User currentUser;
        private List<clientTCP.Utils.FileInfomation> files;

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
            
            string ip = "127.0.0.1", port = "1500";
            server = new Server(Utils.Function.checkIPAddress(ip), Int32.Parse(port));
            //clients = new List<Thread>();
            files = new List<clientTCP.Utils.FileInfomation>();
            list = new List<Network.HandleClient>();
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
                    Console.Write("Waiting for a connection... ");
                    eventLog.Dispatcher.Invoke(new Action(() =>
                    {
                        eventLog.Text += "Waiting for a connection... \n";
                    }), DispatcherPriority.ContextIdle);

                    client = server.waitForConnection();
                    if (client.Connected) { 
                        Network.HandleClient hc = new Network.HandleClient(client);
                        list.Add(hc);
                        new Thread(new ParameterizedThreadStart(handleClient)).Start(hc);
                    }
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



        // The ThreadProc method is called when the thread starts.
        // It loops ten times, writing to the console and yielding 
        // the rest of its time slice each time, and then ends.
        public void handleClient(object newClient)
        {
            Network.HandleClient client = (Network.HandleClient)newClient;
            String name = null;
            string fileName = Utils.Function.Get16CharacterGenerator() + ".xml";
            Boolean _clientRunning = true;

            // Perform a blocking call to accept requests.
            // You could also user server.AcceptSocket() here.
            eventLog.Dispatcher.Invoke(new Action(() =>
            {
                eventLog.Text += "Client Connected!\n";
            }), DispatcherPriority.ContextIdle);
             
            
            client.SendData("+++OPEN");
            try
            {
                while (_clientRunning)
                {
                    eventLog.Dispatcher.Invoke(new Action(() =>
                    {
                        eventLog.Text += "Wait for action!\n";
                    }), DispatcherPriority.ContextIdle);
                    name = client.ReciveCommand();
                    eventLog.Dispatcher.Invoke(new Action(() =>
                    {
                        eventLog.Text += name + "\n";
                    }), DispatcherPriority.ContextIdle);

                    if (name.Equals("+++AUTH"))
                    {
                        client.SendData("+++++OK");
                        int dim = client.reciveDimension();
                        client.SendData("+++++OK");
                        string password = client.reciveCredentials(dim);
                        string[] temp = password.Split(':');
                        currentUser = new Utils.User(temp[0], temp[1]);

                    }
                    if (name.Equals("++LOGIN"))
                    {
                        client.SendData("+++++OK");
                        int dim = client.reciveDimension();
                        client.SendData("+++++OK");
                        string password = client.reciveCredentials(dim);
                        string[] temp = password.Split(':');



                        currentUser = new Utils.User(temp[0], temp[1]);
                        /////////////////////////////////////////////////////////////////////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA

                        string salt = Utils.Function.GetSaltUser(currentUser, dbConn);
                        if (salt == null)
                        {
                            client.SendData("++CLOSE");
                            client.Close();
                        }

                        String PasswordSalt = Network.HandleClient.hashPassword(temp[1], salt);

                        /////////////////////////////////////////////////////////////////////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA

                        currentUser = new Utils.User(temp[0], PasswordSalt);



                        if (Utils.Function.existUser(currentUser, dbConn))
                        {
                            client.SendData("++CLOSE");
                            client.Close();
                        }
                        else
                        {
                            MessageBox.Show("Utente Non Valido!");
                            client.SendData("INVALID");
                            client.Close();
                            _clientRunning = false;
                            return;
                        }

                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text +=  password + "\n";
                        }), DispatcherPriority.ContextIdle);
                        Console.WriteLine("Dimensione : " + dim + "\nUsername\\Password : " + password);
                    }

                    if (name.Equals("++CLOSE"))
                    {
                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += "Client Disconnected!\n";
                        }), DispatcherPriority.ContextIdle);
                        // Shutdown and end connection
                        lock(this.list)
                        {
                            this.list.Remove(client);
                        }
                        client.Close();
                        return;
                    }

                    if (name.Equals("+++LIST"))
                    {
                        String query = String.Format("SELECT ID FROM USERS WHERE Username='{0}' AND Password='{1}'", currentUser.USERNAME, currentUser.PASSWORD);
                        currentUser.ID = this.dbConn.ExecuteScalar(query);
                        query = String.Format("SELECT BACKUP_NAME, Version, TIME FROM BACKUP WHERE USER_ID='{0}'", currentUser.ID);
                        String versions = this.dbConn.ExecuteSelectMultiRow(query, "BACKUP_NAME", "Version", "TIME");
                        Console.WriteLine(versions);
                        if (versions != "")
                        {
                            client.SendData("+++LIST");
                            client.sendDimension(versions.Length);
                            string cmd = client.ReciveCommand();
                            if (cmd.Equals("+++++OK"))
                                client.sendVersions(versions);
                        }
                        else
                        {
                            client.SendData("+++++OK");
                        }
                    }

                    if (name.Equals("+BACKUP"))
                    {
                        files.Clear();
                        client.SendData("+BACKUP");
                        Console.WriteLine("Scarico i dati\n");
                        //client.SendData("+++++OK");
                        int dim =  client.reciveDimension();
                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += "Dimensione file : " + dim + "\n";
                        }), DispatcherPriority.ContextIdle);
                        //client.SendData("+++++OK");
                        client.ReciveXMLData(dim, fileName);
                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += "Upload Complete" + "\n";
                        }), DispatcherPriority.ContextIdle);
                        String newPath = client.saveInformationOnDB(fileName, currentUser.ID, dbConn, files);
                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += newPath + "\n";
                        }), DispatcherPriority.ContextIdle);
                        if (newPath != null)
                        {
                            client.SendData("+UPLOAD");
                            MessageBox.Show(files.Count.ToString());
                            foreach (clientTCP.Utils.FileInfomation file in files)
                            {
                                client.SendData("+++FILE");
                                eventLog.Dispatcher.Invoke(new Action(() =>
                                {
                                    eventLog.Text += "Upload file complete" + "\n";
                                }), DispatcherPriority.ContextIdle);
                                client.ReciveFile(newPath + @"\" + file.PATH, file.FILENAME, (int)file.DIMENSION);
                                string cmd = client.ReciveCommand();
                                if (!cmd.Equals("+++++OK")) break;
                            }
                            // sposto il file .xml nella cartella 
                            File.Move(fileName, newPath + @"\" + fileName);
                            // elimino .xml vecchio e o aggiorno 
                            if (File.Exists(newPath + @"\" + "Config.xml")) File.Delete(newPath + @"\" + "Config.xml");
                            File.Move(newPath + @"\" + fileName, newPath + @"\" + "Config.xml");
                        }
                        name = "+++LIST";

                    }
                    if (name.Equals("RESTORE"))
                    {
                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += "Restoring files!";
                        }), DispatcherPriority.ContextIdle);
                        client.SendData("RESTORE");
                        int dim = client.reciveDimension();
                        client.SendData("+++++OK");
                        string pathForServer = client.reciveCredentials(dim);         
                        client.SendData("+++++OK");
                        IList<Utils.InfoFileToRestore> restore = client.restoreBackup(pathForServer);
                        string cmd = "";
                        Boolean first = true;
                        MessageBox.Show("Lenght : " + restore.Count);
                        foreach (Utils.InfoFileToRestore file in restore)
                        {
                            MessageBox.Show("New File");
                            if (first)
                                client.SendData("+++FILE");
                            eventLog.Dispatcher.Invoke(new Action(() =>
                            {
                                eventLog.Text += "Restore file " + "\ndim : " + file.ABSOLUTE.Length + "\n";
                            }), DispatcherPriority.ContextIdle); 
                            client.sendDimension(file.RELATIVE.Length);
                            string c = client.ReciveCommand();
                            if (c.Equals("+++++OK"))
                            {

                                client.SendData(file.RELATIVE);
                                c = client.ReciveCommand();
                                if (c.Equals("+++++OK"))
                                {
                                        
                                    client.sendDimension(file.FILE.Length);
                                    c = client.ReciveCommand();
                                    if (c.Equals("+++++OK"))
                                    {
                                           
                                        client.SendData(file.FILE);
                                        c = client.ReciveCommand();
                                        if (c.Equals("+++++OK"))
                                        {
                                                
                                            client.sendDimension((int)file.DIM);
                                            c = client.ReciveCommand();
                                            if (c.Equals("+++++OK"))
                                            {
                                                    
                                                client.sendFile(file.ABSOLUTE);
                                                eventLog.Dispatcher.Invoke(new Action(() =>
                                                {
                                                    eventLog.Text += "Restore file :  " + file.FILE + "\n path : " + file.RELATIVE + "\n" ;
                                                }), DispatcherPriority.ContextIdle);
                                                c = client.ReciveCommand();
                                                if (c.Equals("+++++OK"))
                                                {
                                                    client.SendData("+++NEXT");
                                                    first = false;
                                                }
                                                else
                                                {
                                                    client.SendData("++++END");
                                                }
                                            }
                                        }
                                    }
                                    
                                }
                            }
                        }
                        client.SendData("++++END");

                    }
                    // Shutdown and end connection

                    if (name.Equals("++++REG"))
                    {
                        Utils.User user;

                        int UserDim = client.reciveDimension();
                        Console.WriteLine("ciao");


                        client.SendData("+++++OK");
                        string UsernameReg = client.reciveCredentials(UserDim);

                        client.SendData("+++++OK");
                        int PassDim = client.reciveDimension();


                        client.SendData("+++++OK");
                        string PasswordReg = client.reciveCredentials(PassDim);


                        user = new Utils.User(UsernameReg, PasswordReg, this.dbConn);
                        user.register();

                    }

                }
                client.SendData("++CLOSE");
                client.Close();
                return;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                server.stopServer();
                return;
            }
        }
    }
}
