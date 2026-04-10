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

        private void txtCardNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            int selectionStart = txtCardNumber.SelectionStart;
            int oldLength = txtCardNumber.Text.Length;

            string text = txtCardNumber.Text.Replace(" ", "");
            if (text.Length > 16) text = text.Substring(0, 16);

            string formatted = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && i % 4 == 0) formatted += " ";
                formatted += text[i];
            }

            if (txtCardNumber.Text != formatted)
            {
                txtCardNumber.Text = formatted;
                int newPosition = selectionStart + (formatted.Length - oldLength);
                txtCardNumber.SelectionStart = Math.Max(0, Math.Min(newPosition, formatted.Length));
            }
        }

        private void txtExpiry_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = txtExpiry.Text.Replace("/", "");
            if (text.Length > 4)
                text = text.Substring(0, 4);

            string formatted = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (i == 2)
                    formatted += "/";
                formatted += text[i];
            }
            txtExpiry.Text = formatted;
            txtExpiry.CaretIndex = formatted.Length;
        }

        private bool ValidateData()
        {
            string cardNumber = txtCardNumber.Text.Replace(" ", "");
            if (cardNumber.Length < 16)
            {
                MessageBox.Show("Введите корректный номер карты (16 цифр).", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtExpiry.Text) || txtExpiry.Text.Length < 5 || !txtExpiry.Text.Contains("/"))
            {
                MessageBox.Show("Укажите срок действия в формате ММ/ГГ.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (txtCvv.Password.Length < 3)
            {
                MessageBox.Show("Введите 3-значный код CVV.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                    MessageBox.Show("Оплата успешно завершена! Чек отправлен на вашу почту.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при совершении платежа: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SendEmailReceipt(Payment payment)
        {
            try
            {
                var db = Conn.loadMateEntities;
                var order = db.Order.Include("Route.Address.Street.City").Include("Route.Address1.Street.City")
                                    .FirstOrDefault(o => o.Order_id == payment.Order_id);

                if (order == null) return;

                var cargo = db.Cargo.FirstOrDefault(c => c.Cargo_id == order.Cargo_id);
                if (cargo == null) return;

                var user = db.User.FirstOrDefault(u => u.User_id == cargo.Client_id);
                if (string.IsNullOrEmpty(user?.Email)) return;
                string addrFrom = order.Route != null
                    ? $"{order.Route.Address.Street.City.Name}, {order.Route.Address.Street.Name}, д. {order.Route.Address.House_number}"
                    : "Не указан";
                string addrTo = order.Route != null
                    ? $"{order.Route.Address1.Street.City.Name}, {order.Route.Address1.Street.Name}, д. {order.Route.Address1.House_number}"
                    : "Не указан";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("miftakhova_ev@mail.ru", "LoadMate Logistics");
                mail.To.Add(user.Email);
                mail.Subject = $"Электронный чек по заказу №{order.Order_number}";
                mail.IsBodyHtml = true;
                mail.Body = $@"
        <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Arial, sans-serif; background-color: #f8fafc; padding: 20px 10px;'>
            <div style='max-width: 500px; margin: 0 auto; background-color: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>
                
                <div style='background-color: #5cb85c; padding: 25px 20px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 24px; font-weight: bold; color: #ffffff; letter-spacing: 1px;'>LOADMATE</h1>
                    <div style='color: #e8f5e9; font-size: 12px; margin-top: 5px; text-transform: uppercase;'>Подтверждение оплаты</div>
                </div>

                <div style='padding: 25px;'>
                    <p style='margin: 0 0 20px 0; font-size: 16px; color: #1e293b; font-weight: 600;'>Уважаемый(ая) {user.Full_name}!</p>
                    <p style='margin: 0 0 25px 0; color: #475569; font-size: 14px; line-height: 1.5;'>Ваш платеж по заказу успешно принят и обработан. Благодарим за доверие к нашему сервису.</p>
                    
                    <div style='background-color: #f9fbf9; padding: 15px; border-radius: 6px; margin-bottom: 25px;'>
                        <div style='font-size: 11px; color: #94a3b8; text-transform: uppercase; font-weight: bold; margin-bottom: 4px;'>Маршрут перевозки</div>
                        <div style='font-size: 13px; color: #1e293b; margin-bottom: 8px;'>Откуда: {addrFrom}</div>
                        <div style='font-size: 13px; color: #1e293b;'>Куда: {addrTo}</div>
                    </div>

                    <table style='width: 100%; font-size: 14px; color: #475569; border-collapse: collapse;'>
                        <tr>
                            <td style='padding: 8px 0; border-bottom: 1px solid #f1f5f9;'>Номер заказа:</td>
                            <td style='padding: 8px 0; border-bottom: 1px solid #f1f5f9; text-align: right; color: #1e293b; font-weight: 600;'>{order.Order_number}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; border-bottom: 1px solid #f1f5f9;'>Метод оплаты:</td>
                            <td style='padding: 8px 0; border-bottom: 1px solid #f1f5f9; text-align: right; color: #1e293b;'>{payment.Payment_method}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; border-bottom: 1px solid #f1f5f9;'>Дата транзакции:</td>
                            <td style='padding: 8px 0; border-bottom: 1px solid #f1f5f9; text-align: right; color: #1e293b;'>{payment.Paid_date:g}</td>
                        </tr>
                        <tr>
                            <td style='padding: 20px 0 0 0; font-size: 16px; color: #1e293b; font-weight: bold;'>Итого оплачено:</td>
                            <td style='padding: 20px 0 0 0; text-align: right; color: #5cb85c; font-size: 20px; font-weight: bold;'>{payment.Amount:N2} ₽</td>
                        </tr>
                    </table>
                </div>

                <div style='background-color: #f8fafc; padding: 20px; text-align: center; border-top: 1px solid #f1f5f9;'>
                    <div style='font-size: 12px; color: #94a3b8;'>
                        © {DateTime.Now.Year} LoadMate Logistics System<br>
                    </div>
                </div>
            </div>
        </div>";

                using (SmtpClient client = new SmtpClient("smtp.mail.ru", 587))
                {
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential("miftakhova_ev@mail.ru", "Bmz8qEIDckirBNY5cEmE");
                    client.Send(mail);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка отправки Email чека: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}