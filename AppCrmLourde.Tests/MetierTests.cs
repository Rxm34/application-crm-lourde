using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AppCrmLourde.Tests
{
    [TestClass]
    public class MetierTests
    {
        // ===================== FACTURE =====================
        [TestMethod]
        public void CalculTotalFactures_Correct()
        {
            var factures = new List<Facture>
            {
                new Facture { PrixFact = 100 },
                new Facture { PrixFact = 50 }
            };

            double total = factures.Sum(f => f.PrixFact);
            Assert.AreEqual(150, total);
        }

        [TestMethod]
        public void TopClient_Correct()
        {
            var factures = new List<Facture>
            {
                new Facture { NomClient = "A", PrixFact = 100 },
                new Facture { NomClient = "B", PrixFact = 200 },
                new Facture { NomClient = "A", PrixFact = 50 }
            };

            var topClient = factures
                .GroupBy(f => f.NomClient)
                .Select(g => new { Client = g.Key, Total = g.Sum(a => a.PrixFact) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault()?.Client ?? "Aucun";

            Assert.AreEqual("B", topClient);
        }

        [TestMethod]
        public void TopClientVide_Correct()
        {
            var factures = new List<Facture>();
            var topClient = factures
                .GroupBy(f => f.NomClient)
                .Select(g => new { Client = g.Key, Total = g.Sum(a => a.PrixFact) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault()?.Client ?? "Aucun";

            Assert.AreEqual("Aucun", topClient);
        }

        [TestMethod]
        public void TotalProduitsVendus_Correct()
        {
            var factures = new List<Facture>
            {
                new Facture { Lignes = new List<LigneFact> { new LigneFact { Qte = 3 } } },
                new Facture { Lignes = new List<LigneFact> { new LigneFact { Qte = 5 } } }
            };

            int totalQte = factures.Sum(f => f.Lignes.Sum(l => l.Qte));
            Assert.AreEqual(8, totalQte);
        }

        [TestMethod]
        public void CalculMarge_Correct()
        {
            var facture = new Facture { PrixFact = 100 };
            facture.Lignes.Add(new LigneFact { Qte = 5 });
            double prixProd = 12;
            double marge = facture.PrixFact - (prixProd * facture.Lignes[0].Qte); // 100 - 60 = 40
            Assert.AreEqual(40, marge);
        }

        [TestMethod]
        public void CaParMois_Correct()
        {
            var factures = new List<Facture>
            {
                new Facture { DateFact = new DateTime(2025,1,10), PrixFact = 100 },
                new Facture { DateFact = new DateTime(2025,1,15), PrixFact = 50 },
                new Facture { DateFact = new DateTime(2025,2,5), PrixFact = 200 }
            };

            var caJanvier = factures.Where(f => f.DateFact.Month == 1).Sum(f => f.PrixFact);
            var caFevrier = factures.Where(f => f.DateFact.Month == 2).Sum(f => f.PrixFact);

            Assert.AreEqual(150, caJanvier);
            Assert.AreEqual(200, caFevrier);
        }

        [TestMethod]
        public void CaMoisVide_Correct()
        {
            var factures = new List<Facture>();
            double total = factures.Where(f => f.DateFact.Month == DateTime.Now.Month).Sum(f => f.PrixFact);
            Assert.AreEqual(0, total);
        }

        // ===================== CONTACT =====================
        [TestMethod]
        public void NbRdvMois_Correct()
        {
            var contacts = new List<Contact>
            {
                new Contact { DateRdv = new DateTime(2025,1,5) },
                new Contact { DateRdv = new DateTime(2025,1,20) },
                new Contact { DateRdv = new DateTime(2025,2,15) }
            };

            int rdvJanvier = contacts.Count(c => c.DateRdv.Month == 1);
            int rdvFevrier = contacts.Count(c => c.DateRdv.Month == 2);

            Assert.AreEqual(2, rdvJanvier);
            Assert.AreEqual(1, rdvFevrier);
        }

        [TestMethod]
        public void EstConflitRendezVous_Correct()
        {
            var contacts = new List<Contact>
            {
                new Contact { DateRdv = new DateTime(2025,1,1), HeureRdv = new TimeSpan(10,0,0), DureeRdv = 2 },
                new Contact { DateRdv = new DateTime(2025,1,1), HeureRdv = new TimeSpan(13,0,0), DureeRdv = 1 }
            };

            TimeSpan nouveauDebut = new TimeSpan(11, 0, 0);
            TimeSpan nouveauFin = nouveauDebut + TimeSpan.FromHours(1);

            bool conflit = contacts.Any(r =>
                r.DateRdv.Date == new DateTime(2025, 1, 1) &&
                nouveauDebut < r.HeureRdv + TimeSpan.FromHours(r.DureeRdv) &&
                nouveauFin > r.HeureRdv
            );

            Assert.IsTrue(conflit);
        }

        [TestMethod]
        public void EstConflitRendezVous_PasConflit()
        {
            var contacts = new List<Contact>
            {
                new Contact { DateRdv = new DateTime(2025,1,1), HeureRdv = new TimeSpan(10,0,0), DureeRdv = 2 }
            };

            TimeSpan nouveauDebut = new TimeSpan(13, 0, 0);
            TimeSpan nouveauFin = nouveauDebut + TimeSpan.FromHours(1);

            bool conflit = contacts.Any(r =>
                r.DateRdv.Date == new DateTime(2025, 1, 1) &&
                nouveauDebut < r.HeureRdv + TimeSpan.FromHours(r.DureeRdv) &&
                nouveauFin > r.HeureRdv
            );

            Assert.IsFalse(conflit);
        }

        // ===================== PRODUIT =====================
        [TestMethod]
        public void PrixTotalProduit_Correct()
        {
            var produit = new Produit { PrixProd = 15, StockProd = 3 };
            double total = Convert.ToDouble(produit.PrixProd) * produit.StockProd;
            Assert.AreEqual(45, total);
        }

        [TestMethod]
        public void VerificationStock_Correct()
        {
            var produit = new Produit { StockProd = 10 };
            bool stockOk = produit.StockProd >= 5;
            Assert.IsTrue(stockOk);
        }

        [TestMethod]
        public void StockInsuffisant_Correct()
        {
            var produit = new Produit { StockProd = 2 };
            bool stockOk = produit.StockProd >= 5;
            Assert.IsFalse(stockOk);
        }

        [TestMethod]
        public void PrixZeroProduit_Correct()
        {
            var produit = new Produit { PrixProd = 0, StockProd = 10 };
            double total = Convert.ToDouble(produit.PrixProd) * produit.StockProd;
            Assert.AreEqual(0, total);
        }

        // ===================== CLIENT =====================
        [TestMethod]
        public void FacturesParClient_Correct()
        {
            var factures = new List<Facture>
            {
                new Facture { NomClient = "Client1", PrixFact = 100 },
                new Facture { NomClient = "Client2", PrixFact = 200 },
                new Facture { NomClient = "Client1", PrixFact = 50 }
            };

            var totalClient1 = factures.Where(f => f.NomClient == "Client1").Sum(f => f.PrixFact);
            Assert.AreEqual(150, totalClient1);
        }

        [TestMethod]
        public void ClientSansFacture_Correct()
        {
            var factures = new List<Facture>();
            var totalClient = factures.Where(f => f.NomClient == "ClientX").Sum(f => f.PrixFact);
            Assert.AreEqual(0, totalClient);
        }

        // ===================== PROSPECT =====================
        [TestMethod]
        public void ContactsAvecProspects_Correct()
        {
            var contacts = new List<Contact>
            {
                new Contact { IdProsp = new Prospect { NomProsp = "Prospect1" } },
                new Contact { IdProsp = new Prospect { NomProsp = "Prospect2" } },
                new Contact { IdCli = new Client { NomCli = "Client1" } }
            };

            int nbProspects = contacts.Count(c => c.IdProsp != null);
            Assert.AreEqual(2, nbProspects);
        }

        [TestMethod]
        public void ContactsSansProspect_Correct()
        {
            var contacts = new List<Contact>
            {
                new Contact { IdCli = new Client { NomCli = "Client1" } }
            };
            int nbProspects = contacts.Count(c => c.IdProsp != null);
            Assert.AreEqual(0, nbProspects);
        }

        // ===================== CAS LIMITE =====================
        [TestMethod]
        public void FactureQuantiteZero_Correct()
        {
            var facture = new Facture { PrixFact = 100 };
            facture.Lignes.Add(new LigneFact { Qte = 0 });
            double prixProd = 50;
            double marge = facture.PrixFact - (prixProd * facture.Lignes[0].Qte);
            Assert.AreEqual(100, marge);
        }

        [TestMethod]
        public void RendezVousDureeZero_Correct()
        {
            var contact = new Contact { DateRdv = DateTime.Today, HeureRdv = new TimeSpan(10, 0, 0), DureeRdv = 0 };
            TimeSpan fin = contact.HeureRdv + TimeSpan.FromHours(contact.DureeRdv);
            Assert.AreEqual(contact.HeureRdv, fin);
        }
    }
}
