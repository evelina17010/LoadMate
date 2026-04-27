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
    /// Логика взаимодействия для ClientCargoPage.xaml
    /// </summary>
    public partial class ClientCargoPage : Page
    {
        private int clientId;

        public ClientCargoPage(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;
            LoadCargo();
        }

        private void LoadCargo()
        {
            var cargoList = Conn.loadMateEntities.Cargo
                .Where(c => c.Client_id == clientId)
                .OrderByDescending(c => c.Created_at)
                .ToList();

            var cargoWithDetails = cargoList.Select(c => new
            {
                c.Cargo_id,
                c.Description,
                c.Weight_kg,
                c.Volume_m3,
                Is_fragile = c.Is_fragile ? "Да" : "Нет",
                Is_dangerous = c.Is_dangerous ? "Да" : "Нет",
                c.Created_at,
                CargoTypeName = GetCargoTypeName(c.CargoType_id)
            }).ToList();

            CargoGrid.ItemsSource = cargoWithDetails;
        }
        private string GetCargoTypeName(int cargoTypeId)
        {
            var cargoType = Conn.loadMateEntities.CargoType.FirstOrDefault(ct => ct.CargoType_id == cargoTypeId);
            return cargoType != null ? cargoType.Name : "Не указан";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCargo();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtSearch.Text.Trim().ToLower();

            var query = Conn.loadMateEntities.Cargo
                .Where(c => c.Client_id == clientId);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Description.ToLower().Contains(search) || c.Cargo_id.ToString().Contains(search));
            }

            var cargoList = query.OrderByDescending(c => c.Created_at).ToList();

            var cargoWithDetails = cargoList.Select(c => new
            {
                c.Cargo_id,
                c.Description,
                c.Weight_kg,
                c.Volume_m3,
                Is_fragile = c.Is_fragile ? "Да" : "Нет",
                Is_dangerous = c.Is_dangerous ? "Да" : "Нет",
                c.Created_at,
                CargoTypeName = GetCargoTypeName(c.CargoType_id)
            }).ToList();

            CargoGrid.ItemsSource = cargoWithDetails;
        }
    }
}
