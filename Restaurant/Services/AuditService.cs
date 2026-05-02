
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Windows.Input;
using System.Xml;

namespace Restaurant.Services
{
    public static class AuditService
    {
        public static void LogCreate(int employeeId, string entityName, int entityId, string description)
        {
            SaveLog(employeeId, "Добавление", entityName, entityId, description, "");
        }

        public static void LogUpdate<T>(int employeeId, T oldEntity, T newEntity, string entityName, int entityId, string description)
        {
            var changes = GetChanges(oldEntity, newEntity);

            if (changes.Any())
            {
                SaveLog(employeeId, "Редактирование", entityName, entityId, description, string.Join("\n", changes));
            }
        }

        public static void LogDelete(int employeeId, string entityName, int entityId, string description)
        {
            SaveLog(employeeId, "Удаление", entityName, entityId, description, "");
        }

        private static void SaveLog(int employeeId, string eventType, string entityName,
                                    int entityId, string description, string details)
        {
            try
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        using (var context = new RestaurantEntities())
                        {
                            var log = new ActivityLog
                            {
                                EventDate = DateTime.Now,
                                EmployeeID = employeeId,
                                EventType = eventType,
                                EntityName = entityName,
                                EntityID = entityId,
                                Description = description,
                                Details = details
                            };

                            context.ActivityLog.Add(log);
                            context.SaveChanges();
                        }

                        System.Diagnostics.Debug.WriteLine($"\n\n[AUDIT] {eventType}: {entityName} #{entityId} ({description}): {details}\n\n");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AUDIT ERROR] {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUDIT ERROR] {ex.Message}");
            }
        }

        public static bool HasChanged<T>(T oldEntity, T newEntity)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string));

            foreach (var prop in properties)
            {
                var oldValue = prop.GetValue(oldEntity);
                var newValue = prop.GetValue(newEntity);

                if (!Equals(oldValue, newValue))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<string> GetChanges<T>(T oldEntity, T newEntity)
        {
            var changes = new List<string>();

            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.CanWrite).Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string) ||
                           p.PropertyType == typeof(decimal) || p.PropertyType == typeof(DateTime));

            foreach (var prop in properties)
            {
                var oldValue = prop.GetValue(oldEntity);
                var newValue = prop.GetValue(newEntity);

                if (prop.Name.Contains("ID"))
                {
                    oldValue = GetRealName(prop.Name, (int)oldValue);
                    newValue = GetRealName(prop.Name, (int)newValue);
                }

                if (oldValue != null && !Equals(oldValue, newValue))
                {
                    changes.Add($"{prop.Name}: {oldValue?.ToString()} -> {newValue?.ToString()}");
                }
            }

            return changes;
        }

        public static string GetRealName(string field, int Id)
        {
            using (var context = new RestaurantEntities()) {
                switch (field)
                {
                    case "CategoryID":
                        return context.ProdCategories.Find(Id).CategoryName;
                    case "DishID":
                        return context.Dishes.Find(Id).Name;
                }
            }

            return "";
        }

        public static Func<PurchaseItems, int> PurchaseItemKey = item => item.ProductID;
        public static Func<OrderItems, int> OrderItemKey = item => item.DishID;
        public static Func<DishProducts, int> DishProductKey = item => item.ProductID;

        public static Func<Purchases, string> PurchaseDescr = item => PurchaseID(item.PurchaseID);
        public static Func<PurchaseItems, string> PurchaseItemDescr = item => $"{PurchaseID(item.PurchaseID)} ({ProductName(item.ProductID)})";

        public static Func<OrderItems, string> OrderDescr = item => OrderID(item.OrderID);
        public static Func<OrderItems, string> OrderItemDescr = item => $"{OrderID(item.OrderID)} ({DishName(item.DishID)})";
        public static Func<DishProducts, string> DishProductDescr = item => DishProductID(item.DishID, item.ProductID);

        public static string ProductName(int prodID)
        {
            using (var context = new RestaurantEntities())
                return context.Products.Find(prodID).Name;
        }
        public static string DishName(int dishID)
        {
            using (var context = new RestaurantEntities())
                return context.Dishes.Find(dishID).Name;
        }
        public static string EmployeeFullName(int employeeID)
        {
            using (var context = new RestaurantEntities())
                return context.Database
                            .SqlQuery<string>("SELECT dbo.GetEmployeeFullName(@p0)", employeeID)
                            .FirstOrDefault();
        }

        public static string PurchaseID(int purchaseID)
        {
            using (var context = new RestaurantEntities())
                return context.Database
                            .SqlQuery<string>("SELECT dbo.GetPurchaseIdentifier(@p0)", purchaseID)
                            .FirstOrDefault();
        }

        public static string OrderID(int orderID)
        {
            using (var context = new RestaurantEntities())
                return context.Database
                            .SqlQuery<string>("SELECT dbo.GetOrderIdentifier(@p0)", orderID)
                            .FirstOrDefault();
        }

        public static string DishProductID(int dishID, int productID)
        {
            using (var context = new RestaurantEntities())
                return context.Database
                            .SqlQuery<string>("SELECT dbo.GetDishProductIdentifier(@p0, @p1)", dishID, productID)
                            .FirstOrDefault();
        }

        public static void GetPurchaseItemsChanges(
            int userID,
            List<PurchaseItems> oldItems,
            List<PurchaseItems> newItems)
        {
            GetChanges("Позиции закупки", userID, oldItems, newItems, PurchaseItemKey, PurchaseItemDescr);
        }

        public static void GetOrderItemsChanges(
            int userID,
            List<OrderItems> oldItems,
            List<OrderItems> newItems)
        {
            GetChanges("Позиции заказа", userID, oldItems, newItems, OrderItemKey, OrderItemDescr);
        }

        public static void GetDishProductsChanges(
            int userID,
            List<DishProducts> oldItems,
            List<DishProducts> newItems)
        {
            GetChanges("Ингредиенты", userID, oldItems, newItems, DishProductKey, DishProductDescr);
        }

        public static void GetChanges<T>(
            string entityName,
            int userID,
            List<T> oldItems,
            List<T> newItems,
            Func<T, int> keySelector,
            Func<T, string> descrSelector)
        {

            if (oldItems == null) oldItems = new List<T>();
            if (newItems == null) newItems = new List<T>();

            var oldDict = oldItems.ToDictionary(keySelector);
            var newDict = newItems.ToDictionary(keySelector);

            List<T> Added = newItems
                .Where(n => !oldDict.ContainsKey(keySelector(n)))
                .ToList();

            foreach (var item in Added) {
                LogCreate(userID, entityName, keySelector(item), descrSelector(item));
            }

            List<T> Removed = oldItems
                .Where(o => !newDict.ContainsKey(keySelector(o)))
                .ToList();

            foreach (var item in Removed)
            {
                LogDelete(userID, entityName, keySelector(item), descrSelector(item));
            }

            var commonKeys = oldDict.Keys.Intersect(newDict.Keys);

            foreach (var key in commonKeys)
            {
                var oldItem = oldDict[key];
                var newItem = newDict[key];

                LogUpdate(userID, oldItem, newItem, entityName, key, descrSelector(newItem));
            }
        }
    }
}