using Restaurant.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
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

namespace Restaurant.Purnchases
{
    /// <summary>
    /// Логика взаимодействия для EditPurchaseWindow.xaml
    /// </summary>
    public partial class EditPurchaseWindow : Window, INotifyPropertyChanged
    {
        private readonly int? _purchaseId;

        private readonly UserSession user;

        private Purchases BackupPurchase;
        private List<PurchaseItems> BackupItems;

        public ObservableCollection<Suppliers> Suppliers { get; set; }
        public ObservableCollection<ProductWithPrice> AvailableProducts { get; set; }
        public ObservableCollection<EditPurchaseItemViewModel> Items { get; set; }

        private Suppliers _selectedSupplier;
        public Suppliers SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (_selectedSupplier != value)
                {
                    if (Items?.Count > 0)
                    {
                        var result = MessageBox.Show(
                            "Сменить поставщика? Все позиции будут удалены.",
                            "Подтверждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                            return;
                    }

                    _selectedSupplier = value;

                    if (value != null)
                    {
                        LoadProductsForSupplier(value.SupplierID);
                        Items?.Clear();
                    }
                    OnPropertyChanged();
                }
            }
        }

        public DateTime PurchaseDate { get; set; }
        public decimal EstimatedTotalPrice => Items?.Sum(i => i.TotalPrice) ?? 0;

        public EditPurchaseWindow(UserSession userSession, int? purchaseId = null)
        {
            InitializeComponent();

            user = userSession;

            _purchaseId = purchaseId;

            LoadSuppliers();

            if (purchaseId.HasValue)
            {
                LoadPurchase(purchaseId.Value);
            }
            else
            {
                PurchaseDate = DateTime.Now;
                Items = new ObservableCollection<EditPurchaseItemViewModel>();
            }

            DataContext = this;
        }

        private void AddItemButt_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new EditPurchaseItemViewModel();
            newItem.PropertyChanged += (s, ev) =>
            {
                if (ev.PropertyName == nameof(EditPurchaseItemViewModel.TotalPrice))
                    OnPropertyChanged(nameof(EstimatedTotalPrice));
            };

