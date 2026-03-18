using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using BCrypt.Net; // Indispensable après l'installation du package

namespace AppCrmLourde
{
    public partial class PageInscription : Page
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private readonly string logFile = "logs.txt";

        public PageInscription()
        {
            InitializeComponent();
        }

        private void LogAction(string action, string details)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {action} : {details}";
            try { File.AppendAllText(logFile, logEntry + Environment.NewLine); } catch { }
        }

        private void btnInscription_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Text = "";
            lblMessage.Foreground = System.Windows.Media.Brushes.Red;

            string nom = txtNom.Text.Trim();
            string prenom = txtPrenom.Text.Trim();
            string email = txtEmail.Text.Trim();
            string mdpClair = txtMotDePasse.Password.Trim();
            string mdpConfirm = txtMotDePasseConfirm.Password.Trim();
            bool isAdmin = chkIsAdmin.IsChecked ?? false;

            // 1. Vérifications de base
            if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(mdpClair))
            {
                lblMessage.Text = "Tous les champs sont obligatoires.";
                return;
            }

            if (mdpClair != mdpConfirm)
            {
                lblMessage.Text = "Les mots de passe ne correspondent pas.";
                return;
            }

            // 2. Hachage du mot de passe (Cryptage)
            // On ne stocke jamais mdpClair, on génère une empreinte unique (le hash)
            string mdpHache = BCrypt.Net.BCrypt.HashPassword(mdpClair);

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Vérification email existant
                    string checkQuery = "SELECT COUNT(*) FROM managers WHERE MailMan = @mail";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@mail", email);

                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                    {
                        lblMessage.Text = "Cet email est déjà utilisé.";
                        return;
                    }

                    // 3. Insertion avec le mot de passe haché
                    string insertQuery = @"
                        INSERT INTO managers (NomMan, PrenomMan, MailMan, MdpMan, IsAdmin)
                        VALUES (@nom, @prenom, @mail, @mdp, @isAdmin)";

                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@nom", nom);
                    insertCmd.Parameters.AddWithValue("@prenom", prenom);
                    insertCmd.Parameters.AddWithValue("@mail", email);
                    insertCmd.Parameters.AddWithValue("@mdp", mdpHache); // On insère la version sécurisée
                    insertCmd.Parameters.AddWithValue("@isAdmin", isAdmin ? 1 : 0);

                    insertCmd.ExecuteNonQuery();
                }

                lblMessage.Foreground = System.Windows.Media.Brushes.Green;
                lblMessage.Text = "Inscription réussie !";

                LogAction("Création Manager", $"Nom:{nom}, Email:{email}, Admin:{isAdmin}");

                MessageBox.Show("Compte sécurisé créé avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                Hyperlink_Connexion_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message, "Erreur SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                LogAction("Erreur Inscription", ex.Message);
            }
        }

        private void Hyperlink_Connexion_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Content = null;
                mainWindow.ConnexionBorder.Visibility = Visibility.Visible;
            }
        }
    }
}