using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using LoadMate.DBConn;

namespace LoadMate.Windows
{
    public partial class AssignDriverWindow : Window
    {
        private Order _currentOrder;
        private Driver _selectedDriver;
        private int _managerId;
        private int? _oldDriverId;
        private int? _oldTruckId;

        public AssignDriverWindow(Order order, int managerId)
        {
            InitializeComponent();
            _currentOrder = order;
            _managerId = managerId;


            var db = Conn.loadMateEntities;
            var oldOrder = db.Order.FirstOrDefault(o => o.Order_id == _currentOrder.Order_id);
            if (oldOrder != null)
            {
                _oldTruckId = oldOrder.Truck_id;
                if (oldOrder.Truck_id != null)
                {
                    var oldTruck = db.Truck.FirstOrDefault(t => t.Truck_id == oldOrder.Truck_id);
                    if (oldTruck?.Driver_id != null)
                    {
                        _oldDriverId = oldTruck.Driver_id;
                    }
                }
            }

            LoadOrderInfo();
            LoadDrivers();
        }

        private void LoadOrderInfo()
        {
            var db = Conn.loadMateEntities;
            txtOrderNumber.Text = _currentOrder.Order_number;

            var cargo = db.Cargo.FirstOrDefault(c => c.Cargo_id == _currentOrder.Cargo_id);
            if (cargo != null)
            {
                txtCargo.Text = $"{cargo.Description} ({cargo.Weight_kg} кг)";
                var clientUser = db.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
                txtClient.Text = clientUser?.Full_name ?? "Не указан";
            }

            var route = db.Route.Include(r => r.Address.Street.City)
                                .Include(r => r.Address1.Street.City)
                                .FirstOrDefault(r => r.Route_id == _currentOrder.Route_id);

            if (route != null)
            {
                txtRoute.Text = $"{route.Address.Street.City.Name} → {route.Address1.Street.City.Name}";
            }
        }

