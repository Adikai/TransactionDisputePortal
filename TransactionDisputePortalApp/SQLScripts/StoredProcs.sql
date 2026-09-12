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
--Author:      Adhil Sewrathan
--DateCreated: 2026-09-10
--Description: Updates the status of a dispute and atomically adjusts account balances
--==============================================================================
CREATE OR ALTER PROCEDURE [dbo].[UpdateDisputeStatus]
    @DisputeID INT,
    @NewStatusID INT,
    @ChangedByStaffID INT = NULL,
    @Notes NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY 
        BEGIN TRANSACTION;

        DECLARE @PreviousStatusID INT;
        DECLARE @DisputedAmount DECIMAL(18, 2);
        DECLARE @AccountID INT;
        DECLARE @OldBalance DECIMAL(18, 2);
        DECLARE @NewBalance DECIMAL(18, 2);
        DECLARE @ApprovedStatusID INT = 3; 


        SELECT 
            @PreviousStatusID = d.DisputeStatusID,
            @DisputedAmount = d.DisputedAmount,
            @AccountID = a.AccountID,
            @OldBalance = a.Balance
        FROM dbo.Disputes d WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.Transactions t ON d.TransactionID = t.TransactionID
        INNER JOIN dbo.Accounts a WITH (UPDLOCK, HOLDLOCK) ON t.AccountID = a.AccountID
        WHERE d.DisputeID = @DisputeID;


        IF @PreviousStatusID IS NULL
        BEGIN
            RAISERROR('Dispute record not found.', 16, 1);
        END


        IF @PreviousStatusID = @NewStatusID
        BEGIN
            COMMIT TRANSACTION;
            RETURN;
        END

        IF @PreviousStatusID <> @ApprovedStatusID AND @NewStatusID = @ApprovedStatusID
        BEGIN
            SET @NewBalance = @OldBalance + @DisputedAmount;

            UPDATE dbo.Accounts
            SET Balance = @NewBalance
            WHERE AccountID = @AccountID;

            INSERT INTO dbo.AccountBalanceAuditLogs (
                AccountID,
                PreviousBalance,
                NewBalance,
                Reason,
                Timestamp
            )
            VALUES (
                @AccountID,
                @OldBalance,
                @NewBalance,
                CONCAT('Dispute #', @DisputeID, ' Approved - Credit Adjustment'),
                SYSDATETIMEOFFSET()
            );
        END


        IF @PreviousStatusID = @ApprovedStatusID AND @NewStatusID <> @ApprovedStatusID
        BEGIN
            SET @NewBalance = @OldBalance - @DisputedAmount;

            UPDATE dbo.Accounts
            SET Balance = @NewBalance
            WHERE AccountID = @AccountID;

            INSERT INTO dbo.AccountBalanceAuditLogs (
                AccountID,
                PreviousBalance,
                NewBalance,
                Reason,
                Timestamp
            )
            VALUES (
                @AccountID,
                @OldBalance,
                @NewBalance,
                CONCAT('Dispute #', @DisputeID, ' Reverted from Approved - Debit Adjustment'),
                SYSDATETIMEOFFSET()
            );
        END

        -- Update Dispute Record
        UPDATE dbo.Disputes
        SET DisputeStatusID = @NewStatusID,
            UpdatedAt = SYSDATETIMEOFFSET()
        WHERE DisputeID = @DisputeID;

        INSERT INTO dbo.DisputeAuditLogs (
            DisputeID,
            PreviousStatusID,
            NewStatusID,
            ChangedByStaffID,
            Notes,
            Timestamp
        )
        VALUES (
            @DisputeID,
            @PreviousStatusID,
            @NewStatusID,
            NULLIF(@ChangedByStaffID, 0),
            @Notes,
            SYSDATETIMEOFFSET()
        );

        COMMIT TRANSACTION;

    END TRY 
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END;
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
    
