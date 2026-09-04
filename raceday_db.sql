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