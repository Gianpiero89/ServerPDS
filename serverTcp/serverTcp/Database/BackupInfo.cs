using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace serverTcp.Utils
{
    class BackupInfo
    {
        private String backup_id;
        private int version;
        private String path;
        private DateTime lastModify;
        private String crc;

        public BackupInfo(String id)
        {
            this.backup_id = id;
        }

        public int VERSION
        {
            get
            {
                return version;
            }
            set
            {
                version = value;
            }
        }

        public String PATH
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
            }
        }

        public DateTime LAST
        {
            get
            {
                return lastModify;
            }
            set
            {
                lastModify = value;
            }
        }

        public String CRC
        {
            get
            {
                return crc;
            }
            set
            {
                crc = value;
            }
        }

    }
}
