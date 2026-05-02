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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Restaurant.Purnchases
{
    /// <summary>
    /// Логика взаимодействия для SuppliersPage.xaml
    /// </summary>
    public partial class SuppliersPage : Page, INotifyPropertyChanged
    {
        private readonly UserSession user;
        private Suppliers BackupSupplier;

        public ObservableCollection<SupplierViewModel> Suppliers { get; set; }

        public SuppliersPage(UserSession userSession)
        {
            InitializeComponent();
            user = userSession;

            Suppliers = new ObservableCollection<SupplierViewModel>();
            BackupSupplier = new Suppliers();
            LoadSuppliers();

            DataContext = this;
        }

        private void LoadSuppliers()
        {
            using (var context = new RestaurantEntities())
            {
                var suppliers = context.Suppliers
                    .OrderBy(s => s.SupplierID)
                    .ToList();

                Suppliers.Clear();
                for (int i = 0; i < suppliers.Count; i++)
                {
                    Suppliers.Add(new SupplierViewModel
                    {
                        Index = i + 1,
                        Supplier = suppliers[i],
                        IsEditing = false,
                        IsNew = false
                    });
                }
            }
        }

        private void AddSupplierButt_Click(object sender, RoutedEventArgs e)
        {
            if (Suppliers.Any(s => s.IsEditing))
            {
                MessageBox.Show("Сначала закончите редактирование текущего поставщика.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newSupplier = new Suppliers();
            var viewModel = new SupplierViewModel
            {
                Index = Suppliers.Count + 1,
                Supplier = newSupplier,
                IsEditing = true,
                IsNew = true
            };

            Suppliers.Add(viewModel);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var item = SuppliersItemsControl.ItemContainerGenerator
                    .ContainerFromItem(viewModel) as ContentPresenter;

                if (item != null)
                {
                    SwitchToEditMode(item, viewModel);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void EditSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (Suppliers.Any(s => s.IsEditing))
            {
                MessageBox.Show("Сначала закончите редактирование текущего поставщика.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            var viewModel = button.Tag as SupplierViewModel;

            if (viewModel == null || viewModel.IsEditing) return;

            var parent = FindParent<ContentPresenter>(button);
            if (parent == null) return;

            BackupSupplier = new Suppliers
            {
                SupplierID = viewModel.Supplier.SupplierID,
                Name = viewModel.Supplier.Name,
                Phone = viewModel.Supplier.Phone,
                Email = viewModel.Supplier.Email
            };

            viewModel.IsEditing = true;

            SwitchToEditMode(parent, viewModel);
        }

        private void SaveSupplier_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button.Tag as SupplierViewModel;

            if (viewModel == null) return;

            if (string.IsNullOrWhiteSpace(viewModel.Supplier.Name))
            {
                MessageBox.Show("Название поставщика обязательно для заполнения.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new RestaurantEntities())
                {
                   var nameExists = context.Suppliers
                       .Any(s => s.Name.Equals(viewModel.Supplier.Name, StringComparison.OrdinalIgnoreCase)
                              && s.SupplierID != viewModel.Supplier.SupplierID);

                    if (nameExists)
                    {
                        MessageBox.Show($"Поставщик с названием \"{viewModel.Supplier.Name}\" уже существует.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(viewModel.Supplier.Phone))
                    {
                        var phoneExists = context.Suppliers
                            .Any(s => s.Phone == viewModel.Supplier.Phone
                                   && s.SupplierID != viewModel.Supplier.SupplierID);

                        if (phoneExists)
                        {
                            MessageBox.Show($"Телефон \"{viewModel.Supplier.Phone}\" уже используется.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(viewModel.Supplier.Email))
                    {
                        var emailExists = context.Suppliers
                            .Any(s => s.Email == viewModel.Supplier.Email
                                   && s.SupplierID != viewModel.Supplier.SupplierID);

                        if (emailExists)
                        {
                            MessageBox.Show($"Email \"{viewModel.Supplier.Email}\" уже используется.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    if (viewModel.IsNew)
                    {
                        var newSupplier = new Suppliers
                        {
                            Name = viewModel.Supplier.Name,
                            Phone = viewModel.Supplier.Phone,
                            Email = viewModel.Supplier.Email
                        };
                        context.Suppliers.Add(newSupplier);
                        context.SaveChanges();

                        AuditService.LogCreate(user.EmployeeID, "Поставщики", newSupplier.SupplierID, newSupplier.Name);
                    }
                    else
                    {
                        var dbSupplier = context.Suppliers.Find(viewModel.Supplier.SupplierID);
                        if (dbSupplier != null)
                        {
                            var backup = new Suppliers
                            {
                                Name = dbSupplier.Name,
                                Phone = dbSupplier.Phone,
                                Email = dbSupplier.Email
                            };

                            dbSupplier.Name = viewModel.Supplier.Name;
                            dbSupplier.Phone = viewModel.Supplier.Phone;
                            dbSupplier.Email = viewModel.Supplier.Email;

                            var updatedSupplier = new Suppliers
                            {
                                Name = dbSupplier.Name,
                                Phone = dbSupplier.Phone,
                                Email = dbSupplier.Email
                            };

                            context.SaveChanges();

                            AuditService.LogUpdate(user.EmployeeID, backup, updatedSupplier,
                                "Поставщики", dbSupplier.SupplierID, dbSupplier.Name);
                        }
                    }
                }

                var parent = FindParent<ContentPresenter>(button);
                if (parent != null)
                {
                    SwitchToViewMode(parent, viewModel);
                }

                viewModel.IsEditing = false;
                viewModel.IsNew = false;

                LoadSuppliers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button.Tag as SupplierViewModel;

            if (viewModel == null) return;

            if (viewModel.IsNew)
            {
                Suppliers.Remove(viewModel);
                LoadSuppliers();
                return;
            }

            if (BackupSupplier != null)
            {
                viewModel.Supplier = new Suppliers
                {
                    Name = BackupSupplier.Name,
                    Phone = BackupSupplier.Phone,
                    Email = BackupSupplier.Email,
                };
            }

            var parent = FindParent<ContentPresenter>(button);
            if (parent != null)
            {
                SwitchToViewMode(parent, viewModel);
            }

            viewModel.IsEditing = false;
        }
        private void DeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button.Tag as SupplierViewModel;

            if (viewModel == null || viewModel.IsNew)
            {
                Suppliers.Remove(viewModel);
                return;
            }

            var (canDelete, message) = CanDeleteSupplier(viewModel.Supplier.SupplierID);

            if (!canDelete)
            {
                MessageBox.Show(message, "Невозможно удалить поставщика",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить поставщика \"{viewModel.Supplier.Name}\"?\n\n{message}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new RestaurantEntities())
                {
                    var dbSupplier = context.Suppliers.Find(viewModel.Supplier.SupplierID);
                    if (dbSupplier != null)
                    {
                        context.Suppliers.Remove(dbSupplier);
                        context.SaveChanges();

                        AuditService.LogDelete(user.EmployeeID, "Поставщики",
                            viewModel.Supplier.SupplierID, viewModel.Supplier.Name);
                    }
                }

                Suppliers.Remove(viewModel);
                LoadSuppliers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (bool canDelete, string message) CanDeleteSupplier(int supplierId)
        {
            var sb = new StringBuilder();
            var hasBlockers = false;

            using (var context = new RestaurantEntities())
            {
                var activePurchases = context.Purchases
                    .Where(p => p.SupplierID == supplierId
                             && p.PurchaseStatus != "Delivered")
                    .ToList();

                if (activePurchases.Any())
                {
                    hasBlockers = true;
                    sb.AppendLine($"⏳ Поставщик связан с активными закупками ({activePurchases.Count}):");
                    foreach (var purchase in activePurchases.Take(5))
                    {
                        sb.AppendLine($"   • Закупка №{purchase.PurchaseID} от {purchase.CreatedAt:dd.MM.yyyy} (статус: {purchase.PurchaseStatus})");
                    }
                    if (activePurchases.Count > 5)
                        sb.AppendLine($"   ... и ещё {activePurchases.Count - 5}");
                    sb.AppendLine("   Завершите или отмените эти закупки перед удалением поставщика.");
                    sb.AppendLine();
                }

                var productPrices = context.ProductPrices
                    .Where(pp => pp.SupplierID == supplierId)
                    .ToList();

                if (productPrices.Any() && !hasBlockers)
                {
                    sb.AppendLine($"Все цены на продукты удаляемого постващика ({productPrices.Count}) также будут удалены.");
                }
            }

            return (!hasBlockers, sb.ToString());
        }

        private void SwitchToEditMode(ContentPresenter container, SupplierViewModel viewModel)
        {
            var nameText = FindVisualChild<TextBlock>(container, "NameText");
            var nameBox = FindVisualChild<TextBox>(container, "NameBox");
            var phoneText = FindVisualChild<TextBlock>(container, "PhoneText");
            var phoneBox = FindVisualChild<TextBox>(container, "PhoneBox");
            var emailText = FindVisualChild<TextBlock>(container, "EmailText");
            var emailBox = FindVisualChild<TextBox>(container, "EmailBox");
            var editButton = FindVisualChild<Button>(container, "EditButton");
            var saveButton = FindVisualChild<Button>(container, "SaveButton");
            var cancelButton = FindVisualChild<Button>(container, "CancelButton");

            if (nameText != null) nameText.Visibility = Visibility.Collapsed;
            if (nameBox != null) nameBox.Visibility = Visibility.Visible;
            if (phoneText != null) phoneText.Visibility = Visibility.Collapsed;
            if (phoneBox != null) phoneBox.Visibility = Visibility.Visible;
            if (emailText != null) emailText.Visibility = Visibility.Collapsed;
            if (emailBox != null) emailBox.Visibility = Visibility.Visible;
            if (editButton != null) editButton.Visibility = Visibility.Collapsed;
            if (saveButton != null) saveButton.Visibility = Visibility.Visible;
            if (cancelButton != null) cancelButton.Visibility = Visibility.Visible;

            if (nameBox != null) nameBox.Focus();
        }

        private void SwitchToViewMode(ContentPresenter container, SupplierViewModel viewModel)
        {
            var nameText = FindVisualChild<TextBlock>(container, "NameText");
            var nameBox = FindVisualChild<TextBox>(container, "NameBox");
            var phoneText = FindVisualChild<TextBlock>(container, "PhoneText");
            var phoneBox = FindVisualChild<TextBox>(container, "PhoneBox");
            var emailText = FindVisualChild<TextBlock>(container, "EmailText");
            var emailBox = FindVisualChild<TextBox>(container, "EmailBox");
            var editButton = FindVisualChild<Button>(container, "EditButton");
            var saveButton = FindVisualChild<Button>(container, "SaveButton");
            var cancelButton = FindVisualChild<Button>(container, "CancelButton");

            if (nameText != null) nameText.Visibility = Visibility.Visible;
            if (nameBox != null) nameBox.Visibility = Visibility.Collapsed;
            if (phoneText != null) phoneText.Visibility = Visibility.Visible;
            if (phoneBox != null) phoneBox.Visibility = Visibility.Collapsed;
            if (emailText != null) emailText.Visibility = Visibility.Visible;
            if (emailBox != null) emailBox.Visibility = Visibility.Collapsed;
            if (editButton != null) editButton.Visibility = Visibility.Visible;
            if (saveButton != null) saveButton.Visibility = Visibility.Collapsed;
            if (cancelButton != null) cancelButton.Visibility = Visibility.Collapsed;
        }

        private static T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Name == name)
                {
                    return element;
                }

                var found = FindVisualChild<T>(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OpenPrices_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button.Tag as SupplierViewModel;

            if (viewModel == null) return;

            var pricesWindow = new SupplierPricesWindow(
                user,
                viewModel.Supplier.SupplierID,
                viewModel.Supplier.Name);

            pricesWindow.Owner = Application.Current.MainWindow;
            pricesWindow.ShowDialog();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }

    public class SupplierViewModel : INotifyPropertyChanged
    {
        public int Index { get; set; }

        private Suppliers _supplier;
        public Suppliers Supplier
        {
            get => _supplier;
            set
            {
                _supplier = value;
                OnPropertyChanged();
            }
        }


        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged();
            }
        }

        private bool _isNew;
        public bool IsNew
        {
            get => _isNew;
            set
            {
                _isNew = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
