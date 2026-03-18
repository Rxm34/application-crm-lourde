using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using BCrypt.Net;

namespace AppCrmLourde
{
    public partial class PageGestionManagers : Page
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private Manager managerSelectionne = null;

        public PageGestionManagers()
        {
            InitializeComponent();
            ChargerManagers();
        }

        private void ChargerManagers()
        {
            List<Manager> liste = new List<Manager>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM managers ORDER BY NomMan ASC", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            liste.Add(new Manager
                            {
                                IdMan = reader.GetInt32("IdMan"),
                                NomMan = reader.GetString("NomMan"),
                                PrenomMan = reader.GetString("PrenomMan"),
                                MailMan = reader.GetString("MailMan"),
                                IsAdmin = reader.GetBoolean("IsAdmin")
                            });
                        }
                    }
                }
                ManagersDataGrid.ItemsSource = liste;
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        private void ManagersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            managerSelectionne = ManagersDataGrid.SelectedItem as Manager;
            if (managerSelectionne != null)
            {
                txtNom.Text = managerSelectionne.NomMan;
                txtPrenom.Text = managerSelectionne.PrenomMan;
                txtEmail.Text = managerSelectionne.MailMan;
                chkIsAdmin.IsChecked = managerSelectionne.IsAdmin;
                btnEnregistrer.Content = "💾 Modifier le compte";
            }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNom.Text) || string.IsNullOrWhiteSpace(txtEmail.Text)) return;

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query;

                    if (managerSelectionne == null) // NOUVEL AJOUT
                    {
                        string hash = BCrypt.Net.BCrypt.HashPassword(txtMdp.Password);
                        query = "INSERT INTO managers (NomMan, PrenomMan, MailMan, MdpMan, IsAdmin) VALUES (@nom, @prenom, @mail, @mdp, @admin)";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@nom", txtNom.Text.Trim());
                        cmd.Parameters.AddWithValue("@prenom", txtPrenom.Text.Trim());
                        cmd.Parameters.AddWithValue("@mail", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@mdp", hash);
                        cmd.Parameters.AddWithValue("@admin", chkIsAdmin.IsChecked == true ? 1 : 0);
                        cmd.ExecuteNonQuery();
                    }
                    else // MODIFICATION
                    {
                        // On vérifie si on doit changer le mot de passe
                        bool changerMdp = !string.IsNullOrWhiteSpace(txtMdp.Password);
                        query = changerMdp
                            ? "UPDATE managers SET NomMan=@nom, PrenomMan=@prenom, MailMan=@mail, MdpMan=@mdp, IsAdmin=@admin WHERE IdMan=@id"
                            : "UPDATE managers SET NomMan=@nom, PrenomMan=@prenom, MailMan=@mail, IsAdmin=@admin WHERE IdMan=@id";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@nom", txtNom.Text.Trim());
                        cmd.Parameters.AddWithValue("@prenom", txtPrenom.Text.Trim());
                        cmd.Parameters.AddWithValue("@mail", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@admin", chkIsAdmin.IsChecked == true ? 1 : 0);
                        cmd.Parameters.AddWithValue("@id", managerSelectionne.IdMan);

                        if (changerMdp) cmd.Parameters.AddWithValue("@mdp", BCrypt.Net.BCrypt.HashPassword(txtMdp.Password));

                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Compte manager mis à jour !");
                BtnVider_Click(null, null);
                ChargerManagers();
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (managerSelectionne == null) return;

            // Sécurité : ne pas se supprimer soi-même
            if (managerSelectionne.IdMan == SessionManager.ManagerConnecte.IdMan)
            {
                MessageBox.Show("Vous ne pouvez pas supprimer votre propre compte !");
                return;
            }

            if (MessageBox.Show($"Supprimer l'accès de {managerSelectionne.PrenomMan} ?", "Admin", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("DELETE FROM managers WHERE IdMan=@id", conn);
                    cmd.Parameters.AddWithValue("@id", managerSelectionne.IdMan);
                    cmd.ExecuteNonQuery();
                }
                BtnVider_Click(null, null);
                ChargerManagers();
            }
        }

        private void BtnVider_Click(object sender, RoutedEventArgs e)
        {
            managerSelectionne = null;
            txtNom.Clear(); txtPrenom.Clear(); txtEmail.Clear(); txtMdp.Clear(); chkIsAdmin.IsChecked = false;
            btnEnregistrer.Content = "💾 Enregistrer";
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    }
}