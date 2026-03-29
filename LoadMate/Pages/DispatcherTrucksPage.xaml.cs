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
            LoadTrucks();
        }

        private void LoadTrucks()
        {
            var trucks = Conn.loadMateEntities.Truck.ToList();

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
            string search = txtSearch.Text.Trim();
            var trucks = Conn.loadMateEntities.Truck.ToList();

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

            if (!string.IsNullOrEmpty(search))
            {
                trucksWithDetails = trucksWithDetails.Where(t =>
                    t.Model.Contains(search) ||
                    t.Registration_number.Contains(search)).ToList();
            }

            if (cmbStatusFilter.SelectedItem is ComboBoxItem selected && selected.Content.ToString() != "Все статусы")
            {
                string statusName = selected.Content.ToString();
                var status = Conn.loadMateEntities.TruckStatus.FirstOrDefault(ts => ts.Name == statusName);
                if (status != null)
                {
                    trucksWithDetails = trucksWithDetails.Where(t => t.TruckStatus_id == status.TruckStatus_id).ToList();
                }
            }

            TrucksGrid.ItemsSource = trucksWithDetails;
        }
    }
}