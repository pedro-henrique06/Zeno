CREATE TABLE IF NOT EXISTS Users (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Email VARCHAR(200) NOT NULL,
    PasswordHash VARCHAR(500) NOT NULL,
    Phone VARCHAR(20),
    Document VARCHAR(20),
    BirthDate DATETIME,
    Provider INT NOT NULL DEFAULT 0,
    ProviderId VARCHAR(200),
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME,
    EmailVerified BOOLEAN NOT NULL DEFAULT FALSE
) ENGINE=InnoDB;

CREATE UNIQUE INDEX IX_Users_Email ON Users(Email);

CREATE TABLE IF NOT EXISTS Wallets (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(50) NOT NULL,
    Description VARCHAR(200),
    UserId CHAR(36) NOT NULL,
    Balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    Currency VARCHAR(3) NOT NULL DEFAULT 'BRL',
    DailyBudget DECIMAL(18,2) NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_Wallets_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Accounts (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Bank VARCHAR(100),
    Type VARCHAR(50) NOT NULL DEFAULT 'checking',
    Balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    WalletId CHAR(36) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_Accounts_Wallets FOREIGN KEY (WalletId) REFERENCES Wallets(Id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX IX_Accounts_Wallet ON Accounts(WalletId);

CREATE TABLE IF NOT EXISTS Entries (
    Id CHAR(36) PRIMARY KEY,
    Title VARCHAR(100) NOT NULL,
    Value DECIMAL(18,2) NOT NULL,
    Type INT NOT NULL,
    Kind INT NOT NULL DEFAULT 1,
    Description TEXT NOT NULL,
    Category INT NOT NULL,
    Date DATETIME NOT NULL,
    WalletId CHAR(36) NOT NULL,
    CONSTRAINT FK_Entries_Wallets FOREIGN KEY (WalletId) REFERENCES Wallets(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Homes (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(50) NOT NULL,
    Description VARCHAR(200) NOT NULL,
    SplitMode INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS HomeWallets (
    HomeId CHAR(36) NOT NULL,
    WalletId CHAR(36) NOT NULL,
    CONSTRAINT PK_HomeWallets PRIMARY KEY (HomeId, WalletId),
    CONSTRAINT FK_HomeWallets_Homes FOREIGN KEY (HomeId) REFERENCES Homes(Id),
    CONSTRAINT FK_HomeWallets_Wallets FOREIGN KEY (WalletId) REFERENCES Wallets(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS HomeExpenses (
    Id CHAR(36) PRIMARY KEY,
    HomeId CHAR(36) NOT NULL,
    Title VARCHAR(100) NOT NULL,
    Value DECIMAL(18,2) NOT NULL,
    Category INT NOT NULL,
    Month INT NOT NULL,
    Year INT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_HomeExpenses_Homes FOREIGN KEY (HomeId) REFERENCES Homes(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS RecurrentEntries (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    WalletId CHAR(36) NOT NULL,
    Title VARCHAR(200) NOT NULL,
    Value DECIMAL(18,2) NOT NULL,
    Type INT NOT NULL DEFAULT 1,
    Kind INT NOT NULL DEFAULT 0,
    Category INT NOT NULL DEFAULT 0,
    DayOfMonth INT NOT NULL,
    Active BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt DATETIME NOT NULL,
    LastProcessedAt DATETIME NULL,
    CONSTRAINT FK_RecurrentEntries_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_RecurrentEntries_Wallets FOREIGN KEY (WalletId) REFERENCES Wallets(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS HomeMembers (
    HomeId CHAR(36) NOT NULL,
    UserId CHAR(36) NOT NULL,
    Role INT NOT NULL DEFAULT 1,
    JoinedAt DATETIME NOT NULL,
    CONSTRAINT PK_HomeMembers PRIMARY KEY (HomeId, UserId),
    CONSTRAINT FK_HomeMembers_Homes FOREIGN KEY (HomeId) REFERENCES Homes(Id),
    CONSTRAINT FK_HomeMembers_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS FinancialGoals (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    Name VARCHAR(100) NOT NULL,
    TargetAmount DECIMAL(18,2) NOT NULL,
    CurrentAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    TargetDate DATE NOT NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_FinancialGoals_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Debts (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    Name VARCHAR(100) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    MonthlyPayment DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_Debts_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Categories (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NULL,
    Name VARCHAR(100) NOT NULL,
    Type INT NOT NULL,
    CreatedAt DATETIME NOT NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS CategoryRules (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    Keyword VARCHAR(100) NOT NULL,
    CategoryId CHAR(36) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_CategoryRules_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_CategoryRules_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
) ENGINE=InnoDB;

CREATE INDEX IX_RecurrentEntries_Wallet ON RecurrentEntries(WalletId);
CREATE INDEX IX_RecurrentEntries_Day ON RecurrentEntries(DayOfMonth, Active);
CREATE INDEX IX_FinancialGoals_User ON FinancialGoals(UserId);
CREATE INDEX IX_Debts_User ON Debts(UserId);
CREATE INDEX IX_Categories_UserId ON Categories(UserId);
CREATE INDEX IX_CategoryRules_User ON CategoryRules(UserId);

CREATE TABLE IF NOT EXISTS RefreshTokens (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    Token VARCHAR(500) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    RevokedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX IX_RefreshTokens_User ON RefreshTokens(UserId);
CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
