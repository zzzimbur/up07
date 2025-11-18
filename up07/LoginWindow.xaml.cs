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

namespace up07
{
    public partial class LoginWindow : Window
    {
        private DatabaseService dbService;
        public LoginWindow()
        {
            InitializeComponent();
            dbService = new DatabaseService("DESKTOP-2HRDJ7C\\SQLEXPRESS", "up07");
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            User user = dbService.AuthenticateUser(login, password);

            if (user != null)
            {
                MessageBox.Show($"Добро пожаловать, {user.FullName}!", "Успешный вход", MessageBoxButton.OK, MessageBoxImage.Information);

                //открываем главное окно в зависимости от роли
                Window mainWindow = null;

                switch (user.RoleId)
                {
                    case 1: //администратор
                        mainWindow = new AdminWindow(user, dbService);
                        break;
                    case 2: //врач
                        mainWindow = new DoctorWindow(user, dbService);
                        break;
                    case 3: //пользователь
                        mainWindow = new PatientWindow(user, dbService);
                        break;
                }

                if (mainWindow != null)
                {
                    mainWindow.Show();
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}
