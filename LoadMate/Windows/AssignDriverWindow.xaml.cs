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

        public AssignDriverWindow(Order order)
        {
            InitializeComponent();
            _currentOrder = order;
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
            var db = Conn.loadMateEntities;
            var availableDrivers = db.Driver.ToList();

            var driversWithDetails = availableDrivers.Select(d => {
                var user = db.User.FirstOrDefault(u => u.User_id == d.User_id);
                var truck = db.Truck.FirstOrDefault(t => t.Driver_id == d.Driver_id);

                decimal currentLoad = 0;
                if (truck != null)
                {
                    currentLoad = db.Order
                        .Where(o => o.Truck_id == truck.Truck_id && (o.OrderStatus_id == 3 || o.OrderStatus_id == 1))
                        .Select(o => (decimal?)o.Cargo.Weight_kg)
                        .DefaultIfEmpty(0)
                        .Sum() ?? 0;
                }

                return new
                {
                    d.Driver_id,
                    DriverName = user?.Full_name ?? "Неизвестно",
                    Phone = user?.Phone ?? "-",
                    d.License_number,
                    CurrentLoad = $"{currentLoad} кг",
                    DriverStatus = d.DriverStatus_id == 1 ? "Свободен" : "В рейсе"
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
            if (_selectedDriver == null)
            {
                MessageBox.Show("Выберите водителя из списка!");
                return;
            }

            try
            {
                var db = Conn.loadMateEntities;
                var truck = db.Truck.FirstOrDefault(t => t.Driver_id == _selectedDriver.Driver_id);

                if (truck == null)
                {
                    MessageBox.Show("За этим водителем не закреплен транспорт!");
                    return;
                }

                var dbOrder = db.Order.FirstOrDefault(o => o.Order_id == _currentOrder.Order_id);
                if (dbOrder != null)
                {
                    dbOrder.Truck_id = truck.Truck_id;
                    dbOrder.OrderStatus_id = 3; 
                    db.SaveChanges();

                    var orderToEmail = dbOrder;
                    System.Threading.Tasks.Task.Run(() => SendBeautifulEmailToDriver(orderToEmail));

                    MessageBox.Show("Водитель успешно назначен!");
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SendBeautifulEmailToDriver(Order order)
        {
            try
            {
                var db = Conn.loadMateEntities;
                var truck = db.Truck.FirstOrDefault(t => t.Truck_id == order.Truck_id);
                var driver = db.Driver.FirstOrDefault(d => d.Driver_id == truck.Driver_id);
                var user = db.User.FirstOrDefault(u => u.User_id == driver.User_id);
                var cargo = db.Cargo.FirstOrDefault(c => c.Cargo_id == order.Cargo_id);
                var route = db.Route.Include(r => r.Address.Street.City)
                                    .Include(r => r.Address1.Street.City)
                                    .FirstOrDefault(r => r.Route_id == order.Route_id);

                if (string.IsNullOrEmpty(user?.Email)) return;

                string addrFrom = route != null ? $"{route.Address.Street.City.Name}, {route.Address.Street.Name}, д. {route.Address.House_number}" : "Не указан";
                string addrTo = route != null ? $"{route.Address1.Street.City.Name}, {route.Address1.Street.Name}, д. {route.Address1.House_number}" : "Не указан";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                mail.To.Add(user.Email);
                mail.Subject = $"Новый рейс №{order.Order_number}";
                mail.IsBodyHtml = true;

                mail.Body = $@"
                <div style='font-family: sans-serif; background-color: #f8fafc; padding: 20px;'>
                    <div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 8px; overflow: hidden; border: 1px solid #e2e8f0;'>
                        <div style='background: #5cb85c; color: #fff; padding: 20px; text-align: center;'>
                            <h2 style='margin: 0;'>LOADMATE</h2>
                        </div>
                        <div style='padding: 20px;'>
                            <p>Здравствуйте, <b>{user.Full_name}</b>!</p>
                            <p>Вам назначен новый рейс. Детали ниже:</p>
                            <hr style='border: 0; border-top: 1px solid #eee;'/>
                            <p><b>Откуда:</b> {addrFrom}</p>
                            <p><b>Куда:</b> {addrTo}</p>
                            <table style='width: 100%; font-size: 14px;'>
                                <tr><td>Заказ:</td><td align='right'><b>{order.Order_number}</b></td></tr>
                                <tr><td>Груз:</td><td align='right'>{cargo?.Description}</td></tr>
                                <tr><td>Вес:</td><td align='right'>{cargo?.Weight_kg} кг</td></tr>
                                <tr><td>Авто:</td><td align='right'>{truck?.Model}</td></tr>
                            </table>
                        </div>
                        <div style='background: #f1f5f9; padding: 10px; text-align: center; font-size: 12px; color: #64748b;'>
                            © {DateTime.Now.Year} LoadMate System
                        </div>
                    </div>
                </div>";

                using (SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587))
                {
                    smtp.Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "aSC3msYh7oHBrWW0pjxm");
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка почты: " + ex.Message);
            }
        }
    }
}