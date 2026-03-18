using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class PageGestionFactures : Page
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;Convert Zero Datetime=True;";

        public PageGestionFactures()
        {
            InitializeComponent();
            ChargerFactures();
        }

        private void ChargerFactures()
        {
            List<Facture> liste = new List<Facture>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT f.*, c.NomCli, c.PrenomCli FROM factures f JOIN clients c ON f.IdCli = c.IdCli ORDER BY f.IdFact DESC";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            liste.Add(new Facture
                            {
                                IdFact = reader.GetInt32("IdFact"),
                                NomClient = reader.GetString("NomCli") + " " + reader.GetString("PrenomCli"),
                                DateFact = reader.GetDateTime("DateFact"),
                                PrixFact = reader.GetDouble("PrixFact")
                            });
                        }
                    }
                }
                FacturesDataGrid.ItemsSource = liste;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnModifierLignes_Click(object sender, RoutedEventArgs e)
        {
            var f = FacturesDataGrid.SelectedItem as Facture;
            if (f == null) return;

            // On ouvre la fenêtre que l'on a créée ensemble
            FenetreLignesFacture fen = new FenetreLignesFacture(f);
            fen.ShowDialog();
            ChargerFactures(); // Rafraîchir le prix total au retour
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            var f = FacturesDataGrid.SelectedItem as Facture;
            if (f == null) return;

            if (MessageBox.Show("Supprimer cette facture et rendre les produits au stock ?", "ADMIN", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                // Utilise ici la même logique de boucle UPDATE Stock + DELETE que nous avons faite ensemble
                // (Je ne la remets pas en entier pour ne pas surcharger, mais c'est la même)
                SupprimerFactureAvecStock(f.IdFact);
                ChargerFactures();
            }
        }

        private void SupprimerFactureAvecStock(int idF) { /* Code de transaction SQL vu précédemment */ }
        private void BtnRetour_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    }
}