using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task AutoEnrolMultiCampus(TenantInfo tenant,
                                        string tenantConnectionString,
                                        DateTime today)
        {
            using (var dbc = new U3ADbContext(tenant))
            {
                using (var dbcT = new TenantDbContext(tenantConnectionString))
                {

                    var mcEnrolments = dbcT.MultiCampusEnrolment
                                            .Where(x => x.TenantIdentifier == tenant.Identifier)
                                            .OrderByDescending(x => x.Created)
                                            .ToList();
                    if (mcEnrolments.Any(x => x.IsWaitlisted))
                    {
                        var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
                        var term = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
                        if (term != null)
                        {
                            var classes = await BusinessRule.GetClassDetailsAsync(dbc, term, settings);
                            foreach (var e in mcEnrolments)
                            {
                                if (e.ClassID != null)
                                {
                                    // Different participants in each class
                                    var c = classes.FirstOrDefault(x => x.ID == e.ClassID);
                                    if (c != null) { await ProcessMCenrolment(c, e, today); }
                                }
                                else
                                {
                                    // Same participants in each class
                                    var course = await dbc.Course
                                                    .Include(x => x.Classes)
                                                    .FirstOrDefaultAsync(x => x.ID == e.CourseID);
                                    if (course != null)
                                    {
                                        var c = course.Classes.FirstOrDefault();
                                        if (c != null) { await ProcessMCenrolment(c, e, today); }
                                    }
                                }
                            }
                            await BusinessRule.CreateMultiCampusEnrolmentSendMailAsync(dbcT, classes, settings);
                        }
                    }
                    await dbcT.SaveChangesAsync();
                }
            }
        }

        private static async Task ProcessMCenrolment(Class c, MultiCampusEnrolment e, DateTime today)
        {
            if (e.IsWaitlisted)
            {
                if (c.TotalActiveStudents < c.Course.MaximumStudents)
                {
                    e.DateEnrolled = today;
                    e.IsWaitlisted = false;
                    c.TotalActiveStudents++;
                    c.TotalWaitlistedStudents--;
                }
                else { c.TotalWaitlistedStudents++; }
            }
        }

    }
}