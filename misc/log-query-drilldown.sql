DECLARE @invocationId UNIQUEIDENTIFIER = 'ae646bcf-58d3-43d4-a6b2-78fe56f01bba';

DECLARE @results TABLE(
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
)
INSERT INTO @results
SELECT Id, Message, Level, Exception, Environment, MachineName, Application, ApplicationVersion, InfrastructureVersion, SourceContext, CorrelationId, InvocationId, EventId, CreatedOn
FROM [FileIt].[dbo].[CommonLog]
WHERE InvocationId = @invocationId

INSERT INTO @results
SELECT Id, Message, Level, Exception, Environment, MachineName, Application, ApplicationVersion,
InfrastructureVersion, SourceContext, CorrelationId, InvocationId, EventId, CreatedOn
FROM [FileIt].[dbo].[CommonLog]
WHERE CorrelationId IN (SELECT CorrelationId FROM @results) 
  AND InvocationId <> @invocationId

INSERT INTO @results
SELECT Id, Message, Level, Exception, Environment, MachineName, Application, ApplicationVersion,
InfrastructureVersion, SourceContext, CorrelationId, InvocationId, EventId, CreatedOn
FROM [FileIt].[dbo].[CommonLog]
WHERE EventId IN (1,2,3) 
  AND Id NOT IN (SELECT Id FROM @results)
  AND InvocationId IN (SELECT InvocationId FROM @results)   

DECLARE @json NVARCHAR(MAX) = (SELECT TOP (1000) [Id]
      ,[Message]
      --,[MessageTemplate]
      ,[Level]
      ,[Exception]
      --,[Properties]
      ,[Environment]
      ,[MachineName]
      ,[Application]
      ,[ApplicationVersion]
      ,[InfrastructureVersion]
      ,[SourceContext]
      ,[CorrelationId]
      ,[InvocationId]
      ,[EventId]
      ,[CreatedOn]
      --,[ModifiedOn]
  FROM @results
  ORDER BY Id ASc
  FOR JSON AUTO);
  SELECT @json AS JsonResult;
    --provide a name for the query result, not the element root