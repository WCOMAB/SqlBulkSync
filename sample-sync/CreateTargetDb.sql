CREATE DATABASE TargetDatabase
GO
USE TargetDatabase
GO
CREATE TABLE dbo.Company(
    Id          bigint IDENTITY(1,1)    NOT NULL,
    Company     varchar(256)            NOT NULL,
    Address1    varchar(256)            NOT NULL,
    Address2    varchar(256)            NOT NULL,
    PostalCode  varchar(16)             NOT NULL,
    City        varchar(256)            NOT NULL,
    State       varchar(256)            NOT NULL,
    Country     varchar(2)              NOT NULL,
    LastUpdate  datetime                NOT NULL,
    Created     datetime                NOT NULL,
    CONSTRAINT PK_Company PRIMARY KEY CLUSTERED(Id ASC)
)
GO
CREATE SCHEMA sync
GO