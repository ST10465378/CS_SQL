--create raceday database
CREATE DATABASE RaceDay;
USE RaceDay;

--create tables using the 7 entities from ERD

--table users
CREATE TABLE Users (
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

--create table Organisers
CREATE TABLE Organisers (
    OrganiserID         INT IDENTITY(1,1)  NOT NULL,
    UserID               INT                NOT NULL,
    OrganisationName     NVARCHAR(150)      NOT NULL,
    ContactPhone         NVARCHAR(30)       NULL,
    CONSTRAINT PK_Organisers PRIMARY KEY (OrganiserID),
    CONSTRAINT UQ_Organisers_UserID UNIQUE (UserID),
    CONSTRAINT FK_Organisers_Users FOREIGN KEY (UserID)
);

--create table Participants
CREATE TABLE Participants (
    ParticipantID       INT IDENTITY(1,1)  NOT NULL,
    UserID               INT                NOT NULL,
    DateOfBirth          DATE               NOT NULL,
    EmergencyContact     NVARCHAR(100)      NULL,
    CONSTRAINT PK_Participants PRIMARY KEY (ParticipantID),
    CONSTRAINT UQ_Participants_UserID UNIQUE (UserID),
    CONSTRAINT FK_Participants_Users FOREIGN KEY (UserID)     
);

--create table Events
CREATE TABLE Events (
    EventID         INT IDENTITY(1,1)  NOT NULL,
    OrganiserID      INT                NOT NULL,
    Name             NVARCHAR(150)      NOT NULL,
    EventDate        DATE               NOT NULL,
    Location         NVARCHAR(200)      NOT NULL,
    Description      NVARCHAR(1000)     NULL,
    Status           NVARCHAR(20)       NOT NULL DEFAULT 'draft',
    CONSTRAINT PK_Events PRIMARY KEY (EventID),
    CONSTRAINT FK_Events_Organisers FOREIGN KEY (OrganiserID)    
    CONSTRAINT CK_Events_Status CHECK (Status IN ('draft', 'published', 'cancelled'))
);

-- create table Catergories
CREATE TABLE Categories (
    CategoryID          INT IDENTITY(1,1)  NOT NULL,
    EventID              INT                NOT NULL,
    Name                 NVARCHAR(100)      NOT NULL,
    DistanceKM           DECIMAL(5,2)       NOT NULL,
    MaxParticipants       INT                NOT NULL DEFAULT 0,
    EntryFee             DECIMAL(8,2)       NOT NULL DEFAULT 0,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryID),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID)   
    CONSTRAINT CK_Categories_MaxParticipants CHECK (MaxParticipants >= 0),
    CONSTRAINT CK_Categories_EntryFee CHECK (EntryFee >= 0)
);

--create table Enrolments
CREATE TABLE Enrolments (
    EnrolmentID         INT IDENTITY(1,1)  NOT NULL,
    ParticipantID        INT                NOT NULL,
    CategoryID           INT                NOT NULL,
    EnrolmentDate         DATETIME2          NOT NULL DEFAULT SYSDATETIME(),
    PaymentStatus        NVARCHAR(20)       NOT NULL DEFAULT 'pending',
    BibNumber            INT                NULL,
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentID),
    CONSTRAINT FK_Enrolments_Participants FOREIGN KEY (ParticipantID   
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID)
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantID, CategoryID),
    CONSTRAINT CK_Enrolments_PaymentStatus CHECK (PaymentStatus IN ('pending', 'paid', 'refunded'))
);

--create table Results
CREATE TABLE Results (
    ResultID        INT IDENTITY(1,1)  NOT NULL,
    EnrolmentID      INT                NOT NULL,
    FinishTime       TIME(0)            NOT NULL,
    Position         INT                NULL,
    CONSTRAINT PK_Results PRIMARY KEY (ResultID),
    CONSTRAINT UQ_Results_EnrolmentID UNIQUE (EnrolmentID),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID)        
);

--insert seed data into the 7 tables

-- Users
INSERT INTO Users (FullName, Email, PasswordHash, Role) VALUES
('Thabo Mokoena',   'thabo.mokoena@raceday.co.za',   'HASH_PW_1', 'Organiser'),
('Lindiwe Dube',     'lindiwe.dube@raceday.co.za',    'HASH_PW_2', 'Organiser'),
('Sipho Nkosi',      'sipho.nkosi@example.com',       'HASH_PW_3', 'Participant'),
('Amanda van Wyk',   'amanda.vanwyk@example.com',     'HASH_PW_4', 'Participant');
GO

-- Organisers
INSERT INTO Organisers (UserID, OrganisationName, ContactPhone) VALUES
(1, 'Midrand Runners Club',    '011 555 0101'),
(2, 'Braamfontein Athletics',  '011 555 0202');
GO

