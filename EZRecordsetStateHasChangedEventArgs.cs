namespace EZAccess.Data;

public class EZRecordsetStateHasChangedEventArgs: EventArgs
{
    public EZRecordsetStateHasChangedEventArgs(object? record, bool trySaveRecords = false)
    {
        Record = record;
        TrySaveRecords = trySaveRecords;
    }

    public object? Record { get; }
    public bool TrySaveRecords { get; }
}
