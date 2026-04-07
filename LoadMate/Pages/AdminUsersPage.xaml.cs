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
using LoadMate.Windows;
using System.Data.Entity;

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для AdminUsersPage.xaml
    /// </summary>
    public partial class AdminUsersPage : Page
    {
        private List<User> _allUsers;
        private User selectedUser;

        public AdminUsersPage()
        {
            InitializeComponent();
            LoadFilterData();
            LoadUsers();
        }

        private void LoadUsers()
        {
            _allUsers = Conn.loadMateEntities.User.Include(u => u.Role).Include(u => u.UserStatus).ToList();
            ApplyFilters();
        }

        private void LoadFilterData()
        {
            var roles = Conn.loadMateEntities.Role.ToList();
            roles.Insert(0, new Role { Role_id = 0, Name = "Все роли" });
            cmbRoleFilter.ItemsSource = roles;
            cmbRoleFilter.SelectedValuePath = "Role_id";
            cmbRoleFilter.SelectedIndex = 0;
        }

        private void ApplyFilters()
        {
            if (_allUsers == null) return;

            var filtered = _allUsers.AsEnumerable();
            string search = txtSearch.Text.Trim().ToLower();

            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(u =>
                    (u.Full_name != null && u.Full_name.ToLower().Contains(search)) ||
                    (u.Email != null && u.Email.ToLower().Contains(search)));
            }

            if (cmbRoleFilter.SelectedValue != null)
            {
                int selectedRoleId = (int)cmbRoleFilter.SelectedValue;
                if (selectedRoleId != 0)
                {
                    filtered = filtered.Where(u => u.Role_id == selectedRoleId);
                }
            }

            UsersGrid.ItemsSource = filtered.ToList();
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new AddUserWindow();
            if (addWin.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для редактирования");
                return;
            }

            var editWin = new EditUserWindow(selectedUser);
            if (editWin.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void BlockUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null) return;

            try
            {
                var user = Conn.loadMateEntities.User.FirstOrDefault(u => u.User_id == selectedUser.User_id);
                if (user != null)
                {
                    user.UserStatus_id = (user.UserStatus_id == 1) ? 2 : 1;
                    Conn.loadMateEntities.SaveChanges();
                    LoadUsers();
                    MessageBox.Show("Статус пользователя изменен");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbRoleFilter.SelectedIndex = 0;
            LoadUsers();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void RoleFilter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = UsersGrid.SelectedItem as User;
        }
    }
}