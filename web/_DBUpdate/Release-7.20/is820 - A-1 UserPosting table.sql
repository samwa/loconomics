/*
   martes, 08 de mayo de 201816:23:49
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
CREATE TABLE dbo.UserPosting
	(
	userPostingID int NOT NULL IDENTITY (1, 1),
	userID int NOT NULL,
	solutionID int NOT NULL,
	postingTemplateID int NULL,
	title nvarchar(50) NOT NULL,
	statusID int NOT NULL,
	neededSpecializationIDs varchar(300) NULL,
	desiredSpecializationIDs varchar(300) NULL,
	languageID int NOT NULL,
	countryID int NOT NULL,
	createdDate datetimeoffset(0) NOT NULL,
	updatedDate datetimeoffset(0) NOT NULL,
	modifiedBy nvarchar(10) NOT NULL
	)  ON [PRIMARY]
GO
DECLARE @v sql_variant 
SET @v = N'Comma separated list of integers (JSON like but don''t need to be surrounded by square brackets)'
EXECUTE sp_addextendedproperty N'MS_Description', @v, N'SCHEMA', N'dbo', N'TABLE', N'UserPosting', N'COLUMN', N'neededSpecializationIDs'
GO
DECLARE @v sql_variant 
SET @v = N'Comma separated list of integers (JSON like but don''t need to be surrounded by square brackets)'
EXECUTE sp_addextendedproperty N'MS_Description', @v, N'SCHEMA', N'dbo', N'TABLE', N'UserPosting', N'COLUMN', N'desiredSpecializationIDs'
GO
ALTER TABLE dbo.UserPosting ADD CONSTRAINT
	PK_UserPosting PRIMARY KEY CLUSTERED 
	(
	userPostingID
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.UserPosting SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.UserPosting', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.UserPosting', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.UserPosting', 'Object', 'CONTROL') as Contr_Per 