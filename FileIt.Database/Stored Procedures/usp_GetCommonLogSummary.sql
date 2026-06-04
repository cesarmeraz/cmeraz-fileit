CREATE PROCEDURE [dbo].[usp_GetCommonLogSummary]
	@ApplicationName		NVARCHAR(100) = NULL,
	@StartDate			DATETIME2 = NULL,
	@EndDate			DATETIME2 = NULL	
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
         [Id]
        ,[Application]
        ,[InvocationId]
        ,[EventName]
        ,[CreatedOn]        
    FROM [dbo].[CommonLog]
    WHERE [Application] NOT IN('FileIt.Module.Services.Host', 'FileIt.Module.Ui.Host')
        AND EventName = 'FunctionStart'
		AND InvocationId IS NOT NULL
		AND (@ApplicationName IS NULL OR [Application] = @ApplicationName)
		AND (@StartDate IS NULL OR CreatedOn >= @StartDate)
		AND (@EndDate IS NULL OR CreatedOn <= @EndDate)
    ORDER BY [Id] DESC;
END;
GO
