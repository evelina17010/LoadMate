using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LoadMate.DBConn;
using LoadMate.Windows;

namespace LoadMate.Pages
{
    public partial class ClientPaymentsPage : Page
    {
        private int clientId;
        private Payment selectedPayment;

        public ClientPaymentsPage(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;
            LoadPayments();
        }

        private void LoadPayments()
        {
            var cargoIds = Conn.loadMateEntities.Cargo
                .Where(c => c.Client_id == clientId)
                .Select(c => c.Cargo_id)
                .ToList();

            var orderIds = Conn.loadMateEntities.Order
                .Where(o => cargoIds.Contains(o.Cargo_id))
                .Select(o => o.Order_id)
                .ToList();

            var payments = Conn.loadMateEntities.Payment
                .Where(p => orderIds.Contains(p.Order_id))
                .ToList();

            var paymentsWithDetails = payments.Select(p => new
            {
                p.Payment_id,
                p.Order_id,
                p.Amount,
                p.Transaction_date,
                p.Payment_method,
                OrderNumber = GetOrderNumber(p.Order_id),
                StatusName = GetPaymentStatusName(p.PaymentStatus_id)
            }).ToList();

            PaymentsGrid.ItemsSource = paymentsWithDetails;
        }

        private string GetOrderNumber(int orderId)
        {
            var order = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == orderId);
            return order != null ? order.Order_number : "Не указан";
        }

        private string GetPaymentStatusName(int statusId)
        {
            var status = Conn.loadMateEntities.PaymentStatus.FirstOrDefault(ps => ps.PaymentStatus_id == statusId);
            return status != null ? status.Name : "Не указан";
        }

        private void PaymentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = PaymentsGrid.SelectedItem;
            if (selected != null)
            {
                var property = selected.GetType().GetProperty("Payment_id");
                if (property != null)
                {
                    int paymentId = (int)property.GetValue(selected);
                    selectedPayment = Conn.loadMateEntities.Payment.FirstOrDefault(p => p.Payment_id == paymentId);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPayments();
        }

        private void Pay_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPayment == null)
            {
                MessageBox.Show("Выберите платеж для оплаты", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selectedPayment.PaymentStatus_id == 2)
            {
                MessageBox.Show("Этот платеж уже оплачен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var payWindow = new PayWindow(selectedPayment);
            payWindow.Owner = Application.Current.MainWindow;
            if (payWindow.ShowDialog() == true)
            {
                selectedPayment.PaymentStatus_id = 2;
                selectedPayment.Paid_date = DateTime.Now;
                Conn.loadMateEntities.SaveChanges();
                LoadPayments();
                MessageBox.Show("Оплата прошла успешно", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}