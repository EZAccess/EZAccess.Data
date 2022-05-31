using System.Net;

namespace EZAccess.Data;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TModel"></typeparam>
public class EZActionResult<TModel>
{
    public TModel? Content { get; set; }
    public int? Id { get; set; }
    public Dictionary<string, List<string>>? ValidationErrors { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
//    public bool? DeleteSuccessful { get; set; }
//    public bool? UpdateSuccessful { get; set; }
}