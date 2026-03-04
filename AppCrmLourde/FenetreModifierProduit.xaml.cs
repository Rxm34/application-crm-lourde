using System;
using System.Windows;

namespace AppCrmLourde
{
    public partial class FenetreModifierProduit : Window
    {
        private Produit produit;

        public FenetreModifierProduit(Produit produit)
        {
            InitializeComponent();
            this.produit = produit;
            this.DataContext = produit;

            // Pré-remplissage des champs
            NomTextBox.Text = produit.NomProd;
            DescriptionTextBox.Text = produit.DescProd;
            PrixTextBox.Text = produit.PrixProd.ToString();
            StockTextBox.Text = produit.StockProd.ToString();
        }

        private void Valider_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(NomTextBox.Text))
            {
                MessageBox.Show("Le nom du produit est obligatoire.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PrixTextBox.Text, out decimal prix) || prix < 0)
            {
                MessageBox.Show("Le prix doit être un nombre positif.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(StockTextBox.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Le stock doit être un entier positif.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 🔹 Mise à jour de l'objet produit en mémoire
            produit.NomProd = NomTextBox.Text.Trim();
            produit.DescProd = DescriptionTextBox.Text.Trim();
            produit.PrixProd = prix;
            produit.StockProd = stock;

            // 🔹 Fermer la fenêtre et signaler la validation
            this.DialogResult = true;
            this.Close();
        }

        private void Annuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
