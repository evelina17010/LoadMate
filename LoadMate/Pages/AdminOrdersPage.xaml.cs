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
using LoadMate.Windows;
using System.Data.Entity;

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для AdminOrdersPage.xaml
    /// </summary>
    public partial class AdminOrdersPage : Page
    {
        private List<Order> _allOrders;
        private dynamic selectedOrder;

        public AdminOrdersPage()
        {
            InitializeComponent();
            LoadFilterData();
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                _allOrders = Conn.loadMateEntities.Order
                    .Include(o => o.OrderStatus)
                    .Include(o => o.Cargo)
                    .Include(o => o.Route)
                    .ToList();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFilterData()
        {
            try
            {
                var statuses = Conn.loadMateEntities.OrderStatus.ToList();
                statuses.Insert(0, new OrderStatus { OrderStatus_id = 0, Name = "Все статусы" });

                cmbStatusFilter.ItemsSource = statuses;
                cmbStatusFilter.SelectedValuePath = "OrderStatus_id";
                cmbStatusFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке фильтров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allOrders == null) return;

                string search = txtSearch.Text.Trim().ToLower();
                var query = _allOrders.Select(o => new
                {
                    o.Order_id,
                    o.Order_number,
                    o.Price,
                    o.Order_date,
                    o.OrderStatus_id,
                    ClientName = GetClientName(o.Cargo_id),
                    CargoDescription = o.Cargo?.Description ?? "Не указан",
                    RouteFrom = GetRouteAddress(o.Route_id, true),
                    RouteTo = GetRouteAddress(o.Route_id, false),
                    DriverName = GetDriverName(o.Truck_id),
                    TruckModel = GetTruckModel(o.Truck_id),
                    StatusName = o.OrderStatus?.Name ?? "Не указан"
                }).AsEnumerable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(o => (o.Order_number != null && o.Order_number.ToLower().Contains(search)) ||
                                             (o.ClientName != null && o.ClientName.ToLower().Contains(search)));
                }

                if (cmbStatusFilter.SelectedValue != null)
                {
                    int selectedId = (int)cmbStatusFilter.SelectedValue;
                    if (selectedId != 0)
                    {
                        query = query.Where(o => o.OrderStatus_id == selectedId);
                    }
                }

                OrdersGrid.ItemsSource = query.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetClientName(int cargoId)
        {
            try
            {
                var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == cargoId);
                if (cargo == null) return "Не указан";
                var client = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
                return client != null ? client.Full_name : "Не указан";
            }
            catch { return "Ошибка данных"; }
        }

        private string GetRouteAddress(int routeId, bool isStart)
        {
            try
            {
                var route = Conn.loadMateEntities.Route.FirstOrDefault(r => r.Route_id == routeId);
                if (route == null) return "Не указан";

                int addressId = isStart ? route.Start_address_id : route.End_address_id;
                var address = Conn.loadMateEntities.Address
                    .Include(a => a.Street.City)
                    .FirstOrDefault(a => a.Address_id == addressId);

                if (address == null) return "Не указан";
                return $"{address.Street.City.Name}, {address.Street.Name}, {address.House_number}";
            }
            catch { return "Ошибка адреса"; }
        }

        private string GetDriverName(int truckId)
        {
            try
            {
                var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
                if (truck?.Driver_id == null) return "Не назначен";

                var driver = Conn.loadMateEntities.Driver
                    .Include(d => d.User)
                    .FirstOrDefault(d => d.Driver_id == truck.Driver_id);

                return driver?.User?.Full_name ?? "Не назначен";
            }
            catch { return "Ошибка данных"; }
        }

        private string GetTruckModel(int truckId)
        {
            try
            {
                var truck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
                return truck?.Model ?? "Не назначен";
            }
            catch { return "Ошибка данных"; }
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWin = new AddEditOrderWindow();
                if (addWin.ShowDialog() == true) LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии окна: {ex.Message}");
            }
        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ для редактирования", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                int id = selectedOrder.Order_id;
                var orderToEdit = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == id);
                var editWin = new AddEditOrderWindow(orderToEdit);
                if (editWin.ShowDialog() == true) LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}");
            }
        }

        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null) return;

            if (MessageBox.Show($"Удалить заказ №{selectedOrder.Order_number}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    int id = selectedOrder.Order_id;
                    var order = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == id);

                    if (order != null)
                    {
                        var payments = Conn.loadMateEntities.Payment.Where(p => p.Order_id == id).ToList();
                        foreach (var p in payments)
                        {
                            Conn.loadMateEntities.Payment.Remove(p);
                        }
                        Conn.loadMateEntities.Order.Remove(order);
                        Conn.loadMateEntities.SaveChanges();
                        LoadOrders();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtSearch.Text = "";
                cmbStatusFilter.SelectedIndex = 0;
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedOrder = OrdersGrid.SelectedItem;
        }
    }
}