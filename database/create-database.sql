/*
    CustomerRegistration.sql
    Complete SQL Server DDL + DML script for CustomerService.
    Creates the database, schemas, tables, constraints, indexes, views,
    stored procedures, and seed data in one executable file.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CustomerServiceDb') IS NULL
BEGIN
    CREATE DATABASE CustomerServiceDb;
END;
GO

USE CustomerServiceDb;
GO

/* =========================================================
   SCHEMAS
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'reg')
    EXEC(N'CREATE SCHEMA reg');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'notify')
    EXEC(N'CREATE SCHEMA notify');
GO


/* =========================================================
   TABLES
   ========================================================= */
IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices
    (
        Id              uniqueidentifier NOT NULL,
        InvoiceNumber   nvarchar(50)      NOT NULL,
        TotalAmount     decimal(18, 2)    NOT NULL,
        CreatedAt       datetime2(7)      NOT NULL,
        CONSTRAINT PK_Invoices PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Invoices_TotalAmount_NonNegative CHECK (TotalAmount >= 0)
    );
END;
GO

IF OBJECT_ID(N'reg.RegistrationApplications', N'U') IS NULL
BEGIN
    CREATE TABLE reg.RegistrationApplications
    (
        Id                      uniqueidentifier NOT NULL,
        Type                    int              NOT NULL,
        Status                  int              NOT NULL,
        CurrentStep             int              NOT NULL,
        Email                   nvarchar(320)    NOT NULL,
        NormalizedEmail         nvarchar(320)    NOT NULL,
        MobileNumber            nvarchar(30)     NOT NULL,
        NormalizedMobileNumber  nvarchar(30)     NOT NULL,
        NationalId              nvarchar(100)    NULL,
        FullName                nvarchar(200)    NULL,
        LegacyCustomerId        nvarchar(100)    NULL,
        EmailVerified           bit              NOT NULL CONSTRAINT DF_RegistrationApplications_EmailVerified DEFAULT (0),
        SmsVerified             bit              NOT NULL CONSTRAINT DF_RegistrationApplications_SmsVerified DEFAULT (0),
        PinHash                 nvarchar(256)    NULL,
        PinSalt                 nvarchar(256)    NULL,
        PinSetUtc               datetime2(7)     NULL,
        FailedPinAttempts       int              NOT NULL CONSTRAINT DF_RegistrationApplications_FailedPinAttempts DEFAULT (0),
        PinLockedUntilUtc       datetime2(7)     NULL,
        ExpiresUtc              datetime2(7)     NOT NULL,
        CancelledUtc            datetime2(7)     NULL,
        CancellationReason      nvarchar(500)    NULL,
        CreatedUtc              datetime2(7)     NOT NULL,
        UpdatedUtc              datetime2(7)     NOT NULL,
        RowVersion              rowversion       NOT NULL,
        CONSTRAINT PK_RegistrationApplications PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_RegistrationApplications_Type CHECK (Type IN (1, 2)),
        CONSTRAINT CK_RegistrationApplications_Status CHECK (Status IN (1, 2, 3, 4)),
        CONSTRAINT CK_RegistrationApplications_CurrentStep CHECK (CurrentStep BETWEEN 1 AND 9),
        CONSTRAINT CK_RegistrationApplications_FailedPinAttempts CHECK (FailedPinAttempts >= 0),
        CONSTRAINT CK_RegistrationApplications_PinPair CHECK
        (
            (PinHash IS NULL AND PinSalt IS NULL AND PinSetUtc IS NULL)
            OR
            (PinHash IS NOT NULL AND PinSalt IS NOT NULL AND PinSetUtc IS NOT NULL)
        )
    );
END;
GO

