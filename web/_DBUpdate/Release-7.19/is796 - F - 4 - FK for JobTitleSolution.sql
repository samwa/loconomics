/*
   viernes, 09 de marzo de 201817:43:20
   User: 
   Server: ESTUDIO-I3\SQLEXPRESS
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
ALTER TABLE dbo.positions SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.positions', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.positions', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.positions', 'Object', 'CONTROL') as Contr_Per BEGIN TRANSACTION
GO
ALTER TABLE dbo.JobTitleSolution ADD CONSTRAINT
	FK_JobTitleSolution_positions FOREIGN KEY
	(
	JobTitleID,
	LanguageID,
	CountryID
	) REFERENCES dbo.positions
	(
	PositionID,
	LanguageID,
	CountryID
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.JobTitleSolution SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.JobTitleSolution', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.JobTitleSolution', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.JobTitleSolution', 'Object', 'CONTROL') as Contr_Per 