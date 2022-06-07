namespace EZAccess.Data;

public class EZStateHasChangedEventArgs: EventArgs
{
    public EZStateHasChangedEventArgs(object? record, bool trySaveRecords = false)
    {
        Record = record;
        TrySaveRecords = trySaveRecords;
    }

    public object? Record { get; }
    public bool TrySaveRecords { get; }
}