IF OBJECT_ID(N'reg.OtpChallenges', N'U') IS NULL
BEGIN
    CREATE TABLE reg.OtpChallenges
    (
        Id                      uniqueidentifier NOT NULL,
        RegistrationId          uniqueidentifier NOT NULL,
        Channel                 int              NOT NULL,
        CodeHash                nvarchar(128)    NOT NULL,
        Salt                    nvarchar(128)    NOT NULL,
        ExpiresUtc              datetime2(7)     NOT NULL,
        AttemptCount            int              NOT NULL CONSTRAINT DF_OtpChallenges_AttemptCount DEFAULT (0),
        MaxAttempts             int              NOT NULL,
        VerifiedUtc             datetime2(7)     NULL,
        InvalidatedUtc          datetime2(7)     NULL,
        CreatedUtc              datetime2(7)     NOT NULL,
        NextResendAllowedUtc    datetime2(7)     NOT NULL,
        CONSTRAINT PK_OtpChallenges PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_OtpChallenges_RegistrationApplications
            FOREIGN KEY (RegistrationId)
            REFERENCES reg.RegistrationApplications (Id),
        CONSTRAINT CK_OtpChallenges_Channel CHECK (Channel IN (1, 2)),
        CONSTRAINT CK_OtpChallenges_Attempts CHECK (AttemptCount >= 0 AND MaxAttempts > 0),
        CONSTRAINT CK_OtpChallenges_Expiry CHECK (ExpiresUtc > CreatedUtc)
    );
END;
GO

IF OBJECT_ID(N'reg.OtpVerificationAttempts', N'U') IS NULL
BEGIN
    CREATE TABLE reg.OtpVerificationAttempts
    (
        Id                  uniqueidentifier NOT NULL,
        OtpChallengeId      uniqueidentifier NOT NULL,
        WasSuccessful       bit              NOT NULL,
        FailureReason       nvarchar(500)    NULL,
        IpAddress           nvarchar(64)     NULL,
        UserAgent           nvarchar(500)    NULL,
        SubmittedUtc        datetime2(7)     NOT NULL,
        CONSTRAINT PK_OtpVerificationAttempts PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_OtpVerificationAttempts_OtpChallenges
            FOREIGN KEY (OtpChallengeId)
            REFERENCES reg.OtpChallenges (Id)
    );
END;
GO

