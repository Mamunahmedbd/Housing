namespace house_management.Models
{
    /// <summary>
    /// Lifecycle status of a house. Stored as NVARCHAR in the database
    /// (values 'Available' / 'Rented') and converted to/from this enum by
    /// <see cref="Services.HouseService"/>. The integer values are unused
    /// on the database side — they only give the enum a stable ordinal.
    /// </summary>
    public enum HouseStatus
    {
        Available = 0,
        Rented = 1
    }
}
