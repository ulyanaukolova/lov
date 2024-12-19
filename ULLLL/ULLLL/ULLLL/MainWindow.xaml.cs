using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace ULLLL
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Data Source=HOME-PC\\MSSQLSERVER01;Initial Catalog=MarketULLL;Integrated Security=True";

        public MainWindow()
        {
            InitializeComponent(); 

        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string lastName = LastNameTextBox.Text.Trim();
            string firstName = FirstNameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string phone = PhoneTextBox.Text.Trim(); // Добавляем телефон клиента

            // Проверка на заполненность обязательных полей
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Заполните все обязательные поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка на формат email
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Неправильный формат email.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Хеширование пароля перед сохранением
            string hashedPassword = HashPassword(password);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверка уникальности логина
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", username);
                        int count = (int)checkCommand.ExecuteScalar();

                        if (count > 0)
                        {
                            MessageBox.Show("Имя пользователя уже занято.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Добавление нового пользователя
                    string insertUserQuery = "INSERT INTO Users (Username, LastName, FirstName, Email, Password, Role) " +
                                             "VALUES (@Username, @LastName, @FirstName, @Email, @Password, @Role); " +
                                             "SELECT SCOPE_IDENTITY();";

                    int userId;
                    using (SqlCommand command = new SqlCommand(insertUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@LastName", lastName);
                        command.Parameters.AddWithValue("@FirstName", firstName);
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Password", hashedPassword);
                        command.Parameters.AddWithValue("@Role", "user");

                        userId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // Добавление записи в таблицу Clients
                    string insertClientQuery = "INSERT INTO Clients (User_ID, Phone) VALUES (@UserId, @Phone)";
                    using (SqlCommand clientCommand = new SqlCommand(insertClientQuery, connection))
                    {
                        clientCommand.Parameters.AddWithValue("@UserId", userId);
                        clientCommand.Parameters.AddWithValue("@Phone", phone);
                        clientCommand.ExecuteNonQuery();
                    }

                    MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    this.Close();
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Ошибка базы данных: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для проверки корректности email
        private bool IsValidEmail(string email)
        {
            return email.Contains("@") && email.Contains(".") && email.IndexOf('@') < email.LastIndexOf('.');
        }

        // Метод для хеширования пароля с использованием SHA-256
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2")); // Преобразуем каждый байт в шестнадцатеричное представление
                }
                return builder.ToString();
            }
        }
    }
}
