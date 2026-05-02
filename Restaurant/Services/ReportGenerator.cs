using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Restaurant.Reports
{
    public static class ReportGenerator
    {
        public static FlowDocument CreateReport(
            string reportType,
            bool groupByMonth,
            DateTime dateFrom,
            DateTime dateTo,
            string employeeFilter,
            string supplierFilter,
            string revenueValue,
            string expensesValue,
            string profitValue,
            string revenueDetail,
            string expensesDetail,
            string profitDetail,
            IEnumerable<object> operations)
        {
            var doc = new FlowDocument();

            doc.PageWidth = 793.7; 
            doc.PageHeight = 1122.7;
            doc.ColumnWidth = doc.PageWidth - 100;
            doc.PagePadding = new Thickness(50);
            doc.FontFamily = new FontFamily("Segoe UI");
            doc.FontSize = 11;

            var title = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            title.Inlines.Add(new Run("ИТОГИ РАБОТЫ ПРЕДПРИЯТИЯ"));
            doc.Blocks.Add(title);

            var dateParagraph = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            };
            dateParagraph.Inlines.Add(new Run($"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}"));
            doc.Blocks.Add(dateParagraph);

            doc.Blocks.Add(CreateSectionHeader("ПРИМЕНЁННЫЕ ФИЛЬТРЫ"));

            var filtersTable = new Table();
            filtersTable.CellSpacing = 0;
            filtersTable.BorderBrush = Brushes.LightGray;
            filtersTable.BorderThickness = new Thickness(1);

            filtersTable.Columns.Add(new TableColumn { Width = new GridLength(120) });
            filtersTable.Columns.Add(new TableColumn {  });

            AddFilterRow(filtersTable, "Тип отчета:", reportType);
            AddFilterRow(filtersTable, "Период:", groupByMonth ? "За месяц" : "Произвольный");
            AddFilterRow(filtersTable, "Дата с:", dateFrom.ToString("dd.MM.yyyy"));
            AddFilterRow(filtersTable, "Дата по:", dateTo.ToString("dd.MM.yyyy"));

            if (!string.IsNullOrEmpty(employeeFilter))
                AddFilterRow(filtersTable, "Сотрудник:", employeeFilter);

            if (!string.IsNullOrEmpty(supplierFilter))
                AddFilterRow(filtersTable, "Поставщик:", supplierFilter);

            doc.Blocks.Add(filtersTable);

            doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 20, 0, 10) });
            doc.Blocks.Add(CreateSectionHeader("ИТОГОВЫЕ ПОКАЗАТЕЛИ"));

            var totalsTable = new Table();
            totalsTable.CellSpacing = 0;
            totalsTable.BorderBrush = Brushes.LightGray;
            totalsTable.BorderThickness = new Thickness(1);

            totalsTable.Columns.Add(new TableColumn {  });
            totalsTable.Columns.Add(new TableColumn {  });
            totalsTable.Columns.Add(new TableColumn {  });

            var totalsHeader = new TableRowGroup();
            var headerRow = new TableRow { Background = Brushes.LightGray };

            if (reportType == "Выручка" || reportType == "Прибыль")
                headerRow.Cells.Add(CreateCell("ВЫРУЧКА", FontWeights.Bold, TextAlignment.Center));

            if (reportType == "Расходы" || reportType == "Прибыль")
                headerRow.Cells.Add(CreateCell("РАСХОДЫ", FontWeights.Bold, TextAlignment.Center));

            if (reportType == "Прибыль")
                headerRow.Cells.Add(CreateCell("ПРИБЫЛЬ", FontWeights.Bold, TextAlignment.Center));

            totalsHeader.Rows.Add(headerRow);

            var valuesRowGroup = new TableRowGroup();
            var valuesRow = new TableRow();
            if (reportType == "Выручка" || reportType == "Прибыль")
                valuesRow.Cells.Add(CreateCell(revenueValue, FontWeights.Bold, TextAlignment.Center,
                    new SolidColorBrush(Color.FromRgb(40, 167, 69))));
            if (reportType == "Расходы" || reportType == "Прибыль")
                valuesRow.Cells.Add(CreateCell(expensesValue, FontWeights.Bold, TextAlignment.Center,
                    new SolidColorBrush(Color.FromRgb(220, 53, 69))));
            if (reportType == "Прибыль")
                valuesRow.Cells.Add(CreateCell(profitValue, FontWeights.Bold, TextAlignment.Center,
                    new SolidColorBrush(Color.FromRgb(0, 123, 255))));
            valuesRowGroup.Rows.Add(valuesRow);

            totalsTable.RowGroups.Add(totalsHeader);
            totalsTable.RowGroups.Add(valuesRowGroup);
            doc.Blocks.Add(totalsTable);

            if (!string.IsNullOrEmpty(revenueDetail) || !string.IsNullOrEmpty(expensesDetail))
            {
                var detailsParagraph = new Paragraph
                {
                    TextAlignment = TextAlignment.Left,
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                if (reportType == "Выручка" || reportType == "Прибыль")
                    detailsParagraph.Inlines.Add(new Run($"{revenueDetail}  "));

                if (reportType == "Расходы" || reportType == "Прибыль")
                    detailsParagraph.Inlines.Add(new Run($"{expensesDetail}  "));

                if (reportType == "Прибыль")
                    detailsParagraph.Inlines.Add(new Run(profitDetail));

                doc.Blocks.Add(detailsParagraph);
            }

            if (operations != null && operations.Any())
            {
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 20, 0, 10) });
                doc.Blocks.Add(CreateSectionHeader("СПИСОК ОПЕРАЦИЙ"));

                foreach (var item in operations)
                {
                    if (groupByMonth && item is OperationGroup group)
                    {
                        var groupParagraph = new Paragraph
                        {
                            Background = Brushes.AliceBlue,
                            Padding = new Thickness(10, 5, 10, 5),
                            Margin = new Thickness(0, 10, 0, 5),
                            FontWeight = FontWeights.Bold,
                            FontSize = 12,
                            BorderBrush = Brushes.LightBlue,
                            BorderThickness = new Thickness(1)
                        };
                        groupParagraph.Inlines.Add(new Run($"📅 {group.GroupTitle}"));
                        groupParagraph.Inlines.Add(new Run($"    {group.SummaryText}"));
                        doc.Blocks.Add(groupParagraph);

                        foreach (var op in group.Items)
                        {
                            doc.Blocks.Add(CreateOperationParagraph(op));
                        }
                    }
                    else if (!groupByMonth && item is OperationViewModel operation)
                    {
                        doc.Blocks.Add(CreateOperationParagraph(operation));
                    }
                }
            }

            var footer = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 9,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 30, 0, 0)
            };
            footer.Inlines.Add(new Run("© Система управления рестораном | Отчет сформирован автоматически"));
            doc.Blocks.Add(footer);

            return doc;
        }

        private static Paragraph CreateSectionHeader(string text)
        {
            return new Paragraph(new Run(text))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(52, 58, 64)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(0, 0, 0, 5)
            };
        }

        private static void AddFilterRow(Table table, string label, string value)
        {
            var rowGroup = table.RowGroups.Count > 0 ? table.RowGroups[0] : new TableRowGroup();
            if (table.RowGroups.Count == 0) table.RowGroups.Add(rowGroup);

            var row = new TableRow();
            row.Cells.Add(CreateCell(label, FontWeights.Bold, TextAlignment.Right));
            row.Cells.Add(CreateCell(value, FontWeights.Normal, TextAlignment.Left));
            rowGroup.Rows.Add(row);
        }

        private static TableCell CreateCell(string text, FontWeight fontWeight,
            TextAlignment alignment, Brush foreground = null)
        {
            return new TableCell(
                new Paragraph(new Run(text ?? ""))
                {
                    FontWeight = fontWeight,
                    TextAlignment = alignment,
                    Foreground = foreground ?? Brushes.Black,
                    Margin = new Thickness(8, 5, 8, 5),
                    FontSize = 11
                })
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5)
            };
        }

        private static Paragraph CreateOperationParagraph(OperationViewModel op)
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(20, 3, 0, 3),
                Padding = new Thickness(10, 5, 10, 5),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Background = op.OperationType == "Заказ" ?
                    new SolidColorBrush(Color.FromRgb(245, 245, 245)) :
                    Brushes.White
            };

            var icon = op.OperationType == "Заказ" ? "📋" : "📦";
            var operationType = op.OperationType.ToUpper();

            paragraph.Inlines.Add(new Run($"{icon} {operationType}  ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = op.OperationType == "Заказ" ?
                    new SolidColorBrush(Color.FromRgb(21, 87, 36)) :
                    new SolidColorBrush(Color.FromRgb(133, 100, 4))
            });

            paragraph.Inlines.Add(new Run($"{op.OperationDate:dd.MM.yyyy HH:mm}  "));
            paragraph.Inlines.Add(new Run($"№{op.OperationID}  "));

            if (!string.IsNullOrEmpty(op.SupplierName) && op.SupplierName != "—")
                paragraph.Inlines.Add(new Run($"Поставщик: {op.SupplierName}  "));

            paragraph.Inlines.Add(new Run($"Сотрудник: {op.EmployeeName}  "));

            paragraph.Inlines.Add(new Run(FormatAsDollars(op.Amount))
            {
                FontWeight = FontWeights.Bold,
                Foreground = op.OperationType == "Заказ" ?
                    new SolidColorBrush(Color.FromRgb(40, 167, 69)) :
                    new SolidColorBrush(Color.FromRgb(0, 123, 255))
            });

            return paragraph;
        }

        public static string FormatAsDollars(decimal value)
        {
            string sign = value < 0 ? "-" : "";
            return $"{sign}${Math.Abs(value):N2}";
        }
    }
}
