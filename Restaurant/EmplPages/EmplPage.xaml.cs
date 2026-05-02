using Restaurant;
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
using System.Data.Entity;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Restaurant.EmplPages
{

    public partial class EmplPage : Page, INotifyPropertyChanged
    {
        private readonly UserSession _user;

        public ObservableCollection<Employees> Employees { get; set; }

        public EmplPage(UserSession userSession)
        {
            InitializeComponent();
            _user = userSession;

            Employees = new ObservableCollection<Employees>();
            LoadEmployees();

            DataContext = this;
        }

        private void LoadEmployees()
        {
            using (var context = new RestaurantEntities())
            {
                var employees = context.Employees
                    .Include(e => e.Positions)
                    .OrderBy(e => e.LName)
                    .ThenBy(e => e.FName)
                    .ToList();

                Employees.Clear();
                foreach (var emp in employees)
                {
                    Employees.Add(emp);
                }
            }
        }

        private void AddEmployeeBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditEmployeeWindow(_user);
            window.Owner = Application.Current.MainWindow;

            if (window.ShowDialog() == true)
            {
                LoadEmployees();
            }
        }

        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var employeeId = (int)button?.Tag;

            var window = new EditEmployeeWindow(_user, employeeId);
            window.Owner = Application.Current.MainWindow;

            if (window.ShowDialog() == true)
            {
                LoadEmployees();
            }
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var employeeId = (int)button?.Tag;

            if (employeeId == 1)
            {
                MessageBox.Show("Администратора системы удалить нельзя.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (canDelete, message) = CanDeleteEmployee(employeeId);

            if (!canDelete)
            {
                MessageBox.Show(message, "Невозможно удалить сотрудника",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var employee = Employees.FirstOrDefault(emp => emp.EmployeeID == employeeId);
            var employeeName = employee != null ? $"{employee.LName} {employee.FName}" : "";

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить сотрудника \"{employeeName}\"?\n\n{message}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new RestaurantEntities())
                {
                    var dbEmployee = context.Employees.Find(employeeId);
                    if (dbEmployee != null)
                    {
                        context.Employees.Remove(dbEmployee);
                        context.SaveChanges();

                        AuditService.LogDelete(_user.EmployeeID, "Сотрудники", employeeId,
                            $"{dbEmployee.LName} {dbEmployee.FName} {dbEmployee.MName}");
                    }
                }

                LoadEmployees();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (bool canDelete, string message) CanDeleteEmployee(int employeeId)
        {
            var sb = new StringBuilder();
            var hasBlockers = false;

            using (var context = new RestaurantEntities())
            {
                var activeOrders = context.Orders
                    .Where(o => o.EmployeeID == employeeId && o.Status != "Completed")
                    .ToList();

                if (activeOrders.Any())
                {
                    hasBlockers = true;
                    sb.AppendLine($"⏳ Сотрудник связан с активными заказами ({activeOrders.Count}):");
                    foreach (var order in activeOrders.Take(5))
                    {
                        sb.AppendLine($"   • Заказ от {order.CreatedAt:dd.MM.yyyy HH:mm} (статус: {order.Status})");
                    }
                    if (activeOrders.Count > 5)
                        sb.AppendLine($"   ... и ещё {activeOrders.Count - 5}");
                    sb.AppendLine("   Завершите или отмените эти заказы перед удалением сотрудника.");
                    sb.AppendLine();
                }

                var activePurchases = context.Purchases
                    .Where(p => p.EmployeeID == employeeId
                             && p.PurchaseStatus != "Delivered")
                    .ToList();

                if (activePurchases.Any())
                {
                    hasBlockers = true;
                    sb.AppendLine($"⏳ Сотрудник связан с активными закупками ({activePurchases.Count}):");
                    foreach (var purchase in activePurchases.Take(5))
                    {
                        sb.AppendLine($"   • Закупка от {purchase.CreatedAt:dd.MM.yyyy} (статус: {purchase.PurchaseStatus})");
                    }
                    if (activePurchases.Count > 5)
                        sb.AppendLine($"   ... и ещё {activePurchases.Count - 5}");
                    sb.AppendLine("   Завершите или отмените эти закупки перед удалением сотрудника.");
                    sb.AppendLine();
                }
            }

            return (!hasBlockers, sb.ToString());
        }

        private void PositionsBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new PositionsWindow(_user);
            window.Owner = Application.Current.MainWindow;

            window.PositionsChanged += (s, args) =>
            {
                LoadEmployees();
            };

            window.ShowDialog();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}