using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class FenetreAjouterModifierLigne : Window
    {
        private const string ConnexionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private int _idFacture;
        private LigneFacture _ligneModif;
        public List<Produit> Produits { get; set; } = new List<Produit>();

        public FenetreAjouterModifierLigne(int idFacture, LigneFacture ligneModif = null)
        {
            InitializeComponent();
            _idFacture = idFacture;
            _ligneModif = ligneModif;
            ChargerProduits();

            if (_ligneModif != null)
            {
                this.Title = "Modifier une ligne";
                ProduitComboBox.SelectedValue = _ligneModif.IdProd;
                QteTextBox.Text = _ligneModif.QteProd.ToString();
            }
            else
            {
                this.Title = "Ajouter une ligne";
            }
        }

        private void ChargerProduits()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnexionString))
                {
                    conn.Open();
                    string query = "SELECT IdProd, NomProd, PrixProd FROM produits";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
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

                if (Produits.Count > 0 && _ligneModif == null)
                    ProduitComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des produits : " + ex.Message);
            }
        }

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            var selectedProduit = ProduitComboBox.SelectedItem as Produit;
            if (selectedProduit == null)
            {
                MessageBox.Show("Veuillez sélectionner un produit.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QteTextBox.Text, out int qte) || qte <= 0)
            {
                MessageBox.Show("Veuillez saisir une quantité valide et supérieure à 0.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnexionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;

                    if (_ligneModif == null)
                    {
                        cmd.CommandText = "INSERT INTO lignes_facture (IdFact, IdProd, QteProd, PrixProd) VALUES (@idFact, @idProd, @qte, @prix)";
                    }
                    else
                    {
                        cmd.CommandText = "UPDATE lignes_facture SET IdProd=@idProd, QteProd=@qte, PrixProd=@prix WHERE IdLigne=@idLigne";
                        cmd.Parameters.AddWithValue("@idLigne", _ligneModif.IdLigne);
                    }

                    cmd.Parameters.AddWithValue("@idFact", _idFacture);
                    cmd.Parameters.AddWithValue("@idProd", selectedProduit.IdProd);
                    cmd.Parameters.AddWithValue("@qte", qte);
                    cmd.Parameters.AddWithValue("@prix", selectedProduit.PrixProd);

                    cmd.ExecuteNonQuery();
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement de la ligne : " + ex.Message);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}