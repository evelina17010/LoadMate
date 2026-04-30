using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Data.Entity;
using LoadMate.DBConn;
using LoadMate.Pages;

namespace LoadMate.Windows
{
    public partial class ChangeStatusWindow : Window
    {
        public int SelectedStatusId { get; private set; }
        private Order _currentOrder;
        private bool _statusChanged = false;

        public ChangeStatusWindow(int currentStatusId, Order order = null)
        {
            InitializeComponent();
            _currentOrder = order;

            var statuses = Conn.loadMateEntities.OrderStatus.ToList();
            cmbStatus.ItemsSource = statuses;
            cmbStatus.DisplayMemberPath = "Name";
            cmbStatus.SelectedValuePath = "OrderStatus_id";
            cmbStatus.SelectedValue = currentStatusId;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (cmbStatus.SelectedItem != null)
            {
                SelectedStatusId = (int)cmbStatus.SelectedValue;

                if (_currentOrder != null && SelectedStatusId != _currentOrder.OrderStatus_id)
                {
                    _statusChanged = true;
                    SendStatusEmailToClient(_currentOrder, SelectedStatusId);
                }

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Выберите статус", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SendStatusEmailToClient(Order order, int newStatusId)
        {
            try
            {
                var db = Conn.loadMateEntities;

                var cargo = db.Cargo.FirstOrDefault(c => c.Cargo_id == order.Cargo_id);
                if (cargo == null) return;

                var client = db.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
                if (client == null || string.IsNullOrEmpty(client.Email)) return;

                var oldStatus = db.OrderStatus.FirstOrDefault(s => s.OrderStatus_id == order.OrderStatus_id);
                var newStatus = db.OrderStatus.FirstOrDefault(s => s.OrderStatus_id == newStatusId);
                var manager = db.User.FirstOrDefault(u => u.User_id == order.Manager_id);

                var route = db.Route
                    .Include(r => r.Address)
                    .Include(r => r.Address1)
                    .FirstOrDefault(r => r.Route_id == order.Route_id);

                string fromAddress = "Не указан";
                string toAddress = "Не указан";

                if (route != null)
                {
                    var startAddr = db.Address.FirstOrDefault(a => a.Address_id == route.Start_address_id);
                    var endAddr = db.Address.FirstOrDefault(a => a.Address_id == route.End_address_id);

                    if (startAddr != null)
                    {
                        var street = db.Street.FirstOrDefault(s => s.Street_id == startAddr.Street_id);
                        if (street != null)
                        {
                            var city = db.City.FirstOrDefault(c => c.City_id == street.City_id);
                            fromAddress = city != null ? $"{city.Name}, {street.Name}, {startAddr.House_number}" : $"{street.Name}, {startAddr.House_number}";
                        }
                    }

                    if (endAddr != null)
                    {
                        var street = db.Street.FirstOrDefault(s => s.Street_id == endAddr.Street_id);
                        if (street != null)
                        {
                            var city = db.City.FirstOrDefault(c => c.City_id == street.City_id);
                            toAddress = city != null ? $"{city.Name}, {street.Name}, {endAddr.House_number}" : $"{street.Name}, {endAddr.House_number}";
                        }
                    }
                }

                string statusColor = "#5cb85c";
                if (newStatusId == 8) statusColor = "#d9534f";
                else if (newStatusId == 5 || newStatusId == 3) statusColor = "#f0ad4e";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                mail.To.Add(client.Email);
                mail.Subject = $"Изменение статуса заказа №{order.Order_number}";
                mail.IsBodyHtml = true;

                mail.Body = $@"
                <div style='font-family: Arial, sans-serif; background-color: #f8fafc; padding: 20px;'>
                    <div style='max-width: 500px; margin: 0 auto; background-color: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>
                        <div style='background-color: #5cb85c; padding: 20px; text-align: center;'>
                            <h1 style='margin: 0; font-size: 22px; color: #ffffff;'>LOADMATE</h1>
                        </div>
                        <div style='padding: 25px;'>
                            <p style='font-weight: bold; font-size: 16px;'>Здравствуйте, {client.Full_name}!</p>
                            <p>Статус вашего заказа <b>№{order.Order_number}</b> был обновлен.</p>
                            
                            <div style='background-color: #f0f4f8; padding: 15px; border-radius: 8px; margin: 20px 0; text-align: center;'>
                                <div style='font-size: 12px; color: #64748b; margin-bottom: 5px;'>Предыдущий статус</div>
                                <div style='font-size: 16px; font-weight: bold; color: #94a3b8;'>{oldStatus?.Name ?? "Не указан"}</div>
                                <div style='margin: 10px 0;'>↓</div>
                                <div style='font-size: 12px; color: #64748b; margin-bottom: 5px;'>Новый статус</div>
                                <div style='font-size: 20px; font-weight: bold; color: {statusColor};'>{newStatus?.Name ?? "Не указан"}</div>
                            </div>
                            
                            <hr style='border: 0; border-top: 1px solid #f1f5f9; margin: 20px 0;'>
                            <p><b>Откуда:</b> {fromAddress}</p>
                            <p><b>Куда:</b> {toAddress}</p>
                            <p><b>Груз:</b> {cargo.Description}</p>
                            <p><b>Вес:</b> {cargo.Weight_kg} кг</p>
                            <p><b>Объем:</b> {cargo.Volume_m3} м³</p>
                            <p style='font-size: 16px; color: #5cb85c;'><b>Стоимость:</b> {order.Price:N2} ₽</p>
                        </div>
                        <div style='background-color: #f8fafc; padding: 15px; text-align: center; font-size: 11px; color: #94a3b8;'>
                            © {DateTime.Now.Year} LoadMate System<br>
                            Статус заказа можно отслеживать в личном кабинете
                        </div>
                    </div>
                </div>";

                using (SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587))
                {
                    smtp.Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "rGEil7HXFW2suOFKVjvs");
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }

                string managerMessage = $"Письмо клиенту {client.Full_name} об изменении статуса заказа №{order.Order_number} на \"{newStatus?.Name}\" успешно отправлено!";
                MessageBox.Show(managerMessage, "Уведомление диспетчера");
            }
            catch (Exception ex)
            {
                string managerError = $"Не удалось отправить письмо клиенту! Ошибка: {ex.Message}";
                MessageBox.Show(managerError, "Уведомление диспетчера");
                System.Diagnostics.Debug.WriteLine("Ошибка отправки письма о статусе: " + ex.Message);
            }
        }
    }
}