        private void LoadDrivers()
        {
            try
            {
                var db = Conn.loadMateEntities;

                var availableDrivers = db.Driver
                    .Include(d => d.User)
                    .Include(d => d.Truck)
                    .Where(d => d.DriverStatus_id == 1 && d.Truck.Any())
                    .ToList();

                DriversGrid.ItemsSource = availableDrivers.Select(d => new
                {
                    d.Driver_id,
                    DriverName = d.User?.Full_name ?? "Неизвестно",
                    Phone = d.User?.Phone ?? "-",
                    d.License_number,
                    d.Experience_years,
                    TruckModel = d.Truck.FirstOrDefault()?.Model ?? "Нет авто",
                    DriverStatus = "Свободен"
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки водителей: " + ex.Message);
            }
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
            if (_selectedDriver == null)
            {
                MessageBox.Show("Выберите водителя!");
                return;
            }

            try
            {
                var db = Conn.loadMateEntities;
                var truck = db.Truck.FirstOrDefault(t => t.Driver_id == _selectedDriver.Driver_id);
                var dbOrder = db.Order.FirstOrDefault(o => o.Order_id == _currentOrder.Order_id);

                if (dbOrder != null && truck != null)
                {
                    bool isReassign = _oldTruckId.HasValue && _oldTruckId.Value != truck.Truck_id;
                    int? oldDriverIdForRemoval = isReassign ? _oldDriverId : null;
                    int newDriverId = _selectedDriver.Driver_id;

                    dbOrder.Truck_id = truck.Truck_id;
                    dbOrder.Manager_id = _managerId;
                    dbOrder.OrderStatus_id = 3;
                    db.SaveChanges();

                    int orderId = dbOrder.Order_id;

                    if (isReassign)
                    {
                        if (oldDriverIdForRemoval.HasValue && oldDriverIdForRemoval.Value != newDriverId)
                        {
                            System.Threading.Tasks.Task.Run(() => SendUpdatedRouteListToOldDriver(oldDriverIdForRemoval.Value));
                        }
                        System.Threading.Tasks.Task.Run(() => SendEmailToNewDriver(newDriverId, orderId, true));
                        System.Threading.Tasks.Task.Run(() => SendEmailToClientAboutReassign(orderId));
                    }
                    else
                    {
                        System.Threading.Tasks.Task.Run(() => SendEmailToNewDriver(newDriverId, orderId, false));
                        System.Threading.Tasks.Task.Run(() => SendEmailToClient(orderId));
                    }

                    MessageBox.Show(isReassign ? "Водитель переназначен!" : "Водитель успешно назначен!");
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        private void SendUpdatedRouteListToOldDriver(int oldDriverId)
        {
            try
            {
                using (var db = new LoadMateEntities())
                {
                    var driver = db.Driver.Include(d => d.User).FirstOrDefault(d => d.Driver_id == oldDriverId);
                    if (driver?.User == null || string.IsNullOrEmpty(driver.User.Email)) return;

                    var activeOrders = db.Order
                        .Include(o => o.Cargo)
                        .Include(o => o.Truck)
                        .Include(o => o.Route.Address.Street.City)
                        .Include(o => o.Route.Address1.Street.City)
                        .Where(o => o.Truck.Driver_id == oldDriverId && o.OrderStatus_id == 3)
                        .ToList();

                    string ordersHtml = "";
                    if (activeOrders.Any())
                    {
                        foreach (var o in activeOrders)
                        {
                            ordersHtml += $@"
                                <div style='border: 1px solid #e2e8f0; padding: 15px; margin-bottom: 10px; border-radius: 8px;'>
                                    <p><b>Заказ №{o.Order_number}</b></p>
                                    <p style='font-size: 14px;'><b>Откуда:</b> {FormatAddress(o.Route?.Address)}</p>
                                    <p style='font-size: 14px;'><b>Куда:</b> {FormatAddress(o.Route?.Address1)}</p>
                                    <p style='font-size: 14px;'><b>Груз:</b> {o.Cargo?.Description} ({o.Cargo?.Weight_kg} кг)</p>
                                    <p style='font-size: 14px;'><b>Авто:</b> {o.Truck?.Model} ({o.Truck?.Registration_number})</p>
                                </div>";
                        }
                    }
                    else
                    {
                        ordersHtml = "<p style='color: #64748b;'>У вас нет активных заказов на данный момент.</p>";
                    }

                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                    mail.To.Add(driver.User.Email);
                    mail.Subject = "Заказ снят с вашего маршрута";
                    mail.IsBodyHtml = true;
                    mail.Body = $@"
                        <div style='font-family: sans-serif; color: #334155; padding: 20px; background-color: #f8fafc;'>
                            <div style='max-width: 600px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0;'>
                                <div style='background: #d9534f; padding: 20px; text-align: center; color: #fff;'>
                                    <h2 style='margin:0;'>LOADMATE</h2>
                                    <p style='margin:0; opacity: 0.8;'>Изменение маршрута</p>
                                </div>
                                <div style='padding: 25px;'>
                                    <p>Здравствуйте, <b>{driver.User.Full_name}</b>!</p>
                                    <p>Один из заказов был переназначен другому водителю. Ниже представлен ваш актуальный список рейсов:</p>
                                    {ordersHtml}
                                </div>
                                <div style='background: #f1f5f9; padding: 15px; text-align: center; font-size: 12px; color: #64748b;'>
                                    © {DateTime.Now.Year} LoadMate System
                                </div>
                            </div>
                        </div>";

                    ExecuteSmtpSend(mail);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private void SendEmailToNewDriver(int driverId, int orderId, bool isReassign)
        {
            try
            {
                using (var db = new LoadMateEntities())
                {
                    var driver = db.Driver.Include(d => d.User).FirstOrDefault(d => d.Driver_id == driverId);
                    if (driver?.User == null || string.IsNullOrEmpty(driver.User.Email)) return;

                    var newOrder = db.Order
                        .Include(o => o.Cargo)
                        .Include(o => o.Truck)
                        .Include(o => o.Route.Address.Street.City)
                        .Include(o => o.Route.Address1.Street.City)
                        .FirstOrDefault(o => o.Order_id == orderId);

                    var allActiveOrders = db.Order
                        .Include(o => o.Cargo)
                        .Include(o => o.Truck)
                        .Include(o => o.Route.Address.Street.City)
                        .Include(o => o.Route.Address1.Street.City)
                        .Where(o => o.Truck.Driver_id == driverId && o.OrderStatus_id == 3)
                        .ToList();

                    string ordersHtml = "";
                    foreach (var o in allActiveOrders)
                    {
                        ordersHtml += $@"
                            <div style='border: 1px solid #e2e8f0; padding: 15px; margin-bottom: 10px; border-radius: 8px;'>
                                <p><b>Заказ №{o.Order_number}</b></p>
                                <p style='font-size: 14px;'><b>Откуда:</b> {FormatAddress(o.Route?.Address)}</p>
                                <p style='font-size: 14px;'><b>Куда:</b> {FormatAddress(o.Route?.Address1)}</p>
                                <p style='font-size: 14px;'><b>Груз:</b> {o.Cargo?.Description} ({o.Cargo?.Weight_kg} кг)</p>
                                <p style='font-size: 14px;'><b>Авто:</b> {o.Truck?.Model} ({o.Truck?.Registration_number})</p>
                            </div>";
                    }

                    string subject = isReassign ? $"Вам добавлен новый заказ (переназначение)" : $"Вам назначен новый заказ №{newOrder?.Order_number}";
                    string bodyText = isReassign ? "Вам был переназначен заказ от другого водителя." : "Вам назначен новый заказ.";

                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                    mail.To.Add(driver.User.Email);
                    mail.Subject = subject;
                    mail.IsBodyHtml = true;
                    mail.Body = $@"
                        <div style='font-family: sans-serif; color: #334155; padding: 20px; background-color: #f8fafc;'>
                            <div style='max-width: 600px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0;'>
                                <div style='background: #4CAF50; padding: 20px; text-align: center; color: #fff;'>
                                    <h2 style='margin:0;'>LOADMATE</h2>
                                    <p style='margin:0; opacity: 0.8;'>Новое назначение</p>
                                </div>
                                <div style='padding: 25px;'>
                                    <p>Здравствуйте, <b>{driver.User.Full_name}</b>!</p>
                                    <p>{bodyText}</p>
                                    <p>Актуальный список ваших рейсов:</p>
                                    {ordersHtml}
                                </div>
                                <div style='background: #f1f5f9; padding: 15px; text-align: center; font-size: 12px; color: #64748b;'>
                                    © {DateTime.Now.Year} LoadMate System
                                </div>
                            </div>
                        </div>";

                    ExecuteSmtpSend(mail);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private void SendEmailToClient(int orderId)
        {
            try
            {
                using (var db = new LoadMateEntities())
                {
                    var order = db.Order.Include(o => o.Cargo.User).Include(o => o.Truck.Driver.User).FirstOrDefault(o => o.Order_id == orderId);
                    var client = order?.Cargo?.User;
                    if (client == null || string.IsNullOrEmpty(client.Email)) return;

                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                    mail.To.Add(client.Email);
                    mail.Subject = $"Ваш заказ №{order.Order_number} принят в работу";
                    mail.IsBodyHtml = true;
                    mail.Body = $@"
                        <div style='font-family: sans-serif; background-color: #f1f5f9; padding: 20px;'>
                            <div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden; border: 1px solid #e2e8f0;'>
                                <div style='background: #2196F3; padding: 20px; text-align: center; color: #fff;'>
                                    <h2 style='margin:0;'>LOADMATE</h2>
                                </div>
                                <div style='padding: 20px;'>
                                    <p>Здравствуйте, <b>{client.Full_name}</b>!</p>
                                    <p>На ваш заказ назначен водитель: <b>{order.Truck?.Driver?.User?.Full_name}</b>.</p>
                                    <p>Транспорт: <b>{order.Truck?.Model} ({order.Truck?.Registration_number})</b>.</p>
                                </div>
                                <div style='background: #f8fafc; padding: 15px; text-align: center; font-size: 12px; color: #64748b;'>
                                    © {DateTime.Now.Year} LoadMate System
                                </div>
                            </div>
                        </div>";
                    ExecuteSmtpSend(mail);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private void SendEmailToClientAboutReassign(int orderId)
        {
            try
            {
                using (var db = new LoadMateEntities())
                {
                    var order = db.Order.Include(o => o.Cargo.User).Include(o => o.Truck.Driver.User).FirstOrDefault(o => o.Order_id == orderId);
                    var client = order?.Cargo?.User;
                    if (client == null || string.IsNullOrEmpty(client.Email)) return;

                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                    mail.To.Add(client.Email);
                    mail.Subject = $"Изменение по заказу №{order.Order_number}";
                    mail.IsBodyHtml = true;
                    mail.Body = $@"
                        <div style='font-family: sans-serif; background-color: #f1f5f9; padding: 20px;'>
                            <div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden; border: 1px solid #e2e8f0;'>
                                <div style='background: #f0ad4e; padding: 20px; text-align: center; color: #fff;'>
                                    <h2 style='margin:0;'>LOADMATE</h2>
                                </div>
                                <div style='padding: 20px;'>
                                    <p><b>Здравствуйте, {client.Full_name}!</b></p>
                                    <p>По вашему заказу №{order.Order_number} был назначен новый водитель.</p>
                                    <p>Новый водитель: <b>{order.Truck?.Driver?.User?.Full_name ?? "уточняется"}</b></p>
                                    <p>Транспорт: <b>{order.Truck?.Model} ({order.Truck?.Registration_number})</b></p>
                                    <p>Вы можете отслеживать статус заказа в личном кабинете.</p>
                                </div>
                                <div style='background: #f8fafc; padding: 15px; text-align: center; font-size: 12px; color: #64748b;'>
                                    © {DateTime.Now.Year} LoadMate System
                                </div>
                            </div>
                        </div>";
                    ExecuteSmtpSend(mail);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private string FormatAddress(Address addr)
        {
            if (addr == null) return "Не указан";
            return $"{addr.Street?.City?.Name}, {addr.Street?.Name}, д. {addr.House_number}";
        }

        private void ExecuteSmtpSend(MailMessage mail)
        {
            using (SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587))
            {
                smtp.EnableSsl = true;
                smtp.Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "Lx4Le30IKCEbP0FjD7lM");
                smtp.Send(mail);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}