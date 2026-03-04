using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class PageGestionProduit : Page
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private readonly string logFile = "logs.txt";

        private List<Produit> allEntries;
        public ObservableCollection<Produit> Produits { get; set; } = new ObservableCollection<Produit>();

        public PageGestionProduit()
        {
            InitializeComponent();
            this.DataContext = this;
            ChargerProduits();
        }

        private void LogAction(string action, string details)
        {
            string managerInfo = SessionManager.ManagerConnecte != null
                ? $"{SessionManager.ManagerConnecte.PrenomMan} {SessionManager.ManagerConnecte.NomMan} ({SessionManager.ManagerConnecte.MailMan})"
                : "Manager inconnu";

            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {managerInfo} : {action} : {details}";
            try { System.IO.File.AppendAllText(logFile, logEntry + Environment.NewLine); } catch { }
        }

        private void ChargerProduits()
        {
            Produits.Clear();
            allEntries = new List<Produit>();

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
                        Produit p = new Produit
                        {
                            IdProd = reader.GetInt32("IdProd"),
                            NomProd = reader.GetString("NomProd"),
                            DescProd = reader.GetString("DescProd"),
                            PrixProd = reader.GetDecimal("PrixProd"),
                            StockProd = reader.GetInt32("StockProd")
                        };

                        Produits.Add(p);
                        allEntries.Add(p);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement produits : " + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            FenetreAjouterProduit fenetre = new FenetreAjouterProduit();
            if (fenetre.ShowDialog() == true)
            {
                Produit produit = new Produit
                {
                    NomProd = fenetre.Nom,
                    DescProd = fenetre.Description,
                    PrixProd = fenetre.Prix,
                    StockProd = fenetre.Stock
                };

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = @"
                            INSERT INTO produits (NomProd, DescProd, PrixProd, StockProd)
                            VALUES (@nom, @desc, @prix, @stock)";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@nom", produit.NomProd);
                        cmd.Parameters.AddWithValue("@desc", produit.DescProd);
                        cmd.Parameters.AddWithValue("@prix", produit.PrixProd);
                        cmd.Parameters.AddWithValue("@stock", produit.StockProd);
                        cmd.ExecuteNonQuery();
                        produit.IdProd = (int)cmd.LastInsertedId;
                    }

                    Produits.Add(produit);
                    allEntries.Add(produit);

                    LogAction("Ajout produit",
                        $"ID:{produit.IdProd}, Nom:{produit.NomProd}, Prix:{produit.PrixProd}, Stock:{produit.StockProd}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur ajout produit : " + ex.Message);
                }
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            Produit produit = ProduitsDataGrid.SelectedItem as Produit;
            if (produit == null)
            {
                MessageBox.Show("Veuillez sélectionner un produit.");
                return;
            }

            Produit avant = new Produit
            {
                IdProd = produit.IdProd,
                NomProd = produit.NomProd,
                DescProd = produit.DescProd,
                PrixProd = produit.PrixProd,
                StockProd = produit.StockProd
            };

            FenetreModifierProduit fenetre = new FenetreModifierProduit(produit);
            if (fenetre.ShowDialog() == true)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = @"
                    UPDATE produits
                    SET NomProd=@nom, DescProd=@desc, PrixProd=@prix, StockProd=@stock
                    WHERE IdProd=@id";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@nom", produit.NomProd);
                        cmd.Parameters.AddWithValue("@desc", produit.DescProd);
                        cmd.Parameters.AddWithValue("@prix", produit.PrixProd);
                        cmd.Parameters.AddWithValue("@stock", produit.StockProd);
                        cmd.Parameters.AddWithValue("@id", produit.IdProd);
                        cmd.ExecuteNonQuery();
                    }

                    ProduitsDataGrid.Items.Refresh();

                    LogAction("Modification produit",
                        $"ID:{produit.IdProd}\n" +
                        $"AVANT → Nom:{avant.NomProd}, Desc:{avant.DescProd}, Prix:{avant.PrixProd}, Stock:{avant.StockProd}\n" +
                        $"APRÈS → Nom:{produit.NomProd}, Desc:{produit.DescProd}, Prix:{produit.PrixProd}, Stock:{produit.StockProd}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors de la mise à jour en base : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            Produit produit = ProduitsDataGrid.SelectedItem as Produit;
            if (produit == null)
            {
                MessageBox.Show("Veuillez sélectionner un produit.");
                return;
            }

            if (MessageBox.Show($"Supprimer le produit '{produit.NomProd}' ?", "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "DELETE FROM produits WHERE IdProd=@id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", produit.IdProd);
                        cmd.ExecuteNonQuery();
                    }

                    Produits.Remove(produit);
                    allEntries.RemoveAll(p => p.IdProd == produit.IdProd);

                    LogAction("Suppression produit",
                        $"ID:{produit.IdProd}, Nom:{produit.NomProd}, Prix:{produit.PrixProd}, Stock:{produit.StockProd}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur suppression produit : " + ex.Message);
                }
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string search = txtSearch.Text?.ToLower().Trim() ?? "";

            if (string.IsNullOrWhiteSpace(search))
            {
                ProduitsDataGrid.ItemsSource = Produits;
                return;
            }

            var resultats = allEntries.Where(p =>
                (p.NomProd ?? "").ToLower().Contains(search) ||
                (p.DescProd ?? "").ToLower().Contains(search) ||
                p.PrixProd.ToString().Contains(search) ||
                p.StockProd.ToString().Contains(search)
            ).ToList();

            ProduitsDataGrid.ItemsSource = resultats;
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new PageAccueil());
        }
    }
}
