

declare @CURRENT_TERM int = 4

delete SystemSettings
delete Enrolment
delete Leave
delete Term
delete Class
delete Venue
delete Course
delete CourseType
delete Committee
delete receipt
delete ReceiptDataImport
delete Person
delete SendMail
delete Receipt


DECLARE @KEY uniqueidentifier
        ,@VKEY uniqueidentifier
        ,@CRLF char(2)

SET @CRLF = char(13) + char(10)

INSERT INTO [dbo].[SystemSettings]
           ([ID]
           ,[U3AGroup]
           ,[OfficeLocation]
           ,[PostalAddress]
           ,[StreetAddress]
           ,[ABN]
           ,[Email]
           ,[Website]
           ,[Phone]
           ,[AllowedMemberFeePaymentTypes]
           ,[MembershipFee]
           ,[MembershipFeeTerm2]
           ,[MembershipFeeTerm3]
           ,[MembershipFeeTerm4]
           ,[MailSurcharge]
           ,[RequireVaxCertificate]
           ,[CurrentTermID]
           ,[AutoEnrolNewParticipantPercent]
           ,[AutoEnrolRemainderMethod]
           ,[CommitteePositions]
           ,[VolunteerActivities]
           ,SendEmailAddesss
           ,SendEmailDisplayName
           ,BankBSB
           ,BankAccountNo
           ,[MailLabelBottomMargin]
           ,[MailLabelHeight]
           ,[MailLabelLeftMargin]
           ,[MailLabelRightMargin]
           ,[MailLabelTopMargin]
           ,[MailLabelWidth]
           ,TimeZoneId
           ,IncludeMembershipFeeInComplimentary
           ,CeasedReasons
           ,SupportEmailAddesss
           )
     VALUES
           (newid()
           ,'U3A Atherton Tablelands Inc'
           ,'Room 22, Atherton Community Centre'
           ,'PO Box 928, Atherton, QLD 4883'
           ,'42 Mabel Street, Atherton QLD 4833'
           ,'' 
           ,'secretary@athtablands.u3anet.org.au'
           ,'https://athtablands.u3anet.org.au/'
           ,''
           ,0
           ,30.00
           ,30.00
           ,30.00
           ,30.00
           ,0.00
           ,0
           ,null
           ,15.00
           ,'Random'
           ,'President' 
             + @CRLF + 'Vice-President'
             + @CRLF + 'General Secretary'
             + @CRLF + 'Minutes Secretary'
             + @CRLF + 'Treasurer'
             + @CRLF + 'Activity Coordinator'
             + @CRLF + 'Membership Manager'
             + @CRLF + 'Webmaster'
             + @CRLF + 'Publicity Officer'
             + @CRLF + 'Committe Member'
            ,' '
            ,'membership@athtablands.u3anet.org.au'
            ,'Membership Manager'
            ,'633 000'
            ,'161469762'
            ,0
            ,28
            ,7.8
            ,0
            ,10
            ,67.5
            ,'Australia/Brisbane'
            ,1
            ,'Deceased'
                + @CRLF + 'Moved away'
                + @CRLF + 'Health reasons'
                + @CRLF + 'Other'
            ,'membership@athtablands.u3anet.org.au'            
           )

INSERT INTO [dbo].[Term]
           ([ID]
           ,[Year]
           ,[TermNumber]
           ,[StartDate]
           ,[Duration]
           ,[EnrolmentEnds]
           ,[EnrolmentStarts])
     SELECT
           newid()
           ,2025
           ,1
		   ,cast('2025-Feb-03' as DateTime)
           ,12
           ,11
           ,-8

INSERT INTO [dbo].[Term]
           ([ID]
           ,[Year]
           ,[TermNumber]
           ,[StartDate]
           ,[Duration]
           ,[EnrolmentEnds]
           ,[EnrolmentStarts])
     SELECT
           newid()
           ,2025
           ,2
		   ,cast('2025-apr-28' as DateTime)
           ,12
           ,11
           ,-1
INSERT INTO [dbo].[Term]
           ([ID]
           ,[Year]
           ,[TermNumber]
           ,[StartDate]
           ,[Duration]
           ,[EnrolmentEnds]
           ,[EnrolmentStarts])
     SELECT
           newid()
           ,2025
           ,3
		   ,cast('2025-Jul-21' as DateTime)
           ,12
           ,11
           ,-1

INSERT INTO [dbo].[Term]
           ([ID]
           ,[Year]
           ,[TermNumber]
           ,[StartDate]
           ,[Duration]
           ,[EnrolmentEnds]
           ,[EnrolmentStarts])
     SELECT
           newid()
           ,2025
           ,4
		   ,cast('2025-Oct-13' as DateTime)
           ,10
           ,9
           ,-1


UPDATE Term Set IsDefaultTerm = 1 WHERE TermNumber = @CURRENT_TERM
UPDATE Term Set IsClassAllocationFinalised = 1

SET @KEY = newid()
INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[ShortName]
           ,[Comment])
SELECT
			@KEY
			,1
			,'? Unknown ?'
			,'? Unknown ?'
			,''

INSERT INTO [dbo].[Venue]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[MaxNumber]
           ,[Address]
           ,[Comment]
           ,[AccessDetail]
           ,[Coordinator]
           ,[Email]
           ,[Equipment]
           ,[KeyCode]
           ,[Phone])
     SELECT Distinct
			Newid()
           ,0
           ,max(trim(VenueName))
           ,0
           ,''
           ,'Legacy ID: ' + cast([VenueID] as varchar(max))
           ,null
           ,''
           ,''
           ,''
           ,null
           ,''
