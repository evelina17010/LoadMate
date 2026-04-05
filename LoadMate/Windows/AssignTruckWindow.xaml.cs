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
        private Cargo currentCargo;
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
            currentCargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == currentOrder.Cargo_id);
            if (currentCargo != null)
            {
                txtWeight.Text = currentCargo.Weight_kg.ToString() + " кг";
                txtVolume.Text = currentCargo.Volume_m3.ToString() + " м³";
            }
        }
        private void LoadTrucks()
        {
            var trucks = Conn.loadMateEntities.Truck.ToList();
            UpdateGrid(trucks);
        }
        private void UpdateGrid(List<Truck> trucks)
        {
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
            var query = Conn.loadMateEntities.Truck.AsQueryable();
            if (chkOnlyAvailable.IsChecked == true)
            {
                query = query.Where(t => t.TruckStatus_id == 1);
            }
            if (decimal.TryParse(txtMinCapacity.Text, out decimal minCapacity) && minCapacity > 0)
            {
                query = query.Where(t => t.Capacity_kg >= minCapacity);
            }
            UpdateGrid(query.ToList());
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
            try
            {
                if (selectedTruck == null)
                {
                    MessageBox.Show("Выберите транспортное средство из списка.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (selectedTruck.Driver_id == null)
                {
                    MessageBox.Show("Выбранный транспорт не имеет назначенного водителя.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (currentCargo != null)
                {
                    if (selectedTruck.Capacity_kg < currentCargo.Weight_kg)
                    {
                        MessageBox.Show("Грузоподъемность транспорта меньше веса груза!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (selectedTruck.Capacity_m3 < currentCargo.Volume_m3)
                    {
                        MessageBox.Show("Объем кузова меньше объема груза!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                if (selectedTruck.TruckStatus_id != 1)
                {
                    var result = MessageBox.Show("Транспорт не имеет статуса 'Доступен'. Продолжить назначение?", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No) return;
                }
                currentOrder.Truck_id = selectedTruck.Truck_id;
                selectedTruck.TruckStatus_id = 3;
                Conn.loadMateEntities.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}