using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace serverTcp.Utils
{
    class Function
    {
        public static String checkOrCreateFileDb()
        {
            string input = "testDB.s3db";
            FileStream fs = new FileStream(input, FileMode.OpenOrCreate);
            fs.Close();
            return input;

        }

        public static IPAddress checkIPAddress(string IPAdd)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(IPAdd, out ip))
                throw new InvalidOperationException("InvalidIPAddress");
            else
                return IPAddress.Parse(IPAdd);
        }

        public static Boolean existUser(User u, Database.SQLiteDatabase dbConn)
        {
            string query = String.Format("SELECT COUNT(*) FROM USERS WHERE Username='{0}' AND Password='{1}'", u.USERNAME, u.PASSWORD);
            if (Int32.Parse(dbConn.ExecuteScalar(query)) > 0) return true;
            else return false;
        }



        public static String GetSaltUser(User u, Database.SQLiteDatabase dbConn)
        {
            string salt = String.Format("SELECT Salt FROM USERS WHERE Username= '{0}'", u.USERNAME);
            return (dbConn.ExecuteScalar(salt));


        }

        public static string Get16CharacterGenerator()
        {
            string path = Path.GetRandomFileName();
            path = path.Replace(".", "");
            return path;
        }
    }
}
