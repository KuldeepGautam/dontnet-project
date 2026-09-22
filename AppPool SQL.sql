-- =================================================================================================
-- Grants each UBIS microservice's IIS Application Pool identity access to the shared UBIS-Dev
-- database on a developer machine. Run once per machine (idempotent - safe to re-run) with an
-- account that has sysadmin rights, e.g.:
--   sqlcmd -S .\SQLEXPRESS -E -i "AppPool SQL.sql"
--
-- Only services that talk to SQL Server directly need a grant (UBIS_Web and the API Gateway call
-- the microservices over HTTP and never open a SQL connection themselves, so they're excluded).
-- Pool names below match what Publish-Common.ps1 / Setup-DeveloperMachine.ps1 create in IIS; if a
-- developer machine's app pool was named differently, update the names in the list accordingly.
-- =================================================================================================

:setvar DatabaseName "UBIS-Dev"

DECLARE @Pools TABLE (PoolName sysname);
INSERT INTO @Pools (PoolName) VALUES
    (N'ubis-aim'),          -- AIM
    (N'ubis-menuservice'),  -- MenuGenerator
    (N'ubis_LogWriter'),    -- LogWriter
    (N'ubis-prebudget'),    -- PreBudget
    (N'ubis_Email'),        -- Email
    (N'ubis_UserProfile');  -- UserProfile
GO

-- ---------------------------------------------------------------------------
-- 1) Create a SQL Server login for each app pool identity (runs in master).
-- ---------------------------------------------------------------------------
USE [master];
GO

DECLARE @login sysname, @sql nvarchar(max);
DECLARE pool_cursor CURSOR FOR
    SELECT N'IIS APPPOOL\' + PoolName FROM @Pools;

OPEN pool_cursor;
FETCH NEXT FROM pool_cursor INTO @login;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = @login)
    BEGIN
        SET @sql = N'CREATE LOGIN [' + @login + N'] FROM WINDOWS;';
        EXEC (@sql);
        PRINT 'Created login: ' + @login;
    END
    ELSE
    BEGIN
        PRINT 'Login already exists: ' + @login;
    END
    FETCH NEXT FROM pool_cursor INTO @login;
END
CLOSE pool_cursor;
DEALLOCATE pool_cursor;
GO

-- ---------------------------------------------------------------------------
-- 2) Create a database user for each login and add it to db_datareader /
--    db_datawriter on the UBIS-Dev database.
-- ---------------------------------------------------------------------------
USE [$(DatabaseName)];
GO

DECLARE @Pools TABLE (PoolName sysname);
INSERT INTO @Pools (PoolName) VALUES
    (N'ubis-aim'), (N'ubis-menuservice'), (N'ubis_LogWriter'),
    (N'ubis-prebudget'), (N'ubis_Email'), (N'ubis_UserProfile');

DECLARE @login sysname, @sql nvarchar(max);
DECLARE pool_cursor CURSOR FOR
    SELECT N'IIS APPPOOL\' + PoolName FROM @Pools;

OPEN pool_cursor;
FETCH NEXT FROM pool_cursor INTO @login;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = @login)
    BEGIN
        SET @sql = N'CREATE USER [' + @login + N'] FOR LOGIN [' + @login + N'];';
        EXEC (@sql);
        PRINT 'Created database user: ' + @login;
    END

    SET @sql = N'ALTER ROLE db_datareader ADD MEMBER [' + @login + N'];';
    EXEC (@sql);
    SET @sql = N'ALTER ROLE db_datawriter ADD MEMBER [' + @login + N'];';
    EXEC (@sql);
    PRINT 'Granted db_datareader/db_datawriter: ' + @login;

    FETCH NEXT FROM pool_cursor INTO @login;
END
CLOSE pool_cursor;
DEALLOCATE pool_cursor;
GO
