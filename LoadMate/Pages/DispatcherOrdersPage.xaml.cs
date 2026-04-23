using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LoadMate.DBConn;
using LoadMate.Windows;

namespace LoadMate.Pages
{
    public partial class DispatcherOrdersPage : Page
    {
        private int dispatcherId;
        private Order selectedOrder;

        public DispatcherOrdersPage(int dispatcherId)
        {
            InitializeComponent();
            this.dispatcherId = dispatcherId;
            LoadStatusFilter();
            LoadOrders();
        }

        private void LoadStatusFilter()
        {
            var statuses = Conn.loadMateEntities.OrderStatus.ToList();
            statuses.Insert(0, new OrderStatus { OrderStatus_id = 0, Name = "Все" });
            cmbStatusFilter.ItemsSource = statuses;
            cmbStatusFilter.SelectedValuePath = "OrderStatus_id";
            cmbStatusFilter.SelectedIndex = 0;
        }

        private void LoadOrders()
        {
            ApplyFilters();
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

        private string GetDriverName(int? truckId)
        {
            if (truckId == null || truckId == 0) return "Не назначен";
            var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
            if (truck == null || !truck.Driver_id.HasValue) return "Не назначен";

            var driver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == truck.Driver_id);
            if (driver == null) return "Не назначен";

            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == driver.User_id);
            return user != null ? user.Full_name : "Не назначен";
        }

        private string GetTruckModel(int? truckId)
        {
            if (truckId == null || truckId == 0) return "Не назначен";
            var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
            return truck != null ? $"{truck.Model} ({truck.Capacity_kg} кг)" : "Не назначен";
        }

        private string GetOrderStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.OrderStatus.FirstOrDefault(s => s.OrderStatus_id == statusId);
            return status != null ? status.Name : "Не указан";
        }

        private void SendEmailToDriver(Order order)
        {
            try
            {
                var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == order.Truck_id);
                if (truck == null || !truck.Driver_id.HasValue) return;

                var driver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == truck.Driver_id);
                if (driver == null) return;

                var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == driver.User_id);
                if (string.IsNullOrEmpty(user?.Email)) return;

                string fromAddress = GetRouteAddress(order.Route_id, true);
                string toAddress = GetRouteAddress(order.Route_id, false);

                System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
                mail.From = new System.Net.Mail.MailAddress("miftakhova_ev@mail.ru", "LoadMate: Новый заказ");
                mail.To.Add(user.Email);
                mail.Subject = $"Назначен новый заказ №{order.Order_number}";
                mail.Body = $"Здравствуйте, {user.Full_name}!\n\n" +
                            $"Вам назначен новый заказ:\n" +
                            $"Номер: {order.Order_number}\n" +
                            $"Откуда: {fromAddress}\n" +
                            $"Куда: {toAddress}\n" +
                            $"Дата забора: {order.Scheduled_pickup:dd.MM.yyyy}\n" +
                            $"Транспорт: {truck.Model} (Г/П: {truck.Capacity_kg} кг, Гос.номер: {truck.Registration_number})";

                System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient("smtp.mail.ru", 587)
                {
                    EnableSsl = true,
                    Credentials = new System.Net.NetworkCredential("miftakhova_ev@mail.ru", "Bmz8qEIDckirBNY5cEmE"),
                    DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };
                client.Send(mail);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при отправке уведомления водителю: " + ex.Message);
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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void ChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ!");
                return;
            }

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
            if (selectedOrder == null)
            {
                MessageBox.Show("Пожалуйста, выберите заказ из списка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var truckWindow = new AssignTransportWindow(selectedOrder, this.dispatcherId);
            truckWindow.Owner = Application.Current.MainWindow;

            if (truckWindow.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        private void AssignDriver_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null)
            {
                MessageBox.Show("Пожалуйста, выберите заказ из списка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var driverWindow = new AssignDriverWindow(selectedOrder);
            driverWindow.Owner = Application.Current.MainWindow;

            if (driverWindow.ShowDialog() == true)
            {
                LoadOrders();
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
            string search = txtSearch.Text.Trim().ToLower();
            int selectedStatusId = 0;
            if (cmbStatusFilter.SelectedValue != null)
            {
                selectedStatusId = (int)cmbStatusFilter.SelectedValue;
            }

            var ordersQuery = Conn.loadMateEntities.Order.ToList();

            var filteredOrders = ordersQuery.Where(o => {
                bool matchesSearch = string.IsNullOrEmpty(search) ||
                                     o.Order_number.ToLower().Contains(search);

                bool matchesStatus = selectedStatusId == 0 ||
                                     o.OrderStatus_id == selectedStatusId;

                return matchesSearch && matchesStatus;
            }).ToList();

            var details = filteredOrders.Select(o => new
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

            OrdersGrid.ItemsSource = details;
        }
    }
}