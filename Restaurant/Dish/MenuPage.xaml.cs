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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Restaurant
{
    public partial class MenuPage : Page, INotifyPropertyChanged
    {
        public ObservableCollection<MenuCategoryViewModel> Categories { get; set; }

        public MenuPage()
        {
            InitializeComponent();
            Categories = new ObservableCollection<MenuCategoryViewModel>();
            LoadMenu();
            DataContext = this;
        }

        private void LoadMenu()
        {
            using (var context = new RestaurantEntities())
            {
                var dishes = context.Dishes
                    .Include(d => d.DishCategories)
                    .Include(d => d.DishProducts.Select(dp => dp.Products))
                    .OrderBy(d => d.Name)
                    .ToList();

                var groupedDishes = dishes
                    .Where(d => d.DishCategories != null)
                    .GroupBy(d => d.DishCategories)
                    .OrderBy(g => g.Key.CategoryName)
                    .ToList();

                var uncategorized = dishes
                    .Where(d => d.DishCategories == null)
                    .ToList();

                Categories.Clear();

                foreach (var group in groupedDishes)
                {
                    Categories.Add(new MenuCategoryViewModel
                    {
                        CategoryName = group.Key.CategoryName,
                        Dishes = new ObservableCollection<MenuDishViewModel>(
                            group.Select(d => new MenuDishViewModel
                            {
                                Name = d.Name,
                                Price = d.Price,
                                ImagePath = d.ImagePath,
                                Ingredients = string.Join(", ",
                                    d.DishProducts.Select(dp => dp.Products?.Name ?? ""))
                            })
                        )
                    });
                }

                if (uncategorized.Any())
                {
                    Categories.Add(new MenuCategoryViewModel
                    {
                        CategoryName = "Другие блюда",
                        Dishes = new ObservableCollection<MenuDishViewModel>(
                            uncategorized.Select(d => new MenuDishViewModel
                            {
                                Name = d.Name,
                                Price = d.Price,
                                ImagePath = d.ImagePath,
                                Ingredients = string.Join(", ",
                                    d.DishProducts.Select(dp => dp.Products?.Name ?? ""))
                            })
                        )
                    });
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MenuCategoryViewModel
    {
        public string CategoryName { get; set; }
        public ObservableCollection<MenuDishViewModel> Dishes { get; set; }
    }

    public class MenuDishViewModel
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ImagePath { get; set; }
        public string Ingredients { get; set; }
    }
}
