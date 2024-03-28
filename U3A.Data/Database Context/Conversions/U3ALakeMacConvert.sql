
declare @CURRENT_TERM int = 3

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
           ,[MembershipFee]
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
           ,'Lake Macquarie U3A Inc'
           ,''
           ,'PO Box 782 Toronto 2283'
           ,''
           ,'' -- ABN Unknown
           ,'membership@lmu3a.au'
           ,'https://lakemacquarie.u3anet.org.au'
           ,'0490 367 591'
           ,65.00
           ,0.00
           ,1
           ,null
           ,15.00
           ,'Random'
           ,'President' 
             + @CRLF + 'Vice-President'
             + @CRLF + 'Secretary'
             + @CRLF + 'Treasurer'
             + @CRLF + 'Assistant Treasurer'
             + @CRLF + 'Course Coordinator'
             + @CRLF + 'Enrolment Officer'
             + @CRLF + 'Venues Officer'
             + @CRLF + 'Leaders Liaison'
             + @CRLF + 'Publicity Officer'
             + @CRLF + 'Assistant Secretary'
             + @CRLF + 'BSCC Convenor'
            ,'Internal Affairs'
                + @CRLF + 'Grounds & Gardening'
                + @CRLF + 'Working Bees'
                + @CRLF + 'Administration'
            ,'membership@eastlakesu3a.com.au'
            ,'Membership Office'
            ,'650 000'
            ,'959020300'
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
           ,[Calendar Year]
           ,1
		   ,[Semester 1 Start]
           ,DateDiff(wk,[Semester 1 Start],[Term 1 Break Start])
           ,Datediff(wk,[Semester 1 Start],[Term 1 Break Start])
           ,-7
	FROM LMU3AAccess.dbo.[Semester Dates]

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
           ,[Calendar Year]
           ,2
		   ,[Term 1 Break End]
           ,DateDiff(wk,[Term 1 Break End],[Semester 1 End])
           ,Datediff(wk,[Term 1 Break End],[Semester 1 End])
           ,-7
	FROM LMU3AAccess.dbo.[Semester Dates]

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
           ,[Calendar Year]
           ,3
		   ,[Semester 2 Start]
           ,DateDiff(wk,[Semester 2 Start],[Term 2 Break Start])
           ,Datediff(wk,[Semester 2 Start],[Term 2 Break Start])
           ,-7
	FROM LMU3AAccess.dbo.[Semester Dates]

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
           ,[Calendar Year]
           ,4
		   ,[Term 2 Break End]
           ,DateDiff(wk,[Term 2 Break End],[Semester 2 End])
           ,Datediff(wk,[Term 2 Break End],[Semester 2 End])
           ,-7
	FROM LMU3AAccess.dbo.[Semester Dates]

UPDATE Term Set IsDefaultTerm = 1 WHERE TermNumber = @CURRENT_TERM
UPDATE Term Set IsClassAllocationFinalised = 1 WHERE TermNumber <> 1

INSERT INTO [dbo].[CourseType]
           ([ID]
           ,[Discontinued]
           ,[Name]
           ,[Comment])
SELECT
			newid()
			,0
			,[Category]
			,''
FROM LMU3AAccess.dbo.Category

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
           ,(select top 1 value FROM STRING_SPLIT(Location,','))
           ,isnull(Limit,0)
           ,(SELECT REPLACE(SUBSTRING([Location], CHARINDEX(',', [Location]), LEN([Location])), ',', ''))
		   ,isnull(Comment,'')
           ,[Entry Access]
           ,[Venue Coordinator]
           ,[Coordinator e-mail]
           ,Equipment
           ,[key access]
           ,[Coordinator Phone]
FROM LMU3AAccess.dbo.Venue

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
           ,0
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
           )
     SELECT
           newid()				ID
		   ,Membership_ID			ConversionID
           ,[First Name]
           ,Surname
           ,isnull([Street Address],'')
           ,s.Suburb
           ,'NSW'
           ,s.PostCode
           ,case [Gender]
                when 'M' then 'Male'
                when 'F' then 'Female'
                when 'O' then 'Other'
            end Gender
           ,null
           ,cast('1-jan-2022' as Datetime)		DateJoined
           ,null				DtaeCeased
           ,isnull([e-mail Address],'')
           ,isnull([Phone Number],'')	HomePhone
           ,isnull([Mobile Number],'')
           ,'To be advised'				ICEContact
           ,'Unknown'              ICEPhone
           ,0 IsLifeMember
           ,0 Covid19
           ,case
                when trim(isnull([e-mail address],'')) = '' then 'Post'
                else 'Email'
            end as Communication
           ,[CreatedOn] = getDate()
           ,[UpdatedOn] = getDate()
           ,case
                when trim(isnull([PayDate],'')) ='' Then datepart(year,GetDate())-2
                else datepart(year,GetDate())
            end as FinancialTo
FROM	LMU3AAccess.dbo.Membership inner join LMU3AAccess.dbo.Suburb s
        on LMU3AAccess.dbo.Membership.Suburb = s.ID


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

