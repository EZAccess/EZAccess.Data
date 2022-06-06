using System.Net;

namespace EZAccess.Data;

public class EZRecordset<TModel> where TModel : new()
{
    #region Public Readonly Properties

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

    /// <summary>
    /// A message available for the UI if something is wrong
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Any CRUD operation did not execute succesfully
    /// </summary>
    public bool HasFailedOperation { get; private set; }

    /// <summary>
    /// Informs whether the recordset has CRUD functions (Is not readonly) or
    /// whether it has not (Is readonly).
    /// </summary>
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// returns the number of records which are changed and not saved.
    /// </summary>
    public int ChangedRecordsCount
    {
        get { return _changedRecords.Count; }
    }
    #endregion

    #region Public Editable Properties

    /// <summary>
    /// If true then try to save changed records at certain events. If false then
    /// only save changes by explicit commands.
    /// </summary>
    public bool SaveChangesAutomatic { get; set; }

    #endregion

    #region Private Fields
    private Action? _onChange;
    private readonly Func<Task<EZActionResult<List<TModel>?>>>? _getAllRecords;
    private readonly Func<TModel, bool>? _where;
    private readonly Func<TModel, Task<EZActionResult<TModel?>>>? _createRecord;
    private readonly Func<TModel, Task<EZActionResult<TModel?>>>? _readRecord;
    private readonly Func<TModel, Task<EZActionResult<TModel?>>>? _updateRecord;
    private readonly Func<TModel, Task<EZActionResult<bool>>>? _deleteRecord;
    private readonly List<EZRecord<TModel>> _changedRecords = new();
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
                       Func<TModel, Task<EZActionResult<bool>>> deleteRecord,
                       bool saveChangesAutomatic = true)
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
        SaveChangesAutomatic = saveChangesAutomatic;
    }

    #endregion

    #region Core Functions

    /// <summary>
    /// This function starts the refresh function on a seperate thread. Use the AddOnChangeListener
    /// to listen to the onchange event when the function is finished. 
    /// The StartRefreshData is one of the ways to initially populate the recordset.
    /// </summary>
    public void StartRefreshData()
    {
        Task.Run(RefreshDataAsync);
    }

    /// <summary>
    /// This function executes the refresh function asynchrious and can be awaited.
    /// The RefreshDataAsync function is one of the ways to  initially populate the recordset.
    /// </summary>
    /// <returns>Returns a Task to execute the RefreshData async</returns>
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

    /// <summary>
    /// This function (re)populates the recordset with records. It is a requirement that 
    /// the list Data is already populated
    /// </summary>
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
            // If (IsReadOnly) could be used, but the compiler likes this better.
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
                    Records.Add(new EZRecord<TModel>(item, 
                                                          _createRecord, 
                                                          _readRecord, 
                                                          _updateRecord, 
                                                          _deleteRecord, 
                                                          OnStateHasChanged));
                }
                AddNewRecord2(new TModel());
            }
        }
        SelectedRecord = Records.FirstOrDefault();

    }

    /// <summary>
    /// This function will save all changes known to the recordset.
    /// </summary>
    public void SaveAllChanges()
    {
        foreach (var record in _changedRecords)
        {
            if (!record.IsBusy)
            {
                record.SaveChanges();
            }
        }
    }

    /// <summary>
    /// This function adds a new record to the existing recordset. It will also trigger events.
    /// </summary>
    public void AddNewRecord()
    {
        AddNewRecord2(new TModel());
        RecordsHaveChanged?.Invoke(this, new());
        _onChange?.Invoke();
    }

    /// <summary>
    /// This function adds a single record to the recordset after it is properly initialized.
    /// No events will be triggered. 
    /// </summary>
    /// <param name="newModel">The model that will populate the list Data and is contained by the record</param>
    private void AddNewRecord2(TModel newModel)
    {
        if (_createRecord != null && _readRecord != null && _updateRecord != null && _deleteRecord != null)
        {
            Data.Add(newModel);
            Records.Add(new EZRecord<TModel>(newModel, 
                                                  _createRecord, 
                                                  _readRecord, 
                                                  _updateRecord, 
                                                  _deleteRecord, 
                                                  OnStateHasChanged, 
                                                  true));
        }
    }

    /// <summary>
    /// This function will remove the record from the list of records, plus the model will
    /// be removed from the list of Data.
    /// </summary>
    /// <param name="deletedRecord">The record that will be removed</param>
    private void RemoveRecord(EZRecord<TModel> deletedRecord)
    {
        if (Records.Contains(deletedRecord))
        {
            Data.Remove(deletedRecord.Model);
            Records.Remove(deletedRecord);
            _onChange?.Invoke();
        }
        else
        {
            throw new InvalidDataException("The record that is requested to be deleted does " +
                                            "not exist in the list of records of this recordset");
        }
    }

    /// <summary>
    /// This is an event listener that routes the different action when any of the member records 
    /// is changed.
    /// </summary>
    /// <param name="args">Eventargs that contain the record that has been changed.</param>
    public void OnStateHasChanged(EZRecordsetStateHasChangedEventArgs args)
    {
        if (args.Record == null) { return; }
        var changedRecord = (EZRecord<TModel>)args.Record;

        // if any change is made to a new record create a new 'new' record. Only do this if the flag
        // Ischanged is not yet set to true, or the new record is being deleted.
        if (changedRecord.IsNewRecord && !changedRecord.IsChanged && !changedRecord.IsDeleted)
        {
            AddNewRecord();
        }
        // If the flag IsDeleted is set to true the recordset must remove the record from its lists
        if (changedRecord.IsDeleted)
        {
            RemoveRecord(changedRecord);
        }
        // The record will notify that changes to other records may be made. This is only
        // executed if the setting SaveChangesAutomatic in the recordset is set to true.
        if (args.TrySaveRecords && SaveChangesAutomatic)
        {
            foreach (var record in _changedRecords)
            {
                if (record != changedRecord && !record.IsBusy)
                {
                    record.SaveChanges();
                }
            }
        }
        // Check if the changed record is already member of the collection changed records,
        // and check whether it should be added or removed.
        // The collection changed records is recorded to know which records require to be saved
        // if so requested.
        if (_changedRecords.Contains(changedRecord))
        {
            if (!changedRecord.IsChanged)
            {
                _changedRecords.Remove(changedRecord);
            }
        }
        else
        {
            if (changedRecord.IsChanged)
            {
                _changedRecords.Add(changedRecord);
                //_onChange?.Invoke();
            }
        }
        _onChange?.Invoke();
    }

    #endregion

    #region Listerners

    /// <summary>
    /// The onchange listeners will be notified when any change is made to the recordset that
    /// require the UI to update.
    /// </summary>
    /// <param name="listener">Any action that will be invoked on change</param>
    public void AddOnChangeListeners(Action listener)
    {
        _onChange += listener;
    }

    /// <summary>
    /// Remove listeners set by the function AddOnChangeListeners
    /// </summary>
    /// <param name="listener">An Action that has been set to listen</param>
    public void RemoveOnChangeListeners(Action listener)
    {
        _onChange -= listener;
    }


    #endregion

}