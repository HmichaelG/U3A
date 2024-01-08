using Microsoft.EntityFrameworkCore;
using System.Text;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<List<DocumentTemplate>> EditableDocumentTemplatesAsync(U3ADbContext dbc)
        {
            return await dbc.DocumentTemplate
                            .Include(x => x.DocumentType)
                            .OrderBy(x => x.Name).ToListAsync();
        }
        public static async Task<List<DocumentTemplate>> SelectableDocumentTemplatesAsync(U3ADbContext dbc)
        {
            bool hasEmail = (EmailFactory.GetEmailSender(dbc) == null) ? false : true;
            bool hasSMS = (SMSFactory.GetSMSSender(dbc) == null) ? false : true;
            var result = await dbc.DocumentTemplate.AsNoTracking()
                            .Include(x => x.DocumentType)
                            .Where(x => (x.DocumentType.IsEmail && hasEmail) ||
                                            x.DocumentType.IsPostal ||
                                            (x.DocumentType.IsSMS && hasSMS))
                            .OrderBy(x => x.Name).ToListAsync();
            var doc = new DocumentTemplate()
            {
                ID = Guid.Empty,
                DocumentTypeID = 0,
                DocumentType = await dbc.DocumentType.FindAsync(0),
                Name = "*** Custom Email ***"
            };
            result.Insert(0, doc);
            return result;
        }

        public static async Task<string> DuplicateMarkUpAsync(U3ADbContext dbc, DocumentTemplate EmailTemplate)
        {
            string result = string.Empty;
            DocumentTemplate? duplicate = await DuplicateDocumentTemplate(dbc, EmailTemplate);
            if (duplicate != null)
            {
                result = $"<p>The template name [{duplicate.Name.Trim()}] is already on file.</p>";
            }
            return result;
        }

        static async Task<DocumentTemplate?> DuplicateDocumentTemplate(U3ADbContext dbc, DocumentTemplate DocumentTemplate)
        {
            return await dbc.DocumentTemplate.AsNoTracking()
                            .Where(x => x.ID != DocumentTemplate.ID &&
                                        x.Name.Trim().ToUpper() == DocumentTemplate.Name.Trim().ToUpper()).FirstOrDefaultAsync();
        }
    }
}
