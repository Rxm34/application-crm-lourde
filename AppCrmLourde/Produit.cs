using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCrmLourde
{
    public class Produit
    {
        public int IdProd { get; set; }

        public string NomProd { get; set; }

        public string DescProd { get; set; }

        public decimal PrixProd { get; set; }

        public int StockProd { get; set; }
        public string FullName => $"{IdProd} - {NomProd}";
        public string AffichageStock => $"{NomProd} (Stock : {StockProd})";

        public Produit()
        {

        }
    }
}