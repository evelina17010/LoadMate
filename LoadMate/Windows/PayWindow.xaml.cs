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
    /// Логика взаимодействия для PayWindow.xaml
    /// </summary>
    public partial class PayWindow : Window
    {
        private Payment currentPayment;

        public PayWindow(Payment payment)
        {
            InitializeComponent();
            currentPayment = payment;
            LoadPaymentInfo();
        }

        private void LoadPaymentInfo()
        {
            var order = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == currentPayment.Order_id);
            txtOrderNumber.Text = order?.Order_number ?? "Не указан";
            txtAmount.Text = $"{currentPayment.Amount:N2} руб.";

            var status = Conn.loadMateEntities.PaymentStatus.FirstOrDefault(ps => ps.PaymentStatus_id == currentPayment.PaymentStatus_id);
            txtStatus.Text = status?.Name ?? "Не указан";
        }

        private void Pay_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCardNumber.Text) || txtCardNumber.Text.Replace(" ", "").Length < 16)
            {
                MessageBox.Show("Введите корректный номер карты", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtExpiry.Text) || txtExpiry.Text.Length < 5)
            {
                MessageBox.Show("Введите срок действия карты", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtCvv.Password) || txtCvv.Password.Length < 3)
            {
                MessageBox.Show("Введите CVV/CVC код", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}