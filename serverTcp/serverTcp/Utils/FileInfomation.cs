using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace clientTCP.Utils
{
    class FileInfomation
    {
        private String path;
        private String fileName;
        private int dimension;
        private DateTime modified;
        private Byte[] md5String;


        public FileInfomation(String path, String filename)
        {
            this.path = path;
            this.fileName = filename;
        }

        public FileInfomation(String path, String filename, int dimension, Byte[] digest, DateTime time)
        {
            this.path = path;
            this.fileName = filename;
            this.dimension = dimension;
            this.md5String = digest;
            this.modified = time;

        }

        public void ComputeHash(String filePath)
        {
            using (var md5 = SHA256Cng.Create())
            {
                Console.WriteLine(filePath);
                this.md5String = md5.ComputeHash(File.ReadAllBytes(filePath));
            }
        }

        public int DIMENSION
        {
            get
            {
                return dimension;
            }
            set
            {
                value = dimension;
            }
        }

        public DateTime Mod
        {
            get
            {
                return modified;
            }
            set
            {
                modified = value;
            }
        } 

        public Byte[] Md5
        {
            get
            {
                return md5String;
            }
            set
            {
                md5String = value;
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

        public String FILENAME
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }
        }

    }
}
