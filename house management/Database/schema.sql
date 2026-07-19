-- ============================================================================
-- Database Schema for Housing Management Application
-- Target Database: MS SQL Server / LocalDB (Standard for C# WinForms) or SQLite
-- Description: Contains tables for Users (Authentication) and Houses
-- ============================================================================

-- Create Users Table
CREATE TABLE [dbo].[Users] (
    [id]            INT IDENTITY(1,1) NOT NULL,
    [username]      NVARCHAR(50)      NOT NULL,
    [email]         NVARCHAR(100)     NOT NULL,
    [password_hash] NVARCHAR(255)     NOT NULL, -- In production, store SHA-256 or bcrypt hash
    [created_at]    DATETIME          DEFAULT GETDATE() NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_Users_Username] UNIQUE ([username]),
    CONSTRAINT [UQ_Users_Email]    UNIQUE ([email])
);
GO

-- Create Houses Table
CREATE TABLE [dbo].[Houses] (
    [id]         INT IDENTITY(1,1) NOT NULL,
    [name]       NVARCHAR(100)     NOT NULL,
    [address]    NVARCHAR(255)     NOT NULL,
    [status]     NVARCHAR(50)      DEFAULT 'Available' NOT NULL, -- e.g., 'Available', 'Rented'
    [created_at] DATETIME          DEFAULT GETDATE() NULL,
    CONSTRAINT [PK_Houses] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_Houses_Status] CHECK ([status] IN ('Available', 'Rented'))
);
GO

-- Create Non-Clustered Indexes for Faster Searches/Filters
CREATE NONCLUSTERED INDEX [IX_Houses_Name_Address]
    ON [dbo].[Houses] ([name] ASC, [address] ASC);
GO

