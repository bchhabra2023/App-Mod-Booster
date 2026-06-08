IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'mid-appmodassist')
BEGIN
    DROP USER [mid-appmodassist];
END;

CREATE USER [mid-appmodassist] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [mid-appmodassist];
ALTER ROLE db_datawriter ADD MEMBER [mid-appmodassist];
GRANT EXECUTE TO [mid-appmodassist];
GO
