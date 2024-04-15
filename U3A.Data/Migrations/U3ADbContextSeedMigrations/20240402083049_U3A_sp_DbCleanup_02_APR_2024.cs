using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3A.Database.Migrations.U3ADbContextSeedMigrations
{
    /// <inheritdoc />
    public partial class U3A_sp_DbCleanup_02_APR_2024 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(
    @"EXECUTE ('

-- =============================================
-- Author:		M Hanlon
-- Create date: 16 March 2024
-- Description:	End of period - database cleanup
-- =============================================
ALTER PROCEDURE [dbo].[prcDbCleanup]
	-- Add the parameters for the stored procedure here
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	declare @Year int
			,@termNumber int
			,@START_OF_EPOCH int
			,@PERSON_DELETE_FLAG datetime
			,@Today datetime
			,@RetainAttendanceForYears int
			,@RetainEnrolmentForYears int
			,@RetainFinancialsForYears int
			,@RetainRegistrationsNeverCompletedForDays int
			,@RetainUnfinancialPersonsForYears int

	set @START_OF_EPOCH = 2020
	set @PERSON_DELETE_FLAG = CAST(''1-jan-1800'' as datetime)
	set @Today = getdate() at time zone ''UTC''
	set @Year = DATEPART(year,@Today)

	select Top 1 @Year=[Year],@termNumber=[TermNumber] from Term where IsDefaultTerm=1
	if (@Year is null) goto endall

	select @RetainAttendanceForYears=[RetainAttendanceForYears]
		  ,@RetainEnrolmentForYears=[RetainEnrolmentForYears]
		  ,@RetainFinancialsForYears=[RetainFinancialsForYears]
		  ,@RetainRegistrationsNeverCompletedForDays=[RetainRegistrationsNeverCompletedForDays]
		  ,@RetainUnfinancialPersonsForYears=[RetainUnfinancialPersonsForYears]
	from [dbo].[SystemSettings]


	delete CourseType
		where CourseType.Discontinued = 1 and CourseType.ID not in (select CourseTypeID from Course)
	delete Venue
		where Venue.Discontinued = 1 and Venue.ID not in (select VenueID from Class)
	delete Enrolment
		where DATEDIFF(YEAR,Created, @Today) > @RetainEnrolmentForYears
	delete Dropout
		where DATEDIFF(YEAR,Created, @Today) > @RetainEnrolmentForYears
	delete Receipt
		where DATEDIFF(YEAR,[Date], @Today) > @RetainFinancialsForYears
	delete Fee
		where DATEDIFF(YEAR,[Date], @Today) > @RetainFinancialsForYears
	delete OnlinePaymentStatus
		where DATEDIFF(YEAR,[CreatedOn], @Today) > @RetainFinancialsForYears
	delete OnlinePaymentStatus
		where DATEDIFF(YEAR,[CreatedOn], @Today) > @RetainFinancialsForYears

	-- persons who have never completed registration
	delete Person
		where FinancialTo = @START_OF_EPOCH
		and DATEDIFF(day,CreatedOn,@Today) > @RetainRegistrationsNeverCompletedForDays

	-- unfinancial persons - set delete flag so we dont lose history
    update Person
		set DateCeased = @PERSON_DELETE_FLAG
        where FinancialTo != @START_OF_EPOCH
				and @year - FinancialTo > @RetainUnfinancialPersonsForYears

	-- delete Term will cascade deletes to Class, AttendClass, Enrolment, Dropout
	delete Term
			where @Year - [Year] > @RetainAttendanceForYears
	
	-- leave allows a null CourseID so delete manually
	delete Leave
		where CourseID in (
			select ID from Course 
			where @Year - [Year] > @RetainAttendanceForYears)
	delete Course
			where @Year - [Year] > @RetainAttendanceForYears
		
	-- delete leaders manually
	update Class
		set LeaderID = null where LeaderID in (
			select ID from Person
			where @Year - [FinancialTo] > @RetainAttendanceForYears)									
	update Class
		set Leader2ID = null where Leader2ID in (
			select ID from Person
			where @Year - [FinancialTo] > @RetainAttendanceForYears)									
	update Class
		set Leader3ID = null where Leader3ID in  (
			select ID from Person
			where @Year - [FinancialTo] > @RetainAttendanceForYears)									
	delete Committee
		where PersonID in  (
			select ID from Person
			where @Year - [FinancialTo] > @RetainAttendanceForYears)									
	delete Volunteer
		where PersonID in  (
			select ID from Person
			where @Year - [FinancialTo] > @RetainAttendanceForYears)

	-- delete Person will cascade deletes to Receipt, Fee, Leave, AttendClass, Enrolment, Dropout
	delete Person  
			where @Year - [FinancialTo] > @RetainAttendanceForYears												

endall:
	return 0
END

                

GO




    ')"
    );

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
