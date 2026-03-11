using System;

namespace AppCrmLourde
{
    public class LigneFacture
    {
        public int IdLigne { get; set; }
        public int IdFact { get; set; }
        public int IdProd { get; set; }
        public string NomProduit { get; set; }
        public int QteProd { get; set; }
        public double PrixProd { get; set; }
        public double PrixTotalLigne { get; set; }

        public LigneFacture()
        {
        }
    }
}