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
            cmbClient.SelectedValuePath = "User_id";
            cmbClient.ItemsSource = clients;
            if (clients.Count > 0) cmbClient.SelectedIndex = 0;
        }
        private void LoadCargoTypes()
        {
            var types = Conn.loadMateEntities.CargoType.ToList();
            cmbCargoType.SelectedValuePath = "CargoType_id";
            cmbCargoType.ItemsSource = types;
            if (types.Count > 0) cmbCargoType.SelectedIndex = 0;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbClient.SelectedValue == null || cmbCargoType.SelectedValue == null)
                {
                    MessageBox.Show("Выберите клиента и тип груза", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtDescription.Text))
                {
                    MessageBox.Show("Введите описание груза", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!decimal.TryParse(txtWeight.Text.Replace('.', ','), out decimal weight) || weight <= 0)
                {
                    MessageBox.Show("Введите корректный вес (больше 0)", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!decimal.TryParse(txtVolume.Text.Replace('.', ','), out decimal volume) || volume <= 0)
                {
                    MessageBox.Show("Введите корректный объем (больше 0)", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show("Груз успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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