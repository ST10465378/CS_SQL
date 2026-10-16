/* ============================================================
   RaceDay System - Database Schema
   Target: SQL Server (SSMS)
   Matches ERD in /docs/erd.png exactly (7 entities)
   Part 2 update: Events now also has DistanceKM and EventType (run/walk/cycle).
   ============================================================ */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* ------------------------------------------------------------
   Drop tables if they already exist (clean re-run support)
   Order matters: children before parents
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Participants', 'U') IS NOT NULL DROP TABLE dbo.Participants;
IF OBJECT_ID('dbo.Organisers', 'U') IS NOT NULL DROP TABLE dbo.Organisers;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* ============================================================
   TABLE: Users
   ============================================================ */
CREATE TABLE dbo.Users (
    UserID          INT IDENTITY(1,1)   NOT NULL,
    FullName        NVARCHAR(150)       NOT NULL,
    Email           NVARCHAR(255)       NOT NULL,
    PasswordHash    NVARCHAR(255)       NOT NULL,
    Role            NVARCHAR(20)        NOT NULL DEFAULT 'Participant',
    CreatedAt       DATETIME2           NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Users PRIMARY KEY (UserID),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

/* ============================================================
   TABLE: Organisers  (1:1 extension of Users)
   ============================================================ */
CREATE TABLE dbo.Organisers (
    OrganiserID         INT IDENTITY(1,1)  NOT NULL,
    UserID               INT                NOT NULL,
    OrganisationName     NVARCHAR(150)      NOT NULL,
    ContactPhone         NVARCHAR(30)       NULL,
    CONSTRAINT PK_Organisers PRIMARY KEY (OrganiserID),
    CONSTRAINT UQ_Organisers_UserID UNIQUE (UserID),
    CONSTRAINT FK_Organisers_Users FOREIGN KEY (UserID)
        REFERENCES dbo.Users (UserID) ON DELETE CASCADE
);
GO

/* ============================================================
   TABLE: Participants  (1:1 extension of Users)
   ============================================================ */
CREATE TABLE dbo.Participants (
    ParticipantID       INT IDENTITY(1,1)  NOT NULL,
    UserID               INT                NOT NULL,
    DateOfBirth          DATE               NOT NULL,
    EmergencyContact     NVARCHAR(100)      NULL,
    CONSTRAINT PK_Participants PRIMARY KEY (ParticipantID),
    CONSTRAINT UQ_Participants_UserID UNIQUE (UserID),
    CONSTRAINT FK_Participants_Users FOREIGN KEY (UserID)
        REFERENCES dbo.Users (UserID) ON DELETE CASCADE
);
GO

/* ============================================================
   TABLE: Events
   ============================================================ */
CREATE TABLE dbo.Events (
    EventID         INT IDENTITY(1,1)  NOT NULL,
    OrganiserID      INT                NOT NULL,
    Name             NVARCHAR(150)      NOT NULL,
    EventDate        DATE               NOT NULL,
    Location         NVARCHAR(200)      NOT NULL,
    Description      NVARCHAR(1000)     NULL,
    DistanceKM       DECIMAL(6,2)       NOT NULL DEFAULT 0,
    EventType        NVARCHAR(20)       NOT NULL DEFAULT 'run',
    Status           NVARCHAR(20)       NOT NULL DEFAULT 'draft',
    CONSTRAINT PK_Events PRIMARY KEY (EventID),
    CONSTRAINT FK_Events_Organisers FOREIGN KEY (OrganiserID)
        REFERENCES dbo.Organisers (OrganiserID) ON DELETE CASCADE,
    CONSTRAINT CK_Events_Status CHECK (Status IN ('draft', 'published', 'cancelled')),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('run', 'walk', 'cycle')),
    CONSTRAINT CK_Events_DistanceKM CHECK (DistanceKM >= 0)
);
GO

/* ============================================================
   TABLE: Categories
   ============================================================ */
CREATE TABLE dbo.Categories (
    CategoryID          INT IDENTITY(1,1)  NOT NULL,
    EventID              INT                NOT NULL,
    Name                 NVARCHAR(100)      NOT NULL,
    DistanceKM           DECIMAL(5,2)       NOT NULL,
    MaxParticipants       INT                NOT NULL DEFAULT 0,
    EntryFee             DECIMAL(8,2)       NOT NULL DEFAULT 0,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryID),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID)
        REFERENCES dbo.Events (EventID) ON DELETE CASCADE,
    CONSTRAINT CK_Categories_MaxParticipants CHECK (MaxParticipants >= 0),
    CONSTRAINT CK_Categories_EntryFee CHECK (EntryFee >= 0)
);
GO

