using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace serverTcp.Utils
{
    class User
    {
        private String username;
        private String password;
        private String salt;
        private String id;
        private Database.SQLiteDatabase DBConn;

        public User() { }


        public User(String username)
        {
            this.username = username;
        }

        public User(String username, String password)
        {
            this.username = username;
            this.password = password;
        }

        public User(String username, String password, Database.SQLiteDatabase DBConn)
        {
            this.username = username;
            this.password = password;
            this.DBConn = DBConn;
        }


        public String ID
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        public String USERNAME
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
            }
        }

        public String PASSWORD
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }

        public String SALT
        {
            get
            {
                return salt;
            }
            set
            {
                salt = value;
            }
        }

        public bool register()
        {

            Network.HandleClient.DBManagerRegister(this.username, this.password, this.DBConn);

            return true;

        }

    }
}
