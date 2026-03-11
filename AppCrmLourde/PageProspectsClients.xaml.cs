using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class PageProspectsClients : Page
    {
        private readonly string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private List<dynamic> allEntries; // fusion clients + prospects
        private readonly string logFile = "logs.txt";

        public PageProspectsClients()
        {
            InitializeComponent();
            ChargerDonnees();
        }

        #region === Chargement des données ===
        private void ChargerDonnees()
        {
            try
            {
                var clients = new List<Client>();
                var prospects = new List<Prospect>();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Clients
                    string queryClients = "SELECT IdCli, NomCli, PrenomCli, MailCli, TelCli, VilleCli, CPCli, RueCli FROM clients";
                    using (var cmd = new MySqlCommand(queryClients, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            clients.Add(new Client
                            {
                                IdCli = Convert.ToInt32(reader["IdCli"]),
                                NomCli = reader["NomCli"].ToString(),
                                PrenomCli = reader["PrenomCli"].ToString(),
                                MailCli = reader["MailCli"].ToString(),
                                TelCli = reader["TelCli"].ToString(),
                                VilleCli = reader["VilleCli"].ToString(),
                                CPCli = reader["CPCli"].ToString(),
                                RueCli = reader["RueCli"].ToString()
                            });
                        }
                    }

                    // Prospects
                    string queryProspects = "SELECT IdProsp, NomProsp, PrenomProsp, MailProsp, TelProsp, VilleProsp, CPProsp, RueProsp FROM prospects";
                    using (var cmd = new MySqlCommand(queryProspects, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            prospects.Add(new Prospect
                            {
                                IdProsp = Convert.ToInt32(reader["IdProsp"]),
                                NomProsp = reader["NomProsp"].ToString(),
                                PrenomProsp = reader["PrenomProsp"].ToString(),
                                MailProsp = reader["MailProsp"].ToString(),
                                TelProsp = reader["TelProsp"].ToString(),
                                VilleProsp = reader["VilleProsp"].ToString(),
                                CPProsp = reader["CPProsp"].ToString(),
                                RueProsp = reader["RueProsp"].ToString()
                            });
                        }
                    }
                }

                // Fusionner pour le DataGrid
                allEntries = clients
                    .Select(c => new
                    {
                        Id = c.IdCli,
                        Type = "Client",
                        Nom = c.NomCli,
                        Prenom = c.PrenomCli,
                        Mail = c.MailCli,
                        Telephone = c.TelCli,
                        Ville = c.VilleCli,
                        CodePostal = c.CPCli,
                        Rue = c.RueCli
                    })
                    .Concat(prospects.Select(p => new
                    {
                        Id = p.IdProsp,
                        Type = "Prospect",
                        Nom = p.NomProsp,
                        Prenom = p.PrenomProsp,
                        Mail = p.MailProsp,
                        Telephone = p.TelProsp,
                        Ville = p.VilleProsp,
                        CodePostal = p.CPProsp,
                        Rue = p.RueProsp
                    }))
                    .ToList<dynamic>();

                dgClientsProspects.ItemsSource = allEntries;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des données : " + ex.Message);
            }
        }
        #endregion

        #region === Logging ===
        private void LogAction(string action, string details)
        {
            string managerInfo = SessionManager.ManagerConnecte != null
                ? $"{SessionManager.ManagerConnecte.PrenomMan} {SessionManager.ManagerConnecte.NomMan} ({SessionManager.ManagerConnecte.MailMan})"
                : "Manager inconnu";

            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {managerInfo} : {action} : {details}";
            File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }
        #endregion

        #region === Recherche et filtrage ===
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string searchText = txtSearch.Text?.ToLower().Trim() ?? "";
            if (string.IsNullOrWhiteSpace(searchText))
            {
                dgClientsProspects.ItemsSource = allEntries;
                return;
            }

            dgClientsProspects.ItemsSource = allEntries.Where(item =>
                item.Nom.ToLower().Contains(searchText) ||
                item.Prenom.ToLower().Contains(searchText) ||
                item.Mail.ToLower().Contains(searchText) ||
                item.Telephone.ToLower().Contains(searchText) ||
                item.Ville.ToLower().Contains(searchText) ||
                item.CodePostal.ToLower().Contains(searchText) ||
                item.Rue.ToLower().Contains(searchText)
            ).ToList();
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            string selectedType = (cbType.SelectedItem as ComboBoxItem)?.Content.ToString();
            dgClientsProspects.ItemsSource = (selectedType == "Tous" || string.IsNullOrEmpty(selectedType))
                ? allEntries
                : allEntries.Where(x => x.Type == selectedType).ToList();
        }
        #endregion

        #region === Ajouter ===
        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            FenetreAjouterModifier fenetre = new FenetreAjouterModifier();
            if (fenetre.ShowDialog() == true)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = fenetre.TypeChoisi == "Client"
                        ? "INSERT INTO clients (NomCli, PrenomCli, MailCli, TelCli, VilleCli, CPCli, RueCli) VALUES (@Nom, @Prenom, @Mail, @Tel, @Ville, @CP, @Rue)"
                        : "INSERT INTO prospects (NomProsp, PrenomProsp, MailProsp, TelProsp, VilleProsp, CPProsp, RueProsp) VALUES (@Nom, @Prenom, @Mail, @Tel, @Ville, @CP, @Rue)";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Nom", fenetre.Nom);
                    cmd.Parameters.AddWithValue("@Prenom", fenetre.Prenom);
                    cmd.Parameters.AddWithValue("@Mail", fenetre.Mail);
                    cmd.Parameters.AddWithValue("@Tel", fenetre.Telephone);
                    cmd.Parameters.AddWithValue("@Ville", fenetre.Ville);
                    cmd.Parameters.AddWithValue("@CP", fenetre.CodePostal);
                    cmd.Parameters.AddWithValue("@Rue", fenetre.Rue);
                    cmd.ExecuteNonQuery();
                }

                LogAction("Ajout", $"{fenetre.TypeChoisi} : Nom:{fenetre.Nom}, Prénom:{fenetre.Prenom}");
                ChargerDonnees();
            }
        }
        #endregion

        #region === Modifier ===
        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = dgClientsProspects.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("Veuillez sélectionner un enregistrement à modifier.");
                return;
            }

            // Récupérer l'objet réel depuis la base
            object objAmodifier;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                if (selected.Type == "Client")
                {
                    string query = "SELECT * FROM clients WHERE IdCli=@Id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", selected.Id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            objAmodifier = new Client
                            {
                                IdCli = Convert.ToInt32(reader["IdCli"]),
                                NomCli = reader["NomCli"].ToString(),
                                PrenomCli = reader["PrenomCli"].ToString(),
                                MailCli = reader["MailCli"].ToString(),
                                TelCli = reader["TelCli"].ToString(),
                                VilleCli = reader["VilleCli"].ToString(),
                                CPCli = reader["CPCli"].ToString(),
                                RueCli = reader["RueCli"].ToString()
                            };
                        }
                        else return;
                    }
                }
                else
                {
                    string query = "SELECT * FROM prospects WHERE IdProsp=@Id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", selected.Id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            objAmodifier = new Prospect
                            {
                                IdProsp = Convert.ToInt32(reader["IdProsp"]),
                                NomProsp = reader["NomProsp"].ToString(),
                                PrenomProsp = reader["PrenomProsp"].ToString(),
                                MailProsp = reader["MailProsp"].ToString(),
                                TelProsp = reader["TelProsp"].ToString(),
                                VilleProsp = reader["VilleProsp"].ToString(),
                                CPProsp = reader["CPProsp"].ToString(),
                                RueProsp = reader["RueProsp"].ToString()
                            };
                        }
                        else return;
                    }
                }
            }

            // Ouvrir la fenêtre avec l'objet réel
            FenetreAjouterModifier fenetre = new FenetreAjouterModifier(objAmodifier);
            if (fenetre.ShowDialog() == true)
            {
                object modifie = fenetre.GetPersonne();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    if (selected.Type == "Prospect" && modifie is Client)
                    {
                        // CONVERSION DU PROSPECT EN CLIENT
                        using (MySqlTransaction tr = conn.BeginTransaction())
                        {
                            try
                            {
                                // 1. Insérer dans clients
                                string insertClientQuery = "INSERT INTO clients (NomCli, PrenomCli, MailCli, TelCli, VilleCli, CPCli, RueCli) VALUES (@Nom, @Prenom, @Mail, @Tel, @Ville, @CP, @Rue)";
                                MySqlCommand insertCmd = new MySqlCommand(insertClientQuery, conn, tr);
                                insertCmd.Parameters.AddWithValue("@Nom", GetNom(modifie));
                                insertCmd.Parameters.AddWithValue("@Prenom", GetPrenom(modifie));
                                insertCmd.Parameters.AddWithValue("@Mail", GetMail(modifie));
                                insertCmd.Parameters.AddWithValue("@Tel", GetTel(modifie));
                                insertCmd.Parameters.AddWithValue("@Ville", GetVille(modifie));
                                insertCmd.Parameters.AddWithValue("@CP", GetCP(modifie));
                                insertCmd.Parameters.AddWithValue("@Rue", GetRue(modifie));
                                insertCmd.ExecuteNonQuery();

                                long newClientId = insertCmd.LastInsertedId;

                                // 2. Mettre à jour les contacts liés
                                string updateContactsQuery = "UPDATE contacts SET IdCli=@NewIdCli, IdProsp=NULL WHERE IdProsp=@OldIdProsp";
                                MySqlCommand updateContactsCmd = new MySqlCommand(updateContactsQuery, conn, tr);
                                updateContactsCmd.Parameters.AddWithValue("@NewIdCli", newClientId);
                                updateContactsCmd.Parameters.AddWithValue("@OldIdProsp", selected.Id);
                                updateContactsCmd.ExecuteNonQuery();

                                // 3. Supprimer le prospect
                                string deleteProspectQuery = "DELETE FROM prospects WHERE IdProsp=@OldIdProsp";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteProspectQuery, conn, tr);
                                deleteCmd.Parameters.AddWithValue("@OldIdProsp", selected.Id);
                                deleteCmd.ExecuteNonQuery();

                                tr.Commit();
                                LogAction("Conversion", $"Prospect ID:{selected.Id} converti en Client ID:{newClientId}");
                            }
                            catch (Exception ex)
                            {
                                tr.Rollback();
                                MessageBox.Show("Erreur lors de la conversion : " + ex.Message);
                                return;
                            }
                        }
                    }
                    else
                    {
                        // MODIFICATION CLASSIQUE
                        string query = selected.Type == "Client"
                            ? "UPDATE clients SET NomCli=@Nom, PrenomCli=@Prenom, MailCli=@Mail, TelCli=@Tel, VilleCli=@Ville, CPCli=@CP, RueCli=@Rue WHERE IdCli=@Id"
                            : "UPDATE prospects SET NomProsp=@Nom, PrenomProsp=@Prenom, MailProsp=@Mail, TelProsp=@Tel, VilleProsp=@Ville, CPProsp=@CP, RueProsp=@Rue WHERE IdProsp=@Id";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Nom", GetNom(modifie));
                        cmd.Parameters.AddWithValue("@Prenom", GetPrenom(modifie));
                        cmd.Parameters.AddWithValue("@Mail", GetMail(modifie));
                        cmd.Parameters.AddWithValue("@Tel", GetTel(modifie));
                        cmd.Parameters.AddWithValue("@Ville", GetVille(modifie));
                        cmd.Parameters.AddWithValue("@CP", GetCP(modifie));
                        cmd.Parameters.AddWithValue("@Rue", GetRue(modifie));
                        cmd.Parameters.AddWithValue("@Id", selected.Id);
                        cmd.ExecuteNonQuery();

                        LogAction("Modification", $"{selected.Type} ID:{selected.Id} modifié");
                    }
                }

                ChargerDonnees();
            }
        }


        private string GetNom(object obj) => obj is Client c ? c.NomCli : ((Prospect)obj).NomProsp;
        private string GetPrenom(object obj) => obj is Client c ? c.PrenomCli : ((Prospect)obj).PrenomProsp;
        private string GetMail(object obj) => obj is Client c ? c.MailCli : ((Prospect)obj).MailProsp;
        private string GetTel(object obj) => obj is Client c ? c.TelCli : ((Prospect)obj).TelProsp;
        private string GetVille(object obj) => obj is Client c ? c.VilleCli : ((Prospect)obj).VilleProsp;
        private string GetCP(object obj) => obj is Client c ? c.CPCli : ((Prospect)obj).CPProsp;
        private string GetRue(object obj) => obj is Client c ? c.RueCli : ((Prospect)obj).RueProsp;
        #endregion

        #region === Supprimer ===
        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = dgClientsProspects.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("Veuillez sélectionner un enregistrement à supprimer.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Voulez-vous vraiment supprimer {selected.Type} : {selected.Nom} {selected.Prenom} ?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = selected.Type == "Client"
                        ? "DELETE FROM clients WHERE IdCli=@Id"
                        : "DELETE FROM prospects WHERE IdProsp=@Id";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", selected.Id);
                    cmd.ExecuteNonQuery();
                }

                LogAction("Suppression", $"{selected.Type} : {selected.Nom} {selected.Prenom}");
                ChargerDonnees();
            }
        }
        #endregion

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null)
                this.NavigationService.Navigate(new PageAccueil());
            else
                MessageBox.Show("Navigation impossible.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
