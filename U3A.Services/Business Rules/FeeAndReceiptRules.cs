using DevExpress.Blazor.Internal;
using DevExpress.XtraRichEdit.Commands.Internal;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<List<Receipt>> EditableReceiptsForYearAsync(U3ADbContext dbc, int ProcessingYear)
        {
            return await dbc.Receipt
                            .Include(x => x.Person)
                            .OrderBy(x => x.Person.LastName)
                            .ThenBy(x => x.Person.FirstName)
                            .ThenBy(x => x.Date)
                            .Where(x => x.Amount != 0 && x.ProcessingYear == ProcessingYear)
                            .ToListAsync();
        }
        public static async Task<List<Fee>> EditableFeesForYearAsync(U3ADbContext dbc, int ProcessingYear)
        {
            return await dbc.Fee
                            .Include(x => x.Person)
                            .OrderBy(x => x.Person.LastName)
                            .ThenBy(x => x.Person.FirstName)
                            .ThenBy(x => x.Date)
                            .Where(x => x.ProcessingYear == ProcessingYear)
                            .ToListAsync();
        }

        public static async Task ResetPersonDetailsForDeletedReceipt(U3ADbContext dbc, 
                Receipt ReceiptToDelete, int ProcessingYear)
        {
            var person = ReceiptToDelete.Person;
            var resetYear = ProcessingYear - 1;
            if (resetYear < constants.START_OF_TIME) { resetYear = constants.START_OF_TIME; }
            person.FinancialTo = resetYear;
            dbc.Update(person);
        }

        public static async Task SetPersonDetailsForNewReceipt(U3ADbContext dbc, Receipt ReceiptToCreate)
        {
            var person = ReceiptToCreate.Person;
            if (person.FinancialTo < ReceiptToCreate.ProcessingYear)
            {
                person.PreviousFinancialTo = person.FinancialTo;
                person.FinancialTo = ReceiptToCreate.ProcessingYear;
                ReceiptToCreate.DateJoined = person.DateJoined.Value;
            }
            if (person.DateJoined == null) person.DateJoined = DateTime.Today;
            ReceiptToCreate.FinancialTo = ReceiptToCreate.ProcessingYear;
            dbc.Update(person);
        }

        public static async Task SetPersonDetailsForEditedReceipt(U3ADbContext dbc, Receipt OriginalReceipt, Receipt EditedReceipt)
        {
            if (OriginalReceipt.Person.ID != EditedReceipt.Person.ID)
            {
                await SetPersonDetailsForNewReceipt(dbc, EditedReceipt);
            }
            else
            {
                var person = EditedReceipt.Person;
                if (EditedReceipt.FinancialTo == 0) { EditedReceipt.FinancialTo = EditedReceipt.ProcessingYear; }
                if (person.FinancialTo < OriginalReceipt.ProcessingYear)
                {
                    person.PreviousFinancialTo = person.FinancialTo;
                    person.FinancialTo = EditedReceipt.ProcessingYear;
                    dbc.Update(person);
                }
            }
        }
    }
}