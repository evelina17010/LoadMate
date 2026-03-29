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

namespace LoadMate.Pages
{
    /// <summary>
    /// Логика взаимодействия для AdminUsersPage.xaml
    /// </summary>
    public partial class AdminUsersPage : Page
    {
        private User selectedUser;

        public AdminUsersPage()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            var users = Conn.loadMateEntities.User.ToList();

            foreach (var user in users)
            {
                if (user.Role_id > 0)
                {
                    user.Role = Conn.loadMateEntities.Role.FirstOrDefault(r => r.Role_id == user.Role_id);
                }
                if (user.UserStatus_id > 0)
                {
                    user.UserStatus = Conn.loadMateEntities.UserStatus.FirstOrDefault(us => us.UserStatus_id == user.UserStatus_id);
                }
            }

            UsersGrid.ItemsSource = users;
        }

        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = UsersGrid.SelectedItem as User;
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddUserWindow();
            addWindow.Owner = Application.Current.MainWindow;
            if (addWindow.ShowDialog() == true)
            {
                LoadUsers();
                MessageBox.Show("Пользователь добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editWindow = new EditUserWindow(selectedUser);
            editWindow.Owner = Application.Current.MainWindow;
            if (editWindow.ShowDialog() == true)
            {
                LoadUsers();
                MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BlockUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selectedUser.UserStatus_id == 2)
            {
                selectedUser.UserStatus_id = 1;
                MessageBox.Show("Пользователь разблокирован", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                selectedUser.UserStatus_id = 2;
                MessageBox.Show("Пользователь заблокирован", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Conn.loadMateEntities.SaveChanges();
            LoadUsers();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void RoleFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string search = txtSearch.Text.Trim();
            var users = Conn.loadMateEntities.User.ToList();

            foreach (var user in users)
            {
                if (user.Role_id > 0)
                {
                    user.Role = Conn.loadMateEntities.Role.FirstOrDefault(r => r.Role_id == user.Role_id);
                }
                if (user.UserStatus_id > 0)
                {
                    user.UserStatus = Conn.loadMateEntities.UserStatus.FirstOrDefault(us => us.UserStatus_id == user.UserStatus_id);
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.Full_name.Contains(search) || u.Email.Contains(search)).ToList();
            }

            if (cmbRoleFilter.SelectedItem is ComboBoxItem selected && selected.Content.ToString() != "Все роли")
            {
                string roleName = selected.Content.ToString();
                users = users.Where(u => u.Role != null && u.Role.Name == roleName).ToList();
            }

            UsersGrid.ItemsSource = users;
        }
    }
}