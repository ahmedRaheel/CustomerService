IF DB_ID(N'CustomerServiceDb') IS NULL
    CREATE DATABASE CustomerServiceDb;
GO

USE CustomerServiceDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'reg')
    EXEC('CREATE SCHEMA reg');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'notify')
    EXEC('CREATE SCHEMA notify');
GO

IF OBJECT_ID(N'reg.RegistrationApplications', N'U') IS NULL
BEGIN
    CREATE TABLE reg.RegistrationApplications
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_RegistrationApplications PRIMARY KEY,
        Type int NOT NULL,
        Status int NOT NULL,
        Email nvarchar(320) NOT NULL,
        MobileNumber nvarchar(30) NOT NULL,
        NationalId nvarchar(100) NULL,
        FullName nvarchar(200) NULL,
        EmailVerified bit NOT NULL CONSTRAINT DF_RegistrationApplications_EmailVerified DEFAULT 0,
        SmsVerified bit NOT NULL CONSTRAINT DF_RegistrationApplications_SmsVerified DEFAULT 0,
        PinHash nvarchar(128) NULL,
        PinSalt nvarchar(128) NULL,
        PinSetUtc datetime2 NULL,
        CreatedUtc datetime2 NOT NULL,
        UpdatedUtc datetime2 NOT NULL,
        RowVersion rowversion NOT NULL
    );

    CREATE INDEX IX_RegistrationApplications_Email
        ON reg.RegistrationApplications(Email);
    CREATE INDEX IX_RegistrationApplications_MobileNumber
        ON reg.RegistrationApplications(MobileNumber);
END;
GO

IF COL_LENGTH('reg.RegistrationApplications', 'PinHash') IS NULL
    ALTER TABLE reg.RegistrationApplications ADD PinHash nvarchar(128) NULL;
IF COL_LENGTH('reg.RegistrationApplications', 'PinSalt') IS NULL
    ALTER TABLE reg.RegistrationApplications ADD PinSalt nvarchar(128) NULL;
IF COL_LENGTH('reg.RegistrationApplications', 'PinSetUtc') IS NULL
    ALTER TABLE reg.RegistrationApplications ADD PinSetUtc datetime2 NULL;
GO

IF OBJECT_ID(N'reg.OtpChallenges', N'U') IS NULL
BEGIN
    CREATE TABLE reg.OtpChallenges
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_OtpChallenges PRIMARY KEY,
        RegistrationId uniqueidentifier NOT NULL,
        Channel int NOT NULL,
        CodeHash nvarchar(128) NOT NULL,
        Salt nvarchar(128) NOT NULL,
        ExpiresUtc datetime2 NOT NULL,
        AttemptCount int NOT NULL CONSTRAINT DF_OtpChallenges_AttemptCount DEFAULT 0,
        MaxAttempts int NOT NULL,
        VerifiedUtc datetime2 NULL,
        CreatedUtc datetime2 NOT NULL,
        CONSTRAINT FK_OtpChallenges_RegistrationApplications
            FOREIGN KEY (RegistrationId) REFERENCES reg.RegistrationApplications(Id)
    );

    CREATE INDEX IX_OtpChallenges_Registration_Channel_CreatedUtc
        ON reg.OtpChallenges(RegistrationId, Channel, CreatedUtc DESC);
END;
GO

IF OBJECT_ID(N'notify.NotificationTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE notify.NotificationTemplates
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_NotificationTemplates PRIMARY KEY,
        Code nvarchar(100) NOT NULL,
        Name nvarchar(200) NOT NULL,
        Channel int NOT NULL,
        SubjectTemplate nvarchar(500) NULL,
        BodyTemplate nvarchar(max) NOT NULL,
        IsHtml bit NOT NULL,
        IsActive bit NOT NULL,
        Version int NOT NULL,
        CreatedUtc datetime2 NOT NULL,
        UpdatedUtc datetime2 NOT NULL
    );

    CREATE UNIQUE INDEX UX_NotificationTemplates_Code_Channel_Active
        ON notify.NotificationTemplates(Code, Channel)
        WHERE IsActive = 1;
END;
GO

IF OBJECT_ID(N'notify.NotificationDeliveries', N'U') IS NULL
BEGIN
    CREATE TABLE notify.NotificationDeliveries
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_NotificationDeliveries PRIMARY KEY,
        RegistrationId uniqueidentifier NOT NULL,
        OtpChallengeId uniqueidentifier NOT NULL,
        Channel int NOT NULL,
        Destination nvarchar(320) NOT NULL,
        TemplateCode nvarchar(100) NOT NULL,
        Status int NOT NULL,
        AttemptCount int NOT NULL CONSTRAINT DF_NotificationDeliveries_AttemptCount DEFAULT 0,
        ProviderMessageId nvarchar(200) NULL,
        FailureReason nvarchar(2000) NULL,
        CreatedUtc datetime2 NOT NULL,
        SentUtc datetime2 NULL,
        UpdatedUtc datetime2 NOT NULL,
        CONSTRAINT FK_NotificationDeliveries_RegistrationApplications
            FOREIGN KEY (RegistrationId) REFERENCES reg.RegistrationApplications(Id),
        CONSTRAINT FK_NotificationDeliveries_OtpChallenges
            FOREIGN KEY (OtpChallengeId) REFERENCES reg.OtpChallenges(Id)
    );

    CREATE INDEX IX_NotificationDeliveries_Registration_Channel_CreatedUtc
        ON notify.NotificationDeliveries(RegistrationId, Channel, CreatedUtc DESC);
    CREATE INDEX IX_NotificationDeliveries_Status
        ON notify.NotificationDeliveries(Status);
END;
GO

-- The outbox is no longer used. Delivery attempts are tracked directly in notify.NotificationDeliveries.
IF OBJECT_ID(N'integration.OutboxMessages', N'U') IS NOT NULL
    DROP TABLE integration.OutboxMessages;
GO

DECLARE @Now datetime2 = SYSUTCDATETIME();

IF NOT EXISTS
(
    SELECT 1
    FROM notify.NotificationTemplates
    WHERE Code = 'REGISTRATION_EMAIL_OTP' AND Channel = 1 AND IsActive = 1
)
BEGIN
    INSERT notify.NotificationTemplates
    (
        Id, Code, Name, Channel, SubjectTemplate, BodyTemplate,
        IsHtml, IsActive, Version, CreatedUtc, UpdatedUtc
    )
    VALUES
    (
        NEWID(),
        'REGISTRATION_EMAIL_OTP',
        'Registration email OTP',
        1,
        'Your email verification code',
        '<p>Hello {{FullName}},</p><p>Your email verification code is <strong>{{OtpCode}}</strong>.</p><p>It expires in {{ExpiryMinutes}} minutes.</p>',
        1, 1, 1, @Now, @Now
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM notify.NotificationTemplates
    WHERE Code = 'REGISTRATION_SMS_OTP' AND Channel = 2 AND IsActive = 1
)
BEGIN
    INSERT notify.NotificationTemplates
    (
        Id, Code, Name, Channel, SubjectTemplate, BodyTemplate,
        IsHtml, IsActive, Version, CreatedUtc, UpdatedUtc
    )
    VALUES
    (
        NEWID(),
        'REGISTRATION_SMS_OTP',
        'Registration SMS OTP',
        2,
        NULL,
        'Your verification code is {{OtpCode}}. It expires in {{ExpiryMinutes}} minutes.',
        0, 1, 1, @Now, @Now
    );
END;
GO
