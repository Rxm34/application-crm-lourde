using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using BCrypt.Net; // Nécessite le package NuGet BCrypt.Net-Next

namespace AppCrmLourde
{
    public partial class MainWindow : Window
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";

        public MainWindow()
        {
            // Optionnel : force la culture française pour l'Euro et les dates
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");

            InitializeComponent();
        }

        private void btnConnexion_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Text = "";

            string email = txtEmail.Text.Trim();
            string passwordSaisi = txtMotDePasse.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(passwordSaisi))
            {
                string msg = "Veuillez saisir votre email et mot de passe.";
                lblMessage.Text = msg;
                lblMessage.Foreground = Brushes.Red;
                return;
            }

            try
            {
                Manager managerConnecte = null;

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // 1. On cherche l'utilisateur uniquement par son email
                    string query = "SELECT * FROM managers WHERE MailMan=@mail LIMIT 1";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@mail", email);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // On récupère le mot de passe haché qui est dans la base
                            string mdpHacheStocke = reader.GetString("MdpMan");

                            // 2. Vérification du mot de passe saisi avec le hachage
                            if (BCrypt.Net.BCrypt.Verify(passwordSaisi, mdpHacheStocke))
                            {
                                managerConnecte = new Manager
                                {
                                    IdMan = reader.GetInt32("IdMan"),
                                    PrenomMan = reader.GetString("PrenomMan"),
                                    NomMan = reader.GetString("NomMan"),
                                    MailMan = reader.GetString("MailMan"),
                                    IsAdmin = reader.GetBoolean("IsAdmin") // Récupération du rôle Admin
                                };
                            }
                        }
                    }
                }

                if (managerConnecte != null)
                {
                    // Connexion réussie
                    SessionManager.ManagerConnecte = managerConnecte;

                    lblMessage.Text = $"Bienvenue {managerConnecte.PrenomMan} !";
                    lblMessage.Foreground = Brushes.Green;

                    Logger.Log($"Connexion réussie : {managerConnecte.MailMan} (Admin: {managerConnecte.IsAdmin})");

                    // Masquer le panel de connexion et naviguer vers l'accueil
                    ConnexionBorder.Visibility = Visibility.Collapsed;
                    MainFrame.Navigate(new PageAccueil());
                }
                else
                {
                    // Email introuvable OU mot de passe incorrect
                    string msg = "Email ou mot de passe invalide.";
                    lblMessage.Text = msg;
                    lblMessage.Foreground = Brushes.Red;
                    Logger.Log($"Échec de connexion pour : {email}");
                }
            }
            catch (Exception ex)
            {
                string msg = "Erreur de connexion : " + ex.Message;
                Logger.Log(msg);
                MessageBox.Show(msg, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Sécurité : on vide toujours le champ mot de passe après l'essai
            txtMotDePasse.Password = string.Empty;
        }

        private void Hyperlink_Sinscrire_Click(object sender, RoutedEventArgs e)
        {
            ConnexionBorder.Visibility = Visibility.Collapsed;
            MainFrame.Navigate(new PageInscription());
        }
    }
}