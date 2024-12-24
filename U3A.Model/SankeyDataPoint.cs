namespace U3A.Model;

public class SankeyDataPoint
{
    public SankeyDataPoint() { }
    public SankeyDataPoint(string group, string source, string target, long count)
    {
        Group = group;
        Source = source;
        Target = target;
        Count = count;
    }
    public string Group { get; set; }
    public string Source { get; set; }
    public string Target { get; set; }
    public long Count { get; set; }
}
