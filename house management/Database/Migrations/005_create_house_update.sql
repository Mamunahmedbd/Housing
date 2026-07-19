-- ============================================================================
-- Migration: 005_create_house_update.sql
-- Description: Adds the missing sp_UpdateHouse stored procedure so the House
--              module supports full CRUD (Create / Read / Update / Delete),
--              matching the User and Tenant modules. Also upgrades sp_AddHouse
--              to return the new identity id so callers don't need a follow-up
--              SELECT to fetch the created row.
--
-- Applies on top of an existing database. Idempotent: safe to re-run.
-- Mirrors the runtime deployment performed by
-- DatabaseHelper.EnsureStoredProceduresExist().
-- ============================================================================

USE [HousingRental];
GO

-- ----------------------------------------------------------------------------
-- 1. sp_AddHouse — upgraded to return the new identity id
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_AddHouse', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AddHouse;
GO

CREATE PROCEDURE dbo.sp_AddHouse
    @name    NVARCHAR(100),
    @address NVARCHAR(255),
    @status  NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Houses ([name], [address], [status])
    VALUES (@name, @address, @status);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS [NewId];
END;
GO

-- ----------------------------------------------------------------------------
-- 2. sp_UpdateHouse — new procedure, enables the Edit feature
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_UpdateHouse', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateHouse;
GO

CREATE PROCEDURE dbo.sp_UpdateHouse
    @id      INT,
    @name    NVARCHAR(100),
    @address NVARCHAR(255),
    @status  NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Houses
       SET [name]    = @name,
           [address] = @address,
           [status]  = @status
     WHERE [id] = @id;
END;
GO

-- ----------------------------------------------------------------------------
-- 3. VERIFICATION (run manually after applying)
-- ----------------------------------------------------------------------------
-- SELECT name FROM sys.objects
--  WHERE type = 'P' AND name LIKE 'sp[_]%House%' ORDER BY name;
-- -- Expected: sp_AddHouse, sp_DeleteHouse, sp_GetHouses, sp_UpdateHouse
GO