            Items.Add(newItem);
        }

        private void DeleteItemButt_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as EditPurchaseItemViewModel;

            Items.Remove(item);
            OnPropertyChanged(nameof(EstimatedTotalPrice));
        }

        private bool CanSave()
        {
            return SelectedSupplier != null && !Items.Any(i => i.SelectedProduct == null && i.Quantity <= 0);
        }

        private void SaveButt_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            if (!CanSave())
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }
            if (Items.Where(i => i.SelectedProduct != null).GroupBy(i => i.SelectedProduct.ProductID).Any(g => g.Count() > 1))
            {
                MessageBox.Show("Удалите дублирующиеся позиции для одинаковых продуктов.");
                return;
            }
            try
            {
                using (var context = new RestaurantEntities())
                {
                    Purchases purchase;
                    int supplCount = context.Purchases.Where(p => p.SupplierID == SelectedSupplier.SupplierID && p.PurchaseID != _purchaseId && p.PurchaseStatus == "Pending").Count();

                    if (supplCount > 0)
                    {
                        MessageBox.Show("Невозможно добавить поставку: уже существует активная поставка для выбранного поставщика.");
                        return;
                    }

                    if (_purchaseId.HasValue)
                    {
                        purchase = context.Purchases
                            .Include(p => p.PurchaseItems)
                            .First(p => p.PurchaseID == _purchaseId);

                        purchase.SupplierID = SelectedSupplier.SupplierID;
                        context.PurchaseItems.RemoveRange(purchase.PurchaseItems);
                    }
                    else
                    {
                        purchase = new Purchases
                        {
                            EmployeeID = user.EmployeeID,
                            SupplierID = SelectedSupplier.SupplierID,
                            PurchaseStatus = "Pending",
                            Price = 0
                        };
                        context.Purchases.Add(purchase);
                        context.SaveChanges();
                    }

                    purchase.CreatedAt = PurchaseDate;

                    var newItems = new List<PurchaseItems>();
                    foreach (var item in Items.Where(i => i.SelectedProduct != null && i.Quantity > 0))
                    {
                        var purchaseItem = new PurchaseItems
                        {
                            PurchaseID = purchase.PurchaseID,
                            ProductID = item.SelectedProduct.ProductID,
                            UnitPrice = item.SelectedProduct.UnitPrice,
                            PriceID = item.SelectedProduct.PriceID,
                            Quantity = item.Quantity,
                            TotalPrice = item.TotalPrice
                        };
                        purchase.PurchaseItems.Add(purchaseItem);
                        newItems.Add(purchaseItem);
                    }

                    context.SaveChanges();

                    context.Entry(purchase).Reload();

                    if (_purchaseId.HasValue)
                    {
                        AuditService.LogUpdate(user.EmployeeID, BackupPurchase, purchase, "Закупки", purchase.PurchaseID, AuditService.PurchaseID(purchase.PurchaseID));
                    }
                    else
                    {
                        AuditService.LogCreate(user.EmployeeID, "Закупки", purchase.PurchaseID, AuditService.PurchaseID(purchase.PurchaseID));
                    }
                    AuditService.GetPurchaseItemsChanges(user.EmployeeID, BackupItems, purchase.PurchaseItems.ToList());
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

        private void LoadSuppliers()
        {
            using (var context = new RestaurantEntities())
            {
                Suppliers = new ObservableCollection<Suppliers>(context.Suppliers.OrderBy(s => s.Name).ToList());
            }
        }

        private void LoadProductsForSupplier(int supplierId)
        {
            using (var context = new RestaurantEntities())
            {
                var productPrices = context.ProductPrices
                    .Where(pp => pp.SupplierID == supplierId)
                    .Include(pp => pp.Products)
                    .ToList();

                AvailableProducts = new ObservableCollection<ProductWithPrice>(
                    productPrices.Select(pp => new ProductWithPrice
                    {
                        ProductID = (int)pp.ProductID,
                        ProductName = pp.Products.Name,
                        PriceID = pp.PriceID,
                        UnitPrice = pp.Price,
                        Unit = pp.Products.Unit
                    })
                );
            }

            OnPropertyChanged(nameof(AvailableProducts));
        }

        private void LoadPurchase(int purchaseId)
        {
            using (var context = new RestaurantEntities())
            {
                var purchase = context.Purchases
                    .Include(p => p.PurchaseItems.Select(pi => pi.Products))
                    .Include(p => p.Suppliers)
                    .FirstOrDefault(p => p.PurchaseID == purchaseId);

                if (purchase == null) return;

                BackupPurchase = new Purchases
                {
                    PurchaseID = purchase.PurchaseID,
                    EmployeeID = purchase.EmployeeID,
                    CreatedAt = purchase.CreatedAt,
                    SupplierID = purchase.SupplierID,
                    PurchaseStatus = purchase.PurchaseStatus,
                    Price = purchase.Price
                };

                BackupItems = purchase.PurchaseItems.Select(pi => new PurchaseItems
                {
                    PurchaseID = pi.PurchaseID,
                    ProductID = pi.ProductID,
                    Quantity = pi.Quantity,
                    PriceID = pi.PriceID,
                    UnitPrice = pi.UnitPrice,
                    TotalPrice = pi.TotalPrice
                }).ToList();

                PurchaseDate = (DateTime)purchase.CreatedAt;
                SelectedSupplier = Suppliers.FirstOrDefault(s => s.SupplierID == purchase.SupplierID);

                LoadProductsForSupplier((int)purchase.SupplierID);

                Items = new ObservableCollection<EditPurchaseItemViewModel>();
                foreach (var item in purchase.PurchaseItems)
                {
                    var productWithPrice = AvailableProducts.FirstOrDefault(p => p.ProductID == item.ProductID);
                    var vm = new EditPurchaseItemViewModel
                    {
                        SelectedProduct = productWithPrice,
                        Quantity = item.Quantity
                    };
                    vm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(EditPurchaseItemViewModel.TotalPrice))
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
    public class ProductWithPrice
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int PriceID { get; set; }
        public decimal UnitPrice { get; set; }
        public string Unit { get; set; }
    }

    public class EditPurchaseItemViewModel : INotifyPropertyChanged
    {
        private ProductWithPrice _selectedProduct;
        public ProductWithPrice SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UnitPrice));
                OnPropertyChanged(nameof(TotalPrice));
                OnPropertyChanged(nameof(DisplayUnit));
            }
        }

        private decimal _quantity = 1;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal UnitPrice => SelectedProduct?.UnitPrice ?? 0;
        public decimal TotalPrice => Quantity * UnitPrice;
        public string DisplayUnit => SelectedProduct?.Unit ?? "";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
