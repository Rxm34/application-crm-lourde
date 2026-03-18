using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class FenetreLignesFacture : Window
    {
        private string connStr = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private Facture _facture;

        public FenetreLignesFacture(Facture f)
        {
            InitializeComponent();
            _facture = f;
            ChargerProduits();
            ChargerLignes();
        }

        private void ChargerProduits()
        {
            List<Produit> produits = new List<Produit>();
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT IdProd, NomProd, PrixProd, StockProd FROM produits", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        produits.Add(new Produit
                        {
                            IdProd = reader.GetInt32("IdProd"),
                            NomProd = reader.GetString("NomProd"),
                            PrixProd = reader.GetDecimal("PrixProd"),
                            StockProd = reader.GetInt32("StockProd") // On récupère le stock
                        });
                    }
                }
            }
            ProduitComboBox.ItemsSource = produits;
            // On change le chemin d'affichage pour utiliser notre nouvelle propriété
            ProduitComboBox.DisplayMemberPath = "AffichageStock";
        }

        private void ChargerLignes()
        {
            List<dynamic> lignes = new List<dynamic>();
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                // On ajoute l.IdLigne dans le SELECT
                string query = "SELECT l.IdLigne, l.IdFact, l.IdProd, l.Qte, l.PUProd, p.NomProd " +
                               "FROM lignefact l JOIN produits p ON l.IdProd = p.IdProd " +
                               "WHERE l.IdFact = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", _facture.IdFact);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lignes.Add(new
                        {
                            IdLigne = reader.GetInt32("IdLigne"), // IMPORTANT
                            IdFact = reader.GetInt32("IdFact"),
                            IdProd = reader.GetInt32("IdProd"),
                            NomProduit = reader.GetString("NomProd"),
                            Qte = reader.GetInt32("Qte"),
                            PUProd = reader.GetDecimal("PUProd")
                        });
                    }
                }
            }
            LignesDataGrid.ItemsSource = lignes;
        }

        private void BtnAjouterLigne_Click(object sender, RoutedEventArgs e)
        {
            var prod = ProduitComboBox.SelectedItem as Produit;
            if (prod == null || !int.TryParse(QteTextBox.Text, out int qte) || qte <= 0)
            {
                MessageBox.Show("Veuillez saisir une quantité valide.");
                return;
            }

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                // Utilisation d'une transaction pour garantir que le stock et la ligne sont mis à jour ensemble
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Vérifier le stock actuel
                        string checkStockQuery = "SELECT StockProd FROM produits WHERE IdProd = @idP";
                        MySqlCommand cmdCheck = new MySqlCommand(checkStockQuery, conn, trans);
                        cmdCheck.Parameters.AddWithValue("@idP", prod.IdProd);
                        int stockActuel = Convert.ToInt32(cmdCheck.ExecuteScalar());

                        if (stockActuel < qte)
                        {
                            MessageBox.Show($"Stock insuffisant ! Il ne reste que {stockActuel} unités de ce produit.", "Erreur Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return; // On arrête tout ici
                        }

                        // 2. Ajouter/Mettre à jour la ligne de facture
                        string queryLigne = "INSERT INTO lignefact (IdFact, IdProd, Qte, PUProd) VALUES (@idF, @idP, @qte, @pu) " +
                                             "ON DUPLICATE KEY UPDATE Qte = Qte + @qte";
                        MySqlCommand cmdLigne = new MySqlCommand(queryLigne, conn, trans);
                        cmdLigne.Parameters.AddWithValue("@idF", _facture.IdFact);
                        cmdLigne.Parameters.AddWithValue("@idP", prod.IdProd);
                        cmdLigne.Parameters.AddWithValue("@qte", qte);
                        cmdLigne.Parameters.AddWithValue("@pu", prod.PrixProd);
                        cmdLigne.ExecuteNonQuery();

                        // 3. Mettre à jour le stock (Soustraction)
                        string updateStockQuery = "UPDATE produits SET StockProd = StockProd - @qte WHERE IdProd = @idP";
                        MySqlCommand cmdUpdateStock = new MySqlCommand(updateStockQuery, conn, trans);
                        cmdUpdateStock.Parameters.AddWithValue("@qte", qte);
                        cmdUpdateStock.Parameters.AddWithValue("@idP", prod.IdProd);
                        cmdUpdateStock.ExecuteNonQuery();

                        trans.Commit(); // On valide les changements
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback(); // En cas d'erreur, on annule tout
                        MessageBox.Show("Erreur lors de l'ajout : " + ex.Message);
                    }
                }
            }
            ChargerLignes();
            ChargerProduits();
        }

        private void BtnSupprimerLigne_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext;

            // On récupère l'IdLigne (unique) et les infos pour le stock
            int idLigne = (int)item.GetType().GetProperty("IdLigne").GetValue(item);
            int idProd = (int)item.GetType().GetProperty("IdProd").GetValue(item);
            int qteARendre = (int)item.GetType().GetProperty("Qte").GetValue(item);

            if (MessageBox.Show("Supprimer cet article ?", "Confirmation", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Rendre le stock au produit
                        MySqlCommand cmdStock = new MySqlCommand("UPDATE produits SET StockProd = StockProd + @qte WHERE IdProd = @idP", conn, trans);
                        cmdStock.Parameters.AddWithValue("@qte", qteARendre);
                        cmdStock.Parameters.AddWithValue("@idP", idProd);
                        cmdStock.ExecuteNonQuery();

                        // 2. Supprimer UNIQUEMENT la ligne concernée via son IdLigne
                        MySqlCommand cmdDel = new MySqlCommand("DELETE FROM lignefact WHERE IdLigne=@idL", conn, trans);
                        cmdDel.Parameters.AddWithValue("@idL", idLigne);
                        cmdDel.ExecuteNonQuery();

                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show("Erreur lors de la suppression : " + ex.Message);
                    }
                }
            }
            ChargerLignes();
            ChargerProduits(); // Pour rafraîchir l'affichage du stock dans la ComboBox
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}