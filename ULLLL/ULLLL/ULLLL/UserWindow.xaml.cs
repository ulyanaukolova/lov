using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ULLLL
{
    public partial class UserWindow : Window
    {
        private string connectionString = "Data Source=HOME-PC\\MSSQLSERVER01;Initial Catalog=MarketULLL;Integrated Security=True";
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
            OrdersCanvas.Loaded += (s, e) => LoadOrdersChart();

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
                    LoadOrdersChart();

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
                connection.Open();

                // Создаем транзакцию
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Проверка доступного количества на складе для каждого товара в заказе
                        foreach (var item in orderItems)
                        {
                            int availableQuantity = GetProductStockQuantityByName(item.Name, connection, transaction);
                            if (availableQuantity < item.Quantity)
                            {
                                MessageBox.Show($"Недостаточно товара \"{item.Name}\" на складе. Доступно: {availableQuantity}, требуется: {item.Quantity}.");
                                transaction.Rollback();
                                return;
                            }
                        }

                        // Добавление заказа
                        string insertOrderQuery = @"
                INSERT INTO Orders (Client_ID, OrderDate, TotalQuantity, Status)
                VALUES (@ClientID, @OrderDate, @TotalQuantity, @Status);
                SELECT SCOPE_IDENTITY();";

                        SqlCommand orderCommand = new SqlCommand(insertOrderQuery, connection, transaction);
                        orderCommand.Parameters.AddWithValue("@ClientID", clientId);
                        orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                        orderCommand.Parameters.AddWithValue("@TotalQuantity", orderItems.Sum(item => item.Quantity));
                        orderCommand.Parameters.AddWithValue("@Status", "В обработке");

                        int orderId = Convert.ToInt32(orderCommand.ExecuteScalar());

                        // Добавление товаров заказа и обновление количества на складе
                        string insertOrderItemQuery = @"
                INSERT INTO OrderItems (Product_ID, Order_ID, Quantity, UnitPrice)
                VALUES (@ProductID, @OrderID, @Quantity, @UnitPrice);";

                        string updateStockQuery = @"
                UPDATE Products 
                SET StockQuantity = StockQuantity - @Quantity 
                WHERE Product_ID = @ProductID;";

                        foreach (var item in orderItems)
                        {
                            // Вставка товара в таблицу OrderItems
                            SqlCommand orderItemCommand = new SqlCommand(insertOrderItemQuery, connection, transaction);
                            orderItemCommand.Parameters.AddWithValue("@ProductID", GetProductIdByName(item.Name, connection, transaction));
                            orderItemCommand.Parameters.AddWithValue("@OrderID", orderId);
                            orderItemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                            orderItemCommand.Parameters.AddWithValue("@UnitPrice", item.Price);
                            orderItemCommand.ExecuteNonQuery();

                            // Обновление количества на складе
                            SqlCommand updateStockCommand = new SqlCommand(updateStockQuery, connection, transaction);
                            updateStockCommand.Parameters.AddWithValue("@ProductID", GetProductIdByName(item.Name, connection, transaction));
                            updateStockCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                            updateStockCommand.ExecuteNonQuery();
                        }

                        // Подтверждение транзакции
                        transaction.Commit();

                        MessageBox.Show("Заказ успешно оформлен!");
                        LoadProducts();
                        LoadClientOrders();
                        orderItems.Clear();
                        OrderItemsDataGrid.Items.Refresh();
                    }
                    catch (Exception ex)
                    {
                        // Откат транзакции в случае ошибки
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}");
                    }
                }
            }
        }

        // Метод для получения доступного количества на складе по названию товара
        private int GetProductStockQuantityByName(string productName, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT StockQuantity FROM Products WHERE Name = @ProductName";
            SqlCommand command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@ProductName", productName);
            object result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private int GetProductIdByName(string productName, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT Product_ID FROM Products WHERE Name = @ProductName";
            SqlCommand command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@ProductName", productName);
            object result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private void ClearCartButton_Click(object sender, RoutedEventArgs e)
        {
            ClearCart();
        }

        private void ClearCart()
        {
            orderItems.Clear();
            OrderItemsDataGrid.Items.Refresh();
            MessageBox.Show("Корзина успешно очищена!");
        }

        // Метод для удаления выбранного товара из корзины
        private void RemoveSelectedItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderItemsDataGrid.SelectedItem is OrderItem selectedItem)
            {
                orderItems.Remove(selectedItem);
                OrderItemsDataGrid.Items.Refresh();
                MessageBox.Show($"Товар '{selectedItem.Name}' удален из корзины.");
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите товар для удаления.");
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

        

       private void ClientOrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (ClientOrdersDataGrid.SelectedItem is DataRowView selectedOrder)
    {
        int orderId = Convert.ToInt32(selectedOrder["Order_ID"]);
        LoadOrderDetails(orderId);
    }
}

// Метод для загрузки состава заказа
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
                oi.UnitPrice
            FROM 
                OrderItems oi
            JOIN 
                Products p ON oi.Product_ID = p.Product_ID
            WHERE 
                oi.Order_ID = @OrderID";

            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderID", orderId);

            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataTable orderDetailsTable = new DataTable();
            adapter.Fill(orderDetailsTable);

            OrderDetailsDataGrid.ItemsSource = orderDetailsTable.DefaultView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке состава заказа: {ex.Message}");
        }
    }


}
        private void LoadOrdersChart()
        {
            // Очищаем Canvas перед построением графика
            OrdersCanvas.Children.Clear();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
    SELECT 
        CAST(OrderDate AS DATE) AS OrderDate,
        SUM(TotalQuantity) AS TotalQuantity
    FROM Orders
    WHERE Client_ID = @ClientID
    GROUP BY CAST(OrderDate AS DATE)
    ORDER BY OrderDate";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ClientID", clientId);

                    SqlDataReader reader = command.ExecuteReader();

                    List<KeyValuePair<DateTime, int>> dataPoints = new List<KeyValuePair<DateTime, int>>();

                    while (reader.Read())
                    {
                        DateTime date = reader.GetDateTime(0);
                        int quantity = reader.GetInt32(1);
                        dataPoints.Add(new KeyValuePair<DateTime, int>(date, quantity));
                    }

                    if (dataPoints.Count == 0)
                    {
                       
                        return;
                    }

                    // Проверяем размеры Canvas
                    double canvasWidth = OrdersCanvas.ActualWidth;
                    double canvasHeight = OrdersCanvas.ActualHeight;

                    // Если размеры Canvas равны нулю, отложим построение графика
                    if (canvasWidth == 0 || canvasHeight == 0)
                    {
                        return;
                    }

                    double maxQuantity = dataPoints.Max(dp => dp.Value);
                    DateTime minDate = dataPoints.Min(dp => dp.Key);
                    DateTime maxDate = dataPoints.Max(dp => dp.Key);

                    double xScale = canvasWidth / (maxDate - minDate).TotalDays;
                    double yScale = canvasHeight / maxQuantity;

                    // Рисуем оси
                    Line xAxis = new Line
                    {
                        X1 = 0,
                        Y1 = canvasHeight,
                        X2 = canvasWidth,
                        Y2 = canvasHeight,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2
                    };
                    OrdersCanvas.Children.Add(xAxis);

                    Line yAxis = new Line
                    {
                        X1 = 0,
                        Y1 = 0,
                        X2 = 0,
                        Y2 = canvasHeight,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2
                    };
                    OrdersCanvas.Children.Add(yAxis);

                    // Рисуем точки графика
                    Polyline graphLine = new Polyline
                    {
                        Stroke = Brushes.Blue,
                        StrokeThickness = 2
                    };

                    foreach (var point in dataPoints)
                    {
                        double x = (point.Key - minDate).TotalDays * xScale;
                        double y = canvasHeight - (point.Value * yScale);
                        graphLine.Points.Add(new Point(x, y));
                    }

                    OrdersCanvas.Children.Add(graphLine);

                    // Добавление подписей на оси X и Y
                    // Подписи оси X (даты)
                    for (int i = 0; i < dataPoints.Count; i++)
                    {
                        double xPosition = (dataPoints[i].Key - minDate).TotalDays * xScale;
                        TextBlock dateText = new TextBlock
                        {
                            Text = dataPoints[i].Key.ToString("dd/MM"),
                            FontSize = 8,
                            Foreground = Brushes.Black,
                            Margin = new Thickness(xPosition - 10, canvasHeight - 20, 0, 0)
                        };
                        OrdersCanvas.Children.Add(dateText);
                    }

                    // Подписи оси Y (количество)
                    for (int i = 0; i <= 5; i++) // 5 отметок по оси Y
                    {
                        double yPosition = canvasHeight - (canvasHeight * i / 5);
                        TextBlock quantityText = new TextBlock
                        {
                            Text = (maxQuantity * i / 5).ToString(),
                            FontSize = 8,
                            Foreground = Brushes.Black,
                            Margin = new Thickness(5, yPosition - 10, 0, 0)
                        };
                        OrdersCanvas.Children.Add(quantityText);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при построении графика: {ex.Message}");
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow mWindow = new LoginWindow();

            // Показываем новое окно
            mWindow.Show();

            // Закрываем текущее окно (если нужно)
            this.Close();
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
