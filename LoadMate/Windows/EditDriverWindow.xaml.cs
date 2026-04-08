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
        private Driver _currentDriver;
        private User _currentUser;

        public EditDriverWindow(Driver driver)
        {
            InitializeComponent();
            _currentDriver = driver;
            _currentUser = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == driver.User_id);

            LoadStatuses();
            LoadDriverData();
        }

        private void LoadStatuses()
        {
            cmbStatus.ItemsSource = Conn.loadMateEntities.DriverStatus.ToList();
        }

        private void LoadDriverData()
        {
            if (_currentUser != null)
            {
                txtFullName.Text = _currentUser.Full_name;
                txtEmail.Text = _currentUser.Email;
                txtPhone.Text = _currentUser.Phone;

                var login = Conn.loadMateEntities.Login.FirstOrDefault(l => l.User_id == _currentUser.User_id);
                if (login != null)
                {
                    txtUsername.Text = login.Username;
                }
            }

            txtLicenseNumber.Text = _currentDriver.License_number;
            txtExperience.Text = _currentDriver.Experience_years?.ToString() ?? "0";
            cmbStatus.SelectedValue = _currentDriver.DriverStatus_id;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateData()) return;

            try
            {
                if (_currentUser != null)
                {
                    _currentUser.Full_name = txtFullName.Text.Trim();
                    _currentUser.Email = txtEmail.Text.Trim();
                    _currentUser.Phone = txtPhone.Text.Trim();

                    if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                    {
                        var login = Conn.loadMateEntities.Login.FirstOrDefault(l => l.User_id == _currentUser.User_id);
                        if (login != null)
                        {
                            login.Password_hash = HashPassword(txtPassword.Password.Trim());
                        }
                    }
                }

                _currentDriver.License_number = txtLicenseNumber.Text.Trim();

                if (int.TryParse(txtExperience.Text, out int exp))
                    _currentDriver.Experience_years = exp;

                if (cmbStatus.SelectedValue != null)
                    _currentDriver.DriverStatus_id = (int)cmbStatus.SelectedValue;

                Conn.loadMateEntities.SaveChanges();

                MessageBox.Show("Данные водителя успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("ФИО не может быть пустым");
                return false;
            }
            if (!Regex.IsMatch(txtEmail.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введите корректный Email");
                return false;
            }
            if (cmbStatus.SelectedValue == null)
            {
                MessageBox.Show("Выберите статус");
                return false;
            }
            return true;
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return string.Concat(bytes.Select(b => b.ToString("x2")));
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}