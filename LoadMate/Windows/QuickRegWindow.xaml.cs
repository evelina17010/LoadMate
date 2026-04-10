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
    /// Логика взаимодействия для QuickRegWindow.xaml
    /// </summary>
    public partial class QuickRegWindow : Window
    {
        public DBConn.User CreatedUser { get; private set; }

        public QuickRegWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var genders = Conn.loadMateEntities.Gender.ToList();
                cmbGender.SelectedValuePath = "Gender_id";
                cmbGender.ItemsSource = genders;

                if (genders.Count > 0) cmbGender.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }     

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Поля ФИО и Email обязательны для заполнения!");
                return;
            }

            try
            {
                var db = Conn.loadMateEntities;
                var newUser = new DBConn.User
                {
                    Full_name = txtFullName.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Gender_id = (int?)cmbGender.SelectedValue,
                    Role_id = 2,       
                    UserStatus_id = 1,   
                    Created_at = DateTime.Now
                };

                db.User.Add(newUser);
                db.SaveChanges();

                CreatedUser = newUser; 
                MessageBox.Show("Клиент успешно зарегистрирован!");

                this.DialogResult = true; 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
