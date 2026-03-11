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
                var cmd = new MySqlCommand("SELECT * FROM produits", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) produits.Add(new Produit { IdProd = reader.GetInt32("IdProd"), NomProd = reader.GetString("NomProd"), PrixProd = reader.GetDecimal("PrixProd") });
                }
            }
            ProduitComboBox.ItemsSource = produits;
        }

        private void ChargerLignes()
        {
            List<dynamic> lignes = new List<dynamic>();
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = "SELECT l.*, p.NomProd FROM lignefact l JOIN produits p ON l.IdProd = p.IdProd WHERE l.IdFact = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", _facture.IdFact);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) lignes.Add(new { IdFact = reader.GetInt32("IdFact"), IdProd = reader.GetInt32("IdProd"), NomProduit = reader.GetString("NomProd"), Qte = reader.GetInt32("Qte"), PUProd = reader.GetDecimal("PUProd") });
                }
            }
            LignesDataGrid.ItemsSource = lignes;
        }

        private void BtnAjouterLigne_Click(object sender, RoutedEventArgs e)
        {
            var prod = ProduitComboBox.SelectedItem as Produit;
            if (prod == null || !int.TryParse(QteTextBox.Text, out int qte)) return;

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = "INSERT INTO lignefact (IdFact, IdProd, Qte, PUProd) VALUES (@idF, @idP, @qte, @pu) ON DUPLICATE KEY UPDATE Qte = Qte + @qte";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idF", _facture.IdFact);
                cmd.Parameters.AddWithValue("@idP", prod.IdProd);
                cmd.Parameters.AddWithValue("@qte", qte);
                cmd.Parameters.AddWithValue("@pu", prod.PrixProd);
                cmd.ExecuteNonQuery();
            }
            ChargerLignes();
        }

        private void BtnSupprimerLigne_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext;
            int idProd = (int)item.GetType().GetProperty("IdProd").GetValue(item);

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("DELETE FROM lignefact WHERE IdFact=@idF AND IdProd=@idP", conn);
                cmd.Parameters.AddWithValue("@idF", _facture.IdFact);
                cmd.Parameters.AddWithValue("@idP", idProd);
                cmd.ExecuteNonQuery();
            }
            ChargerLignes();
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}