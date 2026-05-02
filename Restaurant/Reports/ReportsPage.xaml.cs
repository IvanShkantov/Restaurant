using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Restaurant.Reports
{
    /// <summary>
    /// Логика взаимодействия для ReportsPage.xaml
    /// </summary>
    public partial class ReportsPage : Page, INotifyPropertyChanged
    {
        public ObservableCollection<string> ReportTypes { get; set; }
        public ObservableCollection<string> PeriodTypes { get; set; }
        public ObservableCollection<EmployeeFilterItem> Employees { get; set; }
        public ObservableCollection<SupplierFilterItem> Suppliers { get; set; }

        private string _selectedReportType;
        public string SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                _selectedReportType = value;
                OnPropertyChanged();
                UpdateFiltersVisibility();
                ShowResult();
            }
        }

        private bool _groupByMonth;
        public bool GroupByMonth
        {
            get => _groupByMonth;
            set
            {
                _groupByMonth = value;
                OnPropertyChanged();
                ShowResult();
            }
        }

        private DateTime _filterDateFrom = DateTime.Today.AddDays(-30);
        public DateTime FilterDateFrom
        {
            get => _filterDateFrom;
            set
            {
                _filterDateFrom = value;
                OnPropertyChanged();
                ShowResult();
            }
        }

        private DateTime _filterDateTo = DateTime.Today;
        public DateTime FilterDateTo
        {
            get => _filterDateTo;
            set
            {
                _filterDateTo = value;
                OnPropertyChanged();
                ShowResult();
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
                ShowResult();
            }
        }

        private SupplierFilterItem _selectedSupplier;
        public SupplierFilterItem SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
                OnPropertyChanged();
                ShowResult();
            }
        }

        public ReportsPage(UserSession userSession)
        {
            InitializeComponent();

            LoadFilterData();

            DataContext = this;
        }

        private ObservableCollection<OperationViewModel> _operations;
        public ObservableCollection<OperationViewModel> Operations
        {
            get => _operations;
            set
            {
                _operations = value;
                OnPropertyChanged();
            }
        }

        private void LoadFilterData()
        {
            ReportTypes = new ObservableCollection<string>
            {
                "Выручка",
                "Расходы",
                "Прибыль"
            };
            SelectedReportType = ReportTypes[2];

            using (var context = new RestaurantEntities())
            {
                var employees = context.Employees
                    .OrderBy(e => e.LName)
                    .ToList();

                Employees = new ObservableCollection<EmployeeFilterItem>(
                    employees.Select(e => new EmployeeFilterItem
                    {
                        EmployeeID = e.EmployeeID,
                        FullName = $"{e.LName} {e.FName}".Trim()
                    })
                );
                Employees.Insert(0, new EmployeeFilterItem { EmployeeID = 0, FullName = "Все сотрудники" });
                SelectedEmployee = Employees[0];

                var suppliers = context.Suppliers
                    .OrderBy(s => s.Name)
                    .ToList();

                Suppliers = new ObservableCollection<SupplierFilterItem>(
                    suppliers.Select(s => new SupplierFilterItem
                    {
                        SupplierID = s.SupplierID,
                        Name = s.Name
                    })
                );
                Suppliers.Insert(0, new SupplierFilterItem { SupplierID = 0, Name = "Все поставщики" });
                SelectedSupplier = Suppliers[0];
            }

            UpdateFiltersVisibility();
        }

        private void UpdateFiltersVisibility()
        {
            EmployeeFilterLabel.Visibility = SelectedReportType == "Расходы" ? Visibility.Collapsed : Visibility.Visible;
            EmployeeFilterCombo.Visibility = SelectedReportType == "Расходы" ? Visibility.Collapsed : Visibility.Visible;
            RevenueCard.Visibility = SelectedReportType == "Расходы" ? Visibility.Collapsed : Visibility.Visible;


            SupplierFilterLabel.Visibility = SelectedReportType == "Выручка" ? Visibility.Collapsed : Visibility.Visible;
            SupplierFilterCombo.Visibility = SelectedReportType == "Выручка" ? Visibility.Collapsed : Visibility.Visible;
            ExpensesCard.Visibility = SelectedReportType == "Выручка" ? Visibility.Collapsed : Visibility.Visible;

            ProfitCard.Visibility = SelectedReportType == "Прибыль" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowResult()
        {
            GenerateReport();
            LoadOperationsList();
        }

        private void GenerateButt_Click(object sender, RoutedEventArgs e)
        {
            ShowResult();
        }

        private void LoadOperationsList()
        {
            try
            {
                using (var context = new RestaurantEntities())
                {
                    int? employeeId;
                    if (SelectedEmployee?.EmployeeID > 0) employeeId = SelectedEmployee.EmployeeID;
                    else employeeId = null;

                    int? supplierId;
                    if (SelectedSupplier?.SupplierID > 0) supplierId = SelectedSupplier.SupplierID;
                    else supplierId = null;

                    string reportType;
                    if (SelectedReportType == "Выручка") reportType = "Revenue";
                    else if (SelectedReportType == "Расходы") reportType = "Expenses";
                    else reportType = "Profit";

                    var operations = context.Database.SqlQuery<OperationDetail>(
                        "SELECT * FROM dbo.GetOperationsList(@p0, @p1, @p2, @p3, @p4) ORDER BY OperationDate DESC",
                        FilterDateFrom, FilterDateTo, reportType, employeeId, supplierId
                    ).ToList();

                    var viewModels = new ObservableCollection<OperationViewModel>(
                        operations.Select(o => new OperationViewModel
                        {
                            OperationType = o.OperationType,
                            OperationID = o.OperationID,
                            OperationDate = o.OperationDate,
                            EmployeeName = o.EmployeeName,
                            SupplierName = o.SupplierName ?? "—",
                            Amount = o.Amount,
                            Status = o.Status
                        })
                    );

                    var groupedCollection = new GroupedOperationsCollection();

                    if (SelectedReportType == "Прибыль")
                        OperationsTitle.Text = $"СПИСОК ОПЕРАЦИЙ (";
                    else if (SelectedReportType == "Выручка")
                        OperationsTitle.Text = $"СПИСОК ЗАКАЗОВ (";
                    else
                        OperationsTitle.Text = $"СПИСОК ЗАКУПОК (";

                    if (!GroupByMonth)
                    {
                        Operations = new ObservableCollection<OperationViewModel>(viewModels);
                        OperationsItemsControl.ItemsSource = Operations;

                        OperationsTitle.Text += $"{Operations?.Count ?? 0})";
                    }
                    else
                    {
                        IEnumerable<IGrouping<string, OperationViewModel>> groups;
                        int count = 0;

                        groups = viewModels.GroupBy(o =>
                            new DateTime(o.OperationDate.Year, o.OperationDate.Month, 1).ToString("MMMM yyyy"));

                        foreach (var group in groups.OrderByDescending(g => g.First().OperationDate))
                        {
                            var operationGroup = new OperationGroup
                            {
                                GroupTitle = group.Key,
                                GroupDate = group.First().OperationDate,
                                Items = new ObservableCollection<OperationViewModel>(group.ToList())
                            };

                            count += operationGroup.Items.Count();
                            groupedCollection.AddGroup(operationGroup);
                        }

                        OperationsItemsControl.ItemsSource = groupedCollection;
                        OperationsTitle.Text += $"{count})";
                    }

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки операций: {ex.Message}");
            }
        }


        private void GenerateReport()
        {
            try
            {
                using (var context = new RestaurantEntities())
                {
                    int? employeeId;
                    if (SelectedEmployee?.EmployeeID > 0) employeeId = SelectedEmployee.EmployeeID;
                    else employeeId = null;

                    int? supplierId;
                    if (SelectedSupplier?.SupplierID > 0) supplierId = SelectedSupplier.SupplierID;
                    else supplierId = null;

                    var details = context.Database.SqlQuery<ProfitDetail>(
                        "SELECT * FROM dbo.GetProfitByPeriod(@p0, @p1, @p2, @p3) ORDER BY PeriodDate",
                        FilterDateFrom, FilterDateTo, employeeId, supplierId).ToList();

                    if (SelectedReportType == "Выручка")
                    {
                        var revenue = context.Database.SqlQuery<decimal>(
                            "SELECT dbo.GetRevenue(@p0, @p1, @p2)",
                            FilterDateFrom, FilterDateTo, employeeId).FirstOrDefault();

                        RevenueValue.Text = FormatAsDollars(revenue);
                        RevenueDetail.Text = $"{details.Sum(d => d.OrderCount)} заказов";
                    }
                    else if (SelectedReportType == "Расходы")
                    {
                        var expenses = context.Database.SqlQuery<decimal>(
                            "SELECT dbo.GetExpenses(@p0, @p1, @p2)",
                            FilterDateFrom, FilterDateTo, supplierId).FirstOrDefault();

                        ExpensesValue.Text = FormatAsDollars(expenses);
                        ExpensesDetail.Text = $"{details.Sum(d => d.PurchaseCount)} закупок";
                    }
                    else
                    {
                        var revenue = context.Database.SqlQuery<decimal>(
                            "SELECT dbo.GetRevenue(@p0, @p1, @p2)",
                            FilterDateFrom, FilterDateTo, employeeId).FirstOrDefault();

                        var expenses = context.Database.SqlQuery<decimal>(
                            "SELECT dbo.GetExpenses(@p0, @p1, @p2)",
                            FilterDateFrom, FilterDateTo, supplierId).FirstOrDefault();

                        var profit = revenue - expenses;

                        RevenueValue.Text = FormatAsDollars(revenue);
                        ExpensesValue.Text = FormatAsDollars(expenses);
                        ProfitValue.Text = FormatAsDollars(profit);

                        RevenueDetail.Text = $"{details.Sum(d => d.OrderCount)} заказов";
                        ExpensesDetail.Text = $"{details.Sum(d => d.PurchaseCount)} закупок";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string FormatAsDollars(decimal value)
        {
            string sign = value < 0 ? "-" : "";
            return $"{sign}${Math.Abs(value):N2}";
        }

        private void ExportButt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var revenueValue = RevenueValue.Text;
                var revenueDetail = RevenueDetail.Text;

                var expensesValue = ExpensesValue.Text;
                var expensesDetail = ExpensesDetail.Text;

                var profitValue = ProfitValue.Text;
                var profitDetail = ProfitDetail.Text;

                var employeeFilter = SelectedEmployee?.EmployeeID > 0 ? SelectedEmployee.FullName : "";
                var supplierFilter = SelectedSupplier?.SupplierID > 0 ? SelectedSupplier.Name : "";

                var operations = OperationsItemsControl.ItemsSource as IEnumerable<object>;

                var doc = ReportGenerator.CreateReport(
                    reportType: SelectedReportType,
                    groupByMonth: GroupByMonth,
                    dateFrom: FilterDateFrom,
                    dateTo: FilterDateTo,
                    employeeFilter: employeeFilter,
                    supplierFilter: supplierFilter,
                    revenueValue: revenueValue,
                    expensesValue: expensesValue,
                    profitValue: profitValue,
                    revenueDetail: revenueDetail,
                    expensesDetail: expensesDetail,
                    profitDetail: profitDetail,
                    operations: operations
                );

                var previewWindow = new ReportPreviewWindow(doc);
                previewWindow.Owner = Application.Current.MainWindow;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

    public class EmployeeFilterItem
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; }
    }

    public class SupplierFilterItem
    {
        public int SupplierID { get; set; }
        public string Name { get; set; }
    }

    public class ProfitDetail
    {
        public DateTime PeriodDate { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal Profit { get; set; }
        public int OrderCount { get; set; }
        public int PurchaseCount { get; set; }
    }
public class OperationDetail
{
    public string OperationType { get; set; }
    public int OperationID { get; set; }
    public DateTime OperationDate { get; set; }
    public string EmployeeName { get; set; }
    public string SupplierName { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
}

public class OperationViewModel
{
    public string OperationType { get; set; }
    public int OperationID { get; set; }
    public DateTime OperationDate { get; set; }
    public string EmployeeName { get; set; }
    public string SupplierName { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public string OperationTypeIcon => OperationType == "Заказ" ? "📋" : "📦";
}
public class ReportRowViewModel
    {
        public DateTime PeriodDate { get; set; }
        public string PeriodDisplay { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal Profit { get; set; }
        public int OrderCount { get; set; }
        public int PurchaseCount { get; set; }
    }
}
