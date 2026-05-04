using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Windows;

namespace Restaurant.Services
{
    public static class WarehouseService
    {
        public static WarehouseCheckResult CheckOrderAvailability(
            Dictionary<int, int> orderItems,
            int? excludeOrderId = null)
        {
            using (var context = new RestaurantEntities())
            {
                var activeOrdersRequired = context.Database
                    .SqlQuery<ProductRequirement>(
                        "SELECT * FROM dbo.GetRequiredProductsForActiveOrders()")
                    .ToDictionary(r => r.ProductID);

                if (excludeOrderId.HasValue)
                {
                    var oldOrderItems = context.OrderItems
                        .Where(oi => oi.OrderID == excludeOrderId.Value)
                        .ToList();

                    foreach (var oldItem in oldOrderItems)
                    {
                        var dishProducts = context.DishProducts
                            .Where(dp => dp.DishID == oldItem.DishID)
                            .ToList();

                        foreach (var dp in dishProducts)
                        {
                            if (activeOrdersRequired.ContainsKey(dp.ProductID))
                            {
                                activeOrdersRequired[dp.ProductID].RequiredQuantity -=
                                    dp.Quantity * oldItem.Quantity;
                            }
                        }
                    }
                }

                foreach (var item in orderItems)
                {
                    var dishProducts = context.DishProducts
                        .Where(dp => dp.DishID == item.Key)
                        .ToList();

                    foreach (var dp in dishProducts)
                    {
                        if (activeOrdersRequired.ContainsKey(dp.ProductID))
                        {
                            activeOrdersRequired[dp.ProductID].RequiredQuantity +=
                                dp.Quantity * item.Value;
                        }
                        else
                        {
                            var product = context.Products
                                .Include(p => p.Warehouse)
                                .FirstOrDefault(p => p.ProductID == dp.ProductID);

                            activeOrdersRequired[dp.ProductID] = new ProductRequirement
                            {
                                ProductID = dp.ProductID,
                                ProductName = product?.Name ?? "Неизвестно",
                                Unit = product?.Unit ?? "",
                                RequiredQuantity = dp.Quantity * item.Value,
                                AvailableQuantity = product?.Warehouse?.Quantity ?? 0
                            };
                        }
                    }
                }

                var shortages = activeOrdersRequired.Values
                    .Select(r => new WarehouseAvailabilityResult
                    {
                        ProductID = r.ProductID,
                        ProductName = r.ProductName,
                        Unit = r.Unit,
                        RequiredQuantity = r.RequiredQuantity,
                        AvailableQuantity = r.AvailableQuantity,
                        Shortage = Math.Max(0, r.RequiredQuantity - r.AvailableQuantity)
                    })
                    .Where(r => r.Shortage > 0)
                    .OrderByDescending(r => r.Shortage)
                    .ToList();

                return new WarehouseCheckResult
                {
                    IsSufficient = !shortages.Any(),
                    Shortages = shortages
                };
            }
        }

        public static bool ShowWarehouseWarning(WarehouseCheckResult checkResult)
        {
            if (checkResult.IsSufficient)
                return true;

            var message = checkResult.ShortageMessage;

            MessageBox.Show(message, "Недостаточно продуктов на складе",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    public class ProductRequirement
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public decimal RequiredQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
    }

    public class WarehouseAvailabilityResult
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public decimal RequiredQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal Shortage { get; set; }

        public string ShortageText =>
            $"{ProductName}: нужно {RequiredQuantity:N2} {Unit}, " +
            $"есть {AvailableQuantity:N2} {Unit}, " +
            $"не хватает {Shortage:N2} {Unit}";
    }

    public class WarehouseCheckResult
    {
        public bool IsSufficient { get; set; }
        public List<WarehouseAvailabilityResult> Shortages { get; set; }

        public string ShortageMessage
        {
            get
            {
                if (IsSufficient) return "Всех продуктов достаточно.";

                var sb = new StringBuilder();
                sb.AppendLine("Недостаточно продуктов на складе:\n");
                foreach (var s in Shortages)
                {
                    sb.AppendLine($"• {s.ShortageText}");
                }
                return sb.ToString();
            }
        }
    }
}
