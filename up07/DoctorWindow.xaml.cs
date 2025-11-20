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
    public partial class DoctorWindow : Window
    {
        private User currentUser;
        private DatabaseService dbService;
        private int? doctorId;
        private PatientInfo selectedPatient;
 
        public DoctorWindow(User user, DatabaseService dbService)
        {
            InitializeComponent();
            currentUser = user;
            this.dbService = dbService;
            UserNameTextBlock.Text = user.FullName;
            LoadDoctorId();
            LoadPatients();
            LoadAppointments();
        }

        private void LoadDoctorId()
        {
            try
            {
                string connectionString = "Server=DESKTOP-2HRDJ7C\\SQLEXPRESS;Database=up07;Integrated Security=True;";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT врач_ид FROM врачи WHERE CONCAT_WS(' ', фамилия, имя, отчество) = @fio";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@fio", currentUser.FullName);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            doctorId = (int)result;
                        }
                        else 
                        {
                            MessageBox.Show("Не найден врач с таким полным именем в БД. Проверьте ФИО в currentUser.FullName.", "Предупреждение",
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch(Exception ex) 
            {
                MessageBox.Show($"Ошибка загрузки ID врача: {ex.Message}", "Ошибка",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPatients()
        {
            try
            {
                List<PatientInfo> patients = new List<PatientInfo>();
                string connectionString = "Server=DESKTOP-2HRDJ7C\\SQLEXPRESS;Database=up07;Integrated Security=True;";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT DISTINCT p.пациент_ид, u.ФИО, p.дата_рождения, p.телефон
                                   FROM пациенты p
                                   INNER JOIN пользователи u ON p.пользователь_ид = u.пользователь_ид
                                   INNER JOIN приемы pr ON p.пациент_ид = pr.пациент_ид
                                   WHERE pr.врач_ид = @doctorId
                                   ORDER BY u.ФИО";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@doctorId", doctorId ?? 0);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                patients.Add(new PatientInfo
                                {
                                    PatientId = reader.GetInt32(0),
                                    FullName = reader.GetString(1),
                                    BirthDate = reader.GetDateTime(2),
                                    Phone = reader.IsDBNull(3) ? "" : reader.GetInt64(3).ToString()
                                });
                            }
                        }
                    }
                }

                PatientsDataGrid.ItemsSource = patients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пациентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAppointments()
        {
            try
            {
                List<AppointmentInfo> appointments = new List<AppointmentInfo>();
                string connectionString = "Server=DESKTOP-2HRDJ7C\\SQLEXPRESS;Database=up07;Integrated Security=True;";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT pr.прием_ид, u.ФИО, pr.дата_приема, pr.тип_приема, pr.диагноз
                                   FROM приемы pr
                                   INNER JOIN пациенты p ON pr.пациент_ид = p.пациент_ид
                                   INNER JOIN пользователи u ON p.пользователь_ид = u.пользователь_ид
                                   WHERE pr.врач_ид = @doctorId
                                   ORDER BY pr.дата_приема DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@doctorId", doctorId ?? 0);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                appointments.Add(new AppointmentInfo
                                {
                                    AppointmentId = reader.GetInt32(0),
                                    PatientName = reader.GetString(1),
                                    AppointmentDate = reader.GetDateTime(2),
                                    Type = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    Diagnosis = reader.IsDBNull(4) ? "" : reader.GetString(4)
                                });
                            }
                        }
                    }
                }

                AppointmentsDataGrid.ItemsSource = appointments;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PatientsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedPatient = PatientsDataGrid.SelectedItem as PatientInfo;
        }

        private void OpenMedicalCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPatient == null)
            {
                MessageBox.Show("Выберите пациента из списка", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Открытие медицинской карты пациента: {selectedPatient.FullName}\n\n" +
                          "Здесь будет функционал редактирования медицинской карты, " +
                          "добавления анализов, рецептов и т.д.", "Медицинская карта",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }

    public class PatientInfo
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Phone { get; set; }
    }

    public class AppointmentInfo
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Type { get; set; }
        public string Diagnosis { get; set; }
    }
}

