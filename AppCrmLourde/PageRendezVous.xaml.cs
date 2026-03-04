using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class PageRendezVous : Page
    {
        private readonly string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private List<dynamic> allEntries;
        private readonly string logFile = "logs.txt";

        public PageRendezVous()
        {
            InitializeComponent();
            InitialiserFiltre();
            ChargerDonnees();
        }

        // ===================== INITIALISATION FILTRE =====================
        private void InitialiserFiltre()
        {
            cbFiltre.Items.Clear();
            cbFiltre.Items.Add(new ComboBoxItem { Content = "Tous" });
            cbFiltre.Items.Add(new ComboBoxItem { Content = "Client" });
            cbFiltre.Items.Add(new ComboBoxItem { Content = "Prospect" });
            cbFiltre.SelectedIndex = 0;
        }

        // ===================== CHARGEMENT =====================
        private void ChargerDonnees()
        {
            try
            {
                var rendezVous = new List<dynamic>();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT c.IdContact, c.DateRdv, c.HeureRdv, c.DureeRdv,
                       cli.IdCli, cli.NomCli, cli.PrenomCli,
                       pro.IdProsp, pro.NomProsp, pro.PrenomProsp
                FROM contacts c
                LEFT JOIN clients cli ON c.IdCli = cli.IdCli
                LEFT JOIN prospects pro ON c.IdProsp = pro.IdProsp";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        string nom = reader["NomCli"] != DBNull.Value
                            ? $"{reader["NomCli"]} {reader["PrenomCli"]}"
                            : $"{reader["NomProsp"]} {reader["PrenomProsp"]}";

                        string type = reader["NomCli"] != DBNull.Value ? "Client" : "Prospect";

                        rendezVous.Add(new
                        {
                            Id = Convert.ToInt32(reader["IdContact"]),
                            Nom = nom,
                            Type = type,
                            DateRdv = Convert.ToDateTime(reader["DateRdv"]),
                            HeureRdv = reader["HeureRdv"].ToString(),
                            DureeRdv = Convert.ToInt32(reader["DureeRdv"]),
                            IdClient = reader["IdCli"] != DBNull.Value ? Convert.ToInt32(reader["IdCli"]) : 0,
                            IdProspect = reader["IdProsp"] != DBNull.Value ? Convert.ToInt32(reader["IdProsp"]) : 0
                        });
                    }
                }

                allEntries = rendezVous;
                dgRendezVous.ItemsSource = allEntries;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }


        // ===================== LOG =====================
        private void LogAction(string action, string details)
        {
            string managerInfo = SessionManager.ManagerConnecte != null
                ? $"{SessionManager.ManagerConnecte.PrenomMan} {SessionManager.ManagerConnecte.NomMan} ({SessionManager.ManagerConnecte.MailMan})"
                : "Manager inconnu";

            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {managerInfo} : {action} : {details}";
            File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }

        // ===================== RECHERCHE =====================
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string searchText = txtSearch.Text?.ToLower().Trim() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                dgRendezVous.ItemsSource = allEntries;
                return;
            }

            var resultats = allEntries.Where(item =>
                item.Nom.ToLower().Contains(searchText) ||
                item.Type.ToLower().Contains(searchText) ||
                item.DateRdv.ToString("yyyy-MM-dd").Contains(searchText) ||
                item.HeureRdv.Contains(searchText) ||
                item.DureeRdv.ToString().Contains(searchText)
            ).ToList();

            dgRendezVous.ItemsSource = resultats;
        }

        // ===================== FILTRE =====================
        private void BtnFiltrer_Click(object sender, RoutedEventArgs e)
        {
            string selectedType = (cbFiltre.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(selectedType) || selectedType == "Tous")
                dgRendezVous.ItemsSource = allEntries;
            else
                dgRendezVous.ItemsSource = allEntries
                    .Where(x => x.Type.Equals(selectedType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }

        // ===================== AJOUT =====================
        // ===================== AJOUT =====================
        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            // Pour l'ajout, on passe null → la fenêtre reste vide
            FenetreAjouterRendezVous fenetre = new FenetreAjouterRendezVous();
            if (fenetre.ShowDialog() == true)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                INSERT INTO contacts (IdCli, IdProsp, DateRdv, HeureRdv, DureeRdv)
                VALUES (@IdCli, @IdProsp, @Date, @Heure, @Duree)";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@IdCli", fenetre.IdClient != 0 ? fenetre.IdClient : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdProsp", fenetre.IdProspect != 0 ? fenetre.IdProspect : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Date", fenetre.DateRdv);
                    cmd.Parameters.AddWithValue("@Heure", fenetre.HeureRdv);
                    cmd.Parameters.AddWithValue("@Duree", fenetre.DureeRdv);
                    cmd.ExecuteNonQuery();
                }

                LogAction("Ajout RDV",
                    $"Contact: {fenetre.NomContact}, Date: {fenetre.DateRdv:yyyy-MM-dd}, Heure: {fenetre.HeureRdv}, Durée: {fenetre.DureeRdv}");

                ChargerDonnees();
            }
        }

        // ===================== MODIFICATION =====================
        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = dgRendezVous.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("Veuillez sélectionner un rendez-vous.");
                return;
            }

            // Ouvre la fenêtre en passant l'objet sélectionné
            FenetreAjouterRendezVous fenetre = new FenetreAjouterRendezVous(selected);

            if (fenetre.ShowDialog() == true)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                UPDATE contacts
                SET IdCli=@IdCli, IdProsp=@IdProsp, DateRdv=@Date, HeureRdv=@Heure, DureeRdv=@Duree
                WHERE IdContact=@Id";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@IdCli", fenetre.IdClient != 0 ? fenetre.IdClient : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdProsp", fenetre.IdProspect != 0 ? fenetre.IdProspect : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Date", fenetre.DateRdv);
                    cmd.Parameters.AddWithValue("@Heure", fenetre.HeureRdv);
                    cmd.Parameters.AddWithValue("@Duree", fenetre.DureeRdv);
                    cmd.Parameters.AddWithValue("@Id", selected.Id);
                    cmd.ExecuteNonQuery();
                }

                LogAction("Modification RDV",
                    $"ID: {selected.Id}\nAVANT → Date: {selected.DateRdv:yyyy-MM-dd}, Heure: {selected.HeureRdv}, Durée: {selected.DureeRdv}\n" +
                    $"APRÈS → Date: {fenetre.DateRdv:yyyy-MM-dd}, Heure: {fenetre.HeureRdv}, Durée: {fenetre.DureeRdv}");

                ChargerDonnees();
            }
        }


        // ===================== SUPPRESSION =====================
        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = dgRendezVous.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("Veuillez sélectionner un rendez-vous.");
                return;
            }

            if (MessageBox.Show("Supprimer ce rendez-vous ?", "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM contacts WHERE IdContact=@Id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", selected.Id);
                    cmd.ExecuteNonQuery();
                }

                LogAction("Suppression RDV", $"ID:{selected.Id} - {selected.Nom}");
                ChargerDonnees();
            }
        }

        // ===================== RETOUR =====================
        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null)
                NavigationService.Navigate(new PageAccueil());
            else
                MessageBox.Show("Navigation impossible.");
        }
    }
}
