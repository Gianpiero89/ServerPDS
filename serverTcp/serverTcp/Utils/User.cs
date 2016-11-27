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
        private String id;

        public User() { }

        public User(String username, String password)
        {
            this.username = username;
            this.password = password;
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


    }
}
