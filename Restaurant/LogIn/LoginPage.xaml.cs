using Restaurant.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Restaurant.LogIn
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            isVisitorSelected = true;

        }
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = tbLogin.Text;
            string password = tbPassword.Password;

            UserSession session = Login(login, password);

            if (session != null)
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.Navigate(new MainPage(session));
            }
            else
            {
                MessageBox.Show("Неверные данные", "Ошибка входа",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private UserSession Login(string login, string password)
        {
            using (var context = new RestaurantEntities())
            {
                var user = context.sp_LoginUser(login, PasswordHasher.Hash(password)).FirstOrDefault();

                if (user == null) return null;

                var positions = context.Positions.Where(p => p.PositionID == user.PositionID).First();

                return new UserSession
                {
                    EmployeeID = user.EmployeeID,
                    FullName = $"{user.LName} {user.FName} {user.MName}",
                    PositionName = user.PositionName,
                    Permissions = positions.Permissions.Select(p => p.PermissionName).ToList()
                };
            }
        }

        private UserSession Registrate(string lName, string fName, string mName, string login, string inviteCodeHash, string passwordHash)
        {
            using (var context = new RestaurantEntities())
            {
                try
                {
                    var result = context.sp_RegisterUser(lName, fName, mName, login, inviteCodeHash, passwordHash).FirstOrDefault();

                    if (result == null)
                    {
                        ShowError("Неизвестная ошибка регистрации.");
                        return null;
                    }

                    switch (result.Result)
                    {
                        case "Success":
                            var positions = context.Positions.Where(p => p.PositionID == result.PositionID).First();

                            return new UserSession
                            {
                                EmployeeID = (int)result.EmployeeID,
                                FullName = $"{result.LName} {result.FName} {result.MName}",
                                PositionName = result.PositionName,
                                Permissions = positions.Permissions.Select(p => p.PermissionName).ToList()
                            };

                        case "LoginExists":
                            ShowError("Логин уже занят. Придумайте другой логин.");
                            LoginBox.Focus();
                            LoginBox.SelectAll();
                            return null;

                        case "AlreadyActivated":
                            ShowError("Сотрудник с такими данными уже зарегистрирован.");
                            return null;

                        case "InvalidCode":
                            ShowError("Неверные ФИО или пригласительный код.\nПроверьте правильность введённых данных.");
                            break;

                        default:
                            ShowError(result.Message ?? "Неизвестная ошибка.");
                            return null;
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка: {ex.Message}");
                    return null;
                }
            }
            return null;
        }

        private void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            var error = ValidateForm();
            if (!string.IsNullOrEmpty(error))
            {
                ShowError(error);
                return;
            }

            var lName = LNameBox.Text.Trim();
            var fName = FNameBox.Text.Trim();
            var mName = string.IsNullOrWhiteSpace(MNameBox.Text) ? null : MNameBox.Text.Trim();
            var inviteCode = InviteCodeBox.Text.Trim();
            var login = LoginBox.Text.Trim();
            var password = PasswordBox.Password;

            var inviteCodeHash = PasswordHasher.Hash(inviteCode);

            var passwordHash = PasswordHasher.Hash(password);

            UserSession session = Registrate(lName, fName, mName, login, inviteCodeHash, passwordHash);

            if (session != null)
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.Navigate(new MainPage(session));
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка регистрации",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        private string ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(LNameBox.Text))
                return "Введите фамилию.";

            if (string.IsNullOrWhiteSpace(FNameBox.Text))
                return "Введите имя.";

            if (string.IsNullOrWhiteSpace(InviteCodeBox.Text))
                return "Введите пригласительный код.";

            if (string.IsNullOrWhiteSpace(LoginBox.Text))
                return "Придумайте логин.";

            if (LoginBox.Text.Trim().Length < 3)
                return "Логин должен содержать не менее 3 символов.";

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                return "Придумайте пароль.";

            if (PasswordBox.Password.Length < 4)
                return "Пароль должен содержать не менее 4 символов.";

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
                return "Пароли не совпадают.";

            return null;
        }
        

        private void RoleChangedPage()
        {
            Clean();   

            if (isVisitorSelected == true)
            {
                gridVisitor.Visibility = Visibility.Visible;
                gridEmpl.Visibility = Visibility.Collapsed;
            }
            else
            {
                gridVisitor.Visibility = Visibility.Collapsed;
                gridEmpl.Visibility = Visibility.Visible;
            }
        }

        private void Clean()
        {
            tbLogin.Text = "";
            tbPassword.Password = "";
            LNameBox.Text = "";
            FNameBox.Text = "";
            MNameBox.Text = "";
            InviteCodeBox.Text = "";
            LoginBox.Text = "";
            PasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
        }

        private void toReg_butt_Click(object sender, RoutedEventArgs e)
        {
            Clean();
            gridEnter.Visibility = Visibility.Collapsed;
            gridRegistr.Visibility = Visibility.Visible;
        }

        private void toEnter_butt_Click(object sender, RoutedEventArgs e)
        {
            Clean();
            gridEnter.Visibility = Visibility.Visible;
            gridRegistr.Visibility = Visibility.Collapsed;
        }

        private void Menu_btn_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new MenuPage());
        }

        private bool isVisitorSelected = false;

        private void Option1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isVisitorSelected = true;
            AnimateToggle(true);
        }

        private void Option2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isVisitorSelected = false;
            AnimateToggle(false);
        }

        private void AnimateToggle(bool visitorSelected)
        {
            var template = RoleToggle.Template;
            var slider = template.FindName("Slider", RoleToggle) as Border;
            var transform = slider?.RenderTransform as TranslateTransform;
            var option1Text = template.FindName("Option1Text", RoleToggle) as TextBlock;
            var option2Text = template.FindName("Option2Text", RoleToggle) as TextBlock;

            if (transform != null)
            {
                var animation = new DoubleAnimation
                {
                    To = visitorSelected ? 0 : 110,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                transform.BeginAnimation(TranslateTransform.XProperty, animation);

                if (option1Text != null && option2Text != null)
                {
                    option1Text.Foreground = visitorSelected ?
                        new SolidColorBrush(Colors.White) :
                        new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));

                    option2Text.Foreground = visitorSelected ?
                        new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)) :
                        new SolidColorBrush(Colors.White);
                }

                OnRoleChanged(visitorSelected);
            }
        }

        public event EventHandler<bool> RoleChanged;

        protected virtual void OnRoleChanged(bool isVisitor)
        {
            RoleChanged?.Invoke(this, isVisitor);
            RoleChangedPage();
        }
    }
}
