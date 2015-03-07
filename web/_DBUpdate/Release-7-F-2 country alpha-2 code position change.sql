/*
   sábado, 07 de marzo de 201512:48:14
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
CREATE TABLE dbo.Tmp_country
	(
	CountryID int NOT NULL,
	LanguageID int NOT NULL,
	CountryName varchar(100) NOT NULL,
	CountryCode varchar(3) NOT NULL,
	CountryCodeAlpha2 char(2) NULL,
	CountryCallingCode varchar(3) NULL,
	CreatedDate datetime NULL,
	UpdatedDate datetime NULL,
	ModifiedBy varchar(25) NULL,
	Active bit NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_country SET (LOCK_ESCALATION = TABLE)
GO
IF EXISTS(SELECT * FROM dbo.country)
	 EXEC('INSERT INTO dbo.Tmp_country (CountryID, LanguageID, CountryName, CountryCode, CountryCodeAlpha2, CountryCallingCode, CreatedDate, UpdatedDate, ModifiedBy, Active)
		SELECT CountryID, LanguageID, CountryName, CountryCode, CountryCodeAlpha2, CountryCallingCode, CreatedDate, UpdatedDate, ModifiedBy, Active FROM dbo.country WITH (HOLDLOCK TABLOCKX)')
GO
DROP TABLE dbo.country
GO
EXECUTE sp_rename N'dbo.Tmp_country', N'country', 'OBJECT' 
GO
ALTER TABLE dbo.country ADD CONSTRAINT
	PK__country__BB42E5E768D28DBC PRIMARY KEY CLUSTERED 
	(
	CountryID,
	LanguageID
	) WITH( PAD_INDEX = OFF, FILLFACTOR = 100, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
COMMIT
select Has_Perms_By_Name(N'dbo.country', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.country', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.country', 'Object', 'CONTROL') as Contr_Per 