IF OBJECT_ID(N'reg.TermDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE reg.TermDocuments
    (
        Id                  uniqueidentifier NOT NULL,
        Code                nvarchar(100)    NOT NULL,
        Title               nvarchar(300)    NOT NULL,
        Content             nvarchar(max)    NOT NULL,
        Version             nvarchar(50)     NOT NULL,
        IsRequired          bit              NOT NULL,
        IsActive            bit              NOT NULL,
        EffectiveFromUtc    datetime2(7)     NOT NULL,
        EffectiveToUtc      datetime2(7)     NULL,
        CreatedUtc          datetime2(7)     NOT NULL,
        UpdatedUtc          datetime2(7)     NOT NULL,
        CONSTRAINT PK_TermDocuments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_TermDocuments_Code_Version UNIQUE (Code, Version),
        CONSTRAINT CK_TermDocuments_EffectiveRange CHECK
        (
            EffectiveToUtc IS NULL OR EffectiveToUtc > EffectiveFromUtc
        )
    );
END;
GO

IF OBJECT_ID(N'reg.RegistrationConsents', N'U') IS NULL
BEGIN
    CREATE TABLE reg.RegistrationConsents
    (
        Id                  uniqueidentifier NOT NULL,
        RegistrationId      uniqueidentifier NOT NULL,
        TermDocumentId      uniqueidentifier NOT NULL,
        TermVersion         nvarchar(50)     NOT NULL,
        Accepted            bit              NOT NULL,
        AcceptedUtc         datetime2(7)     NOT NULL,
        IpAddress           nvarchar(64)     NULL,
        UserAgent           nvarchar(500)    NULL,
        CONSTRAINT PK_RegistrationConsents PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RegistrationConsents_RegistrationApplications
            FOREIGN KEY (RegistrationId)
            REFERENCES reg.RegistrationApplications (Id),
        CONSTRAINT FK_RegistrationConsents_TermDocuments
            FOREIGN KEY (TermDocumentId)
            REFERENCES reg.TermDocuments (Id),
        CONSTRAINT UQ_RegistrationConsents_Registration_Term_Version
            UNIQUE (RegistrationId, TermDocumentId, TermVersion)
    );
END;
GO

IF OBJECT_ID(N'reg.RegistrationStepHistory', N'U') IS NULL
BEGIN
    CREATE TABLE reg.RegistrationStepHistory
    (
        Id                  uniqueidentifier NOT NULL,
        RegistrationId      uniqueidentifier NOT NULL,
        Step                int              NOT NULL,
        Status              nvarchar(50)     NOT NULL,
        OccurredUtc         datetime2(7)     NOT NULL,
        CONSTRAINT PK_RegistrationStepHistory PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RegistrationStepHistory_RegistrationApplications
            FOREIGN KEY (RegistrationId)
            REFERENCES reg.RegistrationApplications (Id),
        CONSTRAINT CK_RegistrationStepHistory_Step CHECK (Step BETWEEN 1 AND 9)
    );
END;
GO

IF OBJECT_ID(N'notify.NotificationTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE notify.NotificationTemplates
    (
        Id                  uniqueidentifier NOT NULL,
        Code                nvarchar(100)    NOT NULL,
        Name                nvarchar(200)    NOT NULL,
        Channel             int              NOT NULL,
        SubjectTemplate     nvarchar(500)    NULL,
        BodyTemplate        nvarchar(max)    NOT NULL,
        IsHtml              bit              NOT NULL,
        IsActive            bit              NOT NULL,
        Version             int              NOT NULL,
        CreatedUtc          datetime2(7)     NOT NULL,
        UpdatedUtc          datetime2(7)     NOT NULL,
        CONSTRAINT PK_NotificationTemplates PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_NotificationTemplates_Channel CHECK (Channel IN (1, 2)),
        CONSTRAINT CK_NotificationTemplates_Version CHECK (Version > 0)
    );
END;
GO

IF OBJECT_ID(N'notify.NotificationDeliveries', N'U') IS NULL
BEGIN
    CREATE TABLE notify.NotificationDeliveries
    (
        Id                  uniqueidentifier NOT NULL,
        RegistrationId      uniqueidentifier NOT NULL,
        OtpChallengeId      uniqueidentifier NULL,
        Channel             int              NOT NULL,
        Destination         nvarchar(320)    NOT NULL,
        TemplateCode        nvarchar(100)    NOT NULL,
        Status              int              NOT NULL,
        AttemptCount        int              NOT NULL,
        ProviderMessageId   nvarchar(200)    NULL,
        FailureReason       nvarchar(2000)   NULL,
        CreatedUtc          datetime2(7)     NOT NULL,
        SentUtc             datetime2(7)     NULL,
        UpdatedUtc          datetime2(7)     NOT NULL,
        CONSTRAINT PK_NotificationDeliveries PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_NotificationDeliveries_RegistrationApplications
            FOREIGN KEY (RegistrationId)
            REFERENCES reg.RegistrationApplications (Id),
        CONSTRAINT FK_NotificationDeliveries_OtpChallenges
            FOREIGN KEY (OtpChallengeId)
            REFERENCES reg.OtpChallenges (Id),
        CONSTRAINT CK_NotificationDeliveries_Channel CHECK (Channel IN (1, 2)),
        CONSTRAINT CK_NotificationDeliveries_Status CHECK (Status IN (1, 2, 3, 4, 5, 6)),
        CONSTRAINT CK_NotificationDeliveries_AttemptCount CHECK (AttemptCount >= 0)
    );
END;
GO


/* =========================================================
   INDEXES
   ========================================================= */



/* =========================================================
   VIEWS
   ========================================================= */
CREATE OR ALTER VIEW dbo.vw_InvoicesSummary
AS
    SELECT
        Id,
        InvoiceNumber,
        TotalAmount,
        CreatedAt
    FROM dbo.Invoices;
GO

/* =========================================================
   STORED PROCEDURES
   ========================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GetInvoicesPaged
    @PageNumber int = 1,
    @PageSize int = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1
        SET @PageNumber = 1;

    IF @PageSize < 1
        SET @PageSize = 20;

    SELECT
        Id,
        InvoiceNumber,
        TotalAmount,
        CreatedAt
    FROM dbo.Invoices
    ORDER BY CreatedAt DESC, Id
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

/* =========================================================
   SEED DATA
   ========================================================= */
DECLARE @NowUtc datetime2(7) = SYSUTCDATETIME();

IF NOT EXISTS
(
    SELECT 1
    FROM reg.TermDocuments
    WHERE Code = N'TERMS_OF_USE'
      AND Version = N'1.0'
)
BEGIN
    INSERT INTO reg.TermDocuments
    (
        Id,
        Code,
        Title,
        Content,
        Version,
        IsRequired,
        IsActive,
        EffectiveFromUtc,
        EffectiveToUtc,
        CreatedUtc,
        UpdatedUtc
    )
    VALUES
    (
        NEWID(),
        N'TERMS_OF_USE',
        N'Terms of Use',
        N'Replace this seed text with the approved Terms of Use content.',
        N'1.0',
        1,
        1,
        @NowUtc,
        NULL,
        @NowUtc,
        @NowUtc
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM reg.TermDocuments
    WHERE Code = N'PRIVACY_POLICY'
      AND Version = N'1.0'
)
BEGIN
    INSERT INTO reg.TermDocuments
    (
        Id,
        Code,
        Title,
        Content,
        Version,
        IsRequired,
        IsActive,
        EffectiveFromUtc,
        EffectiveToUtc,
        CreatedUtc,
        UpdatedUtc
    )
    VALUES
    (
        NEWID(),
        N'PRIVACY_POLICY',
        N'Privacy Policy',
        N'Replace this seed text with the approved Privacy Policy content.',
        N'1.0',
        1,
        1,
        @NowUtc,
        NULL,
        @NowUtc,
        @NowUtc
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM notify.NotificationTemplates
    WHERE Code = N'REGISTRATION_EMAIL_OTP'
      AND Channel = 1
      AND IsActive = 1
)
BEGIN
    INSERT INTO notify.NotificationTemplates
    (
        Id,
        Code,
        Name,
        Channel,
        SubjectTemplate,
        BodyTemplate,
        IsHtml,
        IsActive,
        Version,
        CreatedUtc,
        UpdatedUtc
    )
    VALUES
    (
        NEWID(),
        N'REGISTRATION_EMAIL_OTP',
        N'Registration email OTP',
        1,
        N'Your verification code',
        N'<p>Hello {{FullName}},</p><p>Your code is <strong>{{OtpCode}}</strong>.</p><p>It expires in {{ExpiryMinutes}} minutes.</p>',
        1,
        1,
        1,
        @NowUtc,
        @NowUtc
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM notify.NotificationTemplates
    WHERE Code = N'REGISTRATION_SMS_OTP'
      AND Channel = 2
      AND IsActive = 1
)
BEGIN
    INSERT INTO notify.NotificationTemplates
    (
        Id,
        Code,
        Name,
        Channel,
        SubjectTemplate,
        BodyTemplate,
        IsHtml,
        IsActive,
        Version,
        CreatedUtc,
        UpdatedUtc
    )
    VALUES
    (
        NEWID(),
        N'REGISTRATION_SMS_OTP',
        N'Registration SMS OTP',
        2,
        NULL,
        N'Your verification code is {{OtpCode}}. It expires in {{ExpiryMinutes}} minutes.',
        0,
        1,
        1,
        @NowUtc,
        @NowUtc
    );
END;
GO

PRINT N'CustomerServiceDb database objects and seed data were created successfully.';
GO
