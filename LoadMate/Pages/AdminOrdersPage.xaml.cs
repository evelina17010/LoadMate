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

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для AdminOrdersPage.xaml
    /// </summary>
    public partial class AdminOrdersPage : Page
    {
        private Order selectedOrder;

        public AdminOrdersPage()
        {
            InitializeComponent();
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
                StatusName = GetOrderStatusName(o.OrderStatus_id),
                StatusId = o.OrderStatus_id
            }).ToList();

            OrdersGrid.ItemsSource = ordersWithDetails;
        }

        private string GetClientName(int cargoId)
        {
            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == cargoId);
            if (cargo == null) return "Не указан";
            var client = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
            return client != null ? client.Full_name : "Не указан";
        }

        private string GetCargoDescription(int cargoId)
        {
            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == cargoId);
            return cargo != null ? cargo.Description : "Не указан";
        }

        private string GetRouteAddress(int routeId, bool isStart)
        {
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

        private string GetDriverName(int truckId)
        {
            var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
            if (truck == null || !truck.Driver_id.HasValue) return "Не назначен";
            var driver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == truck.Driver_id);
            if (driver == null) return "Не назначен";
            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == driver.User_id);
            return user != null ? user.Full_name : "Не назначен";
        }

        private string GetTruckModel(int truckId)
        {
            var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
            return truck != null ? truck.Model : "Не назначен";
        }

        private string GetOrderStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.OrderStatus.FirstOrDefault(s => s.OrderStatus_id == statusId);
            return status != null ? status.Name : "Не указан";
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

        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Удалить заказ №{selectedOrder.Order_number}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var payments = Conn.loadMateEntities.Payment.Where(p => p.Order_id == selectedOrder.Order_id).ToList();
                foreach (var payment in payments)
                {
                    Conn.loadMateEntities.Payment.Remove(payment);
                }

                Conn.loadMateEntities.Order.Remove(selectedOrder);
                Conn.loadMateEntities.SaveChanges();
                LoadOrders();
                MessageBox.Show("Заказ удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
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
                StatusName = GetOrderStatusName(o.OrderStatus_id),
                StatusId = o.OrderStatus_id
            }).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                ordersWithDetails = ordersWithDetails.Where(o =>
                    o.Order_number.Contains(search)).ToList();
            }

            if (cmbStatusFilter.SelectedItem is ComboBoxItem selected && selected.Content.ToString() != "Все статусы")
            {
                string statusName = selected.Content.ToString();
                var status = Conn.loadMateEntities.OrderStatus.FirstOrDefault(s => s.Name == statusName);
                if (status != null)
                {
                    ordersWithDetails = ordersWithDetails.Where(o => o.StatusId == status.OrderStatus_id).ToList();
                }
            }

            OrdersGrid.ItemsSource = ordersWithDetails;
        }
    }
}
