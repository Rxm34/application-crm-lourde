using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class FenetreAjouterFacture : Window
    {
        private const string ConnexionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        public Facture NouvelleFacture { get; private set; }
        private List<Produit> Produits = new List<Produit>();
    public FenetreAjouterFacture()
        {
            InitializeComponent();
            ChargerClients();
            DateFacturePicker.SelectedDate = DateTime.Now;
        }

        private void ChargerClients()
        {
            var clients = new List<Client>();
            using (var conn = new MySqlConnection(ConnexionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT IdCli, NomCli, PrenomCli FROM clients", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        clients.Add(new Client
                        {
                            IdCli = reader.GetInt32("IdCli"),
                            NomCli = reader.GetString("NomCli"),
                            PrenomCli = reader.GetString("PrenomCli")
                        });
                    }
                }
            }

            ClientComboBox.ItemsSource = clients;
            ClientComboBox.DisplayMemberPath = "FullName";
            ClientComboBox.SelectedValuePath = "IdCli";
            if (clients.Count > 0) ClientComboBox.SelectedIndex = 0;
        }

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            var selectedClient = ClientComboBox.SelectedItem as Client;
            if (selectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int idCli = selectedClient.IdCli;

            NouvelleFacture = new Facture
            {
                IdCli = idCli,
                PrixFact = 0,
                DateFact = DateFacturePicker.SelectedDate ?? DateTime.Now,
                NomClient = selectedClient.NomCli + " " + selectedClient.PrenomCli
            };

            this.DialogResult = true;
            this.Close();
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
