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
            var order = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == currentPayment.Order_id);
            txtOrderNumber.Text = order?.Order_number ?? "—";
            txtAmount.Text = string.Format("{0:N2} руб.", currentPayment.Amount);
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
                // Обновляем данные в БД
                var paymentRecord = Conn.loadMateEntities.Payment.FirstOrDefault(p => p.Payment_id == currentPayment.Payment_id);
                if (paymentRecord != null)
                {
                    paymentRecord.PaymentStatus_id = 2; // Статус "Оплачено"
                    paymentRecord.Paid_date = DateTime.Now;
                    paymentRecord.Payment_method = (cmbPaymentMethod.SelectedItem as ComboBoxItem)?.Content.ToString();

                    Conn.loadMateEntities.SaveChanges();

                    // Отправка чека
                    SendEmailReceipt(paymentRecord);

                    MessageBox.Show("Оплата успешно завершена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении платежа: " + ex.Message);
            }
        }

        private void SendEmailReceipt(Payment payment)
        {
            try
            {
                var order = Conn.loadMateEntities.Order.FirstOrDefault(o => o.Order_id == payment.Order_id);
                var clientEmail = order?.Cargo?.Client?.User?.Email;

                if (string.IsNullOrEmpty(clientEmail)) return;

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("loadmate_system@mail.ru", "Система LoadMate");
                mail.To.Add(clientEmail);
                mail.Subject = "Квитанция об оплате заказа " + order.Order_number;
                mail.Body = $"Здравствуйте!\n\nВаш платеж на сумму {payment.Amount:N2} руб. успешно принят.\n" +
                            $"Дата: {payment.Paid_date:dd.MM.yyyy HH:mm}\n" +
                            $"Способ: {payment.Payment_method}\n\nСпасибо, что выбрали LoadMate!";

                // Настройка SMTP (пример для Mail.ru/Gmail)
                SmtpClient client = new SmtpClient("smtp.mail.ru", 587);
                client.Credentials = new NetworkCredential("ваш_email", "ваш_пароль_приложения");
                client.EnableSsl = true;
                client.Send(mail);
            }
            catch {  }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
