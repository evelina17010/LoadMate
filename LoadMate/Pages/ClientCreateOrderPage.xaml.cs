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
            LoadData();
            dpScheduledPickup.DisplayDateStart = DateTime.Today;
        }
        private void LoadData()
        {
            var db = Conn.loadMateEntities;

            cmbCargoType.ItemsSource = db.CargoType.ToList();
            cmbCargoType.DisplayMemberPath = "Name";
            cmbCargoType.SelectedValuePath = "CargoType_id";
            cmbTariff.ItemsSource = db.Tariff.Where(t => t.Is_active == true).ToList();
            cmbTariff.DisplayMemberPath = "Name";
            cmbTariff.SelectedValuePath = "Tariff_id";
            var cities = db.City.ToList();
            cmbStartCity.ItemsSource = cities;
            cmbStartCity.DisplayMemberPath = "Name";
            cmbStartCity.SelectedValuePath = "City_id";
            cmbEndCity.ItemsSource = cities;
            cmbEndCity.DisplayMemberPath = "Name";
            cmbEndCity.SelectedValuePath = "City_id";
            dpScheduledPickup.SelectedDate = DateTime.Now.AddDays(1);
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
                cmbStartStreet.DisplayMemberPath = "Name";
                cmbStartStreet.SelectedValuePath = "Street_id";
            }
            else
            {
                cmbEndStreet.ItemsSource = streets;
                cmbEndStreet.DisplayMemberPath = "Name";
                cmbEndStreet.SelectedValuePath = "Street_id";
            }
        }
        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

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
                   var cargo = new Cargo
                    {
                        Client_id = clientId,
                        CargoType_id = (int)cmbCargoType.SelectedValue,
                        Description = txtDescription.Text.Trim(),
                        Weight_kg = decimal.Parse(txtWeight.Text),
                        Volume_m3 = decimal.Parse(txtVolume.Text),
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
                        Manager_id = 1,
                        OrderStatus_id = 1,
                        Truck_id = 1,
                        Order_number = $"CL-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}",
                        Order_date = DateTime.Now,
                        Price = price,
                        Scheduled_pickup = dpScheduledPickup.SelectedDate
                    };
                    db.Order.Add(order);
                    db.SaveChanges();
                    db.Payment.Add(new Payment { Order_id = order.Order_id, PaymentStatus_id = 1, Amount = price, Transaction_date = DateTime.Now });
                    db.SaveChanges();
                    scope.Complete();
                    SendOrderConfirmationEmail(order, price);
                    MessageBox.Show($"Заказ №{order.Order_number} успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }
        private void SendOrderConfirmationEmail(Order order, decimal cost)
        {
            try
            {
                var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == clientId);
                if (user == null || string.IsNullOrEmpty(user.Email)) return;

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate: Новый заказ");
                mail.To.Add(user.Email);
                mail.Subject = "Ваш заказ №" + order.Order_number + " оформлен";
                mail.Body = $"Здравствуйте, {user.Full_name}!\n\n" +
                            $"Ваш заказ успешно создан в системе LoadMate.\n" +
                            $"--------------------------------------------\n" +
                            $"Номер заказа: {order.Order_number}\n" +
                            $"Дата создания: {order.Order_date:dd.MM.yyyy HH:mm}\n" +
                            $"Сумма к оплате: {cost:N2} руб.\n" +
                            $"Планируемая дата забора: {order.Scheduled_pickup:dd.MM.yyyy}\n" +
                            $"--------------------------------------------\n\n" +
                            $"Вы можете оплатить заказ в личном кабинете приложения.\n" +
                            $"Спасибо, что выбрали нас!";

                SmtpClient client = new SmtpClient("smtp.mail.ru", 587);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "Bmz8qEIDckirBNY5cEmE");
                client.Send(mail);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при отправке Email: " + ex.Message, "Уведомление", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private decimal CalculateFinalCost(Tariff tariff)
        {
            decimal.TryParse(txtWeight.Text, out decimal w);
            decimal.TryParse(txtVolume.Text, out decimal v);
            decimal cost = (100 * tariff.Cost_per_km) + (w * tariff.Cost_per_kg) + (v * tariff.Cost_per_m3) + tariff.Additional_cost;
            return (tariff.Min_price.HasValue && cost < tariff.Min_price.Value) ? tariff.Min_price.Value : cost;
        }
        private void CalculateCost_Click(object sender, RoutedEventArgs e) => UpdateCostDisplay();
        private void Tariff_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateCostDisplay();
        private void UpdateCostDisplay() { if (cmbTariff.SelectedItem is Tariff t) txtCost.Text = $"Стоимость: {CalculateFinalCost(t):N2} руб."; }

        private bool ValidateInput()
        {
            if (cmbStartStreet.SelectedValue == null || cmbEndStreet.SelectedValue == null || string.IsNullOrWhiteSpace(txtWeight.Text))
            {
                MessageBox.Show("Заполните адреса и вес!");
                return false;
            }
            return true;
        }
    }
}