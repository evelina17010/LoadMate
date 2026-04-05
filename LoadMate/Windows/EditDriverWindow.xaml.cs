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
    /// Логика взаимодействия для EditDriverWindow.xaml
    /// </summary>
    public partial class EditDriverWindow : Window
    {
        private Driver currentDriver;
        private User currentUser;
        public EditDriverWindow(Driver driver)
        {
            InitializeComponent();
            currentDriver = driver;
            currentUser = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == driver.User_id);
            LoadStatuses();
            LoadDriverData();
        }
        private void LoadStatuses()
        {
            var statuses = Conn.loadMateEntities.DriverStatus.ToList();
            cmbStatus.ItemsSource = statuses;
            cmbStatus.DisplayMemberPath = "Name";
            cmbStatus.SelectedValuePath = "DriverStatus_id";
        }
        private void LoadDriverData()
        {
            if (currentUser != null)
            {
                txtFullName.Text = currentUser.Full_name;
                txtEmail.Text = currentUser.Email;
                txtPhone.Text = currentUser.Phone;

                var login = Conn.loadMateEntities.Login.FirstOrDefault(l => l.User_id == currentUser.User_id);
                if (login != null)
                {
                    txtUsername.Text = login.Username;
                }
            }
            txtLicenseNumber.Text = currentDriver.License_number;
            txtExperience.Text = currentDriver.Experience_years?.ToString() ?? "";
            cmbStatus.SelectedValue = currentDriver.DriverStatus_id;
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
        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }
        private bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Введите ФИО водителя", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show("Введите корректный Email", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtLicenseNumber.Text))
            {
                MessageBox.Show("Введите номер лицензии", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!string.IsNullOrWhiteSpace(txtExperience.Text))
            {
                if (!int.TryParse(txtExperience.Text, out int exp) || exp < 0 || exp > 70)
                {
                    MessageBox.Show("Введите корректный стаж (от 0 до 70 лет)", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            if (cmbStatus.SelectedValue == null)
            {
                MessageBox.Show("Выберите статус водителя", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateData()) return;
                if (currentUser != null)
                {
                    currentUser.Full_name = txtFullName.Text.Trim();
                    currentUser.Email = txtEmail.Text.Trim();
                    currentUser.Phone = txtPhone.Text.Trim();

                    if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                    {
                        var login = Conn.loadMateEntities.Login.FirstOrDefault(l => l.User_id == currentUser.User_id);
                        if (login != null)
                        {
                            login.Password_hash = HashPassword(txtPassword.Password.Trim());
                        }
                    }
                }
                currentDriver.License_number = txtLicenseNumber.Text.Trim();
                if (int.TryParse(txtExperience.Text, out int experience))
                    currentDriver.Experience_years = experience;
                else
                    currentDriver.Experience_years = null;
                currentDriver.DriverStatus_id = (int)cmbStatus.SelectedValue;
                Conn.loadMateEntities.SaveChanges();
                MessageBox.Show("Данные водителя успешно сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}