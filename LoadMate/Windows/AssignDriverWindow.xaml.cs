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
        private Order currentOrder;
        private Driver selectedDriver;

        public AssignDriverWindow(Order order)
        {
            InitializeComponent();
            currentOrder = order;
            LoadOrderInfo();
            LoadDrivers();
        }

        private void LoadOrderInfo()
        {
            txtOrderNumber.Text = currentOrder.Order_number;

            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == currentOrder.Cargo_id);
            if (cargo != null)
            {
                var client = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
                txtClient.Text = client != null ? client.Full_name : "Не указан";
                txtCargo.Text = cargo.Description;
            }

            var route = Conn.loadMateEntities.Route.FirstOrDefault(r => r.Route_id == currentOrder.Route_id);
            if (route != null)
            {
                var startAddress = Conn.loadMateEntities.Address.FirstOrDefault(a => a.Address_id == route.Start_address_id);
                var endAddress = Conn.loadMateEntities.Address.FirstOrDefault(a => a.Address_id == route.End_address_id);
                txtRoute.Text = $"Маршрут #{route.Route_id}";
            }
        }

        private void LoadDrivers()
        {
            var drivers = Conn.loadMateEntities.Driver
                .Where(d => d.DriverStatus_id == 1)
                .ToList();

            var driversWithDetails = drivers.Select(d => new
            {
                d.Driver_id,
                d.User_id,
                d.License_number,
                d.Experience_years,
                DriverName = GetDriverName(d.User_id),
                DriverStatus = GetDriverStatusName(d.DriverStatus_id)
            }).ToList();

            DriversGrid.ItemsSource = driversWithDetails;
        }

        private string GetDriverName(int userId)
        {
            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == userId);
            return user?.Full_name ?? "Не указан";
        }

        private string GetDriverStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.DriverStatus.FirstOrDefault(ds => ds.DriverStatus_id == statusId);
            return status?.Name ?? "Не указан";
        }

        private void DriversGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = DriversGrid.SelectedItem;
            if (selected != null)
            {
                var property = selected.GetType().GetProperty("Driver_id");
                if (property != null)
                {
                    int driverId = (int)property.GetValue(selected);
                    selectedDriver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == driverId);
                }
            }
        }

        private void Assign_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Выберите водителя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Driver_id == selectedDriver.Driver_id);

            if (truck != null)
            {
                currentOrder.Truck_id = truck.Truck_id;
            }

            currentOrder.OrderStatus_id = 3;
            selectedDriver.DriverStatus_id = 2;

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