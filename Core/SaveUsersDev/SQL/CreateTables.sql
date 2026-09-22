-- Dev-only tables for SaveUsersDev. Run against the AIM database before calling the API.
USE [AIM];
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[M_Role]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[M_Role]
    (
        [RoleId] INT IDENTITY(1,1) PRIMARY KEY,
        [RoleName] NVARCHAR(200) NOT NULL UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[M_Users]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[M_Users]
    (
        [UserId] INT IDENTITY(1,1) PRIMARY KEY,
        [UserName] NVARCHAR(50) NOT NULL UNIQUE,
        [Password] NVARCHAR(512) NOT NULL,
        [EmailAddress] NVARCHAR(320) NOT NULL,
        [RoleId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[M_Role]([RoleId])
    );
END
GO

-- Sample roles for testing
INSERT INTO [dbo].[M_Role] (RoleName) SELECT 'Admin' WHERE NOT EXISTS (SELECT 1 FROM [dbo].[M_Role] WHERE RoleName = 'Admin');
INSERT INTO [dbo].[M_Role] (RoleName) SELECT 'User' WHERE NOT EXISTS (SELECT 1 FROM [dbo].[M_Role] WHERE RoleName = 'User');
GO
