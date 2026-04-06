// Pages/RegisterPage.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
    public partial class RegisterPage : Page
    {
        public RegisterPage()
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
        private bool IsDataValid(string fullName, string email, string phone, string username, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Заполните все обязательные поля (ФИО, Email, Логин, Пароль)", "Ошибка валидации");
                return false;
            }
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, emailPattern))
            {
                MessageBox.Show("Введите корректный адрес электронной почты", "Ошибка валидации");
                return false;
            }
            if (!string.IsNullOrEmpty(phone))
            {
                string cleanPhone = Regex.Replace(phone, @"[^\d]", "");
                if (cleanPhone.Length < 10)
                {
                    MessageBox.Show("Номер телефона слишком короткий", "Ошибка валидации");
                    return false;
                }
            }
            if (username.Length < 3 || username.Contains(" "))
            {
                MessageBox.Show("Логин должен быть не менее 3 символов и не содержать пробелов", "Ошибка валидации");
                return false;
            }
            if (password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов", "Ошибка валидации");
                return false;
            }
            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка валидации");
                return false;
            }
            return true;
        }
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password; 
            string confirmPassword = txtConfirmPassword.Password;
            if (!IsDataValid(fullName, email, phone, username, password, confirmPassword))
                return;
            try
            {
                if (Conn.loadMateEntities.Login.Any(l => l.Username == username))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка");
                    return;
                }
                if (Conn.loadMateEntities.User.Any(u => u.Email == email))
                {
                    MessageBox.Show("Пользователь с таким Email уже существует", "Ошибка");
                    return;
                }
                var newUser = new User
                {
                    Full_name = fullName,
                    Email = email,
                    Phone = phone,
                    Role_id = 2, 
                    UserStatus_id = 1, 
                    Created_at = DateTime.Now
                };
                Conn.loadMateEntities.User.Add(newUser);
                Conn.loadMateEntities.SaveChanges(); 
                var newLogin = new Login
                {
                    User_id = newUser.User_id,
                    Username = username,
                    Password_hash = HashPassword(password),
                    Is_active = true,
                    Failed_attempts = 0
                };
                Conn.loadMateEntities.Login.Add(newLogin);
                Conn.loadMateEntities.SaveChanges();
                MessageBox.Show("Регистрация прошла успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.Navigate(new LoginPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка базы данных: {ex.Message}", "Ошибка");
            }
        }
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginPage());
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }
    }
}