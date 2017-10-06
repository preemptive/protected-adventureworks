USE [master]

-- NOTE: Subsitute your own path to the protected-adventureworks git repo.
--       These database files will be ignored by git.

RESTORE DATABASE AdventureWorks2014
FROM disk='C:\git\protected-adventureworks\Database\AdventureWorks2014.bak'
WITH MOVE 'AdventureWorks2014_data' TO 'C:\git\protected-adventureworks\Database\AdventureWorks2014.mdf',
MOVE 'AdventureWorks2014_Log' TO 'C:\git\protected-adventureworks\Database\AdventureWorks2014.ldf'
,REPLACE