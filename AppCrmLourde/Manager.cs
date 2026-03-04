using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCrmLourde
{
    public class Manager
    {
        public int IdMan { get; set; }

        public string NomMan { get; set; }

        public string PrenomMan { get; set; }

        public string MailMan { get; set; }

        public string MdpMan { get; set; }

        public string FullName => $"{NomMan} {PrenomMan}";

        public Manager() { }
    }
}