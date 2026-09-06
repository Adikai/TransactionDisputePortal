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
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
    SELECT 
        t.TransactionID,
        t.AccountID,
        a.AccountNumber,
        t.TransactionDate,
        t.ReferenceNumber,
        t.MerchantName,
        t.TransactionType,
        t.Amount,
        ts.StatusName,
        CAST(CASE WHEN d.DisputeID IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasActiveDispute
    FROM dbo.Transactions t
    INNER JOIN dbo.Accounts a ON t.AccountID = a.AccountID
    INNER JOIN dbo.TransactionStatuses ts ON t.StatusID = ts.StatusID
    LEFT JOIN dbo.Disputes d ON t.TransactionID = d.TransactionID AND d.DisputeStatusID NOT IN (4, 5) -- Exclude Approved/Rejected
    WHERE a.AccountNumber = @AccountNumber
      AND t.TransactionDate >= @StartDate
      AND t.TransactionDate <= @EndDate
    ORDER BY t.TransactionDate DESC;
END;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-08-31
--Description:  Creates a new dispute for a given transaction
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.CreateDispute 
@TransactionID BIGINT,
@CustomerID INT,
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

    SELECT SCOPE_IDENTITY() AS NewDisputeID;
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

    SELECT @@ROWCOUNT AS RowsAffected;

END
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-01
--Description:  Updates the Balance of an account 
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.UpdateAccountBalance
    @AccountID INT,
    @NewBalance DECIMAL(18, 2),
    @Reason NVARCHAR(250) = 'Dispute Adjustment'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @PreviousBalance DECIMAL(18, 2);

    SELECT @PreviousBalance = Balance 
    FROM dbo.Accounts WITH (UPDLOCK, ROWLOCK)
    WHERE AccountID = @AccountID;

    IF @PreviousBalance IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('Account ID not found.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Accounts
    SET Balance = @NewBalance
    WHERE AccountID = @AccountID;

    INSERT INTO dbo.AccountBalanceAuditLogs (AccountID, PreviousBalance, NewBalance, Reason)
    VALUES (@AccountID, @PreviousBalance, @NewBalance, @Reason);

    COMMIT TRANSACTION;

    SELECT @NewBalance AS UpdatedBalance;
END;
GO

--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-05
--Description:  Gets account details per customer ID
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.GetAccountsByCustomerID
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    SELECT 
        A.AccountID,
        A.AccountNumber,
        A.CustomerID,
        A.AccountType,
        A.Balance,
        A.CreatedAt,
        COUNT(D.DisputeID) AS ActiveDisputeCount,
        ISNULL(SUM(D.DisputedAmount), 0.00) AS TotalDisputedAmount
    FROM dbo.Accounts A WITH(NOLOCK)
    LEFT JOIN dbo.Transactions T WITH(NOLOCK) 
        ON A.AccountID = T.AccountID
    LEFT JOIN dbo.Disputes D WITH(NOLOCK) 
        ON T.TransactionID = D.TransactionID 
        AND D.DisputeStatusID IN (1, 2) -- 1: Submitted, 2: Under Review
    WHERE A.CustomerID = @CustomerID
    GROUP BY 
        A.AccountID, 
        A.AccountNumber, 
        A.CustomerID, 
        A.AccountType, 
        A.Balance, 
        A.CreatedAt;
END;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-06
--Description:  Gets customer dashboard metrics per customer ID
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.GetCustomerDashboardMetrics
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    SELECT 
        ISNULL(SUM(A.Balance), 0.00) AS OverallBalance,
        COUNT(DISTINCT A.AccountID) AS TotalAccountsCount,
        COUNT(D.DisputeID) AS OverallActiveDisputeCount,
        ISNULL(SUM(D.DisputedAmount), 0.00) AS OverallDisputedAmount
    FROM dbo.Accounts A WITH(NOLOCK)
    LEFT JOIN dbo.Transactions T WITH(NOLOCK) 
        ON A.AccountID = T.AccountID
    LEFT JOIN dbo.Disputes D WITH(NOLOCK) 
        ON T.TransactionID = D.TransactionID 
        AND D.DisputeStatusID IN (1, 2)
    WHERE A.CustomerID = @CustomerID;
END;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-06
--Description:  Gets recent disputes per customer ID
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.GetRecentDisputesByCustomerID
    @CustomerID INT,
    @TopCount INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    SELECT TOP (@TopCount)
        D.DisputeID,
        T.ReferenceNumber,
        D.ReasonCategory,
        D.DisputedAmount,
        DS.StatusName,
        D.CreatedAt
    FROM dbo.Disputes D WITH(NOLOCK)
    INNER JOIN dbo.Transactions T WITH(NOLOCK) 
        ON D.TransactionID = T.TransactionID
    INNER JOIN dbo.Accounts A WITH(NOLOCK) 
        ON T.AccountID = A.AccountID
    INNER JOIN dbo.DisputeStatuses DS WITH(NOLOCK) 
        ON D.DisputeStatusID = DS.DisputeStatusID
    WHERE A.CustomerID = @CustomerID
    ORDER BY D.CreatedAt DESC;
END;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-06
--Description:  Gets Customer details by email and password
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.GetCustomerDetailsByEmailAndPassword
    @Email NVARCHAR(255),
    @PasswordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
SELECT  CustomerID, 
        FirstName, 
        LastName, 
        Email
FROM dbo.Customers  WITH(NOLOCK)
WHERE Email = @Email AND PasswordHash = @PasswordHash;

END
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-06
--Description:  Gets a single transaction by its ID, including account details and dispute status
--==============================================================================
CREATE OR ALTER PROCEDURE [dbo].[GetTransactionByID]
    @TransactionID BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.TransactionID,
        t.AccountID,
        a.AccountNumber,
        t.TransactionDate,
        t.ReferenceNumber,
        t.MerchantName,
        t.TransactionType,
        t.Amount,
        ts.StatusName,
        CAST(CASE WHEN d.DisputeID IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasActiveDispute
    FROM dbo.Transactions t
    INNER JOIN dbo.Accounts a ON t.AccountID = a.AccountID
    INNER JOIN dbo.TransactionStatuses ts ON t.StatusID = ts.StatusID
    LEFT JOIN dbo.Disputes d ON t.TransactionID = d.TransactionID
    WHERE t.TransactionID = @TransactionID;
END;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-06
--Description:  Gets all disputes for a given customer ID, including transaction and dispute status details
--==============================================================================
CREATE OR ALTER PROCEDURE [dbo].[GetAllDisputesByCustomerID]
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        d.DisputeID,
        d.TransactionID,
        t.ReferenceNumber,
        t.MerchantName,
        d.DisputedAmount,
        d.ReasonCategory,
        ds.StatusName AS DisputeStatus,
        d.CreatedAt AS CreatedDateCreatedAt,
        d.UpdatedAt AS LastUpdatedDate
    FROM dbo.Disputes d
    INNER JOIN dbo.Transactions t ON d.TransactionID = t.TransactionID
    INNER JOIN dbo.DisputeStatuses ds ON d.DisputeStatusID = ds.disputeStatusID
    WHERE d.CustomerID = @CustomerID
    ORDER BY d.CreatedAt DESC;
END;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-06
--Description:  Gets a single dispute by its ID, including transaction and dispute status details
--==============================================================================
CREATE OR ALTER PROCEDURE [dbo].[GetDisputeByID]
    @DisputeID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        d.DisputeID,
        d.TransactionID,
        t.ReferenceNumber,
        t.MerchantName,
        d.DisputedAmount,
        d.ReasonCategory,
        ds.StatusName AS DisputeStatus,
        d.CreatedAt AS CreatedDateCreatedAt,
        d.UpdatedAt AS LastUpdatedDate
    FROM dbo.Disputes d
    INNER JOIN dbo.Transactions t ON d.TransactionID = t.TransactionID
    INNER JOIN dbo.DisputeStatuses ds ON d.DisputeStatusID = ds.disputeStatusID
    WHERE d.DisputeID = @DisputeID
    ORDER BY d.CreatedAt DESC;
END;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-06
--Description:  Gets all accounts for a customer
--==============================================================================
CREATE OR ALTER PROCEDURE [dbo].[GetAccountsByCustomerID]
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.AccountNumber
    FROM dbo.Accounts AS a
    WHERE a.CustomerID = @CustomerID;
END;
GO