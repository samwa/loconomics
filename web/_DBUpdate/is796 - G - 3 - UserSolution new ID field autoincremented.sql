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
ALTER TABLE dbo.UserSolution
	DROP CONSTRAINT FK_UserListingSolution_userprofilepositions
GO
ALTER TABLE dbo.userprofilepositions SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.userprofilepositions', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.userprofilepositions', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.userprofilepositions', 'Object', 'CONTROL') as Contr_Per BEGIN TRANSACTION
GO
ALTER TABLE dbo.UserSolution
	DROP CONSTRAINT FK_UserListingSolution_Solution
GO
ALTER TABLE dbo.Solution SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.Solution', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.Solution', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.Solution', 'Object', 'CONTROL') as Contr_Per BEGIN TRANSACTION
GO
ALTER TABLE dbo.UserSolution
	DROP CONSTRAINT FK_UserListingSolution_users
GO
ALTER TABLE dbo.users SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.users', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.users', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.users', 'Object', 'CONTROL') as Contr_Per BEGIN TRANSACTION
GO
ALTER TABLE dbo.UserSolution
	DROP CONSTRAINT DF__UserListi__Modif__1E1B49FA
GO
ALTER TABLE dbo.UserSolution
	DROP CONSTRAINT DF__UserListi__Activ__1F0F6E33
GO
CREATE TABLE dbo.Tmp_UserSolution
	(
	UserSolutionID int NOT NULL IDENTITY (1, 1),
	UserID int NOT NULL,
	UserListingID int NOT NULL,
	SolutionID int NOT NULL,
	LanguageID int NOT NULL,
	CountryID int NOT NULL,
	DisplayRank int NULL,
	CreatedDate datetimeoffset(7) NOT NULL,
	UpdatedDate datetimeoffset(7) NOT NULL,
	ModifiedBy nvarchar(4) NOT NULL,
	Active bit NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_UserSolution SET (LOCK_ESCALATION = TABLE)
GO
ALTER TABLE dbo.Tmp_UserSolution ADD CONSTRAINT
	DF__UserListi__Modif__1E1B49FA DEFAULT ('sys') FOR ModifiedBy
GO
ALTER TABLE dbo.Tmp_UserSolution ADD CONSTRAINT
	DF__UserListi__Activ__1F0F6E33 DEFAULT ((1)) FOR Active
GO
SET IDENTITY_INSERT dbo.Tmp_UserSolution OFF
GO
IF EXISTS(SELECT * FROM dbo.UserSolution)
	 EXEC('INSERT INTO dbo.Tmp_UserSolution (UserID, UserListingID, SolutionID, LanguageID, CountryID, DisplayRank, CreatedDate, UpdatedDate, ModifiedBy, Active)
		SELECT UserID, UserListingID, SolutionID, LanguageID, CountryID, DisplayRank, CreatedDate, UpdatedDate, ModifiedBy, Active FROM dbo.UserSolution WITH (HOLDLOCK TABLOCKX)')
GO
DROP TABLE dbo.UserSolution
GO
EXECUTE sp_rename N'dbo.Tmp_UserSolution', N'UserSolution', 'OBJECT' 
GO
ALTER TABLE dbo.UserSolution ADD CONSTRAINT
	PK__UserList__667F627E1C330188 PRIMARY KEY CLUSTERED 
	(
	UserSolutionID
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.UserSolution ADD CONSTRAINT
	FK_UserListingSolution_users FOREIGN KEY
	(
	UserID
	) REFERENCES dbo.users
	(
	UserID
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.UserSolution ADD CONSTRAINT
	FK_UserListingSolution_Solution FOREIGN KEY
	(
	SolutionID,
	LanguageID,
	CountryID
	) REFERENCES dbo.Solution
	(
	SolutionID,
	LanguageID,
	CountryID
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.UserSolution ADD CONSTRAINT
	FK_UserListingSolution_userprofilepositions FOREIGN KEY
	(
	UserListingID
	) REFERENCES dbo.userprofilepositions
	(
	UserListingID
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
COMMIT
select Has_Perms_By_Name(N'dbo.UserSolution', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.UserSolution', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.UserSolution', 'Object', 'CONTROL') as Contr_Per 