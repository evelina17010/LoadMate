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
    /// Логика взаимодействия для ClientCreateOrderPage.xaml
    /// </summary>
    public partial class ClientCreateOrderPage : Page
    {
        private int clientId;

        public ClientCreateOrderPage(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;
            LoadComboBoxes();
        }

        private void LoadComboBoxes()
        {
            cmbCargoType.ItemsSource = Conn.loadMateEntities.CargoType.ToList();
            cmbCargoType.DisplayMemberPath = "Name";
            cmbCargoType.SelectedValuePath = "CargoType_id";
            if (cmbCargoType.Items.Count > 0) cmbCargoType.SelectedIndex = 0;

            cmbTariff.ItemsSource = Conn.loadMateEntities.Tariff.Where(t => t.Is_active == true).ToList();
            cmbTariff.DisplayMemberPath = "Name";
            cmbTariff.SelectedValuePath = "Tariff_id";
            if (cmbTariff.Items.Count > 0) cmbTariff.SelectedIndex = 0;

            dpScheduledPickup.SelectedDate = DateTime.Now.AddDays(1);
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

            if (string.IsNullOrWhiteSpace(txtPickupAddress.Text) || string.IsNullOrWhiteSpace(txtDeliveryAddress.Text))
            {
                txtCost.Text = "Стоимость: введите адреса";
                return;
            }

            var tariff = (Tariff)cmbTariff.SelectedItem;

            decimal distance = 100;

            txtCost.Text = $"Стоимость: {(distance * tariff.Cost_per_km + weight * tariff.Cost_per_kg + volume * tariff.Cost_per_m3 + tariff.Additional_cost):N2} руб.";
        }

        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(txtWeight.Text, out decimal weight) || weight <= 0)
                {
                    MessageBox.Show("Введите корректный вес", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtVolume.Text, out decimal volume) || volume <= 0)
                {
                    MessageBox.Show("Введите корректный объем", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDescription.Text) ||
                    string.IsNullOrWhiteSpace(txtPickupAddress.Text) ||
                    string.IsNullOrWhiteSpace(txtDeliveryAddress.Text) ||
                    dpScheduledPickup.SelectedDate == null)
                {
                    MessageBox.Show("Заполните все обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var cargo = new Cargo
                {
                    Client_id = clientId,
                    CargoType_id = (int)cmbCargoType.SelectedValue,
                    Description = txtDescription.Text,
                    Weight_kg = weight,
                    Volume_m3 = volume,
                    Is_fragile = false,
                    Is_dangerous = false,
                    Created_at = DateTime.Now
                };

                Conn.loadMateEntities.Cargo.Add(cargo);
                Conn.loadMateEntities.SaveChanges();

                var startAddress = new Address
                {
                    Street_id = 1,
                    House_number = txtPickupAddress.Text,
                    Additional_info = txtPickupAddress.Text
                };
                Conn.loadMateEntities.Address.Add(startAddress);
                Conn.loadMateEntities.SaveChanges();

                var endAddress = new Address
                {
                    Street_id = 1,
                    House_number = txtDeliveryAddress.Text,
                    Additional_info = txtDeliveryAddress.Text
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

                var dispatcher = Conn.loadMateEntities.User.FirstOrDefault(u => u.Role_id == 4);
                var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.TruckStatus_id == 1);

                var tariff = (Tariff)cmbTariff.SelectedItem;
                decimal cost = (100 * tariff.Cost_per_km) +
                              (weight * tariff.Cost_per_kg) +
                              (volume * tariff.Cost_per_m3) +
                              tariff.Additional_cost;

                if (tariff.Min_price.HasValue && cost < tariff.Min_price.Value)
                    cost = tariff.Min_price.Value;

                var order = new Order
                {
                    Manager_id = dispatcher != null ? dispatcher.User_id : 2,
                    Cargo_id = cargo.Cargo_id,
                    Tariff_id = tariff.Tariff_id,
                    Truck_id = truck != null ? truck.Truck_id : 1,
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