
-- DELETE from CommonLog 
-- where CreatedOn > CAST(GETDATE() as DATE);

select id, Message, Application, CreatedOn from CommonLog 
where CreatedOn >= DATEADD(MINUTE, -30, GETDATE())
order by 1 desc;


