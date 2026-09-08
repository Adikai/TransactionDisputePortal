-- 1. Database Creation
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'TransactionDispute')
BEGIN
    CREATE DATABASE TransactionDispute;
END
GO

USE TransactionDispute;
GO

-- 2. Lookup & Reference Tables
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE Roles (
        RoleID INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL UNIQUE
    );
END
GO

IF OBJECT_ID(N'dbo.TransactionStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE TransactionStatuses (
        StatusID INT PRIMARY KEY,
        StatusName NVARCHAR(50) NOT NULL UNIQUE
    );
END
GO

IF OBJECT_ID(N'dbo.DisputeStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE DisputeStatuses (
        DisputeStatusID INT PRIMARY KEY,
        StatusName NVARCHAR(50) NOT NULL UNIQUE
    );
END
GO

-- 3. Core Entities
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE Customers (
        CustomerID INT IDENTITY(1,1) PRIMARY KEY,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        PhoneNumber VARCHAR(20) NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
    );
END
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NULL
BEGIN
    CREATE TABLE StaffUsers (
        StaffID INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        RoleID INT NOT NULL, 
        PasswordHash NVARCHAR(255) NOT NULL,
        CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_StaffUsers_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
    );
END
GO

IF OBJECT_ID(N'dbo.Accounts', N'U') IS NULL
BEGIN
    CREATE TABLE Accounts (
        AccountID INT IDENTITY(1,1) PRIMARY KEY,
        AccountNumber VARCHAR(20) NOT NULL UNIQUE,
        CustomerID INT NOT NULL,
        AccountType NVARCHAR(50) NOT NULL, 
        Balance DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
        CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_Accounts_Customers FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
    );
END
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NULL
BEGIN
    CREATE TABLE Transactions (
        TransactionID BIGINT IDENTITY(1,1) PRIMARY KEY,
        AccountID INT NOT NULL,
        MerchantName NVARCHAR(150) NOT NULL,
        TransactionDate DATETIMEOFFSET NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        TransactionType NVARCHAR(50) NOT NULL, 
        ReferenceNumber VARCHAR(50) NOT NULL UNIQUE,
        StatusID INT NOT NULL,
        CONSTRAINT FK_Transactions_Accounts FOREIGN KEY (AccountID) REFERENCES Accounts(AccountID),
        CONSTRAINT FK_Transactions_Status FOREIGN KEY (StatusID) REFERENCES TransactionStatuses(StatusID)
    );
END
GO

IF OBJECT_ID(N'dbo.Disputes', N'U') IS NULL
BEGIN
    CREATE TABLE Disputes (
        DisputeID INT IDENTITY(1,1) PRIMARY KEY,
        TransactionID BIGINT NOT NULL UNIQUE, 
        CustomerID INT NOT NULL,
        DisputeStatusID INT NOT NULL,
        ReasonCategory NVARCHAR(100) NOT NULL, 
        CustomerNotes NVARCHAR(1000) NOT NULL,
        DisputedAmount DECIMAL(18, 2) NOT NULL,
        CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_Disputes_Transactions FOREIGN KEY (TransactionID) REFERENCES Transactions(TransactionID),
        CONSTRAINT FK_Disputes_Customers FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
        CONSTRAINT FK_Disputes_Status FOREIGN KEY (DisputeStatusID) REFERENCES DisputeStatuses(DisputeStatusID)
    );
END
GO

IF OBJECT_ID(N'dbo.DisputeAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE DisputeAuditLogs (
        AuditLogID BIGINT IDENTITY(1,1) PRIMARY KEY,
        DisputeID INT NOT NULL,
        PreviousStatusID INT NULL,
        NewStatusID INT NOT NULL,
        ChangedByStaffID INT NULL,    
        ChangedByCustomerID INT NULL, 
        Notes NVARCHAR(1000) NULL,    
        Timestamp DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_Audit_Disputes FOREIGN KEY (DisputeID) REFERENCES Disputes(DisputeID),
        CONSTRAINT FK_Audit_PrevStatus FOREIGN KEY (PreviousStatusID) REFERENCES DisputeStatuses(DisputeStatusID),
        CONSTRAINT FK_Audit_NewStatus FOREIGN KEY (NewStatusID) REFERENCES DisputeStatuses(DisputeStatusID),
        CONSTRAINT FK_Audit_Staff FOREIGN KEY (ChangedByStaffID) REFERENCES StaffUsers(StaffID),
        CONSTRAINT FK_Audit_Customer FOREIGN KEY (ChangedByCustomerID) REFERENCES Customers(CustomerID)
    );
