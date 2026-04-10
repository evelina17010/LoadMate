using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Data.Entity;
using System.Transactions;
using System.Net.Mail;
using System.Net;

namespace LoadMate.Pages
{
    public partial class DispatcherCreateOrderPage : Page
    {
        private int managerId;

        public DispatcherCreateOrderPage(int managerId)
        {
            InitializeComponent();
            this.managerId = managerId;
            LoadData();
            dpScheduledPickup.SelectedDate = DateTime.Today;
            dpScheduledPickup.DisplayDateStart = DateTime.Today;
        }

        private void LoadData()
        {
            var db = Conn.loadMateEntities;
            LoadClients();

            cmbCargoType.ItemsSource = db.CargoType.ToList();
            cmbCargoType.SelectedValuePath = "CargoType_id";

            cmbTariff.ItemsSource = db.Tariff.Where(t => t.Is_active == true).ToList();
            cmbTariff.SelectedValuePath = "Tariff_id";

            var cities = db.City.ToList();
            cmbStartCity.ItemsSource = cities;
            cmbStartCity.SelectedValuePath = "City_id";

            cmbEndCity.ItemsSource = cities;
            cmbEndCity.SelectedValuePath = "City_id";
        }

        private void LoadClients()
        {
            cmbClient.ItemsSource = Conn.loadMateEntities.User
                .Where(u => u.Role_id == 2)
                .OrderByDescending(u => u.Created_at)
                .ToList();
            cmbClient.SelectedValuePath = "User_id";
        }

        private void AddNewClient_Click(object sender, RoutedEventArgs e)
        {
            var regWin = new Windows.QuickRegWindow();
            regWin.Owner = Window.GetWindow(this);
            if (regWin.ShowDialog() == true)
            {
                LoadClients();
                if (regWin.CreatedUser != null) cmbClient.SelectedValue = regWin.CreatedUser.User_id;
            }
        }

        private void City_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cmb = sender as ComboBox;
            if (cmb?.SelectedValue == null) return;

            int cityId = (int)cmb.SelectedValue;
            var streets = Conn.loadMateEntities.Street.Where(s => s.City_id == cityId).ToList();

            if (cmb == cmbStartCity)
            {
                cmbStartStreet.ItemsSource = streets;
                cmbStartStreet.SelectedValuePath = "Street_id";
            }
            else
            {
                cmbEndStreet.ItemsSource = streets;
                cmbEndStreet.SelectedValuePath = "Street_id";
            }
        }

        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            Order newOrder = null;
            Cargo newCargo = null;
            decimal finalPrice = 0;

            using (var scope = new TransactionScope())
            {
                try
                {
                    var db = Conn.loadMateEntities;

                    var startAddr = new Address { Street_id = (int)cmbStartStreet.SelectedValue, House_number = txtStartHouse.Text.Trim() };
                    var endAddr = new Address { Street_id = (int)cmbEndStreet.SelectedValue, House_number = txtEndHouse.Text.Trim() };
                    db.Address.Add(startAddr);
                    db.Address.Add(endAddr);
                    db.SaveChanges();

                    var route = new Route { Start_address_id = startAddr.Address_id, End_address_id = endAddr.Address_id, Distance_km = 100, Estimated_time_hours = 4 };
                    db.Route.Add(route);
                    db.SaveChanges();

                    newCargo = new Cargo
                    {
                        Client_id = (int)cmbClient.SelectedValue,
                        CargoType_id = (int)cmbCargoType.SelectedValue,
                        Description = txtDescription.Text.Trim(),
                        Weight_kg = decimal.Parse(txtWeight.Text),
                        Volume_m3 = decimal.Parse(txtVolume.Text),
                        Created_at = DateTime.Now
                    };
                    db.Cargo.Add(newCargo);
                    db.SaveChanges();

                    var tariff = (Tariff)cmbTariff.SelectedItem;
                    finalPrice = CalculateFinalCost(tariff);

                    newOrder = new Order
                    {
                        Cargo_id = newCargo.Cargo_id,
                        Tariff_id = tariff.Tariff_id,
                        Route_id = route.Route_id,
                        Manager_id = managerId,
                        Truck_id = 1,
                        OrderStatus_id = 1, 
                        Order_number = $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                        Order_date = DateTime.Now,
                        Price = finalPrice,
                        Scheduled_pickup = dpScheduledPickup.SelectedDate
                    };

                    db.Order.Add(newOrder);
                    db.SaveChanges();

                    db.Payment.Add(new Payment { Order_id = newOrder.Order_id, PaymentStatus_id = 1, Amount = finalPrice, Transaction_date = DateTime.Now });
                    db.SaveChanges();

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                    return;
                }
            }

