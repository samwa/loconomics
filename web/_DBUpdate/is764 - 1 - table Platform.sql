/*
   viernes, 02 de febrero de 201818:31:04
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
ALTER TABLE dbo.language SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.language', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.language', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.language', 'Object', 'CONTROL') as Contr_Per BEGIN TRANSACTION
GO
CREATE TABLE dbo.Platform
	(
	PlatformID int NOT NULL,
	Name nvarchar(20) NOT NULL,
	ShortDescription nvarchar(50) NOT NULL,
	LongDescription text NOT NULL,
	FeesDescription text NOT NULL,
	PositiveAspects text NOT NULL,
	NegativeAspects text NOT NULL,
	Advice text NOT NULL,
	SignUpURL nvarchar(255) NOT NULL,
	SignInURL nvarchar(255) NOT NULL,
	LanguageID int NOT NULL,
	CountryID int NOT NULL,
	CreatedDate datetimeoffset(7) NOT NULL,
	UpdatedDate datetimeoffset(0) NOT NULL,
	ModifiedBy nvarchar(4) NOT NULL,
	Active bit NOT NULL
	)  ON [PRIMARY]
	 TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE dbo.Platform ADD CONSTRAINT
	DF_Platform_Active DEFAULT 1 FOR Active
GO
ALTER TABLE dbo.Platform ADD CONSTRAINT
	PK_Platform PRIMARY KEY CLUSTERED 
	(
	PlatformID
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.Platform ADD CONSTRAINT
	FK_Platform_language FOREIGN KEY
	(
	LanguageID,
	CountryID
	) REFERENCES dbo.language
	(
	LanguageID,
	CountryID
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.Platform SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.Platform', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.Platform', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.Platform', 'Object', 'CONTROL') as Contr_Per 