FROM atherton.dbo.course Group by VenueID



INSERT INTO [dbo].[Person]
           ([ID]
		   ,[ConversionID]
           ,[FirstName]
           ,[LastName]
           ,[Address]
           ,[City]
           ,[State]
           ,[Postcode]
           ,[Gender]
           ,[BirthDate]
           ,[DateJoined]
           ,[DateCeased]
           ,[Email]
           ,[HomePhone]
           ,[Mobile]
           ,[ICEContact]
           ,[ICEPhone]
           ,[IsLifeMember]
           ,[VaxCertificateViewed]
           ,[Communication]
           ,[CreatedOn]
           ,[UpdatedOn]
           ,[FinancialTo]
           ,[FinancialToTerm]
           )
     SELECT
           newid()	    ID
		   ,[member #]			ConversionID
           ,[First Name]
           ,[last Name]
           ,trim(isnull([address1],''))
           ,isnull(suburb,'')
           ,''
           ,case when isnumeric([postcode] + '.0e0') = 1
              then cast(postcode as int)
              else 4883
            end /* case */
           ,Gender
           ,null                Birthdate
           ,[joined date]
           ,null				DateCeased
           ,isnull([email address],'')
           ,isnull(substring([home phone],0,20),'')	HomePhone
           ,isnull(substring(mobile,0,20),'')
           ,'-'
           ,'-'
           ,0 IsLifeMember
           ,0 Covid19
           ,case trim(isnull(m.[email address],''))
                when '' then 'Post'
                else 'Email'
            end as Communication
           ,[CreatedOn] = getDate()
           ,[UpdatedOn] = getDate()
           ,2025 FinancialTo
           ,null as FinancialToTerm
FROM	Atherton.dbo.member m
update person set State = 'QLD'
update person set Gender = 'Female' where substring(Gender,1,1) = 'F'
update person set Gender = 'Male' where substring(Gender,1,1) = 'M'
update person set Gender = 'Other' where substring(Gender,1,1) = ' '



INSERT INTO [dbo].[Course]
           ([ID]
           ,[Year]
		   ,[ConversionID]
           ,[Name]
           ,[Description]
           ,[MaximumStudents]
           ,[CourseTypeID]
           ,[CourseParticipationTypeID]
           ,Duration
           ,RequiredStudents
           )
     SELECT
           newid()			ID
           ,DatePart(year,Getdate())
		   ,cast([ClassID] as int)        ConversionID
           ,name		Name
           ,' '
           ,isnull([MaxStudents],999)
           ,(Select Top 1 ID From CourseType)
           ,0   CourseParticipationTypeID
           ,2
           ,10
FROM	atherton.dbo.course

--INSERT INTO [dbo].[Class]
--           ([ID]
--           ,[LeaderID]
--           ,[OfferedTerm1]
--           ,[OfferedTerm2]
--           ,[OfferedTerm3]
--           ,[OfferedTerm4]
--           ,[StartDate]
--           ,[OccurrenceID]
--           ,[OnDayID]
--           ,[StartTime]
--           ,[CourseID]
--           ,[VenueID])
--SELECT
--           newid()	ID
--           ,(Select Top 1 ID From Person 
--                    where Person.FirstName = trim([Facilitator First Name])
--                            and Person.LastName = trim([Facilitator last Name]))
--            ,1 OfferedTerm1
--            ,1 OfferedTerm2
--            ,1 OfferedTerm3
--            ,1 OfferedTerm4
--           ,null StartDate
--            ,2 OccurrenceID
--           ,case [day]
--                when 'monday' then 1
--                when 'tuesday' then 2
--                when 'wednesday' then 3
--                when 'thursday' then 4
--                when 'friday' then 5
--                when 'saturday' then 6
--                else 0
--            end as OnDayID
--           ,[start time] StartTime
--           ,(Select top 1 ID from Course where trim(Course.name) = trim(bathurst.dbo.courses.name)) CourseID
--           ,isnull((Select Top 1 ID From Venue where trim(Name) = trim(venue)),@VKEY)
--FROM	bathurst.dbo.courses

---- Normalise StartTime to 1st Jan 0001 so we have no sort issues

--update class
--	set StartTime = DateAdd(year,(datepart(year,StartTime)-1)*-1,StartTime)
--where DATEPART(year,StartTime) > 1
--update class
--	set StartTime = DateAdd(month,(datepart(month,StartTime)-1)*-1,StartTime)
--where DATEPART(month,StartTime) > 1
--update class
--	set StartTime = DateAdd(day,(datepart(day,StartTime)-1)*-1,StartTime)
--where DATEPART(day,StartTime) > 1

--INSERT INTO [dbo].[Enrolment]
--           ([ID]
--           ,[TermID]
--           ,[CourseID]
--           ,[ClassID]
--           ,[PersonID]
--           ,[Created]
--           ,[IsWaitlisted]
--           ,[DateEnrolled])
--     SELECT
--           newid()
--           ,(Select Top 1 ID from Term where TermNumber = @CURRENT_TERM)
--           ,(select Top 1 ID from Course where Course.ConversionID = p.[key])
--           ,null
--           ,(select Top 1 ID from Person where Person.ConversionID = m.id)
--           ,getdate()
--           ,0
--           ,getdate()
--FROM	bathurst.dbo.enrolments e Inner Join
--			bathurst.dbo.courses p on e.name = p.name inner join
--			bathurst.dbo.members m on e.[First Name] = m.[first name] and e.[Last Name] = m.[last name]

