using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;
using LoadMate.DBConn;

namespace LoadMate.Windows
{
    public partial class AddUserWindow : Window
    {
        private byte[] _photoBytes = null; 

        public AddUserWindow()
        {
            InitializeComponent();
            LoadDictionaryData();
        }

        private void LoadDictionaryData()
        {
            try
            {
                var db = Conn.loadMateEntities;

                var roles = db.Role.ToList();
                cmbRole.SelectedValuePath = "Role_id";
                cmbRole.ItemsSource = roles;
                if (roles.Count > 0) cmbRole.SelectedIndex = 0;

                var genders = db.Gender.ToList();
                cmbGender.SelectedValuePath = "Gender_id";
                cmbGender.ItemsSource = genders;
                if (genders.Count > 0) cmbGender.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fullName = txtFullName.Text.Trim();
                string email = txtEmail.Text.Trim();
                string phone = txtPhone.Text.Trim();
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;

                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Заполните все обязательные поля.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    MessageBox.Show("Введите корректный Email.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (password.Length < 6 || !password.Any(char.IsDigit) || !password.Any(char.IsLetter))
                {
                    MessageBox.Show("Пароль должен быть от 6 символов и содержать буквы и цифры.", "Безопасность", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbRole.SelectedValue == null || cmbGender.SelectedValue == null)
                {
                    MessageBox.Show("Выберите роль и пол.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var db = Conn.loadMateEntities;

                if (db.Login.Any(l => l.Username.ToLower() == username.ToLower()))
                {
                    MessageBox.Show("Логин занят.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newUser = new User
                {
                    Full_name = fullName,
                    Email = email,
                    Phone = phone,
                    Role_id = (int)cmbRole.SelectedValue,
                    Gender_id = (int)cmbGender.SelectedValue,
                    UserStatus_id = 1,
                    Created_at = DateTime.Now,
                    ImagePath = _photoBytes 
                };

                var newLogin = new Login
                {
                    User = newUser,
                    Username = username,
                    Password_hash = HashPassword(password),
                    Is_active = true,
                    Failed_attempts = 0
                };

                db.User.Add(newUser);
                db.Login.Add(newLogin);
                db.SaveChanges();

                MessageBox.Show("Пользователь успешно создан!", "Успех");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}