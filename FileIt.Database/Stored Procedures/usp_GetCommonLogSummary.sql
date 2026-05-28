CREATE PROCEDURE [dbo].[usp_GetCommonLogSummary]
    @EventName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [Id],
        [Message],
        [Application],
        [InvocationId],
        [EventName],
        [CreatedOn]
    FROM [dbo].[CommonLog]
    WHERE [Application] <> 'FileIt.Module.Services.Host'
        AND [EventName] = @EventName
    ORDER BY [Id] DESC;
END;
