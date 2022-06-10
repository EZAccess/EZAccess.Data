namespace EZAccess.Data;

public class EZStateHasChangedEventArgs: EventArgs
{
    public EZStateHasChangedEventArgs(object? record)
    {
        Record = record;
    }

    public object? Record { get; init; }
    public bool SaveRecords { get; init; }
    public bool SetFocus { get; init; }
    public bool NoFocus { get; init; }
}
