using Restaurant.Activity;
using Restaurant.EmplPages;
using Restaurant.LogIn;
using Restaurant.Order;
using Restaurant.Dish;
using Restaurant.Product;
using Restaurant.Purnchases;
using Restaurant.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Collections.Specialized.BitVector32;
using Restaurant.Menu;
using Restaurant.Services;

namespace Restaurant
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        { 
            InitializeComponent();

            using (var context = new RestaurantEntities())
            {
                var positions = context.Positions.Where(p => p.PositionID == 1).First();

                var session = new UserSession
                {
                    EmployeeID = 1,
                    FullName = AuditService.EmployeeFullName(1),
                    PositionName = "Администратор",
                    Permissions = positions.Permissions.Select(p => p.PermissionName).ToList()
                };

                //Close();
                //MainFrame.Navigate(new LoginPage());
                //MainFrame.Navigate(new MenuPage());

                MainFrame.Navigate(new MainPage(session));
                //MainFrame.Navigate(new SuppliersPage(session));

                //MainFrame.Navigate(new OrderPage(session));
                //MainFrame.Navigate(new PurchPage(session));

                //MainFrame.Navigate(new DishesPage(session));
                //MainFrame.Navigate(new ProdPage(session));
                //MainFrame.Navigate(new WarehousePage());

                //MainFrame.Navigate(new ActivityPage(session));
                //MainFrame.Navigate(new ReportsPage(session

            }

        }

        public void Navigate(Page page)
        {
            MainFrame.Navigate(page);
        }
    }

}
