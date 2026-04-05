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
    /// Логика взаимодействия для EditTariffWindow.xaml
    /// </summary>
    public partial class EditTariffWindow : Window
    {
        private Tariff currentTariff;
        public EditTariffWindow(Tariff tariff)
        {
            InitializeComponent();
            currentTariff = tariff;
            LoadTariffData();
        }
        private void LoadTariffData()
        {
            txtName.Text = currentTariff.Name;
            txtDescription.Text = currentTariff.Description;
            txtCostPerKm.Text = currentTariff.Cost_per_km.ToString();
            txtCostPerKg.Text = currentTariff.Cost_per_kg.ToString();
            txtCostPerM3.Text = currentTariff.Cost_per_m3.ToString();
            txtAdditionalCost.Text = currentTariff.Additional_cost.ToString();
            txtMinPrice.Text = currentTariff.Min_price?.ToString() ?? "";
            chkIsActive.IsChecked = currentTariff.Is_active;
        }
        private bool TryParseDecimal(string input, out decimal result)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                result = 0;
                return false;
            }
            return decimal.TryParse(input.Replace(".", ","), out result);
        }
        private bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название тарифа", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!TryParseDecimal(txtCostPerKm.Text, out decimal costPerKm) || costPerKm < 0)
            {
                MessageBox.Show("Введите корректную стоимость за км (положительное число)", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!TryParseDecimal(txtCostPerKg.Text, out decimal costPerKg) || costPerKg < 0)
            {
                MessageBox.Show("Введите корректную стоимость за кг (положительное число)", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!TryParseDecimal(txtCostPerM3.Text, out decimal costPerM3) || costPerM3 < 0)
            {
                MessageBox.Show("Введите корректную стоимость за м³ (положительное число)", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!string.IsNullOrWhiteSpace(txtAdditionalCost.Text) && !TryParseDecimal(txtAdditionalCost.Text, out _))
            {
                MessageBox.Show("Введите корректную сумму дополнительного сбора", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
           if (!string.IsNullOrWhiteSpace(txtMinPrice.Text) && (!TryParseDecimal(txtMinPrice.Text, out decimal minPrice) || minPrice < 0))
            {
                MessageBox.Show("Введите корректную минимальную цену", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateData()) return;
                currentTariff.Name = txtName.Text.Trim();
                currentTariff.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
                if (TryParseDecimal(txtCostPerKm.Text, out decimal costKm)) currentTariff.Cost_per_km = costKm;
                if (TryParseDecimal(txtCostPerKg.Text, out decimal costKg)) currentTariff.Cost_per_kg = costKg;
                if (TryParseDecimal(txtCostPerM3.Text, out decimal costM3)) currentTariff.Cost_per_m3 = costM3;
                if (TryParseDecimal(txtAdditionalCost.Text, out decimal addCost))
                    currentTariff.Additional_cost = addCost;
                else
                    currentTariff.Additional_cost = 0;
                if (TryParseDecimal(txtMinPrice.Text, out decimal minP))
                    currentTariff.Min_price = minP;
                else
                    currentTariff.Min_price = null;
                currentTariff.Is_active = chkIsActive.IsChecked == true;
                Conn.loadMateEntities.SaveChanges();
                MessageBox.Show("Тариф успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}