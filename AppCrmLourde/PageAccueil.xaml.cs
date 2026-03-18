using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace AppCrmLourde
{
    public partial class PageAccueil : Page
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        public string TodayText => DateTime.Now.ToString("dddd, dd MMMM yyyy");

        private const int StartHour = 8;
        private const int EndHour = 18;
        private const double HourHeight = 60; // 1 heure = 60px

        public PageAccueil()
        {
            InitializeComponent();
            DataContext = this;

            // --- VÉRIFICATION ADMIN ---
            // On vérifie si le bouton existe et si l'utilisateur n'est PAS admin
            // 'btnAdmin' est le Name que nous allons donner au bouton dans le XAML
            if (SessionManager.ManagerConnecte != null && !SessionManager.ManagerConnecte.IsAdmin)
            {
                // Si l'utilisateur n'est pas admin, on cache le bouton et on libère l'espace (Collapsed)
                BtnPageAdmin.Visibility = Visibility.Collapsed;
            }

            // Chargement de l'agenda depuis MySQL
            RefreshAgenda();
        }

        public void RefreshAgenda()
        {
            BuildAgenda();
            LoadAppointmentsFromDatabase();
        }

        private void BuildAgenda()
        {
            AgendaGrid.Children.Clear();
            AgendaGrid.RowDefinitions.Clear();
            AgendaGrid.ColumnDefinitions.Clear();

            AgendaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            AgendaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int totalHalfHours = (EndHour - StartHour) * 2;
            for (int i = 0; i <= totalHalfHours; i++)
            {
                AgendaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(HourHeight / 2) });
            }

            for (int h = StartHour; h <= EndHour; h++)
            {
                int rowIndex = (h - StartHour) * 2;

                TextBlock hourText = new TextBlock
                {
                    Text = $"{h:00}:00",
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                Grid.SetRow(hourText, rowIndex);
                Grid.SetColumn(hourText, 0);
                Grid.SetRowSpan(hourText, 2);
                AgendaGrid.Children.Add(hourText);

                if (h < EndHour)
                {
                    Border line = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    Grid.SetRow(line, rowIndex);
                    Grid.SetColumn(line, 1);
                    Grid.SetRowSpan(line, 2);
                    AgendaGrid.Children.Add(line);
                }
            }
        }

        private void LoadAppointmentsFromDatabase()
        {
            List<Contact> todayAppointments = new List<Contact>();
            DateTime today = DateTime.Today;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT co.*, c.IdCli, c.NomCli, c.PrenomCli, p.IdProsp, p.NomProsp, p.PrenomProsp
                        FROM contacts co
                        LEFT JOIN clients c ON co.IdCli = c.IdCli
                        LEFT JOIN prospects p ON co.IdProsp = p.IdProsp
                        WHERE DATE(co.DateRdv) = @today
                        ORDER BY co.HeureRdv ASC";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@today", today);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Contact c = new Contact
                            {
                                IdContact = reader.GetInt32("IdContact"),
                                DateRdv = reader.GetDateTime("DateRdv"),
                                HeureRdv = reader.GetTimeSpan("HeureRdv"),
                                DureeRdv = reader.GetInt32("DureeRdv"),
                                IdCli = reader["IdCli"] != DBNull.Value ? new Client
                                {
                                    IdCli = reader.GetInt32("IdCli"),
                                    NomCli = reader.GetString("NomCli"),
                                    PrenomCli = reader.GetString("PrenomCli")
                                } : null,
                                IdProsp = reader["IdProsp"] != DBNull.Value ? new Prospect
                                {
                                    IdProsp = reader.GetInt32("IdProsp"),
                                    NomProsp = reader.GetString("NomProsp"),
                                    PrenomProsp = reader.GetString("PrenomProsp")
                                } : null
                            };
                            todayAppointments.Add(c);
                        }
                    }
                }

                foreach (var contact in todayAppointments)
                {
                    string titre;
                    Brush couleur;

                    if (contact.IdCli != null)
                    {
                        titre = $"Client: {contact.IdCli.NomCli} {contact.IdCli.PrenomCli}";
                        couleur = Brushes.LightGreen;
                    }
                    else if (contact.IdProsp != null)
                    {
                        titre = $"Prospect: {contact.IdProsp.NomProsp} {contact.IdProsp.PrenomProsp}";
                        couleur = Brushes.LightSalmon;
                    }
                    else
                    {
                        titre = "Rendez-vous inconnu";
                        couleur = Brushes.LightGray;
                    }

                    TimeSpan debut = contact.HeureRdv;
                    TimeSpan fin = debut + TimeSpan.FromHours(contact.DureeRdv);

                    AddAppointment(titre, debut, fin, couleur);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement rendez-vous : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddAppointment(string title, TimeSpan debut, TimeSpan fin, Brush backgroundBrush)
        {
            double startIndex = (debut.TotalMinutes - StartHour * 60) / 30.0;
            double spanRows = (fin - debut).TotalMinutes / 30.0;

            Border appointment = new Border
            {
                Background = backgroundBrush,
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(2)
            };

            TextBlock txt = new TextBlock
            {
                Text = $"{debut:hh\\:mm} - {fin:hh\\:mm}\n{title}",
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            appointment.Child = txt;

            int startRow = (int)startIndex;
            int rowSpan = Math.Max(1, (int)Math.Ceiling(spanRows));

            Grid.SetRow(appointment, startRow);
            Grid.SetColumn(appointment, 1);
            Grid.SetRowSpan(appointment, rowSpan);

            AgendaGrid.Children.Add(appointment);
        }

        // Navigation
        private void BtnPage1_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new PageProspectsClients());
        private void BtnPage2_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new PageRendezVous());
        private void BtnPage3_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new PageGestionProduit());
        private void BtnPage4_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new PageGestionFacture());
        private void BtnPage5_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new PageDashboard());
        private void BtnPage6_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new PageAdmin());
    }
}