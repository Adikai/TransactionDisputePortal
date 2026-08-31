SET QUOTED_IDENTIFIER ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-08-31
--Description:  Gets All transactions for a given account number and date range
--==============================================================================

CREATE OR ALTER PROCEDURE [dbo].[GetTransactionsByAccountAndDateRange]
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