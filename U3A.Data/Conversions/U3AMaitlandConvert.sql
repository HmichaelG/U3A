
--CREATE FUNCTION [dbo].[StripHTML]
--(@HTMLText VARCHAR(MAX))
--RETURNS VARCHAR(MAX)
--AS
--BEGIN
--    DECLARE @Start INT
--    DECLARE @End INT
--    DECLARE @Length INT
--    SET @Start = CHARINDEX('<', @HTMLText)
    
--    WHILE @Start > 0
--        AND @Start < LEN(@HTMLText)
--        AND CHARINDEX('>', @HTMLText, @Start) > 0
--    BEGIN
--        SET @End = CHARINDEX('>', @HTMLText, @Start)
--        SET @Length = (@End - @Start) + 1
--        IF @Length > 0
--            SET @HTMLText = STUFF(@HTMLText, @Start, @Length, '')
        
--        SET @Start = CHARINDEX('<', @HTMLText)
--    END
--	SET @HTMLText = REPLACE(@HTMLText,'&amp;','and')
--    RETURN @HTMLText
--END;




declare @CURRENT_TERM int = 1

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
           ,'U3A Maitland Inc.'
           ,''
           ,'P.O. Box 502 Maitland. NSW 2320'
           ,'-'
           ,'65 498 713 596' -- ABN Unknown
           ,'enrolments@u3amaitland.org.au'
           ,'https://u3amaitland.org.au/'
           ,'0412 207 890'
           ,1
           ,50.00
           ,50.00
           ,25.00
           ,25.00
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
            ,'enrolments@u3amaitland.org.au'
            ,'Membership Office'
            ,'646 000'
            ,'100061759'
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
SELECT
			newid()
			,0
			,[categoryTitle]
			,categoryDescription
FROM mu3a.u3a.qsycsj_u3a_category

SET @KEY = newid()
INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[Comment])
SELECT
			@KEY
			,1
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
     SELECT
			Newid()
           ,0
           ,venueName
           ,0
           ,street + ' ' + town + ' ' + postcode
           ,' '
           ,null
           ,ContactName
           ,ContactEmail
           ,other
           ,null
           ,ContactPhone
FROM mu3a.u3a.qsycsj_u3a_venues

SET @VKEY = newid()
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
     SELECT
			@VKEY
           ,1
           ,'? Unknown ?'
           ,0
           ,''
		   ,''
           ,''
           ,''
           ,''
           ,''
           ,''
           ,''



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
           newid()				ID
		   ,BadgeNo			ConversionID
           ,firstName
           ,lastName
           ,street
           ,town
           ,'NSW'
           ,postCode
           ,case [Gender]
                when 'M' then 'Male'
                when 'F' then 'Female'
                when 'O' then 'Other'
            end Gender
            ,case dob
                when 0 then null
                else '31-dec-' + cast(dob as char(4))
            end BirthDate
           ,memberSince		DateJoined
           ,null				DateCeased
           ,isnull(email,'')
           ,isnull(phone,'')	HomePhone
           ,isnull(mobile,'')
           ,emergencyContactName ICEContact
           ,emergencyContactMobile + ' ' + emergencyContactPhone              ICEPhone
           ,case DATEPART(year,feesDue)
                when 2100 then 1
                else 0
            end as IsLifeMember
           ,0 Covid19
           ,case preferredMail
                when 'p' then 'Post'
                else 'Email'
            end as Communication
           ,[CreatedOn] = getDate()
           ,[UpdatedOn] = getDate()
           ,DATEPART(year,feesDue) FinancialTo
           ,case DATEPART(month,feesDue)
                when 6 then 2
                else null
            end as FinancialToTerm
FROM	mu3a.u3a.qsycsj_u3a_members where feesDue is not null


