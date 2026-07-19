using System;

namespace house_management.Models
{
    /// <summary>
    /// Represents a house row in the <c>Houses</c> table.
    /// Mirrors the existing SQL schema:
    ///   id (INT), name (NVARCHAR(100)), address (NVARCHAR(255)),
    ///   status (NVARCHAR(50), 'Available' | 'Rented'), created_at (DATETIME).
    /// </summary>
    public class House
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public HouseStatus Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
