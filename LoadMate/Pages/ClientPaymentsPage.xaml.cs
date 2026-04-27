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
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPayments(); 
        }
        private void LoadPayments()
        {
            var db = Conn.loadMateEntities;
            var cargoIds = db.Cargo
                .Where(c => c.Client_id == clientId)
                .Select(c => c.Cargo_id)
                .ToList();
            var orderIds = db.Order
                .Where(o => cargoIds.Contains(o.Cargo_id))
                .Select(o => o.Order_id)
                .ToList();
            var payments = db.Payment
                .Where(p => orderIds.Contains(p.Order_id))
                .OrderByDescending(p => p.Transaction_date) 
                .ToList();
            var paymentsWithDetails = payments.Select(p => new
            {
                p.Payment_id,
                p.Order_id,
                p.Amount,
                p.Transaction_date,
                p.Payment_method,
                OrderNumber = db.Order.FirstOrDefault(o => o.Order_id == p.Order_id)?.Order_number ?? "Не указан",
                StatusName = db.PaymentStatus.FirstOrDefault(ps => ps.PaymentStatus_id == p.PaymentStatus_id)?.Name ?? "Не указан"
            }).ToList();

            PaymentsGrid.ItemsSource = paymentsWithDetails;
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

            var parentOrder = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == selectedPayment.Order_id);
            if (parentOrder != null && parentOrder.OrderStatus_id == 1)
            {
                MessageBox.Show("Заказ еще не обработан диспетчером. Оплата будет доступна после назначения транспорта и подтверждения цены.",
                                "Ожидание подтверждения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PayPage payPage = new PayPage(selectedPayment);
            NavigationService.Navigate(payPage);
        }
    }

    }
