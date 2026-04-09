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
    /// Логика взаимодействия для AssignDriverWindow.xaml
    /// </summary>
    public partial class AssignDriverWindow : Window
    {
        private Order _currentOrder;
        private Driver _selectedDriver;
        public AssignDriverWindow(Order order)
        {
            InitializeComponent();
            _currentOrder = order;
            LoadOrderInfo();
            LoadDrivers();
        }
        private void LoadOrderInfo()
        {
            txtOrderNumber.Text = _currentOrder.Order_number;
            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == _currentOrder.Cargo_id);
            if (cargo != null)
            {
                var client = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
                txtClient.Text = client?.Full_name ?? "Не указан";
                txtCargo.Text = cargo.Description;
            }
            var route = Conn.loadMateEntities.Route.FirstOrDefault(r => r.Route_id == _currentOrder.Route_id);
            if (route != null)
            {
                string startCity = GetCityName(route.Start_address_id);
                string endCity = GetCityName(route.End_address_id);
                txtRoute.Text = $"{startCity} -> {endCity}";
            }
        }
        private string GetCityName(int addressId)
        {
            var address = Conn.loadMateEntities.Address.FirstOrDefault(a => a.Address_id == addressId);
            if (address != null)
            {
                var street = Conn.loadMateEntities.Street.FirstOrDefault(s => s.Street_id == address.Street_id);
                if (street != null)
                {
                    var city = Conn.loadMateEntities.City.FirstOrDefault(c => c.City_id == street.City_id);
                    return city?.Name ?? "Неизвестный город";
                }
            }
            return "Адрес не найден";
        }
        private void LoadDrivers()
        {
            var availableDrivers = Conn.loadMateEntities.Driver.Where(d => d.DriverStatus_id == 1).ToList();
            var driversWithDetails = availableDrivers.Select(d => {
                var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == d.User_id);
                var status = Conn.loadMateEntities.DriverStatus.FirstOrDefault(ds => ds.DriverStatus_id == d.DriverStatus_id);
                return new
                {
                    d.Driver_id,
                    DriverName = user?.Full_name ?? "Неизвестно",
                    Phone = user?.Phone ?? "-",
                    d.License_number,
                    d.Experience_years,
                    DriverStatus = status?.Name ?? "Свободен"
                };
            }).ToList();
            DriversGrid.ItemsSource = driversWithDetails;
        }
        private void DriversGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DriversGrid.SelectedItem == null) return;
            dynamic selected = DriversGrid.SelectedItem;
            int driverId = selected.Driver_id;
            _selectedDriver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == driverId);
        }
        private void Assign_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedDriver == null)
                {
                    MessageBox.Show("Пожалуйста, выберите водителя из списка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var newTruck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Driver_id == _selectedDriver.Driver_id);
                if (newTruck == null)
                {
                    MessageBox.Show("За выбранным водителем не закреплен транспорт.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_currentOrder.Truck_id != 0)
                {
                    var oldTruck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == _currentOrder.Truck_id);
                    if (oldTruck != null && oldTruck.Driver_id.HasValue)
                    {
                        var oldDriver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == oldTruck.Driver_id);
                        if (oldDriver != null)
                        {
                            oldDriver.DriverStatus_id = 1; 
                        }
                    }
                }
                _currentOrder.Truck_id = newTruck.Truck_id;
                _currentOrder.OrderStatus_id = 3; 
                _selectedDriver.DriverStatus_id = 2; 
                Conn.loadMateEntities.SaveChanges();

                MessageBox.Show($"Водитель {_selectedDriver.User.Full_name} успешно назначен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}