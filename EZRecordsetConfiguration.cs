namespace EZAccess.Data;

public class EZRecordsetConfiguration<TModel>
{
    private bool _allowRefresh;
    private bool _allowCreate;
    private bool _allowRead;
    private bool _allowUpdate;
    private bool _allowDelete;
    private readonly IEZRecordsetService<TModel>? _eZRecordsetService;
    private Func<Task<EZActionResult<List<TModel>?>>>? _getAllRecords;
    private Func<string?, Task<EZActionResult<List<TModel>?>>>? _getRecordsWhere;
    private Func<TModel, Task<EZActionResult<TModel?>>>? _createRecord;
    private Func<TModel, Task<EZActionResult<TModel?>>>? _getRecord;
    private Func<TModel, Task<EZActionResult<TModel?>>>? _updateRecord;
    private Func<TModel, Task<EZActionResult<bool>>>? _deleteRecord;


    /// <summary>
    /// Use this constructor when the data is provided without a service. The recordset will be 
    /// readonly.
    /// </summary>
    public EZRecordsetConfiguration()
    {
        _allowRefresh = false;
        _allowCreate = false;
        _allowRead = false;
        _allowUpdate = false;
        _allowDelete = false;
    }

    /// <summary>
    /// Use this constructor when the data is provided with a service class of type 
    /// IEZRecordsetService. Set the option of readonly to true if only an initial dataset 
    /// is loaded. For full functionality setthe option readonly to false (default).
    /// </summary>
    /// <param name="service">Provide the service class of type IEZRecordsetService. This service 
    /// is used for all CRUD operations.</param>
    /// <param name="readOnly">Set the recordset to readonly if required. If true all CRUD 
    /// operations are accessible.</param>
    public EZRecordsetConfiguration(IEZRecordsetService<TModel> service, 
                                    bool readOnly = false)
    {
        _allowRefresh = !readOnly;
        _allowCreate = !readOnly;
        _allowRead = !readOnly;
        _allowUpdate = !readOnly;
        _allowDelete = !readOnly;
        _eZRecordsetService = service;
        IEZRecordsetService();
    }

    public EZRecordsetConfiguration(IEZRecordsetService<TModel> service,
                                    bool allowCreate,
                                    bool allowRead,
                                    bool allowUpdate,
                                    bool allowDelete)
    {
        if (allowCreate && !allowUpdate)
        {
            throw new InvalidOperationException("When it is possible to create a record, it " +
                "should also be possible to save changes.");
        }
        if (allowRead && !allowUpdate)
        {
            throw new InvalidOperationException("When it is possible to update a record, " +
                "it should also be possible to read a record.");
        }
        _allowRefresh = true;
        _allowCreate = allowCreate;
        _allowRead = allowRead;
        _allowUpdate = allowUpdate;
        _allowDelete = allowDelete;
        _eZRecordsetService = service;
        IEZRecordsetService();
    }

    public IEZRecordsetService<TModel>? Service => _eZRecordsetService;
    private void IEZRecordsetService()
    { 
        if (_eZRecordsetService is not null)
        {
            _getAllRecords = _eZRecordsetService.GetAllAsync;
            _getRecordsWhere = _eZRecordsetService.GetAllWhereAsync;
            _getRecord = _eZRecordsetService.GetAsync;
            _createRecord = _eZRecordsetService.CreateAsync;
            _updateRecord = _eZRecordsetService.UpdateAsync;
            _deleteRecord = _eZRecordsetService.DeleteAsync;
        }
    }

    public bool AllowRefresh => _allowRefresh && 
        (_getAllRecords is not null || _getRecordsWhere is not null);
    public bool AllowCreate => _allowCreate && _createRecord is not null;
    public bool AllowRead => _allowRead && _getRecord is not null;
    public bool AllowUpdate => _allowUpdate && _updateRecord is not null;
    public bool AllowDelete => _allowDelete && _deleteRecord is not null;

    public Func<Task<EZActionResult<List<TModel>?>>>? GetAllRecords
    {
        get => _getAllRecords;
        init 
        {
            if (value is null) throw new ArgumentNullException(nameof(GetAllRecords));
            _getAllRecords = value;
            _allowRefresh = true;
        }
    }

    public Func<string?, Task<EZActionResult<List<TModel>?>>>? GetRecordsWhere
    {
        get => _getRecordsWhere;
        init
        {
            if (value is null) throw new ArgumentNullException(nameof(GetRecordsWhere));
            _getRecordsWhere = value;
            _allowRefresh = true;
        }
    }

    public Func<TModel, Task<EZActionResult<TModel?>>>? CreateRecord
    {
        get => _createRecord;
        init
        {
            if (_updateRecord is null && value is not null)
            {
                throw new InvalidOperationException("The property CreateRecord can only be set if " +
                    "also the property UpdateRecord is set.");
            }
            _createRecord = value;
            _allowCreate = (_createRecord is not null);
        }
    }

    public Func<TModel, Task<EZActionResult<TModel?>>>? GetRecord
    {
        get => _getRecord;
        init
        {
            _getRecord = value;
            _allowRead = (_getRecord is not null);
            if (_getRecord is null) 
            {
                CreateRecord = null;
                UpdateRecord = null;
            }
        }
    }

    public Func<TModel, Task<EZActionResult<TModel?>>>? UpdateRecord
    {
        get => _updateRecord;
        init
        {
            if (_getRecord is null && value is not null)
            {
                throw new InvalidOperationException("The property UpdateRecord can only be set if " +
                    "also the property GetRecord is set.");
            }
            _updateRecord = value;
            _allowUpdate = (_updateRecord is not null);
            if (_updateRecord is null)
            {
                CreateRecord = null;
            }
        }
    }

    public Func<TModel, Task<EZActionResult<bool>>>? DeleteRecord
    {
        get => _deleteRecord;
        init
        {
            _deleteRecord = value;
            _allowDelete = (_deleteRecord is not null);
        }
    }

    public Func<TModel, bool>? WhereFunc { get; init; }
    public string? WhereString { get; init; }
    public bool AwaitResult { get; init; }
    public bool SaveChangesAutomatic { get; set; }
}
