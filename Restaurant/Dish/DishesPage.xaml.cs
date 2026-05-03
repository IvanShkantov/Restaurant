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
using System.Data.Entity;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Restaurant.Dish
{
    /// <summary>
    /// Логика взаимодействия для DishesPage.xaml
    /// </summary>
    public partial class DishesPage : Page, INotifyPropertyChanged
    {
        private readonly UserSession user;

        public ObservableCollection<DishCategoryViewModel> DishCategories { get; set; }

        public DishesPage(UserSession userSession)
        {
            InitializeComponent();
            user = userSession;

            DishCategories = new ObservableCollection<DishCategoryViewModel>();
            LoadDishes();

            DataContext = this;
        }

        private void LoadDishes()
        {
            using (var context = new RestaurantEntities())
            {
                var categories = context.DishCategories
                    .Include(d => d.Dishes)
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                var uncategorizedDishes = context.Dishes
                    .Where(d => d.CategoryID == null)
                    .OrderBy(d => d.Name)
                    .ToList();

                DishCategories.Clear();


                foreach (var category in categories)
                {
                    DishCategories.Add(new DishCategoryViewModel
                    {
                        CategoryID = category.DishCategoryID,
                        CategoryName = category.CategoryName,
                        IsUncategorized = false,
                        Dishes = new ObservableCollection<DishViewModel>(
                            category.Dishes.OrderBy(d => d.Name).Select(d => new DishViewModel
                            {
                                DishID = d.DishID,
                                Name = d.Name,
                                Price = d.Price,
                                ImagePath = d.ImagePath,
                                CategoryID = d.CategoryID
                            })
                        )
                    });
                }


                if (uncategorizedDishes.Any())
                {
                    DishCategories.Add(new DishCategoryViewModel
                    {
                        CategoryID = -1,
                        CategoryName = "Без категории",
                        IsUncategorized = true,
                        Dishes = new ObservableCollection<DishViewModel>(
                            uncategorizedDishes.Select(d => new DishViewModel
                            {
                                DishID = d.DishID,
                                Name = d.Name,
                                Price = d.Price,
                                ImagePath = d.ImagePath,
                                CategoryID = d.CategoryID
                            })
                        )
                    });
                }
            }
        }

        private void AddDishButt_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditDishWindow(user);
            window.Owner = Application.Current.MainWindow;

            if (window.ShowDialog() == true)
            {
                LoadDishes();
            }
        }

        private void EditDishBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dishId = (int)button?.Tag;

            var window = new EditDishWindow(user, dishId);
            window.Owner = Application.Current.MainWindow;

            if (window.ShowDialog() == true)
            {
                LoadDishes();
            }
        }

        private (bool canDelete, string message) CanDeleteDish(int dishId, RestaurantEntities context)
        {
                var sb = new StringBuilder();
                var hasBlockers = false;

                var linkedItems = context.OrderItems
                    .Include(oi => oi.Orders)
                    .Where(oi => oi.DishID == dishId)
                    .ToList();

                if (!linkedItems.Any())
                {
                    return (true, "");
                }

                var activeItems = linkedItems
                    .Where(oi => oi.Orders.Status != "Completed")
                    .ToList();

                sb.AppendLine($"Блюдо используется в {activeItems.Count} позициях заказов:");
                sb.AppendLine();

                if (activeItems.Any())
                {
                    hasBlockers = true;
                    var orderIds = activeItems
                        .Select(oi => oi.OrderID)
                        .Distinct()
                        .OrderBy(id => id)
                        .ToList();

                    sb.AppendLine($"Активные заказы ({orderIds.Count}):");
                    foreach (var orderId in orderIds)
                    {
                        var order = activeItems.First(oi => oi.OrderID == orderId).Orders;
                        var itemCount = activeItems.Count(oi => oi.OrderID == orderId);
                        sb.AppendLine($"   • Заказ: {AuditService.OrderID(orderId)} " +
                            $"({itemCount} поз., статус: {order.Status})");
                    }
                    sb.AppendLine();
                    sb.AppendLine($"   Завершите или отмените эти заказы перед удалением блюда.");
                    sb.AppendLine();
                }

                return (!hasBlockers, sb.ToString());
        }

        private void DeleteDishBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dishId = (int)button?.Tag;

            try
            {
                using (var context = new RestaurantEntities())
                {
                    var (canDelete, message) = CanDeleteDish(dishId, context);

                    if (!canDelete)
                    {
                        MessageBox.Show(message, "Невозможно удалить блюдо",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show(
                        "Вы уверены, что хотите удалить блюдо?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;

                    var dbDish = context.Dishes.Find(dishId);
                    if (dbDish != null)
                    {
                        context.Dishes.Remove(dbDish);
                        context.SaveChanges();

                        AuditService.LogDelete(user.EmployeeID, "Блюда", dishId, dbDish.Name);
                    }
                }

                LoadDishes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void CategoriesBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new DishCategoriesWindow(user);
            window.Owner = Application.Current.MainWindow;

            // Если окно сохранило изменения — обновляем страницу
            if (window.ShowDialog() == true)
            {
                LoadDishes();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }

    public class DishCategoryViewModel
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public bool IsUncategorized { get; set; }
        public ObservableCollection<DishViewModel> Dishes { get; set; }
    }

    public class DishViewModel
    {
        public int DishID { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ImagePath { get; set; }
        public int? CategoryID { get; set; }
    }
}
