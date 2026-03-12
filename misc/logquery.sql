select * from CommonLog 
where CreatedOn > CAST(GETDATE() as DATE)
order by 1 desc;