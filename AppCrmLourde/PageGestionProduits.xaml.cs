using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class PageGestionProduits : Page
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private Produit produitSelectionne = null;

        public PageGestionProduits()
        {
            InitializeComponent();
            ChargerProduits();
        }

        private void ChargerProduits()
        {
            List<Produit> liste = new List<Produit>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM produits", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            liste.Add(new Produit
                            {
                                IdProd = reader.GetInt32("IdProd"),
                                NomProd = reader.GetString("NomProd"),
                                DescProd = reader.IsDBNull(reader.GetOrdinal("DescProd")) ? "" : reader.GetString("DescProd"),
                                PrixProd = reader.GetDecimal("PrixProd"),
                                StockProd = reader.GetInt32("StockProd")
                            });
                        }
                    }
                }
                ProduitsDataGrid.ItemsSource = liste;
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        // Remplir le formulaire quand on clique sur une ligne
        private void ProduitsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            produitSelectionne = ProduitsDataGrid.SelectedItem as Produit;
            if (produitSelectionne != null)
            {
                txtNom.Text = produitSelectionne.NomProd;
                txtPrix.Text = produitSelectionne.PrixProd.ToString();
                txtStock.Text = produitSelectionne.StockProd.ToString();
                txtDesc.Text = produitSelectionne.DescProd;
                btnEnregistrer.Content = "💾 Modifier le produit";
            }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNom.Text) || !decimal.TryParse(txtPrix.Text, out decimal prix))
            {
                MessageBox.Show("Veuillez remplir au moins le nom et un prix valide.");
                return;
            }

            int stock = int.TryParse(txtStock.Text, out int s) ? s : 0;

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query;
                    if (produitSelectionne == null) // AJOUT
                    {
                        query = "INSERT INTO produits (NomProd, DescProd, PrixProd, StockProd) VALUES (@nom, @desc, @prix, @stock)";
                    }
                    else // MODIFICATION
                    {
                        query = "UPDATE produits SET NomProd=@nom, DescProd=@desc, PrixProd=@prix, StockProd=@stock WHERE IdProd=@id";
                    }

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@nom", txtNom.Text.Trim());
                    cmd.Parameters.AddWithValue("@desc", txtDesc.Text.Trim());
                    cmd.Parameters.AddWithValue("@prix", prix);
                    cmd.Parameters.AddWithValue("@stock", stock);
                    if (produitSelectionne != null) cmd.Parameters.AddWithValue("@id", produitSelectionne.IdProd);

                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Produit enregistré avec succès !");
                ChargerProduits();
            }
            catch (Exception ex) { MessageBox.Show("Erreur enregistrement : " + ex.Message); }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (produitSelectionne == null) return;

            if (MessageBox.Show($"Supprimer {produitSelectionne.NomProd} ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        // Note: Cela échouera si le produit est déjà dans une facture (FK constraint)
                        var cmd = new MySqlCommand("DELETE FROM produits WHERE IdProd=@id", conn);
                        cmd.Parameters.AddWithValue("@id", produitSelectionne.IdProd);
                        cmd.ExecuteNonQuery();
                    }
                    ChargerProduits();
                }
                catch (Exception) { MessageBox.Show("Impossible de supprimer ce produit car il est lié à des factures existantes."); }
            }
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    }
}