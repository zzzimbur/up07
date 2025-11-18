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
    public partial class PatientWindow : Window
    {
        private User currentUser;
        private DatabaseService dbService;
        private int? patientId;
        private string connectionString = "Server=DESKTOP-2HRDJ7C\\SQLEXPRESS;Database=up07;Integrated Security=True;";

        public PatientWindow(User user, DatabaseService dbService)
        {
            InitializeComponent();
            currentUser = user;
            this.dbService = dbService;
            UserNameTextBlock.Text = user.FullName;

            patientId = dbService.GetPatientIdByUserId(user.UserId);
            LoadPatientId();
            LoadProfileData();
            LoadMyAppointments();
            LoadHistory();
        }
        private void LoadPatientId()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT пациент_ид FROM пациенты WHERE пользователь_ид = @userId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", currentUser.UserId);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            patientId = (int)result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ID пациента: {ex.Message}", "Ошибка");
            }
        }

        private void LoadProfileData()
        {
            if (!patientId.HasValue) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT u.ФИО, p.дата_рождения, p.телефон
                                   FROM пациенты p
                                   INNER JOIN пользователи u ON p.пользователь_ид = u.пользователь_ид
                                   WHERE p.пациент_ид = @patientId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                FIOTextBox.Text = reader.GetString(0);
                                BirthDateTextBox.Text = reader.GetDateTime(1).ToString("dd.MM.yyyy");
                                PhoneTextBox.Text = reader.GetInt32(2).ToString();
                            }
                        }
                    }

                    // Загрузка роста и веса из медицинской карты (если есть)
                    string cardQuery = @"SELECT TOP 1 жалобы FROM медицинские_карты 
                                       WHERE пациент_ид = @patientId 
                                       ORDER BY дата_приема DESC";

                    using (SqlCommand cmd = new SqlCommand(cardQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId.Value);
                        object result = cmd.ExecuteScalar();

                        // Здесь можно распарсить жалобы, если там хранится рост/вес
                        // Для примера оставляем пустыми
                        HeightTextBox.Text = "";
                        WeightTextBox.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMyAppointments()
        {
            if (!patientId.HasValue) return;

            try
            {
                List<MyAppointment> appointments = new List<MyAppointment>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT pr.прием_ид, v.фамилия, pr.дата_приема, z.кабинет, pr.тип_приема
                                   FROM приемы pr
                                   INNER JOIN врачи v ON pr.врач_ид = v.врач_ид
                                   LEFT JOIN заведующие z ON v.отделение_ид = z.отделение_ид
                                   WHERE pr.пациент_ид = @patientId 
                                   AND pr.дата_приема >= GETDATE()
                                   ORDER BY pr.дата_приема";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                appointments.Add(new MyAppointment
                                {
                                    AppointmentId = reader.GetInt32(0),
                                    DoctorName = reader.GetString(1),
                                    AppointmentDate = reader.GetDateTime(2),
                                    Cabinet = reader.IsDBNull(3) ? "—" : reader.GetString(3),
                                    Type = reader.IsDBNull(4) ? "—" : reader.GetString(4)
                                });
                            }
                        }
                    }
                }

                MyAppointmentsDataGrid.ItemsSource = appointments;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHistory()
        {
            if (!patientId.HasValue) return;

            try
            {
                List<MyAppointment> history = new List<MyAppointment>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT pr.прием_ид, v.фамилия, pr.дата_приема, z.кабинет, pr.диагноз
                                   FROM приемы pr
                                   INNER JOIN врачи v ON pr.врач_ид = v.врач_ид
                                   LEFT JOIN заведующие z ON v.отделение_ид = z.отделение_ид
                                   WHERE pr.пациент_ид = @patientId 
                                   AND pr.дата_приема < GETDATE()
                                   ORDER BY pr.дата_приема DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                history.Add(new MyAppointment
                                {
                                    AppointmentId = reader.GetInt32(0),
                                    DoctorName = reader.GetString(1),
                                    AppointmentDate = reader.GetDateTime(2),
                                    Cabinet = reader.IsDBNull(3) ? "—" : reader.GetString(3),
                                    Diagnosis = reader.IsDBNull(4) ? "—" : reader.GetString(4)
                                });
                            }
                        }
                    }
                }

                HistoryDataGrid.ItemsSource = history;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MakeAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (!patientId.HasValue)
            {
                MessageBox.Show("Ошибка определения пациента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MakeAppointmentWindow appointmentWindow = new MakeAppointmentWindow(patientId.Value);
            if (appointmentWindow.ShowDialog() == true)
            {
                // Обновляем список записей после успешной записи
                LoadMyAppointments();
            }
        }
        private void CancelAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = MyAppointmentsDataGrid.SelectedItem as MyAppointment;
            if (selected == null)
            {
                MessageBox.Show("Выберите запись для отмены", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите отменить запись к врачу {selected.DoctorName} на {selected.AppointmentDate:dd.MM.yyyy HH:mm}?",
                "Подтверждение отмены",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "DELETE FROM приемы WHERE прием_ид = @appointmentId";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@appointmentId", selected.AppointmentId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Запись успешно отменена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadMyAppointments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отмены записи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!patientId.HasValue) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Обновление ФИО в таблице пользователей
                    string updateUserQuery = @"UPDATE пользователи 
                                             SET ФИО = @fio 
                                             WHERE пользователь_ид = @userId";

                    using (SqlCommand cmd = new SqlCommand(updateUserQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@fio", FIOTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@userId", currentUser.UserId);
                        cmd.ExecuteNonQuery();
                    }

                    // Здесь можно добавить сохранение роста и веса в медицинскую карту
                    // Для этого нужно решить, как именно хранить эти данные
                }

                MessageBox.Show("Данные успешно сохранены", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                currentUser.FullName = FIOTextBox.Text.Trim();
                UserNameTextBlock.Text = currentUser.FullName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных: {ex.Message}", "Ошибка",
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

    public class MyAppointment
    {
        public int AppointmentId { get; set; }
        public string DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Cabinet { get; set; }
        public string Type { get; set; }
        public string Diagnosis { get; set; }
    }
}
