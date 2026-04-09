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

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для DispatcherDriversPage.xaml
    /// </summary>
    public partial class DispatcherDriversPage : Page
    {
        public DispatcherDriversPage()
        {
            InitializeComponent();
            LoadStatusFilter();
            LoadDrivers();
        }

        private void LoadStatusFilter()
        {
            try
            {
                var statuses = Conn.loadMateEntities.DriverStatus.ToList();
                statuses.Insert(0, new DriverStatus { DriverStatus_id = 0, Name = "Все статусы" });
                cmbStatusFilter.ItemsSource = statuses;
                cmbStatusFilter.SelectedValuePath = "DriverStatus_id";
                cmbStatusFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статусов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDrivers()
        {
            try
            {
                var drivers = Conn.loadMateEntities.Driver
                    .Include(d => d.User)
                    .Include(d => d.DriverStatus)
                    .ToList();

                UpdateGrid(drivers);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки водителей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateGrid(List<Driver> drivers)
        {
            var driversWithDetails = drivers.Select(d => new
            {
                d.Driver_id,
                d.License_number,
                d.Experience_years,
                DriverName = d.User?.Full_name ?? "Не указан",
                Phone = d.User?.Phone ?? "Не указан",
                StatusName = d.DriverStatus?.Name ?? "Не указан",
                StatusId = d.DriverStatus_id
            }).ToList();

            DriversGrid.ItemsSource = driversWithDetails;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDrivers();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            try
            {
                string search = txtSearch.Text.Trim().ToLower();
                var query = Conn.loadMateEntities.Driver
                    .Include(d => d.User)
                    .Include(d => d.DriverStatus)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(d => d.User.Full_name.ToLower().Contains(search) ||
                                             d.User.Phone.Contains(search));
                }

                if (cmbStatusFilter.SelectedItem is DriverStatus selectedStatus && selectedStatus.DriverStatus_id != 0)
                {
                    query = query.Where(d => d.DriverStatus_id == selectedStatus.DriverStatus_id);
                }

                UpdateGrid(query.ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}