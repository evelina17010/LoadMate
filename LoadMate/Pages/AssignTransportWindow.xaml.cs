using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using LoadMate.DBConn;
using LoadMate.Services;

namespace LoadMate.Pages
{
    public partial class AssignTransportWindow : Window
    {
        private Order _currentOrder;
        private int? _selectedTruckId;
        private int _managerId;
        private SmartLogisticsService _smartService = new SmartLogisticsService();

        public AssignTransportWindow(Order order, int managerId)
        {
            InitializeComponent();
            _currentOrder = order;
            _managerId = managerId;
            LoadOrderInfo();
            LoadTrucks();
        }

        private void LoadOrderInfo()
        {
            var db = Conn.loadMateEntities;
            var cargo = db.Cargo.FirstOrDefault(c => c.Cargo_id == _currentOrder.Cargo_id);
            txtOrderNumber.Text = _currentOrder.Order_number;
            if (cargo != null)
            {
                txtWeight.Text = $"{cargo.Weight_kg} кг";
                txtVolume.Text = $"{cargo.Volume_m3} м³";
                var clientUser = db.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
                txtClient.Text = clientUser?.Full_name ?? "Не указан";
            }
        }

        private void LoadTrucks()
        {
            try
            {
                var db = Conn.loadMateEntities;

                var allTrucks = db.Truck
                    .Include(t => t.TruckStatus)
                    .Include(t => t.Driver.User)
                    .Include(t => t.Order.Select(o => o.Cargo))
                    .Include(t => t.Order.Select(o => o.Route.Address1.Street.City))
                    .ToList();

                var currentCargo = db.Cargo
                    .Include(c => c.Order.Select(o => o.Route.Address1.Street.City))
                    .FirstOrDefault(c => c.Cargo_id == _currentOrder.Cargo_id);

                List<Truck> filteredTrucks;

                if (chkSmartFilter.IsChecked == true && currentCargo != null)
                {
                    filteredTrucks = _smartService.GetAvailableTrucksForCargo(currentCargo, allTrucks);
                }
                else
                {
                    filteredTrucks = allTrucks.Where(t => t.TruckStatus_id == 1).ToList();
                }
                TrucksGrid.ItemsSource = filteredTrucks.Select(t => new {
                    t.Truck_id,
                    t.Model,
                    t.Registration_number,
                    FreeWeight = t.FreeWeight_kg.ToString("N0"),
                    FreeVolume = t.FreeVolume_m3.ToString("N1"),
                    DriverName = t.Driver?.User?.Full_name ?? "Не назначен",
                    StatusName = t.TruckStatus?.Name ?? "Неизвестно"
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки транспорта: " + ex.Message);
            }
        }

        private void TrucksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrucksGrid.SelectedItem == null) return;
            dynamic selected = TrucksGrid.SelectedItem;
            _selectedTruckId = selected.Truck_id;
        }

