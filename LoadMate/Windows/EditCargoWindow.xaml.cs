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
    /// Логика взаимодействия для EditCargoWindow.xaml
    /// </summary>
    public partial class EditCargoWindow : Window
    {
        private Cargo _currentCargo;

        public EditCargoWindow(Cargo selectedCargo)
        {
            InitializeComponent();
            _currentCargo = selectedCargo;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                cmbClient.ItemsSource = Conn.loadMateEntities.User.Where(u => u.Role_id == 2).ToList();
                cmbCargoType.ItemsSource = Conn.loadMateEntities.CargoType.ToList();

                cmbClient.SelectedValue = _currentCargo.Client_id;
                cmbCargoType.SelectedValue = _currentCargo.CargoType_id;

                txtDescription.Text = _currentCargo.Description;
                txtWeight.Text = _currentCargo.Weight_kg.ToString();
                txtVolume.Text = _currentCargo.Volume_m3.ToString();

                chkFragile.IsChecked = _currentCargo.Is_fragile;
                chkDangerous.IsChecked = _currentCargo.Is_dangerous;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (cmbClient.SelectedValue == null || cmbCargoType.SelectedValue == null)
            {
                MessageBox.Show("Выберите клиента и тип груза!");
                return;
            }

            try
            {
                _currentCargo.Client_id = (int)cmbClient.SelectedValue;
                _currentCargo.CargoType_id = (int)cmbCargoType.SelectedValue;
                _currentCargo.Description = txtDescription.Text;
                if (decimal.TryParse(txtWeight.Text.Replace(".", ","), out decimal weight))
                    _currentCargo.Weight_kg = weight;

                if (decimal.TryParse(txtVolume.Text.Replace(".", ","), out decimal volume))
                    _currentCargo.Volume_m3 = volume;

                _currentCargo.Is_fragile = chkFragile.IsChecked ?? false;
                _currentCargo.Is_dangerous = chkDangerous.IsChecked ?? false;
                Conn.loadMateEntities.SaveChanges();

                MessageBox.Show("Изменения успешно сохранены!");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}