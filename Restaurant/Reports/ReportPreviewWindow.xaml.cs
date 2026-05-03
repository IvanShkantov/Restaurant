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


        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