        private void Assign_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedTruckId.HasValue)
            {
                MessageBox.Show("Выберите транспорт!");
                return;
            }
            try
            {
                var db = Conn.loadMateEntities;
                var truck = db.Truck.FirstOrDefault(t => t.Truck_id == _selectedTruckId);
                var dbOrder = db.Order.FirstOrDefault(o => o.Order_id == _currentOrder.Order_id);

                if (truck != null && dbOrder != null)
                {
                    if (!truck.Driver_id.HasValue)
                    {
                        MessageBox.Show("У транспорта нет водителя!");
                        return;
                    }
                    dbOrder.Truck_id = truck.Truck_id;
                    dbOrder.Manager_id = _managerId;
                    dbOrder.OrderStatus_id = 3;
                    db.SaveChanges();

                    int driverId = truck.Driver_id.Value;
                    int currentOrderId = dbOrder.Order_id;

                    System.Threading.Tasks.Task.Run(() => SendEmailToDriver(driverId));
                    System.Threading.Tasks.Task.Run(() => SendEmailToClient(currentOrderId));

                    MessageBox.Show("Транспорт назначен!");
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        private void SendEmailToDriver(int driverId)
        {
            try
            {
                using (var db = new LoadMateEntities())
                {
                    var driver = db.Driver.Include(d => d.User).FirstOrDefault(d => d.Driver_id == driverId);
                    if (driver?.User == null || string.IsNullOrEmpty(driver.User.Email)) return;

                    var activeOrders = db.Order
                        .Include(o => o.Cargo)
                        .Include(o => o.Truck)
                        .Include(o => o.Route.Address.Street.City)
                        .Include(o => o.Route.Address1.Street.City)
                        .Where(o => o.Truck.Driver_id == driverId && o.OrderStatus_id == 3)
                        .ToList();

                    string ordersHtml = "";
                    foreach (var o in activeOrders)
                    {
                        ordersHtml += $@"
                            <div style='border: 1px solid #e2e8f0; padding: 15px; margin-bottom: 10px; border-radius: 8px;'>
                                <p><b>Заказ №{o.Order_number}</b></p>
                                <p style='font-size: 14px;'><b>Откуда:</b> {FormatAddress(o.Route?.Address)}</p>
                                <p style='font-size: 14px;'><b>Куда:</b> {FormatAddress(o.Route?.Address1)}</p>
                                <p style='font-size: 14px;'><b>Дата забора:</b> {o.Scheduled_pickup:dd.MM.yyyy}</p>
                                <p style='font-size: 14px;'><b>Дата доставки:</b> {o.Scheduled_delivery:dd.MM.yyyy}</p>
                                <p style='font-size: 14px;'><b>Груз:</b> {o.Cargo?.Description} ({o.Cargo?.Weight_kg} кг, {o.Cargo?.Volume_m3} м³)</p>
                                <p style='font-size: 14px;'><b>Авто:</b> {o.Truck?.Model} ({o.Truck?.Registration_number})</p>
                            </div>";
                    }

                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                    mail.To.Add(driver.User.Email);
                    mail.Subject = "Актуальный список назначенных заказов";
                    mail.IsBodyHtml = true;
                    mail.Body = $@"
                        <div style='font-family: sans-serif; color: #334155; padding: 20px; background-color: #f8fafc;'>
                            <div style='max-width: 600px; margin: 0 auto; background: #fff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden;'>
                                <div style='background: #4CAF50; padding: 20px; text-align: center; color: #fff;'>
                                    <h2 style='margin:0;'>LOADMATE</h2>
                                    <p style='margin:0; opacity: 0.8;'>Список текущих задач</p>
                                </div>
                                <div style='padding: 25px;'>
                                    <p>Здравствуйте, <b>{driver.User.Full_name}</b>!</p>
                                    <p>Ниже представлен перечень ваших активных заказов:</p>
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

        private string FormatAddress(Address addr)
        {
            if (addr == null) return "Не указан";
            string city = addr.Street?.City?.Name ?? "г. Неизвестен";
            string street = addr.Street?.Name ?? "ул. Неизвестна";
            string house = addr.House_number ?? "";
            return $"{city}, {street}, д. {house}";
        }

        private void SendEmailToClient(int orderId)
        {
            try
            {
                using (var db = new LoadMateEntities())
                {
                    var order = db.Order.Include(o => o.Cargo.User).FirstOrDefault(o => o.Order_id == orderId);
                    var client = order?.Cargo?.User;
                    if (client == null || string.IsNullOrEmpty(client.Email)) return;

                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                    mail.To.Add(client.Email);
                    mail.Subject = $"Заказ №{order.Order_number} обработан";
                    mail.IsBodyHtml = true;
                    mail.Body = $@"
                        <div style='font-family: sans-serif; background-color: #f1f5f9; padding: 20px;'>
                            <div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden; border: 1px solid #e2e8f0;'>
                                <div style='background: #2196F3; padding: 20px; text-align: center; color: #fff;'>
                                    <h2 style='margin:0;'>LOADMATE</h2>
                                </div>
                                <div style='padding: 20px;'>
                                    <p><b>Здравствуйте, {client.Full_name}!</b></p>
                                    <p>Ваш заказ №{order.Order_number} успешно принят в работу.</p>
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

        private void ExecuteSmtpSend(MailMessage mail)
        {
            using (SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587))
            {
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "rGEil7HXFW2suOFKVjvs");
                smtp.Send(mail);
            }
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e) => LoadTrucks();
        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}