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
using System.Windows.Shapes;
using LoadMate.DBConn;

namespace LoadMate.Windows
{
    /// <summary>
    /// Логика взаимодействия для AddDriverWindow.xaml
    /// </summary>
    public partial class AddDriverWindow : Window
    {
        public AddDriverWindow()
        {
            InitializeComponent();
            LoadStatuses();
        }
        private void LoadStatuses()
        {
            var statuses = Conn.loadMateEntities.DriverStatus.ToList();
            cmbStatus.SelectedValuePath = "DriverStatus_id";
            cmbStatus.ItemsSource = statuses;
            if (statuses.Count > 0) cmbStatus.SelectedIndex = 0;
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
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtFullName.Text) ||
                    string.IsNullOrWhiteSpace(txtEmail.Text) ||
                    string.IsNullOrWhiteSpace(txtUsername.Text) ||
                    string.IsNullOrWhiteSpace(txtPassword.Password) ||
                    string.IsNullOrWhiteSpace(txtLicenseNumber.Text))
                {
                    MessageBox.Show("Заполните все обязательные поля", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string email = txtEmail.Text.Trim();
                string phone = txtPhone.Text.Trim();
                string password = txtPassword.Password;
                string username = txtUsername.Text.Trim();
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    MessageBox.Show("Введите корректный Email адрес", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(phone) && !Regex.IsMatch(phone, @"^((\+7|8)+([0-9]){10})$"))
                {
                    MessageBox.Show("Введите телефон в формате +7XXXXXXXXXX", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (password.Length < 6 || !password.Any(char.IsDigit))
                {
                    MessageBox.Show("Пароль должен быть от 6 символов и содержать цифру", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(txtExperience.Text, out int experience) || experience < 0)
                {
                    MessageBox.Show("Стаж должен быть числом (0 или больше)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (cmbStatus.SelectedValue == null)
                {
                    MessageBox.Show("Выберите статус водителя", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
               }
                if (Conn.loadMateEntities.Login.Any(l => l.Username == username))
                {
                    MessageBox.Show("Логин занят", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (Conn.loadMateEntities.User.Any(u => u.Email == email))
                {
                    MessageBox.Show("Email уже зарегистрирован", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var newUser = new User
                {
                    Full_name = txtFullName.Text.Trim(),
                    Email = email,
                    Phone = phone,
                    Role_id = 3,
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
                var newDriver = new Driver
                {
                    User_id = newUser.User_id,
                    DriverStatus_id = (int)cmbStatus.SelectedValue,
                    License_number = txtLicenseNumber.Text.Trim(),
                    Experience_years = experience,
                    Hire_date = DateTime.Now
                };
                Conn.loadMateEntities.Login.Add(newLogin);
                Conn.loadMateEntities.Driver.Add(newDriver);
                Conn.loadMateEntities.SaveChanges();
                MessageBox.Show("Водитель успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}