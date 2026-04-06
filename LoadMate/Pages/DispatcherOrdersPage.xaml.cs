using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
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

        private void SendEmailToDriver(Order order)
        {
            try
            {
                var db = Conn.loadMateEntities;
                var truck = db.Truck.FirstOrDefault(t => t.Truck_id == order.Truck_id);
                if (truck == null || !truck.Driver_id.HasValue) return;

                var driver = db.Driver.FirstOrDefault(d => d.Driver_id == truck.Driver_id);
                if (driver == null) return;

                var user = db.User.FirstOrDefault(u => u.User_id == driver.User_id);
                if (string.IsNullOrEmpty(user?.Email)) return;

                string from = GetRouteAddress(order.Route_id, true);
                string to = GetRouteAddress(order.Route_id, false);

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate: Новый заказ");
                mail.To.Add(user.Email);
                mail.Subject = $"Назначен новый заказ №{order.Order_number}";
                mail.Body = $"Здравствуйте, {user.Full_name}!\n\n" +
                            $"Вам назначен новый заказ:\n" +
                            $"Номер: {order.Order_number}\n" +
                            $"Откуда: {from}\n" +
                            $"Куда: {to}\n" +
                            $"Дата забора: {order.Scheduled_pickup:dd.MM.yyyy}\n" +
                            $"Транспорт: {truck.Model} ({truck.Registration_number})";

                SmtpClient client = new SmtpClient("smtp.mail.ru", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "Bmz8qEIDckirBNY5cEmE"),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };
                client.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadOrders();

        private void ChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null) return;
            var statusWindow = new ChangeStatusWindow(selectedOrder.OrderStatus_id);
            statusWindow.Owner = Application.Current.MainWindow;
            if (statusWindow.ShowDialog() == true)
            {
                selectedOrder.OrderStatus_id = statusWindow.SelectedStatusId;
                Conn.loadMateEntities.SaveChanges();
                LoadOrders();
            }
        }

        private void AssignTruck_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null) return;
            var truckWindow = new AssignTruckWindow(selectedOrder);
            truckWindow.Owner = Application.Current.MainWindow;
            if (truckWindow.ShowDialog() == true)
            {
                Conn.loadMateEntities.SaveChanges();
                SendEmailToDriver(selectedOrder);
                LoadOrders();
                MessageBox.Show("Транспорт назначен, водителю отправлено уведомление.");
            }
        }

        private void AssignDriver_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null) return;
            var driverWindow = new AssignDriverWindow(selectedOrder);
            driverWindow.Owner = Application.Current.MainWindow;
            if (driverWindow.ShowDialog() == true)
            {
                Conn.loadMateEntities.SaveChanges();
                LoadOrders();
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void ApplyFilters()
        {
            string search = txtSearch.Text.Trim();
            var orders = Conn.loadMateEntities.Order.ToList();

            if (!string.IsNullOrEmpty(search))
                orders = orders.Where(o => o.Order_number.Contains(search)).ToList();

            if (cmbStatusFilter.SelectedItem is ComboBoxItem selected && selected.Content.ToString() != "Все")
            {
                string name = selected.Content.ToString();
                orders = orders.Where(o => o.OrderStatus.Name == name).ToList();
            }

            var details = orders.Select(o => new {
                o.Order_id,
                o.Order_number,
                o.Price,
                o.Order_date,
                ClientName = GetClientName(o.Cargo_id),
                CargoDescription = GetCargoDescription(o.Cargo_id),
                RouteFrom = GetRouteAddress(o.Route_id, true),
                RouteTo = GetRouteAddress(o.Route_id, false),
                DriverName = GetDriverName(o.Truck_id),
                TruckModel = GetTruckModel(o.Truck_id),
                StatusName = GetOrderStatusName(o.OrderStatus_id)
            }).ToList();
            OrdersGrid.ItemsSource = details;
        }
    }
}