--UPDATE Person 
--Set Gender = 'Male'
--WHERE FirstName In ('Liam','Noah','Oliver','Elijah','James','William','Benjamin','Lucas','Henry','Theodore','Jack','Levi','Alexander','Jackson','Mateo','Daniel','Michael','Mason','Sebastian','Ethan','Logan','Owen','Samuel','Jacob','Asher','Aiden','John','Joseph','Wyatt','David','Leo','Luke','Julian','Hudson','Grayson','Matthew','Ezra','Gabriel','Carter','Isaac','Jayden','Luca','Anthony','Dylan','Lincoln','Thomas','Maverick','Elias','Josiah','Charles','Caleb','Christopher','Ezekiel','Miles','Jaxon','Isaiah','Andrew','Joshua','Nathan','Nolan','Adrian','Cameron','Santiago','Eli','Aaron','Ryan','Angel','Cooper','Waylon','Easton','Kai','Christian','Landon','Colton','Roman','Axel','Brooks','Jonathan','Robert','Jameson','Ian','Everett','Greyson','Wesley','Jeremiah','Hunter','Leonardo','Jordan','Jose','Bennett','Silas','Nicholas','Parker','Beau','Weston','Austin','Connor','Carson','Dominic','Xavier','Jaxson','Jace','Emmett','Adam','Declan','Rowan','Micah','Kayden','Gael','River','Ryder','Kingston','Damian','Sawyer','Luka','Evan','Vincent','Legend','Myles','Harrison','August','Bryson','Amir','Giovanni','Chase','Diego','Milo','Jasper','Walker','Jason','Brayden','Cole','Nathaniel','George','Lorenzo','Zion','Luis','Archer','Enzo','Jonah','Thiago','Theo','Ayden','Zachary','Calvin','Braxton','Ashton','Rhett','Atlas','Jude','Bentley','Carlos','Ryker','Adriel','Arthur','Ace','Tyler','Jayce','Max','Elliot','Graham','Kaiden','Maxwell','Juan','Dean','Matteo','Malachi','Ivan','Elliott','Jesus','Emiliano','Messiah','Gavin','Maddox','Camden','Hayden','Leon','Antonio','Justin','Tucker','Brandon','Kevin','Judah','Finn','King','Brody','Xander','Nicolas','Charlie','Arlo','Emmanuel','Barrett','Felix','Alex','Miguel','Abel','Alan','Beckett','Amari','Karter','Timothy','Abraham','Jesse','Zayden','Blake','Alejandro','Dawson','Tristan','Victor','Avery','Joel','Grant','Eric','Patrick','Peter','Richard','Edward','Andres','Emilio','Colt','Knox','Beckham','Adonis','Kyrie','Matias','Oscar','Lukas','Marcus','Hayes','Caden','Remington','Griffin','Nash','Israel','Steven','Holden','Rafael','Zane','Jeremy','Kash','Preston','Kyler','Jax','Jett','Kaleb','Riley','Simon','Phoenix','Javier','Bryce','Louis','Mark','Cash','Lennox','Paxton','Malakai','Paul','Kenneth','Nico','Kaden','Lane','Kairo','Maximus','Omar','Finley','Atticus','Crew','Brantley','Colin','Dallas','Walter','Brady','Callum','Ronan','Hendrix','Jorge','Tobias','Clayton','Emerson','Damien','Zayn','Malcolm','Kayson','Bodhi','Bryan','Aidan','Cohen','Brian','Cayden','Andre','Niko','Maximiliano','Zander','Khalil','Rory','Francisco','Cruz','Kobe','Reid','Daxton','Derek','Martin','Jensen','Karson','Tate','Muhammad','Jaden','Joaquin','Josue','Gideon','Dante','Cody','Bradley','Orion','Spencer','Angelo','Erick','Jaylen','Julius','Manuel','Ellis','Colson','Cairo','Gunner','Wade','Chance','Odin','Anderson','Kane','Raymond','Cristian','Aziel','Prince','Ezequiel','Jake','Otto','Eduardo','Rylan','Ali','Cade','Stephen','Ari','Kameron','Dakota','Warren','Ricardo','Killian','Mario','Romeo','Cyrus','Ismael','Russell','Tyson','Edwin','Desmond','Nasir','Remy','Tanner','Fernando','Hector','Titus','Lawson','Sean','Kyle','Elian','Corbin','Bowen','Wilder','Armani','Royal','Stetson','Briggs','Sullivan','Leonel','Callan','Finnegan','Jay','Zayne','Marshall','Kade','Travis','Sterling','Raiden','Sergio','Tatum','Cesar','Zyaire','Milan','Devin','Gianni','Kamari','Royce','Malik','Jared','Franklin','Clark','Noel','Marco','Archie','Apollo','Pablo','Garrett','Oakley','Memphis','Quinn','Onyx','Alijah','Baylor','Edgar','Nehemiah','Winston','Major','Rhys','Forrest','Jaiden','Reed','Santino','Troy','Caiden','Harvey','Collin','Solomon','Donovan','Damon','Jeffrey','Kason','Sage','Grady','Kendrick','Leland','Luciano','Pedro','Hank','Hugo','Esteban','Johnny','Kashton','Ronin','Ford','Mathias','Porter','Erik','Johnathan','Frank','Tripp','Casey','Fabian','Leonidas','Baker','Matthias','Philip','Jayceon','Kian','Saint','Ibrahim','Jaxton','Augustus','Callen','Trevor','Ruben','Adan','Conor','Dax','Braylen','Kaison','Francis','Kyson','Andy','Lucca','Mack','Peyton','Alexis','Deacon','Kasen','Kamden','Frederick','Princeton','Braylon','Wells','Nikolai','Iker','Bo','Dominick','Moshe','Cassius','Gregory','Lewis','Kieran','Isaias','Seth','Marcos','Omari','Shane','Keegan','Jase','Asa','Sonny','Uriel','Pierce','Jasiah','Eden','Rocco','Banks','Cannon','Denver','Zaiden','Roberto','Shawn','Drew','Emanuel','Kolton','Ayaan','Ares','Conner','Jalen','Alonzo','Enrique','Dalton','Moses','Koda','Bodie','Jamison','Phillip','Zaire','Jonas','Kylo','Moises','Shepherd','Allen','Kenzo','Mohamed','Keanu','Dexter','Conrad','Bruce','Sylas','Soren','Raphael','Rowen','Gunnar','Sutton','Quentin','Jaziel','Emmitt','Makai','Koa','Maximilian','Brixton','Dariel','Zachariah','Roy','Armando','Corey','Saul','Izaiah','Danny','Davis','Ridge','Yusuf','Ariel','Valentino','Jayson','Ronald','Albert','Gerardo','Ryland','Dorian','Drake','Gage','Rodrigo','Hezekiah','Kylan','Boone','Ledger','Santana','Jamari','Jamir','Lawrence','Reece','Kaysen','Shiloh','Arjun','Marcelo','Abram','Benson','Huxley','Nikolas','Zain','Kohen','Samson','Miller','Donald','Finnley','Kannon','Lucian','Watson','Keith','Westin','Tadeo','Sincere','Boston','Axton','Amos','Chandler','Leandro','Raul','Scott','Reign','Alessandro','Camilo','Derrick','Morgan','Julio','Clay','Edison','Jaime','Augustine','Julien','Zeke','Marvin','Bellamy','Landen','Dustin','Jamie','Krew','Kyree','Colter','Johan','Houston','Layton','Quincy','Case','Atreus','Cayson','Aarav','Darius','Harlan','Justice','Abdiel','Layne','Raylan','Arturo','Taylor','Anakin','Ander','Hamza','Otis','Azariah','Leonard','Colby','Duke','Flynn','Trey','Gustavo','Fletcher','Issac','Sam','Trenton','Callahan','Chris','Mohammad','Rayan','Lionel','Bruno','Jaxxon','Zaid','Brycen','Roland','Dillon','Lennon','Ambrose','Rio','Mac','Ahmed','Samir','Yosef','Tru','Creed','Tony','Alden','Aden','Alec','Carmelo','Dario','Marcel','Roger','Ty','Ahmad','Emir','Landyn','Skyler','Mohammed','Dennis','Kareem','Nixon','Rex','Uriah','Lee','Louie','Rayden','Reese','Alberto','Cason','Quinton','Kingsley','Chaim','Alfredo','Mauricio','Caspian','Legacy','Ocean','Ozzy','Briar','Wilson','Forest','Grey','Joziah','Salem','Neil','Remi','Bridger','Harry','Jefferson','Lachlan','Nelson','Casen','Salvador','Magnus','Tommy','Marcellus','Maximo','Jerry','Clyde','Aron','Keaton','Eliam','Lian','Trace','Douglas','Junior','Titan','Cullen','Cillian','Musa','Mylo','Hugh','Tomas','Vincenzo','Westley','Langston','Byron','Kiaan','Loyal','Orlando','Kyro','Amias','Amiri','Jimmy','Vicente','Khari','Brendan','Rey','Ben','Emery','Zyair','Bjorn','Evander','Ramon','Alvin','Ricky','Jagger','Brock','Dakari','Eddie','Blaze','Gatlin','Alonso','Curtis','Kylian','Nathanael','Devon','Wayne','Zakai','Mathew','Rome','Riggs','Aryan','Avi','Hassan','Lochlan','Stanley','Dash','Kaiser','Benicio','Bryant','Talon','Rohan','Wesson','Joe','Noe','Melvin','Vihaan','Zayd','Darren','Enoch','Mitchell','Jedidiah','Brodie','Castiel','Ira','Lance','Guillermo','Thatcher','Ermias','Misael','Jakari','Emory','Mccoy','Rudy','Thaddeus','Valentin','Yehuda','Bode','Madden','Kase','Bear','Boden','Jiraiya','Maurice','Alvaro','Ameer','Demetrius','Eliseo','Kabir','Kellan','Allan','Azrael','Calum','Niklaus','Ray','Damari','Elio','Jon','Leighton','Axl','Dane','Eithan','Eugene','Kenji','Jakob','Colten','Eliel','Nova','Santos','Zahir','Idris','Ishaan','Kole','Korbin','Seven','Alaric','Kellen','Bronson','Franco','Wes','Larry','Mekhi','Jamal','Dilan','Elisha','Brennan','Kace','Van','Felipe','Fisher','Cal','Dior','Judson','Alfonso','Deandre','Rocky','Henrik','Reuben','Anders','Arian','Damir','Jacoby','Khalid','Kye','Mustafa','Jadiel','Stefan','Yousef','Aydin','Jericho','Robin','Wallace','Alistair','Davion','Alfred','Ernesto','Kyng','Everest','Gary','Leroy','Yahir','Braden','Kelvin','Kristian','Adler','Avyaan','Brayan','Jones','Truett','Aries','Joey','Randy','Jaxx','Jesiah','Jovanni','Azriel','Brecken','Harley','Zechariah','Gordon','Jakai','Carl','Graysen','Kylen','Ayan','Branson','Crosby','Dominik','Jabari','Jaxtyn','Kristopher','Ulises','Zyon','Fox','Howard','Salvatore','Turner','Vance','Harlem','Jair','Jakobe','Jeremias','Osiris','Azael','Bowie','Canaan','Elon','Granger','Karsyn','Zavier','Cain','Dangelo','Heath','Yisroel','Gian','Shepard','Harold','Kamdyn','Rene','Rodney','Yaakov','Adrien','Kartier','Cassian','Coleson','Ahmir','Darian','Genesis','Kalel','Agustin','Wylder','Yadiel','Ephraim','Kody','Neo','Ignacio','Osman','Aldo','Abdullah','Cory','Blaine','Dimitri','Khai','Landry','Palmer','Benedict','Leif','Koen','Maxton','Mordechai','Zev','Atharv','Bishop','Blaise','Davian')
--UPDATE Person 
--Set Gender = 'Female'
--WHERE FirstName In ('Olivia','Emma','Charlotte','Amelia','Ava','Sophia','Isabella','Mia','Evelyn','Harper','Luna','Camila','Gianna','Elizabeth','Eleanor','Ella','Abigail','Sofia','Avery','Scarlett','Emily','Aria','Penelope','Chloe','Layla','Mila','Nora','Hazel','Madison','Ellie','Lily','Nova','Isla','Grace','Violet','Aurora','Riley','Zoey','Willow','Emilia','Stella','Zoe','Victoria','Hannah','Addison','Leah','Lucy','Eliana','Ivy','Everly','Lillian','Paisley','Elena','Naomi','Maya','Natalie','Kinsley','Delilah','Claire','Audrey','Aaliyah','Ruby','Brooklyn','Alice','Aubrey','Autumn','Leilani','Savannah','Valentina','Kennedy','Madelyn','Josephine','Bella','Skylar','Genesis','Sophie','Hailey','Sadie','Natalia','Quinn','Caroline','Allison','Gabriella','Anna','Serenity','Nevaeh','Cora','Ariana','Emery','Lydia','Jade','Sarah','Eva','Adeline','Madeline','Piper','Rylee','Athena','Peyton','Everleigh','Vivian','Clara','Raelynn','Liliana','Samantha','Maria','Iris','Ayla','Eloise','Lyla','Eliza','Hadley','Melody','Julia','Parker','Rose','Isabelle','Brielle','Adalynn','Arya','Eden','Remi','Mackenzie','Maeve','Margaret','Reagan','Charlie','Alaia','Melanie','Josie','Elliana','Cecilia','Mary','Daisy','Alina','Lucia','Ximena','Juniper','Kaylee','Magnolia','Summer','Adalyn','Sloane','Amara','Arianna','Isabel','Reese','Emersyn','Sienna','Kehlani','River','Freya','Valerie','Blakely','Genevieve','Esther','Valeria','Katherine','Kylie','Norah','Amaya','Bailey','Ember','Ryleigh','Georgia','Catalina','Emerson','Alexandra','Faith','Jasmine','Ariella','Ashley','Andrea','Millie','June','Khloe','Callie','Juliette','Sage','Ada','Anastasia','Olive','Alani','Brianna','Rosalie','Molly','Brynlee','Amy','Ruth','Aubree','Gemma','Taylor','Oakley','Margot','Arabella','Sara','Journee','Harmony','Blake','Alaina','Aspen','Noelle','Selena','Oaklynn','Morgan','Londyn','Zuri','Aliyah','Jordyn','Juliana','Finley','Presley','Zara','Leila','Marley','Sawyer','Amira','Lilly','London','Kimberly','Elsie','Ariel','Lila','Alana','Diana','Kamila','Nyla','Vera','Hope','Annie','Kaia','Myla','Alyssa','Angela','Ana','Lennon','Evangeline','Harlow','Rachel','Gracie','Rowan','Laila','Elise','Sutton','Lilah','Adelyn','Phoebe','Octavia','Sydney','Mariana','Wren','Lainey','Vanessa','Teagan','Kayla','Malia','Elaina','Saylor','Brooke','Lola','Miriam','Alayna','Adelaide','Daniela','Jane','Payton','Journey','Lilith','Delaney','Dakota','Mya','Charlee','Alivia','Annabelle','Kailani','Lucille','Trinity','Gia','Tatum','Raegan','Camille','Kaylani','Kali','Stevie','Maggie','Haven','Tessa','Daphne','Adaline','Hayden','Joanna','Jocelyn','Lena','Evie','Juliet','Fiona','Cataleya','Angelina','Leia','Paige','Julianna','Milani','Talia','Rebecca','Kendall','Harley','Lia','Phoenix','Dahlia','Logan','Camilla','Thea','Jayla','Brooklynn','Blair','Vivienne','Hallie','Madilyn','Mckenna','Evelynn','Ophelia','Celeste','Alayah','Winter','Catherine','Collins','Nina','Briella','Palmer','Noa','Mckenzie','Kiara','Amari','Adriana','Gracelynn','Lauren','Cali','Kalani','Aniyah','Nicole','Alexis','Mariah','Gabriela','Wynter','Amina','Ariyah','Adelynn','Remington','Reign','Alaya','Dream','Alexandria','Willa','Avianna','Makayla','Gracelyn','Elle','Amiyah','Arielle','Elianna','Giselle','Brynn','Ainsley','Aitana','Charli','Demi','Makenna','Rosemary','Danna','Izabella','Lilliana','Melissa','Samara','Lana','Mabel','Everlee','Fatima','Leighton','Esme','Raelyn','Madeleine','Nayeli','Camryn','Kira','Annalise','Selah','Serena','Royalty','Rylie','Celine','Laura','Brinley','Frances','Michelle','Heidi','Rory','Sabrina','Destiny','Gwendolyn','Alessandra','Poppy','Amora','Nylah','Luciana','Maisie','Miracle','Joy','Liana','Raven','Shiloh','Allie','Daleyza','Kate','Lyric','Alicia','Lexi','Addilyn','Anaya','Malani','Paislee','Elisa','Kayleigh','Azalea','Francesca','Jordan','Regina','Viviana','Aylin','Skye','Daniella','Makenzie','Veronica','Legacy','Maia','Ariah','Alessia','Carmen','Astrid','Maren','Helen','Felicity','Alexa','Danielle','Lorelei','Paris','Adelina','Bianca','Gabrielle','Jazlyn','Scarlet','Bristol','Navy','Esmeralda','Colette','Stephanie','Jolene','Marlee','Sarai','Hattie','Nadia','Rosie','Kamryn','Kenzie','Alora','Holly','Matilda','Sylvia','Cameron','Armani','Emelia','Keira','Braelynn','Jacqueline','Alison','Amanda','Cassidy','Emory','Ari','Haisley','Jimena','Jessica','Elaine','Dorothy','Mira','Eve','Oaklee','Averie','Charleigh','Lyra','Madelynn','Angel','Edith','Jennifer','Raya','Ryan','Heaven','Kyla','Wrenley','Meadow','Carter','Kora','Saige','Kinley','Maci','Mae','Salem','Aisha','Adley','Carolina','Sierra','Alma','Helena','Bonnie','Mylah','Briar','Aurelia','Leona','Macie','Maddison','April','Aviana','Lorelai','Alondra','Kennedi','Monroe','Emely','Maliyah','Ailani','Madilynn','Renata','Katie','Zariah','Imani','Amber','Analia','Ariya','Anya','Emberly','Emmy','Mara','Maryam','Dior','Mckinley','Virginia','Amalia','Mallory','Opal','Shelby','Clementine','Remy','Xiomara','Elliott','Elora','Katalina','Antonella','Skyler','Hanna','Kaliyah','Alanna','Haley','Itzel','Cecelia','Jayleen','Kensley','Beatrice','Journi','Dylan','Ivory','Yaretzi','Meredith','Sasha','Gloria','Oaklyn','Sloan','Abby','Davina','Lylah','Erin','Reyna','Kaitlyn','Michaela','Nia','Fernanda','Jaliyah','Jenna','Sylvie','Miranda','Anne','Mina','Myra','Aleena','Alia','Frankie','Ellis','Kathryn','Nalani','Nola','Jemma','Lennox','Marie','Angelica','Cassandra','Calliope','Adrianna','Ivanna','Zelda','Faye','Karsyn','Oakleigh','Dayana','Amirah','Megan','Siena','Reina','Rhea','Julieta','Malaysia','Henley','Liberty','Leslie','Alejandra','Kelsey','Charley','Capri','Priscilla','Zariyah','Savanna','Emerie','Christina','Skyla','Macy','Mariam','Melina','Chelsea','Dallas','Laurel','Briana','Holland','Lilian','Amaia','Blaire','Margo','Louise','Rosalia','Aleah','Bethany','Flora','Kylee','Kendra','Sunny','Laney','Tiana','Chaya','Ellianna','Milan','Aliana','Estella','Julie','Yara','Rosa','Cheyenne','Emmie','Carly','Janelle','Kyra','Naya','Malaya','Sevyn','Lina','Mikayla','Jayda','Leyla','Eileen','Irene','Karina','Aileen','Aliza','Kataleya','Kori','Indie','Lara','Romina','Jada','Kimber','Amani','Liv','Treasure','Louisa','Marleigh','Winnie','Kassidy','Noah','Monica','Keilani','Zahra','Zaylee','Hadassah','Jamie','Allyson','Anahi','Maxine','Karla','Khaleesi','Johanna','Penny','Hayley','Marilyn','Della','Freyja','Jazmin','Kenna','Ashlyn','Florence','Ezra','Melany','Murphy','Sky','Marina','Noemi','Coraline','Selene','Bridget','Alaiya','Angie','Fallon','Thalia','Rayna','Martha','Halle','Estrella','Joelle','Kinslee','Roselyn','Theodora','Jolie','Dani','Elodie','Halo','Nala','Promise','Justice','Nellie','Novah','Estelle','Jenesis','Miley','Hadlee','Janiyah','Waverly','Braelyn','Pearl','Aila','Katelyn','Sariyah','Azariah','Bexley','Giana','Lea','Cadence','Mavis','Ila','Rivka','Jovie','Yareli','Bellamy','Kamiyah','Kara','Baylee','Jianna','Kai','Alena','Novalee','Elliot','Livia','Ashlynn','Denver','Emmalyn','Persephone','Marceline','Jazmine','Kiana','Mikaela','Aliya','Galilea','Harlee','Jaylah','Lillie','Mercy','Ensley','Bria','Kallie','Celia','Berkley','Ramona','Jaylani','Jessie','Aubrie','Madisyn','Paulina','Averi','Aya','Chana','Milana','Cleo','Iyla','Cynthia','Hana','Lacey','Andi','Giuliana','Milena','Leilany','Saoirse','Adele','Drew','Bailee','Hunter','Rayne','Anais','Kamari','Paula','Rosalee','Teresa','Zora','Avah','Belen','Greta','Layne','Scout','Zaniyah','Amelie','Dulce','Chanel','Clare','Rebekah','Giovanna','Ellison','Isabela','Kaydence','Rosalyn','Royal','Alianna','August','Nyra','Vienna','Amoura','Anika','Harmoni','Kelly','Linda','Aubriella','Kairi','Ryann','Avayah','Gwen','Whitley','Noor','Khalani','Marianna','Addyson','Annika','Karter','Vada','Tiffany','Artemis','Clover','Laylah','Paisleigh','Elyse','Kaisley','Veda','Zendaya','Simone','Alexia','Alisson','Angelique','Ocean','Elia','Lilianna','Maleah','Avalynn','Marisol','Goldie','Malayah','Emmeline','Paloma','Raina','Brynleigh','Chandler','Valery','Adalee','Tinsley','Violeta','Baylor','Lauryn','Marlowe','Birdie','Jaycee','Lexie','Loretta','Lilyana','Princess','Shay','Hadleigh','Natasha','Indigo','Zaria','Addisyn','Deborah','Leanna','Barbara','Kimora','Emerald','Raquel','Julissa','Robin','Austyn','Dalia','Nyomi','Ellen','Kynlee','Salma','Luella','Zayla','Addilynn','Giavanna','Samira','Amaris','Madalyn','Scarlette','Stormi','Etta','Ayleen','Brittany','Brylee','Araceli','Egypt','Iliana','Paityn','Zainab','Billie','Haylee','India','Kaiya','Nancy','Clarissa','Mazikeen','Taytum','Aubrielle','Rylan','Ainhoa','Aspyn','Elina','Elsa','Magdalena','Kailey','Arleth','Joyce','Judith','Crystal','Emberlynn','Landry','Paola','Braylee','Guinevere','Aarna','Aiyana','Kahlani','Lyanna','Sariah','Itzayana','Aniya','Frida','Jaylene','Kiera','Loyalty','Azaria','Jaylee','Kamilah','Keyla','Kyleigh','Micah','Nataly','Kathleen','Zoya','Meghan','Soraya','Zoie','Arlette','Zola','Luisa','Vida','Ryder','Tatiana','Tori','Aarya','Eleanora','Sandra','Soleil','Annabella')
--UPDATE Person 
--Set Gender = 'Female'
--WHERE FirstName In ('Ailsa','Alba','Aniko','Anita','Ann','Anne-Maree','Annette','Ary','Avril','Barbara (Barb)','Barbara Ann','Barbara-Lee','Belinda','Beng','Bernadette','Berris','Beryl','Beth','Betty','Bev','Beverley','Black Crow','Brenda','Bronwyn','Carel','Carmel','Carol','Carole','Carolyn','Celestine','Chander','Chantal','Charlene','Charmain','Cherilynn','Cheryl','Cheryll','Christel','Christine','Chrisula','Claudette','Clifford','Colleen','Coral','Coralie','Dan','Darja','Dawn','Deanne','Dearne','Debby','Debra','Dee','Deirdre','Del','Delma','Denis','Denise','Derene','Desi','Desley','Deslie','Di','Diane','Diane Laraine','Dianne','Doreen','Dot','Elfriede','Elizabeth Mary','Elke','Elva','Elvia','Elvira','Enid','Fay','Flo','Fran','Fred','Freda','Fun','Gail','Gayle','Georgie Beeman','Georgina','Gerda','Glenda','Glennis','Glenys','Gretchen','Group','Gurf','Gwenda','Gyorgy','Harjinder','Heather','Hedley','Helene','Helga','Hilary','Hilda','Inge','Irena','Jacki','Jackie','Jacqui','Jacquie','Jan','Janet','Janette','Janice','Janice Maree','Janiece','Janine','Jann','Janne','Jayne','Jaynell','Jean','Jeanette','Jeanne','Jeannette','Jeannine','Jenell','Jenita','Jennie','Jenny','Jill','Jim','Joan','Joanne','Jo-Anne','Jodi','Josina','Joyce Fay','Juanita','Judi','Judy','Jules','Julianne','Julieann','Julie-Anne','Julienne','Kaethe','Karen','Karen (Kaz)','Karin','Karyn','Kath','Kathie','Kathy','Katrina','Kay','Kaye','Kaylene','Kaz Karen','Keiko','Kerrie','Kerry','Keryn','kilburn','Kim','Klara','Kris','Kristen','Krystyna','La','Lajos','Laurice','Leader','Leanne','Lesley','Libby','Liddy','Lilyan','Lindsay','Lisa','Liz','Lois','Lorelle','Lorna','Lorraine','Ludy','Lyn','Lyndall','Lynette','Lynn','Lynne','Lynnette','Lynora','Macey','Mami','Mandy','Marcia','Marea','Maree','Marian','Marianne','Marion','Marjie','Marsha','Marta','Marty','Marvia','Mary Lou','Maryanne','Maureen','Meg','Merilyn','Merle','Merran','Meryl','Michele','Michell','Moira','Monique','Nan','Narelle','Nattai','Nerida','Neryl','Netta','Nikki','Nita','Noela','Noelene','Noreen','Norma','Nouha','Olga','Pam','Pamela','Paskie','Pat','Patricia','Patty','Pattye','Paulette','Pauline','Pedita','Peta','Philippa','Phyllis','Pina','Rae','Rhonda','Rhonwyn','Rita','Rob','Robyn','Robyn Dawn','Robynne','Ros','Rosalind','Rosely','Rosemarie','Roslyn','Roz','Sally','Salvia','Sandie','Sandra G','Sandy','Selma','Shan','Shani','Sharan','Sharon','Sharyn','Shauna','Sheenadh','Sheila','Sheridan','Shiona','Shirley','Sirke','Sonya','Steph','Sue','Susan','Susan (Sue)','Susana','Susanne','Susie','Suzanne','Suzie','Tarcila','Tess','The','Theresa','Therese','Tim','Tina','To be advised','Toni','Tracey','Tricia','Trish','Ursula','Val','Valda','Velma','Vicki','Vija','Vlase','Wendy','Wilma','Yvonne','Zena')
--UPDATE Person 
--Set Gender = 'Male'
--WHERE Gender = 'Unknown'

