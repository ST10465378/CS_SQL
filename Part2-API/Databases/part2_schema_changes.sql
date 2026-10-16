/* Part 2 changes to the Part 1 schema (the brief requires distance and type on every Event).
   EF Core (code-first) creates these columns automatically; this script is for keeping
   docs/raceday.sql in sync. Run it after raceday.sql. */
USE RaceDayDB;
GO
ALTER TABLE dbo.Events ADD
    DistanceKM DECIMAL(6,2)  NOT NULL CONSTRAINT DF_Events_DistanceKM DEFAULT 0,
    EventType  NVARCHAR(20)  NOT NULL CONSTRAINT DF_Events_EventType  DEFAULT 'run';
GO
ALTER TABLE dbo.Events ADD CONSTRAINT CK_Events_EventType CHECK (EventType IN ('run','walk','cycle'));
GO
UPDATE dbo.Events SET DistanceKM = 10, EventType = 'run' WHERE DistanceKM = 0;
GO
