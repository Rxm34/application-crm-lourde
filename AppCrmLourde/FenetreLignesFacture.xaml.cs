using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class FenetreLignesFacture : Window
    {
        private const string ConnexionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private Facture _facture;
        public ObservableCollection<LigneFacture> Lignes { get; set; } = new ObservableCollection<LigneFacture>();

        public FenetreLignesFacture(Facture facture)
        {
            InitializeComponent();
            _facture = facture;
            TxtInfoFacture.Text = $"Facture #{_facture.IdFact} - Client: {_facture.NomClient} - Date: {_facture.DateFact:dd/MM/yyyy}";
            LignesDataGrid.ItemsSource = Lignes;
            ChargerLignes();
        }

        private void ChargerLignes()
        {
            Lignes.Clear();
            double total = 0;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnexionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT l.IdLigne, l.IdFact, l.IdProd, p.NomProd, l.QteProd, l.PrixProd
                        FROM lignes_facture l
                        JOIN produits p ON l.IdProd = p.IdProd
                        WHERE l.IdFact = @idFact";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@idFact", _facture.IdFact);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            LigneFacture ligne = new LigneFacture
                            {
                                IdLigne = reader.GetInt32("IdLigne"),
                                IdFact = reader.GetInt32("IdFact"),
                                IdProd = reader.GetInt32("IdProd"),
                                NomProduit = reader.GetString("NomProd"),
                                QteProd = reader.GetInt32("QteProd"),
                                PrixProd = reader.GetDouble("PrixProd"),
                                PrixTotalLigne = reader.GetInt32("QteProd") * reader.GetDouble("PrixProd")
                            };
                            Lignes.Add(ligne);
                            total += ligne.PrixTotalLigne;
                        }
                    }
                }
                UpdateTotalFacture(total);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des lignes : " + ex.Message);
            }
        }

        private void UpdateTotalFacture(double total)
        {
            TxtTotalFacture.Text = $"Total Facture : {total:0.00} €";
            _facture.PrixFact = total;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnexionString))
                {
                    conn.Open();
                    string query = "UPDATE factures SET PrixFact = @prix WHERE IdFact = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@prix", total);
                    cmd.Parameters.AddWithValue("@id", _facture.IdFact);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la mise à jour du prix total de la facture : " + ex.Message);
            }
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            FenetreAjouterModifierLigne fenetre = new FenetreAjouterModifierLigne(_facture.IdFact);
            if (fenetre.ShowDialog() == true)
            {
                ChargerLignes();
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (LignesDataGrid.SelectedItem is LigneFacture ligne)
            {
                FenetreAjouterModifierLigne fenetre = new FenetreAjouterModifierLigne(_facture.IdFact, ligne);
                if (fenetre.ShowDialog() == true)
                {
                    ChargerLignes();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une ligne à modifier.");
            }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (LignesDataGrid.SelectedItem is LigneFacture ligne)
            {
                if (MessageBox.Show("Voulez-vous vraiment supprimer cette ligne ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (MySqlConnection conn = new MySqlConnection(ConnexionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM lignes_facture WHERE IdLigne = @id";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.Parameters.AddWithValue("@id", ligne.IdLigne);
                            cmd.ExecuteNonQuery();
                        }
                        ChargerLignes();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erreur lors de la suppression de la ligne : " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une ligne à supprimer.");
            }
        }

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}