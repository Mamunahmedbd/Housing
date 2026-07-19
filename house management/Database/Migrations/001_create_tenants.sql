-- ============================================================================
-- Migration: 001_create_tenants.sql
-- Description: Creates the Tenants database table and its indexes
-- ============================================================================

USE [HousingRental];
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tenants')
BEGIN
    CREATE TABLE Tenants (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        email NVARCHAR(100) NOT NULL UNIQUE,
        phone NVARCHAR(50) NOT NULL,
        created_at DATETIME DEFAULT GETDATE() NULL
    );

    CREATE NONCLUSTERED INDEX [IX_Tenants_Name]
        ON [dbo].[Tenants] ([name] ASC);
END
GO
