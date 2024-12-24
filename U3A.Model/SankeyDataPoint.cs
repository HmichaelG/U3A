namespace U3A.Model;

public class SankeyDataPoint
{
    public SankeyDataPoint() { }
    public SankeyDataPoint(string group, string source, string target, long count, double? percent)
    {
        Group = group;
        Source = source;
        Target = target;
        Count = count;
        Percent = percent;
    }
    public string Group { get; set; }
    public string Source { get; set; }
    public string Target { get; set; }
    public long Count { get; set; }
    public double? Percent { get; set; }
}
