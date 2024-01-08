using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.UI.Forms
{
    public enum PortalMenuSelection
    {
        ShowMenu,
        GetLinkedPerson,
        DoSelectLinkedMember,
        DoMemberMaintenance,
        DoMemberLeave,
        DoEditMemberEnrolment,
        DoViewMemberEnrolment,
        DoShowLeaderMenu,
        DoMemberPayment,
        DoMemberPaymentPreamble,
        DoMemberPaymentDirectDebit,
        DoLinkNewMember,
        DoLinkExistingMember,
        DoUnlinkMember,
        DoWhatsOn,
        DoSignInSignOut,
        DoTellAFriend,
        NotImplemented
    }

    public enum PortalMenuResult
    {
        MenuOptionCompleted,
        MenuOptionCancelled,
        MemberDetailsCompleted,
        NewMemberFeePayment,
        LinkedMemberChanged,
        EnrolmentCancelledTermNotDefined,
    }
}
