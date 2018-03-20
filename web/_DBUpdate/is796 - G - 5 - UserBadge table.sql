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
CREATE TABLE dbo.UserBadge
	(
	UserBadgeID int NOT NULL IDENTITY (1, 1),
	UserID int NOT NULL,
	SolutionID int NOT NULL,
	BadgeURL nvarchar(255) NOT NULL,
	Type nvarchar(20) NOT NULL,
	Category nvarchar(50) NOT NULL,
	ExpiryDate datetimeoffset(0) NULL,
	CreatedDate datetimeoffset(0) NOT NULL,
	ModifiedDate datetimeoffset(0) NOT NULL,
	CreatedBy nvarchar(4) NOT NULL,
	ModifiedBy nvarchar(4) NOT NULL,
	Active bit NOT NULL
	)  ON [PRIMARY]
GO
DECLARE @v sql_variant 
SET @v = N'Special value ''user'' means createdy by the userID'
EXECUTE sp_addextendedproperty N'MS_Description', @v, N'SCHEMA', N'dbo', N'TABLE', N'UserBadge', N'COLUMN', N'CreatedBy'
GO
ALTER TABLE dbo.UserBadge ADD CONSTRAINT
	DF_UserBadge_SolutionID DEFAULT -1 FOR SolutionID
GO
ALTER TABLE dbo.UserBadge ADD CONSTRAINT
	DF_UserBadge_Active DEFAULT 1 FOR Active
GO
ALTER TABLE dbo.UserBadge SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.UserBadge', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.UserBadge', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.UserBadge', 'Object', 'CONTROL') as Contr_Per 