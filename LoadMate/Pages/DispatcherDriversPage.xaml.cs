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
    /// Логика взаимодействия для DispatcherDriversPage.xaml
    /// </summary>
    public partial class DispatcherDriversPage : Page
    {
        public DispatcherDriversPage()
        {
            InitializeComponent();
            LoadDrivers();
        }

        private void LoadDrivers()
        {
            var drivers = Conn.loadMateEntities.Driver.ToList();

            var driversWithDetails = drivers.Select(d => new
            {
                d.Driver_id,
                d.License_number,
                d.Experience_years,
                DriverName = GetDriverName(d.User_id),
                Phone = GetDriverPhone(d.User_id),
                StatusName = GetDriverStatusName(d.DriverStatus_id)
            }).ToList();

            DriversGrid.ItemsSource = driversWithDetails;
        }

        private string GetDriverName(int userId)
        {
            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == userId);
            return user?.Full_name ?? "Не указан";
        }

        private string GetDriverPhone(int userId)
        {
            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == userId);
            return user?.Phone ?? "Не указан";
        }

        private string GetDriverStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.DriverStatus.FirstOrDefault(ds => ds.DriverStatus_id == statusId);
            return status?.Name ?? "Не указан";
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
            string search = txtSearch.Text.Trim();
            var drivers = Conn.loadMateEntities.Driver.ToList();

            var driversWithDetails = drivers.Select(d => new
            {
                d.Driver_id,
                d.License_number,
                d.Experience_years,
                DriverName = GetDriverName(d.User_id),
                Phone = GetDriverPhone(d.User_id),
                StatusName = GetDriverStatusName(d.DriverStatus_id),
                StatusId = d.DriverStatus_id
            }).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                driversWithDetails = driversWithDetails.Where(d =>
                    d.DriverName.Contains(search) ||
                    d.Phone.Contains(search)).ToList();
            }

            if (cmbStatusFilter.SelectedItem is ComboBoxItem selected && selected.Content.ToString() != "Все статусы")
            {
                string statusName = selected.Content.ToString();
                var status = Conn.loadMateEntities.DriverStatus.FirstOrDefault(ds => ds.Name == statusName);
                if (status != null)
                {
                    driversWithDetails = driversWithDetails.Where(d => d.StatusId == status.DriverStatus_id).ToList();
                }
            }

            DriversGrid.ItemsSource = driversWithDetails;
        }
    }
}
