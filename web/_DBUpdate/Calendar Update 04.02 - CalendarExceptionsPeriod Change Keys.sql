/*
   martes, 07 de mayo de 201319:46:12
   User: 
   Server: localhost\SQLEXPRESS
   Database: loconomics
   Application: 
*/

/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.CalendarEventExceptionsPeriod
	DROP CONSTRAINT FK_CalendarEventExceptionsPeriods_CalendarEventExceptionsDates
GO
ALTER TABLE dbo.CalendarEventExceptionsPeriodsList SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.CalendarEventExceptionsPeriodsList', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.CalendarEventExceptionsPeriodsList', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.CalendarEventExceptionsPeriodsList', 'Object', 'CONTROL') as Contr_Per BEGIN TRANSACTION
GO
CREATE TABLE dbo.Tmp_CalendarEventExceptionsPeriod
	(
	IdException int NOT NULL,
	DateStart datetime NOT NULL,
	DateEnd datetime NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_CalendarEventExceptionsPeriod SET (LOCK_ESCALATION = TABLE)
GO
IF EXISTS(SELECT * FROM dbo.CalendarEventExceptionsPeriod)
	 EXEC('INSERT INTO dbo.Tmp_CalendarEventExceptionsPeriod (IdException, DateStart, DateEnd)
		SELECT IdException, DateStart, DateEnd FROM dbo.CalendarEventExceptionsPeriod WITH (HOLDLOCK TABLOCKX)')
GO
DROP TABLE dbo.CalendarEventExceptionsPeriod
GO
EXECUTE sp_rename N'dbo.Tmp_CalendarEventExceptionsPeriod', N'CalendarEventExceptionsPeriod', 'OBJECT' 
GO
ALTER TABLE dbo.CalendarEventExceptionsPeriod ADD CONSTRAINT
	PK_CalendarEventExceptionsPeriod PRIMARY KEY CLUSTERED 
	(
	IdException,
	DateStart
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.CalendarEventExceptionsPeriod ADD CONSTRAINT
	FK_CalendarEventExceptionsPeriods_CalendarEventExceptionsDates FOREIGN KEY
	(
	IdException
	) REFERENCES dbo.CalendarEventExceptionsPeriodsList
	(
	Id
	) ON UPDATE  CASCADE 
	 ON DELETE  CASCADE 
	
GO
COMMIT
select Has_Perms_By_Name(N'dbo.CalendarEventExceptionsPeriod', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.CalendarEventExceptionsPeriod', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.CalendarEventExceptionsPeriod', 'Object', 'CONTROL') as Contr_Per 