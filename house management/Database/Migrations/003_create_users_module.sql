-- ============================================================================
-- Migration: 003_create_users_module.sql
-- Description: Extends the Users table with profile/role/status columns,
--              adds supporting indexes, and deploys the stored procedures
--              used by the User module (CRUD + password management).
--
-- Applies on top of an existing database. Idempotent: safe to re-run.
-- Mirrors the runtime migration performed by DatabaseHelper.InitializeDatabase().
--
-- Columns added:
--   full_name   NVARCHAR(100) NULL   -- optional display name
--   phone       NVARCHAR(30)  NULL   -- optional contact phone
--   role        INT NOT NULL DEFAULT 2  -- 0=Admin, 1=Manager, 2=User
--   status      INT NOT NULL DEFAULT 0  -- 0=Active, 1=Locked
--   updated_at  DATETIME NULL        -- last row update timestamp
--   last_login  DATETIME NULL        -- last successful authentication
-- ============================================================================

USE [HousingRental];
GO

-- ----------------------------------------------------------------------------
-- 1. SCHEMA: add new columns to Users (skip if already present)
-- ----------------------------------------------------------------------------
IF COL_LENGTH('dbo.Users', 'full_name') IS NULL
    ALTER TABLE dbo.Users ADD full_name NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Users', 'phone') IS NULL
    ALTER TABLE dbo.Users ADD phone NVARCHAR(30) NULL;
GO

IF COL_LENGTH('dbo.Users', 'role') IS NULL
    ALTER TABLE dbo.Users ADD role INT NOT NULL DEFAULT 2;
GO

IF COL_LENGTH('dbo.Users', 'status') IS NULL
    ALTER TABLE dbo.Users ADD status INT NOT NULL DEFAULT 0;
GO

IF COL_LENGTH('dbo.Users', 'updated_at') IS NULL
    ALTER TABLE dbo.Users ADD updated_at DATETIME NULL;
GO

IF COL_LENGTH('dbo.Users', 'last_login') IS NULL
    ALTER TABLE dbo.Users ADD last_login DATETIME NULL;
GO

-- ----------------------------------------------------------------------------
-- 2. CONSTRAINTS: validate role/status ranges (skip if already present)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Users_Role')
    ALTER TABLE dbo.Users WITH NOCHECK ADD CONSTRAINT CK_Users_Role CHECK ([role] IN (0, 1, 2));
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Users_Status')
    ALTER TABLE dbo.Users WITH NOCHECK ADD CONSTRAINT CK_Users_Status CHECK ([status] IN (0, 1));
GO

-- ----------------------------------------------------------------------------
-- 3. INDEXES: speed up role/status filtering and admin counts
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Role_Status' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE NONCLUSTERED INDEX IX_Users_Role_Status
        ON dbo.Users ([role] ASC, [status] ASC);
GO

-- ----------------------------------------------------------------------------
-- 4. STORED PROCEDURES: user module operations
-- ----------------------------------------------------------------------------

-- 4.1 sp_GetUsers — list users, with optional search across username/email/full_name
IF OBJECT_ID('dbo.sp_GetUsers', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetUsers;
GO

CREATE PROCEDURE dbo.sp_GetUsers
    @searchKeyword NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @searchKeyword IS NULL OR LTRIM(RTRIM(@searchKeyword)) = ''
    BEGIN
        SELECT [id], [username], [email], [password_hash], [full_name], [phone],
               [role], [status], [created_at], [updated_at], [last_login]
          FROM dbo.Users
         ORDER BY [id] ASC;
    END
    ELSE
    BEGIN
        SELECT [id], [username], [email], [password_hash], [full_name], [phone],
               [role], [status], [created_at], [updated_at], [last_login]
          FROM dbo.Users
         WHERE [username]  LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
            OR [email]     LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
            OR [full_name] LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
         ORDER BY [id] ASC;
    END
END;
GO

-- 4.2 sp_CreateUser — insert a new user and return its new identity id
IF OBJECT_ID('dbo.sp_CreateUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateUser;
GO

CREATE PROCEDURE dbo.sp_CreateUser
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

    INSERT INTO dbo.Users
        ([username], [email], [password_hash], [full_name], [phone], [role], [status])
    VALUES
        (@username, @email, @passwordHash, @fullName, @phone, @role, @status);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS [NewId];
END;
GO

-- 4.3 sp_UpdateUser — update profile/role/status (does not touch password)
IF OBJECT_ID('dbo.sp_UpdateUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateUser;
GO

CREATE PROCEDURE dbo.sp_UpdateUser
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

    UPDATE dbo.Users
       SET [username]   = @username,
           [email]      = @email,
           [full_name]  = @fullName,
           [phone]      = @phone,
           [role]       = @role,
           [status]     = @status,
           [updated_at] = GETDATE()
     WHERE [id] = @id;
END;
GO

-- 4.4 sp_ChangeUserPassword — replace the password hash (used by both the
--     self-service "Change Password" and admin "Reset Password" features)
IF OBJECT_ID('dbo.sp_ChangeUserPassword', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ChangeUserPassword;
GO

CREATE PROCEDURE dbo.sp_ChangeUserPassword
    @id           INT,
    @passwordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
       SET [password_hash] = @passwordHash,
           [updated_at]    = GETDATE()
     WHERE [id] = @id;
END;
GO

-- 4.5 sp_TouchUserLastLogin — record timestamp of a successful authentication
IF OBJECT_ID('dbo.sp_TouchUserLastLogin', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_TouchUserLastLogin;
GO

CREATE PROCEDURE dbo.sp_TouchUserLastLogin
    @id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
       SET [last_login] = GETDATE()
     WHERE [id] = @id;
END;
GO

-- ----------------------------------------------------------------------------
-- 5. SEED: promote the default 'admin' account to Admin role / Active status.
--    The legacy plaintext password is left untouched here; it is migrated to
--    a PBKDF2 hash transparently by UserService.Authenticate on first login.
-- ----------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM dbo.Users WHERE username = N'admin')
BEGIN
    UPDATE dbo.Users
       SET [role]   = 0,    -- Administrator
           [status] = 0     -- Active
     WHERE [username] = N'admin';
END
GO

-- ----------------------------------------------------------------------------
-- 6. VERIFICATION: quick sanity check (run manually after applying)
--    Expected columns:
--      id, username, email, password_hash, full_name, phone,
--      role, status, created_at, updated_at, last_login
-- ----------------------------------------------------------------------------
-- SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
--   FROM INFORMATION_SCHEMA.COLUMNS
--  WHERE TABLE_NAME = 'Users'
--  ORDER BY ORDINAL_POSITION;
--
-- SELECT name, type_desc FROM sys.objects
--  WHERE type = 'P' AND name LIKE 'sp[_]%' ORDER BY name;
GO
