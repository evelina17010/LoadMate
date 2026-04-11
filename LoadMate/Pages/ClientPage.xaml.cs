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
    public partial class ClientPage : Page
    {
        private User currentUser;

        public ClientPage(User user)
        {
            InitializeComponent();
            currentUser = user;
            DataContext = new
            {
                FullName = user.Full_name,
                UserPhoto = GetImage(user.ImagePath)
            };
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

        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            ClientFrame.NavigationService.Navigate(new ClientCreateOrderPage(currentUser.User_id));
        }

        private void MyOrders_Click(object sender, RoutedEventArgs e)
        {
            ClientFrame.NavigationService.Navigate(new ClientOrdersPage(currentUser.User_id));
        }

        private void Payments_Click(object sender, RoutedEventArgs e)
        {
            ClientFrame.NavigationService.Navigate(new ClientPaymentsPage(currentUser.User_id));
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }

        private void Cargo_Click(object sender, RoutedEventArgs e)
        {
            ClientFrame.NavigationService.Navigate(new ClientCargoPage(currentUser.User_id));
        }
    }
}