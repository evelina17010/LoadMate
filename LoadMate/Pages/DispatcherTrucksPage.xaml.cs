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
    /// Логика взаимодействия для DispatcherTrucksPage.xaml
    /// </summary>
    public partial class DispatcherTrucksPage : Page
    {
        public DispatcherTrucksPage()
        {
            InitializeComponent();
            LoadStatusFilter();
            LoadTrucks();
        }

        private void LoadStatusFilter()
        {
            try
            {
                var statuses = Conn.loadMateEntities.TruckStatus.ToList();
                statuses.Insert(0, new TruckStatus { TruckStatus_id = 0, Name = "Все статусы" });
                cmbStatusFilter.ItemsSource = statuses;
                cmbStatusFilter.SelectedValuePath = "TruckStatus_id";
                cmbStatusFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статусов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTrucks()
        {
            try
            {
                var trucks = Conn.loadMateEntities.Truck.ToList();
                UpdateGrid(trucks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки транспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateGrid(List<Truck> trucks)
        {
            var trucksWithDetails = trucks.Select(t => new
            {
                t.Truck_id,
                t.Model,
                t.Registration_number,
                t.Capacity_kg,
                t.Capacity_m3,
                t.TruckStatus_id,
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
            return user != null ? user.Full_name : "Не назначен";
        }

        private string GetTruckStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.TruckStatus.FirstOrDefault(ts => ts.TruckStatus_id == statusId);
            return status != null ? status.Name : "Не указан";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTrucks();
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
                string search = txtSearch.Text.Trim();
                var trucks = Conn.loadMateEntities.Truck.ToList();

                if (!string.IsNullOrEmpty(search))
                {
                    trucks = trucks.Where(t =>
                        t.Model.Contains(search) ||
                        t.Registration_number.Contains(search)).ToList();
                }

                if (cmbStatusFilter.SelectedItem is TruckStatus selectedStatus && selectedStatus.TruckStatus_id != 0)
                {
                    trucks = trucks.Where(t => t.TruckStatus_id == selectedStatus.TruckStatus_id).ToList();
                }

                UpdateGrid(trucks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}