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
using System.Windows.Shapes;

namespace Restaurant.EmplPages
{
    public partial class PositionsWindow : Window
    {
        private readonly UserSession _user;
        private int _nextTempId = -1;

        public ObservableCollection<PositionEditViewModel> Positions { get; set; }

        public event EventHandler PositionsChanged;

        public PositionsWindow(UserSession userSession)
        {
            InitializeComponent();
            _user = userSession;

            Positions = new ObservableCollection<PositionEditViewModel>();
            LoadPositions();

            DataContext = this;
        }

        private void LoadPositions()
        {
            using (var context = new RestaurantEntities())
            {
                var allData = context.vw_AllPermissions.ToList();

                var existingPositions = context.Positions
                    .OrderBy(p => p.PositionName)
                    .ToList();

                Positions.Clear();

                foreach (var position in existingPositions)
                {
                    var positionData = allData.Where(d => d.PositionID == position.PositionID).ToList();

                    var positionVM = new PositionEditViewModel
                    {
                        PositionID = position.PositionID,
                        OriginalName = position.PositionName,
                        Name = position.PositionName,
                        IsExisting = true,
                        IsNew = false,
                        IsModified = false,
                        IsDeleted = false,
                        Permissions = new ObservableCollection<PermissionItemViewModel>(
                            positionData.Select(p => new PermissionItemViewModel
                            {
                                PositionId = position.PositionID,
                                PermissionId = p.PermissionID,
                                PermissionName = p.PermissionName,
                                HasPermission = (bool)p.HasPermission
                            })
                        )
                    };

                    foreach (var perm in positionVM.Permissions)
                    {
                        perm.CompleteInitialization();
                        perm.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(PermissionItemViewModel.HasPermission))
                            {
                                positionVM.IsModified = true;
                            }
                        };
                    }

                    Positions.Add(positionVM);
                }
            }
        }

        private void AddPositionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Positions.Any(p => p.IsNew && string.IsNullOrWhiteSpace(p.Name)))
            {
                MessageBox.Show("Сначала заполните название предыдущей новой должности.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newPosition = new PositionEditViewModel
            {
                PositionID = _nextTempId--,
                OriginalName = "",
                Name = "",
                IsExisting = false,
                IsNew = true,
                IsModified = false,
                IsDeleted = false,
                Permissions = new ObservableCollection<PermissionItemViewModel>()
            };

            using (var context = new RestaurantEntities())
            {
                var allPermissions = context.Permissions.OrderBy(p => p.PermissionName).ToList();
                foreach (var perm in allPermissions)
                {
                    var permVM = new PermissionItemViewModel
                    {
                        PositionId = newPosition.PositionID,
                        PermissionId = perm.PermissionID,
                        PermissionName = perm.PermissionName,
                        HasPermission = false
                    };
                    permVM.CompleteInitialization();
                    permVM.PropertyChanged += (s, ev) =>
                    {
                        if (ev.PropertyName == nameof(PermissionItemViewModel.HasPermission))
                        {
                            newPosition.IsModified = true;
                        }
                    };
                    newPosition.Permissions.Add(permVM);
                }
            }

            Positions.Add(newPosition);
        }

        private void DeletePosition_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var position = button?.Tag as PositionEditViewModel;

            if (position == null) return;

            if (position.IsNew)
            {
                Positions.Remove(position);
                return;
            }

            var (canDelete, message) = CanDeletePosition(position.PositionID);

            if (!canDelete)
            {
                MessageBox.Show(message, "Невозможно удалить должность",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            position.IsDeleted = true;
            position.IsModified = false;
        }

        private void RestorePosition_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var position = button?.Tag as PositionEditViewModel;

            if (position == null) return;

            position.IsDeleted = false;
        }

        private (bool canDelete, string message) CanDeletePosition(int positionId)
        {
            using (var context = new RestaurantEntities())
            {
                var employeesCount = context.Employees.Count(emp => emp.PositionID == positionId);

                if (employeesCount > 0)
                {
                    return (false,
                        $"Должность назначена {employeesCount} сотрудникам.\n" +
                        "Переместите сотрудников на другие должности перед удалением.");
                }

                return (true, "");
            }
        }
        private void SaveAllBtn_Click(object sender, RoutedEventArgs e)
        {
            var errors = ValidatePositions();
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
                    foreach (var positionVM in Positions)
                    {
                        if (positionVM.IsNew && !positionVM.IsDeleted)
                        {
                            var newPosition = new Positions
                            {
                                PositionName = positionVM.Name.Trim()
                            };
                            context.Positions.Add(newPosition);
                            context.SaveChanges(); 

                            foreach (var perm in positionVM.Permissions.Where(p => p.HasPermission))
                            {
                                var permission = context.Permissions.Find(perm.PermissionId);
                                if (permission != null)
                                {
                                    newPosition.Permissions.Add(permission);

                                    AuditService.LogCreate(_user.EmployeeID, "Права", permission.PermissionID, $"{newPosition.PositionName} - {permission.PermissionName}");
                                }
                            }

                            AuditService.LogCreate(_user.EmployeeID, "Должности", newPosition.PositionID, newPosition.PositionName);
                        }
                        else if (positionVM.IsExisting)
                        {
                            if (positionVM.IsDeleted)
                            {
                                var dbPosition = context.Positions
                                    .Include(p => p.Permissions)
                                    .FirstOrDefault(p => p.PositionID == positionVM.PositionID);

                                if (dbPosition != null)
                                {
                                    var backupName = dbPosition.PositionName;

                                    dbPosition.Permissions.Clear();

                                    context.Positions.Remove(dbPosition);

                                    AuditService.LogDelete(_user.EmployeeID, "Должности", positionVM.PositionID, backupName);
                                }
                            }
                            else
                            {
                                var dbPosition = context.Positions
                                    .Include(p => p.Permissions)
                                    .FirstOrDefault(p => p.PositionID == positionVM.PositionID);

                                if (dbPosition != null)
                                {
                                    var oldName = dbPosition.PositionName;

                                    if (positionVM.IsModified || positionVM.Name.Trim() != oldName)
                                    {
                                        dbPosition.PositionName = positionVM.Name.Trim();
                                        AuditService.LogUpdate(_user.EmployeeID,
                                            new Positions { PositionID = dbPosition.PositionID, PositionName = oldName },
                                            new Positions { PositionID = dbPosition.PositionID, PositionName = dbPosition.PositionName },
                                            "Должности", positionVM.PositionID, dbPosition.PositionName);
                                    }

                                    SavePermissions(context, positionVM.PositionID, positionVM.Permissions);
                                }
                            }
                        }
                    }

                    context.SaveChanges();
                }

                PositionsChanged?.Invoke(this, EventArgs.Empty);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePermissions(RestaurantEntities context, int positionId, ObservableCollection<PermissionItemViewModel> permissions)
        {
            var position = context.Positions
                .Include(p => p.Permissions) 
                .FirstOrDefault(p => p.PositionID == positionId);

            if (position == null) return;

            position.Permissions.Clear();

            foreach (var perm in permissions.Where(p => p.HasPermission))
            {
                var permission = context.Permissions.Find(perm.PermissionId);
                if (permission != null)
                {
                    position.Permissions.Add(permission);
                }
            }
        }

        private List<string> ValidatePositions()
        {
            var errors = new List<string>();

            var activePositions = Positions.Where(p => !p.IsDeleted).ToList();

            var emptyNames = activePositions
                .Where(p => string.IsNullOrWhiteSpace(p.Name))
                .ToList();

            if (emptyNames.Any())
            {
                errors.Add("Заполните названия всех должностей.");
            }

            var duplicates = activePositions
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .GroupBy(p => p.Name.Trim().ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                errors.Add($"Обнаружены дублирующиеся названия: {string.Join(", ", duplicates)}");
            }

            using (var context = new RestaurantEntities())
            {
                foreach (var position in activePositions.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
                {
                    var exists = context.Positions.Any(p =>
                        p.PositionName.ToLower() == position.Name.Trim().ToLower()
                        && p.PositionID != position.PositionID);

                    if (exists)
                    {
                        errors.Add($"Должность \"{position.Name}\" уже существует.");
                    }
                }
            }

            return errors;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Positions.Any(p => p.IsNew || p.IsModified || p.IsDeleted))
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

    public class PositionEditViewModel : INotifyPropertyChanged
    {
        public int PositionID { get; set; }
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

        public ObservableCollection<PermissionItemViewModel> Permissions { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class PermissionItemViewModel : INotifyPropertyChanged
    {
        private bool _isInitializing = true;

        public int PositionId { get; set; }
        public int PermissionId { get; set; }
        public string PermissionName { get; set; }

        private bool _hasPermission;
        public bool HasPermission
        {
            get => _hasPermission;
            set
            {
                if (_hasPermission != value)
                {
                    _hasPermission = value;
                    OnPropertyChanged();
                }
            }
        }

        public void CompleteInitialization()
        {
            _isInitializing = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
