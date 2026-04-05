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
    /// Логика взаимодействия для AddUserWindow.xaml
    /// </summary>
    public partial class AddUserWindow : Window
    {
        public AddUserWindow()
        {
            InitializeComponent();
            LoadRoles();
        }
        private void LoadRoles()
        {
            try
            {
                var roles = Conn.loadMateEntities.Role.ToList();
                cmbRole.SelectedValuePath = "Role_id";
                cmbRole.ItemsSource = roles;
                if (roles.Count > 0) cmbRole.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке ролей: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (string.IsNullOrWhiteSpace(fullName) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    MessageBox.Show("Введите корректный адрес электронной почты.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(phone) && !Regex.IsMatch(phone, @"^((\+7|8)+([0-9]){10})$"))
                {
                    MessageBox.Show("Введите телефон в формате +7XXXXXXXXXX или 8XXXXXXXXXX.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (password.Length < 6 || !password.Any(char.IsDigit) || !password.Any(char.IsLetter))
                {
                    MessageBox.Show("Пароль должен быть не менее 6 символов и содержать как буквы, так и цифры.", "Безопасность", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (cmbRole.SelectedValue == null)
                {
                    MessageBox.Show("Выберите роль для нового пользователя.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (Conn.loadMateEntities.Login.Any(l => l.Username.ToLower() == username.ToLower()))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (Conn.loadMateEntities.User.Any(u => u.Email.ToLower() == email.ToLower()))
                {
                    MessageBox.Show("Пользователь с такой почтой уже зарегистрирован.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var newUser = new User
                {
                    Full_name = fullName,
                    Email = email,
                    Phone = phone,
                    Role_id = (int)cmbRole.SelectedValue,
                    UserStatus_id = 1,
                    Created_at = DateTime.Now
                };
                var newLogin = new Login
                {
                    User = newUser, 
                    Username = username,
                    Password_hash = HashPassword(password),
                    Is_active = true,
                    Failed_attempts = 0
                };
                Conn.loadMateEntities.User.Add(newUser);
                Conn.loadMateEntities.Login.Add(newLogin);
                Conn.loadMateEntities.SaveChanges();

                MessageBox.Show("Пользователь успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}