using Restaurant.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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

namespace Restaurant.EmplPages
{
    public partial class EditEmployeeWindow : Window, INotifyPropertyChanged
    {
        private readonly int? _employeeId;
        private readonly UserSession _user;

        private Employees _backupEmployee;

        public ObservableCollection<Positions> Positions { get; set; }

        private string _lastName;
        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(); }
        }

        private string _firstName;
        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(); }
        }

        private string _middleName;
        public string MiddleName
        {
            get => _middleName;
            set { _middleName = value; OnPropertyChanged(); }
        }

        private Positions _selectedPosition;
        public Positions SelectedPosition
        {
            get => _selectedPosition;
            set { _selectedPosition = value; OnPropertyChanged(); }
        }

        private string _inviteCode;
        public string InviteCode
        {
            get => _inviteCode;
            set { _inviteCode = value; OnPropertyChanged(); }
        }

        public bool IsNewEmployee => !_employeeId.HasValue;

        public EditEmployeeWindow(UserSession userSession, int? employeeId = null)
        {
            InitializeComponent();

            _user = userSession;
            _employeeId = employeeId;

            LoadPositions();

            if (employeeId.HasValue)
            {
                Title = "Редактирование сотрудника";
                TitleText.Text = "РЕДАКТИРОВАНИЕ СОТРУДНИКА";
                LoadEmployee(employeeId.Value);
            }
            else
            {
                Title = "Новый сотрудник";
                TitleText.Text = "НОВЫЙ СОТРУДНИК";
                LastName = "";
                FirstName = "";
                MiddleName = "";
                InviteCode = "";
            }

            DataContext = this;
        }

        private void LoadPositions()
        {
            using (var context = new RestaurantEntities())
            {
                Positions = new ObservableCollection<Positions>(
                    context.Positions.OrderBy(p => p.PositionName).ToList()
                );
            }
        }

        private void LoadEmployee(int employeeId)
        {
            using (var context = new RestaurantEntities())
            {
                var employee = context.Employees.Find(employeeId);
                if (employee == null) return;

                _backupEmployee = new Employees
                {
                    EmployeeID = employee.EmployeeID,
                    LName = employee.LName,
                    FName = employee.FName,
                    MName = employee.MName,
                    PositionID = employee.PositionID
                };

                LastName = employee.LName;
                FirstName = employee.FName;
                MiddleName = employee.MName ?? "";
                SelectedPosition = Positions.FirstOrDefault(p => p.PositionID == employee.PositionID);
            }
        }

        private void GenerateInviteCode_Click(object sender, RoutedEventArgs e)
        {
            var random = new Random();
            var code = new StringBuilder();

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            for (int i = 0; i < 12; i++)
            {
                code.Append(chars[random.Next(chars.Length)]);
            }

            InviteCode = code.ToString();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LastName))
            {
                MessageBox.Show("Введите фамилию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(FirstName))
            {
                MessageBox.Show("Введите имя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedPosition == null)
            {
                MessageBox.Show("Выберите должность.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsNewEmployee && string.IsNullOrWhiteSpace(InviteCode))
            {
                MessageBox.Show("Сгенерируйте пригласительный код.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new RestaurantEntities())
                {
                    if (IsNewEmployee)
                    {
                        var newEmployee = new Employees
                        {
                            LName = LastName.Trim(),
                            FName = FirstName.Trim(),
                            MName = MiddleName?.Trim(),
                            PositionID = SelectedPosition.PositionID,
                            Login = null,
                            PasswordHash = PasswordHasher.Hash(InviteCode),
                            IsActivated = false
                        };

                        context.Employees.Add(newEmployee);
                        context.SaveChanges();

                        AuditService.LogCreate(_user.EmployeeID, "Сотрудники", newEmployee.EmployeeID, EmplFullName(newEmployee));
                    }
                    else
                    {
                        var employee = context.Employees.Find(_employeeId.Value);
                        if (employee != null)
                        {
                            employee.LName = LastName.Trim();
                            employee.FName = FirstName.Trim();
                            employee.MName = MiddleName?.Trim();
                            employee.PositionID = SelectedPosition.PositionID;

                            context.SaveChanges();

                            AuditService.LogUpdate(_user.EmployeeID, _backupEmployee, employee,
                                "Сотрудники", employee.EmployeeID, EmplFullName(employee));
                        }
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string EmplFullName(Employees employee)
        {
            return $"{employee.LName} {employee.FName} {employee.MName}";
        }

        private void PositionsBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new PositionsWindow(_user);
            window.Owner = this;

            int? selectedPositionId = SelectedPosition?.PositionID;

            window.PositionsChanged += (s, args) =>
            {
                LoadPositions();
                OnPropertyChanged(nameof(Positions));

                if (selectedPositionId.HasValue)
                {
                    SelectedPosition = Positions.FirstOrDefault(p => p.PositionID == selectedPositionId.Value);
                }
            };

            window.ShowDialog();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
