using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace U3A.Model
{
    public enum DurableActivity

    {
        [Display(Name = "Finalise Payments")]
        DoFinalisePayments,
        [Display(Name = "Auto Enrolment")]
        DoAutoEnrolment,
        [Display(Name = "Bring Forward Enrolments")]
        DoBringForwardEnrolments,
        //[Display(Name = "Send Correspondence")]
        //DoCorrespondence,
        [Display(Name = "Requested Leader Reports")]
        DoSendRequestedLeaderReports,
        [Display(Name = "Process Queued Documents")]
        DoProcessQueuedDocuments,
        [Display(Name = "Create Attendance")]
        DoCreateAttendance,
        [Display(Name = "Build Schedule")]
        DoBuildSchedule,
        [Display(Name = "Database Cleanup")]
        DoDatabaseCleanup,
        [Display(Name = "Membership Alert Email")]
        DoMembershipAlertsEmail
    }

    [NotMapped]
    public class DurableActivityWrapper
    {
        public DurableActivity Type { get; set; }
        public string DisplayText { get; set; }
    }

    [NotMapped]
    public class DurableActivityList : List<DurableActivityWrapper>
    {
        public DurableActivityList()
        {
            AddRange(Enum.GetValues(typeof(DurableActivity))
                            .OfType<DurableActivity>()
                            .Select(t => new DurableActivityWrapper()
                            {
                                Type = t,
                                DisplayText = t.GetAttribute<DisplayAttribute>().Name,
                            }).ToList());
        }

    }

}
