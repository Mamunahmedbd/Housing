using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace house_management
{
    public static class DatabaseHelper
    {
        private static readonly string connectionString;
        private static readonly string masterConnectionString;

        static DatabaseHelper()
        {
            // Retrieve connection string from App.config or fall back to localdb default
            var connSetting = ConfigurationManager.ConnectionStrings["HousingRental"] ?? ConfigurationManager.ConnectionStrings["HousingDb"];
            if (connSetting != null)
            {
                connectionString = connSetting.ConnectionString;
            }
            else
            {
                connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=HousingRental;Integrated Security=True;";
            }

            // Construct connection string for master database to handle database creation
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            };
            masterConnectionString = builder.ConnectionString;
        }

        /// <summary>
        /// Exposes the configured connection string for diagnostic purposes only.
        /// </summary>
        public static string ConnectionString => connectionString;

        /// <summary>
        /// Runs the supplied action inside a freshly opened connection and
        /// guarantees that the connection is disposed afterwards. Centralises
        /// error handling for the whole data layer.
        /// </summary>
        public static void ExecuteDbCommand(Action<SqlCommand> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    action(cmd);
                }
            }
        }

        /// <summary>
        /// Automatically checks for Database presence and creates schema +
        /// stored procedures + seed data if missing, then runs migrations.
        /// </summary>
        public static void InitializeDatabase()
        {
            EnsureDatabaseExists();
            EnsureSchemaExists();
            MigrateUsersTable();
            EnsureStoredProceduresExist();
            EnsureSeedAdmin();
        }

        private static void EnsureDatabaseExists()
        {
            using (SqlConnection masterConn = new SqlConnection(masterConnectionString))
            {
                masterConn.Open();
                string checkDbQuery = "SELECT database_id FROM sys.databases WHERE name = 'HousingRental'";
                using (SqlCommand checkCmd = new SqlCommand(checkDbQuery, masterConn))
                {
                    object result = checkCmd.ExecuteScalar();
                    if (result == null)
                    {
                        using (SqlCommand createCmd = new SqlCommand("CREATE DATABASE HousingRental", masterConn))
                        {
                            createCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private static void EnsureSchemaExists()
        {
            string createUsersTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                BEGIN
                    CREATE TABLE Users (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        username NVARCHAR(50) NOT NULL UNIQUE,
                        email NVARCHAR(100) NOT NULL UNIQUE,
                        password_hash NVARCHAR(255) NOT NULL,
                        full_name NVARCHAR(100) NULL,
                        phone NVARCHAR(30) NULL,
                        role INT NOT NULL DEFAULT 2,
                        status INT NOT NULL DEFAULT 0,
                        created_at DATETIME DEFAULT GETDATE() NULL,
                        updated_at DATETIME NULL,
                        last_login DATETIME NULL
                    );
                END";

            string createHousesTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Houses')
                BEGIN
                    CREATE TABLE Houses (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        name NVARCHAR(100) NOT NULL,
                        address NVARCHAR(255) NOT NULL,
                        status NVARCHAR(50) DEFAULT 'Available' NOT NULL CHECK (status IN ('Available', 'Rented')),
                        created_at DATETIME DEFAULT GETDATE() NULL
                    );

                    INSERT INTO Houses (name, address, status)
                    VALUES
                    (N'Green Villa', N'Downtown St 10', N'Available'),
                    (N'Sunset Apartment', N'Beach Road Block 5', N'Rented'),
                    (N'Royal Palace', N'Al-Mansour District', N'Available');
                END";

            string createTenantsTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tenants')
                BEGIN
                    CREATE TABLE Tenants (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        name NVARCHAR(100) NOT NULL,
                        email NVARCHAR(100) NOT NULL UNIQUE,
                        phone NVARCHAR(50) NOT NULL,
                        created_at DATETIME DEFAULT GETDATE() NULL
                    );

                    INSERT INTO Tenants (name, email, phone)
                    VALUES 
                    (N'John Doe', N'john.doe@email.com', N'+1-555-0199'),
                    (N'Jane Smith', N'jane.smith@email.com', N'+1-555-0144'),
                    (N'Michael Brown', N'michael.b@email.com', N'+1-555-0177');
                END";

            string createRentalsTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Rentals')
                BEGIN
                    CREATE TABLE Rentals (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        house_id INT NOT NULL FOREIGN KEY REFERENCES Houses(id) ON DELETE CASCADE,
                        tenant_id INT NOT NULL FOREIGN KEY REFERENCES Tenants(id) ON DELETE CASCADE,
                        rent_amount DECIMAL(18,2) NOT NULL,
                        start_date DATETIME NOT NULL,
                        end_date DATETIME NOT NULL,
                        status NVARCHAR(50) DEFAULT 'Active' NOT NULL CHECK (status IN ('Active', 'Completed')),
                        created_at DATETIME DEFAULT GETDATE() NULL
                    );

                    INSERT INTO Rentals (house_id, tenant_id, rent_amount, start_date, end_date, status)
                    VALUES 
                    (2, 1, 1500.00, '2026-01-01', '2026-12-31', N'Active');
                END";

            ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = createUsersTable;
                cmd.ExecuteNonQuery();
            });

            ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = createHousesTable;
                cmd.ExecuteNonQuery();
            });

            ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = createTenantsTable;
                cmd.ExecuteNonQuery();
            });

            ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = createRentalsTable;
                cmd.ExecuteNonQuery();
            });
        }

        /// <summary>
        /// Adds any columns that were introduced after the original schema
        /// to existing installations. Safe to run repeatedly.
        /// </summary>
        private static void MigrateUsersTable()
        {
            string[] migrations =
            {
                "IF COL_LENGTH('Users', 'full_name') IS NULL ALTER TABLE Users ADD full_name NVARCHAR(100) NULL",
                "IF COL_LENGTH('Users', 'phone') IS NULL ALTER TABLE Users ADD phone NVARCHAR(30) NULL",
                "IF COL_LENGTH('Users', 'role') IS NULL ALTER TABLE Users ADD role INT NOT NULL DEFAULT 2",
                "IF COL_LENGTH('Users', 'status') IS NULL ALTER TABLE Users ADD status INT NOT NULL DEFAULT 0",
                "IF COL_LENGTH('Users', 'updated_at') IS NULL ALTER TABLE Users ADD updated_at DATETIME NULL",
                "IF COL_LENGTH('Users', 'last_login') IS NULL ALTER TABLE Users ADD last_login DATETIME NULL"
            };

            foreach (string migration in migrations)
            {
                ExecuteDbCommand(cmd =>
                {
                    cmd.CommandText = migration;
                    cmd.ExecuteNonQuery();
                });
            }
        }

        /// <summary>
        /// Promotes the seeded 'admin' account to Admin role and Active status.
        /// Password hashing is handled transparently by UserService.Authenticate
        /// on the next successful login.
        /// </summary>
        private static void EnsureSeedAdmin()
        {
            ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT 1 FROM Users WHERE username = N'admin')
                    BEGIN
                        INSERT INTO Users (username, email, password_hash, role, status)
                        VALUES (N'admin', N'admin@housingapp.com', N'1234', 0, 0);
                    END

                    UPDATE Users
                       SET role = 0,
                           status = 0
                     WHERE username = N'admin';";
                cmd.ExecuteNonQuery();
            });
        }

        /// <summary>
        /// Validates login credentials against the Users database table securely.
        /// Delegates hashing verification to the UserService so legacy plaintext
        /// passwords can be migrated on the fly.
        /// </summary>
        public static bool ValidateUser(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                return false;

            var result = Services.UserService.Authenticate(login, password);
            return result.Success;
        }

        /// <summary>
        /// Fetches the list of houses, with an optional search term filtering by Name or Address.
        /// </summary>
        public static List<House> GetHouses(string searchKeyword = "")
        {
            List<House> list = new List<House>();

            ExecuteDbCommand(cmd =>
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
                        list.Add(new House
                        {
                            ID = reader["id"].ToString(),
                            Name = reader["name"].ToString(),
                            Address = reader["address"].ToString(),
                            Status = reader["status"].ToString()
                        });
                    }
                }
            });

            return list;
        }

        /// <summary>
        /// Inserts a new house record into the Houses database table.
        /// </summary>
        public static bool AddHouse(string name, string address, string status)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
                return false;

            int affected = 0;

            ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_AddHouse";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@name", name.Trim());
                cmd.Parameters.AddWithValue("@address", address.Trim());
                cmd.Parameters.AddWithValue("@status", status);

                affected = cmd.ExecuteNonQuery();
            });

            return affected > 0;
        }

        /// <summary>
        /// Deletes a house record from the Houses database table by its ID.
        /// </summary>
        public static bool DeleteHouse(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            int affected = 0;

            ExecuteDbCommand(cmd =>
            {
                cmd.CommandText = "sp_DeleteHouse";
                cmd.CommandType = CommandType.StoredProcedure;

                int parsedId;
                if (!int.TryParse(id, out parsedId))
                {
                    affected = 0;
                    return;
                }

                cmd.Parameters.AddWithValue("@id", parsedId);
                affected = cmd.ExecuteNonQuery();
            });

            return affected > 0;
        }

        // ---------------------------------------------------------------------
        // Stored procedure bootstrap.
        // DROP and CREATE run as separate batches because CREATE PROCEDURE
        // must be the only statement in its batch. This is idempotent and
        // stays in sync with the .sql files under /Database.
        // ---------------------------------------------------------------------

        private static void EnsureStoredProceduresExist()
        {
            foreach (var procedure in StoredProcedureDefinitions.All)
            {
                ExecuteDbCommand(cmd =>
                {
                    cmd.CommandText = "IF OBJECT_ID('" + procedure.Name + "', 'P') IS NOT NULL DROP PROCEDURE " + procedure.Name + ";";
                    cmd.ExecuteNonQuery();
                });

                ExecuteDbCommand(cmd =>
                {
                    cmd.CommandText = procedure.Body;
                    cmd.ExecuteNonQuery();
                });
            }
        }

        private static class StoredProcedureDefinitions
        {
            public static readonly (string Name, string Body)[] All =
            {
                ("[dbo].[sp_GetHouses]",            HousesGet),
                ("[dbo].[sp_AddHouse]",             HousesAdd),
                ("[dbo].[sp_DeleteHouse]",          HousesDelete),
                ("[dbo].[sp_GetUsers]",             UsersGet),
                ("[dbo].[sp_CreateUser]",           UsersCreate),
                ("[dbo].[sp_UpdateUser]",           UsersUpdate),
                ("[dbo].[sp_ChangeUserPassword]",   UsersChangePassword)
            };

            private const string HousesGet = @"
CREATE PROCEDURE [dbo].[sp_GetHouses]
    @searchKeyword NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @searchKeyword IS NULL OR LTRIM(RTRIM(@searchKeyword)) = ''
    BEGIN
        SELECT [id], [name], [address], [status]
          FROM [dbo].[Houses]
         ORDER BY [id] ASC;
    END
    ELSE
    BEGIN
        SELECT [id], [name], [address], [status]
          FROM [dbo].[Houses]
         WHERE [name]    LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
            OR [address] LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
         ORDER BY [id] ASC;
    END
END;";

            private const string HousesAdd = @"
CREATE PROCEDURE [dbo].[sp_AddHouse]
    @name    NVARCHAR(100),
    @address NVARCHAR(255),
    @status  NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Houses] ([name], [address], [status])
    VALUES (@name, @address, @status);
