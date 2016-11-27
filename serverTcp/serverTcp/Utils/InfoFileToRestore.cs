using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace serverTcp.Utils
{
    class InfoFileToRestore
    {
        private string absolutePath;
        private string relativePath;
        private string file;
        private long dimension;

        public InfoFileToRestore() { }
        public InfoFileToRestore(string absolute, string relative, string file,  long dimension)
        {
            this.absolutePath = absolute;
            this.relativePath = relative;
            this.file = file;
            this.dimension = dimension;
        }


        public string ABSOLUTE
        {
            get
            {
                return absolutePath;
            }
            set
            {
                absolutePath = value;
            }
        }

        public string RELATIVE
        {
            get
            {
                return relativePath;
            }
            set
            {
                relativePath = value;
            }
        }

        public string FILE
        {
            get
            {
                return file;
            }
            set
            {
                file = value;
            }
        }



        public long DIM
        {
            get
            {
                return dimension;
            }
            set
            {
                dimension = value;
            }
        }


    }
}