-- Check Customers
    SELECT 
        c.CustomerID AS UserID,
        c.FirstName,
        c.LastName,
        c.Email,
        'Customer' AS UserRole
    FROM dbo.Customers c WITH(NOLOCK)
    WHERE c.Email = @Email 
      AND c.PasswordHash = @PasswordHash

    UNION ALL

    -- Check Staff Users
    SELECT 
        s.StaffID AS UserID,
        s.FullName AS FirstName,
        '' AS LastName,
        s.Email,
        r.RoleName AS UserRole
    FROM dbo.StaffUsers s WITH(NOLOCK)
    INNER JOIN dbo.Roles r WITH(NOLOCK) 
        ON s.RoleID = r.RoleID
    WHERE s.Email = @Email 
      AND s.PasswordHash = @PasswordHash;

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
CREATE OR ALTER PROCEDURE [dbo].[GetAllDisputes]
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @DisputeStatusID INT = NULL,
    @SearchTerm NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;


    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 OR @PageSize > 100 SET @PageSize = 20;

    SET @SearchTerm = NULLIF(TRIM(@SearchTerm), '');

    SELECT 
        d.DisputeID,
        d.TransactionID,
        t.ReferenceNumber,
        t.MerchantName,
        d.DisputedAmount,
        d.ReasonCategory,
        d.DisputeStatusID,
        ds.StatusName AS DisputeStatus,
        d.CreatedAt AS CreatedDate,
        d.UpdatedAt AS UpdatedDate, 
        COUNT(1) OVER() AS TotalRecords
    FROM dbo.Disputes d WITH (NOLOCK)
    INNER JOIN dbo.Transactions t WITH (NOLOCK) ON d.TransactionID = t.TransactionID
    INNER JOIN dbo.DisputeStatuses ds WITH (NOLOCK) ON d.DisputeStatusID = ds.DisputeStatusID
    WHERE 
        (@DisputeStatusID IS NULL OR @DisputeStatusID = 0 OR d.DisputeStatusID = @DisputeStatusID)
        AND (
            @SearchTerm IS NULL 
            OR t.ReferenceNumber LIKE '%' + @SearchTerm + '%' 
            OR t.MerchantName LIKE '%' + @SearchTerm + '%'
        )
    ORDER BY d.CreatedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-06
--Description:  Gets all disputes for a given customer ID, including transaction and dispute status details
--==============================================================================
CREATE OR ALTER PROCEDURE [dbo].[GetDisputesByCustomerID]
    @CustomerID INT,
    @TopCount INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    SELECT TOP (ISNULL(@TopCount, 2147483647))
        d.DisputeID,
        d.TransactionID,
        d.CustomerID,
        d.DisputeStatusID,
        t.ReferenceNumber,
        t.MerchantName,
        a.AccountNumber,
        ds.StatusName AS DisputeStatus,
        d.ReasonCategory,
        d.CustomerNotes,
        d.DisputedAmount,
        d.CreatedAt AS CreatedDate,
        d.UpdatedAt AS UpdatedDate
    FROM dbo.Disputes d WITH(NOLOCK)
    INNER JOIN dbo.Transactions t WITH(NOLOCK) 
        ON d.TransactionID = t.TransactionID
    INNER JOIN dbo.Accounts a WITH(NOLOCK) 
        ON t.AccountID = a.AccountID
    INNER JOIN dbo.DisputeStatuses ds WITH(NOLOCK) 
        ON d.DisputeStatusID = ds.DisputeStatusID
    WHERE d.CustomerID = @CustomerID
    ORDER BY d.CreatedAt DESC;
END;
GO

--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-10
--Description:  Creates a customer record and returns the new CustomerID
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.CreateCustomer
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @Email NVARCHAR(100),
    @PhoneNumber VARCHAR(20) = NULL,
    @PasswordHash NVARCHAR(255),
    @NewCustomerID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = @Email)
    BEGIN
        RAISERROR('A customer with the email address "%s" already exists.', 16, 1, @Email);
        RETURN;
    END

    INSERT INTO dbo.Customers (FirstName, LastName, Email, PhoneNumber, PasswordHash)
    VALUES (@FirstName, @LastName, @Email, @PhoneNumber, @PasswordHash);

    SET @NewCustomerID = SCOPE_IDENTITY();
END;
GO

--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-10
--Description:  Updates a customer record
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.UpdateCustomer
    @CustomerID INT,
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @Email NVARCHAR(100),
    @PhoneNumber VARCHAR(20) = NULL,
    @PasswordHash NVARCHAR(255) = NUL
AS
BEGIN
    SET NOCOUNT ON;

 
    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerID = @CustomerID)
    BEGIN
        RAISERROR('Customer with ID %d was not found.', 16, 1, @CustomerID);
        RETURN;
    END


    IF EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = @Email AND CustomerID <> @CustomerID)
    BEGIN
        RAISERROR('The email address "%s" is already assigned to another customer.', 16, 1, @Email);
        RETURN;
    END

    UPDATE dbo.Customers
    SET FirstName = @FirstName,
        LastName = @LastName,
        Email = @Email,
        PhoneNumber = @PhoneNumber,
        PasswordHash = COALESCE(@PasswordHash, PasswordHash)
    WHERE CustomerID = @CustomerID;
END;
GO

