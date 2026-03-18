using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AppCrmLourde
{
    /// <summary>
    /// Logique d'interaction pour PageAdmin.xaml
    /// </summary>
    public partial class PageAdmin : Page
    {
        public PageAdmin()
        {
            InitializeComponent();
        }

        private void BtnGestionManagers_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageGestionManagers()); // À créer
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
