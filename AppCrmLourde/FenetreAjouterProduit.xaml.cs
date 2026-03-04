using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class FenetreAjouterProduit : Window
    {
        private const string ConnexionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private Produit produit; // objet interne (ajout ou modification)

        // ================= PROPRIÉTÉS PUBLIQUES =================
        public string Nom => NomTextBox.Text.Trim();
        public string Description => DescriptionTextBox.Text.Trim();
        public decimal Prix => decimal.TryParse(PrixTextBox.Text, out var p) ? p : 0;
        public int Stock => int.TryParse(StockTextBox.Text, out var s) ? s : 0;

        // ================= CONSTRUCTEUR =================
        public FenetreAjouterProduit(Produit p = null)
        {
            InitializeComponent();
            produit = p ?? new Produit();
            DataContext = this;
            LoadDependencies();
        }

        // ================= CHARGEMENT =================
        private void LoadDependencies()
        {
            try
            {
                // Pré-remplissage si modification
                NomTextBox.Text = produit.NomProd ?? "";
                DescriptionTextBox.Text = produit.DescProd ?? "";
                PrixTextBox.Text = produit.PrixProd > 0 ? produit.PrixProd.ToString() : "";
                StockTextBox.Text = produit.StockProd >= 0 ? produit.StockProd.ToString() : "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        // ================= VALIDATION =================
        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NomTextBox.Text) || string.IsNullOrWhiteSpace(PrixTextBox.Text) || string.IsNullOrWhiteSpace(StockTextBox.Text))
            {
                MessageBox.Show("Le Nom, le Prix et le Stock sont obligatoires.", "Erreur de validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PrixTextBox.Text, out decimal prix) || prix <= 0)
            {
                MessageBox.Show("Le prix doit être un nombre décimal positif.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(StockTextBox.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Le stock doit être un entier positif.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Mise à jour de l'objet interne
            produit.NomProd = Nom;
            produit.DescProd = Description;
            produit.PrixProd = Prix;
            produit.StockProd = Stock;

            DialogResult = true;
            Close();
        }

        // ================= ANNULER =================
        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ================= MÉTHODE POUR RÉCUPÉRER LE PRODUIT =================
        public Produit GetProduit()
        {
            return produit;
        }
    }
}
