using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace ULLLL
{
    public partial class LoginWindow : Window
    {
        private string connectionString = "Data Source=HOME-PC\\MSSQLSERVER01;Initial Catalog=MarketULLL;Integrated Security=True";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все обязательные поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string hashedPassword = HashPassword(password); // Хеширование пароля

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Получаем роль пользователя
                    string queryRole = "SELECT Role FROM Users WHERE Username = @Username AND Password = @Password";
                    using (SqlCommand commandRole = new SqlCommand(queryRole, connection))
                    {
                        commandRole.Parameters.AddWithValue("@Username", username);
                        commandRole.Parameters.AddWithValue("@Password", hashedPassword);

                        object resultRole = commandRole.ExecuteScalar();

                        if (resultRole != null)
                        {
                            string role = resultRole.ToString();

                            // Теперь получаем Client_ID пользователя
                            int clientId = 0;
                            string queryClientId = "SELECT Client_ID FROM Clients JOIN Users ON Clients.User_ID = Users.User_ID WHERE Username = @Username";
                            using (SqlCommand commandClientId = new SqlCommand(queryClientId, connection))
                            {
                                commandClientId.Parameters.AddWithValue("@Username", username);

                                object resultClientId = commandClientId.ExecuteScalar();
                                if (resultClientId != null)
                                {
                                    clientId = Convert.ToInt32(resultClientId);
                                }
                            }

                            // Открытие нужного окна на основе роли
                            if (role == "admin")
                            {
                                AdminWindow adminWindow = new AdminWindow();
                                adminWindow.Show();
                            }
                            else if (role == "worker")
                            {
                                WorkerWindow workerWindow = new WorkerWindow();
                                workerWindow.Show();
                            }
                            else if (role == "user")
                            {
                                UserWindow userWindow = new UserWindow(clientId); // Передача Client_ID
                                userWindow.Show();
                            }

                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow registrationWindow = new MainWindow(); 
            registrationWindow.Show();
            this.Close();
        }
    }
}
