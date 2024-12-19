using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
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

namespace ULLLL
{

    /// <summary>
    /// Логика взаимодействия для WorkerWindow.xaml
    /// </summary>
    public partial class WorkerWindow : Window
    {
        private string connectionString = "Data Source=HOME-PC\\MSSQLSERVER01;Initial Catalog=MarketULLL;Integrated Security=True";

        public WorkerWindow()
        {
            InitializeComponent();
            LoadProducts();
            LoadOrders();
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверка, что есть выбранный элемент
            if (ProductsDataGrid.SelectedItem is DataRowView selectedProduct)
            {
                int quantity = Convert.ToInt32(selectedProduct["StockQuantity"]);
                QuantityUpDown.Value = quantity;
            }
        }


        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProduct = ProductsDataGrid.SelectedItem as DataRowView;
            if (selectedProduct == null)
            {
                MessageBox.Show("Пожалуйста, выберите продукт для изменения количества.");
                return;
            }

            if (QuantityUpDown.Value == null)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество.");
                return;
            }

            int newQuantity = QuantityUpDown.Value.Value;
            var productId = selectedProduct["Product_ID"];

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("UPDATE Products SET StockQuantity = @StockQuantity WHERE Product_ID = @ProductId", connection);
                command.Parameters.AddWithValue("@StockQuantity", newQuantity);
                command.Parameters.AddWithValue("@ProductId", productId);

                command.ExecuteNonQuery();
                MessageBox.Show("Количество товара обновлено.");
                LoadProducts(); 
            }
        }
        private void LoadProducts()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Products", connection);
                DataTable productsTable = new DataTable();
                adapter.Fill(productsTable);
                ProductsDataGrid.ItemsSource = productsTable.DefaultView;
            }
        }
        private void OrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is DataRowView selectedOrder)
            {
                string currentStatus = selectedOrder["Status"].ToString();
                OrderStatusComboBox.Text = currentStatus;
            }
        }
        private void LoadOrders()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // SQL-запрос для получения информации о заказах с данными о клиентах

                    string query = @"
                                      SELECT o.Order_ID,
                               u.FirstName + ' ' + u.LastName AS ClientName,
                               c.Phone,
                               u.Email,
                               o.Status,
                               o.OrderDate
                        FROM Orders o
                        JOIN Clients c ON o.Client_ID = c.Client_ID
                        JOIN Users u ON c.User_ID = u.User_ID;
                        ";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable ordersTable = new DataTable();

                    // Заполняем DataTable данными из базы
                    adapter.Fill(ordersTable);

                    // Устанавливаем источник данных для DataGrid
                    OrdersDataGrid.ItemsSource = ordersTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке данных о заказах: {ex.Message}");
                }
            }
        }


        private void UpdateOrderStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is DataRowView selectedOrder)
            {
                int orderId = Convert.ToInt32(selectedOrder["Order_ID"]);
                string newStatus = OrderStatusComboBox.Text;

                // Проверка на выбор нового статуса
                if (string.IsNullOrEmpty(newStatus))
                {
                    MessageBox.Show("Пожалуйста, выберите новый статус.");
                    return;
                }

                // Подключение к базе данных
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();

                        // SQL-запрос для обновления статуса заказа
                        string updateQuery = "UPDATE Orders SET Status = @Status WHERE Order_ID = @OrderId";
                        SqlCommand command = new SqlCommand(updateQuery, connection);

                        // Параметры запроса
                        command.Parameters.AddWithValue("@Status", newStatus);
                        command.Parameters.AddWithValue("@OrderId", orderId);

                        // Выполнение запроса
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Статус заказа обновлен.");
                            LoadOrders();  
                        }
                        else
                        {
                            MessageBox.Show("Не удалось обновить статус заказа.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении статуса: {ex.Message}");
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow mainWindow= new LoginWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
