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
        /// Automatically checks for Database presence and creates schema + seed data if missing.
        /// </summary>
        public static void InitializeDatabase()
        {
            // 1. Check and Create the housing database
            using (SqlConnection masterConn = new SqlConnection(masterConnectionString))
            {
                masterConn.Open();
                string checkDbQuery = "SELECT database_id FROM sys.databases WHERE name = 'HousingRental'";
                using (SqlCommand checkCmd = new SqlCommand(checkDbQuery, masterConn))
                {
                    object result = checkCmd.ExecuteScalar();
                    if (result == null)
                    {
                        string createDbQuery = "CREATE DATABASE HousingRental";
                        using (SqlCommand createCmd = new SqlCommand(createDbQuery, masterConn))
                        {
                            createCmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            // 2. Connect to the HousingRental database and ensure tables exist with initial seeding
            using (SqlConnection dbConn = new SqlConnection(connectionString))
            {
                dbConn.Open();

                // Create Users table and seed default admin user
                string createUsersTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                    BEGIN
                        CREATE TABLE Users (
                            id INT IDENTITY(1,1) PRIMARY KEY,
                            username NVARCHAR(50) NOT NULL UNIQUE,
                            email NVARCHAR(100) NOT NULL UNIQUE,
                            password_hash NVARCHAR(255) NOT NULL,
                            created_at DATETIME DEFAULT GETDATE() NULL
                        );

                        -- Seed default administrator account (username: admin, password: 1234)
                        INSERT INTO Users (username, email, password_hash)
                        VALUES (N'admin', N'admin@housingapp.com', N'1234');
                    END";

                using (SqlCommand cmd = new SqlCommand(createUsersTable, dbConn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Create Houses table and seed initial entries
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

                        -- Seed default properties matching original in-memory data
                        INSERT INTO Houses (name, address, status)
                        VALUES 
                        (N'Green Villa', N'Downtown St 10', N'Available'),
                        (N'Sunset Apartment', N'Beach Road Block 5', N'Rented'),
                        (N'Royal Palace', N'Al-Mansour District', N'Available');
                    END";

                using (SqlCommand cmd = new SqlCommand(createHousesTable, dbConn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Validates login credentials against the Users database table securely.
        /// </summary>
        public static bool ValidateUser(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                return false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(1) FROM Users WHERE (username = @login OR email = @login) AND password_hash = @password";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@login", login.Trim());
                    cmd.Parameters.AddWithValue("@password", password); // Stores standard text for demo compliance
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        /// <summary>
        /// Fetches the list of houses, with an optional search term filtering by Name or Address.
        /// </summary>
        public static List<House> GetHouses(string searchKeyword = "")
        {
            List<House> list = new List<House>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT id, name, address, status FROM Houses";
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    query += " WHERE name LIKE @search OR address LIKE @search";
                }
                query += " ORDER BY id ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(searchKeyword))
                    {
                        cmd.Parameters.AddWithValue("@search", "%" + searchKeyword + "%");
                    }

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
                }
            }
            return list;
        }

        /// <summary>
        /// Inserts a new house record into the Houses database table.
        /// </summary>
        public static bool AddHouse(string name, string address, string status)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
                return false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Houses (name, address, status) VALUES (@name, @address, @status)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", name.Trim());
                    cmd.Parameters.AddWithValue("@address", address.Trim());
                    cmd.Parameters.AddWithValue("@status", status);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        /// <summary>
        /// Deletes a house record from the Houses database table by its ID.
        /// </summary>
        public static bool DeleteHouse(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Houses WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
