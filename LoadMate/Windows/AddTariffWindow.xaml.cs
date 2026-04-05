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
using System.Windows.Shapes;
using LoadMate.DBConn;

namespace LoadMate.Windows
{
    /// <summary>
    /// Логика взаимодействия для AddTariffWindow.xaml
    /// </summary>
    public partial class AddTariffWindow : Window
    {
        public AddTariffWindow()
        {
            InitializeComponent();
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Введите название тарифа", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!decimal.TryParse(txtCostPerKm.Text.Replace('.', ','), out decimal costPerKm) || costPerKm < 0)
                {
                    MessageBox.Show("Введите корректную цену за км", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!decimal.TryParse(txtCostPerKg.Text.Replace('.', ','), out decimal costPerKg) || costPerKg < 0)
                {
                    MessageBox.Show("Введите корректную цену за кг", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!decimal.TryParse(txtCostPerM3.Text.Replace('.', ','), out decimal costPerM3) || costPerM3 < 0)
                {
                    MessageBox.Show("Введите корректную цену за м³", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                decimal additionalCost = 0;
                if (!string.IsNullOrWhiteSpace(txtAdditionalCost.Text))
                {
                    if (!decimal.TryParse(txtAdditionalCost.Text.Replace('.', ','), out additionalCost) || additionalCost < 0)
                    {
                        MessageBox.Show("Доп. сбор должен быть числом", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                decimal? minPrice = null;
                if (!string.IsNullOrWhiteSpace(txtMinPrice.Text))
                {
                    if (decimal.TryParse(txtMinPrice.Text.Replace('.', ','), out decimal minPriceValue) && minPriceValue >= 0)
                    {
                        minPrice = minPriceValue;
                    }
                    else
                    {
                        MessageBox.Show("Минимальная цена должна быть числом", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                var newTariff = new Tariff
                {
                    Name = txtName.Text.Trim(),
                    Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim(),
                    Cost_per_km = costPerKm,
                    Cost_per_kg = costPerKg,
                    Cost_per_m3 = costPerM3,
                    Additional_cost = additionalCost,
                    Min_price = minPrice,
                    Is_active = chkIsActive.IsChecked == true,
                    Created_time = DateTime.Now
                };
                Conn.loadMateEntities.Tariff.Add(newTariff);
                Conn.loadMateEntities.SaveChanges();

                MessageBox.Show("Тариф успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}