using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using house_management.Models;

namespace house_management.Services
{
    /// <summary>
    /// Result of a tenant operation that may succeed or fail, with an
    /// informative message and the affected entity (when applicable).
    /// Mirrors the <see cref="UserResult"/> pattern used by UserService.
    /// </summary>
    public class TenantResult
    {
        public bool Success { get; }
        public string Message { get; }
        public Tenant Tenant { get; }

        private TenantResult(bool success, string message, Tenant tenant)
        {
            Success = success;
            Message = message ?? string.Empty;
            Tenant = tenant;
        }

        public static TenantResult Ok(Tenant tenant, string message = "") => new TenantResult(true, message, tenant);
        public static TenantResult Fail(string message) => new TenantResult(false, message, null);
    }

    /// <summary>
    /// Provides all tenant-related business operations: CRUD, validation and
    /// referential-integrity checks. Acts as the only gateway between the UI
    /// and the <c>Tenants</c> table.
    /// </summary>
    public static class TenantService
    {
        // Validation rules — single source of truth.
        private const int MinNameLength = 2;
        private const int MaxNameLength = 100;
        private const int MaxEmailLength = 100;
        private const int MinPhoneLength = 4;
        private const int MaxPhoneLength = 50;

        /// <summary>
        /// Lists tenants, optionally filtered by a keyword that matches name,
        /// email or phone. Ordered by id ascending.
        /// </summary>
        public static List<Tenant> GetAll(string searchKeyword = "")
        {
            var list = new List<Tenant>();

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_GetTenants";
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

        public static Tenant GetById(int id)
        {
            Tenant tenant = null;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "SELECT TOP 1 * FROM [Tenants] WHERE [id] = @id";
                cmd.Parameters.AddWithValue("@id", id);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        tenant = MapReader(reader);
                    }
                }
            });

            return tenant;
        }

        public static TenantResult Create(Tenant tenant)
        {
            if (tenant == null)
                return TenantResult.Fail("Tenant data is missing.");

            string nameError = ValidateName(tenant.Name);
            if (nameError != null) return TenantResult.Fail(nameError);

            string emailError = ValidateEmail(tenant.Email);
            if (emailError != null) return TenantResult.Fail(emailError);

            string phoneError = ValidatePhone(tenant.Phone);
            if (phoneError != null) return TenantResult.Fail(phoneError);

            if (EmailExists(tenant.Email))
                return TenantResult.Fail("This email is already registered to another tenant.");

            int newId = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_CreateTenant";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@name", tenant.Name.Trim());
                cmd.Parameters.AddWithValue("@email", tenant.Email.Trim().ToLowerInvariant());
                cmd.Parameters.AddWithValue("@phone", tenant.Phone.Trim());

                newId = Convert.ToInt32(cmd.ExecuteScalar());
            });

            Tenant created = GetById(newId);
            return TenantResult.Ok(created, "Tenant created successfully.");
        }

        public static TenantResult Update(Tenant tenant)
        {
            if (tenant == null)
                return TenantResult.Fail("Tenant data is missing.");

            Tenant existing = GetById(tenant.Id);
            if (existing == null)
                return TenantResult.Fail("Tenant not found.");

            string nameError = ValidateName(tenant.Name);
            if (nameError != null) return TenantResult.Fail(nameError);

            string emailError = ValidateEmail(tenant.Email);
            if (emailError != null) return TenantResult.Fail(emailError);

            string phoneError = ValidatePhone(tenant.Phone);
            if (phoneError != null) return TenantResult.Fail(phoneError);

            if (EmailExists(tenant.Email, tenant.Id))
                return TenantResult.Fail("This email is already registered to another tenant.");

            int affected = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_UpdateTenant";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id", tenant.Id);
                cmd.Parameters.AddWithValue("@name", tenant.Name.Trim());
                cmd.Parameters.AddWithValue("@email", tenant.Email.Trim().ToLowerInvariant());
                cmd.Parameters.AddWithValue("@phone", tenant.Phone.Trim());

                affected = cmd.ExecuteNonQuery();
            });

            if (affected == 0)
                return TenantResult.Fail("No changes were made.");

            Tenant updated = GetById(tenant.Id);
            return TenantResult.Ok(updated, "Tenant updated successfully.");
        }

        /// <summary>
        /// Deletes a tenant. Blocked if the tenant is referenced by any rental
        /// (active or completed) — the caller must remove or re-assign those
        /// rentals first. This adds an explicit business check on top of the
        /// database's ON DELETE CASCADE rule so data loss is never silent.
        /// </summary>
        public static TenantResult Delete(int tenantId)
        {
            Tenant tenant = GetById(tenantId);
            if (tenant == null)
                return TenantResult.Fail("Tenant not found.");

            int activeRentals = CountRentalsForTenant(tenantId);
            if (activeRentals > 0)
            {
                return TenantResult.Fail(
                    $"Cannot delete '{tenant.Name}' because {activeRentals} rental(s) " +
                    "reference this tenant. Remove or re-assign those rentals first.");
            }

            int affected = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_DeleteTenant";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id", tenantId);
                affected = cmd.ExecuteNonQuery();
            });

            return affected > 0
                ? TenantResult.Ok(null, "Tenant deleted successfully.")
                : TenantResult.Fail("Failed to delete tenant.");
        }

        /// <summary>
        /// Returns the number of rentals (any status) linked to a tenant.
        /// Used by <see cref="Delete"/> to enforce the referential-integrity rule.
        /// </summary>
        public static int CountRentalsForTenant(int tenantId)
        {
            int count = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "SELECT COUNT(1) FROM [Rentals] WHERE [tenant_id] = @id";
                cmd.Parameters.AddWithValue("@id", tenantId);
                count = Convert.ToInt32(cmd.ExecuteScalar());
            });

            return count;
        }

        // ---- Validation helpers -------------------------------------------------

        public static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Tenant name is required.";

            string trimmed = name.Trim();
            if (trimmed.Length < MinNameLength || trimmed.Length > MaxNameLength)
                return $"Name must be between {MinNameLength} and {MaxNameLength} characters.";

            return null;
        }

        public static string ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Email is required.";

            string trimmed = email.Trim();
            if (trimmed.Length > MaxEmailLength)
                return $"Email must not exceed {MaxEmailLength} characters.";

            try
            {
                var addr = new System.Net.Mail.MailAddress(trimmed);
                if (addr.Address != trimmed)
                    return "Email format is invalid.";
            }
            catch
            {
                return "Email format is invalid.";
            }

            return null;
        }

        public static string ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "Phone number is required.";

            string trimmed = phone.Trim();
            if (trimmed.Length < MinPhoneLength || trimmed.Length > MaxPhoneLength)
                return $"Phone must be between {MinPhoneLength} and {MaxPhoneLength} characters.";

            return null;
        }

        // ---- Persistence helpers ------------------------------------------------

        private static bool EmailExists(string email, int excludeId = 0)
        {
            int count = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = excludeId > 0
                    ? "SELECT COUNT(1) FROM [Tenants] WHERE [email] = @e AND [id] <> @id"
                    : "SELECT COUNT(1) FROM [Tenants] WHERE [email] = @e";

                cmd.Parameters.AddWithValue("@e", email.Trim().ToLowerInvariant());
                if (excludeId > 0) cmd.Parameters.AddWithValue("@id", excludeId);

                count = Convert.ToInt32(cmd.ExecuteScalar());
            });

            return count > 0;
        }

        private static Tenant MapReader(SqlDataReader reader)
        {
            return new Tenant
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["name"] as string ?? string.Empty,
                Email = reader["email"] as string ?? string.Empty,
                Phone = reader["phone"] as string ?? string.Empty,
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
