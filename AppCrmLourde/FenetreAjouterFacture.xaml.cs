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
            ChargerProduits();

            // Associer TextChanged pour recalcul automatique du prix
            QteTextBox.TextChanged += QteTextBox_TextChanged;
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

        private void ChargerProduits()
        {
            Produits.Clear();
            using (var conn = new MySqlConnection(ConnexionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT IdProd, NomProd, PrixProd FROM produits", conn);
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

            ProduitComboBox.ItemsSource = Produits;
            ProduitComboBox.DisplayMemberPath = "NomProd";
            ProduitComboBox.SelectedValuePath = "IdProd";

            // Événement pour mettre à jour le prix automatiquement
            ProduitComboBox.SelectionChanged += ProduitComboBox_SelectionChanged;

            if (Produits.Count > 0)
            {
                ProduitComboBox.SelectedIndex = 0;
                // Forcer l'affichage du prix du produit dès l'ouverture
                ProduitComboBox_SelectionChanged(ProduitComboBox, null);
            }
        }

        private void ProduitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProduitComboBox.SelectedItem is Produit produitSelectionne)
            {
                // Mettre à jour le prix du produit
                PrixProdTextBox.Text = produitSelectionne.PrixProd.ToString("0.00");

                // Mettre à jour le prix total si la quantité est déjà saisie
                if (int.TryParse(QteTextBox.Text, out int qte))
                {
                    PrixFactTextBox.Text = (produitSelectionne.PrixProd * qte).ToString("0.00");
                }
                else
                {
                    PrixFactTextBox.Text = "0.00";
                }
            }
        }

        private void QteTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Recalculer le prix total lorsque la quantité change
            if (ProduitComboBox.SelectedItem is Produit produitSelectionne && int.TryParse(QteTextBox.Text, out int qte))
            {
                PrixFactTextBox.Text = (produitSelectionne.PrixProd * qte).ToString("0.00");
            }
            else
            {
                PrixFactTextBox.Text = "0.00";
            }
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

            var selectedProduit = ProduitComboBox.SelectedItem as Produit;
            if (selectedProduit == null)
            {
                MessageBox.Show("Veuillez sélectionner un produit.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int idProd = selectedProduit.IdProd;

            if (!int.TryParse(QteTextBox.Text, out int qte))
            {
                MessageBox.Show("Veuillez saisir une quantité valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NouvelleFacture = new Facture
            {
                IdCli = idCli,
                IdProd = idProd,
                QteProd = qte,
                PrixProd = Convert.ToDouble(selectedProduit.PrixProd),
                PrixFact = Convert.ToDouble(selectedProduit.PrixProd) * qte,
                DateFact = DateFacturePicker.SelectedDate ?? DateTime.Now
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
