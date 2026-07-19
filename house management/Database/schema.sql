-- ============================================================================
-- Database Schema for Housing Rental Management Application
-- Target Database: MS SQL Server / LocalDB
-- Description: Contains tables for Users, Houses, Tenants, and Rentals
-- ============================================================================

-- Drop existing tables to ensure a clean schema recreate (order is important due to foreign keys)
IF OBJECT_ID('[dbo].[Rentals]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Rentals];
GO
IF OBJECT_ID('[dbo].[Houses]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Houses];
GO
IF OBJECT_ID('[dbo].[Tenants]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Tenants];
GO
IF OBJECT_ID('[dbo].[Users]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Users];
GO

-- Create Users Table
CREATE TABLE [dbo].[Users] (
    [id]            INT IDENTITY(1,1) NOT NULL,
    [username]      NVARCHAR(50)      NOT NULL,
    [email]         NVARCHAR(100)     NOT NULL,
    [password_hash] NVARCHAR(255)     NOT NULL,    -- PBKDF2-SHA256, format: iterations.salt.hash
    [full_name]     NVARCHAR(100)     NULL,
    [phone]         NVARCHAR(30)      NULL,
    [role]          INT               NOT NULL DEFAULT 2,  -- 0=Admin, 1=Manager, 2=User
    [status]        INT               NOT NULL DEFAULT 0,  -- 0=Active, 1=Locked
    [created_at]    DATETIME          DEFAULT GETDATE() NULL,
    [updated_at]    DATETIME          NULL,
    [last_login]    DATETIME          NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_Users_Username] UNIQUE ([username]),
    CONSTRAINT [UQ_Users_Email]    UNIQUE ([email]),
    CONSTRAINT [CK_Users_Role]     CHECK ([role] IN (0, 1, 2)),
    CONSTRAINT [CK_Users_Status]   CHECK ([status] IN (0, 1))
);
GO

-- Create Houses Table
CREATE TABLE [dbo].[Houses] (
    [id]         INT IDENTITY(1,1) NOT NULL,
    [name]       NVARCHAR(100)     NOT NULL,
    [address]    NVARCHAR(255)     NOT NULL,
    [status]     NVARCHAR(50)      DEFAULT 'Available' NOT NULL, -- 'Available', 'Rented'
    [created_at] DATETIME          DEFAULT GETDATE() NULL,
    CONSTRAINT [PK_Houses] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_Houses_Status] CHECK ([status] IN ('Available', 'Rented'))
);
GO

-- Create Tenants Table
CREATE TABLE [dbo].[Tenants] (
    [id]         INT IDENTITY(1,1) NOT NULL,
    [name]       NVARCHAR(100)     NOT NULL,
    [email]      NVARCHAR(100)     NOT NULL,
    [phone]      NVARCHAR(50)      NOT NULL,
    [created_at] DATETIME          DEFAULT GETDATE() NULL,
    CONSTRAINT [PK_Tenants] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_Tenants_Email] UNIQUE ([email])
);
GO

-- Create Rentals Table
CREATE TABLE [dbo].[Rentals] (
    [id]          INT IDENTITY(1,1) NOT NULL,
    [house_id]    INT               NOT NULL,
    [tenant_id]   INT               NOT NULL,
    [rent_amount] DECIMAL(18,2)     NOT NULL,
    [start_date]  DATETIME          NOT NULL,
    [end_date]    DATETIME          NOT NULL,
    [status]      NVARCHAR(50)      DEFAULT 'Active' NOT NULL, -- 'Active', 'Completed'
    [created_at]  DATETIME          DEFAULT GETDATE() NULL,
    CONSTRAINT [PK_Rentals] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [FK_Rentals_Houses] FOREIGN KEY ([house_id]) REFERENCES [dbo].[Houses] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Rentals_Tenants] FOREIGN KEY ([tenant_id]) REFERENCES [dbo].[Tenants] ([id]) ON DELETE CASCADE,
    CONSTRAINT [CK_Rentals_Status] CHECK ([status] IN ('Active', 'Completed'))
);
GO

-- Create Non-Clustered Indexes for Faster Searches/Filters
CREATE NONCLUSTERED INDEX [IX_Houses_Name_Address]
    ON [dbo].[Houses] ([name] ASC, [address] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Tenants_Name]
    ON [dbo].[Tenants] ([name] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Users_Role_Status]
    ON [dbo].[Users] ([role] ASC, [status] ASC);
GO
