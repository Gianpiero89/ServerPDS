using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace serverTcp.Utils
{
    class InfoFileToRestore
    {
        private string path;
        private long dimension;

        public InfoFileToRestore() { }
        public InfoFileToRestore(string path, long dimension)
        {
            this.path = path;
            this.dimension = dimension;
        }


        public string PATH
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
