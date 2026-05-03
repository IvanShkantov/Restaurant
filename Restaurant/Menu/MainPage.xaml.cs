using Restaurant.Activity;
using Restaurant.Dish;
using Restaurant.EmplPages;
using Restaurant.LogIn;
using Restaurant.Order;
using Restaurant.Product;
using Restaurant.Reports;
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

namespace Restaurant.Menu
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page, INotifyPropertyChanged
    {
        private readonly UserSession _session;

        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }

        public MainPage(UserSession session)
        {
            InitializeComponent();
            _session = session;

            tbFIO.Text = session.FullName;
            tbPosition.Text = session.PositionName;

            BuildMenu();

            DataContext = this;
        }

        private void BuildMenu()
        {
            MenuItems = new ObservableCollection<MenuItemViewModel>();

            MenuItems.Add(new MenuItemViewModel
            {
                Icon = "🍏",
                Title = "Продукты",
                Tooltip = "Управление продуктами",
                Command = new RelayCommand(() => NavigationService?.Navigate(new ProdPage(_session)))
            });

            if (_session.HasPermission("Управление блюдами"))
            {
                MenuItems.Add(new MenuItemViewModel
                {
                    Icon = "🍽️",
                    Title = "Блюда",
                    Tooltip = "Управление блюдами",
                    Command = new RelayCommand(() => NavigationService?.Navigate(new DishesPage(_session)))
                });
            }

            if (_session.HasPermission("Оформление закупок"))
            {
                MenuItems.Add(new MenuItemViewModel
                {
                    Icon = "🚚",
                    Title = "Закупки",
                    Tooltip = "Оформление закупок",
                    Command = new RelayCommand(() => 
                        NavigationService?.Navigate(new PurchPage(_session)))
                });
            }

            if (_session.HasPermission("Оформление заказов"))
            {
                MenuItems.Add(new MenuItemViewModel
                {
                    Icon = "📋",
                    Title = "Заказы",
                    Tooltip = "Управление заказами",
                    Command = new RelayCommand(() => NavigationService?.Navigate(new OrderPage(_session)))
                });
            }

            if (_session.HasPermission("Управление складом"))
            {
                MenuItems.Add(new MenuItemViewModel
                {
                    Icon = "🗄️",
                    Title = "Склад",
                    Tooltip = "Управление складом",
                    Command = new RelayCommand(() => NavigationService?.Navigate(new WarehousePage()))
                });
            }

            if (_session.HasPermission("Управление сотрудниками"))
            {
                MenuItems.Add(new MenuItemViewModel
                {
                    Icon = "👥",
                    Title = "Сотрудники",
                    Tooltip = "Управление сотрудниками",
                    Command = new RelayCommand(() => NavigationService?.Navigate(new EmplPage(_session)))
                });
            }

            if (_session.HasPermission("Просмотр журнала"))
            {
                MenuItems.Add(new MenuItemViewModel
                {
                    Icon = "📝",
                    Title = "Журнал учёта",
                    Tooltip = "Просмотр журнала деятельности",
                    Command = new RelayCommand(() => NavigationService?.Navigate(new ActivityPage(_session)))
                });
            }

            if (_session.HasPermission("Управление итогами"))
            {
                MenuItems.Add(new MenuItemViewModel
                {
                    Icon = "📊",
                    Title = "Итоги работы",
                    Tooltip = "Управление итогами",
                    Command = new RelayCommand(() => NavigationService?.Navigate(new ReportsPage(_session)))
                });
            }

            OnPropertyChanged(nameof(MenuItems));
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }
            else
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.Navigate(new LoginPage());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MenuItemViewModel
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Tooltip { get; set; }
        public ICommand Command { get; set; }
    }
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
