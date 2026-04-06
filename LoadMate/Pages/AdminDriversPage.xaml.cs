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

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для AdminDriversPage.xaml
    /// </summary>
    public partial class AdminDriversPage : Page
    {
        private Driver selectedDriver;

        public AdminDriversPage()
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
                d.Hire_date,
                DriverName = GetDriverName(d.User_id),
                Phone = GetDriverPhone(d.User_id),
                Email = GetDriverEmail(d.User_id),
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

        private string GetDriverEmail(int userId)
        {
            var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == userId);
            return user?.Email ?? "Не указан";
        }

        private string GetDriverStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.DriverStatus.FirstOrDefault(ds => ds.DriverStatus_id == statusId);
            return status?.Name ?? "Не указан";
        }

        private void DriversGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = DriversGrid.SelectedItem;
            if (selected != null)
            {
                var property = selected.GetType().GetProperty("Driver_id");
                if (property != null)
                {
                    int driverId = (int)property.GetValue(selected);
                    selectedDriver = Conn.loadMateEntities.Driver.FirstOrDefault(d => d.Driver_id == driverId);
                }
            }
        }

        private void AddDriver_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddDriverWindow();
            addWindow.Owner = Application.Current.MainWindow;
            if (addWindow.ShowDialog() == true)
            {
                LoadDrivers();
                MessageBox.Show("Водитель добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditDriver_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Выберите водителя", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editWindow = new EditDriverWindow(selectedDriver);
            editWindow.Owner = Application.Current.MainWindow;
            if (editWindow.ShowDialog() == true)
            {
                LoadDrivers();
                MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteDriver_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Выберите водителя из списка", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить водителя {GetDriverName(selectedDriver.User_id)}?\nЭто удалит его учетную запись и историю активности.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var db = Conn.loadMateEntities;
                    int currentUserId = selectedDriver.User_id;
                    int currentDriverId = selectedDriver.Driver_id;
                    var tiedTrucks = db.Truck.Where(t => t.Driver_id == currentDriverId).ToList();
                    foreach (var truck in tiedTrucks)
                    {
                        truck.Driver_id = null;
                    }
                    var activityLogs = db.UserActivityLog.Where(al => al.User_id == currentUserId).ToList();
                    foreach (var log in activityLogs)
                    {
                        db.UserActivityLog.Remove(log);
                    }
                    var loginEntry = db.Login.FirstOrDefault(l => l.User_id == currentUserId);
                    if (loginEntry != null)
                    {
                        db.Login.Remove(loginEntry);
                    }
                    var driverToDelete = db.Driver.FirstOrDefault(d => d.Driver_id == currentDriverId);
                    if (driverToDelete != null)
                    {
                        db.Driver.Remove(driverToDelete);
                    }
                    db.SaveChanges();
                    var userToDelete = db.User.FirstOrDefault(u => u.User_id == currentUserId);
                    if (userToDelete != null)
                    {
                        db.User.Remove(userToDelete);
                    }
                    db.SaveChanges();
                    selectedDriver = null;
                    LoadDrivers();
                    MessageBox.Show("Водитель и все связанные данные успешно удалены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.InnerException?.InnerException?.Message ?? ex.Message;
                    MessageBox.Show($"Ошибка при удалении: {errorMessage}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDrivers();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtSearch.Text.Trim();
            var drivers = Conn.loadMateEntities.Driver.ToList();

            var driversWithDetails = drivers.Select(d => new
            {
                d.Driver_id,
                d.License_number,
                d.Experience_years,
                d.Hire_date,
                DriverName = GetDriverName(d.User_id),
                Phone = GetDriverPhone(d.User_id),
                Email = GetDriverEmail(d.User_id),
                StatusName = GetDriverStatusName(d.DriverStatus_id)
            }).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                driversWithDetails = driversWithDetails.Where(d =>
                    d.DriverName.Contains(search) ||
                    d.Phone.Contains(search) ||
                    d.Email.Contains(search)).ToList();
            }

            DriversGrid.ItemsSource = driversWithDetails;
        }
    }
}