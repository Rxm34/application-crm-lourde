using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class FenetreAjouterRendezVous : Window
    {
        private readonly string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private bool estModification = false;

        public int IdClient { get; private set; } = 0;
        public int IdProspect { get; private set; } = 0;
        public string NomContact { get; private set; } = "";
        public DateTime DateRdv => dpDate.SelectedDate ?? DateTime.Now;
        public string HeureRdv => txtHeure.Text.Trim();
        public int DureeRdv => int.TryParse(txtDuree.Text, out int d) ? d : 0;

        private List<Client> clients;
        private List<Prospect> prospects;

        public FenetreAjouterRendezVous(dynamic rdv = null)
        {
            InitializeComponent();
            ChargerClientsEtProspects();

            if (rdv != null)
            {
                estModification = true;
                PreRemplir(rdv);
            }
        }

        private void ChargerClientsEtProspects()
        {
            clients = new List<Client>();
            prospects = new List<Prospect>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Clients
                string queryClients = "SELECT IdCli, NomCli, PrenomCli FROM clients";
                using (var cmd = new MySqlCommand(queryClients, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        clients.Add(new Client
                        {
                            IdCli = Convert.ToInt32(reader["IdCli"]),
                            NomCli = reader["NomCli"].ToString(),
                            PrenomCli = reader["PrenomCli"].ToString()
                        });
                }

                // Prospects
                string queryProspects = "SELECT IdProsp, NomProsp, PrenomProsp FROM prospects";
                using (var cmd = new MySqlCommand(queryProspects, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        prospects.Add(new Prospect
                        {
                            IdProsp = Convert.ToInt32(reader["IdProsp"]),
                            NomProsp = reader["NomProsp"].ToString(),
                            PrenomProsp = reader["PrenomProsp"].ToString()
                        });
                }
            }

            // Lier directement les objets au ComboBox
            cbContacts.Items.Clear();
            foreach (var c in clients)
                cbContacts.Items.Add(c);
            foreach (var p in prospects)
                cbContacts.Items.Add(p);

            // Affichage Nom + Prénom
            cbContacts.DisplayMemberPath = "FullName";
        }

        private void PreRemplir(dynamic rdv)
        {
            int idASelectionner = rdv.Type == "Client" ? rdv.IdClient : rdv.IdProspect;

            foreach (var item in cbContacts.Items)
            {
                if ((item is Client client && client.IdCli == idASelectionner) ||
                    (item is Prospect prospect && prospect.IdProsp == idASelectionner))
                {
                    cbContacts.SelectedItem = item;
                    break;
                }
            }

            dpDate.SelectedDate = rdv.DateRdv;
            txtHeure.Text = rdv.HeureRdv;
            txtDuree.Text = rdv.DureeRdv.ToString();
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            if (cbContacts.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un contact.");
                return;
            }

            var selected = cbContacts.SelectedItem;
            if (selected is Client client)
            {
                IdClient = client.IdCli;
                IdProspect = 0;
                NomContact = client.FullName;
            }
            else if (selected is Prospect prospect)
            {
                IdClient = 0;
                IdProspect = prospect.IdProsp;
                NomContact = prospect.FullName;
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}