END
GO

IF OBJECT_ID(N'dbo.AccountBalanceAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE AccountBalanceAuditLogs (
        AuditLogID BIGINT IDENTITY(1,1) PRIMARY KEY,
        AccountID INT NOT NULL,
        PreviousBalance DECIMAL(18, 2) NOT NULL,
        NewBalance DECIMAL(18, 2) NOT NULL,
        Reason NVARCHAR(250) NULL,
        Timestamp DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_BalanceAudit_Accounts FOREIGN KEY (AccountID) REFERENCES Accounts(AccountID)
    );
END
GO

-- 4. Idempotent Data Seeding

-- 4.1. Reference Data
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'DisputeAnalyst')
    INSERT INTO Roles (RoleName) VALUES ('DisputeAnalyst'), ('Admin'),('Customer');

IF NOT EXISTS (SELECT 1 FROM TransactionStatuses WHERE StatusID = 1)
    INSERT INTO TransactionStatuses (StatusID, StatusName) VALUES 
    (1, 'Posted'), 
    (2, 'Pending'), 
    (3, 'Reversed');

IF NOT EXISTS (SELECT 1 FROM DisputeStatuses WHERE DisputeStatusID = 1)
    INSERT INTO DisputeStatuses (DisputeStatusID, StatusName) VALUES 
    (1, 'Submitted'), 
    (2, 'Under Review'), 
    (3, 'Approved'), 
    (4, 'Rejected');

-- 4.2. Sample Customers & Staff
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Email = 'john.doe@example.com')
    INSERT INTO Customers (FirstName, LastName, Email, PhoneNumber, PasswordHash) 
    VALUES ('John', 'Doe', 'john.doe@example.com', '+27821234567', 'AQAAAAIAAYagAAAAE...'); 

IF NOT EXISTS (SELECT 1 FROM Customers WHERE Email = 'jane.smith@example.com')
    INSERT INTO Customers (FirstName, LastName, Email, PhoneNumber, PasswordHash) 
    VALUES ('Jane', 'Smith', 'jane.smith@example.com', '+27839876543', 'AQAAAAIAAYagAAAAE...');

IF NOT EXISTS (SELECT 1 FROM StaffUsers WHERE Email = 'analyst@bank.com')
    INSERT INTO StaffUsers (Username, FullName, Email, RoleID, PasswordHash)
    VALUES ('bank_analyst', 'admin', 'admin', 1, 'admin');

-- 4.3. Accounts Seeding
IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountNumber = 'ACC-987654321')
    INSERT INTO Accounts (AccountNumber, CustomerID, AccountType, Balance)
    VALUES ('ACC-987654321', (SELECT CustomerID FROM Customers WHERE Email = 'john.doe@example.com'), 'Checking', 15400.50);

IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountNumber = 'ACC-112233445')
    INSERT INTO Accounts (AccountNumber, CustomerID, AccountType, Balance)
    VALUES ('ACC-112233445', (SELECT CustomerID FROM Customers WHERE Email = 'john.doe@example.com'), 'Savings', 42850.00);

IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountNumber = 'ACC-556677889')
    INSERT INTO Accounts (AccountNumber, CustomerID, AccountType, Balance)
    VALUES ('ACC-556677889', (SELECT CustomerID FROM Customers WHERE Email = 'john.doe@example.com'), 'Credit Card', 8200.75);

IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountNumber = 'ACC-998877665')
    INSERT INTO Accounts (AccountNumber, CustomerID, AccountType, Balance)
    VALUES ('ACC-998877665', (SELECT CustomerID FROM Customers WHERE Email = 'jane.smith@example.com'), 'Checking', 6450.20);

-- 4.4. Transactions Seeding
-- Customer 1 - Account 1 (Checking)
IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-001')
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES ((SELECT AccountID FROM Accounts WHERE AccountNumber = 'ACC-987654321'), 'Uber Eats', DATEADD(DAY, -1, SYSDATETIMEOFFSET()), 249.99, 'Card Purchase', 'TXN-2026-001', 1);

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-002')
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES ((SELECT AccountID FROM Accounts WHERE AccountNumber = 'ACC-987654321'), 'Amazon Web Services', DATEADD(DAY, -4, SYSDATETIMEOFFSET()), 1250.00, 'Card Purchase', 'TXN-2026-002', 1);

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-003')
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES ((SELECT AccountID FROM Accounts WHERE AccountNumber = 'ACC-987654321'), 'Unknown International Merchant', DATEADD(DAY, -6, SYSDATETIMEOFFSET()), 4500.00, 'Card Purchase', 'TXN-2026-003', 1);

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-004')
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES ((SELECT AccountID FROM Accounts WHERE AccountNumber = 'ACC-987654321'), 'Takealot Online', DATEADD(DAY, -10, SYSDATETIMEOFFSET()), 899.00, 'Card Purchase', 'TXN-2026-004', 1);

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-005')
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES ((SELECT AccountID FROM Accounts WHERE AccountNumber = 'ACC-987654321'), 'Woolworths Food', DATEADD(DAY, -12, SYSDATETIMEOFFSET()), 654.30, 'Card Purchase', 'TXN-2026-005', 1);

-- Customer 1 - Account 2 (Savings)
IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-006')
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES ((SELECT AccountID FROM Accounts WHERE AccountNumber = 'ACC-112233445'), 'Interest Earned', DATEADD(DAY, -15, SYSDATETIMEOFFSET()), 350.00, 'Credit', 'TXN-2026-006', 1);

-- Customer 1 - Account 3 (Credit Card)
IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-007')
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES ((SELECT AccountID FROM Accounts WHERE AccountNumber = 'ACC-556677889'), 'Apple Store Online', DATEADD(DAY, -3, SYSDATETIMEOFFSET()), 18999.00, 'Card Purchase', 'TXN-2026-007', 1);

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-008')
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES ((SELECT AccountID FROM Accounts WHERE AccountNumber = 'ACC-556677889'), 'Phishing Tech Ltd', DATEADD(DAY, -5, SYSDATETIMEOFFSET()), 3200.00, 'Card Purchase', 'TXN-2026-008', 1);

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-009')
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES ((SELECT AccountID FROM Accounts WHERE AccountNumber = 'ACC-556677889'), 'Shell Fuel Station', DATEADD(DAY, -8, SYSDATETIMEOFFSET()), 750.00, 'Card Purchase', 'TXN-2026-009', 1);

-- Customer 2 - Account 4 (Checking)
IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-010')
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES ((SELECT AccountID FROM Accounts WHERE AccountNumber = 'ACC-998877665'), 'Checkers Hyper', DATEADD(DAY, -2, SYSDATETIMEOFFSET()), 1120.50, 'Card Purchase', 'TXN-2026-010', 1);

-- 4.5. Disputes Seeding
-- Dispute 1: Submitted (Customer 1 - Checking Account - TXN-2026-003)
IF NOT EXISTS (SELECT 1 FROM Disputes WHERE TransactionID = (SELECT TransactionID FROM Transactions WHERE ReferenceNumber = 'TXN-2026-003'))
BEGIN
    INSERT INTO Disputes (TransactionID, CustomerID, DisputeStatusID, ReasonCategory, CustomerNotes, DisputedAmount, CreatedAt, UpdatedAt)
    VALUES (
        (SELECT TransactionID FROM Transactions WHERE ReferenceNumber = 'TXN-2026-003'),
        (SELECT CustomerID FROM Customers WHERE Email = 'john.doe@example.com'),
        1, -- Submitted
        'Fraudulent Charge',
        'I did not make or authorize this overseas transaction.',
        4500.00,
        DATEADD(DAY, -5, SYSDATETIMEOFFSET()),
        DATEADD(DAY, -5, SYSDATETIMEOFFSET())
    );
