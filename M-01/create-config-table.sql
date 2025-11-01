-- ==========================================
-- Project : Adv-sql Milestone 01
-- File: create-tables.sql
-- Developers: Abdurrahman Almouna, yafet Tekleab
-- Overview: This file creates the config table and inserts the initial values
-- ==========================================

IF OBJECT_ID('dbo.APP_CONFIG','U')          IS NOT NULL DROP TABLE dbo.APP_CONFIG;
GO

CREATE TABLE dbo.APP_CONFIG
(
  ConfigItemID       int IDENTITY(1,1) PRIMARY KEY,
  configType         nvarchar(20)  NOT NULL,       
  configDescription  nvarchar(100) NOT NULL,      
  value              decimal(18,4) NOT NULL,       
  CreatedAtUtc       datetime2(0)  NOT NULL CONSTRAINT DF_CFG_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAtUtc       datetime2(0)  NOT NULL CONSTRAINT DF_CFG_Updated DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_CFG_Type CHECK (configType IN ('System','Part')),
  CONSTRAINT UQ_CFG_Key  UNIQUE (configType, configDescription)
);
GO

;MERGE dbo.APP_CONFIG AS tgt
USING (VALUES
  ('System','TimeScale',         CAST(2     AS decimal(18,4))),
  ('Part',  'Harness',           CAST(55    AS decimal(18,4))),
  ('Part',  'Reflector',         CAST(35    AS decimal(18,4))),
  ('Part',  'Housing',           CAST(24    AS decimal(18,4))),
  ('Part',  'Lens',              CAST(40    AS decimal(18,4))),
  ('Part',  'Bulb',              CAST(60    AS decimal(18,4))),
  ('Part',  'Bezel',             CAST(75    AS decimal(18,4))),
  ('System','BinMin',            CAST(5     AS decimal(18,4))),
  ('System','RefreshSpan',       CAST(5     AS decimal(18,4))),
  ('System','AssemblyStations',  CAST(3     AS decimal(18,4))),
  ('System','OrderAmount',       CAST(1000  AS decimal(18,4)))
) AS src(configType, configDescription, value)
ON  tgt.configType = src.configType
AND tgt.configDescription = src.configDescription
WHEN MATCHED THEN
  UPDATE SET tgt.value = src.value
WHEN NOT MATCHED THEN
  INSERT (configType, configDescription, value)
  VALUES (src.configType, src.configDescription, src.value);