INSERT INTO [dbo].[Fee]
           ([ID]
           ,[Date]
           ,[ProcessingYear]
           ,[Amount]
           ,[Description]
           ,[PersonID]
           ,[CreatedOn]
           ,[UpdatedOn]
           ,[User]
           ,[IsMembershipFee])
     SELECT
           newid()
           ,GetDate()
           ,2024
           ,case (datepart(month, feesDue))
                when 12 then -50.00
                else -25.00
            end as amount
           ,'2024 membership fee credit @ syetem conversion'
           ,(select top 1 ID from Person where ConversionID = BadgeNo)
           ,GetDate()
           ,GetDate()
           ,'System Conversion'
           ,1
     FROM mu3a.u3a.qsycsj_u3a_members Where datepart(year,feesdue) IN (2024,2025)

INSERT INTO [dbo].[Fee]
           ([ID]
           ,[Date]
           ,[ProcessingYear]
           ,[Amount]
           ,[Description]
           ,[PersonID]
           ,[CreatedOn]
           ,[UpdatedOn]
           ,[User]
           ,[IsMembershipFee])
     SELECT
           newid()
           ,GetDate()
           ,2025
           ,case (datepart(month, feesDue))
                when 12 then -50.00
                else -25.00
            end as amount
           ,'2025 membership fee credit @ syetem conversion'
           ,(select top 1 ID from Person where ConversionID = BadgeNo)
           ,GetDate()
           ,GetDate()
           ,'System Conversion'
           ,1
     FROM mu3a.u3a.qsycsj_u3a_members Where datepart(year,feesdue) = 2025


