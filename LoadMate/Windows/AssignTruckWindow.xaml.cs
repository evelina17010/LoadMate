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
using System.Windows.Shapes;
using LoadMate.DBConn;
using System.Data.Entity;

namespace LoadMate.Windows
{
    /// <summary>
    /// Логика взаимодействия для AssignTruckWindow.xaml
    /// </summary>
    public partial class AssignTruckWindow : Window
    {
        private Order _currentOrder;
        private Truck _selectedTruck;

        public AssignTruckWindow(Order order)
        {
            InitializeComponent();
            _currentOrder = order;
            LoadOrderInfo();
            LoadAvailableTrucks();
        }

        private void LoadOrderInfo()
        {
            txtOrderNumber.Text = _currentOrder.Order_number;
            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == _currentOrder.Cargo_id);
            if (cargo != null)
            {
                txtWeight.Text = $"{cargo.Weight_kg} кг";
            }
        }

        private void LoadAvailableTrucks()
        {
            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == _currentOrder.Cargo_id);
            decimal cargoWeight = cargo?.Weight_kg ?? 0;

            // Фильтр: статус "Свободен" (1) и грузоподъемность подходит под вес груза
            var availableTrucks = Conn.loadMateEntities.Truck
                .Where(t => t.TruckStatus_id == 1 && t.Capacity_kg >= cargoWeight)
                .ToList();

            TrucksGrid.ItemsSource = availableTrucks.Select(t => new {
                t.Truck_id,
                t.Model,
                t.Registration_number,
                t.Capacity_kg,
                DriverName = GetDriverName(t.Driver_id)
            }).ToList();
        }

        private string GetDriverName(int? driverId)
        {
            if (driverId == null) return "Не привязан";
            var driver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == driverId);
            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == driver.User_id);
            return user?.Full_name ?? "Неизвестно";
        }

        private void Assign_Click(object sender, RoutedEventArgs e)
        {
            if (TrucksGrid.SelectedItem == null) return;

            dynamic selected = TrucksGrid.SelectedItem;
            int truckId = selected.Truck_id;
            _selectedTruck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);

            if (_selectedTruck != null)
            {
                var dbOrder = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == _currentOrder.Order_id);
                dbOrder.Truck_id = _selectedTruck.Truck_id;
                dbOrder.OrderStatus_id = 3; // Статус "В процессе/Назначен"

                Conn.loadMateEntities.SaveChanges();

                // После назначения отправляем красивое письмо водителю
                SendBeautifulEmailToDriver(dbOrder);

                DialogResult = true;
                Close();
            }
        }

        private void SendBeautifulEmailToDriver(Order order)
        {
            try
            {
                var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == order.Truck_id);
                var driver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == truck.Driver_id);
                var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == driver.User_id);
                var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == order.Cargo_id);

                if (string.IsNullOrEmpty(user?.Email)) return;

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                mail.To.Add(user.Email);
                mail.Subject = $"Новый рейс №{order.Order_number}";
                mail.IsBodyHtml = true;

                mail.Body = $@"
                <div style='font-family: sans-serif; background-color: #f1f5f9; padding: 20px;'>
                    <div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden; border: 1px solid #e2e8f0;'>
                        <div style='background: #1e293b; padding: 20px; text-align: center; color: #fff;'>
                            <h2 style='margin:0;'>LOADMATE</h2>
                        </div>
                        <div style='padding: 20px;'>
                            <p><b>Здравствуйте, {user.Full_name}!</b></p>
                            <p>Вам назначен новый заказ. Детали ниже:</p>
                            <hr style='border: 0; border-top: 1px solid #eee;' />
                            <p><b>Номер заказа:</b> {order.Order_number}</p>
                            <p><b>Груз:</b> {cargo?.Description} ({cargo?.Weight_kg} кг)</p>
                            <p><b>Автомобиль:</b> {truck.Model} ({truck.Registration_number})</p>
                            <p><b>Грузоподъемность:</b> {truck.Capacity_kg} кг</p>
                        </div>
                        <div style='background: #f8fafc; padding: 15px; text-align: center; font-size: 12px; color: #64748b;'>
                            © {DateTime.Now.Year} LoadMate System
                        </div>
                    </div>
                </div>";

                using (SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587))
                {
                    smtp.Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "Bmz8qEIDckirBNY5cEmE");
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка почты: " + ex.Message); }
        }
    }
}