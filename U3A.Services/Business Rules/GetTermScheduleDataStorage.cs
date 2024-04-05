using DevExpress.Blazor;
using Microsoft.EntityFrameworkCore;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<DxSchedulerDataStorage> GetTermScheduleDataStorageAsync(U3ADbContext dbc)
        {
            DxSchedulerDataStorage dataStorage = new DxSchedulerDataStorage();
            dataStorage.AppointmentMappings = new DxSchedulerAppointmentMappings()
            {
                Type = "AppointmentType",
                Start = "StartDate",
                End = "EndDate",
                Subject = "Caption",
                AllDay = "AllDay",
                LabelId = "Label",
                StatusId = "Status",
                CustomFieldMappings = new List<DxSchedulerCustomFieldMapping> {
                        new DxSchedulerCustomFieldMapping { Name = "Source", Mapping = "Term" }
                    }
            };
            dataStorage.AppointmentLabelMappings = new DxSchedulerAppointmentLabelMappings()
            {
                Id = "Id",
                Caption = "LabelCaption",
                BackgroundCssClass = "BackgroundCssClass",
                TextCssClass = "TextCssClass"
            };
            dataStorage.AppointmentLabelsSource = new List<LabelObject>() {
                new LabelObject() {
                    Id = 0,
                    LabelCaption = "Term",
                    BackgroundCssClass = "bg-secondary",
                    TextCssClass = "text-black"
                },
                new LabelObject() {
                    Id = 1,
                    LabelCaption = "Default Term",
                    BackgroundCssClass = "bg-success",
                    TextCssClass = "text-white"
                },
                new LabelObject() {
                    Id = 2,
                    LabelCaption = "Enrolment",
                    BackgroundCssClass = "bg-warning",
                    TextCssClass = "text-white"
                },
                new LabelObject() {
                    Id = 3,
                    LabelCaption = "Enrolment Allocation Review",
                    BackgroundCssClass = "bg-info",
                    TextCssClass = "text-white"
                },
            };
            dataStorage.AppointmentsSource = await GetTermScheduleAsync(dbc);
            return dataStorage;
        }
        static async Task<List<TermSchedule>> GetTermScheduleAsync(U3ADbContext dbc)
        {
            List<TermSchedule> list = new List<TermSchedule>();
            TermSchedule schedule;
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
            Term term = await BusinessRule.CurrentTermAsync(dbc);
            if (term != null)
            {
                var terms = await dbc.Term.AsNoTracking().ToListAsync();
                foreach (Term t in terms)
                {
                    // Random allocation day
                    if (settings.AutoEnrolRemainderMethod.ToLower() == "random" &&
                                BusinessRule.IsRandomAllocationTerm(t, settings))
                    {
                        var allocDay = BusinessRule.GetThisTermAllocationDay(t, settings);
                        schedule = new TermSchedule();
                        schedule.StartDate = allocDay;
                        schedule.EndDate = allocDay.AddDays(constants.RANDOM_ALLOCATION_PREVIEW);
                        schedule.AppointmentType = 0;
                        schedule.Caption = $"{t.Name} Enrolment Allocation Review";
                        schedule.Label = 3;
                        schedule.Status = 1;
                        schedule.AllDay = true;
                        schedule.Term = null;
                        list.Add(schedule);
                    }
                    // The term
                    schedule = new TermSchedule();
                    schedule.StartDate = t.StartDate;
                    schedule.EndDate = t.EndDate.Date.AddDays(1);
                    schedule.AppointmentType = 0;
                    schedule.Caption = t.Name;
                    schedule.Label = (t.IsDefaultTerm) ? 1 : 0;
                    schedule.AllDay = true;
                    schedule.Term = t;
                    list.Add(schedule);
                    // The enrolment period
                    schedule = new TermSchedule();
                    schedule.StartDate = t.EnrolmentStartDate;
                    schedule.EndDate = t.EnrolmentEndDate.Date.AddDays(1);
                    schedule.AppointmentType = 0;
                    schedule.Caption = $"{t.Name} Enrolment Period";
                    schedule.Label = 2;
                    schedule.AllDay = true;
                    schedule.Term = t;
                    list.Add(schedule);
                }
            }
            return list;
        }
    }

}
