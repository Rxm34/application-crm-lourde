using LiveCharts;
using LiveCharts.Wpf;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;

namespace AppCrmLourde
{
    public partial class PageDashboard : Page
    {
        private string connectionString = "server=localhost;database=application_crm_lourde;uid=root;pwd=root;";
        private readonly string logFile = "logs.txt";

        private List<Facture> allFactures = new List<Facture>();
        private List<Contact> allContacts = new List<Contact>();

        public PageDashboard()
        {
            InitializeComponent();
            ChargerDashboard();
            LogAction("Ouverture Dashboard", "Page Dashboard chargée");
        }

        private void LogAction(string action, string details)
        {
            string managerInfo = SessionManager.ManagerConnecte != null
                ? $"{SessionManager.ManagerConnecte.PrenomMan} {SessionManager.ManagerConnecte.NomMan}"
                : "Manager inconnu";

            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {managerInfo} : {action} : {details}";
            try { File.AppendAllText(logFile, logEntry + Environment.NewLine); } catch { }
        }

        private void ChargerDashboard()
        {
            ChargerFactures();
            ChargerContacts();

            // 1. Calcul du CA du mois en cours
            double caMois = allFactures
                .Where(f => f.DateFact.Month == DateTime.Now.Month && f.DateFact.Year == DateTime.Now.Year)
                .Sum(f => f.PrixFact);
            lblCaMois.Text = caMois.ToString("N2") + " €";

            // 2. Nombre de RDV du mois
            int rdvMois = allContacts.Count(c => c.DateRdv.Month == DateTime.Now.Month && c.DateRdv.Year == DateTime.Now.Year);
            lblRdvMois.Text = rdvMois.ToString();

            // 3. Nombre total de produits vendus (Somme des quantités dans les lignes)
            int prodVendus = allFactures.Sum(f => f.Lignes.Sum(l => l.Qte));
            lblProduitsVendus.Text = prodVendus.ToString();

            // 4. Top Client (Celui qui a généré le plus de CA)
            var topClient = allFactures
                .GroupBy(f => f.NomClient)
                .Select(g => new { Nom = g.Key, Total = g.Sum(a => a.PrixFact) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();
            lblTopClient.Text = topClient?.Nom ?? "Aucun";

            // --- GRAPHIQUE CA PAR MOIS ---
            int currentYear = DateTime.Now.Year;
            var caParMois = Enumerable.Range(1, 12)
                .Select(month => allFactures
                    .Where(f => f.DateFact.Month == month && f.DateFact.Year == currentYear)
                    .Sum(f => f.PrixFact))
                .ToList();

            chartCaParMois.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "CA (€)",
                    Values = new ChartValues<double>(caParMois),
                    Fill = Brushes.CornflowerBlue,
                    DataLabels = true,
                    LabelPoint = point => point.Y.ToString("N0") + " €"
                }
            };
            SetAxisX(chartCaParMois);

            // --- GRAPHIQUE RDV PAR MOIS ---
            var rdvParMois = Enumerable.Range(1, 12)
                .Select(month => (double)allContacts.Count(c => c.DateRdv.Month == month && c.DateRdv.Year == currentYear))
                .ToList();

            chartRdv.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "RDV",
                    Values = new ChartValues<double>(rdvParMois),
                    Stroke = Brushes.Green,
                    PointGeometrySize = 10,
                    DataLabels = true
                }
            };
            SetAxisX(chartRdv);
        }

        private void SetAxisX(CartesianChart chart)
        {
            chart.AxisX.Clear();
            chart.AxisX.Add(new Axis
            {
                Labels = Enumerable.Range(1, 12)
                    .Select(m => System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m))
                    .ToList()
            });
        }

        private void ChargerFactures()
        {
            allFactures.Clear();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // On récupère les factures et on joint les lignes pour les quantités
                    string query = @"
                        SELECT f.*, c.NomCli, c.PrenomCli, lf.IdProd, lf.Qte
                        FROM factures f
                        JOIN clients c ON f.IdCli = c.IdCli
                        LEFT JOIN lignefact lf ON f.IdFact = lf.IdFact";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int idFact = reader.GetInt32("IdFact");
                            var factureExistante = allFactures.FirstOrDefault(x => x.IdFact == idFact);

                            if (factureExistante == null)
                            {
                                factureExistante = new Facture
                                {
                                    IdFact = idFact,
                                    NomClient = reader.GetString("NomCli") + " " + reader.GetString("PrenomCli"),
                                    PrixFact = reader.GetDouble("PrixFact"),
                                    DateFact = reader.GetDateTime("DateFact")
                                };
                                allFactures.Add(factureExistante);
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("IdProd")))
                            {
                                factureExistante.Lignes.Add(new LigneFact
                                {
                                    Qte = reader.GetInt32("Qte")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur factures : " + ex.Message); }
        }

        private void ChargerContacts()
        {
            allContacts.Clear();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT IdContact, DateRdv FROM contacts";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allContacts.Add(new Contact
                            {
                                IdContact = reader.GetInt32("IdContact"),
                                DateRdv = reader.GetDateTime("DateRdv")
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur contacts : " + ex.Message); }
        }

        // --- EXPORT PDF ---
        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", FileName = "Rapport_Dashboard.pdf" };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    PdfDocument pdfDoc = new PdfDocument();
                    AjouterImagePdf(pdfDoc, RenderToBitmap(chartCaParMois), "Évolution du Chiffre d'Affaires");
                    AjouterImagePdf(pdfDoc, RenderToBitmap(chartRdv), "Évolution des Rendez-vous");
                    pdfDoc.Save(saveFileDialog.FileName);
                    MessageBox.Show("Export terminé avec succès !");
                }
                catch (Exception ex) { MessageBox.Show("Erreur export : " + ex.Message); }
            }
        }

        private BitmapSource RenderToBitmap(UIElement element)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)element.RenderSize.Width, (int)element.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(element);
            return rtb;
        }

        private void AjouterImagePdf(PdfDocument pdfDoc, BitmapSource bitmap, string titre)
        {
            PdfPage page = pdfDoc.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Verdana", 20, XFontStyleEx.Bold);
            gfx.DrawString(titre, font, XBrushes.Black, new XRect(0, 20, page.Width, 40), XStringFormats.Center);

            using (var ms = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(ms);
                XImage img = XImage.FromStream(ms);
                gfx.DrawImage(img, 50, 70, 500, 300);
            }
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new PageAccueil());
    }
}