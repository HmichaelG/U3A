using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3A.Model
{
    public enum CourseEditViewType
    {
        [Display(Name = "Simplified: Just another course with one class.", ShortName = "Simplified")]
        Simplified,
        [Display(Name = "Activity: An event that occurs once only, including after close of term.", ShortName = "Activity")]
        Activity,
        [Display(Name = "Detail: Typically used when there are multiple classes per course.", ShortName = "Detail")]
        Detail,
        [Display(Name = "Settings: System settings applicable to all Course/Classes.", ShortName = "Settings")]
        Settings
    }

    [NotMapped]
    public class CourseEditViewTypeWrapper
    {
        public CourseEditViewType Type { get; set; }
        public string DisplayText { get; set; }
        public string ShortText { get; set; }
    }

    [NotMapped]
    public class CourseEditViewTypeList : List<CourseEditViewTypeWrapper>
    {

        public CourseEditViewTypeList()
        {
            AddRange(Enum.GetValues(typeof(CourseEditViewType))
                            .OfType<CourseEditViewType>()
                            .Select(t => new CourseEditViewTypeWrapper()
                            {
                                Type = t,
                                DisplayText = t.GetAttribute<DisplayAttribute>().Name,
                                ShortText = t.GetAttribute<DisplayAttribute>().ShortName
                            }).ToList());
        }

    }
    [NotMapped]
    public class SelectableCourseEditViewTypeList : List<CourseEditViewTypeWrapper>
    {

        public SelectableCourseEditViewTypeList()
        {
            AddRange(Enum.GetValues(typeof(CourseEditViewType))
                            .OfType<CourseEditViewType>()
                            .Where(t => t != CourseEditViewType.Settings)
                            .Select(t => new CourseEditViewTypeWrapper()
                            {
                                Type = t,
                                DisplayText = t.GetAttribute<DisplayAttribute>().Name,
                                ShortText = t.GetAttribute<DisplayAttribute>().ShortName
                            }).ToList());
        }

    }

}