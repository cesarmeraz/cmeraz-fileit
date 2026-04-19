DECLARE @json NVARCHAR(MAX) = (SELECT TOP (1000) [Id]
      --,[Message]
      --,[MessageTemplate]
      --,[Level]
      --,[Exception]
      --,[Properties]
      --,[Environment]
      --,[MachineName]
      ,[Application]
      --,[ApplicationVersion]
      --,[InfrastructureVersion]
      --,[SourceContext]
      --,[CorrelationId]
      ,[InvocationId]
      ,[EventId]
      ,[CreatedOn]
      --,[ModifiedOn]
  FROM [FileIt].[dbo].[CommonLog]
  WHERE [Application] <> 'FileIt.Modules.Services.Host'
    AND EventId=2
  ORDER BY Id DESC
  FOR JSON AUTO);
  SELECT @json AS JsonResult;
    --provide a name for the query result, not the element root