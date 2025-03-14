using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{

    [Index(nameof(Status))]
    public class DocumentQueue : BaseEntity
    {
        public Guid ID { get; set; }
        public int DelayInHours { get; set; } = 0;
        public string DocumentTemplateJSON { get; set; }
        public string ExportDataJSON { get; set; }
        public string? MemberIdToExport { get; set; } = null;
        public bool SendToMultipleRecipients { get; set; }
        public DocumentQueueStatus Status { get; set; }
        public List<DocumentQueueAttachment> DocumentAttachments { get; set; } = new List<DocumentQueueAttachment>();
        public bool OverrideCommunicationPreference { get; set; }
        public int EmailCount { get; set; }
        public string Result { get; set; }
    }
    public class DocumentQueueAttachment
    {
        public Guid ID { get; set; }
        public Byte[] Attachment { get; set; }

        public Guid DocumentQueueID { get; set; }
        public DocumentQueue DocumentQueue { get; set; }

    }
    public enum DocumentQueueStatus
    {
        ReadyToSend,
        InProcess,
        Complete,
        Error
    }

}
