using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class MainWindow : Window
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnConnexion_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Text = "";

            string email = txtEmail.Text.Trim();
            string password = txtMotDePasse.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                string msg = "Veuillez saisir votre email et mot de passe.";
                lblMessage.Text = msg;
                lblMessage.Foreground = Brushes.Red;
                Logger.Log(msg);
                return;
            }

            try
            {
                Manager managerConnecte = null;

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM managers WHERE MailMan=@mail AND MdpMan=@pass LIMIT 1";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@mail", email);
                    cmd.Parameters.AddWithValue("@pass", password); // Si stocké en clair, sinon adapter avec hash

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            managerConnecte = new Manager
                            {
                                IdMan = reader.GetInt32("IdMan"),
                                PrenomMan = reader.GetString("PrenomMan"),
                                NomMan = reader.GetString("NomMan"),
                                MailMan = reader.GetString("MailMan")
                            };
                        }
                    }
                }

                if (managerConnecte != null)
                {
                    // Stockage du manager connecté
                    SessionManager.ManagerConnecte = managerConnecte;

                    string msg = $"Connexion réussie pour {managerConnecte.MailMan}";
                    lblMessage.Text = $"Connexion réussie ! Bienvenue {managerConnecte.PrenomMan} {managerConnecte.NomMan}";
                    lblMessage.Foreground = Brushes.Green;
                    Logger.Log(msg);

                    ConnexionBorder.Visibility = Visibility.Collapsed;
                    MainFrame.Navigate(new PageAccueil());
                }
                else
                {
                    string msg = "Email ou mot de passe invalide.";
                    lblMessage.Text = msg;
                    lblMessage.Foreground = Brushes.Red;
                    Logger.Log($"Tentative de connexion échouée pour {email} : {msg}");
                }
            }
            catch (Exception ex)
            {
                string msg = "Erreur inattendue lors de la connexion : " + ex.Message;
                Logger.Log(msg);
                MessageBox.Show(msg, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            txtMotDePasse.Password = string.Empty;
        }

        private void Hyperlink_Sinscrire_Click(object sender, RoutedEventArgs e)
        {
            ConnexionBorder.Visibility = Visibility.Collapsed;
            MainFrame.Navigate(new PageInscription());
        }
    }
}
