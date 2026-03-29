using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Логика взаимодействия для AddCargoWindow.xaml
    /// </summary>
    public partial class AddCargoWindow : Window
    {
        public AddCargoWindow()
        {
            InitializeComponent();
            LoadClients();
            LoadCargoTypes();
        }

        private void LoadClients()
        {
            var clients = Conn.loadMateEntities.User.Where(u => u.Role_id == 2).ToList();
            cmbClient.ItemsSource = clients;
            cmbClient.DisplayMemberPath = "Full_name";
            cmbClient.SelectedValuePath = "User_id";
            if (clients.Count > 0) cmbClient.SelectedIndex = 0;
        }

        private void LoadCargoTypes()
        {
            var types = Conn.loadMateEntities.CargoType.ToList();
            cmbCargoType.ItemsSource = types;
            cmbCargoType.DisplayMemberPath = "Name";
            cmbCargoType.SelectedValuePath = "CargoType_id";
            if (types.Count > 0) cmbCargoType.SelectedIndex = 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtDescription.Text) ||
                    !decimal.TryParse(txtWeight.Text, out decimal weight) || weight <= 0 ||
                    !decimal.TryParse(txtVolume.Text, out decimal volume) || volume <= 0)
                {
                    MessageBox.Show("Заполните все поля корректно", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newCargo = new Cargo
                {
                    Client_id = (int)cmbClient.SelectedValue,
                    CargoType_id = (int)cmbCargoType.SelectedValue,
                    Description = txtDescription.Text.Trim(),
                    Weight_kg = weight,
                    Volume_m3 = volume,
                    Is_fragile = chkFragile.IsChecked == true,
                    Is_dangerous = chkDangerous.IsChecked == true,
                    Created_at = DateTime.Now
                };

                Conn.loadMateEntities.Cargo.Add(newCargo);
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