CREATE PROCEDURE [dbo].[usp_GetCommonLogSummary]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
         [Id]
        ,[Message]
        ,[MessageTemplate]
        ,[Level]
        ,[Exception]
        ,[Properties]
        ,[Environment]
        ,[MachineName]
        ,[Application]
        ,[ApplicationVersion]
        ,[InfrastructureVersion]
        ,[SourceContext]
        ,[CorrelationId]
        ,[InvocationId]
        ,[EventName]
        ,[CreatedOn]
        ,[ModifiedOn]
    FROM [dbo].[CommonLog]
    WHERE [Application] NOT IN('FileIt.Module.Services.Host', 'FileIt.Module.Ui.Host')
        AND EventName = 'FunctionStart'
    ORDER BY [Id] DESC;
END;
GO
