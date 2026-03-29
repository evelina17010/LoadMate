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

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для DispatcherCreateOrderPage.xaml
    /// </summary>
    public partial class DispatcherCreateOrderPage : Page
    {
        private int dispatcherId;

        public DispatcherCreateOrderPage(int dispatcherId)
        {
            InitializeComponent();
            this.dispatcherId = dispatcherId;
            LoadComboBoxes();
        }

        private void LoadComboBoxes()
        {
            cmbClient.ItemsSource = Conn.loadMateEntities.User.Where(u => u.Role_id == 2).ToList();
            cmbClient.DisplayMemberPath = "Full_name";
            cmbClient.SelectedValuePath = "User_id";
            if (cmbClient.Items.Count > 0) cmbClient.SelectedIndex = 0;

            cmbCargoType.ItemsSource = Conn.loadMateEntities.CargoType.ToList();
            cmbCargoType.DisplayMemberPath = "Name";
            cmbCargoType.SelectedValuePath = "CargoType_id";
            if (cmbCargoType.Items.Count > 0) cmbCargoType.SelectedIndex = 0;

            cmbTariff.ItemsSource = Conn.loadMateEntities.Tariff.Where(t => t.Is_active == true).ToList();
            cmbTariff.DisplayMemberPath = "Name";
            cmbTariff.SelectedValuePath = "Tariff_id";
            if (cmbTariff.Items.Count > 0) cmbTariff.SelectedIndex = 0;

            cmbTruck.ItemsSource = Conn.loadMateEntities.Truck.Where(t => t.TruckStatus_id == 1).ToList();
            cmbTruck.DisplayMemberPath = "Model";
            cmbTruck.SelectedValuePath = "Truck_id";

            cmbDriver.ItemsSource = Conn.loadMateEntities.Driver.Where(d => d.DriverStatus_id == 1).ToList();
            cmbDriver.DisplayMemberPath = "User.Full_name";
            cmbDriver.SelectedValuePath = "Driver_id";

            var cities = Conn.loadMateEntities.City.ToList();
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
            if (cmb == null) return;

            if (cmb == cmbStartCity && cmbStartCity.SelectedItem != null)
            {
                int cityId = (int)cmbStartCity.SelectedValue;
                var streets = Conn.loadMateEntities.Street.Where(s => s.City_id == cityId).ToList();
                cmbStartStreet.ItemsSource = streets;
                cmbStartStreet.DisplayMemberPath = "Name";
                cmbStartStreet.SelectedValuePath = "Street_id";
            }
            else if (cmb == cmbEndCity && cmbEndCity.SelectedItem != null)
            {
                int cityId = (int)cmbEndCity.SelectedValue;
                var streets = Conn.loadMateEntities.Street.Where(s => s.City_id == cityId).ToList();
                cmbEndStreet.ItemsSource = streets;
                cmbEndStreet.DisplayMemberPath = "Name";
                cmbEndStreet.SelectedValuePath = "Street_id";
            }
        }

        private void Tariff_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateCost_Click(null, null);
        }

        private void CalculateCost_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTariff.SelectedItem == null) return;

            if (!decimal.TryParse(txtWeight.Text, out decimal weight) || weight <= 0)
            {
                txtCost.Text = "Стоимость: введите вес";
                return;
            }

            if (!decimal.TryParse(txtVolume.Text, out decimal volume) || volume <= 0)
            {
                txtCost.Text = "Стоимость: введите объем";
                return;
            }

            var tariff = (Tariff)cmbTariff.SelectedItem;

            decimal distance = 100;
            decimal cost = (distance * tariff.Cost_per_km) +
                          (weight * tariff.Cost_per_kg) +
                          (volume * tariff.Cost_per_m3) +
                          tariff.Additional_cost;

            if (tariff.Min_price.HasValue && cost < tariff.Min_price.Value)
                cost = tariff.Min_price.Value;

            txtCost.Text = $"Стоимость: {cost:N2} руб.";
        }

        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbClient.SelectedItem == null ||
                    !decimal.TryParse(txtWeight.Text, out decimal weight) || weight <= 0 ||
                    !decimal.TryParse(txtVolume.Text, out decimal volume) || volume <= 0 ||
                    string.IsNullOrWhiteSpace(txtDescription.Text) ||
                    cmbStartCity.SelectedItem == null || cmbStartStreet.SelectedItem == null ||
                    string.IsNullOrWhiteSpace(txtStartHouse.Text) ||
                    cmbEndCity.SelectedItem == null || cmbEndStreet.SelectedItem == null ||
                    string.IsNullOrWhiteSpace(txtEndHouse.Text) ||
                    dpScheduledPickup.SelectedDate == null)
                {
                    MessageBox.Show("Заполните все обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var startAddress = new Address
                {
                    Street_id = (int)cmbStartStreet.SelectedValue,
                    House_number = txtStartHouse.Text.Trim()
                };
                Conn.loadMateEntities.Address.Add(startAddress);
                Conn.loadMateEntities.SaveChanges();

                var endAddress = new Address
                {
                    Street_id = (int)cmbEndStreet.SelectedValue,
                    House_number = txtEndHouse.Text.Trim()
                };
                Conn.loadMateEntities.Address.Add(endAddress);
                Conn.loadMateEntities.SaveChanges();

                var route = new Route
                {
                    Start_address_id = startAddress.Address_id,
                    End_address_id = endAddress.Address_id,
                    Distance_km = 100,
                    Estimated_time_hours = 2
                };
                Conn.loadMateEntities.Route.Add(route);
                Conn.loadMateEntities.SaveChanges();

                var cargo = new Cargo
                {
                    Client_id = (int)cmbClient.SelectedValue,
                    CargoType_id = (int)cmbCargoType.SelectedValue,
                    Description = txtDescription.Text,
                    Weight_kg = weight,
                    Volume_m3 = volume,
                    Created_at = DateTime.Now
                };
                Conn.loadMateEntities.Cargo.Add(cargo);
                Conn.loadMateEntities.SaveChanges();

                var tariff = (Tariff)cmbTariff.SelectedItem;
                decimal cost = (100 * tariff.Cost_per_km) +
                              (weight * tariff.Cost_per_kg) +
                              (volume * tariff.Cost_per_m3) +
                              tariff.Additional_cost;

                if (tariff.Min_price.HasValue && cost < tariff.Min_price.Value)
                    cost = tariff.Min_price.Value;

                int? truckId = cmbTruck.SelectedItem != null ? (int?)cmbTruck.SelectedValue : null;
                int? driverId = cmbDriver.SelectedItem != null ? (int?)cmbDriver.SelectedValue : null;

                var order = new Order
                {
                    Manager_id = dispatcherId,
                    Cargo_id = cargo.Cargo_id,
                    Tariff_id = tariff.Tariff_id,
                    Truck_id = truckId ?? 1,
                    Route_id = route.Route_id,
                    OrderStatus_id = 1,
                    Order_number = $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    Order_date = DateTime.Now,
                    Price = cost,
                    Scheduled_pickup = dpScheduledPickup.SelectedDate,
                    Scheduled_delivery = dpScheduledPickup.SelectedDate.Value.AddDays(2)
                };
                Conn.loadMateEntities.Order.Add(order);
                Conn.loadMateEntities.SaveChanges();

                var payment = new Payment
                {
                    Order_id = order.Order_id,
                    PaymentStatus_id = 1,
                    Amount = cost,
                    Transaction_date = DateTime.Now
                };
                Conn.loadMateEntities.Payment.Add(payment);
                Conn.loadMateEntities.SaveChanges();

                MessageBox.Show($"Заказ успешно создан!\nНомер заказа: {order.Order_number}\nСтоимость: {cost:N2} руб.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}