INSERT INTO [dbo].[Course]
           ([ID]
           ,[Year]
		   ,[ConversionID]
           ,[Name]
           ,[Description]
           ,[CourseFeePerYear]
		   ,[CourseFeePerYearDescription]
           ,[CourseFeePerTerm]
		   ,[CourseFeePerTermDescription]
           ,[Duration]
           ,[RequiredStudents]
           ,[MaximumStudents]
           ,[CourseTypeID]
           ,[CourseParticipationTypeID])
     SELECT
           newid()			ID
           ,DatePart(year,Getdate())
		   ,eventCode        ConversionID
           ,eventTitle		Name
           ,dbo.stripHTML( eventDesc)	Description
           ,0 CourseFeePerYear
		   ,'' CourseFeePerYearDescription
           ,0 CourseFeePerTerm
		   ,'' CourseFeePerTermDescription
           ,duration
           ,1
           ,maxNbrPlaces
           ,isnull((Select Top 1 ID From CourseType where Name = (select top 1 categoryTitle From mu3a.u3a.qsycsj_u3a_category C Where Category = c.categoryID)),@KEY) CourseTypeID
           ,0   CourseParticipationTypeID
FROM	mu3a.u3a.qsycsj_u3a_event

INSERT INTO [dbo].[Class]
           ([ID]
           ,[LeaderID]
           ,[OfferedTerm1]
           ,[OfferedTerm2]
           ,[OfferedTerm3]
           ,[OfferedTerm4]
           ,[StartDate]
           ,[OccurrenceID]
           ,[OnDayID]
           ,[StartTime]
           ,[CourseID]
           ,[VenueID])
