using System;
using System.Collections.Generic;
using System.Net.Security;

namespace Sopra.Entities
{
    public class Users
    {
        public long ID { get; set; }
        public long? RefID { get; set; }
        public long? RoleID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public int[]? CompanyID { get; set; }
        public string? RoleName { get; set; }
    }
}