using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
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
    /// Логика взаимодействия для PayPage.xaml
    /// </summary>
    public partial class PayPage : Page
    {
        private Payment currentPayment;
        public PayPage(Payment payment)
        {
            InitializeComponent();
            currentPayment = payment;
            LoadInfo();
        }
        private void LoadInfo()
        {
            try
            {
                var order = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == currentPayment.Order_id);
                if (order != null)
                {
                    txtOrderNumber.Text = order.Order_number;
                    txtAmount.Text = string.Format("{0:N2} руб.", currentPayment.Amount);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
            }
        }
        private bool ValidateData()
        {
            if (txtCardNumber.Text.Replace(" ", "").Length < 16)
            {
                MessageBox.Show("Введите корректный номер карты (16 цифр).", "Валидация");
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtExpiry.Text) || !txtExpiry.Text.Contains("/"))
            {
                MessageBox.Show("Укажите срок действия в формате ММ/ГГ.", "Валидация");
                return false;
            }
            if (txtCvv.Password.Length < 3)
            {
                MessageBox.Show("Введите 3-значный код CVV.", "Валидация");
                return false;
            }
            return true;
        }
        private void Pay_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateData()) return;

            try
            {
                var paymentRecord = Conn.loadMateEntities.Payment.FirstOrDefault(p => p.Payment_id == currentPayment.Payment_id);

                if (paymentRecord != null)
                {
                    paymentRecord.PaymentStatus_id = 2;
                    paymentRecord.Paid_date = DateTime.Now;
                    paymentRecord.Payment_method = (cmbPaymentMethod.SelectedItem as ComboBoxItem)?.Content.ToString();
                    Conn.loadMateEntities.SaveChanges();
                    SendEmailReceipt(paymentRecord);
                    MessageBox.Show("Оплата успешно завершена! Чек отправлен на вашу почту.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при совершении платежа: " + ex.Message);
            }
        }
        private void SendEmailReceipt(Payment payment)
        {
            try
            {
                var order = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == payment.Order_id);
                if (order == null) return;
                var cargo = Conn.loadMateEntities.Cargo.FirstOrDefault(c => c.Cargo_id == order.Cargo_id);
                if (cargo == null) return;
                var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
                string clientEmail = user?.Email;
                if (string.IsNullOrEmpty(clientEmail)) return;
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate: Оплата заказа");
                mail.To.Add(clientEmail);
                mail.Subject = "Электронный чек по заказу №" + order.Order_number;
                mail.Body = $"Уважаемый(ая) {user.Full_name}!\n\n" +
                            $"Ваш платеж успешно принят.\n" +
                            $"Номер заказа: {order.Order_number}\n" +
                            $"Сумма платежа: {payment.Amount:N2} руб.\n" +
                            $"Дата: {payment.Paid_date:f}\n" +
                            $"Способ оплаты: {payment.Payment_method}\n\n" +
                            $"Благодарим за использование сервиса LoadMate!";
                SmtpClient client = new SmtpClient("smtp.mail.ru", 587);
                client.EnableSsl = true;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "Bmz8qEIDckirBNY5cEmE");

                client.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка отправки Email: " + ex.Message);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}
