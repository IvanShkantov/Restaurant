using Restaurant.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace Restaurant.Order
{
    /// <summary>
    /// Логика взаимодействия для EditOrderWindow.xaml
    /// </summary>
    public partial class EditOrderWindow : Window, INotifyPropertyChanged
    {
        private readonly int? _orderId;
        private readonly UserSession user;

        private Orders BackupOrder;
        private List<OrderItems> BackupItems;

        public ObservableCollection<Dishes> AvailableDishes { get; set; }
        public ObservableCollection<EditOrderItemViewModel> Items { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string EmployeeFullName { get; set; }

        public decimal EstimatedTotalPrice => Items?.Sum(i => i.TotalPrice) ?? 0;

        public EditOrderWindow(UserSession userSession, int? orderId = null)
        {
            InitializeComponent();

            user = userSession;
            _orderId = orderId;

            LoadDishes();

            if (orderId.HasValue)
            {
                LoadOrder(orderId.Value);
            }
            else
            {
                EmployeeFullName = user.FullName;
                Items = new ObservableCollection<EditOrderItemViewModel>();
            }

            DataContext = this;
        }

        private void AddItemButt_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new EditOrderItemViewModel();
            newItem.PropertyChanged += (s, ev) =>
            {
                if (ev.PropertyName == nameof(EditOrderItemViewModel.TotalPrice))
                    OnPropertyChanged(nameof(EstimatedTotalPrice));
            };

            Items.Add(newItem);
        }

        private void DeleteItemButt_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as EditOrderItemViewModel;

            if (item != null)
            {
                Items.Remove(item);
                OnPropertyChanged(nameof(EstimatedTotalPrice));
            }
        }

        private bool CanSave()
        {
            return Items != null && !Items.Any(i => i.SelectedDish == null || i.Quantity <= 0);
        }

        private void SaveButt_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            if (!CanSave())
            {
                MessageBox.Show("Заполните необходимые поля.");
                return;
            }

            if (Items.Where(i => i.SelectedDish != null).GroupBy(i => i.SelectedDish.DishID).Any(g => g.Count() > 1))
            {
                MessageBox.Show("Удалите дублирующиеся блюда или объедините их количество.");
                return;
            }

            try
            {
                var orderItems = new Dictionary<int, int>();
                foreach (var item in Items.Where(i => i.SelectedDish != null && i.Quantity > 0))
                {
                    orderItems[item.SelectedDish.DishID] = item.Quantity;
                }

                var checkResult = WarehouseService.CheckOrderAvailability(
                    orderItems,
                    excludeOrderId: _orderId  
                );

                if (!checkResult.IsSufficient)
                {
                    WarehouseService.ShowWarehouseWarning(checkResult);
                    return;
                }

                using (var context = new RestaurantEntities())
                {
                    Orders order;

                    if (_orderId.HasValue)
                    {
                        order = context.Orders
                            .Include("OrderItems")
                            .First(o => o.OrderID == _orderId);

                        context.OrderItems.RemoveRange(order.OrderItems);
                    }
                    else
                    {
                        order = new Orders
                        {
                            EmployeeID = user.EmployeeID,
                            Status = "Created",
                            Price = 0
                        };
                        context.Orders.Add(order);
                        context.SaveChanges();
                    }

                    order.CreatedAt = OrderDate;

                    var newItems = new List<OrderItems>();
                    foreach (var item in Items.Where(i => i.SelectedDish != null && i.Quantity > 0))
                    {
                        var orderItem = new OrderItems
                        {
                            OrderID = order.OrderID,
                            DishID = item.SelectedDish.DishID,
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity,
                            TotalPrice = item.TotalPrice
                        };
                        order.OrderItems.Add(orderItem);
                        newItems.Add(orderItem);
                    }

                    order.Price = order.OrderItems.Sum(oi => oi.TotalPrice);

                    context.SaveChanges();

                    if (_orderId.HasValue)
                    {
                        AuditService.LogUpdate(user.EmployeeID, BackupOrder, order, "Заказы", order.OrderID, AuditService.OrderID(order.OrderID));
                    }
                    else
                    {
                        AuditService.LogCreate(user.EmployeeID, "Заказы", order.OrderID, AuditService.OrderID(order.OrderID));
                    }

                    AuditService.GetOrderItemsChanges(user.EmployeeID, BackupItems, order.OrderItems.ToList());
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDishes()
        {
            using (var context = new RestaurantEntities())
            {
                AvailableDishes = new ObservableCollection<Dishes>(
                    context.Dishes.OrderBy(d => d.Name).ToList()
                );
            }
            OnPropertyChanged(nameof(AvailableDishes));
        }

        private void LoadOrder(int orderId)
        {
            using (var context = new RestaurantEntities())
            {
                var order = context.Orders
                    .Include("OrderItems")
                    .Include("OrderItems.Dishes")
                    .FirstOrDefault(o => o.OrderID == orderId);

                if (order == null) return;

                BackupOrder = new Orders
                {
                    OrderID = order.OrderID,
                    EmployeeID = order.EmployeeID,
                    CreatedAt = order.CreatedAt,
                    Status = order.Status,
                    Price = order.Price
                };

                BackupItems = order.OrderItems.Select(oi => new OrderItems
                {
                    OrderID = oi.OrderID,
                    DishID = oi.DishID,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList();

                OrderDate = (DateTime)order.CreatedAt;

                EmployeeFullName = AuditService.EmployeeFullName((int)order.EmployeeID);

                Items = new ObservableCollection<EditOrderItemViewModel>();
                foreach (var item in order.OrderItems)
                {
                    var dish = AvailableDishes.FirstOrDefault(d => d.DishID == item.DishID);
                    var vm = new EditOrderItemViewModel
                    {
                        SelectedDish = dish,
                        Quantity = item.Quantity
                    };
                    vm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(EditOrderItemViewModel.TotalPrice))
                            OnPropertyChanged(nameof(EstimatedTotalPrice));
                    };
                    Items.Add(vm);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void CancelButt_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class EditOrderItemViewModel : INotifyPropertyChanged
    {
        private Dishes _selectedDish = null;
        public Dishes SelectedDish
        {
            get => _selectedDish;
            set
            {
                _selectedDish = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UnitPrice));
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value && value > 0)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal UnitPrice => SelectedDish?.Price ?? 0;
        public decimal TotalPrice => Quantity * UnitPrice;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
