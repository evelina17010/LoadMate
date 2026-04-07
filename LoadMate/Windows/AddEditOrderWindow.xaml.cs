using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LoadMate.DBConn;

namespace LoadMate.Windows
{
    /// <summary>
    /// Логика взаимодействия для AddEditOrderWindow.xaml
    /// </summary>
    public partial class AddEditOrderWindow : Window
    {
        private Order _currentOrder;
        private bool _isEdit = false;

        public AddEditOrderWindow(Order order = null)
        {
            InitializeComponent();
            LoadData();

            if (order != null)
            {
                _isEdit = true;
                _currentOrder = order;
                txtTitle.Text = "Редактирование заказа";
                FillFields();
            }
            else
            {
                _isEdit = false;
                _currentOrder = new Order();
                tbOrderNumber.Text = $"ORD-{DateTime.Now:yyyyMMddHHmm}";
            }
        }

        private void LoadData()
        {
            try
            {
                var db = Conn.loadMateEntities;
                cmbClient.ItemsSource = db.User.Where(u => u.Role_id == 2).ToList();
                cmbTariff.ItemsSource = db.Tariff.Where(t => t.Is_active == true).ToList();

                var cities = db.City.ToList();
                cmbStartCity.ItemsSource = cities;
                cmbEndCity.ItemsSource = cities;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
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
            }
            else if (cmb == cmbEndCity)
            {
                cmbEndStreet.ItemsSource = streets;
            }
        }

        private void FillFields()
        {
            var db = Conn.loadMateEntities;
            var cargo = db.Cargo.Find(_currentOrder.Cargo_id);
            var route = db.Route.Find(_currentOrder.Route_id);

            tbOrderNumber.Text = _currentOrder.Order_number;
            cmbTariff.SelectedValue = _currentOrder.Tariff_id;

            if (cargo != null)
            {
                cmbClient.SelectedValue = cargo.Client_id;
                tbCargoDesc.Text = cargo.Description;
                tbWeight.Text = cargo.Weight_kg.ToString();
                tbVolume.Text = cargo.Volume_m3.ToString();
            }

            if (route != null)
            {
                var startAddr = db.Address.Find(route.Start_address_id);
                var endAddr = db.Address.Find(route.End_address_id);

                if (startAddr != null)
                {
                    cmbStartCity.SelectedValue = startAddr.Street.City_id;
                    cmbStartStreet.ItemsSource = db.Street.Where(s => s.City_id == startAddr.Street.City_id).ToList();
                    cmbStartStreet.SelectedValue = startAddr.Street_id;
                    txtStartHouse.Text = startAddr.House_number;
                }
                if (endAddr != null)
                {
                    cmbEndCity.SelectedValue = endAddr.Street.City_id;
                    cmbEndStreet.ItemsSource = db.Street.Where(s => s.City_id == endAddr.Street.City_id).ToList();
                    cmbEndStreet.SelectedValue = endAddr.Street_id;
                    txtEndHouse.Text = endAddr.House_number;
                }
            }
            UpdatePrice();
        }

        private void UpdateCost_Event(object sender, EventArgs e) => UpdatePrice();

        private void UpdatePrice()
        {
            if (cmbTariff?.SelectedItem is Tariff t)
            {
                decimal.TryParse(tbWeight.Text.Replace(".", ","), out decimal w);
                decimal.TryParse(tbVolume.Text.Replace(".", ","), out decimal v);

                decimal cost = (100 * t.Cost_per_km) + (w * t.Cost_per_kg) + (v * t.Cost_per_m3) + t.Additional_cost;
                decimal finalCost = (t.Min_price.HasValue && cost < t.Min_price.Value) ? t.Min_price.Value : cost;

                tbPriceDisplay.Text = $"Стоимость: {finalCost:N2} руб.";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (cmbStartStreet.SelectedValue == null || cmbEndStreet.SelectedValue == null || cmbClient.SelectedValue == null || cmbTariff.SelectedValue == null)
            {
                MessageBox.Show("Заполните все поля адресов, клиента и тарифа!", "Валидация");
                return;
            }

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

                    Cargo cargo = _isEdit ? db.Cargo.Find(_currentOrder.Cargo_id) : new Cargo { Created_at = DateTime.Now };
                    cargo.Client_id = (int)cmbClient.SelectedValue;
                    cargo.Description = tbCargoDesc.Text;
                    cargo.Weight_kg = decimal.Parse(tbWeight.Text.Replace(".", ","));
                    cargo.Volume_m3 = decimal.Parse(tbVolume.Text.Replace(".", ","));
                    cargo.CargoType_id = 1;

                    if (!_isEdit) db.Cargo.Add(cargo);
                    db.SaveChanges();
                    var tariff = (Tariff)cmbTariff.SelectedItem;
                    if (!_isEdit)
                    {
                        _currentOrder.Order_number = tbOrderNumber.Text;
                        _currentOrder.Order_date = DateTime.Now;
                        _currentOrder.Cargo_id = cargo.Cargo_id;
                        _currentOrder.OrderStatus_id = 1;
                        _currentOrder.Manager_id = 1;
                        _currentOrder.Truck_id = 1;
                        db.Order.Add(_currentOrder);
                    }

                    _currentOrder.Route_id = route.Route_id;
                    _currentOrder.Tariff_id = tariff.Tariff_id;
                    decimal rawPrice = (100 * tariff.Cost_per_km) + (cargo.Weight_kg * tariff.Cost_per_kg) + (cargo.Volume_m3 * tariff.Cost_per_m3) + tariff.Additional_cost;
                    _currentOrder.Price = (tariff.Min_price.HasValue && rawPrice < tariff.Min_price.Value) ? tariff.Min_price.Value : rawPrice;

                    db.SaveChanges();
                    scope.Complete();

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}