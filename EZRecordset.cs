using System.Net;

namespace EZAccess.Data;

public class EZRecordset<TModel> where TModel : new()
{
    #region Public Properties

    /// <summary>
    /// Return the raw set of data as a List of model type TModel
    /// </summary>
    public List<TModel> Data { get; private set; }

    /// <summary>
    /// Return the data as list of records. The Records derive CRUD functions from the RecordSet object.
    /// </summary>
    public List<EZRecord<TModel>> Records { get; private set; }

    /// <summary>
    /// Return a single record that is currently selected
    /// </summary>
    public EZRecord<TModel>? SelectedRecord { get; private set; }

    /// <summary>
    /// Busy during execution of async actions
    /// </summary>
    public bool IsBusy { get; private set; }

    public string? ErrorMessage { get; private set; }
    public bool HasFailedOperation { get; private set; }
    public bool IsReadOnly { get; private set; }

    #endregion

    #region Private Fields
    private Action? _onChange;
    private readonly Func<Task<EZActionResult<List<TModel>?>>>? _getAllRecords;
    private readonly Func<TModel, bool>? _where;
    private readonly Func<TModel, Task<EZActionResult<TModel?>>>? _createRecord;
    private readonly Func<TModel, Task<EZActionResult<TModel?>>>? _readRecord;
    private readonly Func<TModel, Task<EZActionResult<TModel?>>>? _updateRecord;
    private readonly Func<TModel, Task<EZActionResult<bool>>>? _deleteRecord;
    private List<EZRecord<TModel>> ChangedRecords = new();
    #endregion

    #region Public Events
    public event EventHandler? RecordsHaveChanged;
    #endregion

    #region Class Constructors

    /// <summary>
    /// Initialize the recordset with the raw data only
    /// </summary>
    /// <param name="data">The raw data as List of TModel</param>
    public EZRecordset(List<TModel> data)
    {
        Data = data;
        Records = new List<EZRecord<TModel>>();
        SelectedRecord = Records.FirstOrDefault();
        RefreshRecordSet();
        IsReadOnly = true;
    }

    /// <summary>
    /// Initialize the recordset with a function to read the recordset. 
    /// </summary>
    /// <param name="getAllRecords">Function which is called when all records need load/refresh</param>
    public EZRecordset(Func<Task<EZActionResult<List<TModel>?>>> getAllRecords,
                       Func<TModel, bool>? where)
    {
        Data = new List<TModel>();
        Records = new List<EZRecord<TModel>>();
        _getAllRecords += getAllRecords;
        _where = where;
        IsReadOnly = true;
    }

    /// <summary>
    /// Initialize the recordset with the functions that allow reading all records and single records. 
    /// </summary>
    /// <param name="getAllRecords">Function which is called when all records need load/refresh</param>
    /// <param name="readRecord">Function which is called by a single record to refresh itself</param>
    public EZRecordset(Func<Task<EZActionResult<List<TModel>?>>> getAllRecords,
                       Func<TModel, bool>? where,
                       Func<TModel, Task<EZActionResult<TModel?>>> readRecord)
    {
        Data = new List<TModel>();
        Records = new List<EZRecord<TModel>>();
        _getAllRecords += getAllRecords;
        _where = where;
        _readRecord += readRecord;
        IsReadOnly = true;
    }

    /// <summary>
    /// Initialize the recordset with the functions that allow CRUD operations on the record en recordset. 
    /// </summary>
    /// <param name="getAllRecords">Function which is called when all records need load/refresh</param>
    /// <param name="createRecord">Function which is called when a new record is saved</param>
    /// <param name="readRecord">Function which is called by a single record to refresh itself</param>
    /// <param name="updateRecord">Function which is called when an existing record is saved</param>
    /// <param name="deleteRecord">Function which is called when a record is deleted</param>
    public EZRecordset(Func<Task<EZActionResult<List<TModel>?>>> getAllRecords,
                       Func<TModel, bool>? where,
                       Func<TModel, Task<EZActionResult<TModel?>>> createRecord,
                       Func<TModel, Task<EZActionResult<TModel?>>> readRecord,
                       Func<TModel, Task<EZActionResult<TModel?>>> updateRecord,
                       Func<TModel, Task<EZActionResult<bool>>> deleteRecord)
    {
        Data = new List<TModel>();
        Records = new List<EZRecord<TModel>>();
        _getAllRecords += getAllRecords;
        _where = where;
        _createRecord += createRecord;
        _readRecord += readRecord;
        _updateRecord += updateRecord;
        _deleteRecord += deleteRecord;
        IsReadOnly = false;
    }

