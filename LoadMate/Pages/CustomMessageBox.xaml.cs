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

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string message, string title)
        {
            InitializeComponent();
            txtMessage.Text = message;
            txtTitle.Text = title;

            if (title.ToLower().Contains("ошибка") || title.ToLower().Contains("внимание"))
            {
                txtTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
            }
            else
            {
                if (TryFindResource("PrimaryColor") is Brush primaryBrush)
                {
                    txtTitle.Foreground = primaryBrush;
                }
            }
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        public static void Show(string message, string title = "Уведомление")
        {
            var msg = new CustomMessageBox(message, title);
            msg.ShowDialog();
        }
    }
}