using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class PageGestionFacture : Page
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private readonly string logFile = "logs.txt";

        private List<Facture> allEntries;
        public ObservableCollection<Facture> Factures { get; set; } = new ObservableCollection<Facture>();
        public List<Client> Clients { get; set; } = new List<Client>();
        public List<Produit> Produits { get; set; } = new List<Produit>();

        public PageGestionFacture()
        {
            InitializeComponent();
            this.DataContext = this;
            ChargerClients();
            ChargerProduits();
            ChargerFactures();
        }

        private void LogAction(string action, string details)
        {
            string managerInfo = SessionManager.ManagerConnecte != null
                ? $"{SessionManager.ManagerConnecte.PrenomMan} {SessionManager.ManagerConnecte.NomMan} ({SessionManager.ManagerConnecte.MailMan})"
                : "Manager inconnu";

            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {managerInfo} : {action} : {details}";
            try { File.AppendAllText(logFile, logEntry + Environment.NewLine); } catch { }
        }

        // ===================== CHARGEMENT =====================
        private void ChargerClients()
        {
            Clients.Clear();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM clients";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Clients.Add(new Client
                        {
                            IdCli = reader.GetInt32("IdCli"),
                            NomCli = reader.GetString("NomCli"),
                            PrenomCli = reader.GetString("PrenomCli")
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement clients : " + ex.Message); }
        }

        private void ChargerProduits()
        {
            Produits.Clear();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM produits";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Produits.Add(new Produit
                        {
                            IdProd = reader.GetInt32("IdProd"),
                            NomProd = reader.GetString("NomProd"),
                            PrixProd = reader.GetDecimal("PrixProd")
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement produits : " + ex.Message); }
        }

        private void ChargerFactures()
        {
            Factures.Clear();
            allEntries = new List<Facture>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT f.*, c.NomCli, c.PrenomCli, p.NomProd, p.PrixProd
                        FROM factures f
                        JOIN clients c ON f.IdCli = c.IdCli
                        JOIN produits p ON f.IdProd = p.IdProd";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Facture f = new Facture
                        {
                            IdFact = reader.GetInt32("IdFact"),
                            IdCli = reader.GetInt32("IdCli"),
                            NomClient = reader.GetString("NomCli") + " " + reader.GetString("PrenomCli"),
                            NomProduit = reader.GetString("NomProd"),
                            PrixFact = reader.GetDouble("PrixFact"),
                            DateFact = reader.GetDateTime("DateFact")
                        };
                        f.Lignes.Add(new LigneFact
                        {
                            IdFact = f.IdFact,
                            IdProd = reader.GetInt32("IdProd"),
                            Qte = reader.GetInt32("QteProd")
                        });
                        Factures.Add(f);
                        allEntries.Add(f);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement factures : " + ex.Message); }
        }

        // ===================== AJOUT =====================
        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            FenetreAjouterFacture fenetre = new FenetreAjouterFacture();
            if (fenetre.ShowDialog() == true && fenetre.NouvelleFacture != null)
            {
                Facture f = fenetre.NouvelleFacture;
                var client = Clients.FirstOrDefault(c => c.IdCli == f.IdCli);
                if (client != null) f.NomClient = client.NomCli + " " + client.PrenomCli;

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = @"INSERT INTO factures (IdCli, IdProd, QteProd, PrixProd, PrixFact, DateFact)
                                         VALUES (@cli, @prod, @qte, @prix, @prixfact, @date)";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@cli", f.IdCli);

                        var ligne = f.Lignes.FirstOrDefault();
                        if (ligne != null)
                        {
                            var produit = Produits.FirstOrDefault(p => p.IdProd == ligne.IdProd);
                            cmd.Parameters.AddWithValue("@prod", ligne.IdProd);
                            cmd.Parameters.AddWithValue("@qte", ligne.Qte);
                            cmd.Parameters.AddWithValue("@prix", produit != null ? (double)produit.PrixProd : 0);
                            if (produit != null) f.NomProduit = produit.NomProd;
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@prod", 0);
                            cmd.Parameters.AddWithValue("@qte", 0);
                            cmd.Parameters.AddWithValue("@prix", 0);
                        }

                        cmd.Parameters.AddWithValue("@prixfact", f.PrixFact);
                        cmd.Parameters.AddWithValue("@date", f.DateFact);
                        cmd.ExecuteNonQuery();
                        f.IdFact = (int)cmd.LastInsertedId;
                        if (ligne != null) ligne.IdFact = f.IdFact;
                    }

                    Factures.Add(f);
                    allEntries.Add(f);
                    LogAction("Ajout facture",
                        $"ID:{f.IdFact}, Client:{f.NomClient}, Prix:{f.PrixFact}, Date:{f.DateFact:dd/MM/yyyy}");
                }
                catch (Exception ex) { MessageBox.Show("Erreur ajout facture : " + ex.Message); }
            }
        }

        // ===================== MODIFICATION =====================
        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            Facture f = FacturesDataGrid.SelectedItem as Facture;
            if (f == null) { MessageBox.Show("Veuillez sélectionner une facture."); return; }

            Facture avant = new Facture
            {
                IdFact = f.IdFact,
                NomClient = f.NomClient,
                NomProduit = f.NomProduit,
                PrixFact = f.PrixFact,
                DateFact = f.DateFact
            };

            FenetreModifierFacture fenetre = new FenetreModifierFacture(f);
            if (fenetre.ShowDialog() == true)
            {
                var client = Clients.FirstOrDefault(c => c.IdCli == f.IdCli);
                if (client != null) f.NomClient = client.NomCli + " " + client.PrenomCli;

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = @"UPDATE factures SET IdCli=@cli, IdProd=@prod, QteProd=@qte, PrixProd=@prix, PrixFact=@prixfact, DateFact=@date
                                         WHERE IdFact=@id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@cli", f.IdCli);

                        var ligne = f.Lignes.FirstOrDefault();
                        if (ligne != null)
                        {
                            var produit = Produits.FirstOrDefault(p => p.IdProd == ligne.IdProd);
                            cmd.Parameters.AddWithValue("@prod", ligne.IdProd);
                            cmd.Parameters.AddWithValue("@qte", ligne.Qte);
                            cmd.Parameters.AddWithValue("@prix", produit != null ? (double)produit.PrixProd : 0);
                            if (produit != null) f.NomProduit = produit.NomProd;
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@prod", 0);
                            cmd.Parameters.AddWithValue("@qte", 0);
                            cmd.Parameters.AddWithValue("@prix", 0);
                        }

                        cmd.Parameters.AddWithValue("@prixfact", f.PrixFact);
                        cmd.Parameters.AddWithValue("@date", f.DateFact);
                        cmd.Parameters.AddWithValue("@id", f.IdFact);
                        cmd.ExecuteNonQuery();
                    }

                    FacturesDataGrid.Items.Refresh();
                    LogAction("Modification facture",
                        $"AVANT → ID:{avant.IdFact}, Client:{avant.NomClient}, Prix:{avant.PrixFact}, Date:{avant.DateFact:dd/MM/yyyy}\n" +
                        $"APRÈS → ID:{f.IdFact}, Client:{f.NomClient}, Prix:{f.PrixFact}, Date:{f.DateFact:dd/MM/yyyy}");
                }
                catch (Exception ex) { MessageBox.Show("Erreur modification facture : " + ex.Message); }
            }
        }

        // ===================== SUPPRESSION =====================
        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            Facture f = FacturesDataGrid.SelectedItem as Facture;
            if (f == null) { MessageBox.Show("Veuillez sélectionner une facture."); return; }

            if (MessageBox.Show($"Supprimer la facture #{f.IdFact} ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Facture avant = new Facture
                {
                    IdFact = f.IdFact,
                    NomClient = f.NomClient,
                    NomProduit = f.NomProduit,
                    PrixFact = f.PrixFact,
                    DateFact = f.DateFact
                };

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "DELETE FROM factures WHERE IdFact=@id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", f.IdFact);
                        cmd.ExecuteNonQuery();
                    }

                    Factures.Remove(f);
                    allEntries.Remove(f);
                    LogAction("Suppression facture",
                        $"ID:{avant.IdFact}, Client:{avant.NomClient}, Prix:{avant.PrixFact}, Date:{avant.DateFact:dd/MM/yyyy}");
                }
                catch (Exception ex) { MessageBox.Show("Erreur suppression facture : " + ex.Message); }
            }
        }

        // ===================== RECHERCHE =====================
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string search = txtSearch.Text?.ToLower().Trim() ?? "";
            if (string.IsNullOrWhiteSpace(search)) { FacturesDataGrid.ItemsSource = Factures; return; }

            var resultats = allEntries.Where(f =>
                f.IdFact.ToString().Contains(search) ||
                (f.NomClient ?? "").ToLower().Contains(search) ||
                (f.NomProduit ?? "").ToLower().Contains(search) ||
                f.PrixFact.ToString().Contains(search) ||
                f.DateFact.ToString("dd/MM/yyyy").Contains(search)
            ).ToList();

            FacturesDataGrid.ItemsSource = resultats;
        }

        // ===================== RETOUR =====================
        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new PageAccueil());
        }
    }
}
