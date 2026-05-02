using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using System.Windows.Shapes;

namespace Restaurant.Product
{
    public partial class WarehousePage : Page, INotifyPropertyChanged
    {
        public ObservableCollection<WarehouseCategoryViewModel> Categories { get; set; }

        public WarehousePage()
        {
            InitializeComponent();
            Categories = new ObservableCollection<WarehouseCategoryViewModel>();
            LoadWarehouse();
            DataContext = this;
        }

        private void LoadWarehouse()
        {
            using (var context = new RestaurantEntities())
            {
                var products = context.Products
                    .OrderBy(p => p.Name)
                    .ToList();

                var warehouseData = context.Warehouse.ToList();

                var warehouseProducts = products.Select(p =>
                {
                    var wh = warehouseData.FirstOrDefault(w => w.ProductID == p.ProductID);
                    return new WarehouseProductViewModel
                    {
                        ProductID = p.ProductID,
                        Name = p.Name,
                        Unit = p.Unit,
                        ImagePath = p.ImagePath,
                        CategoryID = p.CategoryID,
                        Quantity = wh?.Quantity ?? 0
                    };
                }).ToList();

                var categories = context.ProdCategories
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                Categories.Clear();

                foreach (var category in categories)
                {
                    var categoryProducts = warehouseProducts
                        .Where(p => p.CategoryID == category.CategoryID)
                        .OrderBy(p => p.Name)
                        .ToList();

                    if (categoryProducts.Any())
                    {
                        Categories.Add(new WarehouseCategoryViewModel
                        {
                            CategoryName = category.CategoryName,
                            Products = new ObservableCollection<WarehouseProductViewModel>(categoryProducts),
                            ZeroStockCount = categoryProducts.Count(p => p.Quantity == 0),
                            HasZeroStock = categoryProducts.Any(p => p.Quantity == 0)
                        });
                    }
                }

                var uncategorized = warehouseProducts
                    .Where(p => p.CategoryID == null)
                    .OrderBy(p => p.Name)
                    .ToList();

                if (uncategorized.Any())
                {
                    Categories.Add(new WarehouseCategoryViewModel
                    {
                        CategoryName = "Без категории",
                        Products = new ObservableCollection<WarehouseProductViewModel>(uncategorized),
                        ZeroStockCount = uncategorized.Count(p => p.Quantity == 0),
                        HasZeroStock = uncategorized.Any(p => p.Quantity == 0)
                    });
                }
            }

            var allProducts = Categories.SelectMany(c => c.Products).ToList();
            ZeroStockCount.Text = allProducts.Count(p => p.Quantity == 0).ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }

    public class WarehouseCategoryViewModel
    {
        public string CategoryName { get; set; }
        public ObservableCollection<WarehouseProductViewModel> Products { get; set; }
        public int ZeroStockCount { get; set; }
        public bool HasZeroStock { get; set; }
    }

    public class WarehouseProductViewModel : INotifyPropertyChanged
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string ImagePath { get; set; }
        public int? CategoryID { get; set; }

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsZeroStock));
                OnPropertyChanged(nameof(IsInStock));
            }
        }

        public bool IsZeroStock => Quantity == 0;
        public bool IsInStock => Quantity > 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class QuantityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal quantity)
            {
                if (quantity == 0)
                    return new SolidColorBrush(Color.FromRgb(220, 53, 69)); 
                else
                    return new SolidColorBrush(Color.FromRgb(40, 167, 69)); 
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
