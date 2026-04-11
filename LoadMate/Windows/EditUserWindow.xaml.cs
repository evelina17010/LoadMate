using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using LoadMate.DBConn;

namespace LoadMate.Windows
{
    public partial class EditUserWindow : Window
    {
        public List<Role> Roles { get; set; }
        private User currentUser;
        private byte[] _newPhotoBytes = null;

        public EditUserWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            Roles = Conn.loadMateEntities.Role.ToList();
            DataContext = this;

            txtFullName.Text = currentUser.Full_name;
            txtEmail.Text = currentUser.Email;
            txtPhone.Text = currentUser.Phone;

            var login = Conn.loadMateEntities.Login.FirstOrDefault(l => l.User_id == currentUser.User_id);
            if (login != null) txtUsername.Text = login.Username;

            cmbRole.SelectedValue = currentUser.Role_id;
            if (currentUser.ImagePath != null)
            {
                _newPhotoBytes = currentUser.ImagePath;
                imgAvatar.Source = BytesToImage(_newPhotoBytes);
            }
        }

        private BitmapImage BytesToImage(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }

        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    _newPhotoBytes = File.ReadAllBytes(ofd.FileName);
                    imgAvatar.Source = BytesToImage(_newPhotoBytes);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка чтения файла: " + ex.Message);
                }
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text)) return ShowError("Введите ФИО");
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !Regex.IsMatch(txtEmail.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return ShowError("Введите корректный Email");
            if (cmbRole.SelectedItem == null) return ShowError("Выберите роль");
            if (!string.IsNullOrWhiteSpace(txtPassword.Password) && txtPassword.Password.Length < 6)
                return ShowError("Пароль должен быть не менее 6 символов");
            return true;
        }

        private bool ShowError(string msg) { MessageBox.Show(msg, "Валидация"); return false; }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateData()) return;

                currentUser.Full_name = txtFullName.Text.Trim();
                currentUser.Email = txtEmail.Text.Trim();
                currentUser.Phone = txtPhone.Text.Trim();
                currentUser.Role_id = (int)cmbRole.SelectedValue;
                currentUser.ImagePath = _newPhotoBytes; 

                if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    var login = Conn.loadMateEntities.Login.FirstOrDefault(l => l.User_id == currentUser.User_id);
                    if (login != null) login.Password_hash = HashPassword(txtPassword.Password.Trim());
                }

                Conn.loadMateEntities.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}