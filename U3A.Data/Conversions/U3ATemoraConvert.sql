
declare @CURRENT_TERM int = 2

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
           ,UTCOffset
           ,IncludeMembershipFeeInComplimentary
           )
     VALUES
           (newid()
           ,'U3A Temora Inc.'
           ,''
           ,''
           ,'-'
           ,'' -- ABN Unknown
           ,'enrolments@u3ayemora.org.au'
           ,'https://www.facebook.com/groups/3484849221751641'
           ,''
           ,0
           ,40.00
           ,40.00
           ,40.00
           ,40.00
           ,0.00
           ,0
           ,null
           ,15.00
           ,'Random'
           ,'President' 
             + @CRLF + 'Vice-President'
             + @CRLF + 'Secretary'
             + @CRLF + 'Treasurer'
             + @CRLF + 'Course Coordinator'
             + @CRLF + 'Enrolment Officer'
             + @CRLF + 'Venues Officer'
             + @CRLF + 'Leaders Liaison'
             + @CRLF + 'Publicity Officer'
            ,null
            ,'enrolments@u3atemora.org.au'
            ,'Membership Office'
            ,' '
            ,' '
            ,0
            ,28
            ,7.8
            ,0
            ,10
            ,67.5
            ,10
            ,1
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
           ,2024
           ,1
		   ,cast('2024-Feb-05' as DateTime)
           ,8
           ,7
           ,-7

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
           ,2024
           ,2
		   ,cast('2024-May-06' as DateTime)
           ,8
           ,7
           ,-6
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
           ,2024
           ,3
		   ,cast('2024-Jul-29' as DateTime)
           ,8
           ,7
           ,-5

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
           ,2024
           ,4
		   ,cast('2024-Oct-21' as DateTime)
           ,8
           ,7
           ,-5


UPDATE Term Set IsDefaultTerm = 1 WHERE TermNumber = @CURRENT_TERM
UPDATE Term Set IsClassAllocationFinalised = 1

INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[Comment])
Values (
			newid()
			,0
			,'Activity'
			,''
        )
INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[Comment])
Values (
			newid()
			,0
			,'Art'
			,''
        )
INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[Comment])
Values (
			newid()
			,0
			,'Craft'
			,''
        )
INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[Comment])
Values (
			newid()
			,0
			,'Discussion'
			,''
        )
INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[Comment])
Values (
			newid()
			,0
			,'Learning'
			,''
        )

INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[Comment])
Values (
			newid()
			,0
			,'Games'
			,''
        )
INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[Comment])
Values (
			newid()
			,0
			,'Exercise'
			,''
        )
INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[Comment])
Values (
			newid()
			,0
			,'Social'
			,''
        )
