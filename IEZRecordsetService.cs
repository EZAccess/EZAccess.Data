namespace EZAccess.Data;

public interface IEZRecordsetService<TModel>
{
    Task<EZActionResult<TModel?>> CreateAsync(TModel createdRecord);
    Task<EZActionResult<bool>> DeleteAsync(TModel deleteRecord);
    Task<EZActionResult<List<TModel>?>> GetAllAsync();
    Task<EZActionResult<TModel?>> GetAsync(TModel readRecord);
    Task<EZActionResult<List<TModel>?>> GetAllWhereAsync(string? where);
    Task<EZActionResult<TModel?>> UpdateAsync(TModel updatedRecord);
}
