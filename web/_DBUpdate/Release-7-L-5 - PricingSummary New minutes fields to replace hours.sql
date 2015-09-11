/*
   viernes, 11 de septiembre de 201512:32:59
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
ALTER TABLE dbo.pricingSummary
	DROP CONSTRAINT DF__pricinges__Prici__04CFADEC
GO
ALTER TABLE dbo.pricingSummary
	DROP CONSTRAINT DF_pricingestimate_PFeePrice
GO
CREATE TABLE dbo.Tmp_pricingSummary
	(
	PricingSummaryID int NOT NULL,
	PricingSummaryRevision int NOT NULL,
	ServiceDurationHours decimal(7, 2) NULL,
	FirstSessionDurationHours decimal(7, 2) NULL,
	ServiceDurationMinutes int NULL,
	FirstSessionDurationMinutes int NULL,
	SubtotalPrice decimal(7, 2) NULL,
	FeePrice decimal(7, 2) NULL,
	TotalPrice decimal(7, 2) NULL,
	PFeePrice decimal(7, 2) NULL,
	CreatedDate datetime NOT NULL,
	UpdatedDate datetime NOT NULL,
	ModifiedBy varchar(25) NOT NULL,
	Active bit NOT NULL,
	SubtotalRefunded decimal(7, 2) NULL,
	FeeRefunded decimal(7, 2) NULL,
	TotalRefunded decimal(7, 2) NULL,
	DateRefunded datetime NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_pricingSummary SET (LOCK_ESCALATION = TABLE)
GO
DECLARE @v sql_variant 
SET @v = N'Payment processing fees price'
EXECUTE sp_addextendedproperty N'MS_Description', @v, N'SCHEMA', N'dbo', N'TABLE', N'Tmp_pricingSummary', N'COLUMN', N'PFeePrice'
GO
ALTER TABLE dbo.Tmp_pricingSummary ADD CONSTRAINT
	DF__pricinges__Prici__04CFADEC DEFAULT ((1)) FOR PricingSummaryRevision
GO
ALTER TABLE dbo.Tmp_pricingSummary ADD CONSTRAINT
	DF_pricingestimate_PFeePrice DEFAULT ((0)) FOR PFeePrice
GO
IF EXISTS(SELECT * FROM dbo.pricingSummary)
	 EXEC('INSERT INTO dbo.Tmp_pricingSummary (PricingSummaryID, PricingSummaryRevision, ServiceDurationHours, FirstSessionDurationHours, SubtotalPrice, FeePrice, TotalPrice, PFeePrice, CreatedDate, UpdatedDate, ModifiedBy, Active, SubtotalRefunded, FeeRefunded, TotalRefunded, DateRefunded)
		SELECT PricingSummaryID, PricingSummaryRevision, ServiceDurationHours, FirstSessionDurationHours, SubtotalPrice, FeePrice, TotalPrice, PFeePrice, CreatedDate, UpdatedDate, ModifiedBy, Active, SubtotalRefunded, FeeRefunded, TotalRefunded, DateRefunded FROM dbo.pricingSummary WITH (HOLDLOCK TABLOCKX)')
GO
ALTER TABLE dbo.pricingSummary
	DROP CONSTRAINT FK_pricingestimate_pricingestimate
GO
ALTER TABLE dbo.booking
	DROP CONSTRAINT FK__booking__pricingSummary
GO
DROP TABLE dbo.pricingSummary
GO
EXECUTE sp_rename N'dbo.Tmp_pricingSummary', N'pricingSummary', 'OBJECT' 
GO
ALTER TABLE dbo.pricingSummary ADD CONSTRAINT
	PK__pricinge__7F7D375D21D600EE PRIMARY KEY CLUSTERED 
	(
	PricingSummaryID,
	PricingSummaryRevision
	) WITH( PAD_INDEX = OFF, FILLFACTOR = 100, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.pricingSummary ADD CONSTRAINT
	FK_pricingestimate_pricingestimate FOREIGN KEY
	(
	PricingSummaryID,
	PricingSummaryRevision
	) REFERENCES dbo.pricingSummary
	(
	PricingSummaryID,
	PricingSummaryRevision
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
COMMIT
select Has_Perms_By_Name(N'dbo.pricingSummary', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.pricingSummary', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.pricingSummary', 'Object', 'CONTROL') as Contr_Per BEGIN TRANSACTION
GO
ALTER TABLE dbo.booking ADD CONSTRAINT
	FK__booking__pricingSummary FOREIGN KEY
	(
	PricingSummaryID,
	PricingSummaryRevision
	) REFERENCES dbo.pricingSummary
	(
	PricingSummaryID,
	PricingSummaryRevision
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.booking SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.booking', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.booking', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.booking', 'Object', 'CONTROL') as Contr_Per 