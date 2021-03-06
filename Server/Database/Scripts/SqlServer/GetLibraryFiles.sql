DROP PROCEDURE IF EXISTS GetLibraryFiles;
GO

CREATE PROCEDURE GetLibraryFiles
    @IntervalIndex int,
	@Status int
AS

if @Status > 0
begin
select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                         and json_value([Data], '$.Status') = @Status
end
else
begin -- disabled

	declare @libsDisabled as varchar(max)
select @libsDisabled=isnull(STRING_AGG(cast([Uid] as varchar(36)),','), '') from DbObject
where type = 'FileFlows.Shared.Models.Library'
  and json_value([Data],'$.Enabled') = '0'



declare @libsOutOfSchedule as varchar(max)
select @libsOutOfSchedule=isnull(STRING_AGG(cast([Uid] as varchar(36)),','), '') from DbObject
where type = 'FileFlows.Shared.Models.Library'
  and not (
            json_value([Data],'$.Schedule') = null or
            json_value([Data],'$.Schedule') = '' or
            substring(json_value([Data], '$.Schedule'), @IntervalIndex, 1) <> '0'
    )


    if @Status = -2 -- disabled
begin
select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                         and json_value([Data], '$.Status') = 0
                         and charindex(json_value([Data], '$.Library.Uid'), @libsDisabled) > 0
                         and charindex(json_value([Data], '$.Library.Uid'), @libsOutOfSchedule) < 1
end
else if @Status = -1 -- out of schedule
begin
select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                         and json_value([Data], '$.Status') = 0
                         and charindex(json_value([Data], '$.Library.Uid'), @libsDisabled) < 1
                         and charindex(json_value([Data], '$.Library.Uid'), @libsOutOfSchedule) > 1
end
else
begin
select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                         and json_value([Data], '$.Status') = 0
                         and charindex(json_value([Data], '$.Library.Uid'), @libsDisabled) < 1
                         and charindex(json_value([Data], '$.Library.Uid'), @libsOutOfSchedule) < 1
end
end

GO