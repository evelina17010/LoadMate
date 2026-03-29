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
    /// Логика взаимодействия для ChangeStatusWindow.xaml
    /// </summary>
    public partial class ChangeStatusWindow : Window
    {
        public int SelectedStatusId { get; private set; }

        public ChangeStatusWindow(int currentStatusId)
        {
            InitializeComponent();

            var statuses = Conn.loadMateEntities.OrderStatus.ToList();
            cmbStatus.ItemsSource = statuses;
            cmbStatus.DisplayMemberPath = "Name";
            cmbStatus.SelectedValuePath = "OrderStatus_id";
            cmbStatus.SelectedValue = currentStatusId;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (cmbStatus.SelectedItem != null)
            {
                SelectedStatusId = (int)cmbStatus.SelectedValue;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Выберите статус", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}