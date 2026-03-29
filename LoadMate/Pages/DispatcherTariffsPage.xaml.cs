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
using LoadMate.DBConn;

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для DispatcherTariffsPage.xaml
    /// </summary>
    public partial class DispatcherTariffsPage : Page
    {
        public DispatcherTariffsPage()
        {
            InitializeComponent();
            LoadTariffs();
        }

        private void LoadTariffs()
        {
            var tariffs = Conn.loadMateEntities.Tariff.ToList();
            TariffsGrid.ItemsSource = tariffs;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTariffs();
        }
    }
}