END;";

            private const string HousesDelete = @"
CREATE PROCEDURE [dbo].[sp_DeleteHouse]
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM [dbo].[Houses] WHERE [id] = @id;
END;";

            private const string UsersGet = @"
CREATE PROCEDURE [dbo].[sp_GetUsers]
    @searchKeyword NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @searchKeyword IS NULL OR LTRIM(RTRIM(@searchKeyword)) = ''
    BEGIN
        SELECT [id], [username], [email], [password_hash], [full_name], [phone],
               [role], [status], [created_at], [updated_at], [last_login]
          FROM [dbo].[Users]
         ORDER BY [id] ASC;
    END
    ELSE
    BEGIN
        SELECT [id], [username], [email], [password_hash], [full_name], [phone],
               [role], [status], [created_at], [updated_at], [last_login]
          FROM [dbo].[Users]
         WHERE [username]  LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
            OR [email]     LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
            OR [full_name] LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
         ORDER BY [id] ASC;
    END
END;";

            private const string UsersCreate = @"
CREATE PROCEDURE [dbo].[sp_CreateUser]
    @username     NVARCHAR(50),
    @email        NVARCHAR(100),
    @passwordHash NVARCHAR(255),
    @fullName     NVARCHAR(100) = NULL,
    @phone        NVARCHAR(30)  = NULL,
    @role         INT = 2,
    @status       INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Users]
        ([username], [email], [password_hash], [full_name], [phone], [role], [status])
    VALUES
        (@username, @email, @passwordHash, @fullName, @phone, @role, @status);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS [NewId];
