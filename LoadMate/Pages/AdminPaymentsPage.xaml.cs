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
using System.Data.Entity;
using System.IO;

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для AdminPaymentsPage.xaml
    /// </summary>
    public partial class AdminPaymentsPage : Page
    {
        private List<Payment> _rawData;

        public AdminPaymentsPage()
        {
            InitializeComponent();
            LoadFilterData();
            LoadPayments();
        }

        private void LoadFilterData()
        {
            try
            {
                var statuses = Conn.loadMateEntities.PaymentStatus.ToList();
                statuses.Insert(0, new PaymentStatus { PaymentStatus_id = 0, Name = "Все статусы" });
                cmbStatusFilter.ItemsSource = statuses;
                cmbStatusFilter.SelectedValuePath = "PaymentStatus_id";
                cmbStatusFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке фильтров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPayments()
        {
            try
            {
                _rawData = Conn.loadMateEntities.Payment
                    .Include(p => p.Order.Cargo.User)
                    .Include(p => p.PaymentStatus)
                    .ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке платежей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_rawData == null) return;

                string search = txtSearch.Text.Trim().ToLower();
                int selectedStatusId = cmbStatusFilter.SelectedValue != null ? (int)cmbStatusFilter.SelectedValue : 0;

                var filteredData = _rawData.Where(p =>
                {
                    bool matchesStatus = (selectedStatusId == 0) || (p.PaymentStatus_id == selectedStatusId);

                    string orderNum = p.Order?.Order_number?.ToLower() ?? "";
                    string client = p.Order?.Cargo?.User?.Full_name?.ToLower() ?? "";
                    string method = p.Payment_method?.ToLower() ?? "";

                    bool matchesSearch = string.IsNullOrEmpty(search) ||
                                        orderNum.Contains(search) ||
                                        client.Contains(search) ||
                                        method.Contains(search);

                    return matchesStatus && matchesSearch;
                });

                PaymentsGrid.ItemsSource = filteredData.Select(p => new
                {
                    p.Payment_id,
                    OrderNumber = p.Order?.Order_number ?? "—",
                    ClientName = p.Order?.Cargo?.User?.Full_name ?? "—",
                    p.Amount,
                    StatusName = p.PaymentStatus?.Name ?? "—",
                    p.PaymentStatus_id,
                    p.Payment_method,
                    p.Transaction_date,
                    p.Paid_date
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtSearch.Text = "";
                cmbStatusFilter.SelectedIndex = 0;
                LoadPayments();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void ExportPayments_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var items = PaymentsGrid.ItemsSource as IEnumerable<dynamic>;
                if (items == null || !items.Any())
                {
                    MessageBox.Show("Нет данных для экспорта", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sfd = new Microsoft.Win32.SaveFileDialog()
                {
                    Filter = "CSV файл (*.csv)|*.csv",
                    FileName = $"Payments_Report_{DateTime.Now:dd_MM_yyyy}"
                };

                if (sfd.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("ID;№ Заказа;Клиент;Сумма;Статус;Метод оплаты;Дата транзакции;Дата оплаты");

                    foreach (var p in items)
                    {
                        csv.AppendLine($"{p.Payment_id};" +
                                       $"{p.OrderNumber};" +
                                       $"{p.ClientName};" +
                                       $"{p.Amount:F2};" +
                                       $"{p.StatusName};" +
                                       $"{p.Payment_method};" +
                                       $"{p.Transaction_date:dd.MM.yyyy HH:mm};" +
                                       $"{p.Paid_date:dd.MM.yyyy HH:mm}");
                    }

                    File.WriteAllText(sfd.FileName, csv.ToString(), Encoding.UTF8);
                    MessageBox.Show("Отчет по платежам успешно сформирован", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Search_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    }
}