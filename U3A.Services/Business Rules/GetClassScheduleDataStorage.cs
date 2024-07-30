using DevExpress.Blazor;
using Microsoft.EntityFrameworkCore;
using Serilog;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<DxSchedulerDataStorage> GetCalendarDataStorageAsync(U3ADbContext dbc, Term selectedTerm)
        {
            return await GetCourseScheduleDataStorageAsync(dbc, selectedTerm, new List<Venue>(), IsCalendarView: true);
        }
        public static async Task<DxSchedulerDataStorage> GetCourseScheduleDataStorageAsync(U3ADbContext dbc, Term selectedTerm)
        {
            return await GetCourseScheduleDataStorageAsync(dbc, selectedTerm, new List<Venue>(), IsCalendarView: false);
        }
        public static async Task<DxSchedulerDataStorage> GetCourseScheduleDataStorageAsync(U3ADbContext dbc,
                    Term selectedTerm, IEnumerable<Venue> VenuesToFilter)
        {
            return await GetCourseScheduleDataStorageAsync(dbc, selectedTerm, VenuesToFilter, IsCalendarView: false);
        }
        static async Task<DxSchedulerDataStorage> GetCourseScheduleDataStorageAsync(U3ADbContext dbc,
                    Term selectedTerm, IEnumerable<Venue> VenuesToFilter, bool IsCalendarView)
        {
            var termsInYear = await BusinessRule.SelectableTermsInCurrentYearAsync(dbc, selectedTerm);
            DxSchedulerDataStorage dataStorage = new DxSchedulerDataStorage()
            {
                AppointmentMappings = new DxSchedulerAppointmentMappings()
                {
                    Type = "AppointmentType",
                    Start = "StartDate",
                    End = "EndDate",
                    Subject = "Caption",
                    AllDay = "AllDay",
                    Location = "Location",
                    Description = "Description",
                    LabelId = "Label",
                    RecurrenceInfo = "Recurrence",
                    CustomFieldMappings = new List<DxSchedulerCustomFieldMapping> {
                        new DxSchedulerCustomFieldMapping { Name = "Source", Mapping = "Class" }
                    }
                },
                AppointmentLabelsSource = new List<LabelObject>() {
            new LabelObject() {
                Id = 0,
                LabelCaption = "Undersubscribed",
                BackgroundCssClass = "bg-warning",
                TextCssClass = "text-white"
            },
            new LabelObject() {
                Id = 1,
                LabelCaption = "Good To Go",
                BackgroundCssClass = "bg-success",
                TextCssClass = "text-white"
            },
            new LabelObject() {
                Id = 2,
                LabelCaption = "Oversubscribed",
                BackgroundCssClass = "bg-danger",
                TextCssClass = "text-white"
            },
            new LabelObject() {
                Id = 3,
                LabelCaption = "Off-Schedule Activity",
                BackgroundCssClass = "bg-info",
                TextCssClass = "text-white"
            },
            new LabelObject() {
                Id = 9,
                LabelCaption = "Cancelled/Postponed",
                BackgroundCssClass = "bg-light",
                TextCssClass = "text-black"
            },
        },
                AppointmentLabelMappings = new DxSchedulerAppointmentLabelMappings()
                {
                    Id = "Id",
                    Caption = "LabelCaption",
                    BackgroundCssClass = "BackgroundCssClass",
                    TextCssClass = "TextCssClass"
                }
            };
            var list = new List<ClassSchedule>();
            if (IsCalendarView)
            {
                foreach (var t in termsInYear)
                {
                    list.AddRange(await GetScheduleAsync(dbc, t, VenuesToFilter, IsCalendarView));
                }
            }
            else
            {
                list = await GetScheduleAsync(dbc, selectedTerm, VenuesToFilter, IsCalendarView);
            }
            list.AddRange(await GetPublicHolidays(dbc));
            dataStorage.AppointmentsSource = list;
            // Set any class on a public holiday to LabelID = 9 (Xancellation)
            foreach (var ph in dbc.PublicHoliday.AsNoTracking().ToArray())
            {
                DxSchedulerDateTimeRange range = new DxSchedulerDateTimeRange(ph.Date,
                            ph.Date.AddDays(1));
                var appointments = dataStorage?.GetAppointments(range);
                if (appointments != null)
                {
                    foreach (var a in appointments)
                    {
                        a.LabelId = 9;
                    }
                }
            }
            // Set any class cancelled to LabelID = 9 (Cancellation)
            foreach (var cancelled in await BusinessRule.AllCancelledClassesAsync(dbc))
            {
                DxSchedulerDateTimeRange range = new DxSchedulerDateTimeRange(cancelled.StartDate,
                            cancelled.EndDate.AddDays(1));
                foreach (var a in dataStorage?.GetAppointments(range))
                {
                    Class c = (Class)a.CustomFields["Source"];
                    if (c != null && c.ID == cancelled.ClassID)
                    {
                        a.LabelId = 9;
                    }
                }
            }

            return dataStorage;
        }
        static async Task<List<ClassSchedule>> GetScheduleAsync(U3ADbContext dbc,
                        Term selectedTerm, IEnumerable<Venue> VenuesToFilter, bool IsCalendarView)
        {
            List<ClassSchedule> list = new List<ClassSchedule>();
            if (selectedTerm == null) { return list; }
            ClassSchedule schedule;
            List<Class> classes;
            if (IsCalendarView)
            {
                classes = await BusinessRule.SchedulledClassesAsync(dbc, selectedTerm);
            }
            else
            {
                classes = await BusinessRule.SchedulledClassesWithCourseEnrolmentsAsync(dbc, selectedTerm);
            }
            foreach (Class c in classes)
            {
                if (VenuesToFilter.Count() <= 0 || VenuesToFilter.Where(x => x.ID == c.VenueID).Any())
                {
                    if (isOfferedInTerm(selectedTerm, c))
                    {
                        OccurrenceType occurrenceType = (OccurrenceType)c.OccurrenceID;
                        switch (occurrenceType)
                        {
                            case OccurrenceType.FirstAndThirdWeekOfMonth:
                                list.Add(CreateSchedule(selectedTerm, c, OccurrenceType.FirstWeekOfMonth));
                                list.Add(CreateSchedule(selectedTerm, c, OccurrenceType.ThirdWeekOfMonth));
                                break;
                            case OccurrenceType.SecondAndFourthWeekOfMonth:
                                list.Add(CreateSchedule(selectedTerm, c, OccurrenceType.SecondWeekOfMonth));
                                list.Add(CreateSchedule(selectedTerm, c, OccurrenceType.FourthWeekOfMonth));
                                break;
                            default:
                                list.Add(CreateSchedule(selectedTerm, c, occurrenceType));
                                break;
                        }
                    }
                }
            }
            return list;
        }

        private static ClassSchedule CreateSchedule(Term selectedTerm, Class c, OccurrenceType occurrenceType)
        {
            ClassSchedule schedule = new ClassSchedule();
            if (c.StartDate.HasValue)
            {
                schedule.StartDate = GetDateTime(c.StartDate.Value, c.StartTime);
            }
            else
            {
                schedule.StartDate = GetDateTime(selectedTerm.StartDate, c.StartTime);
            }
            schedule.EndDate = GetDateTime(schedule.StartDate, c.Course.Duration);

            if ((OccurrenceType?)c.OccurrenceID != OccurrenceType.OnceOnly)
            {
                schedule.AppointmentType = 1;
            }
            else
            {
                schedule.AppointmentType = 0;
            }

            schedule.Caption = c.Course.Name;
            schedule.Description = (c.Leader != null) ? c.Leader.FullName : "";
            schedule.Location = c.Venue.Name;
            schedule.Label = GetLabelStatus(c, selectedTerm);
            schedule.AllDay = false;
            schedule.Recurrence = GetRecurrence(c, selectedTerm, occurrenceType);
            schedule.Class = c;
            return schedule;
        }

        static async Task<List<ClassSchedule>> GetPublicHolidays(U3ADbContext dbc)
        {
            var list = new List<ClassSchedule>();
            foreach (var ph in (await dbc.PublicHoliday.AsNoTracking().ToArrayAsync()))
            {
                // Add the holiday as an all-day event
                list.Add(new ClassSchedule()
                {
                    StartDate = ph.Date,
                    EndDate = ph.Date,
                    AllDay = true,
                    Caption = ph.Name
                });
            }
            return list;
        }
        public static DateTime? GetClassEndDate(Class c, Term selectedTerm)
        {
            DateTime? result = null;
            Term thisTerm = selectedTerm;
            List<ClassSchedule> list = new List<ClassSchedule>();
            var schedule = new ClassSchedule();
            if (c.StartDate.HasValue)
            {
                schedule.StartDate = GetDateTime(c.StartDate.Value, c.StartTime);
            }
            else
            {
                if (IsClassInTerm(c, thisTerm.TermNumber))
                {
                    schedule.StartDate = GetDateTime(thisTerm.StartDate, c.StartTime);
                }
                if (schedule.StartDate == DateTime.MinValue)
                {
                    var endDate = selectedTerm.StartDate.AddMinutes(-1);
                    Log.Warning("        End Date set to Term Start date minus 1 minute {p0}", endDate);
                    Log.Warning("        Because clacluated start date is null.");
                    return endDate;
                }
            }
            schedule.EndDate = GetDateTime(schedule.StartDate, c.Course.Duration);

            if ((OccurrenceType?)c.OccurrenceID != OccurrenceType.OnceOnly)
            {
                schedule.AppointmentType = 1;
            }
            else
            {
                schedule.AppointmentType = 0;
            }
            OccurrenceType occurrenceType = (OccurrenceType)c.OccurrenceID;
            if (occurrenceType == OccurrenceType.FirstAndThirdWeekOfMonth) { occurrenceType = OccurrenceType.ThirdWeekOfMonth; }
            if (occurrenceType == OccurrenceType.SecondAndFourthWeekOfMonth) { occurrenceType = OccurrenceType.FourthWeekOfMonth; }
            schedule.Recurrence = GetRecurrence(c, thisTerm, occurrenceType);
            list.Add(schedule);
            DxSchedulerDataStorage dataStorage = new DxSchedulerDataStorage()
            {
                AppointmentsSource = list,
                AppointmentMappings = new DxSchedulerAppointmentMappings()
                {
                    Type = "AppointmentType",
                    Start = "StartDate",
                    End = "EndDate",
                    Subject = "Caption",
                    AllDay = "AllDay",
                    Location = "Location",
                    Description = "Description",
                    LabelId = "Label",
                    RecurrenceInfo = "Recurrence",
                    CustomFieldMappings = new List<DxSchedulerCustomFieldMapping> {
                            new DxSchedulerCustomFieldMapping { Name = "Source", Mapping = "Class" }
                        }
                },
                AppointmentLabelsSource = new List<LabelObject>()
            };
            var range = new DxSchedulerDateTimeRange(schedule.StartDate,
                            new DateTime(thisTerm.Year, 12, 31));
            var a = dataStorage.GetAppointments(range)
                        .OrderByDescending(x => x.End).FirstOrDefault();
            if (a != null) result = a.End;
            return result;
        }

        static int GetLabelStatus(Class c, Term term)
        {
            int result = 0;
            var enrolments = (from e in c.Course.Enrolments
                              where e.TermID == term.ID
                              select e).ToList();
            string ClassStatus = BusinessRule.GetCourseEnrolmentStatus(c.Course, enrolments);
            if (ClassStatus.ToLower() == "undersubscribed") { result = 0; }
            if (ClassStatus.ToLower() == "good to go") { result = 1; }
            if (ClassStatus.ToLower() == "oversubscribed") { result = 2; }
            if (ClassStatus.ToLower() == "off-schedule activity") { result = 3; }
            return result;
        }
        static string GetRecurrence(Class c, Term term, OccurrenceType occurrenceType)
        {
            DxSchedulerRecurrenceInfo info = new DxSchedulerRecurrenceInfo() { Id = Guid.NewGuid() };
            switch (occurrenceType)
            {
                case OccurrenceType.OnceOnly:
                    {
                        break;
                    }
                case OccurrenceType.Daily:
                    {
                        info.Type = SchedulerRecurrenceType.Daily;
                        info.WeekDays = SchedulerWeekDays.WorkDays;
                        SetupDates(ref info, c, term);
                        break;
                    }
                case OccurrenceType.Weekly:
                    {
                        info.Type = SchedulerRecurrenceType.Weekly;
                        switch (c.OnDayID)
                        {
                            case 0: { info.WeekDays = SchedulerWeekDays.Sunday; break; }
                            case 1: { info.WeekDays = SchedulerWeekDays.Monday; break; }
                            case 2: { info.WeekDays = SchedulerWeekDays.Tuesday; break; }
                            case 3: { info.WeekDays = SchedulerWeekDays.Wednesday; break; }
                            case 4: { info.WeekDays = SchedulerWeekDays.Thursday; break; }
                            case 5: { info.WeekDays = SchedulerWeekDays.Friday; break; }
                            case 6: { info.WeekDays = SchedulerWeekDays.Saturday; break; }
                        }
                        SetupDates(ref info, c, term);
                        break;
                    }
                case OccurrenceType.Fortnightly:
                    {
                        info.Type = SchedulerRecurrenceType.Weekly;
                        switch (c.OnDayID)
                        {
                            case 0: { info.WeekDays = SchedulerWeekDays.Sunday; break; }
                            case 1: { info.WeekDays = SchedulerWeekDays.Monday; break; }
                            case 2: { info.WeekDays = SchedulerWeekDays.Tuesday; break; }
                            case 3: { info.WeekDays = SchedulerWeekDays.Wednesday; break; }
                            case 4: { info.WeekDays = SchedulerWeekDays.Thursday; break; }
                            case 5: { info.WeekDays = SchedulerWeekDays.Friday; break; }
                            case 6: { info.WeekDays = SchedulerWeekDays.Saturday; break; }
                        }
                        SetupDates(ref info, c, term);
                        info.Frequency = 2;
                        break;
                    }
                case OccurrenceType.FirstWeekOfMonth:
                    {
                        info.Type = SchedulerRecurrenceType.Monthly;
                        switch (c.OnDayID)
                        {
                            case 0: { info.WeekDays = SchedulerWeekDays.Sunday; break; }
                            case 1: { info.WeekDays = SchedulerWeekDays.Monday; break; }
                            case 2: { info.WeekDays = SchedulerWeekDays.Tuesday; break; }
                            case 3: { info.WeekDays = SchedulerWeekDays.Wednesday; break; }
                            case 4: { info.WeekDays = SchedulerWeekDays.Thursday; break; }
                            case 5: { info.WeekDays = SchedulerWeekDays.Friday; break; }
                            case 6: { info.WeekDays = SchedulerWeekDays.Saturday; break; }
                        }
                        SetupDates(ref info, c, term);
                        info.WeekOfMonth = SchedulerWeekOfMonth.First;
                        break;
                    }
                case OccurrenceType.SecondWeekOfMonth:
                    {
                        info.Type = SchedulerRecurrenceType.Monthly;
                        switch (c.OnDayID)
                        {
                            case 0: { info.WeekDays = SchedulerWeekDays.Sunday; break; }
                            case 1: { info.WeekDays = SchedulerWeekDays.Monday; break; }
                            case 2: { info.WeekDays = SchedulerWeekDays.Tuesday; break; }
                            case 3: { info.WeekDays = SchedulerWeekDays.Wednesday; break; }
                            case 4: { info.WeekDays = SchedulerWeekDays.Thursday; break; }
                            case 5: { info.WeekDays = SchedulerWeekDays.Friday; break; }
                            case 6: { info.WeekDays = SchedulerWeekDays.Saturday; break; }
                        }
                        SetupDates(ref info, c, term);
                        info.WeekOfMonth = SchedulerWeekOfMonth.Second;
                        break;
                    }
                case OccurrenceType.ThirdWeekOfMonth:
                    {
                        info.Type = SchedulerRecurrenceType.Monthly;
                        switch (c.OnDayID)
                        {
                            case 0: { info.WeekDays = SchedulerWeekDays.Sunday; break; }
                            case 1: { info.WeekDays = SchedulerWeekDays.Monday; break; }
                            case 2: { info.WeekDays = SchedulerWeekDays.Tuesday; break; }
                            case 3: { info.WeekDays = SchedulerWeekDays.Wednesday; break; }
                            case 4: { info.WeekDays = SchedulerWeekDays.Thursday; break; }
                            case 5: { info.WeekDays = SchedulerWeekDays.Friday; break; }
                            case 6: { info.WeekDays = SchedulerWeekDays.Saturday; break; }
                        }
                        SetupDates(ref info, c, term);
                        info.WeekOfMonth = SchedulerWeekOfMonth.Third;
                        break;
                    }
                case OccurrenceType.FourthWeekOfMonth:
                    {
                        info.Type = SchedulerRecurrenceType.Monthly;
                        switch (c.OnDayID)
                        {
                            case 0: { info.WeekDays = SchedulerWeekDays.Sunday; break; }
                            case 1: { info.WeekDays = SchedulerWeekDays.Monday; break; }
                            case 2: { info.WeekDays = SchedulerWeekDays.Tuesday; break; }
                            case 3: { info.WeekDays = SchedulerWeekDays.Wednesday; break; }
                            case 4: { info.WeekDays = SchedulerWeekDays.Thursday; break; }
                            case 5: { info.WeekDays = SchedulerWeekDays.Friday; break; }
                            case 6: { info.WeekDays = SchedulerWeekDays.Saturday; break; }
                        }
                        SetupDates(ref info, c, term);
                        info.WeekOfMonth = SchedulerWeekOfMonth.Fourth;
                        break;
                    }
                case OccurrenceType.LastWeekOfMonth:
                    {
                        info.Type = SchedulerRecurrenceType.Monthly;
                        switch (c.OnDayID)
                        {
                            case 0: { info.WeekDays = SchedulerWeekDays.Sunday; break; }
                            case 1: { info.WeekDays = SchedulerWeekDays.Monday; break; }
                            case 2: { info.WeekDays = SchedulerWeekDays.Tuesday; break; }
                            case 3: { info.WeekDays = SchedulerWeekDays.Wednesday; break; }
                            case 4: { info.WeekDays = SchedulerWeekDays.Thursday; break; }
                            case 5: { info.WeekDays = SchedulerWeekDays.Friday; break; }
                            case 6: { info.WeekDays = SchedulerWeekDays.Saturday; break; }
                        }
                        SetupDates(ref info, c, term);
                        info.WeekOfMonth = SchedulerWeekOfMonth.Last;
                        break;
                    }
                case OccurrenceType.Every5Weeks:
                    {
                        info.Type = SchedulerRecurrenceType.Weekly;
                        switch (c.OnDayID)
                        {
                            case 0: { info.WeekDays = SchedulerWeekDays.Sunday; break; }
                            case 1: { info.WeekDays = SchedulerWeekDays.Monday; break; }
                            case 2: { info.WeekDays = SchedulerWeekDays.Tuesday; break; }
                            case 3: { info.WeekDays = SchedulerWeekDays.Wednesday; break; }
                            case 4: { info.WeekDays = SchedulerWeekDays.Thursday; break; }
                            case 5: { info.WeekDays = SchedulerWeekDays.Friday; break; }
                            case 6: { info.WeekDays = SchedulerWeekDays.Saturday; break; }
                        }
                        SetupDates(ref info, c, term);
                        info.Frequency = 5;
                        break;
                    }
                case OccurrenceType.Every6Weeks:
                    {
                        info.Type = SchedulerRecurrenceType.Weekly;
                        switch (c.OnDayID)
                        {
                            case 0: { info.WeekDays = SchedulerWeekDays.Sunday; break; }
                            case 1: { info.WeekDays = SchedulerWeekDays.Monday; break; }
                            case 2: { info.WeekDays = SchedulerWeekDays.Tuesday; break; }
                            case 3: { info.WeekDays = SchedulerWeekDays.Wednesday; break; }
                            case 4: { info.WeekDays = SchedulerWeekDays.Thursday; break; }
                            case 5: { info.WeekDays = SchedulerWeekDays.Friday; break; }
                            case 6: { info.WeekDays = SchedulerWeekDays.Saturday; break; }
                        }
                        SetupDates(ref info, c, term);
                        info.Frequency = 6;
                        break;
                    }
            }
            return info?.ToXml();
        }

        static void SetupDates(ref DxSchedulerRecurrenceInfo info, Class c, Term term)
        {
            if (c.StartDate.HasValue)
            {
                info.Start = GetDateTime(c.StartDate.Value, c.StartTime);
                if (info.Start < term.StartDate && IsClassInTerm(c, term.TermNumber))
                {
                    info.Start = GetDateTime(term.StartDate, c.StartTime);
                }
            }
            else
            {
                info.Start = GetDateTime(term.StartDate, c.StartTime);
            }
            if (c.Recurrence.HasValue)
            {
                info.OccurrenceCount = c.Recurrence.Value;
                info.Range = SchedulerRecurrenceRange.OccurrenceCount;
            }
            else
            {
                info.End = term.EndDate;
                info.Range = SchedulerRecurrenceRange.EndByDate;
            }
        }

        static DateTime GetDateTime(DateTime DatePart, DateTime TimePart)
        {
            return new DateTime(DatePart.Year, DatePart.Month, DatePart.Day,
                                TimePart.Hour, TimePart.Minute, 0);
        }
        static DateTime GetDateTime(DateTime DatePart, decimal Duration)
        {
            return DatePart.AddHours((double)Duration);
        }
        static bool isOfferedInTerm(Term selectedTerm, Class c)
        {
            bool result = false;
            //if (c.StartDate.HasValue && c.Recurrence.HasValue && c.Recurrence > 0) result = true;
            //if (OccurrenceType.OnceOnly == (OccurrenceType)c.OccurrenceID) result = true;
            if (c.OfferedTerm1 && selectedTerm.TermNumber == 1) { result = true; }
            if (c.OfferedTerm2 && selectedTerm.TermNumber == 2) { result = true; }
            if (c.OfferedTerm3 && selectedTerm.TermNumber == 3) { result = true; }
            if (c.OfferedTerm4 && selectedTerm.TermNumber == 4) { result = true; }
            return result;
        }

    }

}
