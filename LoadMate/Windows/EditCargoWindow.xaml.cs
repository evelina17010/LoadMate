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
    /// Логика взаимодействия для EditCargoWindow.xaml
    /// </summary>
    public partial class EditCargoWindow : Window
    {
        private Cargo currentCargo;
       public EditCargoWindow(Cargo cargo)
        {
            InitializeComponent();
            currentCargo = cargo;
            LoadClients();
            LoadCargoTypes();
            LoadCargoData();
        }
        private void LoadClients()
        {
            var clients = Conn.loadMateEntities.User.Where(u => u.Role_id == 2).ToList();
            cmbClient.ItemsSource = clients;
            cmbClient.DisplayMemberPath = "Full_name";
            cmbClient.SelectedValuePath = "User_id";
        }
        private void LoadCargoTypes()
        {
            var types = Conn.loadMateEntities.CargoType.ToList();
            cmbCargoType.ItemsSource = types;
            cmbCargoType.DisplayMemberPath = "Name";
            cmbCargoType.SelectedValuePath = "CargoType_id";
        }
        private void LoadCargoData()
        {
            cmbClient.SelectedValue = currentCargo.Client_id;
            cmbCargoType.SelectedValue = currentCargo.CargoType_id;
            txtDescription.Text = currentCargo.Description;
            txtWeight.Text = currentCargo.Weight_kg.ToString();
            txtVolume.Text = currentCargo.Volume_m3.ToString();
            chkFragile.IsChecked = currentCargo.Is_fragile;
            chkDangerous.IsChecked = currentCargo.Is_dangerous;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateData()) return;

                currentCargo.Client_id = (int)cmbClient.SelectedValue;
                currentCargo.CargoType_id = (int)cmbCargoType.SelectedValue;
                currentCargo.Description = txtDescription.Text.Trim();
                currentCargo.Weight_kg = decimal.Parse(txtWeight.Text.Replace(".", ","));
                currentCargo.Volume_m3 = decimal.Parse(txtVolume.Text.Replace(".", ","));
                currentCargo.Is_fragile = chkFragile.IsChecked == true;
                currentCargo.Is_dangerous = chkDangerous.IsChecked == true;

                Conn.loadMateEntities.SaveChanges();

                MessageBox.Show("Данные груза успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
       private bool ValidateData()
        {
            if (cmbClient.SelectedValue == null)
            {
                MessageBox.Show("Выберите клиента из списка", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (cmbCargoType.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип груза", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("Описание груза не может быть пустым", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!decimal.TryParse(txtWeight.Text.Replace(".", ","), out decimal weight) || weight <= 0)
            {
                MessageBox.Show("Введите корректный положительный вес", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!decimal.TryParse(txtVolume.Text.Replace(".", ","), out decimal volume) || volume <= 0)
            {
                MessageBox.Show("Введите корректный положительный объем", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}