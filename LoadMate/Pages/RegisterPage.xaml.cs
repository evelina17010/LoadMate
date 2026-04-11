using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using Microsoft.Win32;
using LoadMate.DBConn;

namespace LoadMate.Pages
{
    public partial class RegisterPage : Page
    {
        private byte[] _photoBytes = null;

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

        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png";
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    _photoBytes = File.ReadAllBytes(ofd.FileName);
                    using (MemoryStream ms = new MemoryStream(_photoBytes))
                    {
                        BitmapImage bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = ms;
                        bi.EndInit();
                        imgAvatar.Source = bi;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка чтения файла: " + ex.Message);
                }
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
                MessageBox.Show("Все поля, кроме телефона, обязательны.", "Валидация");
                return false;
            }
            if (!Regex.IsMatch(fullName, @"^[a-zA-Zа-яА-ЯёЁ\s\-]+$") || fullName.Trim().Split(' ').Length < 2)
            {
                MessageBox.Show("Введите корректное ФИО (Фамилия Имя).", "Валидация");
                return false;
            }
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введите корректный Email.", "Валидация");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^(\+7|8)\d{10}$"))
            {
                MessageBox.Show("Формат телефона: +7XXXXXXXXXX.", "Валидация");
                return false;
            }
            if (cmbGender.SelectedValue == null)
            {
                MessageBox.Show("Выберите пол.", "Валидация");
                return false;
            }
            if (password.Length < 6 || !password.Any(char.IsDigit) || !password.Any(char.IsLetter))
            {
                MessageBox.Show("Пароль от 6 символов (буквы и цифры).", "Безопасность");
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
                    MessageBox.Show("Логин занят.", "Ошибка");
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
                    Created_at = DateTime.Now,
                    ImagePath = _photoBytes
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

                MessageBox.Show("Регистрация успешна!", "Успех");
                NavigationService.Navigate(new LoginPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new LoginPage());

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
            else NavigationService.Navigate(new LoginPage());
        }
    }
}