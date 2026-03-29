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
    /// Логика взаимодействия для EditTruckWindow.xaml
    /// </summary>
    public partial class EditTruckWindow : Window
    {
        private Truck currentTruck;

        public EditTruckWindow(Truck truck)
        {
            InitializeComponent();
            currentTruck = truck;
            LoadStatuses();
            LoadDrivers();
            LoadTruckData();
        }

        private void LoadStatuses()
        {
            var statuses = Conn.loadMateEntities.TruckStatus.ToList();
            cmbStatus.ItemsSource = statuses;
            cmbStatus.DisplayMemberPath = "Name";
            cmbStatus.SelectedValuePath = "TruckStatus_id";
        }

        private void LoadDrivers()
        {
            var drivers = Conn.loadMateEntities.Driver.ToList();
            var driversWithNames = drivers.Select(d => new
            {
                d.Driver_id,
                DriverName = GetDriverName(d.User_id)
            }).ToList();

            var driverList = driversWithNames.Select(d => new { d.Driver_id, d.DriverName }).ToList();
            driverList.Insert(0, new { Driver_id = 0, DriverName = "Не назначен" });

            cmbDriver.ItemsSource = driverList;
            cmbDriver.DisplayMemberPath = "DriverName";
            cmbDriver.SelectedValuePath = "Driver_id";
        }

        private string GetDriverName(int userId)
        {
            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == userId);
            return user != null ? user.Full_name : "Не указан";
        }

        private void LoadTruckData()
        {
            txtModel.Text = currentTruck.Model;
            txtRegNumber.Text = currentTruck.Registration_number;
            txtCapacityKg.Text = currentTruck.Capacity_kg.ToString();
            txtCapacityM3.Text = currentTruck.Capacity_m3.ToString();
            txtDimensions.Text = currentTruck.Dimensions;
            txtYear.Text = currentTruck.Year_manufacture?.ToString() ?? "";
            txtFuelConsumption.Text = currentTruck.Fuel_consumption?.ToString() ?? "";
            cmbStatus.SelectedValue = currentTruck.TruckStatus_id;

            if (currentTruck.Driver_id.HasValue)
            {
                cmbDriver.SelectedValue = currentTruck.Driver_id.Value;
            }
            else
            {
                cmbDriver.SelectedValue = 0;
            }
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

                currentTruck.Model = txtModel.Text.Trim();
                currentTruck.Registration_number = txtRegNumber.Text.Trim();
                currentTruck.Capacity_kg = capacityKg;
                currentTruck.Capacity_m3 = capacityM3;
                currentTruck.Dimensions = string.IsNullOrWhiteSpace(txtDimensions.Text) ? null : txtDimensions.Text.Trim();
                currentTruck.Year_manufacture = year;
                currentTruck.Fuel_consumption = fuelConsumption;
                currentTruck.TruckStatus_id = (int)cmbStatus.SelectedValue;

                int selectedDriverId = (int)cmbDriver.SelectedValue;
                currentTruck.Driver_id = selectedDriverId == 0 ? null : (int?)selectedDriverId;

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
