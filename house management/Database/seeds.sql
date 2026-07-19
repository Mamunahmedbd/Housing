-- ============================================================================
-- Seed Data for Housing Management Application
-- Target Database: MS SQL Server / LocalDB or SQLite
-- Description: Sets up default users and default houses matching the code
-- ============================================================================

-- Clear existing data if necessary (order matters due to foreign keys if added later)
TRUNCATE TABLE [dbo].[Houses];
TRUNCATE TABLE [dbo].[Users];
GO

-- ----------------------------------------------------------------------------
-- 1. SEED USER ACCOUNTS
-- ----------------------------------------------------------------------------

-- Default Admin Account (Matches admin / 1234 checks in Form1.cs)
INSERT INTO [dbo].[Users] ([username], [email], [password_hash])
VALUES (N'admin', N'admin@housingapp.com', N'1234');

-- Secondary Manager Account (For future expansion or alternative testing)
INSERT INTO [dbo].[Users] ([username], [email], [password_hash])
VALUES (N'manager', N'manager@housingapp.com', N'securepass5678');
GO

-- ----------------------------------------------------------------------------
-- 2. SEED HOUSES (Includes required defaults and additional test data)
-- ----------------------------------------------------------------------------

INSERT INTO [dbo].[Houses] ([name], [address], [status])
VALUES 
-- Required Default Data from Form1.cs
(N'Green Villa', N'Downtown St 10', N'Available'),
(N'Sunset Apartment', N'Beach Road Block 5', N'Rented'),
(N'Royal Palace', N'Al-Mansour District', N'Available'),

-- Additional Realistic Houses for testing Search and Filter features
(N'Blue Horizon Condo', N'Ocean Avenue Lane 2', N'Available'),
(N'Maple Cottage', N'Suburban Hills Road 45', N'Rented'),
(N'Hilltop Mansion', N'Highland View Heights', N'Available'),
(N'Downtown Studio 4B', N'City Center Plaza', N'Rented'),
(N'Oakwood Residence', N'Westside Garden District', N'Available');
GO
