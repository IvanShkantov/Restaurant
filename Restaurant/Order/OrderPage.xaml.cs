using Restaurant.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Restaurant.Order
{
    /// <summary>
    /// Логика взаимодействия для OrderPage.xaml
    /// </summary>
    public partial class OrderPage : Page
    {
        private readonly UserSession user;

        public ObservableCollection<OrderGroupViewModel> ActiveOrders { get; set; }
        public ObservableCollection<OrderGroupViewModel> CompletedOrders { get; set; }

        public OrderPage(UserSession userSession)
        {
            InitializeComponent();

            user = userSession;

            ActiveOrders = new ObservableCollection<OrderGroupViewModel>();
            CompletedOrders = new ObservableCollection<OrderGroupViewModel>();

            LoadOrders();

            DataContext = this;
        }

        public void LoadOrders()
        {
            using (var context = new RestaurantEntities())
            {
                var flatData = context.vw_OrderDetails
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();

                var orders = flatData.GroupBy(o => new
                {
                    o.OrderID,
                    o.CreatedAt,
                    o.ClosedAt,
                    o.Status,
                    o.TotalOrderPrice,
                    o.EmployeeFullName,
                    o.EmployeeID
                })
                .Select(g => new OrderGroupViewModel
                {
                    OrderID = g.Key.OrderID,
                    CreatedAt = (DateTime)g.Key.CreatedAt,
                    ClosedAt = g.Key.ClosedAt ?? DateTime.Now,
                    Status = g.Key.Status,
                    TotalOrderPrice = (decimal)g.Key.TotalOrderPrice,
                    EmployeeFullName = g.Key.EmployeeFullName,
                    EmployeeID = (int)g.Key.EmployeeID,
                    Items = new ObservableCollection<OrderItemViewModel>(
                        g.Select(item => new OrderItemViewModel
                        {
                            DishID = item.DishID,
                            DishName = item.DishName,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = (decimal)item.ItemTotalPrice
                        })
                    )
                }).ToList();

                ActiveOrders.Clear();

                foreach (var o in orders)
                {
                    if (o.Status != "Completed") ActiveOrders.Add(o);
                }

                var archivedOrders = context.ArchivedOrders
            .Include(ao => ao.ArchivedOrderItems)
            .OrderByDescending(ao => ao.ClosedAt)
            .ToList();

                var completed = new ObservableCollection<OrderGroupViewModel>(
                    archivedOrders.Select(ao => new OrderGroupViewModel
                    {
                        OrderID = ao.OrderID,
                        CreatedAt = ao.CreatedAt,
                        ClosedAt = ao.ClosedAt,
                        Status = "Completed",
                        TotalOrderPrice = ao.TotalPrice,
                        EmployeeFullName = ao.EmployeeFullName,
                        Items = new ObservableCollection<OrderItemViewModel>(
                            ao.ArchivedOrderItems.Select(oi => new OrderItemViewModel
                            {
                                DishName = oi.DishName,
                                Quantity = oi.Quantity,
                                UnitPrice = oi.UnitPrice,
                                TotalPrice = oi.TotalPrice
                            })
                        )
                    })
                );

                CompletedOrders.Clear();

                foreach (var o in completed)
                {
                    CompletedOrders.Add(o);
                }
            }
        }

        private void AddOrderButt_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditOrderWindow(user);
            window.Owner = Application.Current.MainWindow;

            if (window.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        private void EditOrderButt_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var orderId = (int)button?.DataContext;

            var window = new EditOrderWindow(user, orderId);
            window.Owner = Application.Current.MainWindow;

            if (window.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        private void NextStatusButt_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var orderId = (int)button?.DataContext;

            using (var context = new RestaurantEntities())
            {
                var dbOrder = context.Orders.Find(orderId);
                if (dbOrder != null)
                {
                    var oldStatus = dbOrder.Status;

                    switch (dbOrder.Status)
                    {
                        case "Created":
                            dbOrder.Status = "Processing";
                            break;
                        case "Processing":
                            dbOrder.Status = "Ready";
                            break;
                        case "Ready":
                            dbOrder.Status = "Completed";
                            dbOrder.ClosedAt = DateTime.Now;
                            break;
                    }

                    context.SaveChanges();

                    AuditService.LogUpdate(
                        user.EmployeeID,
                        new Orders { OrderID = dbOrder.OrderID, EmployeeID = dbOrder.EmployeeID, Status = oldStatus },
                        dbOrder,
                        "Заказы",
                        orderId,
                        AuditService.OrderID(orderId)
                    );
                }
            }

            LoadOrders();
        }

        private void CancelOrderButt_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var orderId = (int)button?.DataContext;

            var result = MessageBox.Show(
                "Вы уверены, что хотите отменить заказ?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            using (var context = new RestaurantEntities())
            {
                var dbOrder = context.Orders.Find(orderId);
                if (dbOrder != null)
                {
                    context.Orders.Remove(dbOrder);

                    AuditService.LogDelete(user.EmployeeID, "Заказы", orderId, AuditService.OrderID(orderId));

                    context.SaveChanges();
                }
            }

            LoadOrders();
        }


        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }



    public class OrderItemViewModel
    {
        public int DishID { get; set; }
        public string DishName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class OrderGroupViewModel
    {
        public int OrderID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string Status { get; set; }
        public decimal TotalOrderPrice { get; set; }
        public string EmployeeFullName { get; set; }
        public int EmployeeID { get; set; }

        public ObservableCollection<OrderItemViewModel> Items { get; set; }
        public int ItemsCount => Items?.Count ?? 0;

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case "Created": return "📝 Создан";
                    case "Processing": return "🔄 Готовится";
                    case "Ready": return "✅ Готов";
                    case "Completed": return "✔️ Завершён";
                    default: return Status;
                }
            }
        }

        public string NextStatusIcon
        {
            get
            {
                switch (Status)
                {
                    case "Created": return "▶";
                    case "Processing": return "⏩";
                    case "Ready": return "✅";
                    default: return "";
                }
            }
        }

        public string NextStatusTooltip
        {
            get
            {
                switch (Status)
                {
                    case "Created": return "Начать приготовление";
                    case "Processing": return "Отметить как готовый";
                    case "Ready": return "Завершить заказ";
                    default: return "";
                }
            }
        }
    }
}
