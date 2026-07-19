-- ============================================================================
-- Stored Procedures for Housing Management Application
-- Target Database: MS SQL Server / LocalDB
-- Description: Encapsulates CRUD operations and login checks inside the database
-- ============================================================================

USE [HousingRental];
GO

-- ----------------------------------------------------------------------------
-- 1. PROCEDURE: sp_ValidateUser
-- Description: Validates user credentials for Login and Recovery
-- ----------------------------------------------------------------------------
IF OBJECT_ID('[dbo].[sp_ValidateUser]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_ValidateUser];
GO

CREATE PROCEDURE [dbo].[sp_ValidateUser]
    @login NVARCHAR(100),
    @password NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT COUNT(1) 
    FROM [dbo].[Users] 
    WHERE ([username] = @login OR [email] = @login) 
      AND [password_hash] = @password;
END;
GO

-- ----------------------------------------------------------------------------
-- 2. PROCEDURE: sp_GetHouses
-- Description: Retrieves houses, with optional name/address search keyword
-- ----------------------------------------------------------------------------
IF OBJECT_ID('[dbo].[sp_GetHouses]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_GetHouses];
GO

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
        WHERE [name] LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
           OR [address] LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
        ORDER BY [id] ASC;
    END
END;
GO

-- ----------------------------------------------------------------------------
-- 3. PROCEDURE: sp_AddHouse
-- Description: Creates a new house record
-- ----------------------------------------------------------------------------
IF OBJECT_ID('[dbo].[sp_AddHouse]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_AddHouse];
GO

CREATE PROCEDURE [dbo].[sp_AddHouse]
    @name NVARCHAR(100),
    @address NVARCHAR(255),
    @status NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[Houses] ([name], [address], [status])
    VALUES (@name, @address, @status);
END;
GO

-- ----------------------------------------------------------------------------
-- 4. PROCEDURE: sp_DeleteHouse
-- Description: Deletes a house listing by ID
-- ----------------------------------------------------------------------------
IF OBJECT_ID('[dbo].[sp_DeleteHouse]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_DeleteHouse];
GO

CREATE PROCEDURE [dbo].[sp_DeleteHouse]
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [dbo].[Houses]
     WHERE [id] = @id;
END;
GO

-- ----------------------------------------------------------------------------
-- 5. PROCEDURE: sp_GetUsers
-- Description: Retrieves users, optionally filtered by username/email/full_name
-- ----------------------------------------------------------------------------
IF OBJECT_ID('[dbo].[sp_GetUsers]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_GetUsers];
GO

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
END;
GO

-- ----------------------------------------------------------------------------
-- 6. PROCEDURE: sp_CreateUser
-- Description: Creates a new user and returns the new identity id
-- ----------------------------------------------------------------------------
IF OBJECT_ID('[dbo].[sp_CreateUser]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_CreateUser];
GO

CREATE PROCEDURE [dbo].[sp_CreateUser]
    @username      NVARCHAR(50),
    @email         NVARCHAR(100),
    @passwordHash  NVARCHAR(255),
    @fullName      NVARCHAR(100) = NULL,
    @phone         NVARCHAR(30)  = NULL,
    @role          INT = 2,
    @status        INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[Users] ([username], [email], [password_hash], [full_name], [phone], [role], [status])
    VALUES (@username, @email, @passwordHash, @fullName, @phone, @role, @status);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS [NewId];
END;
GO

-- ----------------------------------------------------------------------------
-- 7. PROCEDURE: sp_UpdateUser
-- Description: Updates an existing user's profile / role / status
-- ----------------------------------------------------------------------------
IF OBJECT_ID('[dbo].[sp_UpdateUser]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_UpdateUser];
GO

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
       SET [username]  = @username,
           [email]     = @email,
           [full_name] = @fullName,
           [phone]     = @phone,
           [role]      = @role,
           [status]    = @status,
           [updated_at] = GETDATE()
     WHERE [id] = @id;
END;
GO

-- ----------------------------------------------------------------------------
-- 8. PROCEDURE: sp_ChangeUserPassword
-- Description: Replaces the password hash for a user (used by Change Password
--              and Admin Reset Password features)
-- ----------------------------------------------------------------------------
IF OBJECT_ID('[dbo].[sp_ChangeUserPassword]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_ChangeUserPassword];
GO

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
END;
GO
