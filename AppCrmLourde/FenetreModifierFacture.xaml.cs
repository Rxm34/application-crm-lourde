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

            LoadDependencies();
        }

        private void LoadDependencies()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnexionString))
                {
                    conn.Open();

                    // Chargement des Clients
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

                    // Chargement des Produits
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

                // Configuration des ComboBox
                ClientComboBox.ItemsSource = Clients;
                ClientComboBox.DisplayMemberPath = "FullName";
                ClientComboBox.SelectedValuePath = "IdCli";
                ClientComboBox.SelectedValue = facture.IdCli;

                ProduitComboBox.ItemsSource = Produits;
                ProduitComboBox.DisplayMemberPath = "NomProd";
                ProduitComboBox.SelectedValuePath = "IdProd";

                // Remplissage des champs à partir de la première ligne de facture
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
            // --- Validations ---
            var selectedClient = ClientComboBox.SelectedItem as Client;
            var selectedProduit = ProduitComboBox.SelectedItem as Produit;

            if (selectedClient == null || selectedProduit == null)
            {
                MessageBox.Show("Veuillez sélectionner un client et un produit.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QteTextBox.Text, out int qte) || qte <= 0)
            {
                MessageBox.Show("Quantité invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PrixProdTextBox.Text, out decimal prixProd) || prixProd <= 0)
            {
                MessageBox.Show("Prix produit invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PrixFactTextBox.Text, out decimal prixFact) || prixFact <= 0)
            {
                MessageBox.Show("Prix facture invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DateFacturePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Veuillez sélectionner une date.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // --- Mise à jour de l'objet en mémoire ---
            facture.IdCli = selectedClient.IdCli;
            facture.PrixFact = (double)prixFact;
            facture.DateFact = DateFacturePicker.SelectedDate.Value;

            var ligneModif = facture.Lignes.FirstOrDefault();
            if (ligneModif == null)
            {
                ligneModif = new LigneFact { IdFact = facture.IdFact };
                facture.Lignes.Add(ligneModif);
            }
            ligneModif.IdProd = selectedProduit.IdProd;
            ligneModif.Qte = qte;

            // --- Mise à jour Base de données (Transaction) ---
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnexionString))
                {
                    conn.Open();
                    using (MySqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Update de la table factures
                            string queryFact = "UPDATE factures SET IdCli=@idCli, PrixFact=@prixFact, DateFact=@dateFact WHERE IdFact=@idFact";
                            MySqlCommand cmdFact = new MySqlCommand(queryFact, conn, trans);
                            cmdFact.Parameters.AddWithValue("@idCli", facture.IdCli);
                            cmdFact.Parameters.AddWithValue("@prixFact", facture.PrixFact);
                            cmdFact.Parameters.AddWithValue("@dateFact", facture.DateFact);
                            cmdFact.Parameters.AddWithValue("@idFact", facture.IdFact);
                            cmdFact.ExecuteNonQuery();

                            // 2. Update de la table lignefact (détails du produit)
                            string queryLigne = "UPDATE lignefact SET IdProd=@idProd, Qte=@qte, PUProd=@pu WHERE IdFact=@idFact";
                            MySqlCommand cmdLigne = new MySqlCommand(queryLigne, conn, trans);
                            cmdLigne.Parameters.AddWithValue("@idProd", selectedProduit.IdProd);
                            cmdLigne.Parameters.AddWithValue("@qte", qte);
                            cmdLigne.Parameters.AddWithValue("@pu", prixProd);
                            cmdLigne.Parameters.AddWithValue("@idFact", facture.IdFact);
                            cmdLigne.ExecuteNonQuery();

                            trans.Commit();
                            Logger.Log($"Modification facture : ID {facture.IdFact}, Client {selectedClient.NomCli}");
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la mise à jour : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}