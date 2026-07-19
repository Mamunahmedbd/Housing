-- ============================================================================
-- Seed Data for Housing Rental Management Application
-- Target Database: MS SQL Server / LocalDB
-- Description: Sets up default users, houses, tenants, and rentals matching the code
-- ============================================================================

-- Clear existing data (order matters due to foreign key relationships)
IF OBJECT_ID('[dbo].[Rentals]', 'U') IS NOT NULL TRUNCATE TABLE [dbo].[Rentals];
IF OBJECT_ID('[dbo].[Houses]', 'U') IS NOT NULL DELETE FROM [dbo].[Houses];
IF OBJECT_ID('[dbo].[Tenants]', 'U') IS NOT NULL DELETE FROM [dbo].[Tenants];
IF OBJECT_ID('[dbo].[Users]', 'U') IS NOT NULL DELETE FROM [dbo].[Users];
GO

-- Reset IDENTITY counters
DBCC CHECKIDENT ('[dbo].[Houses]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Tenants]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Users]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Rentals]', RESEED, 0);
GO

-- ----------------------------------------------------------------------------
-- 1. SEED USER ACCOUNTS (System Users)
-- ----------------------------------------------------------------------------
-- NOTE: The plaintext values below are migrated automatically to PBKDF2
-- hashes by UserService.Authenticate on the next successful login. If you
-- prefer to seed a proper hash directly, replace the password_hash column.
INSERT INTO [dbo].[Users] ([username], [email], [password_hash], [full_name], [phone], [role], [status])
VALUES
(N'admin',   N'admin@housingapp.com',   N'1234',          N'System Administrator', N'+1-555-0100', 0, 0),
(N'manager', N'manager@housingapp.com', N'manager123',    N'Property Manager',     N'+1-555-0101', 1, 0),
(N'user1',   N'user1@housingapp.com',   N'user1234',      N'Regular User',         N'+1-555-0102', 2, 0);
GO

-- ----------------------------------------------------------------------------
-- 2. SEED HOUSES
-- ----------------------------------------------------------------------------
INSERT INTO [dbo].[Houses] ([name], [address], [status])
VALUES 
(N'Green Villa', N'Downtown St 10', N'Available'),
(N'Sunset Apartment', N'Beach Road Block 5', N'Rented'),
(N'Royal Palace', N'Al-Mansour District', N'Available'),
(N'Blue Horizon Condo', N'Ocean Avenue Lane 2', N'Available'),
(N'Oakwood Residence', N'Westside Garden District', N'Available');
GO

-- ----------------------------------------------------------------------------
-- 3. SEED TENANTS
-- ----------------------------------------------------------------------------
INSERT INTO [dbo].[Tenants] ([name], [email], [phone])
VALUES 
(N'John Doe', N'john.doe@email.com', N'+1-555-0199'),
(N'Jane Smith', N'jane.smith@email.com', N'+1-555-0144'),
(N'Michael Brown', N'michael.b@email.com', N'+1-555-0177');
GO

-- ----------------------------------------------------------------------------
-- 4. SEED RENTALS (Contracts linking Houses and Tenants)
-- ----------------------------------------------------------------------------
-- Sunset Apartment (ID 2) is rented by John Doe (ID 1)
INSERT INTO [dbo].[Rentals] ([house_id], [tenant_id], [rent_amount], [start_date], [end_date], [status])
VALUES 
(2, 1, 1500.00, '2026-01-01', '2026-12-31', N'Active');
GO
