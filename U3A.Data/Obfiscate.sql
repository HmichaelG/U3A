
declare @OBFISCATE bit = 1


IF (@OBFISCATE = 1)
    BEGIN
    UPDATE Person
		    SET
			    Mobile = replace(replace(replace(Mobile,'9','8'),'6','3'),'2','5')
                ,HomePhone = replace(replace(replace(HomePhone,'9','8'),'6','3'),'2','5')
                ,ICEPhone = replace(replace(replace(ICEPhone,'9','8'),'6','3'),'2','5')
                ,Postcode = replace(replace(replace(Postcode,'9','8'),'6','3'),'2','5')
                ,Email = lower(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(Email,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski'))
                ,LastName = lower(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(LastName,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski'))
                ,Address = replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(Address,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski')
                ,ICEContact = lower(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(ICEContact,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski'))
                ,City = Upper(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(City,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski'))
                ,FirstName = lower(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(FirstName,'o','e'),'a','o'),'i','a'),'u','i'),'t','p'),'c','k'),'d','th'),'ee','e'),'oo','or'),'ll','ski'))

    ;WITH cte as (
	    select	id, Mobile
			    ,Gender
                ,HomePhone
                ,ICEPhone
                ,Postcode
                ,Email
                ,LastName
                ,Address
                ,ICEContact
                ,City
			    ,FirstName
	    from person
    )
    UPDATE Person
            SET
		       FirstName = (SELECT TOP 1 FirstName FROM cte WHERE p.id = p.Id ORDER BY NEWID())
		       ,LastName = (SELECT TOP 1 LastName FROM cte WHERE p.Id = p.Id ORDER BY NEWID())
		       ,mobile = (SELECT TOP 1 mobile FROM cte WHERE p.Id = p.Id ORDER BY NEWID())
		       ,HomePhone = (SELECT TOP 1 HomePhone FROM cte WHERE p.Id = p.Id ORDER BY NEWID())
 		       ,ICEContact = (SELECT TOP 1 ICEContact FROM cte WHERE p.Id = p.Id ORDER BY NEWID())
		       ,ICEPhone = (SELECT TOP 1 ICEPhone FROM cte WHERE p.Id = p.Id ORDER BY NEWID())
		       ,Postcode = (SELECT TOP 1 Postcode FROM cte WHERE p.Id = p.Id ORDER BY NEWID())
		       ,Email = (SELECT TOP 1 Email FROM cte WHERE p.Id = p.Id ORDER BY NEWID())
		       ,Address = (SELECT TOP 1 Address FROM cte WHERE p.Id = p.Id ORDER BY NEWID())
		       ,City = (SELECT TOP 1 City FROM cte WHERE p.Id = p.Id ORDER BY NEWID())
		    from cte p
        UPDATE Person
            SET LastName = UPPER(substring(LastName,1,1)) + RIGHT(LastName,LEN(LastName)-1)
			    ,FirstName = UPPER(substring(FirstName,1,1)) + RIGHT(FirstName,LEN(FirstName)-1)
    END

