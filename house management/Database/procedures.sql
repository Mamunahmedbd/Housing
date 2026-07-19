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
