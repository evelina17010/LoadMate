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
    /// Логика взаимодействия для DispatcherPage.xaml
    /// </summary>
    public partial class DispatcherPage : Page
    {
        private User currentUser;

        public DispatcherPage(User user)
        {
            InitializeComponent();
            currentUser = user;
            DataContext = new { FullName = user.Full_name };
            Orders_Click(null, null);
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            //DispatcherFrame.NavigationService.Navigate(new DispatcherOrdersPage(currentUser.User_id));
        }

        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            //   DispatcherFrame.NavigationService.Navigate(new DispatcherCreateOrderPage(currentUser.User_id));
        }

        private void Drivers_Click(object sender, RoutedEventArgs e)
        {
            // DispatcherFrame.NavigationService.Navigate(new DispatcherDriversPage());
        }

        private void Trucks_Click(object sender, RoutedEventArgs e)
        {
            // DispatcherFrame.NavigationService.Navigate(new DispatcherTrucksPage());
        }

        private void Tariffs_Click(object sender, RoutedEventArgs e)
        {
            // DispatcherFrame.NavigationService.Navigate(new DispatcherTariffsPage());
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }
    }
}