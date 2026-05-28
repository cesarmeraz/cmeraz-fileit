CREATE PROCEDURE [dbo].[usp_GetCommonLogDetail]
    @InvocationId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Temporary table to hold results
    DECLARE @results TABLE (
        Id INT,
        Message NVARCHAR(MAX),
        Level NVARCHAR(50),
        Exception NVARCHAR(MAX),
        Environment NVARCHAR(50),
        MachineName NVARCHAR(50),
        Application NVARCHAR(100),
        ApplicationVersion NVARCHAR(50),
        InfrastructureVersion NVARCHAR(50),
        SourceContext NVARCHAR(100),
        CorrelationId UNIQUEIDENTIFIER,
        InvocationId UNIQUEIDENTIFIER,
        EventId INT,
        CreatedOn DATETIME
    );

    -- 1. Insert logs for the specific InvocationId
    INSERT INTO @results
    SELECT 
        Id, Message, Level, Exception, Environment, MachineName, Application,
        ApplicationVersion, InfrastructureVersion, SourceContext, CorrelationId,
        InvocationId, EventName, CreatedOn
    FROM [dbo].[CommonLog]
    WHERE InvocationId = @InvocationId;

    -- 2. Insert logs for related CorrelationIds, excluding the current InvocationId
    INSERT INTO @results
    SELECT 
        Id, Message, Level, Exception, Environment, MachineName, Application,
        ApplicationVersion, InfrastructureVersion, SourceContext, CorrelationId,
        InvocationId, EventName, CreatedOn
    FROM [dbo].[CommonLog]
    WHERE CorrelationId IN (SELECT CorrelationId FROM @results)
      AND InvocationId <> @InvocationId;

    -- 3. Insert logs for FunctionStart events related to existing InvocationIds
    INSERT INTO @results
    SELECT 
        Id, Message, Level, Exception, Environment, MachineName, Application,
        ApplicationVersion, InfrastructureVersion, SourceContext, CorrelationId,
        InvocationId, EventName, CreatedOn
    FROM [dbo].[CommonLog]
    WHERE EventName IN ('FunctionStart') -- TODO: add more as needed
      AND Id NOT IN (SELECT Id FROM @results)
      AND InvocationId IN (SELECT InvocationId FROM @results);

    -- 4. Return top 1000 results ordered by Id ascending
    SELECT TOP (1000)
        Id,
        Message,
        Level,
        Exception,
        Environment,
        MachineName,
        Application,
        ApplicationVersion,
        InfrastructureVersion,
        SourceContext,
        CorrelationId,
        InvocationId,
        EventId,
        CreatedOn
    FROM @results
    ORDER BY Id ASC;
END;
