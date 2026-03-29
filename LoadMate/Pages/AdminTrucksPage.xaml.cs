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
    /// Логика взаимодействия для AdminTrucksPage.xaml
    /// </summary>
    public partial class AdminTrucksPage : Page
    {
        private Truck selectedTruck;

        public AdminTrucksPage()
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
                t.Dimensions,
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

        private void TrucksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = TrucksGrid.SelectedItem;
            if (selected != null)
            {
                var property = selected.GetType().GetProperty("Truck_id");
                if (property != null)
                {
                    int truckId = (int)property.GetValue(selected);
                    selectedTruck = Conn.loadMateEntities.Truck.FirstOrDefault(t => t.Truck_id == truckId);
                }
            }
        }

        private void AddTruck_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddTruckWindow();
            addWindow.Owner = Application.Current.MainWindow;
            if (addWindow.ShowDialog() == true)
            {
                LoadTrucks();
                MessageBox.Show("Транспорт добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditTruck_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTruck == null)
            {
                MessageBox.Show("Выберите транспорт", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editWindow = new EditTruckWindow(selectedTruck);
            editWindow.Owner = Application.Current.MainWindow;
            if (editWindow.ShowDialog() == true)
            {
                LoadTrucks();
                MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteTruck_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTruck == null)
            {
                MessageBox.Show("Выберите транспорт", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Удалить транспорт {selectedTruck.Model}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Conn.loadMateEntities.Truck.Remove(selectedTruck);
                Conn.loadMateEntities.SaveChanges();
                LoadTrucks();
                MessageBox.Show("Транспорт удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTrucks();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
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
                t.Dimensions,
                DriverName = GetDriverName(t.Driver_id),
                StatusName = GetTruckStatusName(t.TruckStatus_id)
            }).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                trucksWithDetails = trucksWithDetails.Where(t =>
                    t.Model.Contains(search) ||
                    t.Registration_number.Contains(search)).ToList();
            }

            TrucksGrid.ItemsSource = trucksWithDetails;
        }
    }
}