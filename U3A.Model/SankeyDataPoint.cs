namespace U3A.Model;

public class SankeyDataPoint
{
    public string Group { get; set; }
    public string Source { get; set; }
    public string Target { get; set; }
    public string PivotSource { get; set; }
    public string PivotTarget { get; set; }
    public long Count { get; set; }

}