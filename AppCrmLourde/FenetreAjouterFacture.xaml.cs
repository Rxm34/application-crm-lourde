using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class FenetreAjouterFacture : Window
    {
        private const string ConnexionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";

        // Cette propriété sera récupérée par la page précédente
        public Facture NouvelleFacture { get; private set; }

        public FenetreAjouterFacture()
        {
            InitializeComponent();
            ChargerClients();
            // Initialise la date par défaut sur aujourd'hui
            DateFacturePicker.SelectedDate = DateTime.Now;
        }

        private void ChargerClients()
        {
            var clients = new List<Client>();
            try
            {
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
                // FullName doit être une propriété dans ta classe Client
                ClientComboBox.DisplayMemberPath = "FullName";
                ClientComboBox.SelectedValuePath = "IdCli";

                if (clients.Count > 0) ClientComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des clients : " + ex.Message);
            }
        }

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            // Récupération du client sélectionné
            var selectedClient = ClientComboBox.SelectedItem as Client;
            if (selectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Création de l'objet Facture basé uniquement sur ce que contient ton XAML
            NouvelleFacture = new Facture
            {
                IdCli = selectedClient.IdCli,
                NomClient = selectedClient.NomCli + " " + selectedClient.PrenomCli,
                DateFact = DateFacturePicker.SelectedDate ?? DateTime.Now,
                PrixFact = 0, // Initialisé à 0 car pas de saisie de prix/produit ici
            };

            // On ferme la fenêtre en validant
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