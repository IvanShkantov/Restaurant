using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Restaurant.Reports
{
    public class OperationTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OrderTemplate { get; set; }
        public DataTemplate PurchaseTemplate { get; set; }
        public DataTemplate GroupHeaderTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is OperationGroup)
            {
                return GroupHeaderTemplate;
            }

            if (item is OperationViewModel operation)
            {
                return operation.OperationType == "Заказ" ? OrderTemplate : PurchaseTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }

    public class OperationGroup : INotifyPropertyChanged
    {
        public string GroupTitle { get; set; }
        public DateTime GroupDate { get; set; }
        public ObservableCollection<OperationViewModel> Items { get; set; }

        public decimal TotalOrdersAmount => Items?.Where(i => i.OperationType == "Заказ").Sum(i => i.Amount) ?? 0;
        public decimal TotalPurchasesAmount => Items?.Where(i => i.OperationType == "Закупка").Sum(i => i.Amount) ?? 0;
        public int OrdersCount => Items?.Count(i => i.OperationType == "Заказ") ?? 0;
        public int PurchasesCount => Items?.Count(i => i.OperationType == "Закупка") ?? 0;
        public int TotalCount => Items?.Count ?? 0;

        public string SummaryText
        {
            get
            {
                var parts = new List<string>();

                if (OrdersCount > 0)
                    parts.Add($"📋 Заказов: {OrdersCount} на ${TotalOrdersAmount}");

                if (PurchasesCount > 0)
                    parts.Add($"📦 Закупок: {PurchasesCount} на ${TotalPurchasesAmount}");

                if (OrdersCount > 0 && PurchasesCount > 0)
                {
                    var profit = TotalOrdersAmount - TotalPurchasesAmount;
                    var profitColor = profit >= 0 ? "зелёным" : "красным";
                    parts.Add($"Прибыль: ${profit}");
                }

                return string.Join(" | ", parts);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GroupedOperationsCollection : ObservableCollection<object>
    {
        public void AddGroup(OperationGroup group)
        {
            Add(group);
            foreach (var item in group.Items)
            {
                Add(item);
            }
        }
    }
}
