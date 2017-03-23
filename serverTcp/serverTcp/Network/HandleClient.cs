using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Data.SQLite;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace serverTcp.Network
{
    class HandleClient
    {
        TcpClient client;
        NetworkStream ns;
        private Database.SQLiteDatabase dbConn;
        private TextBox eventLog;
        private Utils.User currentUser;
        private List<clientTCP.Utils.FileInfomation> files;
        private volatile Boolean _clientRunning = true;

        public HandleClient(TcpClient client, TextBox eventLog, Database.SQLiteDatabase dbConn)
        {
            this.client = client;
            this.eventLog = eventLog;
            this.dbConn = dbConn;
            files = new List<clientTCP.Utils.FileInfomation>();
            this.ns = client.GetStream();
            // necessario per avviare la connessione
            
            Thread ctThread = new Thread(startClient);
            ctThread.Start();
        }


        public void Close()
        {
            ns.Close();
            client.Close();
        }


        public int SendData(string data)
        {
            try
            {
                // Get a stream object for reading and writing
                Byte[] sendBytes = System.Text.Encoding.ASCII.GetBytes(data);
                ns.Write(sendBytes, 0, sendBytes.Length);
                ns.Flush();
                return 0;
            }
            catch (ObjectDisposedException e)
            {
                return 1;
            }
            catch (SocketException e1)
            {
                Console.WriteLine(e1.Message);
                return 2;
            }
            catch (IOException e2)
            {
                return -1;
            }


        }


        public int reciveDimension()
        {
            try
            {
                byte[] buf = new byte[15];
                int len = ns.Read(buf, 0, 15);
                ns.Flush();
                return BitConverter.ToInt32(buf, 0);
            }
            catch(Exception e)
            {
                Debug.Print(e.Message);
                return -1;
            }
        }

        public String reciveCredentials(int dim)
        {
            try
            {
                Byte[] bytes = new Byte[dim];
                ns.Read(bytes, 0, dim);
                return System.Text.Encoding.ASCII.GetString(bytes, 0, dim);
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return null;
            }
        }


        public String ReciveCommand()
        {

            // Buffer for reading data
            Byte[] bytes = new Byte[7];
            String data = null;

            try
            {
                ns.Read(bytes, 0, 7);
                data = System.Text.Encoding.ASCII.GetString(bytes, 0, 7);
                ns.Flush();
            }
            catch (IOException e)
            {
                Console.WriteLine("Problem!!\n");
                throw new Exception("SocketClosed");
            }
            return data;

        }



        public int ReciveXMLData(int dim, string fileName)
        {

            // Buffer for reading data
            Byte[] bytes = new Byte[1];
            int lenght = 1;
            //StringBuilder sb = new StringBuilder();
            int i;
            Int32 numberOfTotalBytes = dim;
            Int32 byteRecivied = 0;
            try
            {
                FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
                Byte[] head = System.Text.Encoding.ASCII.GetBytes("<?xml version='1.0'?>\n");
                fs.Write(head, 0, head.Length);


                if (dim < lenght)
                {
                    ns.Read(bytes, 0, dim);
                    fs.Write(bytes, 0, dim);
                    fs.Close();
                    return 0;
                }
                else
                {
                    while ((i = ns.Read(bytes, 0, lenght)) != 0)
                    {
                        fs.Write(bytes, 0, lenght);
                        byteRecivied += lenght;
                        if (numberOfTotalBytes - byteRecivied < lenght)
                        {
                            lenght = numberOfTotalBytes - byteRecivied;
                            ns.Read(bytes, 0, lenght);
                            fs.Write(bytes, 0, lenght);
                            break;
                        }

                    }
                    fs.Close();
                    return 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                ns.Close();
                return 2;
            }
        }

        public int ReciveFile(String path, String name,  int dim, int index)
        {
            // Buffer for reading data
            Byte[] bytes = new Byte[1];
            int lenght = 1;
            int i;
            Int32 numberOfTotalBytes = dim;
            Int32 byteRecivied = 0;
            FileStream fs = null;
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                fs = new FileStream(path + @"\" + name, FileMode.Create, FileAccess.ReadWrite);

                if (dim < lenght)
                {
                    ns.Read(bytes, 0, dim);
                    fs.Write(bytes, 0, dim);
                    ns.Flush();
                    fs.Close();
                    return 0;
                }
                else
                {
                    //DataAvailable(ns, 5000);
                    while ((i = ns.Read(bytes, 0, lenght)) > 0)
                    {                        
                        fs.Write(bytes, 0, lenght);
                        byteRecivied += lenght;
                    
                        if (numberOfTotalBytes - byteRecivied < lenght && numberOfTotalBytes - byteRecivied > 0)
                        { 
                            lenght = numberOfTotalBytes - byteRecivied;
                            ns.Read(bytes, 0, lenght);
                            fs.Write(bytes, 0, lenght);
                            break;
                        }
                        if (numberOfTotalBytes - byteRecivied == 0) break; 
                    }
                    ns.Flush();
                    fs.Close();
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (fs != null) fs.Close();
                ns.Close();
                return 2;
            }

        }

        public String saveInformationOnDB(String fileName, String id, Database.SQLiteDatabase dbConn, List<clientTCP.Utils.FileInfomation> files)
        {
            XmlDocument doc = new XmlDocument();
            int num = 0;
            String newPath = null;
            String query;
            String time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            doc.LoadXml(File.ReadAllText(fileName));
            XmlElement root = doc.DocumentElement;
            String backupID = root.Attributes["backup_name"].Value;
            
            // salvo il nuovo backup nella tabella 
            // controllo la versione attuale della cartella di cui effettuo il backup ho scelto di avere al piu 4 versioni 
            String sql = String.Format("SELECT MAX(Version) FROM BACKUP WHERE USER_ID='{0}' AND BACKUP_NAME='{1}'", id, backupID);
            String risultato = dbConn.ExecuteScalar(sql);
            if (risultato != "")
            {
                num = Int32.Parse(risultato);
                if (num == 4)
                {       
                    Console.WriteLine("Superata la versione 4\n");
                    sql = String.Format("SELECT ID FROM BACKUP WHERE USER_ID='{0}' AND BACKUP_NAME='{1}' ORDER BY datetime(TIME) DESC LIMIT 1", id, backupID);
                    risultato = dbConn.ExecuteScalar(sql);
                    Console.WriteLine(risultato);
                    sql = String.Format("UPDATE BACKUP SET TIME='{0}' WHERE ID='{1}'", time, risultato);
                    Console.WriteLine(sql);
                    dbConn.ExecuteScalar(sql);  
                }
                else
                {
                    num += 1;
                    query = String.Format("INSERT INTO BACKUP(BACKUP_NAME, Version, TIME, USER_ID) VALUES('{0}', '{1}', '{2}', '{3}')", backupID, num, time, id);
                    dbConn.ExecuteScalar(query);
                    
                }
            }
            else
            { 
                num += 1;
                query = String.Format("INSERT INTO BACKUP(BACKUP_NAME, Version, TIME, USER_ID) VALUES('{0}', '{1}', '{2}', '{3}')", backupID, num, time, id);
                dbConn.ExecuteScalar(query);
                
            }
            

            XmlNodeList element = root.ChildNodes;
            for (int i = 0; i < element.Count; i++)
            {
                XmlNodeList list = element[i].ChildNodes;
                String path = list.Item(1).FirstChild.Value;
                String name = list.Item(0).FirstChild.Value;
                int dim = Int32.Parse(list.Item(2).FirstChild.Value);
                String crc = list.Item(4).FirstChild.Value;
                DateTime timestamp = Convert.ToDateTime(list.Item(3).FirstChild.Value);
                files.Add(new clientTCP.Utils.FileInfomation(path, name, dim, System.Text.Encoding.ASCII.GetBytes(crc), timestamp));
                                              
            }
            newPath = backupID + @"\" + num;
            return newPath;      
        }


        public IList<Utils.InfoFileToRestore> restoreBackup(String path)
        {
            IList<Utils.InfoFileToRestore> restore = new List<Utils.InfoFileToRestore>();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText(Directory.GetCurrentDirectory() + @"\" + path + @"\Config.xml"));
            Console.WriteLine(Directory.GetCurrentDirectory() + @"\" + path + @"\Config.xml");
            string[] tmp = path.Split('\\');
            XmlElement root = doc.DocumentElement;

            XmlNodeList element = root.ChildNodes;
            for (int i = 0; i < element.Count; i++)
            {
                XmlNodeList list = element[i].ChildNodes;
                String filePosition = Directory.GetCurrentDirectory() + @"\" + path + list.Item(1).FirstChild.Value + @"\" + list.Item(0).FirstChild.Value;
                String relative = list.Item(1).FirstChild.Value + @"\";
                String file = list.Item(0).FirstChild.Value;
                long dim = long.Parse(list.Item(2).FirstChild.Value);
                restore.Add(new Utils.InfoFileToRestore(filePosition, relative, file, dim));
            }
            return restore;
        }

        ///AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAaaaaaaaaaaaaaaaaaaaaaaaaaa
        public static String DBManagerRegister(String Username, String Password, Database.SQLiteDatabase dbConn)
        {
            string Salt = RandomString(10);

            String sql = String.Format("INSERT INTO USERS (Username, Password, Salt) values ('{0}', '{1}', '{2}')", Username, hashPassword(Password, Salt), Salt);
            String risultato = dbConn.ExecuteScalar(sql);
            Console.WriteLine("Risulato: " + risultato);
            return null;
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }



        static public string hashPassword(string password, string salt)
        {
            return Hash(Hash(password) + salt);
        }

        static private string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            X509Certificate cert = new X509Certificate("certificate\\certificate.cer");
            if (cert.Equals(cert))
                return true;
            return false;
        }
        /// ////////////////////////AAAAAAAAAAAAAAAAAAAaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa


        public void sendDimension(int dim)
        {
            try
            {
                byte[] buf = new byte[15];
                BitConverter.GetBytes(dim).CopyTo(buf, 0);
                ns.Write(buf, 0, 15);
                ns.Flush();
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return;
            }

        }

        public void sendVersions(String data)
        {
            try
            {
                Byte[] dati = System.Text.Encoding.ASCII.GetBytes(data);
                ns.Write(dati, 0, dati.Length);
                ns.Flush();
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return;
            }

        }

        public void sendFile(String path)
        {
            try
            {
                Byte[] data = File.ReadAllBytes(@path);
                ns.Write(data, 0, data.Length);
                ns.Flush();
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return;
            }

        }

        public void startClient()
        {
            Utils.User currentUser = null;
            String name = null;
            string fileName = Utils.Function.Get16CharacterGenerator() + ".xml";
            

            SendData("+++OPEN");
            try
            {
                while (_clientRunning)
                {
                    eventLog.Dispatcher.Invoke(new Action(() =>
                    {
                        eventLog.Text += "Wait for action!\n";
                    }), DispatcherPriority.ContextIdle);
                    name = ReciveCommand();
                    eventLog.Dispatcher.Invoke(new Action(() =>
                    {
                        eventLog.Text += name + "\n";
                    }), DispatcherPriority.ContextIdle);

                    if (name.Equals("+++AUTH"))
                    {
                        SendData("+++++OK");
                        int dim = reciveDimension();
                        SendData("+++++OK");
                        string password = reciveCredentials(dim);
                        currentUser = new Utils.User(password);

                    }
                    if (name.Equals("++LOGIN"))
                    {
                        SendData("+++++OK");
                        int dim = reciveDimension();
                        SendData("+++++OK");
                        string password = reciveCredentials(dim);
                        string[] temp = password.Split(':');



                        currentUser = new Utils.User(temp[0], temp[1]);
                        /////////////////////////////////////////////////////////////////////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA

                        string salt = Utils.Function.GetSaltUser(currentUser, dbConn);
                        if (salt == null)
                        {
                            SendData("++CLOSE");
                            Close();
                        }
                        String PasswordSalt = Network.HandleClient.hashPassword(temp[1], salt);
                        /////////////////////////////////////////////////////////////////////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
                        currentUser = new Utils.User(temp[0], PasswordSalt);

                        if (Utils.Function.existUser(currentUser, dbConn))
                        {
                            SendData("++CLOSE");
                            Close();
                        }
                        else
                        {
                          //  MessageBox.Show("Utente Non Valido!");
                            SendData("INVALID");
                            Close();
                           _clientRunning = false;
                            return;
                        }

                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += password + "\n";
                        }), DispatcherPriority.ContextIdle);
                        Console.WriteLine("Dimensione : " + dim + "\nUsername\\Password : " + password);
                    }

                    if (name.Equals("++CLOSE"))
                    {
                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += "Client Disconnected!\n";
                        }), DispatcherPriority.ContextIdle);
                        SendData("++CLOSE");
                        // Shutdown and end connection
                        client.Close();
                        _clientRunning = false;
                        return;
                    }

                    if (name.Equals("+++LIST"))
                    {
                        String query = String.Format("SELECT ID FROM USERS WHERE Username='{0}'", currentUser.USERNAME);
                        currentUser.ID = this.dbConn.ExecuteScalar(query);
                        query = String.Format("SELECT BACKUP_NAME, Version, TIME FROM BACKUP WHERE USER_ID='{0}'", currentUser.ID);
                        String versions = this.dbConn.ExecuteSelectMultiRow(query, "BACKUP_NAME", "Version", "TIME");
                        Console.WriteLine(versions);
                        if (versions != "")
                        {
                            SendData("+++LIST");
                            sendDimension(versions.Length);
                            string cmd = ReciveCommand();
                            if (cmd.Equals("+++++OK"))
                                sendVersions(versions);
                        }
                        else
                        {
                            SendData("+++++OK");
                        }
                    }

                    if (name.Equals("+BACKUP"))
                    {
                        files.Clear();
                        SendData("+BACKUP");
                        Console.WriteLine("Scarico i dati\n");
                        //client.SendData("+++++OK");
                        int dim = reciveDimension();
                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += "Dimensione file : " + dim + "\n";
                        }), DispatcherPriority.ContextIdle);
                        SendData("+++++OK");
                        ReciveXMLData(dim, fileName);
                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += "Upload Complete" + "\n";
                        }), DispatcherPriority.ContextIdle);
                        string newPath = saveInformationOnDB(fileName, currentUser.ID, dbConn, files);
                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += newPath + "\n";
                        }), DispatcherPriority.ContextIdle);
                        if (newPath != null)
                        {
                            SendData("+UPLOAD");
                            //MessageBox.Show("Numero FIle : " + files.Count);
                            string cmd = ReciveCommand();
                            if (cmd.Equals("+++++OK"))
                            {
                                int i = 0;
                                foreach (clientTCP.Utils.FileInfomation file in files)
                                {
                                    //MessageBox.Show("Path : " + file.PATH + "\nFilename : " + file.FILENAME + "\nDimension : " + file.DIMENSION);
                                    SendData("+++FILE");
                                    eventLog.Dispatcher.Invoke(new Action(() =>
                                    {
                                        eventLog.Text += "Upload file complete" + "\n";
                                    }), DispatcherPriority.ContextIdle);
                                    
                                    if (ReciveFile(newPath + @"\" + file.PATH, file.FILENAME, (int)file.DIMENSION, i) != 2)
                                    {
                                        SendData("+++++OK");
                                        i++;
                                        cmd = ReciveCommand();
                                        if (!cmd.Equals("+++++OK")) break;
                                    }
                                    else
                                    {
                                        Close();
                                        string deletes = String.Format(@"C:\Users\Gianpiero\Source\Repos\ServerPDS\serverTcp\serverTcp\bin\debug\{0}", newPath.Trim());
                                        var dir = new DirectoryInfo(deletes);
                                        dir.Delete(true);
                                        deleteEntryFromDataBase(currentUser.ID, newPath, dbConn);
                                        return;
                                    }
                                }
                                // sposto il file .xml nella cartella 
                                File.Move(fileName, newPath + @"\" + fileName);
                                // elimino .xml vecchio e o aggiorno 
                                if (File.Exists(newPath + @"\" + "Config.xml")) File.Delete(newPath + @"\" + "Config.xml");
                                File.Move(newPath + @"\" + fileName, newPath + @"\" + "Config.xml");
                            }
                        }
                        name = "+++LIST";

                    }
                    if (name.Equals("RESTORE"))
                    {
                        eventLog.Dispatcher.Invoke(new Action(() =>
                        {
                            eventLog.Text += "Restoring files!";
                        }), DispatcherPriority.ContextIdle);
                        SendData("RESTORE");
                        int dim = reciveDimension();
                        SendData("+++++OK");
                        string pathForServer = reciveCredentials(dim);
                        SendData("+++++OK");
                        IList<Utils.InfoFileToRestore> restore = restoreBackup(pathForServer);

                        Boolean first = true;
                        foreach (Utils.InfoFileToRestore file in restore)
                        {
                            if (first)
                                SendData("+++FILE");
                            eventLog.Dispatcher.Invoke(new Action(() =>
                            {
                                eventLog.Text += "Restore file " + "\ndim : " + file.ABSOLUTE.Length + "\n";
                            }), DispatcherPriority.ContextIdle);
                            sendDimension(file.RELATIVE.Length);
                            string c = ReciveCommand();
                            if (c.Equals("+++++OK"))
                            {

                                SendData(file.RELATIVE);
                                c = ReciveCommand();
                                if (c.Equals("+++++OK"))
                                {

                                    sendDimension(file.FILE.Length);
                                    c = ReciveCommand();
                                    if (c.Equals("+++++OK"))
                                    {

                                        SendData(file.FILE);
                                        c = ReciveCommand();
                                        if (c.Equals("+++++OK"))
                                        {

                                            sendDimension((int)file.DIM);
                                            c = ReciveCommand();
                                            if (c.Equals("+++++OK"))
                                            {

                                                sendFile(file.ABSOLUTE);
                                                eventLog.Dispatcher.Invoke(new Action(() =>
                                                {
                                                    eventLog.Text += "Restore file :  " + file.FILE + "\n path : " + file.RELATIVE + "\n";
                                                }), DispatcherPriority.ContextIdle);
                                                c = ReciveCommand();
                                                if (c.Equals("+++++OK"))
                                                {
                                                    SendData("+++NEXT");
                                                    first = false;
                                                }
                                                else
                                                {
                                                    SendData("++++END");
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                        }
                        SendData("++++END");

                    }
                    // Shutdown and end connection

                    if (name.Equals("++++REG"))
                    {
                        Utils.User user;

                        int UserDim = reciveDimension();
                        Console.WriteLine("ciao");

                        SendData("+++++OK");
                        string UsernameReg = reciveCredentials(UserDim);

                        SendData("+++++OK");
                        int PassDim = reciveDimension();


                        SendData("+++++OK");
                        string PasswordReg = reciveCredentials(PassDim);


                        user = new Utils.User(UsernameReg, PasswordReg, this.dbConn);
                        user.register();

                    }

                }
                SendData("++CLOSE");
                Close();
                return;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "Main Loop");
                return;
            }
        }

        public void deleteEntryFromDataBase(String id, String path, Database.SQLiteDatabase dbconn)
        {
            string[] backupId;

            backupId = path.Split('\\');
            MessageBox.Show("Backup ID : " + backupId[0] + " " + backupId[1]);
            string query = String.Format("DELETE FROM BACKUP WHERE USER_ID='{0}' AND BACKUP_NAME='{1}' AND Version='{2}'", id, backupId[0], backupId[1]);
            dbconn.ExecuteNonQuery(query);
            return;
        }

    }
}
