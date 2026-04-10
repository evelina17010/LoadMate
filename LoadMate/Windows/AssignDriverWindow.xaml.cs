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
            txtOrderNumber.Text = _currentOrder.Order_number;
            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == _currentOrder.Cargo_id);
            if (cargo != null)
            {
                var client = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
                txtClient.Text = client?.Full_name ?? "Не указан";
                txtCargo.Text = $"{cargo.Description} ({cargo.Weight_kg} кг)";
            }
            var route = Conn.loadMateEntities.Route.FirstOrDefault(r => r.Route_id == _currentOrder.Route_id);
            if (route != null)
            {
                txtRoute.Text = $"{GetCityName(route.Start_address_id)} -> {GetCityName(route.End_address_id)}";
            }
        }

        private string GetCityName(int addressId)
        {
            var address = Conn.loadMateEntities.Address
                .Include(a => a.Street.City)
                .FirstOrDefault(a => a.Address_id == addressId);
            return address?.Street?.City?.Name ?? "Неизвестно";
        }

        private void LoadDrivers()
        {
            var availableDrivers = Conn.loadMateEntities.Driver
                .Where(d => d.DriverStatus_id == 1 || d.DriverStatus_id == 2)
                .ToList();

            var driversWithDetails = availableDrivers.Select(d => {
                var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == d.User_id);
                var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Driver_id == d.Driver_id);

                double currentLoad = 0;
                if (truck != null)
                {
                    currentLoad = (double)(Conn.loadMateEntities.Order
                        .Where(o => o.Truck_id == truck.Truck_id && (o.OrderStatus_id == 3 || o.OrderStatus_id == 1))
                        .Sum(o => (decimal?)o.Cargo.Weight_kg) ?? 0);
                }

                return new
                {
                    d.Driver_id,
                    DriverName = user?.Full_name ?? "Неизвестно",
                    Phone = user?.Phone ?? "-",
                    d.License_number,
                    Experience_years = $"Загрузка: {currentLoad} кг",
                    DriverStatus = d.DriverStatus_id == 1 ? "Свободен" : "В рейсе (Попутно)"
                };
            }).ToList();

            DriversGrid.ItemsSource = driversWithDetails;
        }

        private void DriversGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DriversGrid.SelectedItem == null) return;
            dynamic selected = DriversGrid.SelectedItem;
            int driverId = selected.Driver_id;
            _selectedDriver = Conn.loadMateEntities.Driver
                .Include(u => u.User)
                .FirstOrDefault(d => d.Driver_id == driverId);
        }

        private void Assign_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedDriver == null)
                {
                    MessageBox.Show("Пожалуйста, выберите водителя.");
                    return;
                }

                var db = Conn.loadMateEntities;
                var truck = db.Truck.FirstOrDefault(t => t.Driver_id == _selectedDriver.Driver_id);

                if (truck == null)
                {
                    MessageBox.Show("За выбранным водителем не закреплено транспортное средство в базе данных.");
                    return;
                }

                var orderToUpdate = db.Order.FirstOrDefault(o => o.Order_id == _currentOrder.Order_id);
                if (orderToUpdate != null)
                {
                    orderToUpdate.Truck_id = truck.Truck_id;
                    orderToUpdate.OrderStatus_id = 3;
                    var driverToUpdate = db.Driver.FirstOrDefault(d => d.Driver_id == _selectedDriver.Driver_id);
                    if (driverToUpdate != null) driverToUpdate.DriverStatus_id = 2;

                    db.SaveChanges();
                    SendBeautifulEmailToDriver(orderToUpdate, truck, _selectedDriver.User);

                    MessageBox.Show($"Заказ успешно назначен водителю {_selectedDriver.User.Full_name}");
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при назначении: {ex.Message}");
            }
        }

        private void SendBeautifulEmailToDriver(Order order, Truck truck, User user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.Email)) return;

                var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == order.Cargo_id);
                var route = Conn.loadMateEntities.Route.FirstOrDefault(r => r.Route_id == order.Route_id);

                string fromAddr = GetCityName(route?.Start_address_id ?? 0);
                string toAddr = GetCityName(route?.End_address_id ?? 0);

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                mail.To.Add(user.Email);
                mail.Subject = $"Назначен новый рейс №{order.Order_number}";
                mail.IsBodyHtml = true;

                mail.Body = $@"
                <div style='font-family: Arial, sans-serif; background-color: #f8fafc; padding: 25px;'>
                    <div style='max-width: 550px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0;'>
                        <div style='background: #1e293b; padding: 25px; text-align: center;'>
                            <h1 style='color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 1px;'>LOADMATE</h1>
                        </div>
                        <div style='padding: 25px;'>
                            <p style='font-size: 16px; color: #1e293b;'><b>Здравствуйте, {user.Full_name}!</b></p>
                            <p style='color: #475569;'>Вам назначен новый рейс в системе мониторинга.</p>
                            
                            <div style='background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                                <p style='margin: 5px 0;'><b>Маршрут:</b> {fromAddr} — {toAddr}</p>
                                <p style='margin: 5px 0;'><b>Груз:</b> {cargo?.Description ?? "Не указан"}</p>
                                <p style='margin: 5px 0;'><b>Вес:</b> {cargo?.Weight_kg} кг</p>
                            </div>

                            <table style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                                <tr>
                                    <td style='padding: 8px 0; color: #64748b; border-bottom: 1px solid #f1f5f9;'>Номер заказа:</td>
                                    <td style='padding: 8px 0; text-align: right; font-weight: 600;'>{order.Order_number}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px 0; color: #64748b; border-bottom: 1px solid #f1f5f9;'>Автомобиль:</td>
                                    <td style='padding: 8px 0; text-align: right; font-weight: 600;'>{truck.Model}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px 0; color: #64748b;'>Гос. номер:</td>
                                    <td style='padding: 8px 0; text-align: right; font-weight: 600;'>{truck.Registration_number}</td>
                                </tr>
                            </table>
                        </div>
                        <div style='background: #f8fafc; padding: 15px; text-align: center; border-top: 1px solid #f1f5f9;'>
                            <p style='margin: 0; font-size: 12px; color: #94a3b8;'>© {DateTime.Now.Year} LoadMate Logistics Team</p>
                        </div>
                    </div>
                </div>";

                using (SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587))
                {
                    smtp.Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "Bmz8qEIDckirBNY5cEmE");
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка отправки письма: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}