// Pages/LoginPage.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string passwordHash = HashPassword(password);

            var login = Conn.loadMateEntities.Login
                .FirstOrDefault(l => l.Username == username && l.Password_hash == passwordHash && l.Is_active == true);

            if (login != null)
            {
                var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == login.User_id);

                if (user != null)
                {
                    if (user.UserStatus_id == 2)
                    {
                        MessageBox.Show("Ваш аккаунт заблокирован", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    switch (user.Role_id)
                    {
                        case 1:
                            NavigationService.Navigate(new AdminPage(user));
                            break;
                        case 2:
                            NavigationService.Navigate(new ClientPage(user));
                            break;
                        case 4:
                            NavigationService.Navigate(new DispatcherPage(user));
                            break;
                        default:
                            MessageBox.Show("Неизвестная роль пользователя", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                }
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegisterPage());
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }
    }
}