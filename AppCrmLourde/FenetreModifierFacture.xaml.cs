using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Linq;

namespace AppCrmLourde
{
    public partial class FenetreModifierFacture : Window
    {
        private const string ConnexionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";

        private Facture facture;

        public List<Client> Clients { get; set; } = new List<Client>();
        public List<Produit> Produits { get; set; } = new List<Produit>();

        public FenetreModifierFacture(Facture facture)
        {
            InitializeComponent();
            this.facture = facture;
            this.DataContext = this;

            // Charger les listes Client/Produit depuis MySQL
            LoadDependencies();
        }

        private void LoadDependencies()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnexionString))
                {
                    conn.Open();

                    // Clients
                    string queryClients = "SELECT IdCli, NomCli, PrenomCli FROM clients";
                    MySqlCommand cmdClients = new MySqlCommand(queryClients, conn);
                    using (var reader = cmdClients.ExecuteReader())
                    {
                        Clients.Clear();
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

                    // Produits
                    string queryProduits = "SELECT IdProd, NomProd, PrixProd FROM produits";
                    MySqlCommand cmdProduits = new MySqlCommand(queryProduits, conn);
                    using (var reader = cmdProduits.ExecuteReader())
                    {
                        Produits.Clear();
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

                // Bind ComboBox et remplir les champs existants
                ClientComboBox.ItemsSource = Clients;
                ClientComboBox.DisplayMemberPath = "FullName"; // Assurez-vous que FullName existe
                ClientComboBox.SelectedValuePath = "IdCli";
                ClientComboBox.SelectedValue = facture.IdCli;

                ProduitComboBox.ItemsSource = Produits;
                ProduitComboBox.DisplayMemberPath = "NomProd";
                ProduitComboBox.SelectedValuePath = "IdProd";

                var ligne = facture.Lignes.FirstOrDefault();
                if (ligne != null)
                {
                    ProduitComboBox.SelectedValue = ligne.IdProd;
                    QteTextBox.Text = ligne.Qte.ToString();
                    var produit = Produits.FirstOrDefault(p => p.IdProd == ligne.IdProd);
                    if (produit != null) PrixProdTextBox.Text = produit.PrixProd.ToString();
                }

                PrixFactTextBox.Text = facture.PrixFact.ToString();
                DateFacturePicker.SelectedDate = facture.DateFact;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des dépendances : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = false;
                this.Close();
            }
        }

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            // Vérification client
            var selectedClient = ClientComboBox.SelectedItem as Client;
            if (selectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Vérification produit
            var selectedProduit = ProduitComboBox.SelectedItem as Produit;
            if (selectedProduit == null)
            {
                MessageBox.Show("Veuillez sélectionner un produit.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Vérification des champs numériques
            if (!int.TryParse(QteTextBox.Text, out int qte) || qte <= 0)
            {
                MessageBox.Show("Quantité invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(PrixProdTextBox.Text, out double prixProd) || prixProd <= 0)
            {
                MessageBox.Show("Prix produit invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(PrixFactTextBox.Text, out double prixFact) || prixFact <= 0)
            {
                MessageBox.Show("Prix facture invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DateFacturePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Veuillez sélectionner une date de facture.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Mise à jour de l'objet facture en mémoire
            facture.IdCli = selectedClient.IdCli;
            facture.PrixFact = prixFact;
            facture.DateFact = DateFacturePicker.SelectedDate.Value;

            var ligneModif = facture.Lignes.FirstOrDefault();
            if (ligneModif == null)
            {
                ligneModif = new LigneFact { IdFact = facture.IdFact };
                facture.Lignes.Add(ligneModif);
            }
            ligneModif.IdProd = selectedProduit.IdProd;
            ligneModif.Qte = qte;

            // 🔹 Mise à jour dans MySQL
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnexionString))
                {
                    conn.Open();
                    string query = @"UPDATE factures
                                     SET IdCli=@idCli, IdProd=@idProd, QteProd=@qte, PrixProd=@prixProd, PrixFact=@prixFact, DateFact=@dateFact
                                     WHERE IdFact=@idFact";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@idCli", facture.IdCli);
                    cmd.Parameters.AddWithValue("@idProd", selectedProduit.IdProd);
                    cmd.Parameters.AddWithValue("@qte", qte);
                    cmd.Parameters.AddWithValue("@prixProd", prixProd);
                    cmd.Parameters.AddWithValue("@prixFact", facture.PrixFact);
                    cmd.Parameters.AddWithValue("@dateFact", facture.DateFact);
                    cmd.Parameters.AddWithValue("@idFact", facture.IdFact);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                        Logger.Log($"Modification facture : ID {facture.IdFact}, Client {selectedClient.NomCli}, Produit {selectedProduit.NomProd}");
                    else
                        MessageBox.Show("Aucune modification effectuée.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la modification de la facture : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
