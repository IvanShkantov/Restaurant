using Restaurant.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
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

namespace Restaurant.Dish
{
    /// <summary>
    /// Логика взаимодействия для DishCategoriesWindow.xaml
    /// </summary>
    public partial class DishCategoriesWindow : Window
    {
        private readonly UserSession user;
        private int _nextTempId = -1;

        public ObservableCollection<DishCategoryEditViewModel> Categories { get; set; }


        public event EventHandler CategoriesChanged;

        public DishCategoriesWindow(UserSession userSession)
        {
            InitializeComponent();
            user = userSession;

            Categories = new ObservableCollection<DishCategoryEditViewModel>();
            LoadCategories();

            DataContext = this;
        }

        private void LoadCategories()
        {
            using (var context = new RestaurantEntities())
            {
                var existingCategories = context.DishCategories
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                Categories.Clear();
                foreach (var category in existingCategories)
                {
                    Categories.Add(new DishCategoryEditViewModel
                    {
                        CategoryID = category.DishCategoryID,
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

            var newCategory = new DishCategoryEditViewModel
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
            var category = button?.Tag as DishCategoryEditViewModel;

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
            var category = button?.Tag as DishCategoryEditViewModel;

            if (category == null) return;

            category.IsDeleted = false;
        }

        private (bool canDelete, string message) CanDeleteCategory(int categoryId)
        {
            using (var context = new RestaurantEntities())
            {
                var dishesCount = context.Dishes.Count(d => d.CategoryID == categoryId);

                if (dishesCount > 0)
                {
                    return (false,
                        $"Категория содержит {dishesCount} блюд.\n" +
                        "Переместите блюда в другую категорию перед удалением.");
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
                            var newCategory = new DishCategories
                            {
                                CategoryName = category.Name.Trim()
                            };
                            context.DishCategories.Add(newCategory);

                            context.SaveChanges();
                            AuditService.LogCreate(user.EmployeeID, "Категории блюд", category.CategoryID, category.Name);
                        }
                        else if (category.IsExisting)
                        {
                            if (category.IsDeleted)
                            {
                                var dbCategory = context.DishCategories.Find(category.CategoryID);
                                if (dbCategory != null)
                                {
                                    context.DishCategories.Remove(dbCategory);
                                    AuditService.LogDelete(user.EmployeeID, "Категории блюд", dbCategory.DishCategoryID, category.Name);
                                }
                            }
                            else if (category.IsModified)
                            {
                                DishCategories dbCategory = context.DishCategories.Find(category.CategoryID);
                                if (dbCategory != null)
                                {
                                    AuditService.LogUpdate(user.EmployeeID, dbCategory, 
                                        new DishCategories { DishCategoryID = dbCategory.DishCategoryID, CategoryName = category.Name },
                                        "Категории блюд", category.CategoryID, category.Name);
                                    dbCategory.CategoryName = category.Name.Trim();
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
                    var exists = context.DishCategories.Any(c =>
                        c.CategoryName.ToLower() == category.Name.Trim().ToLower()
                        && c.DishCategoryID != category.CategoryID);

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

    public class DishCategoryEditViewModel : INotifyPropertyChanged
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
