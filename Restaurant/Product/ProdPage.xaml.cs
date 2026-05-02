using Microsoft.Win32;
using Restaurant.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Globalization;
using System.IO;
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
using System.Windows.Navigation;

namespace Restaurant.Product
{
    public partial class ProdPage : Page, INotifyPropertyChanged
    {
        private readonly UserSession user;

        public bool CanEdit { get; set; }
        public ObservableCollection<ProdCategoryViewModel> Categories { get; set; }

        public ProdPage(UserSession userSession)
        {
            InitializeComponent();
            user = userSession;
            CanEdit = user.HasPermission("Редактирование продуктов");

            Categories = new ObservableCollection<ProdCategoryViewModel>();
            LoadData();
            DataContext = this;
        }

        private void LoadData()
        {
            using (var context = new RestaurantEntities())
            {
                var categories = context.ProdCategories
                    .Include(c => c.Products)
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                var uncategorizedProducts = context.Products
                    .Where(p => p.CategoryID == null)
                    .OrderBy(p => p.Name)
                    .ToList();

                Categories.Clear();

                foreach (var category in categories)
                {
                    var activeProducts = category.Products
                        .OrderBy(p => p.Name)
                        .ToList();

                    Categories.Add(new ProdCategoryViewModel
                    {
                        CategoryID = category.CategoryID,
                        CategoryName = category.CategoryName,
                        IsUncategorized = false,
                        Products = new ObservableCollection<ProductViewModel>(
                            activeProducts.Select(p => new ProductViewModel
                            {
                                ProductID = p.ProductID,
                                Name = p.Name,
                                Unit = p.Unit,
                                ImagePath = p.ImagePath,
                                CategoryID = p.CategoryID
                            })
                        )
                    });
                }

                if (uncategorizedProducts.Any())
                {
                    Categories.Add(new ProdCategoryViewModel
                    {
                        CategoryID = -1,
                        CategoryName = "Без категории",
                        IsUncategorized = true,
                        Products = new ObservableCollection<ProductViewModel>(
                            uncategorizedProducts.Select(p => new ProductViewModel
                            {
                                ProductID = p.ProductID,
                                Name = p.Name,
                                Unit = p.Unit,
                                ImagePath = p.ImagePath,
                                CategoryID = p.CategoryID
                            })
                        )
                    });
                }
            }
        }

        private void CategoriesBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProdCategoriesWindow(user);
            window.Owner = Application.Current.MainWindow;

            if (window.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var productId = (int)button?.Tag;

            var editWindow = new EditProdWindow(user, productId);
            editWindow.Owner = Application.Current.MainWindow;

            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var productId = (int)button?.Tag;

            using (var context = new RestaurantEntities())
            {
                var (canDelete, message) = CanDeleteProduct(productId, context);

                if (!canDelete)
                {
                    MessageBox.Show(message, "Невозможно удалить продукт",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить продукт?\n\n{message}",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                try
                {
                    var dbProduct = context.Products.Find(productId);
                    if (dbProduct != null)
                    {
                        context.Products.Remove(dbProduct);
                        context.SaveChanges();

                        AuditService.LogDelete(user.EmployeeID, "Продукты", dbProduct.ProductID, dbProduct.Name);
                    }

                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private (bool canDelete, string message) CanDeleteProduct(int productId, RestaurantEntities context)
        {
            var sb = new StringBuilder();
            var hasBlockers = false;

            var productPrices = context.ProductPrices
                .Where(pp => pp.ProductID == productId)
                .ToList();

            if (productPrices.Any())
            {
                hasBlockers = true;
                sb.AppendLine($"💰 Продукт используется в ценах поставщиков ({productPrices.Count})");
                sb.AppendLine("   Удалите цены поставщиков перед удалением продукта.");
                sb.AppendLine();
            }

            var purchaseItems = context.PurchaseItems
                .Include(pi => pi.Purchases)
                .Where(pi => pi.ProductID == productId
                          && pi.Purchases.PurchaseStatus != "Delivered")
                .ToList();

            if (purchaseItems.Any())
            {
                hasBlockers = true;
                var purchaseIds = purchaseItems.Select(pi => pi.PurchaseID).Distinct().ToList();
                sb.AppendLine($"⏳ Продукт используется в активных закупках ({purchaseIds.Count}):");
                foreach (var id in purchaseIds)
                {
                    var purchase = purchaseItems.First(pi => pi.PurchaseID == id).Purchases;
                    sb.AppendLine($"   • Закупка №{id} (статус: {purchase.PurchaseStatus})");
                }
                sb.AppendLine("   Завершите или отмените эти закупки перед удалением.");
                sb.AppendLine();
            }

            var dishProducts = context.DishProducts
                .Include(dp => dp.Dishes)
                .Where(dp => dp.ProductID == productId)
                .ToList();

            if (dishProducts.Any())
            {
                hasBlockers = true;
                sb.AppendLine($"🍽 Продукт является ингредиентом блюд ({dishProducts.Count}):");
                foreach (var dp in dishProducts.Take(5))
                {
                    sb.AppendLine($"   • {dp.Dishes.Name} ({dp.Quantity})");
                }
                if (dishProducts.Count > 5)
                    sb.AppendLine($"   ... и ещё {dishProducts.Count - 5}");
                sb.AppendLine("   Удалите продукт из состава блюд перед удалением.");
                sb.AppendLine();
            }

            return (!hasBlockers, sb.ToString());
        }

        private void AddProdButt_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditProdWindow(user);
            editWindow.Owner = Application.Current.MainWindow;

            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProdCategoryViewModel
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public bool IsUncategorized { get; set; }
        public ObservableCollection<ProductViewModel> Products { get; set; }
    }

    public class ProductViewModel
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string ImagePath { get; set; }
        public int? CategoryID { get; set; }
    }
}