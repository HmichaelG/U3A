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
        DoShowEnrolmentSubmenu,
        DoRequestMemberEnrolment,
        DoWithdrawMemberEnrolment,
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
        PrintMemberBadge,
        DoReturnToAdminPortal,
        NotImplemented
    }

    public enum PortalMenuResult
    {
        MenuOptionCompleted,
        EnrolmentSubmenuOptionCompleted,
        EnrolmentSubmenuOptionCancelled,
        MenuOptionCancelled,
        MemberDetailsCompleted,
        NewMemberFeePayment,
        LinkedMemberChanged,
        EnrolmentCancelledTermNotDefined,
    }
}
