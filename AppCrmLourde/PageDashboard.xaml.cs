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
                ? $"{SessionManager.ManagerConnecte.PrenomMan} {SessionManager.ManagerConnecte.NomMan} ({SessionManager.ManagerConnecte.MailMan})"
                : "Manager inconnu";

            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {managerInfo} : {action} : {details}";
            try
            {
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'écriture du log : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ChargerDashboard()
        {
            ChargerFactures();
            ChargerContacts();

            double caMois = allFactures
                .Where(f => f.DateFact.Month == DateTime.Now.Month && f.DateFact.Year == DateTime.Now.Year)
                .Sum(f => f.PrixFact);
            lblCaMois.Text = caMois.ToString("0.00") + " €";

            int rdvMois = allContacts.Count(c => c.DateRdv.Month == DateTime.Now.Month && c.DateRdv.Year == DateTime.Now.Year);
            lblRdvMois.Text = rdvMois.ToString();

            int prodVendus = allFactures.Sum(f => f.QteProd);
            lblProduitsVendus.Text = prodVendus.ToString();

            var topClient = allFactures
                .GroupBy(f => f.NomClient)
                .Select(g => new
                {
                    Nom = g.First().NomClient.Split(' ')[0],
                    Total = g.Sum(a => a.PrixFact)
                })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            lblTopClient.Text = topClient?.Nom ?? "Aucun";

            LogAction("Chargement Dashboard", $"CA mois: {caMois:0.00} €, RDV mois: {rdvMois}, Produits vendus: {prodVendus}, Top client: {topClient?.Nom ?? "Aucun"}");

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
                    LabelPoint = point => point.Y.ToString("0.00 €")
                }
            };
            chartCaParMois.AxisX.Clear();
            chartCaParMois.AxisX.Add(new Axis
            {
                Title = "Mois",
                Labels = Enumerable.Range(1, 12)
                    .Select(m => System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m))
                    .ToList()
            });
            chartCaParMois.AxisY.Clear();
            chartCaParMois.AxisY.Add(new Axis { Title = "Montant (€)" });
            chartCaParMois.DataTooltip = null;

            var rdvParMois = Enumerable.Range(1, 12)
                .Select(month => allContacts.Count(c => c.DateRdv.Month == month && c.DateRdv.Year == currentYear))
                .ToList();

            chartRdv.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "RDV",
                    Values = new ChartValues<int>(rdvParMois),
                    Stroke = Brushes.Green,
                    Fill = Brushes.Transparent,
                    PointGeometrySize = 12,
                    DataLabels = true,
                    LabelPoint = point => point.Y.ToString()
                }
            };
            chartRdv.AxisX.Clear();
            chartRdv.AxisX.Add(new Axis
            {
                Title = "Mois",
                Labels = Enumerable.Range(1, 12)
                    .Select(m => System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m))
                    .ToList()
            });
            chartRdv.AxisY.Clear();
            chartRdv.AxisY.Add(new Axis { Title = "Nombre RDV" });
            chartRdv.DataTooltip = null;
        }

        private void ChargerFactures()
        {
            allFactures.Clear();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT f.*, c.NomCli, c.PrenomCli, p.NomProd, p.PrixProd
                        FROM factures f
                        JOIN clients c ON f.IdCli = c.IdCli
                        JOIN produits p ON f.IdProd = p.IdProd";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Facture f = new Facture
                        {
                            IdFact = reader.GetInt32("IdFact"),
                            IdCli = reader.GetInt32("IdCli"),
                            NomClient = reader.GetString("NomCli") + " " + reader.GetString("PrenomCli"),
                            IdProd = reader.GetInt32("IdProd"),
                            NomProduit = reader.GetString("NomProd"),
                            QteProd = reader.GetInt32("QteProd"),
                            PrixProd = reader.GetDouble("PrixProd"),
                            PrixFact = reader.GetDouble("PrixFact"),
                            DateFact = reader.GetDateTime("DateFact")
                        };
                        allFactures.Add(f);
                    }
                }
                LogAction("Chargement Factures", $"Nombre factures: {allFactures.Count}");
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement factures : " + ex.Message); }
        }

        private void ChargerContacts()
        {
            allContacts.Clear();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT co.*, c.IdCli, c.NomCli, c.PrenomCli, p.IdProsp, p.NomProsp
                        FROM contacts co
                        LEFT JOIN clients c ON co.IdCli = c.IdCli
                        LEFT JOIN prospects p ON co.IdProsp = p.IdProsp";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();
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
                                NomProsp = reader.GetString("NomProsp")
                            } : null
                        };
                        allContacts.Add(c);
                    }
                }
                LogAction("Chargement Contacts", $"Nombre contacts: {allContacts.Count}");
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement contacts : " + ex.Message); }
        }

        private BitmapSource RenderToBitmap(UIElement element)
        {
            element.Measure(new Size(element.RenderSize.Width, element.RenderSize.Height));
            element.Arrange(new Rect(new Size(element.RenderSize.Width, element.RenderSize.Height)));
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)element.RenderSize.Width,
                (int)element.RenderSize.Height,
                96, 96,
                PixelFormats.Pbgra32);
            rtb.Render(element);
            return rtb;
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = "Dashboard.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = saveFileDialog.FileName;
                    PdfDocument pdfDoc = new PdfDocument();

                    var bitmapCA = RenderToBitmap(chartCaParMois);
                    AjouterImagePdf(pdfDoc, bitmapCA);

                    var bitmapRDV = RenderToBitmap(chartRdv);
                    AjouterImagePdf(pdfDoc, bitmapRDV);

                    pdfDoc.Save(filePath);
                    pdfDoc.Close();

                    MessageBox.Show("PDF téléchargé !");
                    LogAction("Export PDF", $"Dashboard exporté vers : {filePath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors de l'export PDF : " + ex.Message);
                    LogAction("Erreur Export PDF", ex.Message);
                }
            }
        }

        private void AjouterImagePdf(PdfDocument pdfDoc, BitmapSource bitmap)
        {
            using (var ms = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(ms);
                ms.Position = 0;

                XImage img = XImage.FromStream(ms);
                PdfPage page = pdfDoc.AddPage();
                page.Width = XUnit.FromPoint(img.PixelWidth * 72 / 96.0);
                page.Height = XUnit.FromPoint(img.PixelHeight * 72 / 96.0);

                XGraphics gfx = XGraphics.FromPdfPage(page);
                gfx.DrawImage(img, 0, 0, page.Width.Point, page.Height.Point);
            }
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new PageAccueil());
            LogAction("Navigation", "Retour vers PageAccueil");
        }
    }
}
