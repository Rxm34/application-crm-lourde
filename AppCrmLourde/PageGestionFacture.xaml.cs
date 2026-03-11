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
                        SELECT f.*, c.NomCli, c.PrenomCli
                        FROM factures f
                        JOIN clients c ON f.IdCli = c.IdCli";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Facture f = new Facture
                        {
                            IdFact = reader.GetInt32("IdFact"),
                            IdCli = reader.GetInt32("IdCli"),
                            NomClient = reader.GetString("NomCli") + " " + reader.GetString("PrenomCli"),
                            PrixFact = reader.GetDouble("PrixFact"),
                            DateFact = reader.GetDateTime("DateFact")
                        };
                        // Note: Lignes loading might not be strictly necessary if not displayed in DataGrid
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
                        string query = @"INSERT INTO factures (IdCli, PrixFact, DateFact)
                                         VALUES (@cli, @prixfact, @date)";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@cli", f.IdCli);
                        cmd.Parameters.AddWithValue("@prixfact", f.PrixFact);
                        cmd.Parameters.AddWithValue("@date", f.DateFact);
                        cmd.ExecuteNonQuery();
                        f.IdFact = (int)cmd.LastInsertedId;
                    }

                    Factures.Add(f);
                    allEntries.Add(f);
                    LogAction("Ajout facture",
                        $"ID:{f.IdFact}, Client:{f.NomClient}, Prix:{f.PrixFact}, Date:{f.DateFact:dd/MM/yyyy}");

                    // Ouvre automatiquement la fenêtre des lignes pour la nouvelle facture
                    FenetreLignesFacture fenetreLignes = new FenetreLignesFacture(f);
                    fenetreLignes.ShowDialog();
                    ChargerFactures();
                }
                catch (Exception ex) { MessageBox.Show("Erreur ajout facture : " + ex.Message); }
            }
        }

        // ===================== MODIFICATION =====================
        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            Facture f = FacturesDataGrid.SelectedItem as Facture;
            if (f == null) { MessageBox.Show("Veuillez sélectionner une facture."); return; }

            // Ouvre la nouvelle fenêtre de gestion des lignes
            FenetreLignesFacture fenetre = new FenetreLignesFacture(f);
            fenetre.ShowDialog();

            ChargerFactures();
        }

        // ===================== SUPPRESSION =====================
        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            Facture f = FacturesDataGrid.SelectedItem as Facture;
            if (f == null) { MessageBox.Show("Veuillez sélectionner une facture."); return; }

            if (MessageBox.Show($"Supprimer la facture #{f.IdFact} et toutes ses lignes ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        // Delete related lines first (or rely on ON DELETE CASCADE in the DB)
                        string queryLignes = "DELETE FROM lignes_facture WHERE IdFact=@id";
                        MySqlCommand cmdLignes = new MySqlCommand(queryLignes, conn);
                        cmdLignes.Parameters.AddWithValue("@id", f.IdFact);
                        cmdLignes.ExecuteNonQuery();

                        string query = "DELETE FROM factures WHERE IdFact=@id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", f.IdFact);
                        cmd.ExecuteNonQuery();
                    }

                    Factures.Remove(f);
                    allEntries.Remove(f);
                    LogAction("Suppression facture",
                        $"ID:{f.IdFact}, Client:{f.NomClient}, Prix:{f.PrixFact}, Date:{f.DateFact:dd/MM/yyyy}");
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
