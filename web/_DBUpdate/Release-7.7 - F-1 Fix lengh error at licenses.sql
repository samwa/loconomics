/*
   martes, 03 de enero de 201717:28:21
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
ALTER TABLE dbo.licensecertification
	DROP CONSTRAINT DF__licensece__Langu__5BF880E2
GO
CREATE TABLE dbo.Tmp_licensecertification
	(
	LicenseCertificationID int NOT NULL,
	LicenseCertificationType varchar(100) NOT NULL,
	LicenseCertificationTypeDescription varchar(4000) NULL,
	LicenseCertificationAuthority varchar(500) NULL,
	VerificationWebsiteURL varchar(2078) NULL,
	HowToGetLicensedURL varchar(2078) NULL,
	CreatedDate datetime NOT NULL,
	UpdatedDate datetime NOT NULL,
	ModifiedBy varchar(25) NOT NULL,
	Active bit NOT NULL,
	LanguageID int NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_licensecertification SET (LOCK_ESCALATION = TABLE)
GO
ALTER TABLE dbo.Tmp_licensecertification ADD CONSTRAINT
	DF__licensece__Langu__5BF880E2 DEFAULT ((1)) FOR LanguageID
GO
IF EXISTS(SELECT * FROM dbo.licensecertification)
	 EXEC('INSERT INTO dbo.Tmp_licensecertification (LicenseCertificationID, LicenseCertificationType, LicenseCertificationTypeDescription, LicenseCertificationAuthority, VerificationWebsiteURL, HowToGetLicensedURL, CreatedDate, UpdatedDate, ModifiedBy, Active, LanguageID)
		SELECT LicenseCertificationID, LicenseCertificationType, CONVERT(varchar(4000), LicenseCertificationTypeDescription), LicenseCertificationAuthority, VerificationWebsiteURL, HowToGetLicensedURL, CreatedDate, UpdatedDate, ModifiedBy, Active, LanguageID FROM dbo.licensecertification WITH (HOLDLOCK TABLOCKX)')
GO
DROP TABLE dbo.licensecertification
GO
EXECUTE sp_rename N'dbo.Tmp_licensecertification', N'licensecertification', 'OBJECT' 
GO
ALTER TABLE dbo.licensecertification ADD CONSTRAINT
	PK__licensec__65E993A46F0B5556 PRIMARY KEY CLUSTERED 
	(
	LicenseCertificationID,
	LanguageID
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
COMMIT
select Has_Perms_By_Name(N'dbo.licensecertification', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.licensecertification', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.licensecertification', 'Object', 'CONTROL') as Contr_Per 