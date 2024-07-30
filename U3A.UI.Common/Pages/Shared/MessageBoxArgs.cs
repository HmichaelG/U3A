public class MessageBoxArgs
{
    public MessageBoxArgs()
    {
        Caption = String.Empty;
        Message = String.Empty;
        OKButtonText = "Ok";
        NoButtonText = "No";
        CancelButtonText = "Cancel";
        ShowCancelButton = true;
        ShowOkButton = true;
        ShowNoButton = false;
    }

    public string Caption { get; set; }
    public string Message { get; set; }
    public string OKButtonText { get; set; }
    public string NoButtonText { get; set; }
    public string CancelButtonText { get; set; }
    public bool ShowCancelButton { get; set; }
    public bool ShowOkButton { get; set; }
    public bool ShowNoButton { get; set; }
}

