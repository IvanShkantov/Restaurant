using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant
{
    public class UserSession
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; }
        public string PositionName { get; set; }
        public List<string> Permissions { get; set; }

        public bool HasPermission(string permission)
        {
            return Permissions.Contains(permission);
        }
    }
}
