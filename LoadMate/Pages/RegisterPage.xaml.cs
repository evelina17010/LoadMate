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
            LoadGenders();
        }

        private void LoadGenders()
        {
            try
            {
                var genders = Conn.loadMateEntities.Gender.ToList();
                cmbGender.SelectedValuePath = "Gender_id";
                cmbGender.ItemsSource = genders;
                if (genders.Count > 0) cmbGender.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников: " + ex.Message);
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private bool IsDataValid(string fullName, string email, string phone, string username, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Все поля, кроме телефона, обязательны для заполнения.", "Валидация");
                return false;
            }
            if (!Regex.IsMatch(fullName, @"^[a-zA-Zа-яА-ЯёЁ\s\-]+$") || fullName.Trim().Split(' ').Length < 2)
            {
                MessageBox.Show("Введите корректное ФИО (только буквы, Фамилия Имя).", "Валидация");
                return false;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введите корректный адрес электронной почты.", "Валидация");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(phone))
            {
                if (!Regex.IsMatch(phone, @"^(\+7|8)\d{10}$"))
                {
                    MessageBox.Show("Телефон должен быть в формате +7XXXXXXXXXX или 8XXXXXXXXXX.", "Валидация");
                    return false;
                }
            }
            if (cmbGender.SelectedValue == null)
            {
                MessageBox.Show("Выберите пол.", "Валидация");
                return false;
            }

            if (password.Length < 6 || !password.Any(char.IsDigit) || !password.Any(char.IsLetter))
            {
                MessageBox.Show("Пароль должен быть не менее 6 символов и содержать как буквы, так и цифры.", "Безопасность");
                return false;
            }
            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.", "Валидация");
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
                var db = Conn.loadMateEntities;

                if (db.Login.Any(l => l.Username.ToLower() == username.ToLower()))
                {
                    MessageBox.Show("Этот логин уже занят. Попробуйте другой.", "Ошибка");
                    return;
                }

                if (db.User.Any(u => u.Email.ToLower() == email.ToLower()))
                {
                    MessageBox.Show("Пользователь с таким Email уже зарегистрирован.", "Ошибка");
                    return;
                }

                var newUser = new User
                {
                    Full_name = fullName,
                    Email = email,
                    Phone = phone,
                    Role_id = 2,
                    Gender_id = (int)cmbGender.SelectedValue,
                    UserStatus_id = 1,
                    Created_at = DateTime.Now
                };

                db.User.Add(newUser);
                db.SaveChanges(); 

                var newLogin = new Login
                {
                    User_id = newUser.User_id,
                    Username = username,
                    Password_hash = HashPassword(password),
                    Is_active = true,
                    Failed_attempts = 0
                };

                db.Login.Add(newLogin);
                db.SaveChanges();

                MessageBox.Show("Регистрация успешно завершена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.Navigate(new LoginPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Критическая ошибка");
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new LoginPage());

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
            else NavigationService.Navigate(new MainPage());
        }
    }
}