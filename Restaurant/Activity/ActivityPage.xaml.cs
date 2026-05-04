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

namespace Restaurant.Activity
{
    /// <summary>
    /// Логика взаимодействия для ActivityPage.xaml
    /// </summary>
    public partial class ActivityPage : Page, INotifyPropertyChanged
    {
        private ObservableCollection<ActivityLogViewModel> _allLogs;

        private ObservableCollection<ActivityLogViewModel> _filteredLogs;
        public ObservableCollection<ActivityLogViewModel> FilteredLogs
        {
            get => _filteredLogs;
            set
            {
                _filteredLogs = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<EmployeeFilterItem> Employees { get; set; }
        public ObservableCollection<string> EventTypes { get; set; }
        public ObservableCollection<string> EntityNames { get; set; }

        private DateTime? _filterDateFrom;
        public DateTime? FilterDateFrom
        {
            get => _filterDateFrom;
            set
            {
                _filterDateFrom = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        private DateTime? _filterDateTo;
        public DateTime? FilterDateTo
        {
            get => _filterDateTo;
            set
            {
                _filterDateTo = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        private EmployeeFilterItem _selectedEmployee;
        public EmployeeFilterItem SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        private string _selectedEventType;
        public string SelectedEventType
        {
            get => _selectedEventType;
            set
            {
                _selectedEventType = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        private string _selectedEntityName;
        public string SelectedEntityName
        {
            get => _selectedEntityName;
            set
            {
                _selectedEntityName = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public ActivityPage(UserSession userSession)
        {
            InitializeComponent();

            LoadFilterData();
            LoadLogs();

            DataContext = this;
        }

        private void LoadFilterData()
        {
            using (var context = new RestaurantEntities())
            {
                var employees = context.Employees
                    .OrderBy(e => e.LName)
                    .ThenBy(e => e.FName)
                    .ToList();

                Employees = new ObservableCollection<EmployeeFilterItem>(
                    employees.Select(e => new EmployeeFilterItem
                    {
                        EmployeeID = e.EmployeeID,
                        FullName = $"{e.LName} {e.FName} {e.MName}".Trim()
                    })
                );

                Employees.Insert(0, new EmployeeFilterItem { EmployeeID = 0, FullName = "Все сотрудники" });

                EventTypes = new ObservableCollection<string>
                {
                    "Все события",
                    "Добавление",
                    "Редактирование",
                    "Удаление",
                };

                var entityNames = context.ActivityLog
                    .Select(l => l.EntityName)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();

                EntityNames = new ObservableCollection<string>(entityNames);
                EntityNames.Insert(0, "Все объекты");
            }

            FilterDateFrom = DateTime.Today.AddDays(-7);
            FilterDateTo = DateTime.Today;
            SelectedEmployee = Employees.FirstOrDefault();
            SelectedEventType = EventTypes.FirstOrDefault();
            SelectedEntityName = EntityNames.FirstOrDefault();
        }

        private void LoadLogs()
        {
            using (var context = new RestaurantEntities())
            {
                var logs = context.ActivityLog
                    .OrderByDescending(l => l.EventDate)
                    .ToList();

                _allLogs = new ObservableCollection<ActivityLogViewModel>(
                    logs.Select(l => new ActivityLogViewModel
                    {
                        LogID = l.LogID,
                        EventDate = l.EventDate,
                        EmployeeID = l.EmployeeID ?? 0,
                        EmployeeFullName = l.EmployeeNameSnapshot,
                        EventType = l.EventType,
                        EntityName = l.EntityName,
                        EntityID = l.EntityID,
                        Description = l.Description,
                        Details = l.Details
                    })
                );
            }

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allLogs == null) return;

            var filtered = _allLogs.AsEnumerable();

            if (FilterDateFrom.HasValue)
            {
                filtered = filtered.Where(l => l.EventDate.Date >= FilterDateFrom.Value.Date);
            }

            if (FilterDateTo.HasValue)
            {
                filtered = filtered.Where(l => l.EventDate.Date <= FilterDateTo.Value.Date);
            }

            if (SelectedEmployee != null && SelectedEmployee.EmployeeID > 0)
            {
                filtered = filtered.Where(l => l.EmployeeID == SelectedEmployee.EmployeeID);
            }

            if (!string.IsNullOrEmpty(SelectedEventType) && SelectedEventType != "Все события")
            {
                filtered = filtered.Where(l => l.EventType == SelectedEventType);
            }

            if (!string.IsNullOrEmpty(SelectedEntityName) && SelectedEntityName != "Все объекты")
            {
                filtered = filtered.Where(l => l.EntityName == SelectedEntityName);
            }

            FilteredLogs = new ObservableCollection<ActivityLogViewModel>(filtered);
        }

        private void ResetFilterButt_Click(object sender, RoutedEventArgs e)
        {
            FilterDateFrom = DateTime.Today.AddDays(-7);
            FilterDateTo = DateTime.Today;
            SelectedEmployee = Employees.FirstOrDefault();
            SelectedEventType = EventTypes.FirstOrDefault();
            SelectedEntityName = EntityNames.FirstOrDefault();
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

    public class ActivityLogViewModel
    {
        public int LogID { get; set; }
        public DateTime EventDate { get; set; }
        public int? EmployeeID { get; set; }
        public string EmployeeFullName { get; set; }
        public string EventType { get; set; }
        public string EntityName { get; set; }
        public int? EntityID { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
    }

    public class EmployeeFilterItem
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; }
    }
}
