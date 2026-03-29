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
    /// Логика взаимодействия для AddTruckWindow.xaml
    /// </summary>
    public partial class AddTruckWindow : Window
    {
        public AddTruckWindow()
        {
            InitializeComponent();
            LoadStatuses();
        }

        private void LoadStatuses()
        {
            var statuses = Conn.loadMateEntities.TruckStatus.ToList();
            cmbStatus.ItemsSource = statuses;
            cmbStatus.DisplayMemberPath = "Name";
            cmbStatus.SelectedValuePath = "TruckStatus_id";
            if (statuses.Count > 0) cmbStatus.SelectedIndex = 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtModel.Text) ||
                    string.IsNullOrWhiteSpace(txtRegNumber.Text) ||
                    !decimal.TryParse(txtCapacityKg.Text, out decimal capacityKg) || capacityKg <= 0 ||
                    !decimal.TryParse(txtCapacityM3.Text, out decimal capacityM3) || capacityM3 <= 0)
                {
                    MessageBox.Show("Заполните все обязательные поля корректно", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int? year = null;
                if (!string.IsNullOrWhiteSpace(txtYear.Text))
                {
                    if (!int.TryParse(txtYear.Text, out int yearValue) || yearValue < 1900 || yearValue > DateTime.Now.Year + 1)
                    {
                        MessageBox.Show("Введите корректный год выпуска", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    year = yearValue;
                }

                decimal? fuelConsumption = null;
                if (!string.IsNullOrWhiteSpace(txtFuelConsumption.Text))
                {
                    if (!decimal.TryParse(txtFuelConsumption.Text, out decimal fuelValue) || fuelValue <= 0)
                    {
                        MessageBox.Show("Введите корректный расход топлива", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    fuelConsumption = fuelValue;
                }

                var newTruck = new Truck
                {
                    Model = txtModel.Text.Trim(),
                    Registration_number = txtRegNumber.Text.Trim(),
                    Capacity_kg = capacityKg,
                    Capacity_m3 = capacityM3,
                    Dimensions = string.IsNullOrWhiteSpace(txtDimensions.Text) ? null : txtDimensions.Text.Trim(),
                    Year_manufacture = year,
                    Fuel_consumption = fuelConsumption,
                    TruckStatus_id = (int)cmbStatus.SelectedValue,
                    Driver_id = null
                };

                Conn.loadMateEntities.Truck.Add(newTruck);
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