--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-10
--Description:  Deletes a customer record and all associated data (accounts, transactions, disputes, and audit logs)
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.DeleteCustomer
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerID = @CustomerID)
    BEGIN
        RAISERROR('Customer with ID %d was not found.', 16, 1, @CustomerID);
        RETURN;
    END

    BEGIN TRANSACTION;
    BEGIN TRY

        DELETE FROM dbo.DisputeAuditLogs
        WHERE ChangedByCustomerID = @CustomerID
           OR DisputeID IN (SELECT DisputeID FROM dbo.Disputes WHERE CustomerID = @CustomerID);


        DELETE FROM dbo.Disputes
        WHERE CustomerID = @CustomerID;

        DELETE FROM dbo.AccountBalanceAuditLogs
        WHERE AccountID IN (SELECT AccountID FROM dbo.Accounts WHERE CustomerID = @CustomerID);


        DELETE FROM dbo.Transactions
        WHERE AccountID IN (SELECT AccountID FROM dbo.Accounts WHERE CustomerID = @CustomerID);


        DELETE FROM dbo.Accounts
        WHERE CustomerID = @CustomerID;


        DELETE FROM dbo.Customers
        WHERE CustomerID = @CustomerID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-12
--Description:  Gets all Customers
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.GetCustomers
AS
BEGIN
    SET NOCOUNT ON;

SELECT [CustomerID]
      ,[FirstName]
      ,[LastName]
      ,[Email]
      ,[PhoneNumber]
      ,[PasswordHash]
      ,[CreatedAt]
  FROM [TransactionDispute].[dbo].[Customers] WITH(NOLOCK)
END;
GO
USE TransactionDispute;
GO

--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-12
--Description:  Creates an Account for a given CustomerID and returns the new AccountID
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.CreateAccount
    @CustomerID INT,
    @AccountNumber VARCHAR(20),
    @AccountType NVARCHAR(50),
    @InitialBalance DECIMAL(18, 2) = 0.00,
    @NewAccountID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY

        IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerID = @CustomerID)
        BEGIN
            ;THROW 50001, 'Invalid CustomerID. Customer does not exist.', 1;
        END


        IF EXISTS (SELECT 1 FROM dbo.Accounts WHERE AccountNumber = @AccountNumber)
        BEGIN
            ;THROW 50002, 'An account with this Account Number already exists.', 1;
        END

        BEGIN TRANSACTION;

        INSERT INTO dbo.Accounts (AccountNumber, CustomerID, AccountType, Balance)
        VALUES (@AccountNumber, @CustomerID, @AccountType, @InitialBalance);

        SET @NewAccountID = SCOPE_IDENTITY();

        IF @InitialBalance <> 0.00
        BEGIN
            INSERT INTO dbo.AccountBalanceAuditLogs (AccountID, PreviousBalance, NewBalance, Reason)
            VALUES (@NewAccountID, 0.00, @InitialBalance, 'Initial deposit on account creation');
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-12
--Description:  Updates an Account's details, including AccountType and Balance, with automatic audit logging for balance changes
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.UpdateAccount
    @AccountID INT,
    @AccountType NVARCHAR(50),
    @NewBalance DECIMAL(18, 2),
    @Reason NVARCHAR(250) = 'Admin details update'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        DECLARE @CurrentBalance DECIMAL(18, 2);

        SELECT @CurrentBalance = Balance
        FROM dbo.Accounts
        WHERE AccountID = @AccountID;

        IF @CurrentBalance IS NULL
        BEGIN
            ;THROW 50003, 'Account not found.', 1;
        END

        BEGIN TRANSACTION;

        IF @CurrentBalance <> @NewBalance
        BEGIN
            INSERT INTO dbo.AccountBalanceAuditLogs (AccountID, PreviousBalance, NewBalance, Reason)
            VALUES (@AccountID, @CurrentBalance, @NewBalance, @Reason);
        END

        UPDATE dbo.Accounts
        SET AccountType = @AccountType,
            Balance = @NewBalance
        WHERE AccountID = @AccountID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

--==============================================================================
--Author:  Adhil Sewrathan
--DateCreated: 2026-09-12
--Description:  Deletes an Account and all associated data (transactions, disputes, and audit logs) with proper checks to prevent deletion if linked financial transactions exist
--==============================================================================
CREATE OR ALTER PROCEDURE dbo.DeleteAccount
    @AccountID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE AccountID = @AccountID)
        BEGIN
            ;THROW 50004, 'Account not found.', 1;
        END


        IF EXISTS (SELECT 1 FROM dbo.Transactions WHERE AccountID = @AccountID)
        BEGIN
            ;THROW 50005, 'Cannot delete account because active transaction records are linked to it.', 1;
        END

        BEGIN TRANSACTION;

        DELETE FROM dbo.AccountBalanceAuditLogs
        WHERE AccountID = @AccountID;

        DELETE FROM dbo.Accounts
        WHERE AccountID = @AccountID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO