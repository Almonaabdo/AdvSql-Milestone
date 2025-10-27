-- ==========================================
-- Project : Adv-sql Milestone 01
-- File: create-tables.sql
-- Developers: Abdurrahman Almouna, yafet Tekleab
-- Overview: This file creates the 10 database tables required for M-01
-- ==========================================

use [advsql-milestone];


CREATE TABLE APP_CONFIG (
    ConfigID INT IDENTITY(1,1) PRIMARY KEY,
    TimeScaleTick DECIMAL(10,2),
    OrderQuantityTarget INT,
    StationCount INT,
    RejectionRateThreshold DECIMAL(5,2),
    ShutdownScriptMinutes INT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);
GO


CREATE TABLE APP_PLANT (
    PlantID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Capacity INT,
    UnitOfMeasure NVARCHAR(50)
);
GO

CREATE TABLE APP_STATION (
    StationID INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50),
    Name NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    PlantID INT NOT NULL,
    FOREIGN KEY (PlantID) REFERENCES APP_PLANT(PlantID)
);
GO

CREATE TABLE APP_WORKERSKILL (
    SkillID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100),
    MaxRate DECIMAL(10,2)
);
GO


CREATE TABLE APP_WORKER (
    WorkerID INT IDENTITY(1,1) PRIMARY KEY,
    StationID INT NOT NULL,
    SkillID INT NOT NULL,
    Name NVARCHAR(100),
    FOREIGN KEY (StationID) REFERENCES APP_STATION(StationID),
    FOREIGN KEY (SkillID) REFERENCES APP_WORKERSKILL(SkillID)
);
GO


CREATE TABLE APP_ALERT (
    AlertID INT IDENTITY(1,1) PRIMARY KEY,
    StationID INT NOT NULL,
    PartID INT NOT NULL,
    AlertType NVARCHAR(100),
    Reason NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (StationID) REFERENCES APP_STATION(StationID),
    FOREIGN KEY (PartID) REFERENCES APP_PLANT(PlantID)
);
GO


CREATE TABLE APP_BIN (
    BinID INT IDENTITY(1,1) PRIMARY KEY,
    StationID INT NOT NULL,
    PartID INT NOT NULL,
    Quantity INT DEFAULT 0,
    MinQty INT DEFAULT 0,
    FOREIGN KEY (StationID) REFERENCES APP_STATION(StationID),
    FOREIGN KEY (PartID) REFERENCES APP_PLANT(PlantID)
);
GO


CREATE TABLE APP_BINTRANSACTION (
    BinTransID INT IDENTITY(1,1) PRIMARY KEY,
    BinID INT NOT NULL,
    StationID INT NOT NULL,
    PartID INT NOT NULL,
    DeltaQty INT,
    Reason NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (BinID) REFERENCES APP_BIN(BinID),
    FOREIGN KEY (StationID) REFERENCES APP_STATION(StationID),
    FOREIGN KEY (PartID) REFERENCES APP_PLANT(PlantID)
);
GO

CREATE TABLE APP_ASSEMBLY (
    AssemblyID INT IDENTITY(1,1) PRIMARY KEY,
    StationID INT NOT NULL,
    WorkerID INT NOT NULL,
    StartedAt DATETIME DEFAULT GETDATE(),
    FinishedAt DATETIME NULL,
    Result CHAR(1) CHECK (Result IN ('P','F')),
    FOREIGN KEY (StationID) REFERENCES APP_STATION(StationID),
    FOREIGN KEY (WorkerID) REFERENCES APP_WORKER(WorkerID)
);
GO

CREATE TABLE APP_ASSEMBLYPARTUSE (
    AssemblyPartUseID INT IDENTITY(1,1) PRIMARY KEY,
    AssemblyID INT NOT NULL,
    PartID INT NOT NULL,
    Quantity INT,
    FOREIGN KEY (AssemblyID) REFERENCES APP_ASSEMBLY(AssemblyID),
    FOREIGN KEY (PartID) REFERENCES APP_PLANT(PlantID)
);
GO