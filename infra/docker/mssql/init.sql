-- Faaz platform database initialisation
-- Runs once when SQL Server container first starts

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'FaazDb')
BEGIN
    CREATE DATABASE FaazDb;
END
GO

USE FaazDb;
GO

-- EF Core migrations will create all tables.
-- This script only ensures the database exists so migrations can run.
