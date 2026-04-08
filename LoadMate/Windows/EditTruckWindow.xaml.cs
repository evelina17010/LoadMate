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
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                cmbStatus.ItemsSource = Conn.loadMateEntities.TruckStatus.ToList();
                var drivers = Conn.loadMateEntities.Driver.Select(d => new
                {
                    d.Driver_id,
                    DriverName = d.User.Full_name
                }).ToList();
                var driverList = new List<object> { new { Driver_id = (int?)null, DriverName = "Не назначен" } };
                driverList.AddRange(drivers.Cast<object>());

                cmbDriver.ItemsSource = driverList;
                txtModel.Text = currentTruck.Model;
                txtRegNumber.Text = currentTruck.Registration_number;
                txtCapacityKg.Text = currentTruck.Capacity_kg.ToString();
                txtCapacityM3.Text = currentTruck.Capacity_m3.ToString();
                cmbStatus.SelectedValue = currentTruck.TruckStatus_id;
                cmbDriver.SelectedValue = currentTruck.Driver_id;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtModel.Text) || cmbStatus.SelectedValue == null)
            {
                MessageBox.Show("Заполните модель и выберите статус!");
                return;
            }

            try
            {
                currentTruck.Model = txtModel.Text.Trim();
                currentTruck.Registration_number = txtRegNumber.Text.Trim().ToUpper();
                if (decimal.TryParse(txtCapacityKg.Text.Replace(".", ","), out decimal kg))
                   currentTruck.Capacity_kg = kg;
                if (decimal.TryParse(txtCapacityM3.Text.Replace(".", ","), out decimal m3))
                    currentTruck.Capacity_m3 = m3;
                currentTruck.TruckStatus_id = (int)cmbStatus.SelectedValue;
                var selectedDriverId = cmbDriver.SelectedValue;
                currentTruck.Driver_id = (selectedDriverId == null) ? null : (int?)selectedDriverId;
                Conn.loadMateEntities.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}