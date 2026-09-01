SET QUOTED_IDENTIFIER ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
GO
USE TransactionDispute;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-08-31
--Description:  Gets All transactions for a given account number and date range
--==============================================================================

CREATE OR ALTER PROCEDURE dbo.GetTransactionsByAccountAndDateRange
    @AccountNumber NVARCHAR(20),
    @StartDate DATETIMEOFFSET,
    @EndDate DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  
        T.TransactionID,
        T.AccountID,
        T.MerchantName,
        T.Amount,
        T.TransactionType,
        T.ReferenceNumber,
        S.StatusName,
        T.TransactionDate
    FROM dbo.Transactions T
    INNER JOIN dbo.Accounts A ON T.AccountID = A.AccountID
    INNER JOIN dbo.TransactionStatuses S ON T.StatusID = S.StatusID
    WHERE A.AccountNumber = @AccountNumber
      AND T.TransactionDate >= @StartDate 
      AND T.TransactionDate < @EndDate;
END;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-08-31
--Description:  Creates a new dispute for a given transaction
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.CreateDispute 
@TransactionID BIGINT,
@customerID INT,
@DisputeStatusID INT,
@ReasonCategory NVARCHAR(100),
@CustomerNotes NVARCHAR(1000),
@DisputedAmount DECIMAL(18, 2) 
AS 
BEGIN
SET
    NOCOUNT ON;

INSERT INTO
        dbo.Disputes (
        TransactionID,
        CustomerID,
        DisputeStatusID,
        ReasonCategory,
        CustomerNotes,
        DisputedAmount,
        CreatedAt,
        UpdatedAt
    )
VALUES
    (
        @TransactionID,
        @customerID,
        @DisputeStatusID,
        @ReasonCategory,
        @CustomerNotes,
        @DisputedAmount,
        SYSDATETIMEOFFSET(),
        NULL
    );

END;

GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-01
--Description:  Creates a new dispute for a given transaction
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.InsertAuditLog
@DisputeID INT,
@PreviousStatusID INT,
@NewStatusID INT,
@ChangedByStaffID INT,
@ChangedByCustomerID INT,
@Notes NVARCHAR(1000)

AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.DisputeAuditLogs
           (DisputeID
           ,PreviousStatusID
           ,NewStatusID
           ,ChangedByStaffID
           ,ChangedByCustomerID
           ,Notes
           ,[Timestamp])
     VALUES(
           @DisputeID,
           @PreviousStatusID,
           @NewStatusID,
           @ChangedByStaffID,
           @ChangedByCustomerID,
           @Notes,
           SYSDATETIMEOFFSET())

END
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-01
--Description:  Updates the status of a given dispute
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.UpdateDisputeStatus
@DisputeID INT,
@NewStatusID INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Disputes
    SET DisputeStatusID = @NewStatusID,
    UpdatedAt = SYSDATETIMEOFFSET()
    WHERE DisputeID = @DisputeID
    AND DisputeStatusID <> @NewStatusID;

END
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-01
--Description:  Updates the Balance of an account 
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.UpdateAccountBalance
@AccountID INT,
@NewBalance DECIMAL(18, 2)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Accounts
    SET Balance = @NewBalance
    WHERE AccountID = @AccountID;

END
GO