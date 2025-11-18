CREATE OR ALTER PROCEDURE dbo.usp_CreateStationWithBins
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        DECLARE @stationCount int = (SELECT COUNT(*) FROM dbo.APP_STATION); -- get current amount of stations
        
        
        -- figure out the next station number
        DECLARE @nextNo int = 
            ISNULL((
                SELECT MAX(TRY_CAST(SUBSTRING(Code, 2, 10) AS int))
                FROM dbo.APP_STATION
                WHERE Code LIKE 'S%'
            ), 0) + 1;

        DECLARE @code nvarchar(50)  = CONCAT('S', @nextNo);
        DECLARE @name nvarchar(100) = CONCAT('Station ', @nextNo);

       -- create new station row and remember its identity value
        INSERT dbo.APP_STATION(Code, Name, IsActive) VALUES (@code, @name, 1);
        DECLARE @stationId int = SCOPE_IDENTITY();      -- the NEW station

        -- pull current part values from app_config
        ;WITH configParts AS (
            SELECT
                configDescription AS PartName,
                CAST(value AS int) AS ConfigQty
            FROM dbo.APP_CONFIG
            WHERE configType = 'Part'
        )
        -- Ensure all parts exist
        INSERT dbo.APP_PART(Name, BinCapacity)
        SELECT c.PartName, NULL
        FROM configParts c
        WHERE NOT EXISTS (SELECT 1 FROM dbo.APP_PART p WHERE p.Name = c.PartName);

        -- Detect which parts changed vs APP_PART.BinCapacity
        ;WITH configParts AS (
            SELECT
                configDescription AS PartName,
                CAST(value AS int) AS ConfigQty
            FROM dbo.APP_CONFIG
            WHERE configType = 'Part'
        ),
        changed AS (
            SELECT p.PartID, p.Name, p.BinCapacity AS OldQty, c.ConfigQty
            FROM dbo.APP_PART p
            JOIN configParts c ON c.PartName = p.Name
            WHERE ISNULL(p.BinCapacity, -2147483648) <> c.ConfigQty
        )
        -- 1) Update APP_PART.BinCapacity to match config (for changed parts only)
        UPDATE p
           SET p.BinCapacity = c.ConfigQty
        FROM dbo.APP_PART p
        JOIN changed c ON c.PartID = p.PartID;

        ----------------------------------------------------------------------
        -- S1 behavior: just insert bins for the new station with config qty.
        -- S2+ behavior: also UPDATE existing stations' APP_BIN.Quantity
        DECLARE @min int =
            (SELECT CAST(value AS int)
             FROM dbo.APP_CONFIG
             WHERE configType='System' AND configDescription='BinMin');
        IF @min IS NULL SET @min = 5;

        -- Insert bins for the NEW station from current config
        ;WITH configParts AS (
            SELECT configDescription AS PartName, CAST(value AS int) AS ConfigQty
            FROM dbo.APP_CONFIG WHERE configType='Part'
        ),
        parts AS (
            SELECT p.PartID, p.Name, c.ConfigQty
            FROM dbo.APP_PART p
            JOIN configParts c ON c.PartName = p.Name
        )
        INSERT dbo.APP_BIN(StationID, PartID, Quantity, MinQty)
        SELECT @stationId, pr.PartID, pr.ConfigQty, @min
        FROM parts pr
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.APP_BIN b
            WHERE b.StationID = @stationId AND b.PartID = pr.PartID
        );

        -- If this is NOT the first station, push config changes to existing stations' bins
        IF @stationCount >= 1
        BEGIN
            ;WITH configParts AS (
                SELECT configDescription AS PartName, CAST(value AS int) AS ConfigQty
                FROM dbo.APP_CONFIG WHERE configType='Part'
            ),
            changed AS (
                SELECT p.PartID, c.ConfigQty
                FROM dbo.APP_PART p
                JOIN configParts c ON c.PartName = p.Name
                WHERE ISNULL(p.BinCapacity, -2147483648) = c.ConfigQty  -- already updated above
            )
            UPDATE b
               SET b.Quantity = ch.ConfigQty
            FROM dbo.APP_BIN b
            JOIN changed ch ON ch.PartID = b.PartID
            WHERE b.StationID <> @stationId;   -- update ONLY existing stations, not the new one
        END

        COMMIT TRAN;

        -- Return the new station details
        SELECT @stationId AS StationID, @code AS StationCode;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        ;THROW;
    END CATCH
END
