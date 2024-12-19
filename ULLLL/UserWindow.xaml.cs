using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ULLLL
{
    public partial class UserWindow : Window
    {
        private string connectionString = "Data Source=LUVYPYAN\\LUCYPYAN;Initial Catalog=MarketUL1;Integrated Security=True";
        private ObservableCollection<OrderItem> orderItems = new ObservableCollection<OrderItem>();
        private int clientId;

        public UserWindow(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;
            LoadProducts();
            LoadClientOrders();
            OrderItemsDataGrid.ItemsSource = orderItems;
            LoadClientInfo();
        }

        // Метод для загрузки списка товаров
        private void LoadProducts()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                    SELECT p.Product_ID, p.Name, c.CategoryName, p.Price, p.StockQuantity, p.Description
                    FROM Products p
                    JOIN Categories c ON p.Category_ID = c.Category_ID";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable productsTable = new DataTable();
                    adapter.Fill(productsTable);
                    ProductsDataGrid.ItemsSource = productsTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке товаров: {ex.Message}");
                }
            }
        }

        // Загрузка заказов текущего клиента
        private void LoadClientOrders()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                    SELECT Order_ID, OrderDate, TotalQuantity, Status
                    FROM Orders
                    WHERE Client_ID = @ClientID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ClientID", clientId);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable ordersTable = new DataTable();
                    adapter.Fill(ordersTable);

                    ClientOrdersDataGrid.ItemsSource = ordersTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}");
                }
            }
        }

        // Добавление выбранного товара в текущий заказ
        private void AddToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is DataRowView selectedProduct)
            {
                string name = selectedProduct["Name"].ToString();
                decimal price = Convert.ToDecimal(selectedProduct["Price"]);
                int quantity = 1;

                var existingItem = orderItems.FirstOrDefault(i => i.Name == name);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    orderItems.Add(new OrderItem { Name = name, Price = price, Quantity = quantity });
                }
                OrderItemsDataGrid.Items.Refresh();
            }
        }

        // Оформление заказа и добавление его в базу данных
        private void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (orderItems.Count == 0)
            {
                MessageBox.Show("Пожалуйста, добавьте товары в заказ перед оформлением.");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string insertOrderQuery = @"
                    INSERT INTO Orders (Client_ID, OrderDate, TotalQuantity, Status)
                    VALUES (@ClientID, @OrderDate, @TotalQuantity, @Status);
                    SELECT SCOPE_IDENTITY();";

                    SqlCommand orderCommand = new SqlCommand(insertOrderQuery, connection);
                    orderCommand.Parameters.AddWithValue("@ClientID", clientId);
                    orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                    orderCommand.Parameters.AddWithValue("@TotalQuantity", orderItems.Sum(item => item.Quantity));
                    orderCommand.Parameters.AddWithValue("@Status", "В обработке");

                    int orderId = Convert.ToInt32(orderCommand.ExecuteScalar());

                    string insertOrderItemQuery = @"
                    INSERT INTO OrderItems (Product_ID, Order_ID, Quantity, UnitPrice)
                    VALUES (@ProductID, @OrderID, @Quantity, @UnitPrice);";

                    foreach (var item in orderItems)
                    {
                        SqlCommand orderItemCommand = new SqlCommand(insertOrderItemQuery, connection);
                        orderItemCommand.Parameters.AddWithValue("@ProductID", GetProductIdByName(item.Name, connection));
                        orderItemCommand.Parameters.AddWithValue("@OrderID", orderId);
                        orderItemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                        orderItemCommand.Parameters.AddWithValue("@UnitPrice", item.Price);
                        orderItemCommand.ExecuteNonQuery();
                    }

                    MessageBox.Show("Заказ успешно оформлен!");
                    LoadClientOrders();
                    orderItems.Clear();
                    OrderItemsDataGrid.Items.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}");
                }
            }
        }


        private void LoadClientInfo()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Измененный запрос с использованием JOIN
                string query = @"
            SELECT 
                u.Username, 
                u.FirstName, 
                u.LastName, 
                c.Phone, 
                u.Email 
            FROM 
                Clients c 
            JOIN 
                Users u ON c.User_ID = u.User_ID 
            WHERE 
                c.Client_ID = @ClientId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", this.clientId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ClientUsernameTextBlock.Text = $"Имя пользователя: {reader["Username"]}";
                            ClientEmailTextBlock.Text = $"Email: {reader["Email"]}";
                            ClientFirstNameTextBlock.Text = $"Имя: {reader["FirstName"]}";
                            ClientLastNameTextBlock.Text = $"Фамилия: {reader["LastName"]}";
                            ClientPhoneTextBlock.Text = $"Телефон: {reader["Phone"]}";
                        }
                        else
                        {
                            ClientIdTextBlock.Text = $"Клиент с ID {this.clientId} не найден.";
                        }
                    }
                }
            }
        }


        // Получение Product_ID по имени товара
        private int GetProductIdByName(string productName, SqlConnection connection)
        {
            string query = "SELECT Product_ID FROM Products WHERE Name = @ProductName";
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProductName", productName);
            object result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is DataRowView selectedRow)
            {
                // Проверка наличия столбца "Description" в выбранной строке
                if (selectedRow.Row.Table.Columns.Contains("Description"))
                {
                    string description = selectedRow["Description"]?.ToString();
                    // Отображение описания выбранного товара
                    MessageBox.Show($"Описание: {description}", "Информация о товаре", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Описание для этого товара не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string currentPassword = CurrentPasswordTextBox.Text;
            string newPassword = NewPasswordTextBox.Text;
            string confirmPassword = ConfirmPasswordTextBox.Text;

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            // Проверка на совпадение нового пароля и его подтверждения
            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Новый пароль и подтверждение не совпадают.");
                return;
            }

            // Подключение к базе данных
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Проверка правильности текущего пароля
                string checkPasswordQuery = "SELECT Password FROM Users WHERE User_ID = (SELECT User_ID FROM Clients WHERE Client_ID = @ClientId)";
                using (SqlCommand checkCommand = new SqlCommand(checkPasswordQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@ClientId", this.clientId);

                    string storedPassword = (string)checkCommand.ExecuteScalar();
                    if (storedPassword == null)
                    {
                        MessageBox.Show("Клиент не найден.");
                        return;
                    }

                    if (storedPassword != currentPassword)
                    {
                        MessageBox.Show("Неверный текущий пароль.");
                        return;
                    }
                }

                // Обновление пароля
                string updatePasswordQuery = "UPDATE Users SET Password = @NewPassword WHERE User_ID = (SELECT User_ID FROM Clients WHERE Client_ID = @ClientId)";
                using (SqlCommand updateCommand = new SqlCommand(updatePasswordQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@NewPassword", newPassword);
                    updateCommand.Parameters.AddWithValue("@ClientId", clientId);

                    updateCommand.ExecuteNonQuery();
                    MessageBox.Show("Пароль успешно изменен.");
                }
            }
        }

    }

    public partial class Copy2OfUserWindow : Window
    {
        private string connectionString = "Data Source=LUVYPYAN\\LUCYPYAN;Initial Catalog=MarketUL1;Integrated Security=True";
        private ObservableCollection<OrderItem> orderItems = new ObservableCollection<OrderItem>();
        private int clientId;

        public Copy2OfUserWindow(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;
            LoadProducts();
            LoadClientOrders();
            OrderItemsDataGrid.ItemsSource = orderItems;
            LoadClientInfo();
        }

        // Метод для загрузки списка товаров
        private void LoadProducts()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                    SELECT p.Product_ID, p.Name, c.CategoryName, p.Price, p.StockQuantity, p.Description
                    FROM Products p
                    JOIN Categories c ON p.Category_ID = c.Category_ID";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable productsTable = new DataTable();
                    adapter.Fill(productsTable);
                    ProductsDataGrid.ItemsSource = productsTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке товаров: {ex.Message}");
                }
            }
        }

        // Загрузка заказов текущего клиента
        private void LoadClientOrders()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                    SELECT Order_ID, OrderDate, TotalQuantity, Status
                    FROM Orders
                    WHERE Client_ID = @ClientID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ClientID", clientId);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable ordersTable = new DataTable();
                    adapter.Fill(ordersTable);

                    ClientOrdersDataGrid.ItemsSource = ordersTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}");
                }
            }
        }

        // Добавление выбранного товара в текущий заказ
        private void AddToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is DataRowView selectedProduct)
            {
                string name = selectedProduct["Name"].ToString();
                decimal price = Convert.ToDecimal(selectedProduct["Price"]);
                int quantity = 1;

                var existingItem = orderItems.FirstOrDefault(i => i.Name == name);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    orderItems.Add(new OrderItem { Name = name, Price = price, Quantity = quantity });
                }
                OrderItemsDataGrid.Items.Refresh();
            }
        }

        // Оформление заказа и добавление его в базу данных
        private void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (orderItems.Count == 0)
            {
                MessageBox.Show("Пожалуйста, добавьте товары в заказ перед оформлением.");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string insertOrderQuery = @"
                    INSERT INTO Orders (Client_ID, OrderDate, TotalQuantity, Status)
                    VALUES (@ClientID, @OrderDate, @TotalQuantity, @Status);
                    SELECT SCOPE_IDENTITY();";

                    SqlCommand orderCommand = new SqlCommand(insertOrderQuery, connection);
                    orderCommand.Parameters.AddWithValue("@ClientID", clientId);
                    orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                    orderCommand.Parameters.AddWithValue("@TotalQuantity", orderItems.Sum(item => item.Quantity));
                    orderCommand.Parameters.AddWithValue("@Status", "В обработке");

                    int orderId = Convert.ToInt32(orderCommand.ExecuteScalar());

                    string insertOrderItemQuery = @"
                    INSERT INTO OrderItems (Product_ID, Order_ID, Quantity, UnitPrice)
                    VALUES (@ProductID, @OrderID, @Quantity, @UnitPrice);";

                    foreach (var item in orderItems)
                    {
                        SqlCommand orderItemCommand = new SqlCommand(insertOrderItemQuery, connection);
                        orderItemCommand.Parameters.AddWithValue("@ProductID", GetProductIdByName(item.Name, connection));
                        orderItemCommand.Parameters.AddWithValue("@OrderID", orderId);
                        orderItemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                        orderItemCommand.Parameters.AddWithValue("@UnitPrice", item.Price);
                        orderItemCommand.ExecuteNonQuery();
                    }

                    MessageBox.Show("Заказ успешно оформлен!");
                    LoadClientOrders();
                    orderItems.Clear();
                    OrderItemsDataGrid.Items.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}");
                }
            }
        }


        private void LoadClientInfo()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Измененный запрос с использованием JOIN
                string query = @"
            SELECT 
                u.Username, 
                u.FirstName, 
                u.LastName, 
                c.Phone, 
                u.Email 
            FROM 
                Clients c 
            JOIN 
                Users u ON c.User_ID = u.User_ID 
            WHERE 
                c.Client_ID = @ClientId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", this.clientId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ClientUsernameTextBlock.Text = $"Имя пользователя: {reader["Username"]}";
                            ClientEmailTextBlock.Text = $"Email: {reader["Email"]}";
                            ClientFirstNameTextBlock.Text = $"Имя: {reader["FirstName"]}";
                            ClientLastNameTextBlock.Text = $"Фамилия: {reader["LastName"]}";
                            ClientPhoneTextBlock.Text = $"Телефон: {reader["Phone"]}";
                        }
                        else
                        {
                            ClientIdTextBlock.Text = $"Клиент с ID {this.clientId} не найден.";
                        }
                    }
                }
            }
        }


        // Получение Product_ID по имени товара
        private int GetProductIdByName(string productName, SqlConnection connection)
        {
            string query = "SELECT Product_ID FROM Products WHERE Name = @ProductName";
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProductName", productName);
            object result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is DataRowView selectedRow)
            {
                // Проверка наличия столбца "Description" в выбранной строке
                if (selectedRow.Row.Table.Columns.Contains("Description"))
                {
                    string description = selectedRow["Description"]?.ToString();
                    // Отображение описания выбранного товара
                    MessageBox.Show($"Описание: {description}", "Информация о товаре", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Описание для этого товара не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string currentPassword = CurrentPasswordTextBox.Text;
            string newPassword = NewPasswordTextBox.Text;
            string confirmPassword = ConfirmPasswordTextBox.Text;

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            // Проверка на совпадение нового пароля и его подтверждения
            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Новый пароль и подтверждение не совпадают.");
                return;
            }

            // Подключение к базе данных
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Проверка правильности текущего пароля
                string checkPasswordQuery = "SELECT Password FROM Users WHERE User_ID = (SELECT User_ID FROM Clients WHERE Client_ID = @ClientId)";
                using (SqlCommand checkCommand = new SqlCommand(checkPasswordQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@ClientId", this.clientId);

                    string storedPassword = (string)checkCommand.ExecuteScalar();
                    if (storedPassword == null)
                    {
                        MessageBox.Show("Клиент не найден.");
                        return;
                    }

                    if (storedPassword != currentPassword)
                    {
                        MessageBox.Show("Неверный текущий пароль.");
                        return;
                    }
                }

                // Обновление пароля
                string updatePasswordQuery = "UPDATE Users SET Password = @NewPassword WHERE User_ID = (SELECT User_ID FROM Clients WHERE Client_ID = @ClientId)";
                using (SqlCommand updateCommand = new SqlCommand(updatePasswordQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@NewPassword", newPassword);
                    updateCommand.Parameters.AddWithValue("@ClientId", clientId);

                    updateCommand.ExecuteNonQuery();
                    MessageBox.Show("Пароль успешно изменен.");
                }
            }
        }

    }

    public partial class Copy1OfUserWindow : Window
    {
        private string connectionString = "Data Source=LUVYPYAN\\LUCYPYAN;Initial Catalog=MarketUL1;Integrated Security=True";
        private ObservableCollection<OrderItem> orderItems = new ObservableCollection<OrderItem>();
        private int clientId;

        public Copy1OfUserWindow(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;
            LoadProducts();
            LoadClientOrders();
            OrderItemsDataGrid.ItemsSource = orderItems;
            LoadClientInfo();
        }

        // Метод для загрузки списка товаров
        private void LoadProducts()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                    SELECT p.Product_ID, p.Name, c.CategoryName, p.Price, p.StockQuantity, p.Description
                    FROM Products p
                    JOIN Categories c ON p.Category_ID = c.Category_ID";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable productsTable = new DataTable();
                    adapter.Fill(productsTable);
                    ProductsDataGrid.ItemsSource = productsTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке товаров: {ex.Message}");
                }
            }
        }

        // Загрузка заказов текущего клиента
        private void LoadClientOrders()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                    SELECT Order_ID, OrderDate, TotalQuantity, Status
                    FROM Orders
                    WHERE Client_ID = @ClientID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ClientID", clientId);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable ordersTable = new DataTable();
                    adapter.Fill(ordersTable);

                    ClientOrdersDataGrid.ItemsSource = ordersTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}");
                }
            }
        }

        // Добавление выбранного товара в текущий заказ
        private void AddToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is DataRowView selectedProduct)
            {
                string name = selectedProduct["Name"].ToString();
                decimal price = Convert.ToDecimal(selectedProduct["Price"]);
                int quantity = 1;

                var existingItem = orderItems.FirstOrDefault(i => i.Name == name);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    orderItems.Add(new OrderItem { Name = name, Price = price, Quantity = quantity });
                }
                OrderItemsDataGrid.Items.Refresh();
            }
        }

        // Оформление заказа и добавление его в базу данных
        private void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (orderItems.Count == 0)
            {
                MessageBox.Show("Пожалуйста, добавьте товары в заказ перед оформлением.");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string insertOrderQuery = @"
                    INSERT INTO Orders (Client_ID, OrderDate, TotalQuantity, Status)
                    VALUES (@ClientID, @OrderDate, @TotalQuantity, @Status);
                    SELECT SCOPE_IDENTITY();";

                    SqlCommand orderCommand = new SqlCommand(insertOrderQuery, connection);
                    orderCommand.Parameters.AddWithValue("@ClientID", clientId);
                    orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                    orderCommand.Parameters.AddWithValue("@TotalQuantity", orderItems.Sum(item => item.Quantity));
                    orderCommand.Parameters.AddWithValue("@Status", "В обработке");

                    int orderId = Convert.ToInt32(orderCommand.ExecuteScalar());

                    string insertOrderItemQuery = @"
                    INSERT INTO OrderItems (Product_ID, Order_ID, Quantity, UnitPrice)
                    VALUES (@ProductID, @OrderID, @Quantity, @UnitPrice);";

                    foreach (var item in orderItems)
                    {
                        SqlCommand orderItemCommand = new SqlCommand(insertOrderItemQuery, connection);
                        orderItemCommand.Parameters.AddWithValue("@ProductID", GetProductIdByName(item.Name, connection));
                        orderItemCommand.Parameters.AddWithValue("@OrderID", orderId);
                        orderItemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                        orderItemCommand.Parameters.AddWithValue("@UnitPrice", item.Price);
                        orderItemCommand.ExecuteNonQuery();
                    }

                    MessageBox.Show("Заказ успешно оформлен!");
                    LoadClientOrders();
                    orderItems.Clear();
                    OrderItemsDataGrid.Items.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}");
                }
            }
        }


        private void LoadClientInfo()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Измененный запрос с использованием JOIN
                string query = @"
            SELECT 
                u.Username, 
                u.FirstName, 
                u.LastName, 
                c.Phone, 
                u.Email 
            FROM 
                Clients c 
            JOIN 
                Users u ON c.User_ID = u.User_ID 
            WHERE 
                c.Client_ID = @ClientId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", this.clientId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ClientUsernameTextBlock.Text = $"Имя пользователя: {reader["Username"]}";
                            ClientEmailTextBlock.Text = $"Email: {reader["Email"]}";
                            ClientFirstNameTextBlock.Text = $"Имя: {reader["FirstName"]}";
                            ClientLastNameTextBlock.Text = $"Фамилия: {reader["LastName"]}";
                            ClientPhoneTextBlock.Text = $"Телефон: {reader["Phone"]}";
                        }
                        else
                        {
                            ClientIdTextBlock.Text = $"Клиент с ID {this.clientId} не найден.";
                        }
                    }
                }
            }
        }


        // Получение Product_ID по имени товара
        private int GetProductIdByName(string productName, SqlConnection connection)
        {
            string query = "SELECT Product_ID FROM Products WHERE Name = @ProductName";
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProductName", productName);
            object result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is DataRowView selectedRow)
            {
                // Проверка наличия столбца "Description" в выбранной строке
                if (selectedRow.Row.Table.Columns.Contains("Description"))
                {
                    string description = selectedRow["Description"]?.ToString();
                    // Отображение описания выбранного товара
                    MessageBox.Show($"Описание: {description}", "Информация о товаре", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Описание для этого товара не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string currentPassword = CurrentPasswordTextBox.Text;
            string newPassword = NewPasswordTextBox.Text;
            string confirmPassword = ConfirmPasswordTextBox.Text;

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            // Проверка на совпадение нового пароля и его подтверждения
            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Новый пароль и подтверждение не совпадают.");
                return;
            }

            // Подключение к базе данных
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Проверка правильности текущего пароля
                string checkPasswordQuery = "SELECT Password FROM Users WHERE User_ID = (SELECT User_ID FROM Clients WHERE Client_ID = @ClientId)";
                using (SqlCommand checkCommand = new SqlCommand(checkPasswordQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@ClientId", this.clientId);

                    string storedPassword = (string)checkCommand.ExecuteScalar();
                    if (storedPassword == null)
                    {
                        MessageBox.Show("Клиент не найден.");
                        return;
                    }

                    if (storedPassword != currentPassword)
                    {
                        MessageBox.Show("Неверный текущий пароль.");
                        return;
                    }
                }

                // Обновление пароля
                string updatePasswordQuery = "UPDATE Users SET Password = @NewPassword WHERE User_ID = (SELECT User_ID FROM Clients WHERE Client_ID = @ClientId)";
                using (SqlCommand updateCommand = new SqlCommand(updatePasswordQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@NewPassword", newPassword);
                    updateCommand.Parameters.AddWithValue("@ClientId", clientId);

                    updateCommand.ExecuteNonQuery();
                    MessageBox.Show("Пароль успешно изменен.");
                }
            }
        }

    }

    public partial class CopyOfUserWindow : Window
    {
        private string connectionString = "Data Source=LUVYPYAN\\LUCYPYAN;Initial Catalog=MarketUL1;Integrated Security=True";
        private ObservableCollection<OrderItem> orderItems = new ObservableCollection<OrderItem>();
        private int clientId;

        public CopyOfUserWindow(int clientId)
        {
            InitializeComponent();
            this.clientId = clientId;
            LoadProducts();
            LoadClientOrders();
            OrderItemsDataGrid.ItemsSource = orderItems;
            LoadClientInfo();
        }

        // Метод для загрузки списка товаров
        private void LoadProducts()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                    SELECT p.Product_ID, p.Name, c.CategoryName, p.Price, p.StockQuantity, p.Description
                    FROM Products p
                    JOIN Categories c ON p.Category_ID = c.Category_ID";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable productsTable = new DataTable();
                    adapter.Fill(productsTable);
                    ProductsDataGrid.ItemsSource = productsTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке товаров: {ex.Message}");
                }
            }
        }

        // Загрузка заказов текущего клиента
        private void LoadClientOrders()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                    SELECT Order_ID, OrderDate, TotalQuantity, Status
                    FROM Orders
                    WHERE Client_ID = @ClientID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ClientID", clientId);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable ordersTable = new DataTable();
                    adapter.Fill(ordersTable);

                    ClientOrdersDataGrid.ItemsSource = ordersTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}");
                }
            }
        }

        // Добавление выбранного товара в текущий заказ
        private void AddToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is DataRowView selectedProduct)
            {
                string name = selectedProduct["Name"].ToString();
                decimal price = Convert.ToDecimal(selectedProduct["Price"]);
                int quantity = 1;

                var existingItem = orderItems.FirstOrDefault(i => i.Name == name);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    orderItems.Add(new OrderItem { Name = name, Price = price, Quantity = quantity });
                }
                OrderItemsDataGrid.Items.Refresh();
            }
        }

        // Оформление заказа и добавление его в базу данных
        private void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (orderItems.Count == 0)
            {
                MessageBox.Show("Пожалуйста, добавьте товары в заказ перед оформлением.");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string insertOrderQuery = @"
                    INSERT INTO Orders (Client_ID, OrderDate, TotalQuantity, Status)
                    VALUES (@ClientID, @OrderDate, @TotalQuantity, @Status);
                    SELECT SCOPE_IDENTITY();";

                    SqlCommand orderCommand = new SqlCommand(insertOrderQuery, connection);
                    orderCommand.Parameters.AddWithValue("@ClientID", clientId);
                    orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                    orderCommand.Parameters.AddWithValue("@TotalQuantity", orderItems.Sum(item => item.Quantity));
                    orderCommand.Parameters.AddWithValue("@Status", "В обработке");

                    int orderId = Convert.ToInt32(orderCommand.ExecuteScalar());

                    string insertOrderItemQuery = @"
                    INSERT INTO OrderItems (Product_ID, Order_ID, Quantity, UnitPrice)
                    VALUES (@ProductID, @OrderID, @Quantity, @UnitPrice);";

                    foreach (var item in orderItems)
                    {
                        SqlCommand orderItemCommand = new SqlCommand(insertOrderItemQuery, connection);
                        orderItemCommand.Parameters.AddWithValue("@ProductID", GetProductIdByName(item.Name, connection));
                        orderItemCommand.Parameters.AddWithValue("@OrderID", orderId);
                        orderItemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                        orderItemCommand.Parameters.AddWithValue("@UnitPrice", item.Price);
                        orderItemCommand.ExecuteNonQuery();
                    }

                    MessageBox.Show("Заказ успешно оформлен!");
                    LoadClientOrders();
                    orderItems.Clear();
                    OrderItemsDataGrid.Items.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}");
                }
            }
        }


        private void LoadClientInfo()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Измененный запрос с использованием JOIN
                string query = @"
            SELECT 
                u.Username, 
                u.FirstName, 
                u.LastName, 
                c.Phone, 
                u.Email 
            FROM 
                Clients c 
            JOIN 
                Users u ON c.User_ID = u.User_ID 
            WHERE 
                c.Client_ID = @ClientId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", this.clientId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ClientUsernameTextBlock.Text = $"Имя пользователя: {reader["Username"]}";
                            ClientEmailTextBlock.Text = $"Email: {reader["Email"]}";
                            ClientFirstNameTextBlock.Text = $"Имя: {reader["FirstName"]}";
                            ClientLastNameTextBlock.Text = $"Фамилия: {reader["LastName"]}";
                            ClientPhoneTextBlock.Text = $"Телефон: {reader["Phone"]}";
                        }
                        else
                        {
                            ClientIdTextBlock.Text = $"Клиент с ID {this.clientId} не найден.";
                        }
                    }
                }
            }
        }


        // Получение Product_ID по имени товара
        private int GetProductIdByName(string productName, SqlConnection connection)
        {
            string query = "SELECT Product_ID FROM Products WHERE Name = @ProductName";
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProductName", productName);
            object result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is DataRowView selectedRow)
            {
                // Проверка наличия столбца "Description" в выбранной строке
                if (selectedRow.Row.Table.Columns.Contains("Description"))
                {
                    string description = selectedRow["Description"]?.ToString();
                    // Отображение описания выбранного товара
                    MessageBox.Show($"Описание: {description}", "Информация о товаре", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Описание для этого товара не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string currentPassword = CurrentPasswordTextBox.Text;
            string newPassword = NewPasswordTextBox.Text;
            string confirmPassword = ConfirmPasswordTextBox.Text;

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            // Проверка на совпадение нового пароля и его подтверждения
            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Новый пароль и подтверждение не совпадают.");
                return;
            }

            // Подключение к базе данных
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Проверка правильности текущего пароля
                string checkPasswordQuery = "SELECT Password FROM Users WHERE User_ID = (SELECT User_ID FROM Clients WHERE Client_ID = @ClientId)";
                using (SqlCommand checkCommand = new SqlCommand(checkPasswordQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@ClientId", this.clientId);

                    string storedPassword = (string)checkCommand.ExecuteScalar();
                    if (storedPassword == null)
                    {
                        MessageBox.Show("Клиент не найден.");
                        return;
                    }

                    if (storedPassword != currentPassword)
                    {
                        MessageBox.Show("Неверный текущий пароль.");
                        return;
                    }
                }

                // Обновление пароля
                string updatePasswordQuery = "UPDATE Users SET Password = @NewPassword WHERE User_ID = (SELECT User_ID FROM Clients WHERE Client_ID = @ClientId)";
                using (SqlCommand updateCommand = new SqlCommand(updatePasswordQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@NewPassword", newPassword);
                    updateCommand.Parameters.AddWithValue("@ClientId", clientId);

                    updateCommand.ExecuteNonQuery();
                    MessageBox.Show("Пароль успешно изменен.");
                }
            }
        }

    }

    // Класс для представления элементов заказа
    public class OrderItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
