namespace EZAccess.Data;

public class EZRecordsetConfiguration<TModel>
{
    public Func<Task<EZActionResult<List<TModel>?>>>? GetAllRecords { get; set; }
    public Func<string?, Task<EZActionResult<List<TModel>?>>>? GetRecordsWhere { get; set; }
    public Func<TModel, bool>? WhereFunc { get; set; }
    public Func<TModel, Task<EZActionResult<TModel?>>>? CreateRecord { get; set; }
    public Func<TModel, Task<EZActionResult<TModel?>>>? ReadRecord { get; set; }
    public Func<TModel, Task<EZActionResult<TModel?>>>? UpdateRecord { get; set; }
    public Func<TModel, Task<EZActionResult<bool>>>? DeleteRecord { get; set; }
    public bool SaveChangesAutomatic { get; set; }
    public string? WhereString { get; set; }
}