END

-- Dispute 2: Under Review (Customer 1 - Checking Account - TXN-2026-002)
IF NOT EXISTS (SELECT 1 FROM Disputes WHERE TransactionID = (SELECT TransactionID FROM Transactions WHERE ReferenceNumber = 'TXN-2026-002'))
BEGIN
    INSERT INTO Disputes (TransactionID, CustomerID, DisputeStatusID, ReasonCategory, CustomerNotes, DisputedAmount, CreatedAt, UpdatedAt)
    VALUES (
        (SELECT TransactionID FROM Transactions WHERE ReferenceNumber = 'TXN-2026-002'),
        (SELECT CustomerID FROM Customers WHERE Email = 'john.doe@example.com'),
        2, -- Under Review
        'Incorrect Amount',
        'My monthly subscription was billed twice instead of once.',
        1250.00,
        DATEADD(DAY, -3, SYSDATETIMEOFFSET()),
        DATEADD(DAY, -1, SYSDATETIMEOFFSET())
    );
END

-- Dispute 3: Submitted (Customer 1 - Credit Card Account - TXN-2026-008)
IF NOT EXISTS (SELECT 1 FROM Disputes WHERE TransactionID = (SELECT TransactionID FROM Transactions WHERE ReferenceNumber = 'TXN-2026-008'))
BEGIN
    INSERT INTO Disputes (TransactionID, CustomerID, DisputeStatusID, ReasonCategory, CustomerNotes, DisputedAmount, CreatedAt, UpdatedAt)
    VALUES (
        (SELECT TransactionID FROM Transactions WHERE ReferenceNumber = 'TXN-2026-008'),
        (SELECT CustomerID FROM Customers WHERE Email = 'john.doe@example.com'),
        1, -- Submitted
        'Fraudulent Charge',
        'Card details were compromised. Unknown transaction.',
        3200.00,
        DATEADD(DAY, -2, SYSDATETIMEOFFSET()),
        DATEADD(DAY, -2, SYSDATETIMEOFFSET())
    );
END

-- Dispute 4: Approved (Customer 1 - Checking Account - TXN-2026-004)
IF NOT EXISTS (SELECT 1 FROM Disputes WHERE TransactionID = (SELECT TransactionID FROM Transactions WHERE ReferenceNumber = 'TXN-2026-004'))
BEGIN
    INSERT INTO Disputes (TransactionID, CustomerID, DisputeStatusID, ReasonCategory, CustomerNotes, DisputedAmount, CreatedAt, UpdatedAt)
    VALUES (
        (SELECT TransactionID FROM Transactions WHERE ReferenceNumber = 'TXN-2026-004'),
        (SELECT CustomerID FROM Customers WHERE Email = 'john.doe@example.com'),
        3, -- Approved
        'Goods Not Received',
        'Item order was cancelled by seller but charge was not refunded.',
        899.00,
        DATEADD(DAY, -9, SYSDATETIMEOFFSET()),
        DATEADD(DAY, -7, SYSDATETIMEOFFSET())
    );
END

-- 4.6. Dispute Audit Logs
IF NOT EXISTS (SELECT 1 FROM DisputeAuditLogs WHERE Notes = 'Initial dispute submitted by customer.')
BEGIN
    INSERT INTO DisputeAuditLogs (DisputeID, PreviousStatusID, NewStatusID, ChangedByCustomerID, Notes, Timestamp)
    SELECT DisputeID, NULL, 1, CustomerID, 'Initial dispute submitted by customer.', CreatedAt
    FROM Disputes;
END
GO