using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Restaurant.Controls
{
    public class CenteredDataGridTextColumn : DataGridTextColumn
    {
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var textBlock = base.GenerateElement(cell, dataItem) as TextBlock;
            if (textBlock != null)
            {
                textBlock.VerticalAlignment = VerticalAlignment.Center;
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                textBlock.TextTrimming = TextTrimming.CharacterEllipsis;

                var binding = textBlock.GetBindingExpression(TextBlock.TextProperty);
                if (binding != null)
                {
                    textBlock.SetBinding(TextBlock.ToolTipProperty, binding.ParentBinding);
                }
            }
            return textBlock;
        }
    }

    public class LeftDataGridTextColumn : DataGridTextColumn
    {
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var textBlock = base.GenerateElement(cell, dataItem) as TextBlock;
            if (textBlock != null)
            {
                textBlock.VerticalAlignment = VerticalAlignment.Center;
                textBlock.HorizontalAlignment = HorizontalAlignment.Left;
                textBlock.Margin = new Thickness(10,0,0,0);
                textBlock.TextTrimming = TextTrimming.CharacterEllipsis;

                var binding = textBlock.GetBindingExpression(TextBlock.TextProperty);
                if (binding != null)
                {
                    textBlock.SetBinding(TextBlock.ToolTipProperty, binding.ParentBinding);
                }
            }
            return textBlock;
        }
    }
}
