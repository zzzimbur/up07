using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Runtime.Remoting.Messaging;
using System.Security.RightsManagement;
using System.Windows.Controls.Primitives;
using System.Runtime.Remoting;


namespace up07
{
    public class DatabaseService
    {
        private readonly string connectionString;

        public DatabaseService(string server, string database)
        {
            connectionString = $"Server={server};Database={database};Integrated Security=True;";
        }

        // Хеширование пароля
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Регистрация нового пользователя
        public bool RegisterUser(string fio, string login, string password, DateTime birthDate, string gender, string snils, string omsPolicy, string address, long phone)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Проверка существования логина
                    string checkQuery = "SELECT COUNT(*) FROM пользователи WHERE логин = @login";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@login", login);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            return false; // Логин уже существует
                        }
                    }

                    // Создание пациента
                    string patientQuery = @"INSERT INTO пациенты (дата_рождения, пол, СНИЛС, полис_ОМС, адрес, телефон) 
                                          OUTPUT INSERTED.пациент_ид
                                          VALUES (@birthDate, @gender, @snils, @omsPolicy, @address, @phone)";

                    int patientId;
                    using (SqlCommand cmd = new SqlCommand(patientQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@birthDate", birthDate);
                        cmd.Parameters.AddWithValue("@gender", gender);
                        cmd.Parameters.AddWithValue("@snils", snils);
                        cmd.Parameters.AddWithValue("@omsPolicy", omsPolicy);
                        cmd.Parameters.AddWithValue("@address", address);
                        cmd.Parameters.AddWithValue("@phone", phone);
                        patientId = (int)cmd.ExecuteScalar();
                    }

                    // Получение роли "Пользователь" (роль_ид = 3)
                    int roleId = 3;

                    // Создание пользователя
                    string userQuery = @"INSERT INTO пользователи (ФИО, логин, пароль, роль_ид, пользователь_ид) 
                                       VALUES (@fio, @login, @password, @roleId, @patientId)";

                    using (SqlCommand cmd = new SqlCommand(userQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@fio", fio);
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", HashPassword(password));
                        cmd.Parameters.AddWithValue("@roleId", roleId);
                        cmd.Parameters.AddWithValue("@patientId", patientId);
                        cmd.ExecuteNonQuery();
                    }
                    // Обновление связи пациента с пользователем
                    string updatePatientQuery = "UPDATE пациенты SET пользователь_ид = @patientId WHERE пациент_ид = @patientId";
                    using (SqlCommand cmd = new SqlCommand(updatePatientQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId);
                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка регистрации: {ex.Message}");
                return false;
            }
        }

        // Авторизация пользователя
        public User AuthenticateUser(string login, string password)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT u.пользователь_ид, u.ФИО, u.роль_ид, r.название 
                                   FROM пользователи u
                                   INNER JOIN роли r ON u.роль_ид = r.роль_ид
                                   WHERE u.логин = @login AND u.пароль = @password";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", HashPassword(password));

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new User
                                {
                                    UserId = reader.GetInt32(0),
                                    FullName = reader.GetString(1),
                                    RoleId = reader.GetInt32(2),
                                    RoleName = reader.GetString(3)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка авторизации: {ex.Message}");
            }
            return null;
        }

        // Получение ID пациента по ID пользователя
        public int? GetPatientIdByUserId(int userId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT пациент_ид FROM пациенты WHERE пользователь_ид = @userId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        object result = cmd.ExecuteScalar();
                        return result != null ? (int?)result : null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }
        // Изменение пароля (для администратора)
        public bool ChangeUserPassword(int userId, string newPassword)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE пользователи SET пароль = @password WHERE пользователь_ид = @userId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@password", HashPassword(newPassword));
                        cmd.Parameters.AddWithValue("@userId", userId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // Изменение логина (для администратора)
        public bool ChangeUserLogin(int userId, string newLogin)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE пользователи SET логин = @login WHERE пользователь_ид = @userId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", newLogin);
                        cmd.Parameters.AddWithValue("@userId", userId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // Изменение роли (для администратора)
        public bool ChangeUserRole(int userId, int newRoleId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE пользователи SET роль_ид = @roleId WHERE пользователь_ид = @userId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@roleId", newRoleId);
                        cmd.Parameters.AddWithValue("@userId", userId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }

    // Класс пользователя
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }
}