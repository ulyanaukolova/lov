using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OfficeOpenXml;
using OfficeOpenXml.Style;


namespace ULLLL
{
    public partial class AdminWindow : Window
    {
        private string connectionString = "Data Source=HOME-PC\\MSSQLSERVER01;Initial Catalog=MarketULLL;Integrated Security=True";
        private DispatcherTimer backupTimer;


        public AdminWindow()
        {
            InitializeComponent();
            LoadUsers();
            LoadProducts();
            LoadCategories();
            LoadClients();
            LoadOrders();
            LoadOrderItems();
            LoadClientsForOrders();
            LoadReviews();
            LoadReviewClientsAndProducts();
            LoadProductsForOrderItems();
            LoadOrdersForOrderItems();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            LoadClientsIntoComboBox();
            backupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(59)
            };
            backupTimer.Tick += BackupTimer_Tick;
            backupTimer.Start();
        }



        private void BackupTimer_Tick(object sender, EventArgs e)
        {
            CreateDatabaseBackup();
        }

        private void CreateDatabaseBackup()
        {
            string backupFilePath = $@"C:\Backups\MarketUL_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string backupQuery = $"BACKUP DATABASE MarketUL TO DISK = '{backupFilePath}'";

                    using (SqlCommand command = new SqlCommand(backupQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    var notification = new NotificationWindow("Бэкап успешно создан!");
                    notification.Show();

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании автоматического бэкапа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

       

        private void LoadReviewClientsAndProducts()
        {
            // Загрузка клиентов в ComboBox
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Загрузка клиентов
                string clientQuery = "SELECT Client_ID, Username FROM Clients INNER JOIN Users ON Clients.User_ID = Users.User_ID";
                SqlDataAdapter clientAdapter = new SqlDataAdapter(clientQuery, connection);
                DataTable clientsTable = new DataTable();
                clientAdapter.Fill(clientsTable);
                ReviewClientComboBox.ItemsSource = clientsTable.DefaultView;

                // Загрузка товаров
                string productQuery = "SELECT Product_ID, Name FROM Products";
                SqlDataAdapter productAdapter = new SqlDataAdapter(productQuery, connection);
                DataTable productsTable = new DataTable();
                productAdapter.Fill(productsTable);
                ReviewProductComboBox.ItemsSource = productsTable.DefaultView;
            }
        }


        private void LoadClientsForOrders()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // SQL-запрос для получения данных клиентов и пользователей
                    string query = "SELECT Clients.Client_ID, Users.Username " +
                                   "FROM Clients " +
                                   "JOIN Users ON Clients.User_ID = Users.User_ID";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable clientsTable = new DataTable();
                    adapter.Fill(clientsTable);

                    // Привязываем таблицу данных к ComboBox
                    OrderClientComboBox.ItemsSource = clientsTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка загрузки клиентов: " + ex.Message);
                }
            }
        }




        private void LoadProducts()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Products";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable usersTable = new DataTable();
                adapter.Fill(usersTable);
                ProductsDataGrid.ItemsSource = usersTable.DefaultView;
            }
        }




        private void LoadCategories()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Categories";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable categoriesTable = new DataTable();
                adapter.Fill(categoriesTable);
                CategoriesDataGrid.ItemsSource = categoriesTable.DefaultView;
            }
        }

        private void LoadClients()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT 
                        c.Client_ID, 
                        u.Username, 
                        c.Phone, 
                        c.RegistrationDate 
                    FROM Clients c
                    JOIN Users u ON c.User_ID = u.User_ID";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable clientsTable = new DataTable();
                adapter.Fill(clientsTable);
                ClientsDataGrid.ItemsSource = clientsTable.DefaultView;
            }
        }

        private void LoadOrders()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT 
                o.Order_ID, 
                c.Client_ID, 
                u.Username, 
                o.OrderDate, 
                o.TotalQuantity, 
                o.Status 
            FROM Orders o
            JOIN Clients c ON o.Client_ID = c.Client_ID  -- Предполагается, что в Orders есть поле Client_ID
            JOIN Users u ON c.User_ID = u.User_ID";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable ordersTable = new DataTable();
                adapter.Fill(ordersTable);
                OrdersDataGrid.ItemsSource = ordersTable.DefaultView;
            }
        }


        private void LoadOrderItems()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT 
                oi.OrderItem_ID, 
                oi.Product_ID,  -- Добавляем Product_ID
                p.Name AS ProductName, 
                o.Order_ID, 
                oi.Quantity, 
                oi.UnitPrice 
            FROM OrderItems oi
            JOIN Products p ON oi.Product_ID = p.Product_ID
            JOIN Orders o ON oi.Order_ID = o.Order_ID";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable orderItemsTable = new DataTable();
                adapter.Fill(orderItemsTable);
                OrderItemsDataGrid.ItemsSource = orderItemsTable.DefaultView;
            }
        }

       

        private void LoadReviews()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT Reviews.Review_ID, Reviews.Rating, Reviews.ReviewText, 
                   Products.Product_ID, Products.Name AS ProductName, 
                   Clients.Client_ID, Users.Username 
            FROM Reviews
            INNER JOIN Products ON Reviews.Product_ID = Products.Product_ID
            INNER JOIN Clients ON Reviews.Client_ID = Clients.Client_ID
            INNER JOIN Users ON Clients.User_ID = Users.User_ID";

                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable reviewsTable = new DataTable();
                adapter.Fill(reviewsTable);

                ReviewsDataGrid.ItemsSource = reviewsTable.DefaultView;
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка наличия данных в полях для ввода
                if (string.IsNullOrWhiteSpace(ProductNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(ProductPriceTextBox.Text) ||
                    string.IsNullOrWhiteSpace(ProductStockQuantityTextBox.Text) ||
                    string.IsNullOrWhiteSpace(ProductDescriptionTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля для добавления нового товара.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Получаем данные из полей ввода
                string productName = ProductNameTextBox.Text.Trim();
                decimal productPrice;
                int stockQuantity;

                if (!decimal.TryParse(ProductPriceTextBox.Text.Trim(), out productPrice) || productPrice <= 0)
                {
                    MessageBox.Show("Введите корректную цену товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!int.TryParse(ProductStockQuantityTextBox.Text.Trim(), out stockQuantity) || stockQuantity < 0)
                {
                    MessageBox.Show("Введите корректное количество товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string productDescription = ProductDescriptionTextBox.Text.Trim();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для добавления нового товара
                    string query = @"
            INSERT INTO Products (Name, Price, StockQuantity, Description) 
            VALUES (@Name, @Price, @StockQuantity, @Description)";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Name", productName);
                    command.Parameters.AddWithValue("@Price", productPrice);
                    command.Parameters.AddWithValue("@StockQuantity", stockQuantity);
                    command.Parameters.AddWithValue("@Description", productDescription);

                    // Выполняем запрос на добавление
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Новый товар успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadProducts(); // Метод для обновления DataGrid после добавления товара
                    }
                    else
                    {
                        MessageBox.Show("Не удалось добавить новый товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при добавлении товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProductsDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите товар для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Получаем ID выбранного товара
                DataRowView selectedRow = (DataRowView)ProductsDataGrid.SelectedItem;
                int productId = Convert.ToInt32(selectedRow["Product_ID"]);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для удаления товара
                    string query = "DELETE FROM Products WHERE Product_ID = @Product_ID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Product_ID", productId);

                    // Выполняем запрос на удаление
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Товар успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadProducts(); // Метод для обновления DataGrid после удаления
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при удалении товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void UpdateProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProductsDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите товар для обновления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Проверка наличия данных в полях для ввода
                if (string.IsNullOrWhiteSpace(ProductNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(ProductPriceTextBox.Text) ||
                    string.IsNullOrWhiteSpace(ProductStockQuantityTextBox.Text) ||
                    string.IsNullOrWhiteSpace(ProductDescriptionTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля для обновления товара.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Получаем данные из полей ввода
                string productName = ProductNameTextBox.Text.Trim();
                decimal productPrice;
                int stockQuantity;

                // Проверка корректности ввода цены и количества
                if (!decimal.TryParse(ProductPriceTextBox.Text.Trim(), out productPrice) || productPrice <= 0)
                {
                    MessageBox.Show("Введите корректную цену товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!int.TryParse(ProductStockQuantityTextBox.Text.Trim(), out stockQuantity) || stockQuantity < 0)
                {
                    MessageBox.Show("Введите корректное количество товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string productDescription = ProductDescriptionTextBox.Text.Trim();

                // Получаем ID выбранного товара
                DataRowView selectedRow = (DataRowView)ProductsDataGrid.SelectedItem;
                int productId = Convert.ToInt32(selectedRow["Product_ID"]);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для обновления товара
                    string query = @"
            UPDATE Products
            SET Name = @Name, Price = @Price, StockQuantity = @StockQuantity, Description = @Description
            WHERE Product_ID = @Product_ID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Name", productName);
                    command.Parameters.AddWithValue("@Price", productPrice);
                    command.Parameters.AddWithValue("@StockQuantity", stockQuantity);
                    command.Parameters.AddWithValue("@Description", productDescription);
                    command.Parameters.AddWithValue("@Product_ID", productId);

                    // Выполняем запрос на обновление
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Товар успешно обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadProducts(); // Метод для обновления DataGrid после обновления
                    }
                    else
                    {
                        MessageBox.Show("Не удалось обновить товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при обновлении товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUsers()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Users";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable usersTable = new DataTable();
                adapter.Fill(usersTable);
                UsersDataGrid.ItemsSource = usersTable.DefaultView;
            }
        }
        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка наличия данных в полях для ввода
                if (string.IsNullOrWhiteSpace(UserEmailTextBox.Text) || string.IsNullOrWhiteSpace(UserLastNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(UserFirstNameTextBox.Text) || string.IsNullOrWhiteSpace(UserNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(UserRoleComboBox.Text) || string.IsNullOrWhiteSpace(PasswordTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля для добавления нового пользователя.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Получаем данные из полей ввода
                string username = UserNameTextBox.Text.Trim();
                string lastName = UserLastNameTextBox.Text.Trim();
                string firstName = UserFirstNameTextBox.Text.Trim();
                string email = UserEmailTextBox.Text.Trim();
                string pass = PasswordTextBox.Text.Trim();
                string role = UserRoleComboBox.Text;

                // TODO: Замените на механизм хеширования паролей
                string hashedPassword = HashPassword(pass); // Функция для хеширования пароля

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверка, существует ли уже пользователь с таким же именем
                    string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                    SqlCommand checkUserCommand = new SqlCommand(checkUserQuery, connection);
                    checkUserCommand.Parameters.AddWithValue("@Username", username);
                    int userCount = (int)checkUserCommand.ExecuteScalar();

                    if (userCount > 0)
                    {
                        MessageBox.Show("Пользователь с таким именем уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // SQL-запрос для добавления нового пользователя
                    string query = @"
            INSERT INTO Users (Username, LastName, FirstName, Password, Email, Role) 
            VALUES (@Username, @LastName, @FirstName, @Password, @Email, @Role)";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@LastName", lastName);
                    command.Parameters.AddWithValue("@FirstName", firstName);
                    command.Parameters.AddWithValue("@Password", hashedPassword); // Используем зашифрованный пароль
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Role", role);

                    // Выполняем запрос на добавление
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Новый пользователь успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadUsers(); // Метод для обновления DataGrid после добавления
                    }
                    else
                    {
                        MessageBox.Show("Не удалось добавить нового пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при добавлении нового пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        private void ExportUsersToCSV(string filePath)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM Users";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        // Записываем заголовки
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            writer.Write(reader.GetName(i));
                            if (i < reader.FieldCount - 1)
                                writer.Write(",");
                        }
                        writer.WriteLine();

                        // Записываем строки данных
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                writer.Write(reader[i].ToString());
                                if (i < reader.FieldCount - 1)
                                    writer.Write(",");
                            }
                            writer.WriteLine();
                        }
                    }
                }

                MessageBox.Show("Экспорт данных успешно выполнен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для удаления.");
                return;
            }

            DataRowView selectedRow = (DataRowView)UsersDataGrid.SelectedItem;
            int userId = Convert.ToInt32(selectedRow["User_ID"]);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Проверка связи с таблицей Clients
                string clientQuery = "SELECT COUNT(*) FROM Clients WHERE User_ID = @User_ID";
                using (SqlCommand clientCommand = new SqlCommand(clientQuery, connection))
                {
                    clientCommand.Parameters.AddWithValue("@User_ID", userId);
                    int clientCount = (int)clientCommand.ExecuteScalar();

                    if (clientCount > 0)
                    {
                        MessageBox.Show("Невозможно удалить пользователя, так как он связан с записями в таблице клиентов.");
                        return;
                    }
                }

                // Проверка связи с таблицей Orders
                string orderQuery = @"
            SELECT COUNT(*) 
            FROM Orders o
            JOIN Clients c ON o.Client_ID = c.Client_ID
            WHERE c.User_ID = @User_ID";
                using (SqlCommand orderCommand = new SqlCommand(orderQuery, connection))
                {
                    orderCommand.Parameters.AddWithValue("@User_ID", userId);
                    int orderCount = (int)orderCommand.ExecuteScalar();

                    if (orderCount > 0)
                    {
                        MessageBox.Show("Невозможно удалить пользователя, так как он связан с заказами.");
                        return;
                    }
                }

                // Проверка связи с таблицей Reviews
                string reviewQuery = @"
            SELECT COUNT(*) 
            FROM Reviews r
            JOIN Clients c ON r.Client_ID = c.Client_ID
            WHERE c.User_ID = @User_ID";
                using (SqlCommand reviewCommand = new SqlCommand(reviewQuery, connection))
                {
                    reviewCommand.Parameters.AddWithValue("@User_ID", userId);
                    int reviewCount = (int)reviewCommand.ExecuteScalar();

                    if (reviewCount > 0)
                    {
                        MessageBox.Show("Невозможно удалить пользователя, так как он оставил отзывы.");
                        return;
                    }
                }

                // Если все проверки пройдены, выполняем удаление
                string deleteQuery = "DELETE FROM Users WHERE User_ID = @User_ID";
                using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@User_ID", userId);
                    deleteCommand.ExecuteNonQuery();
                }

                MessageBox.Show("Пользователь успешно удален.");
                LoadUsers();
            }
        }

        private void UpdateUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка, выбран ли пользователь для обновления
                if (UsersDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите пользователя для обновления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Проверка наличия данных в полях для ввода
                if (string.IsNullOrWhiteSpace(UserEmailTextBox.Text) || string.IsNullOrWhiteSpace(UserLastNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(UserFirstNameTextBox.Text) || string.IsNullOrWhiteSpace(UserNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(UserRoleComboBox.Text) || string.IsNullOrWhiteSpace(PasswordTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля для обновления пользователя.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Получаем данные из полей ввода
                string username = UserNameTextBox.Text.Trim();
                string lastName = UserLastNameTextBox.Text.Trim();
                string firstName = UserFirstNameTextBox.Text.Trim();
                string email = UserEmailTextBox.Text.Trim();
                string pass = PasswordTextBox.Text.Trim();
                string role = UserRoleComboBox.Text;

                // Получаем ID выбранного пользователя из DataGrid
                DataRowView selectedRow = (DataRowView)UsersDataGrid.SelectedItem;
                int userId = Convert.ToInt32(selectedRow["User_ID"]);

                // TODO: Замените на механизм хеширования паролей
                string hashedPassword = HashPassword(pass); // Функция для хеширования пароля

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для обновления данных пользователя
                    string query = @"
            UPDATE Users
            SET Username = @Username, LastName = @LastName, FirstName = @FirstName, 
                Password = @Password, Email = @Email, Role = @Role
            WHERE User_ID = @UserId";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@LastName", lastName);
                    command.Parameters.AddWithValue("@FirstName", firstName);
                    command.Parameters.AddWithValue("@Password", hashedPassword); // Используем зашифрованный пароль
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Role", role);
                    command.Parameters.AddWithValue("@UserId", userId);

                    // Выполняем запрос на обновление
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Данные пользователя успешно обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadUsers(); // Метод для обновления DataGrid после обновления
                    }
                    else
                    {
                        MessageBox.Show("Не удалось обновить данные пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при обновлении данных пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Пример хеширования пароля, замените на реальный алгоритм



        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            string categoryName = CategoryNameTextBox.Text.Trim(); // Предполагаем, что есть TextBox для имени категории

            if (string.IsNullOrEmpty(categoryName))
            {
                MessageBox.Show("Имя категории не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Categories (CategoryName) VALUES (@CategoryName)";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CategoryName", categoryName);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    MessageBox.Show("Категория успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadCategories(); // Метод для обновления списка категорий в интерфейсе
                }
                else
                {
                    MessageBox.Show("Не удалось добавить категорию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Предполагаем, что есть DataGrid для отображения категорий и выбранная категория в нем
            if (CategoriesDataGrid.SelectedItem is DataRowView selectedCategory)
            {
                int categoryId = (int)selectedCategory["Category_ID"]; // Получаем ID выбранной категории

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM Categories WHERE Category_ID = @CategoryId";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CategoryId", categoryId);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Категория успешно удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadCategories(); // Обновляем список категорий в интерфейсе
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить категорию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите категорию для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void UpdateCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesDataGrid.SelectedItem is DataRowView selectedCategory)
            {
                int categoryId = (int)selectedCategory["Category_ID"]; // Получаем ID выбранной категории
                string newCategoryName = CategoryNameTextBox.Text.Trim(); // Предполагаем, что есть TextBox для ввода нового имени

                if (string.IsNullOrEmpty(newCategoryName))
                {
                    MessageBox.Show("Имя категории не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE Categories SET CategoryName = @CategoryName WHERE Category_ID = @CategoryId";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CategoryName", newCategoryName);
                    command.Parameters.AddWithValue("@CategoryId", categoryId);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Категория успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadCategories();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось обновить категорию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите категорию для обновления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void AddClientButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientUsernameComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                int userId = (int)selectedItem.Tag; // Получаем User_ID выбранного клиента
                string phone = ClientPhoneTextBox.Text.Trim();
                DateTime? registrationDate = ClientRegistrationDatePicker.SelectedDate;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO Clients (User_ID, Phone, RegistrationDate) VALUES (@UserId, @Phone, @RegistrationDate)";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@RegistrationDate", registrationDate ?? DateTime.Now);

                    command.ExecuteNonQuery();
                    MessageBox.Show("Клиент успешно добавлен!");
                    LoadClients(); // Обновляем DataGrid
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите клиента из списка.");
            }
        }


        private void DeleteClientButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранного клиента из DataGrid
            var selectedClient = ClientsDataGrid.SelectedItem as DataRowView;
            if (selectedClient == null)
            {
                MessageBox.Show("Пожалуйста, выберите клиента для удаления.");
                return;
            }

            var clientId = selectedClient["Client_ID"];

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Удаляем связанные отзывы
                SqlCommand deleteReviewsCommand = new SqlCommand("DELETE FROM Reviews WHERE Client_ID = @ClientId", connection);
                deleteReviewsCommand.Parameters.AddWithValue("@ClientId", clientId);
                deleteReviewsCommand.ExecuteNonQuery();

                SqlCommand deleteOrdersCommand = new SqlCommand("DELETE FROM Orders WHERE Client_ID = @ClientId", connection);
                deleteOrdersCommand.Parameters.AddWithValue("@ClientId", clientId);
                deleteOrdersCommand.ExecuteNonQuery();

                // Удаляем клиента
                SqlCommand deleteClientCommand = new SqlCommand("DELETE FROM Clients WHERE Client_ID = @ClientId", connection);
                deleteClientCommand.Parameters.AddWithValue("@ClientId", clientId);
                deleteClientCommand.ExecuteNonQuery();

                MessageBox.Show("Клиент успешно удален.");
                LoadClients();
                LoadReviews();
            }
        }



        private void UpdateClientButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is DataRowView selectedRow)
            {
                int clientId = (int)selectedRow["Client_ID"];
                if (ClientUsernameComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    int userId = (int)selectedItem.Tag; // Получаем User_ID выбранного клиента
                    string phone = ClientPhoneTextBox.Text.Trim();
                    DateTime? registrationDate = ClientRegistrationDatePicker.SelectedDate;

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "UPDATE Clients SET User_ID = @UserId, Phone = @Phone, RegistrationDate = @RegistrationDate WHERE Client_ID = @ClientId";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@Phone", phone);
                        command.Parameters.AddWithValue("@RegistrationDate", registrationDate ?? DateTime.Now);
                        command.Parameters.AddWithValue("@ClientId", clientId);

                        command.ExecuteNonQuery();
                        MessageBox.Show("Данные клиента успешно обновлены!");
                        LoadClients();
                    }
                }
                else
                {
                    MessageBox.Show("Пожалуйста, выберите клиента из списка.");
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите клиента для обновления.");
            }
        }


        private void AddOrderButton_Click(object sender, RoutedEventArgs e)
        {
            // Получение значений из элементов формы
            var selectedClientId = OrderClientComboBox.SelectedValue;  // Идентификатор клиента
            var orderDate = OrderDatePicker.SelectedDate;  // Дата заказа
            var orderStatus = OrderStatusTextBox.Text;
            var price = OrderTotalQuantityTextBox.Text;// Статус заказа

            // Проверка на наличие обязательных данных
            if (selectedClientId == null || orderDate == null || price == null || string.IsNullOrWhiteSpace(orderStatus))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            // Подключение к базе данных
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // SQL-запрос для добавления нового заказа
                SqlCommand command = new SqlCommand(
                    "INSERT INTO Orders (Client_ID, OrderDate, Status, TotalQuantity) VALUES (@ClientId, @OrderDate, @Status, @TotalQuantity)",
                    connection);

                // Передача параметров в запрос
                command.Parameters.AddWithValue("@ClientId", selectedClientId);
                command.Parameters.AddWithValue("@OrderDate", orderDate);
                command.Parameters.AddWithValue("@Status", orderStatus);
                command.Parameters.AddWithValue("@TotalQuantity", price);


                // Выполнение запроса
                command.ExecuteNonQuery();
                MessageBox.Show("Новый заказ успешно добавлен.");
                LoadOrders();  // Обновляем таблицу заказов
            }
        }


        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is DataRowView selectedRow)
            {
                // Извлекаем Order_ID из выбранной строки
                int selectedOrderId = (int)selectedRow["Order_ID"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Сначала удаляем все записи из OrderItems
                    string deleteOrderItemsQuery = "DELETE FROM OrderItems WHERE Order_ID = @OrderId";
                    using (SqlCommand command = new SqlCommand(deleteOrderItemsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@OrderId", selectedOrderId);
                        command.ExecuteNonQuery();
                    }

                    // Затем удаляем все записи из OrderHistory
                    string deleteOrderHistoryQuery = "DELETE FROM OrderHistory WHERE Order_ID = @OrderId";
                    using (SqlCommand command = new SqlCommand(deleteOrderHistoryQuery, connection))
                    {
                        command.Parameters.AddWithValue("@OrderId", selectedOrderId);
                        command.ExecuteNonQuery();
                    }

                    // Теперь можно удалить сам заказ
                    string deleteOrderQuery = "DELETE FROM Orders WHERE Order_ID = @OrderId";
                    using (SqlCommand command = new SqlCommand(deleteOrderQuery, connection))
                    {
                        command.Parameters.AddWithValue("@OrderId", selectedOrderId);
                        command.ExecuteNonQuery();
                    }
                }

                // Обновите ваш DataGrid или выполните другие действия после удаления
                LoadOrders();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите заказ для удаления.");
            }
        }




        private void UpdateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка на наличие выбранного заказа
            if (OrdersDataGrid.SelectedItem is DataRowView selectedRow)
            {
                // Получение идентификатора выбранного заказа
                int selectedOrderId = (int)selectedRow["Order_ID"];

                // Получение новых значений из элементов формы
                var selectedClientId = OrderClientComboBox.SelectedValue;  // Идентификатор клиента
                var orderDate = OrderDatePicker.SelectedDate;  // Дата заказа
                var orderStatus = OrderStatusTextBox.Text;
                var sum = OrderTotalQuantityTextBox.Text;// Статус заказа

                // Проверка на наличие обязательных данных
                if (selectedClientId == null || orderDate == null || sum == null || string.IsNullOrWhiteSpace(orderStatus))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля.");
                    return;
                }

                // Подключение к базе данных
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // SQL-запрос для обновления заказа
                    SqlCommand command = new SqlCommand(
                        "UPDATE Orders SET Client_ID = @ClientId, OrderDate = @OrderDate, TotalQuantity=@TotalQuantity, Status = @Status WHERE Order_ID = @OrderId",
                        connection);

                    // Передача параметров в запрос
                    command.Parameters.AddWithValue("@ClientId", selectedClientId);
                    command.Parameters.AddWithValue("@OrderDate", orderDate);
                    command.Parameters.AddWithValue("@Status", orderStatus);
                    command.Parameters.AddWithValue("@TotalQuantity", sum);

                    command.Parameters.AddWithValue("@OrderId", selectedOrderId);  // Идентификатор заказа для обновления

                    // Выполнение запроса
                    command.ExecuteNonQuery();
                    MessageBox.Show("Заказ успешно обновлён.");
                    LoadOrders();  // Обновляем таблицу заказов
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите заказ для обновления.");
            }
        }





        private void AddOrderItemButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка наличия данных в полях для ввода
                if (OrderItemProductComboBox.SelectedItem == null || OrderItemOrderComboBox.SelectedItem == null ||
                    string.IsNullOrWhiteSpace(OrderItemQuantityTextBox.Text) || string.IsNullOrWhiteSpace(OrderItemUnitPriceTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля для добавления товара в заказ.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Получаем данные из полей ввода
                DataRowView selectedProduct = (DataRowView)OrderItemProductComboBox.SelectedItem;
                int productId = Convert.ToInt32(selectedProduct["Product_ID"]);

                DataRowView selectedOrder = (DataRowView)OrderItemOrderComboBox.SelectedItem;
                int orderId = Convert.ToInt32(selectedOrder["Order_ID"]);

                int quantity;
                decimal unitPrice;

                // Проверка корректности ввода количества и цены
                if (!int.TryParse(OrderItemQuantityTextBox.Text.Trim(), out quantity) || quantity <= 0)
                {
                    MessageBox.Show("Введите корректное количество.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!decimal.TryParse(OrderItemUnitPriceTextBox.Text.Trim(), out unitPrice) || unitPrice <= 0)
                {
                    MessageBox.Show("Введите корректную цену за единицу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для добавления товара в заказ
                    string query = @"
            INSERT INTO OrderItems (Product_ID, Order_ID, Quantity, UnitPrice)
            VALUES (@Product_ID, @Order_ID, @Quantity, @UnitPrice)";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Product_ID", productId);
                    command.Parameters.AddWithValue("@Order_ID", orderId);
                    command.Parameters.AddWithValue("@Quantity", quantity);
                    command.Parameters.AddWithValue("@UnitPrice", unitPrice);

                    // Выполняем запрос на добавление
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Товар успешно добавлен в заказ.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadOrderItems(); // Метод для обновления DataGrid после добавления
                    }
                    else
                    {
                        MessageBox.Show("Не удалось добавить товар в заказ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при добавлении товара в заказ: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void DeleteOrderItemButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем выбранную строку из DataGrid
                DataRowView selectedRow = (DataRowView)OrderItemsDataGrid.SelectedItem;

                if (selectedRow == null)
                {
                    MessageBox.Show("Пожалуйста, выберите элемент для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int orderItemId = Convert.ToInt32(selectedRow["OrderItem_ID"]);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для удаления товара в заказе
                    string query = "DELETE FROM OrderItems WHERE OrderItem_ID = @OrderItem_ID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@OrderItem_ID", orderItemId);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Элемент успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadOrderItems(); // Метод для обновления DataGrid после удаления
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить элемент.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при удалении элемента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void UpdateOrderItemButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем выбранную строку из DataGrid
                DataRowView selectedRow = (DataRowView)OrderItemsDataGrid.SelectedItem;

                if (selectedRow == null)
                {
                    MessageBox.Show("Пожалуйста, выберите элемент для обновления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int orderItemId = Convert.ToInt32(selectedRow["OrderItem_ID"]);
                int productId = Convert.ToInt32(((DataRowView)OrderItemProductComboBox.SelectedItem)["Product_ID"]);
                int orderId = Convert.ToInt32(((DataRowView)OrderItemOrderComboBox.SelectedItem)["Order_ID"]);
                int quantity = Convert.ToInt32(OrderItemQuantityTextBox.Text);
                decimal unitPrice = Convert.ToDecimal(OrderItemUnitPriceTextBox.Text);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для обновления товара в заказе
                    string query = @"
                UPDATE OrderItems 
                SET Product_ID = @Product_ID, Order_ID = @Order_ID, Quantity = @Quantity, UnitPrice = @UnitPrice
                WHERE OrderItem_ID = @OrderItem_ID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@OrderItem_ID", orderItemId);
                    command.Parameters.AddWithValue("@Product_ID", productId);
                    command.Parameters.AddWithValue("@Order_ID", orderId);
                    command.Parameters.AddWithValue("@Quantity", quantity);
                    command.Parameters.AddWithValue("@UnitPrice", unitPrice);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Элемент успешно обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadOrderItems(); // Метод для обновления DataGrid после обновления
                    }
                    else
                    {
                        MessageBox.Show("Не удалось обновить элемент.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при обновлении элемента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


       

        

       

        private void AddReviewButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем значения из полей
            var clientId = ReviewClientComboBox.SelectedValue;
            var productId = ReviewProductComboBox.SelectedValue;
            var rating = ReviewRatingTextBox.Text;
            var reviewText = ReviewTextTextBox.Text;

            // Проверка на заполненность полей
            if (clientId == null || productId == null || string.IsNullOrWhiteSpace(rating) || string.IsNullOrWhiteSpace(reviewText))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            // Подключение к базе данных
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // SQL-запрос для добавления отзыва
                SqlCommand command = new SqlCommand("INSERT INTO Reviews (Client_ID, Product_ID, Rating, ReviewText) VALUES (@ClientId, @ProductId, @Rating, @ReviewText)", connection);
                command.Parameters.AddWithValue("@ClientId", clientId);
                command.Parameters.AddWithValue("@ProductId", productId);
                command.Parameters.AddWithValue("@Rating", rating);
                command.Parameters.AddWithValue("@ReviewText", reviewText);

                // Выполнение запроса
                command.ExecuteNonQuery();
                MessageBox.Show("Отзыв добавлен.");
                LoadReviews();  // Обновляем таблицу отзывов
            }
        }

        private void DeleteReviewButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранный отзыв из DataGrid
            var selectedReview = ReviewsDataGrid.SelectedItem as DataRowView;
            if (selectedReview == null)
            {
                MessageBox.Show("Пожалуйста, выберите отзыв для удаления.");
                return;
            }

            var reviewId = selectedReview["Review_ID"];

            // Подключение к базе данных
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // SQL-запрос для удаления отзыва
                SqlCommand command = new SqlCommand("DELETE FROM Reviews WHERE Review_ID = @ReviewId", connection);
                command.Parameters.AddWithValue("@ReviewId", reviewId);

                // Выполнение запроса
                command.ExecuteNonQuery();
                MessageBox.Show("Отзыв удален.");
                LoadReviews();  // Обновляем таблицу отзывов
            }
        }


        private void UpdateReviewButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранный отзыв из DataGrid
            var selectedReview = ReviewsDataGrid.SelectedItem as DataRowView;
            if (selectedReview == null)
            {
                MessageBox.Show("Пожалуйста, выберите отзыв для обновления.");
                return;
            }

            // Получаем значения из полей формы
            var reviewId = selectedReview["Review_ID"];
            var newClientId = ReviewClientComboBox.SelectedValue;  // Новое значение клиента
            var newProductId = ReviewProductComboBox.SelectedValue;  // Новое значение товара
            var newRating = ReviewRatingTextBox.Text;  // Новое значение рейтинга
            var newReviewText = ReviewTextTextBox.Text;  // Новый текст отзыва

            // Проверка на заполненность полей
            if (newClientId == null || newProductId == null || string.IsNullOrWhiteSpace(newRating) || string.IsNullOrWhiteSpace(newReviewText))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            // Подключение к базе данных
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // SQL-запрос для обновления всех полей отзыва
                SqlCommand command = new SqlCommand(
                    "UPDATE Reviews SET Client_ID = @ClientId, Product_ID = @ProductId, Rating = @Rating, ReviewText = @ReviewText WHERE Review_ID = @ReviewId",
                    connection);

                // Передаем новые значения параметрам запроса
                command.Parameters.AddWithValue("@ClientId", newClientId);
                command.Parameters.AddWithValue("@ProductId", newProductId);
                command.Parameters.AddWithValue("@Rating", newRating);
                command.Parameters.AddWithValue("@ReviewText", newReviewText);
                command.Parameters.AddWithValue("@ReviewId", reviewId);

                // Выполнение запроса
                command.ExecuteNonQuery();
                MessageBox.Show("Отзыв обновлен.");
                LoadReviews();  // Обновляем таблицу отзывов
            }
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem != null)
            {
                DataRowView selectedRow = (DataRowView)UsersDataGrid.SelectedItem;

                UserNameTextBox.Text = selectedRow["Username"].ToString();
                UserEmailTextBox.Text = selectedRow["Email"].ToString();
                UserFirstNameTextBox.Text = selectedRow["FirstName"].ToString();
                UserLastNameTextBox.Text = selectedRow["LastName"].ToString();
                string userRole = selectedRow["Role"].ToString();
                UserRoleComboBox.SelectedItem = userRole;
            }
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem != null)
            {
                DataRowView selectedRow = (DataRowView)ProductsDataGrid.SelectedItem;

                ProductNameTextBox.Text = selectedRow["Name"].ToString();
                ProductPriceTextBox.Text = selectedRow["Price"].ToString();
                ProductStockQuantityTextBox.Text = selectedRow["StockQuantity"].ToString();
                ProductDescriptionTextBox.Text = selectedRow["Description"].ToString();

            }
        }


        private void LoadProductsForOrderItems()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Product_ID, Name FROM Products";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable productsTable = new DataTable();
                adapter.Fill(productsTable);

                // Привязка данных к ComboBox для товаров
                OrderItemProductComboBox.ItemsSource = productsTable.DefaultView;
                OrderItemProductComboBox.DisplayMemberPath = "Name"; // Имя продукта
                OrderItemProductComboBox.SelectedValuePath = "Product_ID"; // ID продукта
            }
        }
        private void LoadOrdersForOrderItems()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Order_ID, OrderDate FROM Orders";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable ordersTable = new DataTable();
                adapter.Fill(ordersTable);

                // Привязка данных к ComboBox для заказов
                OrderItemOrderComboBox.ItemsSource = ordersTable.DefaultView;
                OrderItemOrderComboBox.DisplayMemberPath = "OrderDate"; // Дата заказа
                OrderItemOrderComboBox.SelectedValuePath = "Order_ID"; // ID заказа
            }
        }


        private void OrderItemsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrderItemsDataGrid.SelectedItem != null)
            {
                DataRowView row = OrderItemsDataGrid.SelectedItem as DataRowView;

                // Приведение к числовому типу для ComboBox
                int productId, orderId;
                if (int.TryParse(row["Product_ID"].ToString(), out productId))
                {
                    OrderItemProductComboBox.SelectedValue = productId;
                }

                if (int.TryParse(row["Order_ID"].ToString(), out orderId))
                {
                    OrderItemOrderComboBox.SelectedValue = orderId;
                }

                // Заполнение других текстовых полей
                OrderItemQuantityTextBox.Text = row["Quantity"].ToString();
                OrderItemUnitPriceTextBox.Text = row["UnitPrice"].ToString();
            }
        }



        

        private void CategoriesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoriesDataGrid.SelectedItem != null)
            {
                DataRowView selectedRow = (DataRowView)CategoriesDataGrid.SelectedItem;
                CategoryNameTextBox.Text = selectedRow["CategoryName"].ToString();
            }
        }
        private void ClientsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem != null)
            {
                DataRowView selectedRow = (DataRowView)ClientsDataGrid.SelectedItem;

                ClientUsernameComboBox.Text = selectedRow["Username"].ToString();
                ClientPhoneTextBox.Text = selectedRow["Phone"].ToString();

                if (selectedRow["RegistrationDate"] != DBNull.Value)
                {
                    ClientRegistrationDatePicker.SelectedDate = Convert.ToDateTime(selectedRow["RegistrationDate"]);
                }
                else
                {
                    ClientRegistrationDatePicker.SelectedDate = null;
                }
            }
        }

        private void OrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is DataRowView selectedRow)
            {
                // Заполняем элементы управления данными из выбранной строки
                OrderTotalQuantityTextBox.Text = selectedRow["TotalQuantity"].ToString();
                OrderStatusTextBox.Text = selectedRow["Status"].ToString();

                if (DateTime.TryParse(selectedRow["OrderDate"].ToString(), out DateTime orderDate))
                {
                    OrderDatePicker.SelectedDate = orderDate;
                }

                OrderClientComboBox.SelectedValue = selectedRow["Client_ID"];

                // Получение ID заказа
                int orderId = Convert.ToInt32(selectedRow["Order_ID"]);

                // Загрузка деталей заказа
                LoadOrderDetails(orderId);
            }
        }

        private void LoadOrderDetails(int orderId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    p.Name AS ProductName,
                    oi.Quantity,
                    oi.UnitPrice,
                    (oi.Quantity * oi.UnitPrice) AS TotalPrice
                FROM OrderItems oi
                JOIN Products p ON oi.Product_ID = p.Product_ID
                WHERE oi.Order_ID = @OrderID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@OrderID", orderId);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable orderDetailsTable = new DataTable();
                    adapter.Fill(orderDetailsTable);

                    OrderDetailsDataGrid.ItemsSource = orderDetailsTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке деталей заказа: {ex.Message}");
                }
            }
        }

        private void ReviewsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReviewsDataGrid.SelectedItem != null)
            {
                DataRowView selectedRow = (DataRowView)ReviewsDataGrid.SelectedItem;

                ReviewRatingTextBox.Text = selectedRow["Rating"].ToString();
                ReviewTextTextBox.Text = selectedRow["ReviewText"].ToString();

                ReviewClientComboBox.SelectedValue = selectedRow["Client_ID"];
                ReviewProductComboBox.SelectedValue = selectedRow["Product_ID"];
            }
        }

       


       

        private void SearchOrderItemsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchOrderItemsTextBox.Text.Trim();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT oi.OrderItem_ID, p.Name AS ProductName, o.Order_ID, oi.Quantity, oi.UnitPrice
            FROM OrderItems oi
            JOIN Products p ON oi.Product_ID = p.Product_ID
            JOIN Orders o ON oi.Order_ID = o.Order_ID
            WHERE p.Name LIKE @SearchText OR o.Order_ID LIKE @SearchText";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                OrderItemsDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }


        private void SearchOrderItemsButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchOrderItemsTextBox.Text.Trim();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT oi.OrderItem_ID, p.Name AS ProductName, o.Order_ID, oi.Quantity, oi.UnitPrice
            FROM OrderItems oi
            JOIN Products p ON oi.Product_ID = p.Product_ID
            JOIN Orders o ON oi.Order_ID = o.Order_ID
            WHERE p.Name LIKE @SearchText OR o.Order_ID LIKE @SearchText";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                OrderItemsDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }

        private void SearchReviewsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchReviewsTextBox.Text.Trim();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT r.Review_ID, r.Rating, r.ReviewText, p.Name AS ProductName, 
                   c.Client_ID, u.Username AS ClientName
            FROM Reviews r
            JOIN Products p ON r.Product_ID = p.Product_ID
            JOIN Clients c ON r.Client_ID = c.Client_ID
            JOIN Users u ON c.User_ID = u.User_ID
            WHERE r.ReviewText LIKE @SearchText OR u.Username LIKE @SearchText";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                ReviewsDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }


        private void SearchReviewsButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchReviewsTextBox.Text.Trim();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT r.Review_ID, r.Rating, r.ReviewText, p.Name AS ProductName, 
                   c.Client_ID, u.Username AS ClientName
            FROM Reviews r
            JOIN Products p ON r.Product_ID = p.Product_ID
            JOIN Clients c ON r.Client_ID = c.Client_ID
            JOIN Users u ON c.User_ID = u.User_ID
            WHERE r.ReviewText LIKE @SearchText OR u.Username LIKE @SearchText";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                ReviewsDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }

        private void SearchOrdersTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchOrders();
        }


        private void SearchOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            SearchOrders();

        }
        private void SearchOrders()
        {
            string searchText = SearchOrdersTextBox.Text.Trim();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT o.Order_ID, c.Client_ID, u.Username AS ClientName, 
                   o.OrderDate, o.TotalQuantity, o.Status
            FROM Orders o
            JOIN Clients c ON o.Client_ID = c.Client_ID
            JOIN Users u ON c.User_ID = u.User_ID
            WHERE u.Username LIKE @SearchText
            OR o.Status LIKE @SearchText
            OR o.TotalQuantity LIKE @SearchText
            OR CONVERT(VARCHAR, o.OrderDate, 120) LIKE @SearchText";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                OrdersDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }

        private void ClientSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchClients();
        }

        private void SearchClientButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void SearchClients()
        {
            string searchText = ClientSearchTextBox.Text.Trim(); // Получаем текст из текстбокса для поиска

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT c.Client_ID, u.Username, c.Phone, 
                   CONVERT(VARCHAR, c.RegistrationDate, 120) AS RegistrationDate
            FROM Clients c
            JOIN Users u ON c.User_ID = u.User_ID
            WHERE u.Username LIKE @SearchText
            OR c.Client_ID LIKE @SearchText
            OR c.Phone LIKE @SearchText
            OR CONVERT(VARCHAR, c.RegistrationDate, 120) LIKE @SearchText";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                ClientsDataGrid.ItemsSource = searchResultsTable.DefaultView; // Привязываем результаты к DataGrid
            }
        }

        private void SearchUsers()
        {
            string searchText = UserSearchTextBox.Text.Trim();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT User_ID, Username, LastName, FirstName, Email, Role
            FROM Users
            WHERE Username LIKE @SearchText
            OR LastName LIKE @SearchText
            OR FirstName LIKE @SearchText
            OR Email LIKE @SearchText";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                UsersDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }
        private void SearchCategories()
        {
            string searchText = SearchCategoriesTextBox.Text.Trim();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT Category_ID, CategoryName
            FROM Categories
            WHERE CategoryName LIKE @SearchText";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                CategoriesDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }
        private void SearchProducts()
        {
            string searchText = SearchProductsTextBox.Text.Trim();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT Product_ID, Name, Price, StockQuantity, Description, CreationDate
            FROM Products
            WHERE Name LIKE @SearchText
            OR Price LIKE @SearchText
            OR StockQuantity LIKE @SearchText
            OR Description LIKE @SearchText";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                ProductsDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }


        private void SearchUsersTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchUsers();
        }

        private void SearchProductsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchProducts();
        }

        private void SearchCategoriesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchCategories();
        }

        private void UserSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterAndSortUsers();
        }

        private void FilterAndSortUsers()
        {
            string searchText = UserSearchTextBox.Text.Trim();

            // Получаем выбранные параметры сортировки
            string sortColumn = ((ComboBoxItem)UserSortColumnComboBox.SelectedItem)?.Tag.ToString() ?? "Username";
            string sortDirection = ((ComboBoxItem)UserSortDirectionComboBox.SelectedItem)?.Tag.ToString() ?? "ASC";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $@"
            SELECT User_ID, Username, LastName, FirstName, Email, Role 
            FROM Users
            WHERE Username LIKE @SearchText
            OR LastName LIKE @SearchText
            OR FirstName LIKE @SearchText
            OR Email LIKE @SearchText
            OR Role LIKE @SearchText
            ORDER BY {sortColumn} {sortDirection}";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                UsersDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }


        private void ProductSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterAndSortProducts();
        }

        private void FilterAndSortProducts()
        {
            string searchText = SearchProductsTextBox.Text.Trim();

            // Получаем выбранные параметры сортировки
            string sortColumn = ((ComboBoxItem)ProductSortColumnComboBox.SelectedItem)?.Tag.ToString() ?? "Name";
            string sortDirection = ((ComboBoxItem)ProductSortDirectionComboBox.SelectedItem)?.Tag.ToString() ?? "ASC";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $@"
        SELECT Product_ID, Name, Price, StockQuantity, Description
        FROM Products
        WHERE Name LIKE @SearchText
        OR Price LIKE @SearchText
        OR StockQuantity LIKE @SearchText
        OR Description LIKE @SearchText
        ORDER BY {sortColumn} {sortDirection}";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                ProductsDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }


        private void CategorySortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterAndSortCategories();
        }

        private void FilterAndSortCategories()
        {
            string searchText = SearchCategoriesTextBox.Text.Trim();

            // Получаем выбранные параметры сортировки
            string sortColumn = ((ComboBoxItem)CategorySortColumnComboBox.SelectedItem)?.Tag.ToString() ?? "CategoryName";
            string sortDirection = ((ComboBoxItem)CategorySortDirectionComboBox.SelectedItem)?.Tag.ToString() ?? "ASC";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $@"
        SELECT Category_ID, CategoryName
        FROM Categories
        WHERE CategoryName LIKE @SearchText
        ORDER BY {sortColumn} {sortDirection}";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                CategoriesDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }


        private void OrderProductSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterAndSortOrderProducts();
        }

        private void FilterAndSortOrderProducts()
        {
            string searchText = SearchOrderItemsTextBox.Text.Trim();
            string orderIDFilter = int.TryParse(searchText, out int orderId) ? orderId.ToString() : null;

            string sortColumn = ((ComboBoxItem)OrderProductSortColumnComboBox.SelectedItem)?.Tag.ToString() ?? "ProductName";
            string sortDirection = ((ComboBoxItem)OrderProductSortDirectionComboBox.SelectedItem)?.Tag.ToString() ?? "ASC";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT oi.OrderItem_ID, p.Name AS ProductName, o.Order_ID, oi.Quantity, oi.UnitPrice
            FROM OrderItems oi
            JOIN Products p ON oi.Product_ID = p.Product_ID
            JOIN Orders o ON oi.Order_ID = o.Order_ID
            WHERE p.Name LIKE @SearchText
            OR (@OrderID IS NOT NULL AND o.Order_ID = @OrderID)
            ORDER BY " + sortColumn + " " + sortDirection;

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");
                command.Parameters.AddWithValue("@OrderID", (object)orderId ?? DBNull.Value);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                OrderItemsDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }



       

        

        private void ReviewSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterAndSortReviews();
        }

        private void FilterAndSortReviews()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Получаем выбранные параметры сортировки
                    string sortColumn = ((ComboBoxItem)ReviewSortColumnComboBox.SelectedItem)?.Tag.ToString() ?? "Rating"; // по умолчанию "Rating"
                    string sortDirection = ((ComboBoxItem)ReviewSortDirectionComboBox.SelectedItem)?.Tag.ToString() ?? "ASC"; // по умолчанию "ASC"

                    // SQL-запрос с динамической сортировкой
                    string query = $@"
            SELECT Reviews.Review_ID, Reviews.Rating, Reviews.ReviewText, 
                   Products.Product_ID, Products.Name AS ProductName, 
                   Clients.Client_ID, Users.Username 
            FROM Reviews
            INNER JOIN Products ON Reviews.Product_ID = Products.Product_ID
            INNER JOIN Clients ON Reviews.Client_ID = Clients.Client_ID
            INNER JOIN Users ON Clients.User_ID = Users.User_ID
            ORDER BY {sortColumn} {sortDirection}";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable reviewsTable = new DataTable();
                    adapter.Fill(reviewsTable);

                    ReviewsDataGrid.ItemsSource = reviewsTable.DefaultView;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при загрузке и сортировке отзывов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void ClientSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterAndSortClients();
        }

        private void FilterAndSortClients()
        {
            string searchText = ClientSearchTextBox.Text.Trim();

            // Получаем выбранные параметры сортировки
            string sortColumn = ((ComboBoxItem)ClientSortColumnComboBox.SelectedItem)?.Tag.ToString() ?? "Username";
            string sortDirection = ((ComboBoxItem)ClientSortDirectionComboBox.SelectedItem)?.Tag.ToString() ?? "ASC";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $@"
            SELECT c.Client_ID, u.Username, c.Phone, 
                   CONVERT(VARCHAR, c.RegistrationDate, 120) AS RegistrationDate
            FROM Clients c
            JOIN Users u ON c.User_ID = u.User_ID
            WHERE u.Username LIKE @SearchText
            OR c.Client_ID LIKE @SearchText
            OR c.Phone LIKE @SearchText
            OR CONVERT(VARCHAR, c.RegistrationDate, 120) LIKE @SearchText
            ORDER BY {sortColumn} {sortDirection}";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                ClientsDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }

        private void OrderSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterAndSortOrders();
        }

        private void FilterAndSortOrders()
        {
            string searchText = SearchOrdersTextBox.Text.Trim();

            // Получаем выбранные параметры сортировки
            string sortColumn = ((ComboBoxItem)OrderSortColumnComboBox.SelectedItem)?.Tag.ToString() ?? "OrderDate";
            string sortDirection = ((ComboBoxItem)OrderSortDirectionComboBox.SelectedItem)?.Tag.ToString() ?? "ASC";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $@"
        SELECT o.Order_ID, c.Client_ID, u.Username AS ClientName, 
               o.OrderDate, o.TotalQuantity, o.Status
        FROM Orders o
        JOIN Clients c ON o.Client_ID = c.Client_ID
        JOIN Users u ON c.User_ID = u.User_ID
        WHERE u.Username LIKE @SearchText
        OR o.Status LIKE @SearchText
        OR o.TotalQuantity LIKE @SearchText
        OR CONVERT(VARCHAR, o.OrderDate, 120) LIKE @SearchText
        ORDER BY {sortColumn} {sortDirection}";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable searchResultsTable = new DataTable();
                adapter.Fill(searchResultsTable);

                MessageBox.Show($"Найдено записей: {searchResultsTable.Rows.Count}"); // Отладка

                OrdersDataGrid.ItemsSource = searchResultsTable.DefaultView;
            }
        }
        private void LoadClientsIntoComboBox()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT User_ID, Username FROM Users ORDER BY Username";

                SqlCommand command = new SqlCommand(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                ClientUsernameComboBox.Items.Clear();

                while (reader.Read())
                {
                    ClientUsernameComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = reader["Username"].ToString(),
                        Tag = reader["User_ID"]
                    });
                }
            }
        }


        private void ClientUsernameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientUsernameComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                int selectedUserId = (int)selectedItem.Tag;
            }
        }

        private void OrderClientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BackupDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            // Укажите путь для сохранения резервной копии
            string backupPath = @"C:\Backups\MarketUL.bak";

            // Подключение к базе данных и выполнение бэкапа
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string backupQuery = $"BACKUP DATABASE MarketUL TO DISK = '{backupPath}'";

                    using (SqlCommand command = new SqlCommand(backupQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Резервная копия успешно создана.", "Бэкап", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании бэкапа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RestoreDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            // Открываем диалог для выбора файла бэкапа
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Backup Files (*.bak)|*.bak",
                Title = "Выберите файл бэкапа для восстановления"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Получаем путь к выбранному файлу бэкапа
                string backupFilePath = openFileDialog.FileName;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();

                        // Отключаем пользователей от базы данных
                        string setSingleUserQuery = "ALTER DATABASE MarketUL SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                        using (SqlCommand command = new SqlCommand(setSingleUserQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        // Запрос на восстановление базы данных
                        string restoreQuery = $"RESTORE DATABASE MarketUL FROM DISK = '{backupFilePath}' WITH REPLACE";
                        using (SqlCommand command = new SqlCommand(restoreQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        // Перевод базы данных в много-пользовательский режим
                        string setMultiUserQuery = "ALTER DATABASE MarketUL SET MULTI_USER";
                        using (SqlCommand command = new SqlCommand(setMultiUserQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        MessageBox.Show("База данных успешно восстановлена.", "Восстановление", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при восстановлении базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ExportUsersToExcel(string filePath)
        {
            try
            {
                using (ExcelPackage package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Users");

                    // Добавление заголовков
                    worksheet.Cells[1, 1].Value = "Username";
                    worksheet.Cells[1, 2].Value = "LastName";
                    worksheet.Cells[1, 3].Value = "FirstName";
                    worksheet.Cells[1, 4].Value = "Email";
                    worksheet.Cells[1, 5].Value = "Role";

                    // Настройка стиля заголовков
                    using (var range = worksheet.Cells[1, 1, 1, 5])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    // Добавление данных из DataGrid
                    var usersTable = ((DataView)UsersDataGrid.ItemsSource).Table;
                    for (int i = 0; i < usersTable.Rows.Count; i++)
                    {
                        worksheet.Cells[i + 2, 1].Value = usersTable.Rows[i]["Username"];
                        worksheet.Cells[i + 2, 2].Value = usersTable.Rows[i]["LastName"];
                        worksheet.Cells[i + 2, 3].Value = usersTable.Rows[i]["FirstName"];
                        worksheet.Cells[i + 2, 4].Value = usersTable.Rows[i]["Email"];
                        worksheet.Cells[i + 2, 5].Value = usersTable.Rows[i]["Role"];
                    }

                    // Сохранение файла
                    package.SaveAs(new FileInfo(filePath));
                    MessageBox.Show("Экспорт данных успешно выполнен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                FileName = "UsersExport.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                ExportUsersToExcel(saveFileDialog.FileName);
            }
        }
        private void ImportUsersFromExcel(string filePath)
        {
            try
            {
                using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                        {
                            string username = worksheet.Cells[row, 1].Text.Trim();
                            string lastName = worksheet.Cells[row, 2].Text.Trim();
                            string firstName = worksheet.Cells[row, 3].Text.Trim();
                            string email = worksheet.Cells[row, 4].Text.Trim();
                            string role = worksheet.Cells[row, 5].Text.Trim();
                            string hashedPassword = HashPassword(worksheet.Cells[row, 6].Text.Trim());

                            // Проверка существования пользователя с таким именем
                            string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                            SqlCommand checkUserCommand = new SqlCommand(checkUserQuery, connection);
                            checkUserCommand.Parameters.AddWithValue("@Username", username);
                            int userCount = (int)checkUserCommand.ExecuteScalar();

                            if (userCount == 0)
                            {
                                string insertQuery = @"
                        INSERT INTO Users (Username, LastName, FirstName, Password, Email, Role) 
                        VALUES (@Username, @LastName, @FirstName, @Password, @Email, @Role)";

                                SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                                insertCommand.Parameters.AddWithValue("@Username", username);
                                insertCommand.Parameters.AddWithValue("@LastName", lastName);
                                insertCommand.Parameters.AddWithValue("@FirstName", firstName);
                                insertCommand.Parameters.AddWithValue("@Password", hashedPassword);
                                insertCommand.Parameters.AddWithValue("@Email", email);
                                insertCommand.Parameters.AddWithValue("@Role", role);

                                insertCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }

                MessageBox.Show("Импорт данных успешно выполнен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImportUsersFromExcel(openFileDialog.FileName);
                LoadUsers();
            }
        }
    }
}