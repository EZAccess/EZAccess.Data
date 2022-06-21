namespace EZAccess.Data;

public class EZRecordsetConfiguration<TModel>
{
    private readonly bool _allowRefresh;
    private readonly bool _allowCreate;
    private readonly bool _allowRead;
    private readonly bool _allowUpdate;
    private readonly bool _allowDelete;
    private Func<Task<EZActionResult<List<TModel>?>>>? getAllRecords;
    private Func<string?, Task<EZActionResult<List<TModel>?>>>? getRecordsWhere;
    private Func<TModel, Task<EZActionResult<TModel?>>>? createRecord;
    private Func<TModel, Task<EZActionResult<TModel?>>>? getRecord;
    private Func<TModel, Task<EZActionResult<TModel?>>>? updateRecord;
    private Func<TModel, Task<EZActionResult<bool>>>? deleteRecord;
    private IEZRecordsetService<TModel>? eZRecordsetService;

    public EZRecordsetConfiguration(bool allowRefresh = true,
                                    bool allowCreate = true,
                                    bool allowRead = true,
                                    bool allowUpdate = true,
                                    bool allowDelete = true)
    {
        _allowRefresh = allowRefresh;
        _allowCreate = allowCreate;
        _allowRead = allowRead;
        _allowUpdate = allowUpdate;
        _allowDelete = allowDelete;
    }

    public IEZRecordsetService<TModel>? EZRecordsetService 
    { 
        get => eZRecordsetService;
        set
        {
            eZRecordsetService = value;
            if (eZRecordsetService is not null)
            {
                GetAllRecords = eZRecordsetService.GetAllAsync;
                GetRecordsWhere = eZRecordsetService.GetAllWhereAsync;
                GetRecord = eZRecordsetService.GetAsync;
                CreateRecord = eZRecordsetService.CreateAsync;
                UpdateRecord = eZRecordsetService.UpdateAsync;
                DeleteRecord = eZRecordsetService.DeleteAsync;
            }
        }
    }

    public bool AllowRefresh => _allowRefresh && 
        (getAllRecords is not null || getRecordsWhere is not null);
    public bool AllowCreate => _allowCreate && createRecord is not null;
    public bool AllowRead => _allowRead && getRecord is not null;
    public bool AllowUpdate => _allowUpdate && updateRecord is not null;
    public bool AllowDelete => _allowDelete && deleteRecord is not null;

    public Func<Task<EZActionResult<List<TModel>?>>>? GetAllRecords
    {
        get => getAllRecords;
        set 
        {
            getAllRecords = _allowRefresh ? value : null; 
        }
    }

    public Func<string?, Task<EZActionResult<List<TModel>?>>>? GetRecordsWhere
    {
        get => getRecordsWhere;
        set
        {
            getRecordsWhere = _allowRefresh ? value : null;
        }
    }

    public Func<TModel, Task<EZActionResult<TModel?>>>? CreateRecord
    {
        get => createRecord;
        set
        {
            createRecord = _allowCreate ? value : null;
        }
    }

    public Func<TModel, Task<EZActionResult<TModel?>>>? GetRecord
    {
        get => getRecord;
        set
        {
            getRecord = _allowRead ? value : null;
        }
    }

    public Func<TModel, Task<EZActionResult<TModel?>>>? UpdateRecord
    {
        get => updateRecord;
        set
        {
            updateRecord = _allowUpdate ? value : null;
        }
    }

    public Func<TModel, Task<EZActionResult<bool>>>? DeleteRecord
    {
        get => deleteRecord;
        set
        {
            deleteRecord = _allowDelete ? value : null;
        }
    }

    public Func<TModel, bool>? WhereFunc { get; set; }
    public string? WhereString { get; set; }
    public bool SaveChangesAutomatic { get; set; }
    public bool AwaitResult { get; set; }
}
