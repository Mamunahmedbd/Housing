using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using house_management.Models;

namespace house_management.Services
{
    /// <summary>
    /// Result of a house operation that may succeed or fail, with an
    /// informative message and the affected entity (when applicable).
    /// Mirrors the <see cref="UserResult"/> / <see cref="TenantResult"/> pattern.
    /// </summary>
    public class HouseResult
    {
        public bool Success { get; }
        public string Message { get; }
        public House House { get; }

        private HouseResult(bool success, string message, House house)
        {
            Success = success;
            Message = message ?? string.Empty;
            House = house;
        }

        public static HouseResult Ok(House house, string message = "") => new HouseResult(true, message, house);
        public static HouseResult Fail(string message) => new HouseResult(false, message, null);
    }

    /// <summary>
    /// Provides all house-related business operations: CRUD, validation and
    /// referential-integrity checks. Acts as the only gateway between the UI
    /// and the <c>Houses</c> table.
    /// </summary>
    public static class HouseService
    {
        // Validation rules — single source of truth.
        private const int MinNameLength = 2;
        private const int MaxNameLength = 100;
        private const int MaxAddressLength = 255;

        /// <summary>
        /// Lists houses, optionally filtered by a keyword that matches name or address.
        /// </summary>
        public static List<House> GetAll(string searchKeyword = "")
        {
            var list = new List<House>();

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_GetHouses";
                cmd.CommandType = CommandType.StoredProcedure;

                string keyword = string.IsNullOrWhiteSpace(searchKeyword) ? null : searchKeyword.Trim();
                cmd.Parameters.Add(new SqlParameter("@searchKeyword", SqlDbType.NVarChar, 100)
                {
                    Value = (object)keyword ?? DBNull.Value
                });

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapReader(reader));
                    }
                }
            });

            return list;
        }

        public static House GetById(int id)
        {
            House house = null;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "SELECT TOP 1 * FROM [Houses] WHERE [id] = @id";
                cmd.Parameters.AddWithValue("@id", id);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        house = MapReader(reader);
                    }
                }
            });

            return house;
        }

        public static HouseResult Create(House house)
        {
            if (house == null)
                return HouseResult.Fail("House data is missing.");

            string nameError = ValidateName(house.Name);
            if (nameError != null) return HouseResult.Fail(nameError);

            string addressError = ValidateAddress(house.Address);
            if (addressError != null) return HouseResult.Fail(addressError);

            int newId = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_AddHouse";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@name", house.Name.Trim());
                cmd.Parameters.AddWithValue("@address", house.Address.Trim());
                cmd.Parameters.AddWithValue("@status", StatusToDb(house.Status));

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    newId = Convert.ToInt32(result);
                }
            });

            House created = newId > 0 ? GetById(newId) : null;
            return HouseResult.Ok(created, "House created successfully.");
        }

        public static HouseResult Update(House house)
        {
            if (house == null)
                return HouseResult.Fail("House data is missing.");

            House existing = GetById(house.Id);
            if (existing == null)
                return HouseResult.Fail("House not found.");

            string nameError = ValidateName(house.Name);
            if (nameError != null) return HouseResult.Fail(nameError);

            string addressError = ValidateAddress(house.Address);
            if (addressError != null) return HouseResult.Fail(addressError);

            int affected = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_UpdateHouse";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id", house.Id);
                cmd.Parameters.AddWithValue("@name", house.Name.Trim());
                cmd.Parameters.AddWithValue("@address", house.Address.Trim());
                cmd.Parameters.AddWithValue("@status", StatusToDb(house.Status));

                affected = cmd.ExecuteNonQuery();
            });

            if (affected == 0)
                return HouseResult.Fail("No changes were made.");

            House updated = GetById(house.Id);
            return HouseResult.Ok(updated, "House updated successfully.");
        }

        /// <summary>
        /// Deletes a house. Blocked if the house is referenced by any rental
        /// (active or completed) — the caller must remove those rentals first.
        /// This adds an explicit business check on top of the database's
        /// ON DELETE CASCADE rule so data loss is never silent.
        /// </summary>
        public static HouseResult Delete(int houseId)
        {
            House house = GetById(houseId);
            if (house == null)
                return HouseResult.Fail("House not found.");

            int rentals = CountRentalsForHouse(houseId);
            if (rentals > 0)
            {
                return HouseResult.Fail(
                    $"Cannot delete '{house.Name}' because {rentals} rental(s) " +
                    "reference this house. Remove those rentals first.");
            }

            int affected = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_DeleteHouse";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id", houseId);
                affected = cmd.ExecuteNonQuery();
            });

            return affected > 0
                ? HouseResult.Ok(null, "House deleted successfully.")
                : HouseResult.Fail("Failed to delete house.");
        }

        /// <summary>
        /// Returns the number of rentals linked to a house (any status).
        /// Used by <see cref="Delete"/> to enforce the referential-integrity rule.
        /// </summary>
        public static int CountRentalsForHouse(int houseId)
        {
            int count = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "SELECT COUNT(1) FROM [Rentals] WHERE [house_id] = @id";
                cmd.Parameters.AddWithValue("@id", houseId);
                count = Convert.ToInt32(cmd.ExecuteScalar());
            });

            return count;
        }

        // ---- Validation helpers -------------------------------------------------

        public static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "House name is required.";

            string trimmed = name.Trim();
            if (trimmed.Length < MinNameLength || trimmed.Length > MaxNameLength)
                return $"Name must be between {MinNameLength} and {MaxNameLength} characters.";

            return null;
        }

        public static string ValidateAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return "Address is required.";

            string trimmed = address.Trim();
            if (trimmed.Length > MaxAddressLength)
                return $"Address must not exceed {MaxAddressLength} characters.";

            return null;
        }

        // ---- Conversion helpers (DB stores status as NVARCHAR) -----------------

        /// <summary>
        /// Returns the database string representation of a status value.
        /// </summary>
        public static string StatusToDb(HouseStatus status)
        {
            return status == HouseStatus.Rented ? "Rented" : "Available";
        }

        /// <summary>
        /// Parses a database status string into a <see cref="HouseStatus"/> enum.
        /// Unknown values default to <see cref="HouseStatus.Available"/>.
        /// </summary>
        public static HouseStatus StatusFromDb(object value)
        {
            if (value == null || value == DBNull.Value) return HouseStatus.Available;
            string raw = Convert.ToString(value);
            return string.Equals(raw, "Rented", StringComparison.OrdinalIgnoreCase)
                ? HouseStatus.Rented
                : HouseStatus.Available;
        }

        // ---- Persistence helpers ------------------------------------------------

        private static House MapReader(SqlDataReader reader)
        {
            return new House
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["name"] as string ?? string.Empty,
                Address = reader["address"] as string ?? string.Empty,
                Status = StatusFromDb(reader["status"]),
                CreatedAt = TryGetDateTime(reader, "created_at")
            };
        }

        private static DateTime? TryGetDateTime(SqlDataReader reader, string column)
        {
            if (!ColumnExists(reader, column)) return null;
            object value = reader[column];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }

        private static bool ColumnExists(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
