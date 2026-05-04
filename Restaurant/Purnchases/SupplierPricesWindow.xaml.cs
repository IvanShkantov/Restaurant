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
using System.Data.Entity;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Restaurant.Purnchases
{
    /// <summary>
    /// Логика взаимодействия для SupplierPricesWindow.xaml
    /// </summary>
    public partial class SupplierPricesWindow : Window
    {
        private readonly int _supplierId;
        private readonly string _supplierName;
        private readonly UserSession user;

        public ProductPrices BackupPrice;

        public ObservableCollection<ProductPriceViewModel> Prices { get; set; }

        public SupplierPricesWindow(UserSession userSession, int supplierId, string supplierName)
        {
            InitializeComponent();

            user = userSession;
            _supplierId = supplierId;
            _supplierName = supplierName;

            SupplierNameTitle.Text = supplierName;

            Prices = new ObservableCollection<ProductPriceViewModel>();
            BackupPrice = new ProductPrices();
            
            LoadPrices();
            LoadAvailableProducts();

            DataContext = this;
        }

        private void LoadPrices()
        {
            using (var context = new RestaurantEntities())
            {
                var prices = context.ProductPrices
                    .Include(p => p.Products)
                    .Include(p => p.Products.ProdCategories)
                    .Where(pp => pp.SupplierID == _supplierId)
                    .OrderBy(pp => pp.Products.Name)
                    .ToList();

                Prices.Clear();
                foreach (var price in prices)
                {
                    Prices.Add(new ProductPriceViewModel
                    {
                        Price = price,
                        Product = price.Products,
                        IsEditing = false,
                        IsNew = false
                    });
                }
            }
        }

        private void LoadAvailableProducts()
        {
            using (var context = new RestaurantEntities())
            {
                var existingProductIds = Prices.Select(p => p.Price.ProductID).ToHashSet();

                var availableProducts = context.Products
                    .Where(p => !existingProductIds.Contains(p.ProductID))
                    .OrderBy(p => p.Name)
                    .ToList();

                ProductCombo.ItemsSource = availableProducts;
            }
        }

        private void AddPriceButt_Click(object sender, RoutedEventArgs e)
        {
            AddPricePanel.Visibility = Visibility.Visible;
            PriceBox.Text = "0,00";
            ProductCombo.SelectedIndex = -1;

            LoadAvailableProducts();
        }

        private void SavePriceBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedProduct = ProductCombo.SelectedItem as Products;
            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите продукт.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceBox.Text.Replace('.', ','), out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену (больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new RestaurantEntities())
                {
                    var exists = context.ProductPrices
                        .Any(pp => pp.SupplierID == _supplierId && pp.ProductID == selectedProduct.ProductID);

                    if (exists)
                    {
                        MessageBox.Show($"Цена для продукта \"{selectedProduct.Name}\" уже существует.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var newPrice = new ProductPrices
                    {
                        SupplierID = _supplierId,
                        ProductID = selectedProduct.ProductID,
                        Price = price
                    };

                    context.ProductPrices.Add(newPrice);
                    context.SaveChanges();

                    AuditService.LogCreate(user.EmployeeID, "Цены поставщиков", newPrice.PriceID, $"{_supplierName}: {selectedProduct.Name}");
                }

                AddPricePanel.Visibility = Visibility.Collapsed;
                LoadPrices();
                LoadAvailableProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка");
            }
        }

        private void CancelAddBtn_Click(object sender, RoutedEventArgs e)
        {
            AddPricePanel.Visibility = Visibility.Collapsed;
        }

        private void EditPrice_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button.Tag as ProductPriceViewModel;

            if (viewModel == null || viewModel.IsEditing) return;

            BackupPrice = new ProductPrices { Price = viewModel.Price.Price };
            viewModel.IsEditing = true;

            var parent = FindParent<Border>(button);
            if (parent == null) return;

            SwitchToEditMode(parent, viewModel);
        }

        private void SavePrice_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button.Tag as ProductPriceViewModel;

            if (viewModel == null) return;

            if (viewModel.Price.Price <= 0)
            {
                MessageBox.Show("Цена должна быть больше 0.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new RestaurantEntities())
                {
                    var dbPrice = context.ProductPrices.Find(viewModel.Price.PriceID);
                    if (dbPrice != null)
                    {
                        var oldPrice = dbPrice;

                        AuditService.LogUpdate(user.EmployeeID,
                            dbPrice,
                            viewModel.Price,
                            "Цены поставщиков",
                            dbPrice.PriceID,
                            $"{_supplierName}: {viewModel.Product.Name}");

                        dbPrice.Price = viewModel.Price.Price;

                        context.SaveChanges();
                    }
                }

                viewModel.IsEditing = false;

                var parent = FindParent<Border>(button);
                if (parent != null)
                {
                    SwitchToViewMode(parent, viewModel);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка");
            }
        }

        private void CancelEditPrice_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button.Tag as ProductPriceViewModel;

            if (viewModel == null) return;

            if (BackupPrice != null)
            {
                viewModel.Price = new ProductPrices { Price = BackupPrice.Price };
            }

            viewModel.IsEditing = false;

            var parent = FindParent<Border>(button);
            if (parent != null)
            {
                SwitchToViewMode(parent, viewModel);
            }
        }

        private void DeletePrice_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button.Tag as ProductPriceViewModel;

            if (viewModel == null) return;

            var (canDelete, message) = CanDeletePrice(viewModel.Price.PriceID);

            if (!canDelete)
            {
                MessageBox.Show(message, "Невозможно удалить цену",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить цену для продукта \"{viewModel.Product.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new RestaurantEntities())
                {
                    var dbPrice = context.ProductPrices.Find(viewModel.Price.PriceID);
                    if (dbPrice != null)
                    {
                        context.ProductPrices.Remove(dbPrice);
                        context.SaveChanges();

                        AuditService.LogDelete(user.EmployeeID, "Цены поставщиков",
                            viewModel.Price.PriceID,
                            $"{_supplierName}: {viewModel.Product.Name}");
                    }
                }

                Prices.Remove(viewModel);
                LoadAvailableProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка");
            }
        }

        private (bool canDelete, string message) CanDeletePrice(int priceId)
        {
            var sb = new System.Text.StringBuilder();
            var hasBlockers = false;

            using (var context = new RestaurantEntities())
            {
                var linkedItems = context.PurchaseItems
                    .Include(pi => pi.Purchases)
                    .Where(pi => pi.PriceID == priceId)
                    .ToList();

                if (!linkedItems.Any())
                {
                    return (true, "");
                }

                var activeItems = linkedItems
                    .Where(pi => pi.Purchases.PurchaseStatus != "Delivered")
                    .ToList();

                if (activeItems.Any())
                {
                    hasBlockers = true;
                    var purchaseIds = activeItems
                        .Select(pi => pi.PurchaseID)
                        .Distinct()
                        .OrderBy(id => id)
                        .ToList();

                    sb.AppendLine($"Цена используется в активных закупках ({purchaseIds.Count}):");
                    foreach (var id in purchaseIds)
                    {
                        var purchase = activeItems.First(pi => pi.PurchaseID == id).Purchases;
                        var itemCount = activeItems.Count(pi => pi.PurchaseID == id);
                        sb.AppendLine($"   • Закупка от {purchase.CreatedAt:dd.MM.yyyy} ({itemCount} поз., статус: {purchase.PurchaseStatus})");
                    }
                    sb.AppendLine("   Завершите или отмените эти закупки перед удалением цены.");
                    sb.AppendLine();
                }
            }

            return (!hasBlockers, sb.ToString());
        }

        private void SwitchToViewMode(Border container, ProductPriceViewModel viewModel)
        {
            var priceText = FindVisualChild<TextBlock>(container, "PriceText");
            var priceEditBox = FindVisualChild<TextBox>(container, "PriceEditBox");
            var editBtn = FindVisualChild<Button>(container, "EditButton");
            var saveBtn = FindVisualChild<Button>(container, "SaveButton");
            var cancelBtn = FindVisualChild<Button>(container, "CancelEditButton");

            if (priceText != null) priceText.Visibility = Visibility.Visible;
            if (priceEditBox != null) priceEditBox.Visibility = Visibility.Collapsed;
            if (editBtn != null) editBtn.Visibility = Visibility.Visible;
            if (saveBtn != null) saveBtn.Visibility = Visibility.Collapsed;
            if (cancelBtn != null) cancelBtn.Visibility = Visibility.Collapsed;
        }

        private void SwitchToEditMode(Border container, ProductPriceViewModel viewModel)
        {
            var priceText = FindVisualChild<TextBlock>(container, "PriceText");
            var priceEditBox = FindVisualChild<TextBox>(container, "PriceEditBox");
            var editBtn = FindVisualChild<Button>(container, "EditButton");
            var saveBtn = FindVisualChild<Button>(container, "SaveButton");
            var cancelBtn = FindVisualChild<Button>(container, "CancelEditButton");

            if (priceText != null) priceText.Visibility = Visibility.Collapsed;
            if (priceEditBox != null) priceEditBox.Visibility = Visibility.Visible;
            if (editBtn != null) editBtn.Visibility = Visibility.Collapsed;
            if (saveBtn != null) saveBtn.Visibility = Visibility.Visible;
            if (cancelBtn != null) cancelBtn.Visibility = Visibility.Visible;

            if (priceEditBox != null)
            {
                priceEditBox.Focus();
                priceEditBox.SelectAll();
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Name == name)
                    return element;

                var found = FindVisualChild<T>(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
                parent = VisualTreeHelper.GetParent(parent);
            return parent as T;
        }

    }
    public class ProductPriceViewModel : INotifyPropertyChanged
    {

        private ProductPrices _price;
        public ProductPrices Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged();
            }
        }

        public Products Product { get; set; }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged();
            }
        }

        private bool _isNew;
        public bool IsNew
        {
            get => _isNew;
            set
            {
                _isNew = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
