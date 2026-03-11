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
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;Convert Zero Datetime=True;";
        private readonly string logFile = "logs.txt";

        private List<Facture> allEntries = new List<Facture>();
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

        private void ChargerClients()
        {
            Clients.Clear();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT IdCli, NomCli, PrenomCli FROM clients";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (var reader = cmd.ExecuteReader())
                    {
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
            }
            catch (Exception ex) { MessageBox.Show("Erreur clients: " + ex.Message); }
        }

        private void ChargerProduits()
        {
            Produits.Clear();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT IdProd, NomProd, PrixProd FROM produits";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (var reader = cmd.ExecuteReader())
                    {
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
            }
            catch (Exception ex) { MessageBox.Show("Erreur produits: " + ex.Message); }
        }

        private void ChargerFactures()
        {
            Factures.Clear();
            allEntries.Clear();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // Jointure simple pour l'affichage (Prend le premier produit trouvé pour la vue d'ensemble)
                    string query = @"
                        SELECT f.*, c.NomCli, c.PrenomCli 
                        FROM factures f
                        JOIN clients c ON f.IdCli = c.IdCli";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Factures.Add(new Facture
                            {
                                IdFact = reader.GetInt32("IdFact"),
                                IdCli = reader.GetInt32("IdCli"),
                                NomClient = reader.GetString("NomCli") + " " + reader.GetString("PrenomCli"),
                                PrixFact = reader.GetDouble("PrixFact"),
                                DateFact = reader.GetDateTime("DateFact")
                            });
                        }
                    }
                }
                allEntries = Factures.ToList();
                FacturesDataGrid.ItemsSource = Factures;
            }
            catch (Exception ex) { MessageBox.Show("Erreur factures: " + ex.Message); }
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            FenetreAjouterFacture fenetre = new FenetreAjouterFacture();
            if (fenetre.ShowDialog() == true && fenetre.NouvelleFacture != null)
            {
                Facture f = fenetre.NouvelleFacture;

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    MySqlTransaction trans = conn.BeginTransaction(); // Transaction pour assurer l'intégrité
                    try
                    {
                        // 1. Insertion Facture (Table factures)
                        string qFact = "INSERT INTO factures (IdCli, PrixFact, DateFact) VALUES (@cli, @prix, @date)";
                        MySqlCommand cmdF = new MySqlCommand(qFact, conn, trans);
                        cmdF.Parameters.AddWithValue("@cli", f.IdCli);
                        cmdF.Parameters.AddWithValue("@prix", f.PrixFact);
                        cmdF.Parameters.AddWithValue("@date", f.DateFact);
                        cmdF.ExecuteNonQuery();

                        f.IdFact = (int)cmdF.LastInsertedId;

                        // 2. Insertion Ligne (Table lignefact)
                        var ligne = f.Lignes.FirstOrDefault();
                        if (ligne != null)
                        {
                            var prod = Produits.FirstOrDefault(p => p.IdProd == ligne.IdProd);
                            string qLigne = "INSERT INTO lignefact (IdFact, IdProd, Qte, PUProd) VALUES (@idF, @idP, @qte, @pu)";
                            MySqlCommand cmdL = new MySqlCommand(qLigne, conn, trans);
                            cmdL.Parameters.AddWithValue("@idF", f.IdFact);
                            cmdL.Parameters.AddWithValue("@idP", ligne.IdProd);
                            cmdL.Parameters.AddWithValue("@qte", ligne.Qte);
                            cmdL.Parameters.AddWithValue("@pu", prod != null ? prod.PrixProd : 0);
                            cmdL.ExecuteNonQuery();
                        }

                        trans.Commit();
                        Factures.Add(f);
                        allEntries.Add(f);
                        LogAction("Ajout facture", $"ID:{f.IdFact}, Client:{f.NomClient}, Total:{f.PrixFact}");
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show("Erreur SQL : " + ex.Message);
                    }
                }
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            Facture f = FacturesDataGrid.SelectedItem as Facture;
            if (f == null) { MessageBox.Show("Veuillez sélectionner une facture."); return; }

            // On ouvre la fenêtre de gestion des lignes
            FenetreLignesFacture fenetre = new FenetreLignesFacture(f);
            fenetre.ShowDialog();

            // Au retour, on recalcule et on rafraîchit
            RecalculerTotalFacture(f.IdFact);
            ChargerFactures();
        }

        // Ajoute cette méthode à la fin de ta classe
        public void RecalculerTotalFacture(int idFacture)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // Somme de toutes les lignes pour cette facture
                    string query = @"UPDATE factures 
                             SET PrixFact = (SELECT IFNULL(SUM(Qte * PUProd), 0) FROM lignefact WHERE IdFact = @id)
                             WHERE IdFact = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", idFacture);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur recalcul total : " + ex.Message); }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            Facture f = FacturesDataGrid.SelectedItem as Facture;
            if (f == null) return;

            if (MessageBox.Show($"Supprimer la facture #{f.IdFact} ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        // Supprime d'abord les lignes (Foreign Key)
                        new MySqlCommand($"DELETE FROM lignefact WHERE IdFact={f.IdFact}", conn).ExecuteNonQuery();
                        // Supprime la facture
                        new MySqlCommand($"DELETE FROM factures WHERE IdFact={f.IdFact}", conn).ExecuteNonQuery();
                    }
                    Factures.Remove(f);
                    allEntries.Remove(f);
                    LogAction("Suppression facture", $"ID:{f.IdFact}");
                }
                catch (Exception ex) { MessageBox.Show("Erreur: " + ex.Message); }
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string s = txtSearch.Text.ToLower().Trim();
            if (string.IsNullOrEmpty(s)) { FacturesDataGrid.ItemsSource = Factures; return; }

            var res = allEntries.Where(f =>
                f.NomClient.ToLower().Contains(s) ||
                f.IdFact.ToString().Contains(s)
            ).ToList();
            FacturesDataGrid.ItemsSource = res;
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new PageAccueil());
        }
    }
}