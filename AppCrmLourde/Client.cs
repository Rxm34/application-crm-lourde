using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCrmLourde
{
    public class Client
    {
        public int IdCli { get; set; }

        public string NomCli { get; set; }

        public string PrenomCli { get; set; }

        public string MailCli { get; set; }

        public string TelCli { get; set; }

        public string VilleCli { get; set; }

        public string CPCli { get; set; }

        public string RueCli { get; set; }

        public string FullName => $" {NomCli} {PrenomCli}";

        public Client() 
        { 

        }

    }
}
