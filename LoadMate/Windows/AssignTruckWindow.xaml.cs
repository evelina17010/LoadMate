using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LoadMate.DBConn;
using System.Net.Mail;
using System.Net;

namespace LoadMate.Windows
{
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
                txtVolume.Text = $"{cargo.Volume_m3} м³";
            }
        }

        private void LoadAvailableTrucks()
        {
            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == _currentOrder.Cargo_id);
            decimal cargoWeight = cargo?.Weight_kg ?? 0;

            var availableTrucks = Conn.loadMateEntities.Truck
                .Where(t => t.TruckStatus_id == 1 && t.Capacity_kg >= cargoWeight)
                .ToList();

            UpdateTrucksGrid(availableTrucks);
        }

        private void UpdateTrucksGrid(System.Collections.Generic.List<Truck> trucks)
        {
            var trucksWithDetails = trucks.Select(t => new
            {
                t.Truck_id,
                t.Model,
                t.Registration_number,
                t.Capacity_kg,
                t.Capacity_m3,
                DriverName = GetDriverName(t.Driver_id),
                StatusName = GetTruckStatusName(t.TruckStatus_id)
            }).ToList();

            TrucksGrid.ItemsSource = trucksWithDetails;
        }

        private string GetDriverName(int? driverId)
        {
            if (!driverId.HasValue) return "Не назначен";
            var driver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == driverId);
            if (driver == null) return "Не назначен";
            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == driver.User_id);
            return user?.Full_name ?? "Не назначен";
        }

        private string GetTruckStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.TruckStatus.FirstOrDefault(ts => ts.TruckStatus_id == statusId);
            return status?.Name ?? "Не указан";
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            bool onlyAvailable = chkOnlyAvailable.IsChecked ?? false;
            decimal minCap = 0;

            if (!string.IsNullOrWhiteSpace(txtMinCapacity.Text))
            {
                decimal.TryParse(txtMinCapacity.Text, out minCap);
            }

            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == _currentOrder.Cargo_id);
            decimal cargoWeight = cargo?.Weight_kg ?? 0;

            var query = Conn.loadMateEntities.Truck.AsQueryable();

            if (onlyAvailable)
            {
                query = query.Where(t => t.TruckStatus_id == 1);
            }

            if (minCap > 0)
            {
                query = query.Where(t => t.Capacity_kg >= minCap);
            }
            else
            {
                query = query.Where(t => t.Capacity_kg >= cargoWeight);
            }

            UpdateTrucksGrid(query.ToList());
        }

        private void TrucksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrucksGrid.SelectedItem == null) return;

            dynamic selected = TrucksGrid.SelectedItem;
            int truckId = selected.Truck_id;
            _selectedTruck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
        }

        private void Assign_Click(object sender, RoutedEventArgs e)
        {
            if (TrucksGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите транспортное средство", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic selected = TrucksGrid.SelectedItem;
            int truckId = selected.Truck_id;
            _selectedTruck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);

            if (_selectedTruck != null)
            {
                var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == _currentOrder.Cargo_id);

                if (cargo != null)
                {
                    if (_selectedTruck.Capacity_kg < cargo.Weight_kg)
                    {
                        MessageBox.Show("Грузоподъемность транспорта меньше веса груза!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (_selectedTruck.Capacity_m3 < cargo.Volume_m3)
                    {
                        MessageBox.Show("Объем кузова меньше объема груза!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                var dbOrder = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == _currentOrder.Order_id);
                if (dbOrder != null)
                {
                    dbOrder.Truck_id = _selectedTruck.Truck_id;
                    dbOrder.OrderStatus_id = 3;
                    Conn.loadMateEntities.SaveChanges();

                    System.Threading.Tasks.Task.Run(() => SendEmailToDriver(dbOrder));
                    MessageBox.Show("Транспорт успешно назначен!");
                    DialogResult = true;
                    Close();

                }
            }
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

                var cargo = db.Cargo.FirstOrDefault(c => c.Cargo_id == order.Cargo_id);

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                mail.To.Add(user.Email);
                mail.Subject = $"Новый рейс №{order.Order_number}";
                mail.IsBodyHtml = true;

                mail.Body = $@"
                <div style='font-family: sans-serif; background-color: #f1f5f9; padding: 20px;'>
                    <div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden; border: 1px solid #e2e8f0;'>
                        <div style='background: #4CAF50; padding: 20px; text-align: center; color: #fff;'>
                            <h2 style='margin:0;'>LOADMATE</h2>
                        </div>
                        <div style='padding: 20px;'>
                            <p><b>Здравствуйте, {user.Full_name}!</b></p>
                            <p>Вам назначен новый заказ. Детали ниже:</p>
                            <hr style='border: 0; border-top: 1px solid #eee;' />
                            <p><b>Номер заказа:</b> {order.Order_number}</p>
                            <p><b>Груз:</b> {cargo?.Description} ({cargo?.Weight_kg} кг, {cargo?.Volume_m3} м³)</p>
                            <p><b>Автомобиль:</b> {truck.Model} ({truck.Registration_number})</p>
                            <p><b>Грузоподъемность:</b> {truck.Capacity_kg} кг</p>
                            <p><b>Объем кузова:</b> {truck.Capacity_m3} м³</p>
                        </div>
                        <div style='background: #f8fafc; padding: 15px; text-align: center; font-size: 12px; color: #64748b;'>
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
                System.Diagnostics.Debug.WriteLine("Ошибка отправки Email: " + ex.Message);
                MessageBox.Show("Заказ назначен, но уведомление водителю не отправлено: " + ex.Message,
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}