INSERT INTO [dbo].[Receipt]
           ([ID]
		   ,[Date]
           ,[Amount]
           ,[Description]
           ,[Identifier]
           ,[PersonID]
           ,[FinancialTo]
           ,[CreatedOn]
           ,[UpdatedOn]
           ,[DateJoined]
           ,[ProcessingYear]
           ,[User])
     SELECT
			newid()
           ,dateadd(day,-1,GetDate())
           ,0.00
           ,'System coversion'
           ,''
           ,Person.ID
           ,datepart(year,GetDate())
           ,GetDate()
           ,GetDate()
           ,Person.DateJoined
           ,Person.FinancialTo
           ,'System Conversion'
     FROM Person Where FinancialTo = datepart(year,GetDate())

--IF (@OBFISCATE = 1)
--    BEGIN
--    UPDATE Person
--        SET Mobile = replace(replace(replace(Mobile,'9','8'),'6','3'),'2','5')
--            ,HomePhone = replace(replace(replace(HomePhone,'9','8'),'6','3'),'2','5')
--            ,ICEPhone = replace(replace(replace(ICEPhone,'9','8'),'6','3'),'2','5')
--            ,Postcode = replace(replace(replace(Postcode,'9','8'),'6','3'),'2','5')
--            ,Email = lower(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(Email,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski'))
--            ,LastName = lower(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(LastName,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski'))
--            ,Address = replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(Address,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski')
--            ,ICEContact = lower(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(ICEContact,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski'))
--            ,City = Upper(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(City,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski'))
--    UPDATE Person
--        SET LastName = UPPER(substring(LastName,1,1)) + RIGHT(LastName,LEN(LastName)-1)
--    END

update LMU3AAccess.dbo.[Time]
        set Time = '0:00 am' where time = 'tbastart'
update LMU3AAccess.dbo.[Time]
        set Time = '1:00 am' where time = 'tbafin'
update LMU3AAccess.dbo.[Time]
        set Time = '12:00 pm' where time = '12 noon'

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
		   ,[Code Programme]        ConversionID
           ,[Title of Course]		Name
           ,isnull([Description],'')	Description
           ,0 CourseFeePerYear
		   ,'' CourseFeePerYearDescription
           ,0 CourseFeePerTerm
		   ,'' CourseFeePerTermDescription
           ,isnull(datediff(mi,
                        (select top 1 cast([time] as datetime) from LMU3AAccess.dbo.[Time] T where T.ID = [Time ( Start )])   
                        ,(select top 1 cast([time] as datetime) from LMU3AAccess.dbo.[Time] T where T.ID = [Time ( Finish )]))/60.00,30) Duration
           ,isnull([Minimum in Class],0) RequiredStudents
           ,isnull([Class Limit],0) MaximumStudents
           ,isnull((Select Top 1 ID From CourseType where Name = (select top 1 Category From LMU3AAccess.dbo.Category C Where LMU3AAccess.dbo.Programme.Category = c.ID)),@KEY) CourseTypeID
           ,0   CourseParticipationTypeID
FROM	LMU3AAccess.dbo.Programme

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
           ,(Select Top 1 ID From Person where Person.ConversionID = [Course Leader A]) LeaderID
           ,1 OfferedTerm1
           ,1 OfferedTerm2
           ,1 OfferedTerm3
           ,1 OfferedTerm4
           ,case
                when charindex(',',[Session Dates]) > 0 Then null
                when charindex(';',[Session Dates]) > 0 Then null
                else cast ([Session Dates] + ' ' + '2023' as datetime)
            end as StartDate
           ,case
                when charindex(',',[Session Dates]) > 0 Then 2
                when charindex(';',[Session Dates]) > 0 Then 2
                else 0
            end as OccurrenceID
           ,case
                when [Day of Week] <= 6 then [Day of Week]
                else 0
            end as OnDayID
           ,(select top 1 cast([time] as datetime) from LMU3AAccess.dbo.[Time] T where T.ID = [Time ( Start )]) StartTime
           ,(Select top 1 ID from Course where ConversionID = [Code Programme]) CourseID
           ,isnull((Select Top 1 ID From Venue where Name = 
                            (select top 1 (select top 1 value FROM STRING_SPLIT(Location,',')) from LMU3AAccess.dbo.Venue v where v.ID = LMU3AAccess.dbo.Programme.Venue )),@VKEY)   VenueID
FROM	LMU3AAccess.dbo.Programme

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
           ,(select Top 1 ID from Course where Course.ConversionID = p.[Code Programme])
           ,null
           ,(select Top 1 ID from Person where Person.ConversionID = m.Membership_ID)
           ,DATETIMEFROMPARTS(DATEPART(year,[Enrol Date]), 
                              DATEPART(month,[Enrol Date]),
                              DATEPART(day,[Enrol Date]),
                              DATEPART(hour,[Enrol Date]),
                              DATEPART(minute,[Enrol Date])
                              ,DATEPART(second,[Enrol Date])
                              ,Cast(rand(checksum(newid()))*1000  as int)) -- !! Important
           ,Isnull([WaitListed ?],0)
           ,null
FROM	LMU3AAccess.dbo.Enrolment e Inner Join
			LMU3AAccess.dbo.Programme p on e.[Programme Code] = p.[Programme ID] inner join
			LMU3AAccess.dbo.Membership m on e.[Membership ID] = m.Membership_ID
Where e.[Withdrawn from Course ?] <> 1

UPDATE Enrolment Set DateEnrolled = Created Where IsWaitlisted = 0