            if (newOrder != null)
            {
                var clientUser = (User)cmbClient.SelectedItem;
                SendBeautifulEmail(newOrder, newCargo, finalPrice, clientUser);
                MessageBox.Show($"Заявка {newOrder.Order_number} создана. Транспорт будет назначен позже.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
        }

        private void SendBeautifulEmail(Order order, Cargo cargo, decimal price, User user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.Email)) return;

                var db = Conn.loadMateEntities;
                var route = db.Route.Include(r => r.Address.Street.City).Include(r => r.Address1.Street.City).FirstOrDefault(r => r.Route_id == order.Route_id);

                string fullAddressFrom = route != null ? $"{route.Address.Street.City.Name}, {route.Address.Street.Name}, д. {route.Address.House_number}" : "Не указан";
                string fullAddressTo = route != null ? $"{route.Address1.Street.City.Name}, {route.Address1.Street.Name}, д. {route.Address1.House_number}" : "Не указан";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                mail.To.Add(user.Email);
                mail.Subject = $"Заявка №{order.Order_number} принята в работу";
                mail.IsBodyHtml = true;

                mail.Body = $@"
                <div style='font-family: Arial, sans-serif; background-color: #f8fafc; padding: 20px;'>
                    <div style='max-width: 500px; margin: 0 auto; background-color: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>
                        <div style='background-color: #5cb85c; padding: 25px; text-align: center;'>
                            <h1 style='margin: 0; font-size: 24px; color: #ffffff;'>LOADMATE</h1>
                        </div>
                        <div style='padding: 25px;'>
                            <p style='font-weight: bold;'>Здравствуйте, {user.Full_name}!</p>
                            <p>Ваша заявка успешно создана. Мы подбираем подходящий транспорт.</p>
                            <hr style='border: 0; border-top: 1px solid #f1f5f9; margin: 20px 0;'>
                            <p><b>Откуда:</b> {fullAddressFrom}</p>
                            <p><b>Куда:</b> {fullAddressTo}</p>
                            <p><b>Груз:</b> {cargo.Description}</p>
                            <p style='font-size: 18px; color: #5cb85c;'><b>Стоимость:</b> {price:N2} ₽</p>
                        </div>
                        <div style='background-color: #f8fafc; padding: 15px; text-align: center; font-size: 12px; color: #94a3b8;'>
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
            catch { }
        }

        private decimal CalculateFinalCost(Tariff tariff)
        {
            decimal.TryParse(txtWeight.Text, out decimal w);
            decimal.TryParse(txtVolume.Text, out decimal v);
            decimal cost = (100 * tariff.Cost_per_km) + (w * tariff.Cost_per_kg) + (v * tariff.Cost_per_m3) + tariff.Additional_cost;
            return (tariff.Min_price.HasValue && cost < tariff.Min_price.Value) ? tariff.Min_price.Value : cost;
        }

        private void UpdateCost_Event(object sender, EventArgs e)
        {
            if (cmbTariff.SelectedItem is Tariff t) txtCost.Text = $"Итоговая стоимость: {CalculateFinalCost(t):N2} руб.";
        }

        private bool ValidateInput()
        {
            if (cmbClient.SelectedValue == null || cmbStartStreet.SelectedValue == null || cmbEndStreet.SelectedValue == null ||
                cmbTariff.SelectedValue == null || string.IsNullOrWhiteSpace(txtWeight.Text))
            {
                MessageBox.Show("Заполните все обязательные поля.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
    }
}