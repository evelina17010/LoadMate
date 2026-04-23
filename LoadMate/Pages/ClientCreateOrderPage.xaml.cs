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
using System.Transactions;
using LoadMate.Windows;

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для ClientCreateOrderPage.xaml
    /// </summary>
    public partial class ClientCreateOrderPage : Page
    {
        private int clientId;

        public ClientCreateOrderPage(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;
            dpScheduledPickup.DisplayDateStart = DateTime.Today;
            dpScheduledPickup.SelectedDate = DateTime.Now.AddDays(1);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var db = Conn.loadMateEntities;
                cmbCargoType.ItemsSource = db.CargoType.ToList();
                cmbTariff.ItemsSource = db.Tariff.Where(t => t.Is_active == true).ToList();
                var cities = db.City.ToList();
                cmbStartCity.ItemsSource = cities;
                cmbEndCity.ItemsSource = cities;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Ошибка при загрузке данных: " + ex.Message, "Ошибка");
            }
        }

        private decimal GetDistance(int cityA, int cityB)
        {
            if (cityA == cityB) return 15.0m; 
            try
            {
                var db = Conn.loadMateEntities;
                var distRecord = db.Distance.FirstOrDefault(d =>
                    (d.City_A_id == cityA && d.City_B_id == cityB) ||
                    (d.City_A_id == cityB && d.City_B_id == cityA));

                return distRecord != null ? distRecord.Distance_km : 500.0m;
            }
            catch { return 500.0m; }
        }

        private void City_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cmb = sender as ComboBox;
            if (cmb?.SelectedValue == null) return;
            try
            {
                int cityId = (int)cmb.SelectedValue;
                var streets = Conn.loadMateEntities.Street.Where(s => s.City_id == cityId).ToList();
                if (cmb == cmbStartCity) cmbStartStreet.ItemsSource = streets;
                else cmbEndStreet.ItemsSource = streets;
                UpdateCost_Event(null, null);
            }
            catch (Exception ex) { CustomMessageBox.Show("Ошибка: " + ex.Message, "Ошибка"); }
        }

        private void UpdateCost_Event(object sender, EventArgs e)
        {
            if (cmbTariff.SelectedItem is Tariff t)
                txtCost.Text = $"Предварительная стоимость: {CalculateFinalCost(t):N2} руб.";
        }

        private decimal CalculateFinalCost(Tariff tariff)
        {
            if (cmbStartCity.SelectedValue == null || cmbEndCity.SelectedValue == null) return 0;

            decimal distance = GetDistance((int)cmbStartCity.SelectedValue, (int)cmbEndCity.SelectedValue);

            decimal.TryParse(txtWeight.Text.Replace(".", ","), out decimal w);
            decimal.TryParse(txtVolume.Text.Replace(".", ","), out decimal v);
            decimal cost = (distance * tariff.Cost_per_km) + (w * tariff.Cost_per_kg) + (v * tariff.Cost_per_m3) + tariff.Additional_cost;

            return (tariff.Min_price.HasValue && cost < tariff.Min_price.Value) ? tariff.Min_price.Value : cost;
        }

        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;
            Order savedOrder = null;
            Cargo savedCargo = null;
            User clientUser = null;
            decimal priceToMail = 0;

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
                    decimal actualDistance = GetDistance((int)cmbStartCity.SelectedValue, (int)cmbEndCity.SelectedValue);

                    var route = new Route
                    {
                        Start_address_id = startAddr.Address_id,
                        End_address_id = endAddr.Address_id,
                        Distance_km = actualDistance,
                        Estimated_time_hours = (int)(actualDistance / 60) + 1
                    };
                    db.Route.Add(route);
                    db.SaveChanges();

                    var cargo = new Cargo
                    {
                        Client_id = clientId,
                        CargoType_id = (int)cmbCargoType.SelectedValue,
                        Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? "Нет описания" : txtDescription.Text.Trim(),
                        Weight_kg = decimal.Parse(txtWeight.Text.Replace(".", ",")),
                        Volume_m3 = decimal.Parse(txtVolume.Text.Replace(".", ",")),
                        Created_at = DateTime.Now
                    };
                    db.Cargo.Add(cargo);
                    db.SaveChanges();

                    var tariff = (Tariff)cmbTariff.SelectedItem;
                    decimal price = CalculateFinalCost(tariff);

                    var order = new Order
                    {
                        Cargo_id = cargo.Cargo_id,
                        Tariff_id = tariff.Tariff_id,
                        Route_id = route.Route_id,
                        Manager_id = null,
                        OrderStatus_id = 1,
                        Truck_id = null,
                        Order_number = $"CL-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}",
                        Order_date = DateTime.Now,
                        Price = price,
                        Scheduled_pickup = dpScheduledPickup.SelectedDate
                    };
                    db.Order.Add(order);
                    db.SaveChanges();

                    db.Payment.Add(new Payment { Order_id = order.Order_id, PaymentStatus_id = 1, Amount = price, Transaction_date = DateTime.Now });
                    db.SaveChanges();

                    clientUser = db.User.FirstOrDefault(u => u.User_id == clientId);
                    savedOrder = order;
                    savedCargo = cargo;
                    priceToMail = price;

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Ошибка БД: " + (ex.InnerException?.Message ?? ex.Message), "Ошибка");
                    return;
                }
            }
            if (savedOrder != null && clientUser != null)
            {
                SendBeautifulEmail(savedOrder, savedCargo, priceToMail, clientUser);
                CustomMessageBox.Show($"Заказ №{savedOrder.Order_number} успешно создан! Информация отправлена на почту.", "Успех");
                ClearFields();
            }
        }

        private void SendBeautifulEmail(Order order, Cargo cargo, decimal price, User user)
        {
            try
            {
                string targetEmail = user.Email;
                if (string.IsNullOrEmpty(targetEmail)) return;

                var db = Conn.loadMateEntities;
                var route = db.Route.Include("Address.Street.City").Include("Address1.Street.City").FirstOrDefault(r => r.Route_id == order.Route_id);

                string fullAddressFrom = route != null
                    ? $"{route.Address.Street.City.Name}, {route.Address.Street.Name}, д. {route.Address.House_number}" : "Не указан";

                string fullAddressTo = route != null
                    ? $"{route.Address1.Street.City.Name}, {route.Address1.Street.Name}, д. {route.Address1.House_number}" : "Не указан";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                mail.To.Add(targetEmail);
                mail.Subject = $"Заказ №{order.Order_number} оформлен";
                mail.IsBodyHtml = true;

                mail.Body = $@"
        <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; padding: 20px 10px;'>
            <div style='max-width: 500px; margin: 0 auto; background-color: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>
                
                <div style='background-color: #5cb85c; padding: 25px 20px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 24px; font-weight: bold; color: #ffffff; letter-spacing: 1px;'>LOADMATE</h1>
                </div>

                <div style='padding: 25px;'>
                    <p style='margin: 0 0 20px 0; font-size: 16px; color: #1e293b; font-weight: 600;'>Здравствуйте, {user.Full_name}!</p>
                    <p style='margin: 0 0 25px 0; color: #475569; font-size: 14px; line-height: 1.5;'>Ваша заявка на перевозку успешно создана. Ниже указаны детали маршрута и параметры груза.</p>
                    
                    <div style='margin-bottom: 25px;'>
                        <div style='margin-bottom: 15px;'>
                            <div style='font-size: 11px; color: #94a3b8; text-transform: uppercase; font-weight: bold; margin-bottom: 4px;'>Пункт отправления</div>
                            <div style='font-size: 14px; color: #1e293b; line-height: 1.4;'>{fullAddressFrom}</div>
                        </div>
                        <div>
                            <div style='font-size: 11px; color: #94a3b8; text-transform: uppercase; font-weight: bold; margin-bottom: 4px;'>Пункт назначения</div>
                            <div style='font-size: 14px; color: #1e293b; line-height: 1.4;'>{fullAddressTo}</div>
                        </div>
                    </div>

                    <div style='border-top: 1px solid #f1f5f9; padding-top: 20px;'>
                        <table style='width: 100%; font-size: 14px; color: #475569;'>
                            <tr>
                                <td style='padding: 5px 0;'>Номер заказа:</td>
                                <td style='padding: 5px 0; text-align: right; color: #1e293b; font-weight: 600;'>{order.Order_number}</td>
                            </tr>
                            <tr>
                                <td style='padding: 5px 0;'>Груз:</td>
                                <td style='padding: 5px 0; text-align: right; color: #1e293b;'>{cargo.Description}</td>
                            </tr>
                            <tr>
                                <td style='padding: 15px 0 5px 0; font-size: 16px; color: #1e293b; font-weight: bold;'>Стоимость:</td>
                                <td style='padding: 15px 0 5px 0; text-align: right; color: #5cb85c; font-size: 20px; font-weight: bold;'>{price:N2} ₽</td>
                            </tr>
                        </table>
                    </div>
                </div>

                <div style='background-color: #f8fafc; padding: 20px; text-align: center; border-top: 1px solid #f1f5f9;'>
                    <div style='font-size: 12px; color: #94a3b8;'>
                        © {DateTime.Now.Year} LoadMate System<br>
                        Служба поддержки: miftakhova_ev@mail.ru
                    </div>
                </div>
            </div>
        </div>";

                using (SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587))
                {
                    smtp.Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "rGEil7HXFW2suOFKVjvs");
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ClearFields()
        {
            txtDescription.Clear(); txtWeight.Clear(); txtVolume.Clear();
            txtStartHouse.Clear(); txtEndHouse.Clear();
            txtCost.Text = "Предварительная стоимость: 0,00 руб.";
            cmbCargoType.SelectedIndex = -1; cmbTariff.SelectedIndex = -1;
            cmbStartCity.SelectedIndex = -1; cmbEndCity.SelectedIndex = -1;
            cmbStartStreet.ItemsSource = null; cmbEndStreet.ItemsSource = null;
            dpScheduledPickup.SelectedDate = DateTime.Now.AddDays(1);
        }

        private bool ValidateInput()
        {
            if (cmbCargoType.SelectedValue == null || cmbTariff.SelectedValue == null ||
                cmbStartStreet.SelectedValue == null || cmbEndStreet.SelectedValue == null ||
                string.IsNullOrWhiteSpace(txtWeight.Text) || string.IsNullOrWhiteSpace(txtVolume.Text))
            {
                CustomMessageBox.Show("Заполните все обязательные поля!", "Внимание");
                return false;
            }
            return true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }
    }
}