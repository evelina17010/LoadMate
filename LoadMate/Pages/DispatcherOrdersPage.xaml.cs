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
using System.Windows.Navigation;
using System.Windows.Shapes;
using LoadMate.DBConn;
using LoadMate.Windows;

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для DispatcherOrdersPage.xaml
    /// </summary>
    public partial class DispatcherOrdersPage : Page
    {
        private int dispatcherId;
        private Order selectedOrder;

        public DispatcherOrdersPage(int dispatcherId)
        {
            InitializeComponent();
            this.dispatcherId = dispatcherId;
            LoadOrders();
        }

        private void LoadOrders()
        {
            var orders = Conn.loadMateEntities.Order.ToList();

            var ordersWithDetails = orders.Select(o => new
            {
                o.Order_id,
                o.Order_number,
                o.Price,
                o.Order_date,
                o.Scheduled_pickup,
                o.Scheduled_delivery,
                ClientName = GetClientName(o.Cargo_id),
                CargoDescription = GetCargoDescription(o.Cargo_id),
                RouteFrom = GetRouteAddress(o.Route_id, true),
                RouteTo = GetRouteAddress(o.Route_id, false),
                DriverName = GetDriverName(o.Truck_id),
                TruckModel = GetTruckModel(o.Truck_id),
                StatusName = GetOrderStatusName(o.OrderStatus_id)
            }).ToList();

            OrdersGrid.ItemsSource = ordersWithDetails;
        }

        private string GetClientName(int? cargoId)
        {
            if (!cargoId.HasValue) return "Не указан";
            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == cargoId);
            if (cargo == null) return "Не указан";
            var client = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
            return client?.Full_name ?? "Не указан";
        }

        private string GetCargoDescription(int? cargoId)
        {
            if (!cargoId.HasValue) return "Не указан";
            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == cargoId);
            return cargo?.Description ?? "Не указан";
        }

        private string GetRouteAddress(int? routeId, bool isStart)
        {
            if (!routeId.HasValue) return "Не указан";
            var route = Conn.loadMateEntities.Route.FirstOrDefault(r => r.Route_id == routeId);
            if (route == null) return "Не указан";

            int addressId = isStart ? route.Start_address_id : route.End_address_id;
            var address = Conn.loadMateEntities.Address.FirstOrDefault(a => a.Address_id == addressId);
            if (address == null) return "Не указан";

            var street = Conn.loadMateEntities.Street.FirstOrDefault(s => s.Street_id == address.Street_id);
            if (street == null) return address.House_number;

            var city = Conn.loadMateEntities.City.FirstOrDefault(c => c.City_id == street.City_id);
            if (city == null) return $"{street.Name}, {address.House_number}";

            return $"{city.Name}, {street.Name}, {address.House_number}";
        }

        private string GetDriverName(int? truckId)
        {
            if (!truckId.HasValue) return "Не назначен";
            var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
            if (truck == null || !truck.Driver_id.HasValue) return "Не назначен";
            var driver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == truck.Driver_id);
            if (driver == null) return "Не назначен";
            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == driver.User_id);
            return user?.Full_name ?? "Не назначен";
        }

        private string GetTruckModel(int? truckId)
        {
            if (!truckId.HasValue) return "Не назначен";
            var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
            return truck?.Model ?? "Не назначен";
        }

        private string GetOrderStatusName(int? statusId)
        {
            if (!statusId.HasValue) return "Не указан";
            var status = Conn.loadMateEntities.OrderStatus.FirstOrDefault(s => s.OrderStatus_id == statusId);
            return status?.Name ?? "Не указан";
        }

        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = OrdersGrid.SelectedItem;
            if (selected != null)
            {
                var property = selected.GetType().GetProperty("Order_id");
                if (property != null)
                {
                    int orderId = (int)property.GetValue(selected);
                    selectedOrder = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == orderId);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void ChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int currentStatusId = selectedOrder.OrderStatus_id;
            var statusWindow = new ChangeStatusWindow(currentStatusId);
            statusWindow.Owner = Application.Current.MainWindow;
            if (statusWindow.ShowDialog() == true)
            {
                selectedOrder.OrderStatus_id = statusWindow.SelectedStatusId;
                Conn.loadMateEntities.SaveChanges();
                LoadOrders();
                MessageBox.Show("Статус изменен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AssignDriver_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var driverWindow = new AssignDriverWindow(selectedOrder);
            driverWindow.Owner = Application.Current.MainWindow;
            if (driverWindow.ShowDialog() == true)
            {
                Conn.loadMateEntities.SaveChanges();
                LoadOrders();
                MessageBox.Show("Водитель назначен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AssignTruck_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var truckWindow = new AssignTruckWindow(selectedOrder);
            truckWindow.Owner = Application.Current.MainWindow;
            if (truckWindow.ShowDialog() == true)
            {
                Conn.loadMateEntities.SaveChanges();
                LoadOrders();
                MessageBox.Show("Транспорт назначен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string search = txtSearch.Text.Trim();
            var orders = Conn.loadMateEntities.Order.ToList();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o => o.Order_number.Contains(search)).ToList();
            }

            if (cmbStatusFilter.SelectedItem is ComboBoxItem selected && selected.Content.ToString() != "Все")
            {
                string statusName = selected.Content.ToString();
                var status = Conn.loadMateEntities.OrderStatus.FirstOrDefault(s => s.Name == statusName);
                if (status != null)
                {
                    orders = orders.Where(o => o.OrderStatus_id == status.OrderStatus_id).ToList();
                }
            }

            var ordersWithDetails = orders.Select(o => new
            {
                o.Order_id,
                o.Order_number,
                o.Price,
                o.Order_date,
                o.Scheduled_pickup,
                o.Scheduled_delivery,
                ClientName = GetClientName(o.Cargo_id),
                CargoDescription = GetCargoDescription(o.Cargo_id),
                RouteFrom = GetRouteAddress(o.Route_id, true),
                RouteTo = GetRouteAddress(o.Route_id, false),
                DriverName = GetDriverName(o.Truck_id),
                TruckModel = GetTruckModel(o.Truck_id),
                StatusName = GetOrderStatusName(o.OrderStatus_id)
            }).ToList();

            OrdersGrid.ItemsSource = ordersWithDetails;
        }
    }
}