using Microsoft.Win32;
using Restaurant.Dish;
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
using System.Runtime.Remoting.Contexts;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Restaurant.Product
{
    public partial class EditProdWindow : Window, INotifyPropertyChanged
    {
        private readonly int? _productId;
        private readonly UserSession user;

        private Products _backupProduct;

        public ObservableCollection<ProdCategories> Categories { get; set; }

        private Products _product;
        public Products Product
        {
            get => _product;
            set { _product = value; OnPropertyChanged(); }
        }

        public EditProdWindow(UserSession userSession, int? productId = null)
        {
            InitializeComponent();

            user = userSession;
            _productId = productId;

            LoadCategories();

            if (productId.HasValue)
            {
                LoadProduct(productId.Value);
            }
            else
            {
                Product = new Products
                {
                    Name = "",
                    Unit = ""
                };
            }

            DataContext = this;
        }

        private void LoadCategories()
        {
            using (var context = new RestaurantEntities())
            {
                Categories = new ObservableCollection<ProdCategories>(
                    context.ProdCategories.OrderBy(c => c.CategoryName).ToList()
                );
            }
        }

        private void LoadProduct(int productId)
        {
            using (var context = new RestaurantEntities())
            {
                var product = context.Products.Find(productId);
                if (product == null) return;

                _backupProduct = new Products
                {
                    ProductID = product.ProductID,
                    Name = product.Name,
                    Unit = product.Unit,
                    ImagePath = product.ImagePath,
                    CategoryID = product.CategoryID
                };

                Product = new Products
                {
                    ProductID = product.ProductID,
                    Name = product.Name,
                    Unit = product.Unit,
                    ImagePath = product.ImagePath,
                    CategoryID = product.CategoryID
                };
            }
        }

        private void ChooseImageBtn_Click(object sender, RoutedEventArgs e)
        {
            string img = ImageService.ChooseImage("Products", ProductImage);
            if (img != null)
            {
                Product.ImagePath = img;
                OnPropertyChanged(nameof(Product));
            }
        }

        private void SaveButt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Product.Name))
            {
                MessageBox.Show("Введите название продукта.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Product.Unit))
            {
                MessageBox.Show("Введите единицу измерения.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new RestaurantEntities())
                {
                    Products savedProduct;

                    if (_productId.HasValue)
                    {
                        savedProduct = context.Products.Find(Product.ProductID);
                        if (savedProduct != null)
                        {
                            savedProduct.Name = Product.Name;
                            savedProduct.Unit = Product.Unit;
                            savedProduct.ImagePath = Product.ImagePath;
                            savedProduct.CategoryID = Product.CategoryID;

                            context.SaveChanges();

                            AuditService.LogUpdate(user.EmployeeID, _backupProduct, savedProduct,
                                "Продукты", savedProduct.ProductID, savedProduct.Name);
                        }
                    }
                    else
                    {
                        savedProduct = new Products
                        {
                            Name = Product.Name,
                            Unit = Product.Unit,
                            ImagePath = Product.ImagePath,
                            CategoryID = Product.CategoryID
                        };
                        context.Products.Add(savedProduct);
                        context.SaveChanges();

                        AuditService.LogCreate(user.EmployeeID, "Продукты", savedProduct.ProductID, savedProduct.Name);
                    }
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

        private void CategoriesBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProdCategoriesWindow(user);
            window.Owner = this;

            int? selectedCategoryId = Product.CategoryID;

            if (window.ShowDialog() == true)
            {
                LoadCategories();
                OnPropertyChanged(nameof(Categories));

                if (selectedCategoryId.HasValue)
                {
                    Product.CategoryID = selectedCategoryId.Value;
                    OnPropertyChanged(nameof(Product));
                }
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
    }
}