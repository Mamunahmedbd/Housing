using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using house_management.Models;

namespace house_management.Services
{
    /// <summary>
    /// Result of an operation that may succeed or fail, with an informative message.
    /// </summary>
    public class UserResult
    {
        public bool Success { get; }
        public string Message { get; }
        public User User { get; }

        private UserResult(bool success, string message, User user)
        {
            Success = success;
            Message = message ?? string.Empty;
            User = user;
        }

        public static UserResult Ok(User user, string message = "") => new UserResult(true, message, user);
        public static UserResult Fail(string message) => new UserResult(false, message, null);
    }

    /// <summary>
    /// Provides all user-related business operations: authentication, CRUD,
    /// password management and validation. Acts as the only gateway between
    /// the UI and the Users table.
    /// </summary>
    public static class UserService
    {
        // Validation rules — single source of truth.
        private const int MinUsernameLength = 3;
        private const int MaxUsernameLength = 50;
        private const int MinPasswordLength = 4;
        private const int MaxPasswordLength = 128;
        private const int MaxEmailLength = 100;

        public static UserResult Authenticate(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                return UserResult.Fail("Username and password are required.");

            User user = GetByLogin(login);
            if (user == null)
                return UserResult.Fail("Invalid username or password.");

            if (user.Status == UserStatus.Locked)
                return UserResult.Fail("This account is locked. Contact an administrator.");

            bool isVerified = false;
            if (PasswordHasher.NeedsMigration(user.PasswordHash))
            {
                isVerified = (password == user.PasswordHash);
            }
            else
            {
                isVerified = PasswordHasher.Verify(password, user.PasswordHash);
            }

            if (!isVerified)
                return UserResult.Fail("Invalid username or password.");

            // Transparent migration for legacy plaintext passwords.
            if (PasswordHasher.NeedsMigration(user.PasswordHash))
            {
                UpdatePasswordInternal(user.Id, password);
                user = GetById(user.Id);
            }

            TouchLastLogin(user.Id);
            user.LastLogin = DateTime.Now;

            return UserResult.Ok(user, "Login successful.");
        }

