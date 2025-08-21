using System;
using System.Collections.Generic;
using System.Text;

public class ICalendarExporter
{
    public class CalendarEvent
    {
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public string Uid { get; set; } = Guid.NewGuid().ToString();
    }

    public string Export(IEnumerable<CalendarEvent> events)
    {
        var sb = new StringBuilder();

        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//U3Admin.org.au//Schedule Exporter//EN");

        foreach (var evt in events)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{evt.Uid}");
            sb.AppendLine($"DTSTAMP:{FormatDateTimeUtc(DateTime.UtcNow)}");
            sb.AppendLine($"DTSTART:{FormatDateTimeUtc(evt.StartUtc)}");
            sb.AppendLine($"DTEND:{FormatDateTimeUtc(evt.EndUtc)}");
            sb.AppendLine($"SUMMARY:{EscapeText(evt.Summary)}");
            sb.AppendLine($"DESCRIPTION:{EscapeText(evt.Description)}");
            sb.AppendLine($"LOCATION:{EscapeText(evt.Location)}");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private string FormatDateTimeUtc(DateTime dt)
    {
        return dt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
    }

    private string EscapeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\n", "\\n");
    }
}