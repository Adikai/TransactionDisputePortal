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

-- 4. Idempotent Data Seeding

-- Reference Data
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'DisputeAnalyst')
    INSERT INTO Roles (RoleName) VALUES ('DisputeAnalyst'), ('Admin');

IF NOT EXISTS (SELECT 1 FROM TransactionStatuses WHERE StatusID = 1)
    INSERT INTO TransactionStatuses (StatusID, StatusName) VALUES 
    (1, 'Posted'), (2, 'Pending'), (3, 'Reversed');

IF NOT EXISTS (SELECT 1 FROM DisputeStatuses WHERE DisputeStatusID = 1)
    INSERT INTO DisputeStatuses (DisputeStatusID, StatusName) VALUES 
    (1, 'Submitted'), (2, 'Under Review'), (3, 'Approved'), (4, 'Rejected');

-- Sample Customers & Staff
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Email = 'john.doe@example.com')
    INSERT INTO Customers (FirstName, LastName, Email, PhoneNumber, PasswordHash) 
    VALUES ('John', 'Doe', 'john.doe@example.com', '+27821234567', 'AQAAAAIAAYagAAAAE...'); -- Dummy Hash

IF NOT EXISTS (SELECT 1 FROM StaffUsers WHERE Email = 'analyst@bank.com')
    INSERT INTO StaffUsers (Username, FullName, Email, RoleID, PasswordHash)
    VALUES ('bank_analyst', 'Sarah Connor', 'analyst@bank.com', 1, 'AQAAAAIAAYagAAAAE...');

-- Sample Account & Transactions
IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountNumber = 'ACC-987654321')
    INSERT INTO Accounts (AccountNumber, CustomerID, AccountType, Balance)
    VALUES ('ACC-987654321', 1, 'Checking', 15400.50);

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE ReferenceNumber = 'TXN-2026-001')
BEGIN
    INSERT INTO Transactions (AccountID, MerchantName, TransactionDate, Amount, TransactionType, ReferenceNumber, StatusID)
    VALUES 
    (1, 'Uber Eats', SYSDATETIMEOFFSET(), 249.99, 'Card Purchase', 'TXN-2026-001', 1),
    (1, 'Amazon Web Services', SYSDATETIMEOFFSET(), 1250.00, 'Card Purchase', 'TXN-2026-002', 1),
    (1, 'Unknown International Merchant', SYSDATETIMEOFFSET(), 4500.00, 'Card Purchase', 'TXN-2026-003', 1);
END
GO