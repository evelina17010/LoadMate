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
    /// Логика взаимодействия для ClientPage.xaml
    /// </summary>
    public partial class ClientPage : Page
    {
        private User currentUser;

        public ClientPage(User user)
        {
            InitializeComponent();
            currentUser = user;
            DataContext = new { FullName = user.Full_name };
            CreateOrder_Click(null, null);
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

        }
    }
}