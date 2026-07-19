using System;

namespace house_management.Models
{
    /// <summary>
    /// Represents a tenant row in the <c>Tenants</c> table.
    /// Mirrors the existing SQL schema exactly:
    ///   id (INT), name (NVARCHAR(100)), email (NVARCHAR(100)),
    ///   phone (NVARCHAR(50)), created_at (DATETIME).
    /// </summary>
    public class Tenant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
