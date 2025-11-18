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
using System.Data.SqlClient;

namespace up07
{
    public partial class AdminWindow : Window
    {
        private User currentUser;
        private DatabaseService dbService;
        private UserInfo selectedUser;

        public AdminWindow(User user, DatabaseService dbService)
        {
            InitializeComponent();
            currentUser = user;
            this.dbService = dbService;
            UserNameTextBlock.Text = user.FullName;
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                List<UserInfo> users = new List<UserInfo>();
                string connectionString = "Server=DESKTOP-2HRDJ7C\\SQLEXPRESS;Database=up07;Integrated Security=True;";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT u.пользователь_ид, u.ФИО, u.логин, r.название 
                                   FROM пользователи u
                                   INNER JOIN роли r ON u.роль_ид = r.роль_ид
                                   ORDER BY u.пользователь_ид";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new UserInfo
                            {
                                UserId = reader.GetInt32(0),
                                FullName = reader.GetString(1),
                                Login = reader.GetString(2),
                                RoleName = reader.GetString(3)
                            });
                        }
                    }
                }

                UsersDataGrid.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = UsersDataGrid.SelectedItem as UserInfo;
        }

        private void ChangeLoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя из списка", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewLoginTextBox.Text))
            {
                MessageBox.Show("Введите новый логин", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dbService.ChangeUserLogin(selectedUser.UserId, NewLoginTextBox.Text.Trim()))
            {
                MessageBox.Show("Логин успешно изменен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                NewLoginTextBox.Clear();
                LoadUsers();
            }
            else
            {
                MessageBox.Show("Ошибка изменения логина", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя из списка", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                MessageBox.Show("Введите новый пароль", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dbService.ChangeUserPassword(selectedUser.UserId, NewPasswordBox.Password))
            {
                MessageBox.Show("Пароль успешно изменен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                NewPasswordBox.Clear();
            }
            else
            {
                MessageBox.Show("Ошибка изменения пароля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangeRoleButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя из списка", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите новую роль", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int newRoleId = int.Parse(((ComboBoxItem)RoleComboBox.SelectedItem).Tag.ToString());

            if (dbService.ChangeUserRole(selectedUser.UserId, newRoleId))
            {
                MessageBox.Show("Роль успешно изменена", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                RoleComboBox.SelectedIndex = -1;
                LoadUsers();
            }
            else
            {
                MessageBox.Show("Ошибка изменения роли", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }

    public class UserInfo
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Login { get; set; }
        public string RoleName { get; set; }
    }
}
