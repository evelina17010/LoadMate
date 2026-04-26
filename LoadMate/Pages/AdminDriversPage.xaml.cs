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
using System.IO;

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
            try
            {
                string search = txtSearch.Text.Trim().ToLower();
                var drivers = Conn.loadMateEntities.Driver
                    .Include(d => d.User)
                    .Include(d => d.DriverStatus)
                    .ToList();

                var driversWithDetails = drivers.Select(d => new
                {
                    d.Driver_id,
                    d.License_number,
                    d.Experience_years,
                    d.Hire_date,
                    DriverName = d.User?.Full_name ?? "Не указан",
                    Phone = d.User?.Phone ?? "Не указан",
                    Email = d.User?.Email ?? "Не указан",
                    StatusName = d.DriverStatus?.Name ?? "Не указан"
                }).ToList();

                if (!string.IsNullOrEmpty(search))
                {
                    driversWithDetails = driversWithDetails.Where(d =>
                        (d.DriverName != null && d.DriverName.ToLower().Contains(search)) ||
                        (d.Phone != null && d.Phone.ToLower().Contains(search)) ||
                        (d.Email != null && d.Email.ToLower().Contains(search))).ToList();
                }

                DriversGrid.ItemsSource = driversWithDetails;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DriversGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
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
                else
                {
                    selectedDriver = null;
                }
            }
            catch
            {
                selectedDriver = null;
            }
        }

        private void AddDriver_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddDriverWindow();
                addWindow.Owner = Application.Current.MainWindow;
                if (addWindow.ShowDialog() == true)
                {
                    LoadDrivers();
                    MessageBox.Show("Водитель добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditDriver_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Выберите водителя", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var editWindow = new EditDriverWindow(selectedDriver);
                editWindow.Owner = Application.Current.MainWindow;
                if (editWindow.ShowDialog() == true)
                {
                    LoadDrivers();
                    MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteDriver_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Выберите водителя из списка", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить водителя {selectedDriver.User?.Full_name ?? ""}?\nЭто удалит его учетную запись и историю активности.",
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
                    if (loginEntry != null) db.Login.Remove(loginEntry);

                    var driverToDelete = db.Driver.FirstOrDefault(d => d.Driver_id == currentDriverId);
                    if (driverToDelete != null) db.Driver.Remove(driverToDelete);

                    db.SaveChanges();

                    var userToDelete = db.User.FirstOrDefault(u => u.User_id == currentUserId);
                    if (userToDelete != null) db.User.Remove(userToDelete);

                    db.SaveChanges();

                    selectedDriver = null;
                    LoadDrivers();
                    MessageBox.Show("Водитель успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.InnerException?.InnerException?.Message ?? ex.Message;
                    MessageBox.Show($"Критическая ошибка: {errorMessage}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ExportDrivers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var items = DriversGrid.ItemsSource as IEnumerable<dynamic>;
                if (items == null || !items.Any())
                {
                    MessageBox.Show("Нет данных для экспорта", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sfd = new Microsoft.Win32.SaveFileDialog()
                {
                    Filter = "CSV файл (*.csv)|*.csv",
                    FileName = $"Список_водителей_{DateTime.Now:dd_MM_yyyy}"
                };

                if (sfd.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("ID;ФИО;Телефон;Email;Номер лицензии;Стаж (лет);Статус;Дата приема");

                    foreach (var d in items)
                    {
                        csv.AppendLine($"{d.Driver_id};" +
                                       $"{d.DriverName};" +
                                       $"{d.Phone};" +
                                       $"{d.Email};" +
                                       $"{d.License_number};" +
                                       $"{d.Experience_years};" +
                                       $"{d.StatusName};" +
                                       $"{d.Hire_date:dd.MM.yyyy}");
                    }
                    File.WriteAllText(sfd.FileName, csv.ToString(), Encoding.UTF8);

                    MessageBox.Show("Список водителей успешно экспортирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadDrivers();

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => LoadDrivers();
    }
}