        public static List<User> GetAll(string searchKeyword = "")
        {
            var list = new List<User>();

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_GetUsers";
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

        public static User GetById(int id)
        {
            User user = null;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "SELECT TOP 1 * FROM [Users] WHERE [id] = @id";
                cmd.Parameters.AddWithValue("@id", id);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = MapReader(reader);
                    }
                }
            });

            return user;
        }

        public static User GetByLogin(string login)
        {
            User user = null;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "SELECT TOP 1 * FROM [Users] WHERE [username] = @login OR [email] = @login";
                cmd.Parameters.AddWithValue("@login", login.Trim());

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = MapReader(reader);
                    }
                }
            });

            return user;
        }

        public static UserResult Create(User user, string password)
        {
            if (user == null)
                return UserResult.Fail("User data is missing.");

            string usernameError = ValidateUsername(user.Username);
            if (usernameError != null) return UserResult.Fail(usernameError);

            string emailError = ValidateEmail(user.Email);
            if (emailError != null) return UserResult.Fail(emailError);

            string passwordError = ValidatePassword(password);
            if (passwordError != null) return UserResult.Fail(passwordError);

            if (UsernameExists(user.Username))
                return UserResult.Fail("This username is already taken.");

            if (EmailExists(user.Email))
                return UserResult.Fail("This email is already registered.");

            string hash = PasswordHasher.Hash(password);
            int newId = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_CreateUser";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@username", user.Username.Trim());
                cmd.Parameters.AddWithValue("@email", user.Email.Trim().ToLowerInvariant());
                cmd.Parameters.AddWithValue("@passwordHash", hash);
                AddNullable(cmd, "@fullName", user.FullName);
                AddNullable(cmd, "@phone", user.Phone);
                cmd.Parameters.AddWithValue("@role", (int)user.Role);
                cmd.Parameters.AddWithValue("@status", (int)user.Status);

                newId = Convert.ToInt32(cmd.ExecuteScalar());
            });

            User created = GetById(newId);
            return UserResult.Ok(created, "User created successfully.");
        }

        public static UserResult Update(User user)
        {
            if (user == null)
                return UserResult.Fail("User data is missing.");

            User existing = GetById(user.Id);
            if (existing == null)
                return UserResult.Fail("User not found.");

            string usernameError = ValidateUsername(user.Username);
            if (usernameError != null) return UserResult.Fail(usernameError);

            string emailError = ValidateEmail(user.Email);
            if (emailError != null) return UserResult.Fail(emailError);

            if (UsernameExists(user.Username, user.Id))
                return UserResult.Fail("This username is already taken.");

            if (EmailExists(user.Email, user.Id))
                return UserResult.Fail("This email is already registered.");

            int affected = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_UpdateUser";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id", user.Id);
                cmd.Parameters.AddWithValue("@username", user.Username.Trim());
                cmd.Parameters.AddWithValue("@email", user.Email.Trim().ToLowerInvariant());
                AddNullable(cmd, "@fullName", user.FullName);
                AddNullable(cmd, "@phone", user.Phone);
                cmd.Parameters.AddWithValue("@role", (int)user.Role);
                cmd.Parameters.AddWithValue("@status", (int)user.Status);

                affected = cmd.ExecuteNonQuery();
            });

            if (affected == 0)
                return UserResult.Fail("No changes were made.");

            User updated = GetById(user.Id);
            UserSession.Refresh(updated);
            return UserResult.Ok(updated, "User updated successfully.");
        }

        public static UserResult ChangePassword(int userId, string currentPassword, string newPassword)
        {
            User user = GetById(userId);
            if (user == null)
                return UserResult.Fail("User not found.");

            if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
                return UserResult.Fail("Current password is incorrect.");

            string passwordError = ValidatePassword(newPassword);
            if (passwordError != null) return UserResult.Fail(passwordError);

            UpdatePasswordInternal(userId, newPassword);
            return UserResult.Ok(user, "Password changed successfully.");
        }

        public static UserResult ResetPassword(int userId, string newPassword)
        {
            User user = GetById(userId);
            if (user == null)
                return UserResult.Fail("User not found.");

            string passwordError = ValidatePassword(newPassword);
            if (passwordError != null) return UserResult.Fail(passwordError);

            UpdatePasswordInternal(userId, newPassword);
            return UserResult.Ok(user, "Password has been reset.");
        }

        public static UserResult Delete(int userId)
        {
            if (UserSession.IsAuthenticated && UserSession.CurrentUser.Id == userId)
                return UserResult.Fail("You cannot delete your own account while logged in.");

            User user = GetById(userId);
            if (user == null)
                return UserResult.Fail("User not found.");

            int admins = CountByRole(UserRole.Admin);
            if (user.Role == UserRole.Admin && admins <= 1)
                return UserResult.Fail("Cannot delete the last remaining administrator.");

            int affected = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "DELETE FROM [Users] WHERE [id] = @id";
                cmd.Parameters.AddWithValue("@id", userId);
                affected = cmd.ExecuteNonQuery();
            });

            return affected > 0
                ? UserResult.Ok(null, "User deleted successfully.")
                : UserResult.Fail("Failed to delete user.");
        }

        public static int CountByRole(UserRole role)
        {
            int count = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "SELECT COUNT(1) FROM [Users] WHERE [role] = @role";
                cmd.Parameters.AddWithValue("@role", (int)role);
                count = Convert.ToInt32(cmd.ExecuteScalar());
            });

            return count;
        }

        // ---- Validation helpers -------------------------------------------------

        public static string ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return "Username is required.";

            string trimmed = username.Trim();
            if (trimmed.Length < MinUsernameLength || trimmed.Length > MaxUsernameLength)
                return $"Username must be between {MinUsernameLength} and {MaxUsernameLength} characters.";

            foreach (char c in trimmed)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '.' && c != '-')
                    return "Username may only contain letters, numbers, '_', '.' or '-'.";
            }

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

        public static string ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "Password is required.";

            if (password.Length < MinPasswordLength || password.Length > MaxPasswordLength)
                return $"Password must be between {MinPasswordLength} and {MaxPasswordLength} characters.";

            return null;
        }

        // ---- Persistence helpers ------------------------------------------------

        private static bool UsernameExists(string username, int excludeId = 0)
        {
            int count = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = excludeId > 0
                    ? "SELECT COUNT(1) FROM [Users] WHERE [username] = @u AND [id] <> @id"
                    : "SELECT COUNT(1) FROM [Users] WHERE [username] = @u";

                cmd.Parameters.AddWithValue("@u", username.Trim());
                if (excludeId > 0) cmd.Parameters.AddWithValue("@id", excludeId);

                count = Convert.ToInt32(cmd.ExecuteScalar());
            });

            return count > 0;
        }

        private static bool EmailExists(string email, int excludeId = 0)
        {
            int count = 0;

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = excludeId > 0
                    ? "SELECT COUNT(1) FROM [Users] WHERE [email] = @e AND [id] <> @id"
                    : "SELECT COUNT(1) FROM [Users] WHERE [email] = @e";

                cmd.Parameters.AddWithValue("@e", email.Trim().ToLowerInvariant());
                if (excludeId > 0) cmd.Parameters.AddWithValue("@id", excludeId);

                count = Convert.ToInt32(cmd.ExecuteScalar());
            });

            return count > 0;
        }

        private static void UpdatePasswordInternal(int userId, string newPassword)
        {
            string hash = PasswordHasher.Hash(newPassword);

            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_ChangeUserPassword";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id", userId);
                cmd.Parameters.AddWithValue("@passwordHash", hash);

                cmd.ExecuteNonQuery();
            });
        }

        private static void TouchLastLogin(int userId)
        {
            DatabaseHelper.ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "UPDATE [Users] SET [last_login] = GETDATE() WHERE [id] = @id";
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            });
        }

        private static User MapReader(SqlDataReader reader)
        {
            return new User
            {
                Id = Convert.ToInt32(reader["id"]),
                Username = reader["username"] as string ?? string.Empty,
                Email = reader["email"] as string ?? string.Empty,
                PasswordHash = reader["password_hash"] as string ?? string.Empty,
                FullName = TryGetString(reader, "full_name"),
                Phone = TryGetString(reader, "phone"),
                Role = TryGetEnum<UserRole>(reader, "role", UserRole.User),
                Status = TryGetEnum<UserStatus>(reader, "status", UserStatus.Active),
                CreatedAt = TryGetDateTime(reader, "created_at"),
                UpdatedAt = TryGetDateTime(reader, "updated_at"),
                LastLogin = TryGetDateTime(reader, "last_login")
            };
        }

        private static string TryGetString(SqlDataReader reader, string column)
        {
            if (!ColumnExists(reader, column)) return null;
            object value = reader[column];
            return value == DBNull.Value ? null : value as string;
        }

        private static DateTime? TryGetDateTime(SqlDataReader reader, string column)
        {
            if (!ColumnExists(reader, column)) return null;
            object value = reader[column];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }

        private static T TryGetEnum<T>(SqlDataReader reader, string column, T defaultValue) where T : struct
        {
            if (!ColumnExists(reader, column)) return defaultValue;
            object value = reader[column];
            if (value == DBNull.Value) return defaultValue;

            int parsed = Convert.ToInt32(value);
            return Enum.IsDefined(typeof(T), parsed) ? (T)(object)parsed : defaultValue;
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

        private static void AddNullable(SqlCommand cmd, string name, string value)
        {
            cmd.Parameters.AddWithValue(name, string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim());
        }
    }
}
