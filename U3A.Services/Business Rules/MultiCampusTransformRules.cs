using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static Person GetPersonFromMCPerson(MultiCampusPerson p)
        {
            return new()
            {
                ID = p.ID,
                Title = p.Title,
                PostNominals = p.PostNominals,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                HomePhone = p.HomePhone,
                Mobile = p.Mobile,
                SMSOptOut = p.SMSOptOut,
                ICEContact = p.ICEContact,
                ICEPhone = p.ICEPhone,
                VaxCertificateViewed = p.VaxCertificateViewed,
                Communication = p.Communication,
                Address = p.Address,
                City = p.City,
                State = p.State,
                Postcode = p.Postcode,
                IsMultiCampusVisitor = true,
                FinancialTo = TimezoneAdjustment.GetLocalTime().Date.Year
            };
        }

        public static Term GetTermFromMCTerm(MultiCampusTerm t)
        {
            return new()
            {
                Year = t.Year,
                TermNumber = t.TermNumber,
                Duration = t.Duration,
                EnrolmentEnds = t.EnrolmentEnds,
                EnrolmentStarts = t.EnrolmentStarts,
                ID = t.ID,
                StartDate = t.StartDate,
                IsDefaultTerm = t.IsDefaultTerm,
                IsClassAllocationFinalised = t.IsClassAllocationFinalised,
            };
        }
        public static Enrolment GetEnrolmentFromMCEnrolment(MultiCampusEnrolment e,
                                    MultiCampusPerson p,
                                    Class c,
                                    MultiCampusTerm t)
        {
            return GetEnrolmentFromMCEnrolment(e, GetPersonFromMCPerson(p), t, c);
        }
        public static Enrolment GetEnrolmentFromMCEnrolment(MultiCampusEnrolment e,
                                    MultiCampusPerson p,
                                    MultiCampusTerm t)
        {
            return GetEnrolmentFromMCEnrolment(e, GetPersonFromMCPerson(p), t);
        }
        public static Enrolment GetEnrolmentFromMCEnrolment(MultiCampusEnrolment e,
                                    Person p,
                                    MultiCampusTerm t,
                                    Class? c = null)
        {
            return new()
            {
                ID = e.ID,
                IsWaitlisted = e.IsWaitlisted,
                PersonID = e.PersonID,
                TermID = e.TermID,
                Term = GetTermFromMCTerm(t),
                CourseID = e.CourseID,
                Course = (c != null) ? c.Course : null, 
                ClassID = e.ClassID,
                Created = e.Created,
                DateEnrolled = e.DateEnrolled,
                Person = p,
            };
        }

        public static SendMail GetSendMailFromMCSendMail(MultiCampusSendMail sm,
                                                            MultiCampusPerson p)
        {
            return new()
            {
                CreatedOn = sm.CreatedOn,
                DocumentName = sm.DocumentName,
                ID = sm.ID,
                IsUserRequested = false,
                PersonID = sm.PersonID,
                Person = GetPersonFromMCPerson(p),
                TermID = sm.TermID,
                RecordKey = sm.RecordKey,
                Status = sm.Status,
                UpdatedOn = sm.UpdatedOn,
                User = sm.User,
            };
        }

    }
}
