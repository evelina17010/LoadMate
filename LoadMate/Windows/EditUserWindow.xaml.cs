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
    /// Логика взаимодействия для EditUserWindow.xaml
    /// </summary>
    public partial class EditUserWindow : Window
    {
        private User currentUser;

        public EditUserWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            LoadRoles();
            LoadUserData();
        }

        private void LoadRoles()
        {
            var roles = Conn.loadMateEntities.Role.ToList();
            cmbRole.ItemsSource = roles;
            cmbRole.DisplayMemberPath = "Name";
            cmbRole.SelectedValuePath = "Role_id";
        }

        private void LoadUserData()
        {
            txtFullName.Text = currentUser.Full_name;
            txtEmail.Text = currentUser.Email;
            txtPhone.Text = currentUser.Phone;

            var login = Conn.loadMateEntities.Login.FirstOrDefault(l => l.User_id == currentUser.User_id);
            if (login != null)
            {
                txtUsername.Text = login.Username;
            }

            cmbRole.SelectedValue = currentUser.Role_id;
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
                    string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Заполните все обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                currentUser.Full_name = txtFullName.Text.Trim();
                currentUser.Email = txtEmail.Text.Trim();
                currentUser.Phone = txtPhone.Text.Trim();
                currentUser.Role_id = (int)cmbRole.SelectedValue;

                if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    var login = Conn.loadMateEntities.Login.FirstOrDefault(l => l.User_id == currentUser.User_id);
                    if (login != null)
                    {
                        login.Password_hash = HashPassword(txtPassword.Password.Trim());
                    }
                }

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