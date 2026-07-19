-- ============================================================================
-- Migration: 002_create_rentals.sql
-- Description: Creates the Rentals database table with foreign keys
-- ============================================================================

USE [HousingRental];
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Rentals')
BEGIN
    CREATE TABLE Rentals (
        id INT IDENTITY(1,1) PRIMARY KEY,
        house_id INT NOT NULL,
        tenant_id INT NOT NULL,
        rent_amount DECIMAL(18,2) NOT NULL,
        start_date DATETIME NOT NULL,
        end_date DATETIME NOT NULL,
        status NVARCHAR(50) DEFAULT 'Active' NOT NULL CHECK (status IN ('Active', 'Completed')),
        created_at DATETIME DEFAULT GETDATE() NULL,
        CONSTRAINT [FK_Rentals_Houses] FOREIGN KEY ([house_id]) REFERENCES [dbo].[Houses] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Rentals_Tenants] FOREIGN KEY ([tenant_id]) REFERENCES [dbo].[Tenants] ([id]) ON DELETE CASCADE
    );
END
GO