SELECT
           newid()	ID
           ,(Select Top 1 ID From Person where Person.ConversionID = fkLeaderID)
           ,case 
                when termsAvailable like '%1%' then 1 else 0
            end OfferedTerm1
           ,case 
                when termsAvailable like '%2%' then 1 else 0
            end OfferedTerm2
           ,case 
                when termsAvailable like '%3%' then 1 else 0
            end OfferedTerm3
           ,case 
                when termsAvailable like '%4%' then 1 else 0
            end OfferedTerm4
           ,null StartDate
           ,case
                when repeatType = 'no-repeat' Then 0
                when repeatType = 'weekly' Then 2
                when repeatType = 'monthly' Then 4
                else 2
            end as OccurrenceID
           ,case
                when weekDays = 'mon' then 1
                when weekDays = 'tue' then 2
                when weekDays = 'wed' then 3
                when weekDays = 'thu' then 4
                when weekDays = 'fri' then 5
                when WeekDays = 'sat' then 6
                else 0
            end as OnDayID
           ,eventTime StartTime
           ,(Select top 1 ID from Course where ConversionID = eventCode) CourseID
           ,isnull((Select Top 1 ID From Venue where Name = 
                            (select top 1 venueName from mu3a.u3a.qsycsj_u3a_venues v where v.venueID = fkVenueID )),@VKEY)   VenueID
