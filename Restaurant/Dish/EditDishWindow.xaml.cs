using Microsoft.Win32;
using Restaurant.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

namespace Restaurant.Dish
{
    /// <summary>
    /// Логика взаимодействия для EditDishWindow.xaml
    /// </summary>
    public partial class EditDishWindow : Window, INotifyPropertyChanged
    {
        private readonly int? _dishId;
        private readonly UserSession user;

        private Dishes _backupDish;
        private List<DishProducts> BackupProducts;

        public ObservableCollection<DishCategories> Categories { get; set; }
        public ObservableCollection<Products> AvailableProducts { get; set; }
        public ObservableCollection<DishProductViewModel> Products { get; set; }

        private string _name;
        public string DishName
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(); }
        }

        private DishCategories _selectedCategory;
        public DishCategories SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public EditDishWindow(UserSession userSession, int? dishId = null)
        {
            InitializeComponent();

            user = userSession;
            _dishId = dishId;

            BackupProducts = new List<DishProducts>();

            LoadCategories();
            LoadAvailableProducts();

            if (dishId.HasValue)
            {
                LoadDish(dishId.Value);
            }
            else
            {
                DishName = "";
                Price = 0;
                Products = new ObservableCollection<DishProductViewModel>();
            }

            DataContext = this;
        }

        private void LoadCategories()
        {
            using (var context = new RestaurantEntities())
            {
                Categories = new ObservableCollection<DishCategories>(
                    context.DishCategories.OrderBy(c => c.CategoryName).ToList()
                );
            }
        }

        private void LoadAvailableProducts()
        {
            using (var context = new RestaurantEntities())
            {
                AvailableProducts = new ObservableCollection<Products>(
                    context.Products.OrderBy(p => p.Name).ToList()
                );
            }
        }

        private void LoadDish(int dishId)
        {
            using (var context = new RestaurantEntities())
            {
                var dish = context.Dishes
                    .Include(d => d.DishProducts)
                    .Include("DishProducts.Products")
                    .FirstOrDefault(d => d.DishID == dishId);

                if (dish == null) return;

                _backupDish = new Dishes
                {
                    DishID = dish.DishID,
                    Name = dish.Name,
                    Price = dish.Price,
                    ImagePath = dish.ImagePath,
                    CategoryID = dish.CategoryID
                };

                DishName = dish.Name;
                Price = dish.Price;
                ImagePath = dish.ImagePath;
                SelectedCategory = Categories.FirstOrDefault(c => c.DishCategoryID == dish.CategoryID);

                Products = new ObservableCollection<DishProductViewModel>();

                foreach (var item in dish.DishProducts)
                {
                    var product = AvailableProducts.FirstOrDefault(p => p.ProductID == item.ProductID);
                    var vm = new DishProductViewModel
                    {
                        SelectedProduct = product,
                        Quantity = item.Quantity
                    };

                    Products.Add(vm);

                    BackupProducts.Add(new DishProducts
                    {
                        DishID = item.DishID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity
                    });
                }
            }
        }

        private void ChooseImageBtn_Click(object sender, RoutedEventArgs e)
        {
            string img = ImageService.ChooseImage("Dishes", DishImage);
            if (img != null) { ImagePath = img; }
        }

        private void AddProductBtn_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new DishProductViewModel();

            Products.Add(newItem);
        }

        private void DeleteProductBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as DishProductViewModel;

            if (item != null)
            {
                Products.Remove(item);
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(DishName)
                && Price > 0
                && Products != null
                && !Products.Any(p => p.SelectedProduct == null || p.Quantity <= 0);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!CanSave())
            {
                MessageBox.Show("Заполните все обязательные поля.", "Ошибка");
                return;
            }

            if (Products.Where(p => p.SelectedProduct != null)
                .GroupBy(p => p.SelectedProduct.ProductID)
                .Any(g => g.Count() > 1))
            {
                MessageBox.Show("Удалите дублирующиеся продукты.", "Ошибка");
                return;
            }

            try
            {
                using (var context = new RestaurantEntities())
                {
                    Dishes dish;

                    if (_dishId.HasValue)
                    {
                        dish = context.Dishes
                            .Include(d => d.DishProducts)
                            .First(d => d.DishID == _dishId);

                        context.DishProducts.RemoveRange(dish.DishProducts);

                        dish.Name = DishName;
                        dish.Price = Price;
                    }
                    else
                    {
                        dish = new Dishes{ Name = DishName, Price = Price, ImagePath = ImagePath};
                        context.Dishes.Add(dish);
                        context.SaveChanges();
                    }

                    dish.ImagePath = ImagePath;
                    dish.CategoryID = SelectedCategory?.DishCategoryID;

                    foreach (var item in Products)
                    {
                        var dishProduct = new DishProducts
                        {
                            DishID = dish.DishID,
                            ProductID = item.SelectedProduct.ProductID,
                            Quantity = item.Quantity
                        };
                        dish.DishProducts.Add(dishProduct);
                    }

                    AuditService.GetDishProductsChanges(user.EmployeeID, BackupProducts, dish.DishProducts.ToList());

                    context.SaveChanges();

                    if (_dishId.HasValue)
                    {
                        AuditService.LogUpdate(user.EmployeeID, _backupDish, dish,
                            "Блюда", dish.DishID, dish.Name);
                    }
                    else
                    {
                        AuditService.LogCreate(user.EmployeeID, "Блюда",
                            dish.DishID, dish.Name);
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка");
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void CategoriesBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new DishCategoriesWindow(user);
            window.Owner = this;

            int? selectedCategoryId = SelectedCategory?.DishCategoryID;

            if (window.ShowDialog() == true)
            {
                LoadCategories();
                OnPropertyChanged(nameof(Categories));

                if (selectedCategoryId.HasValue)
                {
                    SelectedCategory = Categories.FirstOrDefault(c => c.DishCategoryID == selectedCategoryId.Value);
                }
            }
        }
    }

    public class DishProductViewModel : INotifyPropertyChanged
    {
        private Products _selectedProduct;
        public Products SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
            }
        }

        private decimal _quantity = 1;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value && value > 0)
                {
                    _quantity = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
