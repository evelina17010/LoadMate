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
    /// Логика взаимодействия для AssignTruckWindow.xaml
    /// </summary>
    public partial class AssignTruckWindow : Window
    {
        private Order currentOrder;
        private Truck selectedTruck;

        public AssignTruckWindow(Order order)
        {
            InitializeComponent();
            currentOrder = order;
            LoadOrderInfo();
            LoadTrucks();
        }

        private void LoadOrderInfo()
        {
            txtOrderNumber.Text = currentOrder.Order_number;

            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == currentOrder.Cargo_id);
            if (cargo != null)
            {
                txtWeight.Text = cargo.Weight_kg.ToString() + " кг";
                txtVolume.Text = cargo.Volume_m3.ToString() + " м³";
            }
        }

        private void LoadTrucks()
        {
            var trucks = Conn.loadMateEntities.Truck.ToList();

            var trucksWithDetails = trucks.Select(t => new
            {
                t.Truck_id,
                t.Model,
                t.Registration_number,
                t.Capacity_kg,
                t.Capacity_m3,
                DriverName = GetDriverName(t.Driver_id),
                StatusName = GetTruckStatusName(t.TruckStatus_id)
            }).ToList();

            TrucksGrid.ItemsSource = trucksWithDetails;
        }

        private string GetDriverName(int? driverId)
        {
            if (!driverId.HasValue) return "Не назначен";
            var driver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == driverId);
            if (driver == null) return "Не назначен";
            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == driver.User_id);
            return user?.Full_name ?? "Не назначен";
        }

        private string GetTruckStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.TruckStatus.FirstOrDefault(ts => ts.TruckStatus_id == statusId);
            return status?.Name ?? "Не указан";
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            var trucks = Conn.loadMateEntities.Truck.ToList();

            if (chkOnlyAvailable.IsChecked == true)
            {
                trucks = trucks.Where(t => t.TruckStatus_id == 1).ToList();
            }

            if (decimal.TryParse(txtMinCapacity.Text, out decimal minCapacity) && minCapacity > 0)
            {
                trucks = trucks.Where(t => t.Capacity_kg >= minCapacity).ToList();
            }

            var trucksWithDetails = trucks.Select(t => new
            {
                t.Truck_id,
                t.Model,
                t.Registration_number,
                t.Capacity_kg,
                t.Capacity_m3,
                DriverName = GetDriverName(t.Driver_id),
                StatusName = GetTruckStatusName(t.TruckStatus_id)
            }).ToList();

            TrucksGrid.ItemsSource = trucksWithDetails;
        }

        private void TrucksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = TrucksGrid.SelectedItem;
            if (selected != null)
            {
                var property = selected.GetType().GetProperty("Truck_id");
                if (property != null)
                {
                    int truckId = (int)property.GetValue(selected);
                    selectedTruck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
                }
            }
        }

        private void Assign_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTruck == null)
            {
                MessageBox.Show("Выберите транспортное средство", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            currentOrder.Truck_id = selectedTruck.Truck_id;
            selectedTruck.TruckStatus_id = 3;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}