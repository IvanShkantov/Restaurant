using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

namespace Restaurant.Reports
{
    /// <summary>
    /// Логика взаимодействия для ReportPreviewWindow.xaml
    /// </summary>
    public partial class ReportPreviewWindow : Window
    {
        private readonly FlowDocument _document;

        public ReportPreviewWindow(FlowDocument document)
        {
            InitializeComponent();
            _document = document;
            DocViewer.Document = _document;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                _document.PageWidth = printDialog.PrintableAreaWidth;
                _document.PageHeight = printDialog.PrintableAreaHeight;

                printDialog.PrintDocument(
                    ((IDocumentPaginatorSource)_document).DocumentPaginator,
                    "Итоги работы предприятия");
            }
        }

        private void SaveXps_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Word files (*.rtf)|*.rtf"
            };

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;

                FlowDocument doc = DocViewer.Document;

                TextRange range = new TextRange(doc.ContentStart, doc.ContentEnd);

                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    range.Save(fs, DataFormats.Rtf);
                }

                Process.Start(new ProcessStartInfo(filePath)
                {
                    UseShellExecute = true
                });
            }
        }

        private void SaveDocx_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Word Document (*.docx)|*.docx|All files (*.*)|*.*",
                FileName = $"Отчет_{DateTime.Now:dd.MM.yyyy}.docx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var xaml = System.Windows.Markup.XamlWriter.Save(_document);

                    File.WriteAllText(dialog.FileName, xaml);

                    MessageBox.Show("Отчет сохранен. Для полноценного формата DOCX требуется установка OpenXML SDK.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