    #endregion

    public void StartRefreshData()
    {
        Task.Run(RefreshDataAsync);
    }

    public async Task RefreshDataAsync()
    {
        if (IsBusy) { return; }
        try
        {
            HasFailedOperation = false;
            IsBusy = true;
            _onChange?.Invoke();
            if (_getAllRecords != null) {
                var result = await _getAllRecords();
                if (result?.Content != null) 
                {
                    if (_where != null)
                    {
                        var content = result.Content;
                        Data = content.Where(_where).ToList();
                    }
                    else
                    {
                        Data = result.Content;
                    }
                    //Data.Add(new());
                }
                else 
                {
                    Data.Clear();
                }
                RefreshRecordSet();
            }
            IsBusy = false;
            _onChange?.Invoke();
        }
        catch (Exception ex) {
            IsBusy = false;
            HasFailedOperation = true;
            ErrorMessage = ex.Message;
            _onChange?.Invoke();
        }
    }

    private void RefreshRecordSet()
    {
        Records.Clear();
        if (_readRecord == null)
        {
            foreach (var item in Data)
            {
                Records.Add(new EZRecord<TModel>(item));
            }
        }
        else
        {
            if (_createRecord == null || _updateRecord == null || _deleteRecord == null)
            {
                foreach (var item in Data)
                {
                    Records.Add(new EZRecord<TModel>(item, _readRecord, OnStateHasChanged));
                }
            }
            else
            {
                foreach (var item in Data)
                {
                    Records.Add(new EZRecord<TModel>(item, _createRecord, _readRecord, _updateRecord, _deleteRecord, OnStateHasChanged));
                }
                AddNewRecord(new TModel());
            }
        }
        SelectedRecord = Records.FirstOrDefault();

    }

    public void AddNewRecord()
    {
        AddNewRecord(new TModel());
        RecordsHaveChanged?.Invoke(this, new());
        _onChange?.Invoke();
    }

    private void AddNewRecord(TModel newModel)
    {
        if (_createRecord != null && _readRecord != null && _updateRecord != null && _deleteRecord != null)
        {
            Data.Add(newModel);
            Records.Add(new EZRecord<TModel>(newModel, _createRecord, _readRecord, _updateRecord, _deleteRecord, OnStateHasChanged, true));
        }
    }

    private void RemoveRecord(EZRecord<TModel> deletedRecord)
    {
        Data.Remove(deletedRecord.Model);
        Records.Remove(deletedRecord);
        _onChange?.Invoke();
    }

    public void OnStateHasChanged(EZRecordsetStateHasChangedEventArgs args)
    {
        if (args.Record == null) { return; }
        var changedRecord = (EZRecord<TModel>)args.Record;

        if (changedRecord.IsNewRecord && !changedRecord.IsChanged && !changedRecord.IsDeleted)
        {
            AddNewRecord();
        }
        if (changedRecord.IsDeleted)
        {
            RemoveRecord(changedRecord);
        }
        if (args.TrySaveRecords)
        {
            foreach (var record in ChangedRecords)
            {
                if (record != changedRecord && !record.IsBusy)
                {
                    record.SaveChanges();
                }
            }
        }
        if (ChangedRecords.Contains(changedRecord))
        {
            if (!changedRecord.IsChanged)
            {
                ChangedRecords.Remove(changedRecord);
            }
        }
        else
        {
            if (changedRecord.IsChanged)
            {
                ChangedRecords.Add(changedRecord);
            }
        }
        //_onChange?.Invoke();
    }

    #region Listerners

    public void AddOnChangeListeners(Action listener)
    {
        _onChange += listener;
    }

    public void RemoveOnChangeListeners(Action listener)
    {
        _onChange -= listener;
    }


    #endregion

}