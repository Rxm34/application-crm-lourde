using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCrmLourde
{
    public class Prospect
    {
        public int IdProsp { get; set; }

        public string NomProsp { get; set; }

        public string PrenomProsp { get; set; }

        public string MailProsp { get; set; }

        public string TelProsp { get; set; }

        public string VilleProsp { get; set; }

        public string CPProsp { get; set; }

        public string RueProsp { get; set; }
        public string FullName => $"{NomProsp} {PrenomProsp}";

        public Prospect()
        { 

        }
    }
}
