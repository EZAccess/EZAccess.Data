namespace EZAccess.Data;

public class EZRecordset<TModel> : IDisposable where TModel : new()
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
    public EZRecord<TModel>? CurrentRecord { get; private set; }

    /// <summary>
    /// Returns the number of records in the set.
    /// </summary>
    public int RecordCount { 
        get 
        { 
            return Records.Count;
        } 
    }

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
    /// Allow refresh if the configuration contains a function to refresh.
    /// This function is simular to read, but read applies to a single record, 
    /// while refresh applies to the recordset.
    /// </summary>
    public bool AllowRefresh {
        get
        {
            return _configuration.GetAllRecords is not null || _configuration.GetRecordsWhere is not null;
        }
    }

    /// <summary>
    /// Allow create if the configuration contains a function to create a new record.
    /// </summary>
    public bool AllowCreate { 
        get 
        { 
            return _configuration.CreateRecord is not null; 
        } 
    }

    /// <summary>
    /// Allow read if the configuration contains a function to read a record.
    /// This function is simular to refresh, but applies to a single record.
    /// </summary>
    public bool AllowRead {
        get
        {
            return _configuration.ReadRecord is not null;
        }
    }

    /// <summary>
    /// Allow update if the configuration contains a function to update a record.
    /// </summary>
    public bool AllowUpdate {
        get
        {
            return _configuration.UpdateRecord is not null;
        }
    }

    /// <summary>
    /// Allow delete if the configuration contains a function to delete a record.
    /// </summary>
    public bool AllowDelete { 
        get
        {
            return _configuration.DeleteRecord is not null;
        }
    }

    /// <summary>
    /// returns the number of records which are changed and not saved.
    /// </summary>
    public int ChangedRecordsCount
    {
        get { return _changedRecords.Count; }
    }

    /// <summary>
    /// returns the number of records that are invalid and cannot be saved.
    /// </summary>
    public int InvalidRecordsCount 
    {
        get { return _invalidRecords.Count; }
    }

    /// <summary>
    /// Returns the index number of the currently selected record in the set.
    /// </summary>
    public int CurrentIndex { 
        get {
            if (CurrentRecord == null) {
                return 0;
            }
            else {
                return Records.IndexOf(CurrentRecord) + 1;
            }
        } 
    }

    /// <summary>
    /// Returns a list of EZField objects containing attribute information of the model fields.
    /// </summary>
    public IReadOnlyList<EZField> Fields { 
        get {
            return _fields;
        } 
    }


    #endregion

    #region Public Editable Properties

    /// <summary>
    /// If true then try to save changed records at certain events. If false then
    /// only save changes by explicit commands.
    /// </summary>
    public bool SaveChangesAutomatic { 
        get {
            return _configuration.SaveChangesAutomatic;
        }
        set { 
            _configuration.SaveChangesAutomatic = value;
        }
    }

    /// <summary>
    /// If true then create a new record as soon as the last new record is changed.
    /// </summary>
    public bool AddNewRecordAutomatic { get; set; }

    #endregion

    #region Private Fields
    private Action<object>? _onChange;
    private readonly List<EZRecord<TModel>> _changedRecords = new();
    private readonly List<EZRecord<TModel>> _invalidRecords = new();
    private EZRecord<TModel>? _newRecord;
    private readonly EZRecordsetConfiguration<TModel> _configuration;
    private readonly List<EZField> _fields;
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
        CurrentRecord = Records.FirstOrDefault();
        RefreshRecordSet();
        _configuration = new EZRecordsetConfiguration<TModel>();
        _fields = EZRecordset<TModel>.FillFieldList();
    }

    /// <summary>
    /// Initialize the recordset with functions to perform CRUD operations
    /// </summary>
    /// <param name="configuration">Configuration object contains configuration info for the Recordset</param>
    /// <exception cref="InvalidOperationException"></exception>
    public EZRecordset(EZRecordsetConfiguration<TModel> configuration)
    {
        Data = new List<TModel>();
        Records = new List<EZRecord<TModel>>();
        _configuration = configuration;
        _fields = EZRecordset<TModel>.FillFieldList();
        if (_configuration.GetAllRecords is null && _configuration.GetRecordsWhere is null)
        {
            throw new InvalidOperationException("The EZRecordset configuration requires either " +
                "a GetAllRecords or an GetRecordsWhere function!");
        }
        if (_configuration.CreateRecord is not null && _configuration.UpdateRecord is null)
        {
            throw new InvalidOperationException("The EZRecordset configuration requires " +
                "an UpdateRecord function if a CreateRecord function is provided!");
        }
        if (_configuration.UpdateRecord is not null && _configuration.ReadRecord is null)
        {
            throw new InvalidOperationException("The EZRecordset configuration requires " +
                "a ReadRecord function if an UpdateRecord function is provided!");
        }
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
            _onChange?.Invoke(this);
            if (_configuration.GetAllRecords != null) {
                var result = await _configuration.GetAllRecords();
                if (result?.Content != null) 
                {
                    if (_configuration.WhereFunc != null)
                    {
                        var content = result.Content;
                        Data = content.Where(_configuration.WhereFunc).ToList();
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
            _onChange?.Invoke(this);
        }
        catch (Exception ex) {
            IsBusy = false;
            HasFailedOperation = true;
            ErrorMessage = ex.Message;
            _onChange?.Invoke(this);
        }
    }

    /// <summary>
    /// This function (re)populates the recordset with records. It is a requirement that 
    /// the list Data is already populated
    /// </summary>
    private void RefreshRecordSet()
    {
        Records.Clear();
        _changedRecords.Clear();
        _invalidRecords.Clear();
        _newRecord = null;
        foreach (var item in Data)
        {
            Records.Add(new EZRecord<TModel>(this, item, _configuration, OnStateHasChanged));
        }
        if (AddNewRecordAutomatic && AllowCreate)
        {
            AddNewRecord(new TModel());
        }
        CurrentRecord = Records.FirstOrDefault();

    }

    /// <summary>
    /// This function will save all changes known to the recordset.
    /// </summary>
    public void SaveAllChanges()
    {
        foreach (var record in _changedRecords)
        {
            if (!record.IsBusy && !record.HasValidationErrors)
            {
                record.SaveChanges();
            }
        }
    }

    /// <summary>
    /// This function adds a new record to the existing recordset. It will also trigger events.
    /// </summary>
    public bool TryAddNewRecord()
    {
        if (_newRecord == null)
        {
            var newRecord = AddNewRecord(new TModel());
            RecordsHaveChanged?.Invoke(this, new());
            _onChange?.Invoke(this);
            return true;
        }
        return false;
    }

    /// <summary>
    /// This function adds a single record to the recordset after it is properly initialized.
    /// No events will be triggered. 
    /// </summary>
    /// <param name="newModel">The model that will populate the list Data and is contained by the record</param>
    private EZRecord<TModel>? AddNewRecord(TModel newModel)
    {
        if (_configuration.CreateRecord != null && 
            _configuration.ReadRecord != null && 
            _configuration.UpdateRecord != null && 
            _configuration.DeleteRecord != null)
        {
            Data.Add(newModel);
            _newRecord = new EZRecord<TModel>(this, newModel,
                                                  _configuration,
                                                  OnStateHasChanged,
                                                  true);
            Records.Add(_newRecord);
            return _newRecord;
        }
        return null;
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
            _onChange?.Invoke(this);
        }
        else
        {
            throw new InvalidDataException("The record that is requested to be deleted does " +
                                            "not exist in the list of records of this recordset");
        }
    }

    #endregion

    #region Navigation Functions

    public void SelectFirst()
    {
        CurrentRecord = Records.FirstOrDefault();
        CurrentRecord?.SetFocus(true);
    }

    public void SelectPrevious()
    {
        if (CurrentIndex > 1)
        {
            CurrentRecord = Records.ElementAt(CurrentIndex - 2);
            CurrentRecord?.SetFocus(true);
        }
    }

    public void SelectNext()
    {
        if (CurrentIndex < Records.Count)
        {
            CurrentRecord = Records.ElementAt(CurrentIndex);
            CurrentRecord?.SetFocus(true);
        }
    }

    public void SelectLast()
    {
        CurrentRecord = Records.LastOrDefault();
        CurrentRecord?.SetFocus(true);
    }

    public void GoToRecord(int index)
    {
        if (Records.Count >= index && index >= 1)
        {
            CurrentRecord = Records.ElementAt(index - 1);
            CurrentRecord?.SetFocus(true);
        }
    }

    public void FocusOrAddNewRecord()
    {
        TryAddNewRecord();
        _newRecord?.SetFocus(true);
    }

    #endregion

    #region EventListeners

    /// <summary>
    /// This is an event listener that routes the different action when any of the member records 
    /// is changed.
    /// </summary>
    /// <param name="args">Eventargs that contain the record that has been changed.</param>
    public void OnStateHasChanged(EZStateHasChangedEventArgs args)
    {
        if (args.Record == null) { return; }
        var changedRecord = (EZRecord<TModel>)args.Record;

        // if any change is made to a new record create a new 'new' record. Only do this if the flag
        // Ischanged is not yet set to true, or the new record is being deleted.
        if (changedRecord.IsNewRecord && !changedRecord.IsChanged && !changedRecord.IsDeleted && !args.SetFocus)
        {
            _newRecord = null;
            if (AddNewRecordAutomatic)
            {
                TryAddNewRecord();
            }
        }
        // If the flag IsDeleted is set to true the recordset must remove the record from its lists
        if (changedRecord.IsDeleted)
        {
            RemoveRecord(changedRecord);
        }
        // The record will notify that changes to other records may be made. This is only
        // executed if the setting SaveChangesAutomatic in the recordset is set to true.
        if (args.SaveRecords && SaveChangesAutomatic)
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
            }
        }

        // Check if the changed record is already member of the collection invalid records,
        // and check whether it should be added or removed.
        // The collection invalid records is recorded to know which records require to be saved
        // but cannot be saved.
        if (_invalidRecords.Contains(changedRecord))
        {
            if (!changedRecord.HasValidationErrors)
            {
                _invalidRecords.Remove(changedRecord);
            }
        }
        else
        {
            if (changedRecord.HasValidationErrors)
            {
                _invalidRecords.Add(changedRecord);
            }
        }

        if (!args.SetFocus && CurrentRecord != changedRecord && !args.NoFocus)
        {
            CurrentRecord = changedRecord;
            CurrentRecord?.SetFocus(true);
        }

        if (args.SetFocus)
        {
            CurrentRecord = changedRecord;
        }

        _onChange?.Invoke(this);
    }

    #endregion

    #region Listerners

    /// <summary>
    /// The onchange listeners will be notified when any change is made to the recordset that
    /// require the UI to update.
    /// </summary>
    /// <param name="listener">Any action that will be invoked on change</param>
    public void AddOnChangeListeners(Action<object> listener)
    {
        _onChange += listener;
    }

    /// <summary>
    /// Remove listeners set by the function AddOnChangeListeners
    /// </summary>
    /// <param name="listener">An Action that has been set to listen</param>
    public void RemoveOnChangeListeners(Action<object> listener)
    {
        _onChange -= listener;
    }

    #endregion

    #region Helper Functions
    private static List<EZField> FillFieldList()
    {
        List<EZField> list = new();
        var properties = typeof(TModel).GetProperties();
        foreach (var property in properties)
        {
            EZField field = new(typeof(TModel), property);
            list.Add(field);
        }
        return list;
    }

    #endregion

    public void Dispose()
    {
        throw new NotImplementedException();
    }

}