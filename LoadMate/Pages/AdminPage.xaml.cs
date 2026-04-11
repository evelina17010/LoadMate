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
using System.IO;
using LoadMate.DBConn;

namespace LoadMate.Pages
{
    public partial class AdminPage : Page
    {
        private User currentUser;

        public AdminPage(User user)
        {
            InitializeComponent();
            currentUser = user;

            DataContext = new
            {
                FullName = user.Full_name,
                UserPhoto = GetImage(user.ImagePath)
            };

            Users_Click(null, null);
        }

        private BitmapImage GetImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;

            try
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    return image;
                }
            }
            catch
            {
                return null;
            }
        }

        private void Users_Click(object sender, RoutedEventArgs e)
        {
            AdminFrame.NavigationService.Navigate(new AdminUsersPage());
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            AdminFrame.NavigationService.Navigate(new AdminOrdersPage());
        }

        private void Cargo_Click(object sender, RoutedEventArgs e)
        {
            AdminFrame.NavigationService.Navigate(new AdminCargoPage());
        }

        private void Drivers_Click(object sender, RoutedEventArgs e)
        {
            AdminFrame.NavigationService.Navigate(new AdminDriversPage());
        }

        private void Trucks_Click(object sender, RoutedEventArgs e)
        {
            AdminFrame.NavigationService.Navigate(new AdminTrucksPage());
        }

        private void Tariffs_Click(object sender, RoutedEventArgs e)
        {
            AdminFrame.NavigationService.Navigate(new AdminTariffsPage());
        }

        private void Payments_Click(object sender, RoutedEventArgs e)
        {
            AdminFrame.NavigationService.Navigate(new AdminPaymentsPage());
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }
    }
}