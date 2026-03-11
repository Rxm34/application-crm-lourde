using System.Text.RegularExpressions;
using System.Windows;

namespace AppCrmLourde
{
    public partial class FenetreAjouterModifier : Window
    {
        private object personne; // Client ou Prospect
        private bool estModification; // savoir si c'est une modification ou un ajout

        public string Nom => txtNom.Text.Trim();
        public string Prenom => txtPrenom.Text.Trim();
        public string Mail => txtMail.Text.Trim();
        public string Telephone => txtTel.Text.Trim();
        public string Ville => txtVille.Text.Trim();
        public string CodePostal => txtCP.Text.Trim();
        public string Rue => txtRue.Text.Trim();
        public string TypeChoisi => cbType.SelectedItem?.ToString() ?? "Client";

        public FenetreAjouterModifier(object obj = null)
        {
            InitializeComponent();

            // Type par défaut
            cbType.Items.Clear();
            cbType.Items.Add("Client");
            cbType.Items.Add("Prospect");

            if (obj != null)
            {
                personne = obj;
                estModification = true;

                if (obj is Client)
                {
                    cbType.SelectedIndex = 0;
                    cbType.IsEnabled = false;
                }
                else if (obj is Prospect)
                {
                    cbType.SelectedIndex = 1;
                    cbType.IsEnabled = true; // Prospect can become a Client
                }
            }
            else
            {
                personne = null;
                estModification = false;
                cbType.SelectedIndex = 0;
            }

            ChargerChamps();
        }

        private void ChargerChamps()
        {
            if (personne is Client c)
            {
                txtNom.Text = c.NomCli;
                txtPrenom.Text = c.PrenomCli;
                txtMail.Text = c.MailCli;
                txtTel.Text = c.TelCli;
                txtVille.Text = c.VilleCli;
                txtCP.Text = c.CPCli;
                txtRue.Text = c.RueCli;
            }
            else if (personne is Prospect p)
            {
                txtNom.Text = p.NomProsp;
                txtPrenom.Text = p.PrenomProsp;
                txtMail.Text = p.MailProsp;
                txtTel.Text = p.TelProsp;
                txtVille.Text = p.VilleProsp;
                txtCP.Text = p.CPProsp;
                txtRue.Text = p.RueProsp;
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Nom) || string.IsNullOrWhiteSpace(Prenom))
            {
                MessageBox.Show("Nom et prénom obligatoires", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(Mail) &&
                !Regex.IsMatch(Mail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Email invalide", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(CodePostal) &&
                !Regex.IsMatch(CodePostal, @"^\d{5}$"))
            {
                MessageBox.Show("Code postal invalide (5 chiffres)", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Mettre à jour l'objet interne
            if (TypeChoisi == "Client")
            {
                // If it was a prospect, we create a new client
                if (personne == null || personne is Prospect)
                    personne = new Client();

                var c = personne as Client;
                c.NomCli = Nom;
                c.PrenomCli = Prenom;
                c.MailCli = Mail;
                c.TelCli = Telephone;
                c.VilleCli = Ville;
                c.CPCli = CodePostal;
                c.RueCli = Rue;
            }
            else
            {
                if (personne == null) personne = new Prospect();
                var p = personne as Prospect;
                p.NomProsp = Nom;
                p.PrenomProsp = Prenom;
                p.MailProsp = Mail;
                p.TelProsp = Telephone;
                p.VilleProsp = Ville;
                p.CPProsp = CodePostal;
                p.RueProsp = Rue;
            }

            this.DialogResult = true;
            this.Close();
        }

        // Retourne l'objet Client ou Prospect modifié
        public object GetPersonne()
        {
            return personne;
        }
    }
}
