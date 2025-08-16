-- Script para criar a database VeniceOrdersDB no SQL Server
USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'VeniceOrdersDB')
BEGIN
    CREATE DATABASE VeniceOrdersDB;
    PRINT 'Database VeniceOrdersDB criada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Database VeniceOrdersDB já existe.';
END
GO

-- Alterar para usar a nova database
USE VeniceOrdersDB;
GO

-- Criar um schema específico se necessário (opcional)
-- IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'orders')
-- BEGIN
--     EXEC('CREATE SCHEMA orders');
--     PRINT 'Schema orders criado com sucesso!';
-- END
-- GO

PRINT 'Inicialização do SQL Server concluída!';
