CREATE OR ALTER PROCEDURE dbo.usp_get_expenses
    @statusName NVARCHAR(50) = NULL,
    @userEmail NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.ExpenseId,
        u.UserName,
        u.Email,
        c.CategoryName,
        s.StatusName,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS Amount,
        e.Currency,
        CAST(e.ExpenseDate AS DATETIME2) AS ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedAt,
        reviewer.UserName AS ReviewedBy
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON u.UserId = e.UserId
    INNER JOIN dbo.ExpenseCategories c ON c.CategoryId = e.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON s.StatusId = e.StatusId
    LEFT JOIN dbo.Users reviewer ON reviewer.UserId = e.ReviewedBy
    WHERE (@statusName IS NULL OR s.StatusName = @statusName)
      AND (@userEmail IS NULL OR u.Email = @userEmail)
    ORDER BY e.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_get_categories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_get_users
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, UserName, Email
    FROM dbo.Users
    WHERE IsActive = 1
    ORDER BY UserName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_create_expense
    @userEmail NVARCHAR(255),
    @categoryName NVARCHAR(100),
    @amount DECIMAL(10,2),
    @expenseDate DATE,
    @description NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @userId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Email = @userEmail);
    DECLARE @categoryId INT = (SELECT TOP 1 CategoryId FROM dbo.ExpenseCategories WHERE CategoryName = @categoryName);
    DECLARE @draftStatusId INT = (SELECT TOP 1 StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft');

    IF @userId IS NULL THROW 50001, 'User not found', 1;
    IF @categoryId IS NULL THROW 50002, 'Category not found', 1;
    IF @draftStatusId IS NULL THROW 50003, 'Draft status not found', 1;

    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, CreatedAt)
    VALUES (@userId, @categoryId, @draftStatusId, CAST(ROUND(@amount * 100, 0) AS INT), 'GBP', @expenseDate, @description, SYSUTCDATETIME());
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_update_expense_status
    @expenseId INT,
    @managerEmail NVARCHAR(255),
    @statusName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @statusId INT = (SELECT TOP 1 StatusId FROM dbo.ExpenseStatus WHERE StatusName = @statusName);
    DECLARE @managerId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Email = @managerEmail);

    IF @statusId IS NULL THROW 50004, 'Status not found', 1;

    UPDATE dbo.Expenses
    SET
        StatusId = @statusId,
        SubmittedAt = CASE WHEN @statusName = 'Submitted' THEN SYSUTCDATETIME() ELSE SubmittedAt END,
        ReviewedBy = CASE WHEN @statusName IN ('Approved', 'Rejected') THEN @managerId ELSE ReviewedBy END,
        ReviewedAt = CASE WHEN @statusName IN ('Approved', 'Rejected') THEN SYSUTCDATETIME() ELSE ReviewedAt END
    WHERE ExpenseId = @expenseId;
END
GO
