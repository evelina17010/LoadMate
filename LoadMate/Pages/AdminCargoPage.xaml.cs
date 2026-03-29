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
    /// Логика взаимодействия для AdminCargoPage.xaml
    /// </summary>
    public partial class AdminCargoPage : Page
    {
        private Cargo selectedCargo;

        public AdminCargoPage()
        {
            InitializeComponent();
            LoadCargo();
        }

        private void LoadCargo()
        {
            var cargoList = Conn.loadMateEntities.Cargo.ToList();

            var cargoWithDetails = cargoList.Select(c => new
            {
                c.Cargo_id,
                ClientName = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == c.Client_id)?.Full_name ?? "Не указан",
                CargoTypeName = Conn.loadMateEntities.CargoType.FirstOrDefault(ct => ct.CargoType_id == c.CargoType_id)?.Name ?? "Не указан",
                c.Description,
                c.Weight_kg,
                c.Volume_m3,
                c.Is_fragile,
                c.Is_dangerous,
                c.Created_at
            }).ToList();

            CargoGrid.ItemsSource = cargoWithDetails;
        }

        private void CargoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = CargoGrid.SelectedItem;
            if (selected != null)
            {
                var property = selected.GetType().GetProperty("Cargo_id");
                if (property != null)
                {
                    int cargoId = (int)property.GetValue(selected);
                    selectedCargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == cargoId);
                }
            }
        }

        private void AddCargo_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new Windows.AddCargoWindow();
            addWindow.Owner = Application.Current.MainWindow;
            if (addWindow.ShowDialog() == true)
            {
                LoadCargo();
                MessageBox.Show("Груз добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditCargo_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCargo == null)
            {
                MessageBox.Show("Выберите груз", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editWindow = new Windows.EditCargoWindow(selectedCargo);
            editWindow.Owner = Application.Current.MainWindow;
            if (editWindow.ShowDialog() == true)
            {
                LoadCargo();
                MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteCargo_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCargo == null)
            {
                MessageBox.Show("Выберите груз", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Удалить груз \"{selectedCargo.Description}\"?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Conn.loadMateEntities.Cargo.Remove(selectedCargo);
                Conn.loadMateEntities.SaveChanges();
                LoadCargo();
                MessageBox.Show("Груз удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCargo();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtSearch.Text.Trim();
            var cargoList = Conn.loadMateEntities.Cargo.ToList();

            var cargoWithDetails = cargoList.Select(c => new
            {
                c.Cargo_id,
                ClientName = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == c.Client_id)?.Full_name ?? "Не указан",
                CargoTypeName = Conn.loadMateEntities.CargoType.FirstOrDefault(ct => ct.CargoType_id == c.CargoType_id)?.Name ?? "Не указан",
                c.Description,
                c.Weight_kg,
                c.Volume_m3,
                c.Is_fragile,
                c.Is_dangerous,
                c.Created_at
            }).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                cargoWithDetails = cargoWithDetails.Where(c =>
                    c.Description.Contains(search) ||
                    c.ClientName.Contains(search)).ToList();
            }

            CargoGrid.ItemsSource = cargoWithDetails;
        }
    }
}