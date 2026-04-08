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
            try
            {
                string search = txtSearch.Text.Trim().ToLower();
                var trucks = Conn.loadMateEntities.Truck
                    .Include(t => t.Driver.User)
                    .Include(t => t.TruckStatus)
                    .ToList();

                var trucksWithDetails = trucks.Select(t => new
                {
                    t.Truck_id,
                    t.Model,
                    t.Registration_number,
                    t.Capacity_kg,
                    t.Capacity_m3,
                    t.Dimensions,
                    DriverName = t.Driver?.User?.Full_name ?? "Не назначен",
                    StatusName = t.TruckStatus?.Name ?? "Не указан"
                }).ToList();

                if (!string.IsNullOrEmpty(search))
                {
                    trucksWithDetails = trucksWithDetails.Where(t =>
                        (t.Model != null && t.Model.ToLower().Contains(search)) ||
                        (t.Registration_number != null && t.Registration_number.ToLower().Contains(search))).ToList();
                }

                TrucksGrid.ItemsSource = trucksWithDetails;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке транспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TrucksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
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
                else
                {
                    selectedTruck = null;
                }
            }
            catch
            {
                selectedTruck = null;
            }
        }

        private void AddTruck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddTruckWindow();
                addWindow.Owner = Application.Current.MainWindow;
                if (addWindow.ShowDialog() == true)
                {
                    LoadTrucks();
                    MessageBox.Show("Транспорт добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTruck_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTruck == null)
            {
                MessageBox.Show("Выберите транспорт", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var editWindow = new EditTruckWindow(selectedTruck);
                editWindow.Owner = Application.Current.MainWindow;
                if (editWindow.ShowDialog() == true)
                {
                    LoadTrucks();
                    MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                try
                {
                    Conn.loadMateEntities.Truck.Remove(selectedTruck);
                    Conn.loadMateEntities.SaveChanges();
                    LoadTrucks();
                    MessageBox.Show("Транспорт удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось удалить транспорт. Возможно, он назначен на активный заказ. {ex.Message}", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadTrucks();

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => LoadTrucks();
    }
}