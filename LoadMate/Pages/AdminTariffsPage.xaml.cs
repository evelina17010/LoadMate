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
    /// Логика взаимодействия для AdminTariffsPage.xaml
    /// </summary>
    public partial class AdminTariffsPage : Page
    {
        private Tariff selectedTariff;

        public AdminTariffsPage()
        {
            InitializeComponent();
            LoadTariffs();
        }

        private void LoadTariffs()
        {
            try
            {
                var tariffs = Conn.loadMateEntities.Tariff.ToList();
                TariffsGrid.ItemsSource = tariffs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке тарифов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TariffsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                selectedTariff = TariffsGrid.SelectedItem as Tariff;
            }
            catch
            {
                selectedTariff = null;
            }
        }

        private void AddTariff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddTariffWindow();
                addWindow.Owner = Application.Current.MainWindow;
                if (addWindow.ShowDialog() == true)
                {
                    LoadTariffs();
                    MessageBox.Show("Тариф добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении тарифа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTariff_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTariff == null)
            {
                MessageBox.Show("Выберите тариф", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var editWindow = new EditTariffWindow(selectedTariff);
                editWindow.Owner = Application.Current.MainWindow;
                if (editWindow.ShowDialog() == true)
                {
                    LoadTariffs();
                    MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании тарифа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteTariff_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTariff == null)
            {
                MessageBox.Show("Выберите тариф", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Удалить тариф {selectedTariff.Name}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Conn.loadMateEntities.Tariff.Remove(selectedTariff);
                    Conn.loadMateEntities.SaveChanges();
                    LoadTariffs();
                    MessageBox.Show("Тариф удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось удалить тариф. Возможно, он используется в других записях. {ex.Message}", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTariffs();
        }
    }
}