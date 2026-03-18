using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCrmLourde
{
    public class LigneFact
    {
        public int IdLigne { get; set; }
        public int IdFact { get; set; }
        public int IdProd { get; set; }
        public int Qte { get; set; }
        public double PUProd { get; set; }

        public LigneFact()
        {
        }
    }
}
