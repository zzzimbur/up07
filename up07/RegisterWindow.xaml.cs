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
    public partial class RegisterWindow : Window
    {
        private DatabaseService dbService;
        public RegisterWindow()
        {
            InitializeComponent();
            dbService = new DatabaseService("DESKTOP-2HRDJ7C\\SQLEXPRESS", "up07");
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация полей
            if (string.IsNullOrWhiteSpace(FIOTextBox.Text) ||
                BirthDatePicker.SelectedDate == null ||
                GenderComboBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(SNILSTextBox.Text) ||
                string.IsNullOrWhiteSpace(OMSTextBox.Text) ||
                string.IsNullOrWhiteSpace(AddressTextBox.Text) ||
                string.IsNullOrWhiteSpace(PhoneTextBox.Text) ||
                string.IsNullOrWhiteSpace(LoginTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка совпадения паролей
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка длины пароля
            if (PasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //очищаем телефон от маски
            string cleanPhone = new string(PhoneTextBox.Text.Where(char.IsDigit).ToArray());

            //парсинг телефона
            if (!long.TryParse(PhoneTextBox.Text, out long phone))
            {
                MessageBox.Show("Неверный формат телефона", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Получение пола
            string gender = ((ComboBoxItem)GenderComboBox.SelectedItem).Content.ToString();

            // Регистрация
            bool success = dbService.RegisterUser(
                FIOTextBox.Text.Trim(),
                LoginTextBox.Text.Trim(),
                PasswordBox.Password,
                BirthDatePicker.SelectedDate.Value,
                gender,
                SNILSTextBox.Text.Trim(),
                OMSTextBox.Text.Trim(),
                AddressTextBox.Text.Trim(),
                phone
            );

            if (success)
            {
                MessageBox.Show("Регистрация успешно завершена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Ошибка регистрации. Возможно, этот логин уже занят.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoginLink_Click(object sender, EventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //разрешаем только цифры
            e.Handled = !IsTextAllowed(e.Text);
        }
        private static bool IsTextAllowed(string text)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text, "^[0-9]+$");
        }
        private void PhoneTextBox_TextChanged(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null) return;

            //убираем все символы, кроме цифр
            string text = new string(textBox.Text.Where(char.IsDigit).ToArray());

            if (text.Length == 0) return;

            //форматируем +7 (999) 999-99-99
            string formatted = "+7";
            if (text.Length > 1)
                formatted += " (" + text.Substring(1, Math.Min(3, text.Length - 1));
            if (text.Length >= 5)
                formatted += ") " + text.Substring(4, Math.Min(3, text.Length - 4));
            if (text.Length >= 8)
                formatted += "-" + text.Substring(7, Math.Min(2, text.Length - 7));
            if (text.Length >= 10)
                formatted += " -" + text.Substring(9, Math.Min(2, text.Length - 9));

            //устанавливаем курсор в конец
            int cursorPosition = textBox.SelectionStart;
            textBox.TextChanged -= PhoneTextBox_TextChanged;
            textBox.Text = formatted;
            textBox.SelectionStart = formatted.Length;
            textBox.TextChanged += PhoneTextBox_TextChanged;
        }
    }
}
