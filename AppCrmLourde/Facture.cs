using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCrmLourde
{
    public class Facture
    {
        public int IdFact { get; set; }

        public int IdCli { get; set; }

        public int IdProd { get; set; }

        public int QteProd { get; set; }

        public double PrixProd { get; set; }

        public double PrixFact { get; set; }

        public string NomClient { get; set; }
        public string NomProduit { get; set; }

        public DateTime DateFact { get; set; }

        public Facture()
        {

        }
    }
}