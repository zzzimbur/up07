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
    public partial class MakeAppointmentWindow : Window
    {
        private int patientId;
        private string connectionString = "Server=DESKTOP-2HRDJ7C\\SQLEXPRESS;Database=up07;Integrated Security=True;";

        public MakeAppointmentWindow(int patientId)
        {
            InitializeComponent();
            this.patientId = patientId;

            // Устанавливаем минимальную дату - завтра
            AppointmentDatePicker.DisplayDateStart = DateTime.Now.AddDays(1);

            LoadSpecialties();
        }

        private void LoadSpecialties()
        {
            try
            {
                List<Specialty> specialties = new List<Specialty>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT специальность_ид, название FROM специальности ORDER BY название";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            specialties.Add(new Specialty
                            {
                                SpecialtyId = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }

                SpecialtyComboBox.ItemsSource = specialties;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки специальностей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SpecialtyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpecialtyComboBox.SelectedValue == null) return;

            int specialtyId = (int)SpecialtyComboBox.SelectedValue;
            LoadDoctors(specialtyId);
        }

        private void LoadDoctors(int specialtyId)
        {
            try
            {
                List<Doctor> doctors = new List<Doctor>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT врач_ид, фамилия + ' ' + имя + ' ' + отчество as ФИО, 
                                   категория, o.название as отделение
                                   FROM врачи v
                                   INNER JOIN отделения o ON v.отделение_ид = o.отделение_ид
                                   WHERE специальность_ид = @specialtyId
                                   ORDER BY фамилия";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@specialtyId", specialtyId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                doctors.Add(new Doctor
                                {
                                    DoctorId = reader.GetInt32(0),
                                    FullName = reader.GetString(1),
                                    Category = reader.GetString(2),
                                    Department = reader.GetString(3)
                                });
                            }
                        }
                    }
                }
                DoctorComboBox.ItemsSource = doctors;
                DoctorComboBox.SelectedIndex = -1;
                DoctorInfoBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки врачей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DoctorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DoctorComboBox.SelectedItem == null)
            {
                DoctorInfoBorder.Visibility = Visibility.Collapsed;
                return;
            }

            var doctor = DoctorComboBox.SelectedItem as Doctor;
            DoctorInfoText.Text = $"Категория: {doctor.Category}\nОтделение: {doctor.Department}";
            DoctorInfoBorder.Visibility = Visibility.Visible;
        }

        private void AppointmentDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentDatePicker.SelectedDate == null || DoctorComboBox.SelectedValue == null)
            {
                TimeComboBox.IsEnabled = false;
                return;
            }

            LoadAvailableTimeSlots();
        }

        private void LoadAvailableTimeSlots()
        {
            try
            {
                int doctorId = (int)DoctorComboBox.SelectedValue;
                DateTime selectedDate = AppointmentDatePicker.SelectedDate.Value;

                List<string> availableSlots = new List<string>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Получаем занятые слоты
                    string query = @"SELECT DATEPART(HOUR, дата_приема), DATEPART(MINUTE, дата_приема) 
                                   FROM приемы 
                                   WHERE врач_ид = @doctorId 
                                   AND CAST(дата_приема AS DATE) = @date";

                    List<TimeSpan> bookedSlots = new List<TimeSpan>();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@doctorId", doctorId);
                        cmd.Parameters.AddWithValue("@date", selectedDate.Date);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int hour = reader.GetInt32(0);
                                int minute = reader.GetInt32(1);
                                bookedSlots.Add(new TimeSpan(hour, minute, 0));
                            }
                        }
                    }

                    // Генерируем доступные слоты (9:00 - 17:30, интервал 30 минут)
                    for (int hour = 9; hour < 18; hour++)
                    {
                        for (int minute = 0; minute < 60; minute += 30)
                        {
                            if (hour == 17 && minute == 30) break; // Последний слот 17:00

                            TimeSpan slot = new TimeSpan(hour, minute, 0);

                            if (!bookedSlots.Contains(slot))
                            {
                                availableSlots.Add($"{hour:D2}:{minute:D2}");
                            }
                        }
                    }
                }

                TimeComboBox.ItemsSource = availableSlots;
                TimeComboBox.IsEnabled = availableSlots.Count > 0;
                if (availableSlots.Count == 0)
                {
                    MessageBox.Show("На выбранную дату нет свободных мест. Выберите другую дату.", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки времени: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (SpecialtyComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите специальность", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DoctorComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите врача", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AppointmentDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TimeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите время", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создание записи
            try
            {
                int doctorId = (int)DoctorComboBox.SelectedValue;
                DateTime appointmentDate = AppointmentDatePicker.SelectedDate.Value;
                string timeString = TimeComboBox.SelectedItem.ToString();
                string[] timeParts = timeString.Split(':');

                DateTime fullDateTime = new DateTime(
                    appointmentDate.Year,
                    appointmentDate.Month,
                    appointmentDate.Day,
                    int.Parse(timeParts[0]),
                    int.Parse(timeParts[1]),
                    0
                );

                string appointmentType = ((ComboBoxItem)TypeComboBox.SelectedItem).Content.ToString();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO приемы (пациент_ид, врач_ид, дата_приема, тип_приема) 
                                   VALUES (@patientId, @doctorId, @appointmentDate, @type)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId);
                        cmd.Parameters.AddWithValue("@doctorId", doctorId);
                        cmd.Parameters.AddWithValue("@appointmentDate", fullDateTime);
                        cmd.Parameters.AddWithValue("@type", appointmentType);

                        cmd.ExecuteNonQuery();
                    }
                }

                var doctor = DoctorComboBox.SelectedItem as Doctor;
                MessageBox.Show($"Вы успешно записаны на прием!\n\n" +
                              $"Врач: {doctor.FullName}\n" +
                              $"Дата: {fullDateTime:dd.MM.yyyy}\n" +
                              $"Время: {fullDateTime:HH:mm}\n" +
                              $"Тип: {appointmentType}",
                              "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания записи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class Specialty
    {
        public int SpecialtyId { get; set; }
        public string Name { get; set; }
    }

    public class Doctor
    {
        public int DoctorId { get; set; }
        public string FullName { get; set; }
        public string Category { get; set; }
        public string Department { get; set; }
    }
}


