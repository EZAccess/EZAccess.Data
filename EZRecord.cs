namespace EZAccess.Data;

public class EZRecord<TModel> : IDisposable where TModel : new()
{
    #region Public Properties
    public TModel Model { get; private set; }
    //public List<EZField> Fields { get; }
    //public bool IsReadOnly { get; private set; }
    public bool IsChanged { get; private set; }
    public bool IsDeleted { get; private set; }
    public bool IsNewRecord { get; private set; }
    public bool IsBusy { get; private set; }
    public bool IsSaved { get; private set; }
    public bool HasFailedOperation { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Dictionary<string, List<string>> ValidationErrors { get; private set; }
    public bool HasValidationErrors
    {
        get { 
            if (ValidationErrors == null)
            {
                return false;
            }
            else
            {
                return ValidationErrors.Any();
            }
        }
    }
    public bool HasFocus
    {
        get {
            return _parent.CurrentRecord == this;
        }
    }

    public bool DeleteRequested { get; private set; }

    /// <summary>
    /// Allow create if the configuration contains a function to create a new record.
    /// </summary>
    public bool AllowCreate
    {
        get
        {
            return _configuration.CreateRecord is not null;
        }
    }

    /// <summary>
    /// Allow read if the configuration contains a function to read a record.
    /// This function is simular to refresh, but applies to a single record.
    /// </summary>
    public bool AllowRead
    {
        get
        {
            return _configuration.ReadRecord is not null;
        }
    }

    /// <summary>
    /// Allow update if the configuration contains a function to update a record.
    /// </summary>
    public bool AllowUpdate
    {
        get
        {
            return _configuration.UpdateRecord is not null;
        }
    }

    /// <summary>
    /// Allow delete if the configuration contains a function to delete a record.
    /// </summary>
    public bool AllowDelete
    {
        get
        {
            return _configuration.DeleteRecord is not null;
        }
    }

    #endregion

    #region Private Fields
    private readonly EZRecordsetConfiguration<TModel> _configuration;
    private readonly EZRecordset<TModel> _parent;
    #endregion

    #region Events
    public event EventHandler<EZStateHasChangedEventArgs>? StateHasChanged;
    public event EventHandler<string>? OnCRUDError;
    public event EventHandler<TModel>? OnAfterRefresh;
    public event EventHandler<TModel>? OnAfterUndo;
    public event EventHandler<TModel>? OnAfterUpdate;
    public event EventHandler<TModel>? OnAfterDelete;
    public event EventHandler<BeforeCRUDEventArgs<TModel>>? OnBeforeRefresh;
    public event EventHandler<BeforeCRUDEventArgs<TModel>>? OnBeforeUndo;
    public event EventHandler<BeforeCRUDEventArgs<TModel>>? OnBeforeUpdate;
    public event EventHandler<BeforeCRUDEventArgs<TModel>>? OnBeforeDelete;
    public event EventHandler<bool>? OnFocus;
    #endregion

    #region Class Constructors

    /// <summary>
    /// Initialize the record with raw data only. No CRUD operations are available
    /// </summary>
    /// <param name="parent">The recordset that holds the list of records</param>
    /// <param name="model">Data of TModel that contain the raw data of the record</param>
    internal EZRecord(EZRecordset<TModel> parent, TModel model)
    {
        _parent = parent;
        _configuration = new();
        Model = model;
        ValidationErrors = new();
    }

    /// <summary>
    /// Initializes the record and configures it with the configuration object
    /// </summary>
    /// <param name="parent">The recordset that holds the list of records</param>
    /// <param name="model">The model that contains the data</param>
    /// <param name="configuration">The object containing configuration for the CRUD operations</param>
    /// <param name="onStateHasChanged">Listener to changes</param>
    /// <param name="isNewRecord">If the record is a new record insert True</param>
    internal EZRecord(EZRecordset<TModel> parent, 
                      TModel model, 
                      EZRecordsetConfiguration<TModel> configuration,
                      Action<EZStateHasChangedEventArgs>? onStateHasChanged,
                      bool isNewRecord = false)
    {
        _parent = parent;
        _configuration = configuration;
        Model = model;
        ValidationErrors = new();
        if (onStateHasChanged != null)
        {
            StateHasChanged = (s, e) => onStateHasChanged(e);
        }
        IsNewRecord = isNewRecord;
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Save changes to the Record using [C]R[U]D functions set by the constructor if any. This operation is executed async on a different thread. 
    /// </summary>
    public void SaveChanges()
    {
        Task.Run(SaveChangesAsync);
    }

    public async Task SaveChangesAsync()
    {
        if (IsBusy || !AllowUpdate || !AllowCreate) { return; }
        try
        {
            if (_configuration.UpdateRecord != null && _configuration.CreateRecord != null && IsChanged)
            {
                HasFailedOperation = false;
                IsBusy = true;
                ValidationErrors = new();
                StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
                BeforeCRUDEventArgs<TModel> args = new(Model);
                OnBeforeUpdate?.Invoke(this, args);
                if (IsNewRecord)
                {
//                    BeforeCreate?.Invoke(this, args);
                    if (!args.Cancel)
                    {
                        var result = await _configuration.CreateRecord(Model);
                        if (result.IsSuccess && result.Content != null)
                        {
                            Model = result.Content;
                        }
                        else
                        {
                            HasFailedOperation = true;
                            ErrorMessage = result.ErrorMessage;
                            if (result.ValidationErrors != null)
                            {
                                ValidationErrors = result.ValidationErrors;
                            }
                        }
                    }
                }
                else
                {
                    if (!args.Cancel)
                    {
                        var result = await _configuration.UpdateRecord(Model);
                        if (result.IsSuccess && result.Content != null)
                        {
                            Model = result.Content;
                        }
                        else
                        {
                            HasFailedOperation = true;
                            ErrorMessage = result.ErrorMessage;
                            if (result.ValidationErrors != null)
                            {
                                ValidationErrors = result.ValidationErrors;
                            }
                        }
                    }
                }
                IsBusy = false;
                if (!HasFailedOperation)
                {
                    IsChanged = false;
                    IsNewRecord = false;
                    IsSaved = true;
                    OnAfterUpdate?.Invoke(this, Model);
                }
                StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
            }
        }
        catch (Exception ex)
        {
            IsBusy = false;
            HasFailedOperation = true;
            ErrorMessage = ex.Message;
            StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
        }
        if (HasFailedOperation)
        {
            OnCRUDError?.Invoke(this, $"Save Changes was not successful: {ErrorMessage}");
        }
    }

    /// <summary>
    /// Undo changes to the Record using C[R]UD functions set by the constructor if any. This operation is executed async on a different thread. 
    /// </summary>
    public void UndoChanges()
    {
        Task.Run(UndoChangesAsync);
    }

    public async Task UndoChangesAsync()
    {
        if (IsBusy || !AllowRead) { return; }
        try
        {
            if (IsNewRecord)
            {
                await DeleteAsync();
                return;
            }
            if (_configuration.ReadRecord != null && IsChanged)
            {
                HasFailedOperation = false;
                IsBusy = true;
                StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
                BeforeCRUDEventArgs<TModel> args = new(Model);
                OnBeforeUndo?.Invoke(this, args);
                if (!args.Cancel)
                {
                    var result = await _configuration.ReadRecord(Model);
                    if (result.IsSuccess && result.Content != null)
                    {
                        Model = result.Content;
                    }
                    else
                    {
                        HasFailedOperation = true;
                        ErrorMessage = result.ErrorMessage;
                    }
                }
                IsBusy = false;
                if (!HasFailedOperation)
                {
                    IsChanged = false;
                    OnAfterUndo?.Invoke(this, Model);
                }
                StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
            }
        }
        catch (Exception ex)
        {
            IsBusy = false;
            HasFailedOperation = true;
            ErrorMessage = ex.Message;
            StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
        }
        if (HasFailedOperation)
        {
            OnCRUDError?.Invoke(this, $"Undo was not successful: {ErrorMessage}");
        }
    }

    /// <summary>
    /// Refresh the Record using C[R]UD functions set by the constructor if any. This operation is executed async on a different thread. 
    /// </summary>
    public void Refresh()
    {
        Task.Run(RefreshAsync);
    }

    public async Task RefreshAsync()
    {
        if (!IsBusy)
        {
            try
            {
                if (IsNewRecord) { return; }
                if (IsChanged) { 
                    // If refresh is called when a record is changed, execute the undo function instead
                    await UndoChangesAsync();
                    return; 
                }
                HasFailedOperation = false;
                IsBusy = true;
                StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
                if (_configuration.ReadRecord != null)
                {
                    BeforeCRUDEventArgs<TModel> args = new(Model);
                    OnBeforeRefresh?.Invoke(this, args);
                    if (!args.Cancel)
                    {
                        var result = await _configuration.ReadRecord(Model);
                        if (result.IsSuccess && result.Content != null)
                        {
                            Model = result.Content;
                        }
                        else
                        {
                            HasFailedOperation = true;
                            ErrorMessage = result.ErrorMessage;
                        }
                    }
                }
                IsBusy = false;
                if (!HasFailedOperation)
                {
                    OnAfterRefresh?.Invoke(this, Model);
                    IsSaved = false;
                }
                StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
            }
            catch (Exception ex)
            {
                IsBusy = false;
                HasFailedOperation = true;
                ErrorMessage = ex.Message;
                StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
            }
            if (HasFailedOperation)
            {
                OnCRUDError?.Invoke(this, $"Refresh was not successful: {ErrorMessage}");
            }
        }
    }

    /// <summary>
    /// Delete the Record using CRUD functions set by the constructor if any. This operation is executed async on a different thread. 
    /// </summary>
    public void Delete()
    {
        Task.Run(DeleteAsync);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task DeleteAsync()
    {
        if (IsBusy || !AllowDelete || IsDeleted) { return; }
        try
        {
            HasFailedOperation = false;
            IsBusy = true;
            StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
            if (_configuration.DeleteRecord != null)// && !IsNewRecord)
            {
                BeforeCRUDEventArgs<TModel> args = new(Model);
                OnBeforeDelete?.Invoke(this, args);
                if (!args.Cancel)
                {
                    if (IsNewRecord)
                    {
                        IsDeleted = true;
                    }
                    else
                    {
                        var result = await _configuration.DeleteRecord(Model);
                        if (result.IsSuccess)
                        {
                            IsDeleted = true;
                        }
                        else
                        {
                            HasFailedOperation = true;
                            ErrorMessage = result.ErrorMessage;
                        }
                    }
                }
            }
            IsBusy = false;
            if (!HasFailedOperation)
            {
                OnAfterDelete?.Invoke(this, Model);
            }
            StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
        }
        catch (Exception ex)
        {
            IsBusy = false;
            HasFailedOperation = true;
            ErrorMessage = ex.Message;
            StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
        }
        if (HasFailedOperation)
        {
            OnCRUDError?.Invoke(this, $"Delete was not successful: {ErrorMessage}");
        }
    }

    /// <summary>
    /// Set the property DeleteRequested to true. To delete the record the DeleteAsync() 
    /// need to be called. This intermediate state can be used while a confirmation of
    /// the user is pending.
    /// </summary>
    public void RequestDelete()
    {
        DeleteRequested = true;
        StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
    }

    public void CancelDelete()
    {
        DeleteRequested = false;
        StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
    }


    #endregion

    #region Other Operations

    public void SetFocus(bool byProgram)
    {
        OnFocus?.Invoke(this, byProgram);
        StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { SetFocus = true, SaveRecords = true });
    }

    #endregion

    #region Listeners

    /// <summary>
    /// OnFieldChanged is a listener that should be set as listener to any changes to fields of the form
    /// </summary>
    /// <param name="sender">The object that triggers the event</param>
    /// <param name="eventArgs">The arguments send with the event by the EditContext object</param>
    public void OnFieldChanged(string fieldName)
    {
        if (!IsChanged)
        {
            if (IsNewRecord)
            {
                // For a new record this event need to be triggered twice: 1 time to create a new record,
                // 2 times to be added to changed records
                StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this));
            }
            IsChanged = true;
            IsSaved = false;
            StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { SaveRecords = true });
        }
        // If any changes are made on a field, all custom validation messagesd of this field need to be cleared
        if (ValidationErrors.Any())
        {
            if (ValidationErrors.ContainsKey(fieldName))
            {
                ValidationErrors.Remove(fieldName);
                StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { SaveRecords = true });
            }
        }

    }

    public void OnValidationStateChanged(IEnumerable<string>? validationMessages)
    {
        const string key = "EditFormMessages";
        bool stateChanged = false;
        if (ValidationErrors.ContainsKey(key))
        {
            ValidationErrors.Remove(key);
            stateChanged = true;
        }
        if (validationMessages != null)
        {
            if (validationMessages.Any())
            {
                ValidationErrors.Add(key, validationMessages.ToList());
                stateChanged = true;
            }
        }
        if (stateChanged)
        {
            StateHasChanged?.Invoke(this, new EZStateHasChangedEventArgs(this) { NoFocus = true });
        }
    }

    #endregion

    #region Garbage Collection
    public void Dispose()
    {
        throw new NotImplementedException();
    }
    #endregion

}