FROM	mu3a.u3a.qsycsj_u3a_event

-- Normalise StartTime to 1st Jan 0001 so we have no sort issues

update class
	set StartTime = DateAdd(year,(datepart(year,StartTime)-1)*-1,StartTime)
where DATEPART(year,StartTime) > 1
update class
	set StartTime = DateAdd(month,(datepart(month,StartTime)-1)*-1,StartTime)
where DATEPART(month,StartTime) > 1
update class
	set StartTime = DateAdd(day,(datepart(day,StartTime)-1)*-1,StartTime)
where DATEPART(day,StartTime) > 1

INSERT INTO [dbo].[Enrolment]
           ([ID]
           ,[TermID]
           ,[CourseID]
           ,[ClassID]
           ,[PersonID]
           ,[Created]
           ,[IsWaitlisted]
           ,[DateEnrolled])
     SELECT
           newid()
           ,(Select Top 1 ID from Term where TermNumber = @CURRENT_TERM)
           ,(select Top 1 ID from Course where Course.ConversionID = p.eventCode)
           ,null
           ,(select Top 1 ID from Person where Person.ConversionID = m.BadgeNo)
           ,DATETIMEFROMPARTS(DATEPART(year,e.DateEntered), 
                              DATEPART(month,e.DateEntered),
                              DATEPART(day,e.DateEntered),
                              DATEPART(hour,e.DateEntered),
                              DATEPART(minute,e.DateEntered)
                              ,DATEPART(second,e.DateEntered)
                              ,Cast(rand(checksum(newid()))*1000  as int)) -- !! Important
           ,0
           ,e.DateEntered
FROM	mu3a.u3a.qsycsj_u3a_enrolments e Inner Join
			mu3a.u3a.qsycsj_u3a_event p on e.fkEventCode = p.eventCode inner join
			mu3a.u3a.qsycsj_u3a_members m on e.fkBadgeNo = m.BadgeNo

