using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LoadMate.DBConn;

namespace LoadMate.Pages
{
    public partial class ClientOrdersPage : Page
    {
        private int clientId;

        public ClientOrdersPage(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;
            LoadOrders();
        }

        private void LoadOrders()
        {
            var cargoIds = Conn.loadMateEntities.Cargo
                .Where(c => c.Client_id == clientId)
                .Select(c => c.Cargo_id)
                .ToList();

            var orders = Conn.loadMateEntities.Order
                .Where(o => cargoIds.Contains(o.Cargo_id))
                .ToList();

            var ordersWithDetails = orders.Select(o => new
            {
                o.Order_id,
                o.Order_number,
                o.Price,
                o.Order_date,
                o.Scheduled_pickup,
                o.Scheduled_delivery,
                CargoDescription = GetCargoDescription(o.Cargo_id),
                RouteFrom = GetRouteAddress(o.Route_id, true),
                RouteTo = GetRouteAddress(o.Route_id, false),
                StatusName = GetOrderStatusName(o.OrderStatus_id)
            }).ToList();

            OrdersGrid.ItemsSource = ordersWithDetails;
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

        private string GetOrderStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.OrderStatus.FirstOrDefault(s => s.OrderStatus_id == statusId);
            return status != null ? status.Name : "Не указан";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtSearch.Text.Trim();

            var cargoIds = Conn.loadMateEntities.Cargo
                .Where(c => c.Client_id == clientId)
                .Select(c => c.Cargo_id)
                .ToList();

            var orders = Conn.loadMateEntities.Order
                .Where(o => cargoIds.Contains(o.Cargo_id))
                .ToList();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o => o.Order_number.Contains(search)).ToList();
            }

            var ordersWithDetails = orders.Select(o => new
            {
                o.Order_id,
                o.Order_number,
                o.Price,
                o.Order_date,
                o.Scheduled_pickup,
                o.Scheduled_delivery,
                CargoDescription = GetCargoDescription(o.Cargo_id),
                RouteFrom = GetRouteAddress(o.Route_id, true),
                RouteTo = GetRouteAddress(o.Route_id, false),
                StatusName = GetOrderStatusName(o.OrderStatus_id)
            }).ToList();

            OrdersGrid.ItemsSource = ordersWithDetails;
        }
    }
}