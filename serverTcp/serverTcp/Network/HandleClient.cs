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

namespace serverTcp.Network
{
    class HandleClient
    {
        TcpClient client;
        NetworkStream ns;

        public HandleClient(TcpClient client)
        {
            this.client = client;
            this.ns = client.GetStream();
        }

        public void Close()
        {
            ns.Close();
            client.Close();
        }


        public void SendData(string data)
        {
            try
            {
                // Get a stream object for reading and writing
                Byte[] sendBytes = System.Text.Encoding.ASCII.GetBytes(data);
                ns.Write(sendBytes, 0, sendBytes.Length);
                ns.Flush();
            }
            catch (ObjectDisposedException e)
            {
                throw;
            }
            catch (SocketException e1)
            {
                throw;
            }
            catch (IOException e2)
            {
                throw;
            }


        }


        public int reciveDimension()
        {
            byte[] buf = new byte[client.ReceiveBufferSize];
            int len = ns.Read(buf, 0, client.ReceiveBufferSize);
            ns.Flush();
            return BitConverter.ToInt32(buf, 0);
        }

        public String reciveCredentials(int dim)
        {
            Byte[] bytes = new Byte[dim];
            ns.Read(bytes, 0, dim);
            return System.Text.Encoding.ASCII.GetString(bytes, 0, dim);
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



        public void ReciveXMLData(int dim, string fileName)
        {

            // Buffer for reading data
            Byte[] bytes = new Byte[1024];
            int lenght = 1024;
            //StringBuilder sb = new StringBuilder();
            int i;
            Int32 numberOfTotalBytes = dim;
            Int32 byteRecivied = 0;
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            Byte[] head = System.Text.Encoding.ASCII.GetBytes("<?xml version='1.0'?>\n");
            fs.Write(head, 0, head.Length);


            if (dim < lenght)
            {
                ns.Read(bytes, 0, dim);
                fs.Write(bytes, 0, dim);
                fs.Close();
                return;
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
            }

            return;
        }

        public void ReciveFile(String path, String name,  int dim)
        {

            // Buffer for reading data
            Byte[] bytes = new Byte[1024];
            int lenght = 1024;
            int i;
            Int32 numberOfTotalBytes = dim;
            Int32 byteRecivied = 0;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            FileStream fs = new FileStream(path+@"\"+name, FileMode.Create, FileAccess.ReadWrite);
            

            if (dim < lenght)
            {
                ns.Read(bytes, 0, dim);
                fs.Write(bytes, 0, dim);
                fs.Close();
                return;
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
            }

            return;
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
                    sql = String.Format("SELECT ID FROM BACKUP WHERE USER_ID='{0}' AND BACKUP_NAME='{1}' ORDER BY datetime(TIME) ASC LIMIT 1", id, backupID);
                    risultato = dbConn.ExecuteScalar(sql);
                    Console.WriteLine(risultato);
                    sql = String.Format("UPDATE BACKUP SET TIME='{0}' WHERE ID='{1}'", time, risultato);
                    Console.WriteLine(sql);
                    dbConn.ExecuteScalar(sql);  
                }
                else
                {
                    num += 1;
                    query = String.Format("INSERT INTO BACKUP(BACKUP_NAME, Version, TIME, CURRENT, USER_ID) VALUES('{0}', '{1}', '{2}', NULL, '{3}')", backupID, num, time, id);
                    dbConn.ExecuteScalar(query);
                    
                }
            }
            else
            {
                query = String.Format("INSERT INTO BACKUP(BACKUP_NAME, Version, TIME, CURRENT, USER_ID) VALUES('{0}', '{1}', '{2}', NULL, '{3}')", backupID, num, time, id);
                dbConn.ExecuteScalar(query);
                num += 1;
            }
            

            XmlNodeList element = root.ChildNodes;
            for (int i = 0; i < element.Count; i++)
            {
                XmlNodeList list = element[i].ChildNodes;
                String path = list.Item(1).FirstChild.Value;
                String name = list.Item(0).FirstChild.Value;
                long dim = long.Parse(list.Item(2).FirstChild.Value);
                String crc = list.Item(4).FirstChild.Value;
                DateTime timestamp = Convert.ToDateTime(list.Item(3).FirstChild.Value);
                files.Add(new clientTCP.Utils.FileInfomation(path, name, dim, System.Text.Encoding.ASCII.GetBytes(crc), Convert.ToDateTime(time)));
                                              
            }
            newPath = backupID + @"\" + num;
            return newPath;      
        }


        public List<String> restoreBackup(String fileName, String id)
        {
            return null;
        }
        
        ///AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAaaaaaaaaaaaaaaaaaaaaaaaaaa
        public static String DBManagerRegister(String Username, String Password, Database.SQLiteDatabase dbConn)
        {
            string Salt = RandomString(10);
           
            String sql = String.Format("INSERT INTO USERS (Username, Password,Salt) values ('{0}', '{1}', '{2}')",Username, hashPassword(Password, Salt),Salt);
            String risultato = dbConn.ExecuteScalar(sql);
            Console.WriteLine("Risulato: "+risultato);
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
            byte[] buf = BitConverter.GetBytes(dim);
            ns.Write(buf, 0, buf.Length);
            ns.Flush();

        }

        public void sendVersions(String data)
        {
            Byte[] dati = System.Text.Encoding.ASCII.GetBytes(data);
            ns.Write(dati, 0, dati.Length);
            ns.Flush();

        }

    }
}
