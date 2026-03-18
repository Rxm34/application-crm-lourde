using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class PageGestionClients : Page
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private Client clientSelectionne = null;

        public PageGestionClients()
        {
            InitializeComponent();
            ChargerClients();
        }

        private void ChargerClients()
        {
            List<Client> liste = new List<Client>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM clients ORDER BY NomCli ASC", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            liste.Add(new Client
                            {
                                IdCli = reader.GetInt32("IdCli"),
                                NomCli = reader.GetString("NomCli"),
                                PrenomCli = reader.GetString("PrenomCli"),
                                MailCli = reader.IsDBNull(reader.GetOrdinal("MailCli")) ? "" : reader.GetString("MailCli"),
                                TelCli = reader.IsDBNull(reader.GetOrdinal("TelCli")) ? "" : reader.GetString("TelCli"),
                                RueCli = reader.IsDBNull(reader.GetOrdinal("RueCli")) ? "" : reader.GetString("RueCli"),
                                CPCli = reader.IsDBNull(reader.GetOrdinal("CPCli")) ? "" : reader.GetString("CPCli"),
                                VilleCli = reader.IsDBNull(reader.GetOrdinal("VilleCli")) ? "" : reader.GetString("VilleCli")
                            });
                        }
                    }
                }
                ClientsDataGrid.ItemsSource = liste;
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement clients : " + ex.Message); }
        }

        private void ClientsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            clientSelectionne = ClientsDataGrid.SelectedItem as Client;
            if (clientSelectionne != null)
            {
                txtNom.Text = clientSelectionne.NomCli;
                txtPrenom.Text = clientSelectionne.PrenomCli;
                txtEmail.Text = clientSelectionne.MailCli;
                txtTel.Text = clientSelectionne.TelCli;
                txtRue.Text = clientSelectionne.RueCli;
                txtCP.Text = clientSelectionne.CPCli;
                txtVille.Text = clientSelectionne.VilleCli;
                btnEnregistrer.Content = "💾 Modifier le client";
            }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNom.Text) || string.IsNullOrWhiteSpace(txtPrenom.Text))
            {
                MessageBox.Show("Le nom et le prénom sont obligatoires.");
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = (clientSelectionne == null)
                        ? "INSERT INTO clients (NomCli, PrenomCli, MailCli, TelCli, RueCli, CPCli, VilleCli) VALUES (@nom, @prenom, @mail, @tel, @rue, @cp, @ville)"
                        : "UPDATE clients SET NomCli=@nom, PrenomCli=@prenom, MailCli=@mail, TelCli=@tel, RueCli=@rue, CPCli=@cp, VilleCli=@ville WHERE IdCli=@id";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@nom", txtNom.Text.Trim());
                    cmd.Parameters.AddWithValue("@prenom", txtPrenom.Text.Trim());
                    cmd.Parameters.AddWithValue("@mail", txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@tel", txtTel.Text.Trim());
                    cmd.Parameters.AddWithValue("@rue", txtRue.Text.Trim());
                    cmd.Parameters.AddWithValue("@cp", txtCP.Text.Trim());
                    cmd.Parameters.AddWithValue("@ville", txtVille.Text.Trim());
                    if (clientSelectionne != null) cmd.Parameters.AddWithValue("@id", clientSelectionne.IdCli);

                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Client enregistré avec succès !");
                ChargerClients();
            }
            catch (Exception ex) { MessageBox.Show("Erreur enregistrement : " + ex.Message); }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (clientSelectionne == null) return;

            if (MessageBox.Show($"Supprimer définitivement {clientSelectionne.FullName} ?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        var cmd = new MySqlCommand("DELETE FROM clients WHERE IdCli=@id", conn);
                        cmd.Parameters.AddWithValue("@id", clientSelectionne.IdCli);
                        cmd.ExecuteNonQuery();
                    }
                    ChargerClients();
                }
                catch (Exception) { MessageBox.Show("Impossible de supprimer ce client car il possède des factures ou RDV liés."); }
            }
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    }
}