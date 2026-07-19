-- ============================================================================
-- Migration: 004_create_tenants_module.sql
-- Description: Deploys the stored procedures used by the Tenant module
--              (CRUD with optional search). The Tenants table itself and its
--              name index already exist from migration 001_create_tenants.sql;
--              this migration only adds the procedure layer on top.
--
-- Applies on top of an existing database. Idempotent: safe to re-run.
-- Mirrors the runtime deployment performed by
-- DatabaseHelper.EnsureStoredProceduresExist().
-- ============================================================================

USE [HousingRental];
GO

-- ----------------------------------------------------------------------------
-- 1. sp_GetTenants — list tenants, with optional search across name/email/phone
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_GetTenants', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetTenants;
GO

CREATE PROCEDURE dbo.sp_GetTenants
    @searchKeyword NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @searchKeyword IS NULL OR LTRIM(RTRIM(@searchKeyword)) = ''
    BEGIN
        SELECT [id], [name], [email], [phone], [created_at]
          FROM dbo.Tenants
         ORDER BY [id] ASC;
    END
    ELSE
    BEGIN
        SELECT [id], [name], [email], [phone], [created_at]
          FROM dbo.Tenants
         WHERE [name]  LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
            OR [email] LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
            OR [phone] LIKE '%' + LTRIM(RTRIM(@searchKeyword)) + '%'
         ORDER BY [id] ASC;
    END
END;
GO

-- ----------------------------------------------------------------------------
-- 2. sp_CreateTenant — insert a new tenant and return its new identity id
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_CreateTenant', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateTenant;
GO

CREATE PROCEDURE dbo.sp_CreateTenant
    @name  NVARCHAR(100),
    @email NVARCHAR(100),
    @phone NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Tenants ([name], [email], [phone])
    VALUES (@name, @email, @phone);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS [NewId];
END;
GO

-- ----------------------------------------------------------------------------
-- 3. sp_UpdateTenant — update an existing tenant's profile
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_UpdateTenant', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateTenant;
GO

CREATE PROCEDURE dbo.sp_UpdateTenant
    @id    INT,
    @name  NVARCHAR(100),
    @email NVARCHAR(100),
    @phone NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Tenants
       SET [name]  = @name,
           [email] = @email,
           [phone] = @phone
     WHERE [id] = @id;
END;
GO

-- ----------------------------------------------------------------------------
-- 4. sp_DeleteTenant — delete a tenant by id
--    NOTE: The Tenants table has ON DELETE CASCADE from Rentals, but the
--    application layer (TenantService.Delete) blocks the operation when
--    rentals still reference the tenant, to avoid silent data loss.
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_DeleteTenant', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteTenant;
GO

CREATE PROCEDURE dbo.sp_DeleteTenant
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Tenants WHERE [id] = @id;
END;
GO

-- ----------------------------------------------------------------------------
-- 5. VERIFICATION (run manually after applying)
-- ----------------------------------------------------------------------------
-- SELECT name, type_desc FROM sys.objects
--  WHERE type = 'P' AND name LIKE 'sp[_]Tenant%' ORDER BY name;
GO
