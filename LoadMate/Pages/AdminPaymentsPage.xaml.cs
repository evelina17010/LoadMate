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
    /// Логика взаимодействия для AdminPaymentsPage.xaml
    /// </summary>
    public partial class AdminPaymentsPage : Page
    {
        public AdminPaymentsPage()
        {
            InitializeComponent();
            LoadPayments();
        }

        private void LoadPayments()
        {
            var payments = Conn.loadMateEntities.Payment.ToList();

            var paymentsWithDetails = payments.Select(p => new
            {
                p.Payment_id,
                p.Order_id,
                p.Amount,
                p.Payment_method,
                p.Transaction_date,
                p.Paid_date,
                p.PaymentStatus_id,
                OrderNumber = GetOrderNumber(p.Order_id),
                ClientName = GetClientName(p.Order_id),
                StatusName = GetPaymentStatusName(p.PaymentStatus_id)
            }).ToList();

            PaymentsGrid.ItemsSource = paymentsWithDetails;
        }

        private string GetOrderNumber(int orderId)
        {
            var order = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == orderId);
            return order != null ? order.Order_number : "Не указан";
        }

        private string GetClientName(int orderId)
        {
            var order = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == orderId);
            if (order == null) return "Не указан";

            var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == order.Cargo_id);
            if (cargo == null) return "Не указан";

            var client = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
            return client != null ? client.Full_name : "Не указан";
        }

        private string GetPaymentStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.PaymentStatus.FirstOrDefault(ps => ps.PaymentStatus_id == statusId);
            return status != null ? status.Name : "Не указан";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPayments();
        }

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var payments = Conn.loadMateEntities.Payment.ToList();

            var paymentsWithDetails = payments.Select(p => new
            {
                p.Payment_id,
                p.Order_id,
                p.Amount,
                p.Payment_method,
                p.Transaction_date,
                p.Paid_date,
                p.PaymentStatus_id,
                OrderNumber = GetOrderNumber(p.Order_id),
                ClientName = GetClientName(p.Order_id),
                StatusName = GetPaymentStatusName(p.PaymentStatus_id)
            }).ToList();

            if (cmbStatusFilter.SelectedItem is ComboBoxItem selected && selected.Content.ToString() != "Все статусы")
            {
                string statusName = selected.Content.ToString();
                var status = Conn.loadMateEntities.PaymentStatus.FirstOrDefault(ps => ps.Name == statusName);
                if (status != null)
                {
                    paymentsWithDetails = paymentsWithDetails.Where(p => p.PaymentStatus_id == status.PaymentStatus_id).ToList();
                }
            }

            PaymentsGrid.ItemsSource = paymentsWithDetails;
        }
    }
}