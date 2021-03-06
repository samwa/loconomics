/*
   lunes, 03 de abril de 201715:52:39
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
CREATE TABLE dbo.Tmp_UserPaymentPlan
	(
	UserID int NOT NULL,
	SubscriptionID varchar(250) NULL,
	PaymentPlan varchar(25) NULL,
	PaymentMethod varchar(50) NULL,
	PaymentPlanLastChangedDate datetime NULL,
	NextPaymentDueDate datetime NULL,
	NextPaymentAmount money NULL,
	LastPaymentDate datetime NULL,
	LastPaymentAmount money NULL,
	TotalPastDueAmount money NULL,
	FirstBillingDate datetimeoffset(7) NULL,
	SubscriptionEndDate datetimeoffset(7) NULL,
	PaymentMethodToken varchar(250) NULL,
	PaymentExpiryDate datetimeoffset(7) NULL,
	PlanStatus varchar(50) NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_UserPaymentPlan SET (LOCK_ESCALATION = TABLE)
GO
IF EXISTS(SELECT * FROM dbo.UserPaymentPlan)
	 EXEC('INSERT INTO dbo.Tmp_UserPaymentPlan (UserID, SubscriptionID, PaymentPlan, PaymentMethod, PaymentPlanLastChangedDate, NextPaymentDueDate, NextPaymentAmount, LastPaymentDate, LastPaymentAmount, TotalPastDueAmount)
		SELECT UserID, SubscriptionID, PaymentPlan, PaymentMethod, PaymentPlanLastChangedDate, NextPaymentDueDate, NextPaymentAmount, LastPaymentDate, LastPaymentAmount, TotalPastDueAmount FROM dbo.UserPaymentPlan WITH (HOLDLOCK TABLOCKX)')
GO
DROP TABLE dbo.UserPaymentPlan
GO
EXECUTE sp_rename N'dbo.Tmp_UserPaymentPlan', N'UserPaymentPlan', 'OBJECT' 
GO
ALTER TABLE dbo.UserPaymentPlan ADD CONSTRAINT
	PK__Owner__1788CCAC6A7BAA63 PRIMARY KEY CLUSTERED 
	(
	UserID
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
COMMIT
select Has_Perms_By_Name(N'dbo.UserPaymentPlan', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.UserPaymentPlan', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.UserPaymentPlan', 'Object', 'CONTROL') as Contr_Per 