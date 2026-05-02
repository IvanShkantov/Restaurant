using Restaurant.LogIn;
using Restaurant.Purnchases;
using Restaurant.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
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

namespace Restaurant
{
    /// <summary>
    /// Логика взаимодействия для PurchPage.xaml
    /// </summary>
    public partial class PurchPage : Page
    {
        private readonly UserSession user;

        public ObservableCollection<PurchaseGroupViewModel> Purchases { get; set; }
        public ObservableCollection<PurchaseGroupViewModel> Delivered { get; set; }

        public PurchPage(UserSession userSession)
        {
            InitializeComponent();

            user = userSession;

            Purchases = new ObservableCollection<PurchaseGroupViewModel>();
            Delivered = new ObservableCollection<PurchaseGroupViewModel>();

            LoadPurchases();

            DataContext = this;
        }

        public void LoadPurchases()
        {
            using (var context = new RestaurantEntities())
            {

                var flatData = context.vw_PurchaseDetails
                        .OrderByDescending(p => p.CreatedAt)
                        .ToList();

                var purchases = new ObservableCollection<PurchaseGroupViewModel>(
                    flatData.GroupBy(p => new
                    {
                        p.PurchaseID,
                        p.CreatedAt,
                        p.ClosedAt,
                        p.PurchaseStatus,
                        p.TotalPurchasePrice,
                        p.EmployeeFullName,
                        p.SupplierName,
                        p.SupplierID,
                        p.EmployeeID
                    })
                    .Select(g => new PurchaseGroupViewModel
                    {
                        PurchaseID = g.Key.PurchaseID,
                        CreatedAt = (DateTime)g.Key.CreatedAt,
                        ClosedAt = g.Key.ClosedAt ?? DateTime.Now,
                        PurchaseStatus = g.Key.PurchaseStatus,
                        TotalPurchasePrice = (decimal)g.Key.TotalPurchasePrice,
                        EmployeeFullName = g.Key.EmployeeFullName,
                        SupplierName = g.Key.SupplierName,
                        SupplierID = (int)g.Key.SupplierID,
                        EmployeeID = (int)g.Key.EmployeeID,
                        Items = new ObservableCollection<PurchaseItemViewModel>(
                            g.Select(item => new PurchaseItemViewModel
                            {
                                ProductID = item.ProductID,
                                ProductName = item.ProductName,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice,
                                TotalPrice = (decimal)item.ItemTotalPrice
                            })
                        )
                    })
                );

                Purchases.Clear();

                var archivedPurchases = context.ArchivedPurchases
            .Include(ap => ap.ArchivedPurchaseItems)
            .OrderByDescending(ap => ap.ClosedAt)
            .ToList();

                var delivered = new ObservableCollection<PurchaseGroupViewModel>(
                    archivedPurchases.Select(ap => new PurchaseGroupViewModel
                    {
                        PurchaseID = ap.PurchaseID,
                        CreatedAt = ap.CreatedAt,
                        ClosedAt = ap.ClosedAt,
                        PurchaseStatus = "Delivered",
                        TotalPurchasePrice = ap.TotalPrice,
                        EmployeeFullName = ap.EmployeeFullName,
                        SupplierName = ap.SupplierName,
                        Items = new ObservableCollection<PurchaseItemViewModel>(
                            ap.ArchivedPurchaseItems.Select(pi => new PurchaseItemViewModel
                            {
                                ProductName = pi.ProductName,
                                Quantity = pi.Quantity,
                                UnitPrice = pi.UnitPrice,
                                TotalPrice = pi.TotalPrice
                            })
                        )
                    })
                );

                Delivered.Clear();

                foreach (var p in purchases)
                {
                    if (p.PurchaseStatus == "Pending")
                        Purchases.Add(p);
                }
                foreach (var p in delivered)
                {
                    Delivered.Add(p);
                }
            }
        }

        private void EditPurnchButt_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var purchaseId = (int)button?.DataContext;            

            var window = new EditPurchaseWindow(user, purchaseId);
            window.Owner = Application.Current.MainWindow;

            if (window.ShowDialog() == true)
            {
                LoadPurchases();
            }
        }

        private void AddPurnchButt_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditPurchaseWindow(user);
            window.Owner = Application.Current.MainWindow;

            if (window.ShowDialog() == true)
            {
                LoadPurchases();
            }
        }

        private void MarkButt_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var purchaseId = (int)button?.DataContext;

            using (var context = new RestaurantEntities())
            {
                var dbPurchase = context.Purchases.Find(purchaseId);
                if (dbPurchase != null)
                {
                    Purchases Backup = new Purchases()
                    {
                        PurchaseID = dbPurchase.PurchaseID,
                        EmployeeID = dbPurchase.EmployeeID,
                        SupplierID = dbPurchase.SupplierID,
                        PurchaseStatus = dbPurchase.PurchaseStatus
                    };

                    dbPurchase.PurchaseStatus = "Delivered";
                    dbPurchase.ClosedAt = DateTime.Now;

                    context.SaveChanges();

                    AuditService.LogUpdate(user.EmployeeID, Backup, dbPurchase, "Поставки", purchaseId, AuditService.PurchaseID(purchaseId));
                }
            }

            LoadPurchases();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var purchaseId = (int)button?.DataContext;

            var result = MessageBox.Show(
                "Вы уверены, что хотите отменить поставку?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            using (var context = new RestaurantEntities())
            {
                var dbPurchase = context.Purchases.Find(purchaseId);
                if (dbPurchase != null)
                {
                    context.Purchases.Remove(dbPurchase);

                    AuditService.LogDelete(user.EmployeeID, "Поставки", purchaseId, AuditService.PurchaseID(purchaseId));

                    context.SaveChanges();
                }
            }

            LoadPurchases();
        }

        private void SuppliersButt_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new SuppliersPage(user));
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }

    public class PurchaseItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class PurchaseGroupViewModel
    {
        public int PurchaseID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ClosedAt { get; set; }
        public string PurchaseStatus { get; set; }
        public decimal TotalPurchasePrice { get; set; }
        public string EmployeeFullName { get; set; }
        public string SupplierName { get; set; }
        public int SupplierID { get; set; }
        public int EmployeeID { get; set; }

        public ObservableCollection<PurchaseItemViewModel> Items { get; set; }

        public string StatusText
        {
            get
            {
                switch (PurchaseStatus)
                {
                    case "Pending": return "⏳ Ожидает доставки";
                    case "Delivered": return "✅ Доставлено";
                    default: return PurchaseStatus;
                }
            }
        }
    }
}