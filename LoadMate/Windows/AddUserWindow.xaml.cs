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
            var roles = Conn.loadMateEntities.Role.ToList();
            cmbRole.ItemsSource = roles;
            cmbRole.DisplayMemberPath = "Name";
            cmbRole.SelectedValuePath = "Role_id";
            if (roles.Count > 0) cmbRole.SelectedIndex = 0;
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtFullName.Text) ||
                    string.IsNullOrWhiteSpace(txtEmail.Text) ||
                    string.IsNullOrWhiteSpace(txtUsername.Text) ||
                    string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    MessageBox.Show("Заполните все обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existingLogin = Conn.loadMateEntities.Login.FirstOrDefault(l => l.Username == txtUsername.Text.Trim());
                if (existingLogin != null)
                {
                    MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existingUser = Conn.loadMateEntities.User.FirstOrDefault(u => u.Email == txtEmail.Text.Trim());
                if (existingUser != null)
                {
                    MessageBox.Show("Пользователь с таким email уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newUser = new User
                {
                    Full_name = txtFullName.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Role_id = (int)cmbRole.SelectedValue,
                    UserStatus_id = 1,
                    Created_at = DateTime.Now
                };

                Conn.loadMateEntities.User.Add(newUser);
                Conn.loadMateEntities.SaveChanges();

                var newLogin = new Login
                {
                    User_id = newUser.User_id,
                    Username = txtUsername.Text.Trim(),
                    Password_hash = HashPassword(txtPassword.Password.Trim()),
                    Is_active = true,
                    Failed_attempts = 0
                };

                Conn.loadMateEntities.Login.Add(newLogin);
                Conn.loadMateEntities.SaveChanges();

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