END;";

            private const string UsersUpdate = @"
CREATE PROCEDURE [dbo].[sp_UpdateUser]
    @id       INT,
    @username NVARCHAR(50),
    @email    NVARCHAR(100),
    @fullName NVARCHAR(100) = NULL,
    @phone    NVARCHAR(30)  = NULL,
    @role     INT,
    @status   INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Users]
       SET [username]   = @username,
           [email]      = @email,
           [full_name]  = @fullName,
           [phone]      = @phone,
           [role]       = @role,
           [status]     = @status,
           [updated_at] = GETDATE()
     WHERE [id] = @id;
END;";

            private const string UsersChangePassword = @"
CREATE PROCEDURE [dbo].[sp_ChangeUserPassword]
    @id           INT,
    @passwordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Users]
       SET [password_hash] = @passwordHash,
           [updated_at]    = GETDATE()
     WHERE [id] = @id;
END;";
        }

        // --- NEW PROJECT MODULES DATA & CRUD SUPPORT ---

        public static List<Tenant> GetTenants(string searchKeyword = "")
        {
            List<Tenant> list = new List<Tenant>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT id, name, email, phone FROM Tenants";
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    query += " WHERE name LIKE @search OR email LIKE @search OR phone LIKE @search";
                }
                query += " ORDER BY id ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(searchKeyword))
                        cmd.Parameters.AddWithValue("@search", "%" + searchKeyword + "%");

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Tenant
                            {
                                ID = reader["id"].ToString(),
                                Name = reader["name"].ToString(),
                                Email = reader["email"].ToString(),
                                Phone = reader["phone"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        public static bool AddTenant(string name, string email, string phone)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone))
                return false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Tenants (name, email, phone) VALUES (@name, @email, @phone)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", name.Trim());
                    cmd.Parameters.AddWithValue("@email", email.Trim());
                    cmd.Parameters.AddWithValue("@phone", phone.Trim());
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public static bool DeleteTenant(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Tenants WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public static List<Rental> GetRentals(string searchKeyword = "")
        {
            List<Rental> list = new List<Rental>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT r.id, r.house_id, h.name AS house_name, r.tenant_id, t.name AS tenant_name, 
                           r.rent_amount, r.start_date, r.end_date, r.status
                    FROM Rentals r
                    INNER JOIN Houses h ON r.house_id = h.id
                    INNER JOIN Tenants t ON r.tenant_id = t.id";
                
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    query += " WHERE h.name LIKE @search OR t.name LIKE @search OR r.status LIKE @search";
                }
                query += " ORDER BY r.id ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(searchKeyword))
                        cmd.Parameters.AddWithValue("@search", "%" + searchKeyword + "%");

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Rental
                            {
                                ID = reader["id"].ToString(),
                                HouseID = reader["house_id"].ToString(),
                                HouseName = reader["house_name"].ToString(),
                                TenantID = reader["tenant_id"].ToString(),
                                TenantName = reader["tenant_name"].ToString(),
                                RentAmount = Convert.ToDecimal(reader["rent_amount"]).ToString("F2"),
                                StartDate = Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd"),
                                EndDate = Convert.ToDateTime(reader["end_date"]).ToString("yyyy-MM-dd"),
                                Status = reader["status"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        public static bool AddRental(string houseId, string tenantId, decimal rentAmount, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrEmpty(houseId) || string.IsNullOrEmpty(tenantId))
                return false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Rentals (house_id, tenant_id, rent_amount, start_date, end_date, status) VALUES (@house_id, @tenant_id, @rent, @start, @end, 'Active')";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@house_id", houseId);
                    cmd.Parameters.AddWithValue("@tenant_id", tenantId);
                    cmd.Parameters.AddWithValue("@rent", rentAmount);
                    cmd.Parameters.AddWithValue("@start", startDate);
                    cmd.Parameters.AddWithValue("@end", endDate);
                    
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        // Update House status to Rented
                        string updateQuery = "UPDATE Houses SET status = 'Rented' WHERE id = @house_id";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@house_id", houseId);
                            updateCmd.ExecuteNonQuery();
                        }
                        return true;
                    }
                    return false;
                }
            }
        }

        public static bool DeleteRental(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                string houseId = "";
                string getHouseQuery = "SELECT house_id FROM Rentals WHERE id = @id";
                using (SqlCommand getHouseCmd = new SqlCommand(getHouseQuery, conn))
                {
                    getHouseCmd.Parameters.AddWithValue("@id", id);
                    object res = getHouseCmd.ExecuteScalar();
                    if (res != null) houseId = res.ToString();
                }

                string query = "DELETE FROM Rentals WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0 && !string.IsNullOrEmpty(houseId))
                    {
                        string updateQuery = "UPDATE Houses SET status = 'Available' WHERE id = @house_id";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@house_id", houseId);
                            updateCmd.ExecuteNonQuery();
                        }
                        return true;
                    }
                    return rows > 0;
                }
            }
        }

        public static List<UserRecord> GetUsers(string searchKeyword = "")
        {
            List<UserRecord> list = new List<UserRecord>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT id, username, email, created_at FROM Users";
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    query += " WHERE username LIKE @search OR email LIKE @search";
                }
                query += " ORDER BY id ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(searchKeyword))
                        cmd.Parameters.AddWithValue("@search", "%" + searchKeyword + "%");

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new UserRecord
                            {
                                ID = reader["id"].ToString(),
                                Username = reader["username"].ToString(),
                                Email = reader["email"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["created_at"]).ToString("yyyy-MM-dd HH:mm")
                            });
                        }
                    }
                }
            }
            return list;
        }

        public static bool AddUser(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Users (username, email, password_hash) VALUES (@username, @email, @pass)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username.Trim());
                    cmd.Parameters.AddWithValue("@email", email.Trim());
                    cmd.Parameters.AddWithValue("@pass", password);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public static bool DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Users WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }

    // --- DATA MODULE RECORD CLASSES ---

    public class Tenant
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class Rental
    {
        public string ID { get; set; }
        public string HouseID { get; set; }
        public string HouseName { get; set; }
        public string TenantID { get; set; }
        public string TenantName { get; set; }
        public string RentAmount { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Status { get; set; }
    }

    public class UserRecord
    {
        public string ID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string CreatedAt { get; set; }
    }
}
