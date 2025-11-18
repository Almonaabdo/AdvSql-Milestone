USE [advsql];
GO
CREATE OR ALTER PROCEDURE dbo.usp_CreateStationWithBins
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        -- count existing stations (0 => we're creating S1)
        DECLARE @stationCount int = (SELECT COUNT(*) FROM dbo.APP_STATION);

        -- figure out next station code S1,S2,...
        DECLARE @nextNo int =
            ISNULL((
                SELECT MAX(TRY_CAST(SUBSTRING(Code, 2, 10) AS int))
                FROM dbo.APP_STATION
                WHERE Code LIKE 'S%'
            ), 0) + 1;

        DECLARE @code nvarchar(50)  = CONCAT('S', @nextNo);
        DECLARE @name nvarchar(100) = CONCAT('Station ', @nextNo);

        -- create the new station
        INSERT dbo.APP_STATION(Code, Name, IsActive) VALUES (@code, @name, 1);
        DECLARE @stationId int = SCOPE_IDENTITY();

        -- ensure APP_PART has every part from APP_CONFIG (configType='Part')
        ;WITH cfg AS (
            SELECT configDescription AS PartName, CAST(value AS int) AS ConfigQty
            FROM dbo.APP_CONFIG WHERE configType='Part'
        )
        INSERT dbo.APP_PART(Name, BinCapacity)
        SELECT c.PartName, NULL
        FROM cfg c
        WHERE NOT EXISTS (SELECT 1 FROM dbo.APP_PART p WHERE p.Name=c.PartName);

        -- capture CHANGED parts BEFORE updating BinCapacity
        DECLARE @ChangedParts TABLE(PartID int PRIMARY KEY, NewConfigQty int);
        INSERT @ChangedParts(PartID, NewConfigQty)
        SELECT p.PartID, c.ConfigQty
        FROM dbo.APP_PART p
        JOIN (
            SELECT configDescription AS PartName, CAST(value AS int) AS ConfigQty
            FROM dbo.APP_CONFIG WHERE configType='Part'
        ) c ON c.PartName = p.Name
        WHERE ISNULL(p.BinCapacity, -2147483648) <> c.ConfigQty;

        -- align APP_PART.BinCapacity to config (changed parts only)
        UPDATE p
           SET p.BinCapacity = cp.NewConfigQty
        FROM dbo.APP_PART p
        JOIN @ChangedParts cp ON cp.PartID = p.PartID;

        -- runner threshold
        DECLARE @min int =
            (SELECT CAST(value AS int)
             FROM dbo.APP_CONFIG
             WHERE configType='System' AND configDescription='BinMin');
        IF @min IS NULL SET @min = 5;

        -- insert bins for the NEW station with current config quantities
        ;WITH cfg AS (
            SELECT configDescription AS PartName, CAST(value AS int) AS ConfigQty
            FROM dbo.APP_CONFIG WHERE configType='Part'
        )
        , parts AS (
            SELECT p.PartID, p.Name, c.ConfigQty
            FROM dbo.APP_PART p
            JOIN cfg c ON c.PartName = p.Name
        )
        INSERT dbo.APP_BIN(StationID, PartID, Quantity, MinQty)
        SELECT @stationId, pr.PartID, pr.ConfigQty, @min
        FROM parts pr
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.APP_BIN b
            WHERE b.StationID=@stationId AND b.PartID=pr.PartID
        );

        -- for S2+ only: update existing stations' bin quantities
        --              but ONLY for parts whose config changed
        IF @stationCount >= 1
        BEGIN
            UPDATE b
               SET b.Quantity = cp.NewConfigQty
            FROM dbo.APP_BIN b
            JOIN @ChangedParts cp ON cp.PartID = b.PartID
            WHERE b.StationID <> @stationId
              AND b.Quantity <> cp.NewConfigQty; 
        END

        COMMIT TRAN;

        SELECT @stationId AS StationID, @code AS StationCode;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        ;THROW;
    END CATCH
END
GO
