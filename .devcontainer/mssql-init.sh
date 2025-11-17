PRINT 'Running Pre-Deployment Scripts...'
:r ./db/fileit/Script.PreDeployment.sql


USE [FileIt]
GO  

PRINT 'Running SimpleRequestLog.sql...'
:r ./db/fileit/SimpleRequestLog.sql


SELECT * FROM SimpleRequestLog

GO