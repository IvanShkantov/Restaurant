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

namespace Restaurant.Product
{
    public partial class ProdCategoriesWindow : Window
    {
        private readonly UserSession user;
        private int _nextTempId = -1;

        public ObservableCollection<ProdCategoryEditViewModel> Categories { get; set; }

        public event EventHandler CategoriesChanged;

        public ProdCategoriesWindow(UserSession userSession)
        {
            InitializeComponent();
            user = userSession;

            Categories = new ObservableCollection<ProdCategoryEditViewModel>();
            LoadCategories();

            DataContext = this;
        }

        private void LoadCategories()
        {
            using (var context = new RestaurantEntities())
            {
                var existingCategories = context.ProdCategories
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                Categories.Clear();
                foreach (var category in existingCategories)
                {
                    Categories.Add(new ProdCategoryEditViewModel
                    {
                        CategoryID = category.CategoryID,
                        OriginalName = category.CategoryName,
                        Name = category.CategoryName,
                        IsExisting = true,
                        IsNew = false,
                        IsModified = false,
                        IsDeleted = false
                    });
                }
            }
        }

        private void AddCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Categories.Any(c => c.IsNew && string.IsNullOrWhiteSpace(c.Name)))
            {
                MessageBox.Show("Сначала заполните название предыдущей новой категории.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newCategory = new ProdCategoryEditViewModel
            {
                CategoryID = _nextTempId--,
                OriginalName = "",
                Name = "",
                IsExisting = false,
                IsNew = true,
                IsModified = false,
                IsDeleted = false
            };

            Categories.Add(newCategory);
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var category = button?.Tag as ProdCategoryEditViewModel;

            if (category == null) return;

            if (category.IsNew)
            {
                Categories.Remove(category);
                return;
            }

            var (canDelete, message) = CanDeleteCategory(category.CategoryID);

            if (!canDelete)
            {
                MessageBox.Show(message, "Невозможно удалить категорию",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            category.IsDeleted = true;
            category.IsModified = false;
        }

        private void RestoreCategory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var category = button?.Tag as ProdCategoryEditViewModel;

            if (category == null) return;

            category.IsDeleted = false;
        }

        private (bool canDelete, string message) CanDeleteCategory(int categoryId)
        {
            using (var context = new RestaurantEntities())
            {
                var productsCount = context.Products.Count(p => p.CategoryID == categoryId);

                if (productsCount > 0)
                {
                    return (false,
                        $"Категория содержит {productsCount} активных продуктов.\n" +
                        "Переместите продукты в другую категорию или деактивируйте их перед удалением.");
                }

                return (true, "");
            }
        }

        private void SaveAllBtn_Click(object sender, RoutedEventArgs e)
        {
            var errors = ValidateCategories();
            if (errors.Any())
            {
                MessageBox.Show(string.Join("\n", errors), "Ошибки валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new RestaurantEntities())
                {
                    foreach (var category in Categories)
                    {
                        if (category.IsNew && !category.IsDeleted)
                        {
                            var newCategory = new ProdCategories
                            {
                                CategoryName = category.Name.Trim()
                            };
                            context.ProdCategories.Add(newCategory);

                            context.SaveChanges();
                            AuditService.LogCreate(user.EmployeeID, "Категории продуктов", newCategory.CategoryID, newCategory.CategoryName);
                        }
                        else if (category.IsExisting)
                        {
                            if (category.IsDeleted)
                            {
                                var dbCategory = context.ProdCategories.Find(category.CategoryID);
                                if (dbCategory != null)
                                {
                                    var backupName = dbCategory.CategoryName;
                                    context.ProdCategories.Remove(dbCategory);

                                    context.SaveChanges();
                                    AuditService.LogDelete(user.EmployeeID, "Категории продуктов", category.CategoryID, backupName);
                                }
                            }
                            else if (category.IsModified)
                            {
                                var dbCategory = context.ProdCategories.Find(category.CategoryID);
                                if (dbCategory != null)
                                {
                                    var oldName = dbCategory.CategoryName;
                                    dbCategory.CategoryName = category.Name.Trim();

                                    context.SaveChanges();
                                    AuditService.LogUpdate(user.EmployeeID,
                                        new ProdCategories { CategoryID = dbCategory.CategoryID, CategoryName = oldName },
                                        new ProdCategories { CategoryID = dbCategory.CategoryID, CategoryName = dbCategory.CategoryName },
                                        "Категории продуктов", category.CategoryID, dbCategory.CategoryName);
                                }
                            }
                        }
                    }

                    context.SaveChanges();
                }

                CategoriesChanged?.Invoke(this, EventArgs.Empty);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<string> ValidateCategories()
        {
            var errors = new List<string>();

            var activeCategories = Categories.Where(c => !c.IsDeleted).ToList();

            var emptyNames = activeCategories
                .Where(c => string.IsNullOrWhiteSpace(c.Name))
                .ToList();

            if (emptyNames.Any())
            {
                errors.Add("Заполните названия всех категорий.");
            }

            var duplicates = activeCategories
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .GroupBy(c => c.Name.Trim().ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                errors.Add($"Обнаружены дублирующиеся названия: {string.Join(", ", duplicates)}");
            }

            using (var context = new RestaurantEntities())
            {
                foreach (var category in activeCategories.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
                {
                    var exists = context.ProdCategories.Any(c =>
                        c.CategoryName.ToLower() == category.Name.Trim().ToLower()
                        && c.CategoryID != category.CategoryID);

                    if (exists)
                    {
                        errors.Add($"Категория \"{category.Name}\" уже существует в базе данных.");
                    }
                }
            }

            return errors;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Categories.Any(c => c.IsNew || c.IsModified || c.IsDeleted))
            {
                var result = MessageBox.Show(
                    "Есть несохранённые изменения. Закрыть без сохранения?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;
            }

            DialogResult = false;
            Close();
        }
    }

    public class ProdCategoryEditViewModel : INotifyPropertyChanged
    {
        public int CategoryID { get; set; }
        public string OriginalName { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();

                    if (IsExisting && !IsNew)
                    {
                        IsModified = Name?.Trim() != OriginalName?.Trim();
                    }
                }
            }
        }

        private bool _isExisting;
        public bool IsExisting
        {
            get => _isExisting;
            set { _isExisting = value; OnPropertyChanged(); }
        }

        private bool _isNew;
        public bool IsNew
        {
            get => _isNew;
            set { _isNew = value; OnPropertyChanged(); }
        }

        private bool _isModified;
        public bool IsModified
        {
            get => _isModified;
            set { _isModified = value; OnPropertyChanged(); }
        }

        private bool _isDeleted;
        public bool IsDeleted
        {
            get => _isDeleted;
            set { _isDeleted = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

