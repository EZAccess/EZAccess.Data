using Microsoft.AspNetCore.Components.Forms;

namespace EZAccess.Data;

public class EZRecord<TModel> : IDisposable where TModel : new()
{
    #region Public Properties
    public TModel Model { get; private set; }
    public List<EZField> Fields { get; }
    public bool IsReadOnly { get; private set; }
    public bool IsChanged { get; private set; }
    public bool IsDeleted { get; private set; }
    public bool IsNewRecord { get; private set; }
    public bool IsBusy { get; private set; }
    public bool HasFailedOperation { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Dictionary<string, List<string>>? ValidationErrors { get; private set; }
    #endregion

    #region Private Fields
    private readonly Func<TModel, Task<EZActionResult<TModel?>>>? _createRecord;
    private readonly Func<TModel, Task<EZActionResult<TModel?>>>? _readRecord;
    private readonly Func<TModel, Task<EZActionResult<TModel?>>>? _updateRecord;
    private readonly Func<TModel, Task<EZActionResult<bool>>>? _deleteRecord;
    #endregion

    #region Events
    public event EventHandler<EZRecordsetStateHasChangedEventArgs>? StateHasChanged;
    public event EventHandler<string>? OnCRUDError;
    public event EventHandler<TModel>? OnAfterRefresh;
    public event EventHandler<TModel>? OnAfterUndo;
    public event EventHandler<TModel>? OnAfterUpdate;
    public event EventHandler<TModel>? OnAfterDelete;
    public event EventHandler<BeforeCRUDEventArgs<TModel>>? OnBeforeRefresh;
    public event EventHandler<BeforeCRUDEventArgs<TModel>>? OnBeforeUndo;
    public event EventHandler<BeforeCRUDEventArgs<TModel>>? OnBeforeUpdate;
    public event EventHandler<BeforeCRUDEventArgs<TModel>>? OnBeforeDelete;
    #endregion

    #region Class Constructors

    /// <summary>
    /// Initialize the record with raw data only. No CRUD operations are available
    /// </summary>
    /// <param name="data">Data of TModel that contain the raw data of the record</param>
    internal EZRecord(TModel data)
    {
        Model = data;
        IsReadOnly = true;
        Fields = new List<EZField>();
        Type type = typeof(TModel);

        foreach (var item in type.GetProperties())
        {
            Fields.Add(new EZField(type, item));
        }
    }

    /// <summary>
    /// Initialize the record with raw data and a deligate to refresh the record. No CRUD operations are available
    /// </summary>
    /// <param name="data">Data of TModel that contain the raw data of the record</param>
    /// <param name="readRecord">Delegate to execute when e refresh is required</param>
    /// <param name="onParametersChanged">Action that is executed when any property of the record is changed</param>
    internal EZRecord(TModel data, 
                    Func<TModel, Task<EZActionResult<TModel?>>> readRecord,
                    Action<EZRecordsetStateHasChangedEventArgs>? onStateHasChanged)
    {
        Model = data;
        IsReadOnly = true;
        Fields = new List<EZField>();
        Type type = typeof(TModel);

        foreach (var item in type.GetProperties())
        {
            Fields.Add(new EZField(type, item));
        }
        _readRecord = readRecord;
//        _onStateHasChanged = onStateHasChanged;
        if (onStateHasChanged != null)
        {
            StateHasChanged = (s, e) => onStateHasChanged(e);
        }
    }

    /// <summary>
    /// Initialize the record with raw data and a deligates to do CRUD operations
    /// </summary>
    /// <param name="data">Data of TModel that contain the raw data of the record</param>
    /// <param name="createRecord">Delegate to execute when a new record is saved</param>
    /// <param name="readRecord">Delegate to execute when a refresh is required</param>
    /// <param name="updateRecord">Delegate to execute when an existing record is saved</param>
    /// <param name="deleteRecord">Delegate to execute when a record is deleted</param>
    /// <param name="onStateHasChanged">Action that is executed when the Data of the record is changed</param>
    /// <param name="isNewRecord">Record will be treated as new record</param>
    internal EZRecord(TModel data, 
                    Func<TModel, Task<EZActionResult<TModel?>>> createRecord, 
                    Func<TModel, Task<EZActionResult<TModel?>>> readRecord, 
                    Func<TModel, Task<EZActionResult<TModel?>>> updateRecord, 
                    Func<TModel, Task<EZActionResult<bool>>> deleteRecord,
                    Action<EZRecordsetStateHasChangedEventArgs>? onStateHasChanged,
                    bool isNewRecord = false)
    {
        Model = data;
        IsReadOnly = false;
        Fields = new List<EZField>();
        Type type = typeof(TModel);

        foreach (var item in type.GetProperties())
        {
            Fields.Add(new EZField(type, item));
        }
        _createRecord = createRecord;
        _readRecord = readRecord;
        _updateRecord = updateRecord;
        _deleteRecord = deleteRecord;
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
        //SaveChangesAsync(displayErrors).Start();
    }

    public async Task SaveChangesAsync()
    {
        if (IsBusy || IsReadOnly) { return; }
        try
        {
            if (_updateRecord != null && _createRecord != null && IsChanged)
            {
                HasFailedOperation = false;
                IsBusy = true;
                ValidationErrors = null;
                StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
                BeforeCRUDEventArgs<TModel> args = new(Model);
                OnBeforeUpdate?.Invoke(this, args);
                if (IsNewRecord)
                {
//                    BeforeCreate?.Invoke(this, args);
                    if (!args.Cancel)
                    {
                        var result = await _createRecord(Model);
                        if (result.IsSuccess && result.Content != null)
                        {
                            Model = result.Content;
                        }
                        else
                        {
                            HasFailedOperation = true;
                            ErrorMessage = result.ErrorMessage;
                            ValidationErrors = result.ValidationErrors;
                        }
                    }
                }
                else
                {
                    if (!args.Cancel)
                    {
                        var result = await _updateRecord(Model);
                        if (result.IsSuccess && result.Content != null)
                        {
                            Model = result.Content;
                        }
                        else
                        {
                            HasFailedOperation = true;
                            ErrorMessage = result.ErrorMessage;
                            ValidationErrors = result.ValidationErrors;
                        }
                    }
                }
                IsBusy = false;
                if (!HasFailedOperation)
                {
                    IsChanged = false;
                    IsNewRecord = false;
                    OnAfterUpdate?.Invoke(this, Model);
                }
                StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
            }
        }
        catch (Exception ex)
        {
            IsBusy = false;
            HasFailedOperation = true;
            ErrorMessage = ex.Message;
            StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
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
        if (IsBusy || IsReadOnly) { return; }
        try
        {
            if (IsNewRecord)
            {
                await DeleteAsync();
                return;
            }
            if (_readRecord != null && IsChanged)
            {
                HasFailedOperation = false;
                IsBusy = true;
                StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
                BeforeCRUDEventArgs<TModel> args = new(Model);
                OnBeforeUndo?.Invoke(this, args);
                if (!args.Cancel)
                {
                    var result = await _readRecord(Model);
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
                StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
            }
        }
        catch (Exception ex)
        {
            IsBusy = false;
            HasFailedOperation = true;
            ErrorMessage = ex.Message;
            StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
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
                StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
                if (_readRecord != null)
                {
                    BeforeCRUDEventArgs<TModel> args = new(Model);
                    OnBeforeRefresh?.Invoke(this, args);
                    if (!args.Cancel)
                    {
                        var result = await _readRecord(Model);
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
                }
                StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
            }
            catch (Exception ex)
            {
                IsBusy = false;
                HasFailedOperation = true;
                ErrorMessage = ex.Message;
                StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
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

    public async Task DeleteAsync()
    {
        if (IsBusy || IsReadOnly || IsDeleted) { return; }
        try
        {
            HasFailedOperation = false;
            IsBusy = true;
            StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
            if (_deleteRecord != null)// && !IsNewRecord)
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
                        var result = await _deleteRecord(Model);
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
            StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
        }
        catch (Exception ex)
        {
            IsBusy = false;
            HasFailedOperation = true;
            ErrorMessage = ex.Message;
            StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
        }
        if (HasFailedOperation)
        {
            OnCRUDError?.Invoke(this, $"Delete was not successful: {ErrorMessage}");
        }
    }

    #endregion

    #region Listeners

    /// <summary>
    /// OnFieldChanged is a listener that should be set as listener to any changes to fields of the form
    /// </summary>
    /// <param name="sender">The object that triggers the event</param>
    /// <param name="eventArgs">The arguments send with the event by the EditContext object</param>
    public void OnFieldChanged(object? sender, FieldChangedEventArgs eventArgs)
    {
//        if (IsNewRecord && !IsChanged)
        if (!IsChanged)
        {
            if (IsNewRecord)
            {
                // For a new record this event need to be triggered twice: 1 time to create a new record,
                // 2 times to be added to changed records
                StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this));
            }
            IsChanged = true;
            StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this, true));
        }
        if (ValidationErrors != null)
        {
            if (ValidationErrors.ContainsKey(eventArgs.FieldIdentifier.FieldName))
            {
                ValidationErrors.Remove(eventArgs.FieldIdentifier.FieldName);
                if (!ValidationErrors.Any())
                {
                    ValidationErrors = null;
                }
                StateHasChanged?.Invoke(this, new EZRecordsetStateHasChangedEventArgs((object)this, true));
            }
        }
    }

    #endregion

    #region Garbage Collection
    public void Dispose()
    {
        var x = 1;
    }
    #endregion

}
