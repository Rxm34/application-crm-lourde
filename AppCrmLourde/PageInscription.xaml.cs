using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class PageInscription : Page
    {
        // ===================== ATTRIBUTS =====================
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private readonly string logFile = "logs.txt";

        // ===================== CONSTRUCTEUR =====================
        public PageInscription()
        {
            InitializeComponent();
        }

        // ===================== LOG =====================
        private void LogAction(string action, string details)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {action} : {details}";
            try
            {
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch { }
        }

        // ===================== INSCRIPTION =====================
        private void btnInscription_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Text = "";
            lblMessage.Foreground = System.Windows.Media.Brushes.Red;

            string nom = txtNom.Text.Trim();
            string prenom = txtPrenom.Text.Trim();
            string email = txtEmail.Text.Trim();
            string mdp = txtMotDePasse.Password.Trim();
            string mdpConfirm = txtMotDePasseConfirm.Password.Trim();

            // 🔹 Vérifications (même logique que ProspectsClients)
            if (string.IsNullOrWhiteSpace(nom) ||
                string.IsNullOrWhiteSpace(prenom) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(mdp) ||
                string.IsNullOrWhiteSpace(mdpConfirm))
            {
                lblMessage.Text = "Tous les champs sont obligatoires.";
                return;
            }

            if (mdp != mdpConfirm)
            {
                lblMessage.Text = "Les mots de passe ne correspondent pas.";
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // 🔹 Vérification email existant
                    string checkQuery = "SELECT COUNT(*) FROM managers WHERE MailMan = @mail";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@mail", email);

                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (exists > 0)
                    {
                        lblMessage.Text = "Cet email est déjà utilisé.";
                        return;
                    }

                    // 🔹 Insertion manager
                    string insertQuery = @"
                        INSERT INTO managers (NomMan, PrenomMan, MailMan, MdpMan)
                        VALUES (@nom, @prenom, @mail, @mdp)";

                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@nom", nom);
                    insertCmd.Parameters.AddWithValue("@prenom", prenom);
                    insertCmd.Parameters.AddWithValue("@mail", email);
                    insertCmd.Parameters.AddWithValue("@mdp", mdp);
                    insertCmd.ExecuteNonQuery();
                }

                lblMessage.Foreground = System.Windows.Media.Brushes.Green;
                lblMessage.Text = "Inscription réussie. Vous pouvez vous connecter.";

                LogAction(
                    "Création Manager",
                    $"Nom:{nom}, Prénom:{prenom}, Email:{email}"
                );

                // Redirection connexion
                Hyperlink_Connexion_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'inscription : " + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);

                LogAction("Erreur Inscription", ex.Message);
            }
            finally
            {
                txtMotDePasse.Password = "";
                txtMotDePasseConfirm.Password = "";
            }
        }

        // ===================== NAVIGATION =====================
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
