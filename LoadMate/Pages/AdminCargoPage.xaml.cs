using System;
using System.Collections.Generic;
using System.Data;
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
using System.IO;

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
            try
            {
                string search = txtSearch.Text.Trim().ToLower();

                var cargoList = Conn.loadMateEntities.Cargo
                    .Include(c => c.User)
                    .Include(c => c.CargoType)
                    .ToList();

                var cargoWithDetails = cargoList.Select(c => new
                {
                    c.Cargo_id,
                    ClientName = c.User?.Full_name ?? "Не указан",
                    CargoTypeName = c.CargoType?.Name ?? "Не указан",
                    Description = c.Description ?? "Без описания",
                    c.Weight_kg,
                    c.Volume_m3,
                    IsFragileText = c.Is_fragile ? "Да" : "Нет",
                    IsDangerousText = c.Is_dangerous ? "Да" : "Нет",
                    c.Created_at,
                    OriginalObject = c
                }).ToList();

                if (!string.IsNullOrEmpty(search))
                {
                    cargoWithDetails = cargoWithDetails.Where(c =>
                        (c.Description != null && c.Description.ToLower().Contains(search)) ||
                        (c.ClientName != null && c.ClientName.ToLower().Contains(search))).ToList();
                }

                CargoGrid.ItemsSource = cargoWithDetails;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CargoGrid.SelectedItem != null)
                {
                    dynamic selected = CargoGrid.SelectedItem;
                    selectedCargo = selected.OriginalObject;
                }
                else
                {
                    selectedCargo = null;
                }
            }
            catch
            {
                selectedCargo = null;
            }
        }

        private void AddCargo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new Windows.AddCargoWindow();
                addWindow.Owner = Application.Current.MainWindow;
                if (addWindow.ShowDialog() == true)
                {
                    LoadCargo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии окна добавления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCargo_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCargo == null)
            {
                MessageBox.Show("Выберите груз для редактирования", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var editWindow = new Windows.EditCargoWindow(selectedCargo);
                editWindow.Owner = Application.Current.MainWindow;
                if (editWindow.ShowDialog() == true)
                {
                    LoadCargo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCargo_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCargo == null)
            {
                MessageBox.Show("Выберите груз для удаления", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить груз \"{selectedCargo.Description}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Conn.loadMateEntities.Cargo.Remove(selectedCargo);
                    Conn.loadMateEntities.SaveChanges();

                    MessageBox.Show("Груз успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadCargo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось удалить груз. Возможно, он связан с существующим заказом. {ex.Message}", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
                    var entry = Conn.loadMateEntities.Entry(selectedCargo);
                    if (entry.State == EntityState.Deleted) entry.State = EntityState.Unchanged;
                }
            }
        }
        private void ExportCargo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var items = CargoGrid.ItemsSource as IEnumerable<dynamic>;
                if (items == null || !items.Any())
                {
                    MessageBox.Show("Нет данных для экспорта", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sfd = new Microsoft.Win32.SaveFileDialog()
                {
                    Filter = "CSV файл (*.csv)|*.csv",
                    FileName = $"Отчет_Грузы_{DateTime.Now:dd_MM_yyyy}"
                };

                if (sfd.ShowDialog() == true)
                {
                    var csv = new StringBuilder();

                    csv.AppendLine("ID;Клиент;Тип груза;Описание;Вес (кг);Объем (м3);Хрупкий;Опасный;Дата создания");
                    foreach (var item in items)
                    {
                        csv.AppendLine($"{item.Cargo_id};" +
                                       $"{item.ClientName};" +
                                       $"{item.CargoTypeName};" +
                                       $"{item.Description};" +
                                       $"{item.Weight_kg};" +
                                       $"{item.Volume_m3};" +
                                       $"{item.IsFragileText};" +
                                       $"{item.IsDangerousText};" +
                                       $"{item.Created_at:dd.MM.yyyy}");
                    }

                    File.WriteAllText(sfd.FileName, csv.ToString(), Encoding.UTF8);

                    MessageBox.Show("Отчет успешно выгружен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadCargo();

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => LoadCargo();
    }
}