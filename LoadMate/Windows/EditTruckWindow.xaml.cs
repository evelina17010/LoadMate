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
            cmbStatus.ItemsSource = Conn.loadMateEntities.TruckStatus.ToList();
            cmbStatus.DisplayMemberPath = "Name";
            cmbStatus.SelectedValuePath = "TruckStatus_id";
        }
        private void LoadDrivers()
        {
            var driverList = Conn.loadMateEntities.Driver.Select(d => new {d.Driver_id,
            DriverName = d.User.Full_name}).ToList<object>();
            driverList.Insert(0, new { Driver_id = 0, DriverName = "Не назначен" });
            cmbDriver.ItemsSource = driverList;
            cmbDriver.DisplayMemberPath = "DriverName";
            cmbDriver.SelectedValuePath = "Driver_id";
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
            cmbDriver.SelectedValue = currentTruck.Driver_id ?? 0;
        }
        private bool TryParseDecimal(string input, out decimal result)
        {
            if (string.IsNullOrWhiteSpace(input)) { result = 0; return false; }
            return decimal.TryParse(input.Replace(".", ","), out result);
        }
        private bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(txtModel.Text) || string.IsNullOrWhiteSpace(txtRegNumber.Text))
            {
                MessageBox.Show("Заполните модель и номер", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!TryParseDecimal(txtCapacityKg.Text, out decimal kg) || kg <= 0 ||
                !TryParseDecimal(txtCapacityM3.Text, out decimal m3) || m3 <= 0)
            {
                MessageBox.Show("Введите корректные числа для веса и объема", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (cmbStatus.SelectedValue == null)
            {
                MessageBox.Show("Выберите статус", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateData()) return;
                currentTruck.Model = txtModel.Text.Trim();
                currentTruck.Registration_number = txtRegNumber.Text.Trim().ToUpper();
                TryParseDecimal(txtCapacityKg.Text, out decimal kg);
                currentTruck.Capacity_kg = kg;
                TryParseDecimal(txtCapacityM3.Text, out decimal m3);
                currentTruck.Capacity_m3 = m3;
                currentTruck.Dimensions = string.IsNullOrWhiteSpace(txtDimensions.Text) ? null : txtDimensions.Text.Trim();
                if (int.TryParse(txtYear.Text, out int year)) currentTruck.Year_manufacture = year;
                else currentTruck.Year_manufacture = null;
                if (TryParseDecimal(txtFuelConsumption.Text, out decimal fuel)) currentTruck.Fuel_consumption = fuel;
                else currentTruck.Fuel_consumption = null;
                currentTruck.TruckStatus_id = (int)cmbStatus.SelectedValue;
                int selectedId = Convert.ToInt32(cmbDriver.SelectedValue);
                currentTruck.Driver_id = (selectedId == 0) ? null : (int?)selectedId;
                Conn.loadMateEntities.SaveChanges();
               MessageBox.Show("Сохранено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}