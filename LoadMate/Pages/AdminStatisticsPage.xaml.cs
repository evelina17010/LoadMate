using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using LoadMate.DBConn;
using System.Data.Entity;
using System.IO;

namespace LoadMate.Pages
{
    public partial class AdminStatisticsPage : Page
    {
        public AdminStatisticsPage()
        {
            InitializeComponent();
            CalculateStatistics();
        }

        private void CalculateStatistics()
        {
            try
            {
                var db = Conn.loadMateEntities;
                var allOrders = db.Order.ToList();
                decimal totalRevenue = allOrders.Where(o => o.OrderStatus_id == 7).Sum(o => o.Price); 

                txtTotalRevenue.Text = $"{totalRevenue:N2} руб.";
                txtTotalOrders.Text = allOrders.Count.ToString();
                int activeTrucks = db.Truck.Count(t => t.TruckStatus_id == 1);
                txtActiveTrucks.Text = activeTrucks.ToString();

                decimal avgCheck = allOrders.Count > 0 ? totalRevenue / allOrders.Count : 0;
                txtAverageCheck.Text = $"{avgCheck:N2} руб.";
                var tariffStats = db.Tariff.ToList().Select(t => new
                {
                    t.Name,
                    UsageCount = db.Order.Count(o => o.Tariff_id == t.Tariff_id),
                    TotalRevenue = db.Order.Where(o => o.Tariff_id == t.Tariff_id && o.OrderStatus_id == 7).Sum(o => (decimal?)o.Price) ?? 0
                })
                .OrderByDescending(x => x.UsageCount).ToList();
                TariffStatsGrid.ItemsSource = tariffStats;
                var statusStats = db.OrderStatus.ToList().Select(s => new
                {
                    StatusName = s.Name,
                    Count = db.Order.Count(o => o.OrderStatus_id == s.OrderStatus_id)
                }).ToList();
                OrderStatusList.ItemsSource = statusStats;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете статистики: {ex.Message}");
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            CalculateStatistics();
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog()
                {
                    Filter = "CSV файл (*.csv)|*.csv",
                    FileName = $"Financial_Report_{DateTime.Now:dd_MM_yyyy}"
                };

                if (sfd.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Показатель;Значение");
                    csv.AppendLine($"Дата отчета;{DateTime.Now}");
                    csv.AppendLine($"Общая выручка;{txtTotalRevenue.Text}");
                    csv.AppendLine($"Всего заказов;{txtTotalOrders.Text}");
                    csv.AppendLine($"Средний чек;{txtAverageCheck.Text}");

                    File.WriteAllText(sfd.FileName, csv.ToString(), Encoding.UTF8);
                    MessageBox.Show("Отчет сохранен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}