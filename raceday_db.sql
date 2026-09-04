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

