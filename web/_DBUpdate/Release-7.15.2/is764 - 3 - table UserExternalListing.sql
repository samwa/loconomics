/*
   viernes, 02 de febrero de 201818:40:54
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
CREATE TABLE dbo.UserExternalListing
	(
	UserExternalListingID int NOT NULL IDENTITY (1, 1),
	UserID int NOT NULL,
	PlatformID int NOT NULL,
	Title nvarchar(50) NOT NULL,
	JobTitles text NOT NULL,
	Notes text NOT NULL,
	CreatedDate datetimeoffset(0) NOT NULL,
	UpdatedDate datetimeoffset(0) NOT NULL,
	ModifiedBy nvarchar(4) NOT NULL,
	Active bit NOT NULL
	)  ON [PRIMARY]
	 TEXTIMAGE_ON [PRIMARY]
GO
DECLARE @v sql_variant 
SET @v = N'JSON array with { jobTitleID: jobTitleSingularName }'
EXECUTE sp_addextendedproperty N'MS_Description', @v, N'SCHEMA', N'dbo', N'TABLE', N'UserExternalListing', N'COLUMN', N'JobTitles'
GO
ALTER TABLE dbo.UserExternalListing ADD CONSTRAINT
	DF_UserExternalListing_Active DEFAULT 1 FOR Active
GO
ALTER TABLE dbo.UserExternalListing SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.UserExternalListing', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.UserExternalListing', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.UserExternalListing', 'Object', 'CONTROL') as Contr_Per 