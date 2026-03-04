using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCrmLourde
{
    public class Contact
    {
        public int IdContact { get; set; }

        public Client IdCli { get; set; }

        public Prospect IdProsp { get; set; }

        public DateTime DateRdv { get; set; }

        public TimeSpan HeureRdv { get; set; }

        public int DureeRdv { get; set; } 

        public Contact()
        {
        }
    }
}