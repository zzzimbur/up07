using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace up07
{
    public class PatientService
    {
        private readonly string connectionString;

        public PatientService(string server, string database)
        {
            connectionString = $"Server={server};Database={database};Integrated Security=True;";
        }

        // Получение списка всех врачей по специальности
        public List<DoctorInfo> GetDoctorsBySpecialty(int? specialtyId = null)
        {
            List<DoctorInfo> doctors = new List<DoctorInfo>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT v.врач_ид, v.фамилия, v.имя, v.отчество, 
                                   s.название as специальность, v.категория, 
                                   o.название as отделение
                                   FROM врачи v
                                   INNER JOIN специальности s ON v.специальность_ид = s.специальность_ид
                                   INNER JOIN отделения o ON v.отделение_ид = o.отделение_ид
                                   WHERE (@specialtyId IS NULL OR v.специальность_ид = @specialtyId)
                                   ORDER BY v.фамилия, v.имя";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@specialtyId", (object)specialtyId ?? DBNull.Value);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                doctors.Add(new DoctorInfo
                                {
                                    DoctorId = reader.GetInt32(0),
                                    LastName = reader.GetString(1),
                                    FirstName = reader.GetString(2),
                                    MiddleName = reader.GetString(3),
                                    Specialty = reader.GetString(4),
                                    Category = reader.GetString(5),
                                    Department = reader.GetString(6)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения списка врачей: {ex.Message}");
            }

            return doctors;
        }

        // Получение списка специальностей
        public List<SpecialtyInfo> GetSpecialties()
        {
            List<SpecialtyInfo> specialties = new List<SpecialtyInfo>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT специальность_ид, название FROM специальности ORDER BY название";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            specialties.Add(new SpecialtyInfo
                            {
                                SpecialtyId = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения специальностей: {ex.Message}");
            }

            return specialties;
        }
        // Создание записи на прием
        public bool CreateAppointment(int patientId, int doctorId, DateTime appointmentDate, string type)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Проверка занятости времени
                    string checkQuery = @"SELECT COUNT(*) FROM приемы 
                                        WHERE врач_ид = @doctorId 
                                        AND дата_приема = @appointmentDate";

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@doctorId", doctorId);
                        checkCmd.Parameters.AddWithValue("@appointmentDate", appointmentDate);

                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            return false; // Время занято
                        }
                    }

                    // Создание записи
                    string insertQuery = @"INSERT INTO приемы (пациент_ид, врач_ид, дата_приема, тип) 
                                         VALUES (@patientId, @doctorId, @appointmentDate, @type)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId);
                        cmd.Parameters.AddWithValue("@doctorId", doctorId);
                        cmd.Parameters.AddWithValue("@appointmentDate", appointmentDate);
                        cmd.Parameters.AddWithValue("@type", type);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания записи: {ex.Message}");
                return false;
            }
        }

        // Получение доступного времени для записи к врачу
        public List<DateTime> GetAvailableTimeSlots(int doctorId, DateTime date)
        {
            List<DateTime> availableSlots = new List<DateTime>();

            try
            {
                // Рабочее время: 9:00 - 18:00, интервал 30 минут
                DateTime startTime = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0);
                DateTime endTime = new DateTime(date.Year, date.Month, date.Day, 18, 0, 0);

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Получаем занятые слоты
                    string query = "SELECT дата_приема FROM приемы WHERE врач_ид = @doctorId AND CAST(дата_приема AS DATE) = @date";
                    List<DateTime> bookedSlots = new List<DateTime>();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@doctorId", doctorId);
                        cmd.Parameters.AddWithValue("@date", date.Date);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                bookedSlots.Add(reader.GetDateTime(0));
                            }
                        }
                    }
                    // Генерируем доступные слоты
                    for (DateTime slot = startTime; slot < endTime; slot = slot.AddMinutes(30))
                    {
                        if (!bookedSlots.Contains(slot))
                        {
                            availableSlots.Add(slot);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения доступного времени: {ex.Message}");
            }

            return availableSlots;
        }

        // Получение медицинской карты пациента
        public PatientMedicalCard GetMedicalCard(int patientId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT mk.медицинская_карта_ид, mk.дата_приема, v.ФИО, 
                                   mk.жалобы, mk.история_болезней, mk.диагнозы, mk.лечение
                                   FROM медицинские_карты mk
                                   INNER JOIN врачи v ON mk.врач_ид = v.врач_ид
                                   WHERE mk.пациент_ид = @patientId
                                   ORDER BY mk.дата_приема DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new PatientMedicalCard
                                {
                                    CardId = reader.GetInt32(0),
                                    Date = reader.GetDateTime(1),
                                    DoctorName = reader.GetString(2),
                                    Complaints = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    MedicalHistory = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    Diagnoses = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                    Treatment = reader.IsDBNull(6) ? "" : reader.GetString(6)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения медицинской карты: {ex.Message}");
            }

            return null;
        }
    }

    // Вспомогательные классы
    public class DoctorInfo
    {
        public int DoctorId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Specialty { get; set; }
        public string Category { get; set; }
        public string Department { get; set; }

        public string FullName => $"{LastName} {FirstName} {MiddleName}";
    }

    public class SpecialtyInfo
    {
        public int SpecialtyId { get; set; }
        public string Name { get; set; }
    }

    public class PatientMedicalCard
    {
        public int CardId { get; set; }
        public DateTime Date { get; set; }
        public string DoctorName { get; set; }
        public string Complaints { get; set; }
        public string MedicalHistory { get; set; }
        public string Diagnoses { get; set; }
        public string Treatment { get; set; }
    }
}