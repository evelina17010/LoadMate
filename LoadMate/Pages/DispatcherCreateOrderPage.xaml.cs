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
    /// <summary>
    /// Логика взаимодействия для DispatcherCreateOrderPage.xaml
    /// </summary>
    public partial class DispatcherCreateOrderPage : Page
    {
        private int managerId;
       public DispatcherCreateOrderPage(int managerId)
        {
            InitializeComponent();
            this.managerId = managerId;
            LoadData();
            dpScheduledPickup.DisplayDateStart = DateTime.Today;
        }
        private void LoadData()
        {
            var db = Conn.loadMateEntities;
            cmbClient.ItemsSource = db.User.Where(u => u.Role_id == 2).ToList();
            cmbClient.DisplayMemberPath = "Full_name";
            cmbClient.SelectedValuePath = "User_id";
            cmbCargoType.ItemsSource = db.CargoType.ToList();
            cmbCargoType.DisplayMemberPath = "Name";
            cmbCargoType.SelectedValuePath = "CargoType_id";
            cmbTariff.ItemsSource = db.Tariff.Where(t => t.Is_active == true).ToList();
            cmbTariff.DisplayMemberPath = "Name";
            cmbTariff.SelectedValuePath = "Tariff_id";
            cmbTruck.ItemsSource = db.Truck.Where(t => t.TruckStatus_id == 1).ToList();
            cmbTruck.DisplayMemberPath = "Registration_number";
            cmbTruck.SelectedValuePath = "Truck_id";
            var cities = db.City.ToList();
            cmbStartCity.ItemsSource = cities;
            cmbStartCity.DisplayMemberPath = "Name";
            cmbStartCity.SelectedValuePath = "City_id";
            cmbEndCity.ItemsSource = cities;
            cmbEndCity.DisplayMemberPath = "Name";
            cmbEndCity.SelectedValuePath = "City_id";
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
        private void SendEmailToDriver(Order order)
        {
            try
            {
                var db = Conn.loadMateEntities;
                var truck = db.Truck.FirstOrDefault(t => t.Truck_id == order.Truck_id);
               if (truck == null || !truck.Driver_id.HasValue) return;
                var driver = db.Driver.FirstOrDefault(d => d.Driver_id == truck.Driver_id);
                var user = db.User.FirstOrDefault(u => u.User_id == driver.User_id);
                if (string.IsNullOrEmpty(user?.Email)) return;
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate");
                mail.To.Add(user.Email);
                mail.Subject = "Новый заказ назначен";
                mail.Body = $"Водитель {user.Full_name}, вам назначен заказ {order.Order_number}.\nДата: {order.Scheduled_pickup:dd.MM.yyyy}";

                SmtpClient client = new SmtpClient("smtp.mail.ru", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "Bmz8qEIDckirBNY5cEmE"),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };
                client.Send(mail);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
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
                        Client_id = (int)cmbClient.SelectedValue,
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
                        Manager_id = managerId,
                        Truck_id = (int)cmbTruck.SelectedValue,
                        OrderStatus_id = 2,
                        Order_number = $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                        Order_date = DateTime.Now,
                        Price = price,
                        Scheduled_pickup = dpScheduledPickup.SelectedDate
                    };
                    db.Order.Add(order);
                    db.SaveChanges();
                    db.Payment.Add(new Payment { Order_id = order.Order_id, PaymentStatus_id = 1, Amount = price, Transaction_date = DateTime.Now });
                    db.SaveChanges();
                    scope.Complete();
                    SendEmailToDriver(order);
                    MessageBox.Show("Заказ создан.");
                    NavigationService.GoBack();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
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
            if (cmbTariff.SelectedItem is Tariff t)
                txtCost.Text = $"Итоговая стоимость: {CalculateFinalCost(t):N2} руб.";
        }
        private bool ValidateInput()
        {
            if (cmbClient.SelectedValue == null || cmbStartStreet.SelectedValue == null ||
                cmbEndStreet.SelectedValue == null || cmbTruck.SelectedValue == null ||
                string.IsNullOrWhiteSpace(txtWeight.Text))
            {
                MessageBox.Show("Заполните все поля!");
                return false;
            }
            return true;
        }
    }
}