/* ============================================================
   TABLE: Enrolments
   ============================================================ */
CREATE TABLE dbo.Enrolments (
    EnrolmentID         INT IDENTITY(1,1)  NOT NULL,
    ParticipantID        INT                NOT NULL,
    CategoryID           INT                NOT NULL,
    EnrolmentDate         DATETIME2          NOT NULL DEFAULT SYSDATETIME(),
    PaymentStatus        NVARCHAR(20)       NOT NULL DEFAULT 'pending',
    BibNumber            INT                NULL,
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentID),
    CONSTRAINT FK_Enrolments_Participants FOREIGN KEY (ParticipantID)
        REFERENCES dbo.Participants (ParticipantID) ON DELETE CASCADE,
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID)
        REFERENCES dbo.Categories (CategoryID) ON DELETE NO ACTION,
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantID, CategoryID),
    CONSTRAINT CK_Enrolments_PaymentStatus CHECK (PaymentStatus IN ('pending', 'paid', 'refunded'))
);
GO

/* ============================================================
   TABLE: Results  (1:1 with Enrolments)
   ============================================================ */
CREATE TABLE dbo.Results (
    ResultID        INT IDENTITY(1,1)  NOT NULL,
    EnrolmentID      INT                NOT NULL,
    FinishTime       TIME(0)            NOT NULL,
    Position         INT                NULL,
    CONSTRAINT PK_Results PRIMARY KEY (ResultID),
    CONSTRAINT UQ_Results_EnrolmentID UNIQUE (EnrolmentID),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID)
        REFERENCES dbo.Enrolments (EnrolmentID) ON DELETE CASCADE
);
GO


/* ============================================================
   SEED DATA
   ============================================================ */

-- Users: 2 Organisers + 2 Participants (minimum required)
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role) VALUES
('Thabo Mokoena',   'thabo.mokoena@raceday.co.za',   'HASH_PLACEHOLDER_1', 'Organiser'),
('Lindiwe Dube',     'lindiwe.dube@raceday.co.za',    'HASH_PLACEHOLDER_2', 'Organiser'),
('Sipho Nkosi',      'sipho.nkosi@example.com',       'HASH_PLACEHOLDER_3', 'Participant'),
('Amanda van Wyk',   'amanda.vanwyk@example.com',     'HASH_PLACEHOLDER_4', 'Participant');
GO

-- Organisers
INSERT INTO dbo.Organisers (UserID, OrganisationName, ContactPhone) VALUES
(1, 'Midrand Runners Club',    '011 555 0101'),
(2, 'Braamfontein Athletics',  '011 555 0202');
GO

-- Participants
INSERT INTO dbo.Participants (UserID, DateOfBirth, EmergencyContact) VALUES
(3, '1998-04-12', 'Nomsa Nkosi - 082 555 0303'),
(4, '2001-09-27', 'Pieter van Wyk - 083 555 0404');
GO

-- Events: 3 events (minimum required)
INSERT INTO dbo.Events (OrganiserID, Name, EventDate, Location, Description, DistanceKM, EventType, Status) VALUES
(1, 'Midrand Spring Run',        '2026-10-10', 'Midrand, Gauteng',       'Annual spring road race through Midrand.',        10.00, 'run',   'published'),
(1, 'DynaFrame Night Run',       '2026-11-14', 'Sandton, Gauteng',       'Evening fun run with glow-in-the-dark theme.',     5.00, 'run',   'published'),
(2, 'Braamfontein City Chase',   '2026-12-05', 'Braamfontein, Gauteng',  'Urban trail race through the city.',              21.10, 'run',   'draft');
GO

-- Categories: at least one per event
INSERT INTO dbo.Categories (EventID, Name, DistanceKM, MaxParticipants, EntryFee) VALUES
(1, '5km Fun Run',   5.00,  200, 100.00),
(1, '10km Race',     10.00, 150, 150.00),
(2, '5km Night Run', 5.00,  300, 120.00),
(3, '21km Half Marathon', 21.10, 100, 250.00);
GO

-- Sample enrolments
INSERT INTO dbo.Enrolments (ParticipantID, CategoryID, PaymentStatus, BibNumber) VALUES
(1, 1, 'paid',    101),
(1, 3, 'pending', 102),
(2, 2, 'paid',    201);
GO

-- Sample results (only for enrolments that have completed a race)
INSERT INTO dbo.Results (EnrolmentID, FinishTime, Position) VALUES
(1, '00:28:45', 12),
(3, '00:52:10', 8);
GO

PRINT 'RaceDay database schema created and seeded successfully.';
GO
