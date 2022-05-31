using System.Net.Http.Json;

namespace EZAccess.Data;

public abstract class EZHttpService
{
    readonly protected HttpClient httpClient;

    public EZHttpService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    async protected Task<EZActionResult<T>> HttpGet<T>(string requestUri)
    {
        EZActionResult<T> result = new();
        try
        {
            var response = await httpClient.GetAsync(requestUri);
            result.StatusCode = response.StatusCode;
            result.IsSuccess = response.IsSuccessStatusCode;
            if (response.IsSuccessStatusCode)
            {
                result.Content = await response.Content.ReadFromJsonAsync<T>();
                if (result.Content != null)
                {
                    result.Id = GetId(result.Content);
                }
            }
            else
            {
                result.ErrorMessage = $"An error occured while requesting data from the server: {response.ReasonPhrase}";
                if (result.Content != null)
                {
                    result.ValidationErrors = await response.Content.ReadFromJsonAsync<Dictionary<string, List<string>>>();
                }
            }
        }
        catch (Exception exception)
        {
            result.ErrorMessage = $"Unhandled exception: {exception.Message}";
        }
        return result;
    }

    async protected Task<EZActionResult<T>> HttpPost<T>(string requestUri, T newT)
    {
        EZActionResult<T> result = new();
        try
        {
            var response = await httpClient.PostAsJsonAsync(requestUri, newT);
            result.StatusCode = response.StatusCode;
            result.IsSuccess = response.IsSuccessStatusCode;
            if (response.IsSuccessStatusCode)
            {
                result.Content = await response.Content.ReadFromJsonAsync<T>();
                if (result.Content != null)
                {
                    result.Id = GetId(result.Content);
                }
            }
            else
            {
                result.ErrorMessage = $"An error occured while posting data to the server: {response.ReasonPhrase}";
                result.ValidationErrors = await response.Content.ReadFromJsonAsync<Dictionary<string, List<string>>>();
            }
        }
        catch (Exception exception)
        {
            result.ErrorMessage = $"Unhandled exception: {exception.Message}";
        }
        return result;
    }

    async protected Task<EZActionResult<T>> HttpPut<T>(string requestUri, T updatedT)
    {
        EZActionResult<T> result = new();
        try
        {
            var response = await httpClient.PutAsJsonAsync(requestUri, updatedT);
            result.StatusCode = response.StatusCode;
            result.IsSuccess = response.IsSuccessStatusCode;
            if (response.IsSuccessStatusCode)
            {
                result.Content = await response.Content.ReadFromJsonAsync<T>();
                if (result.Content != null)
                {
                    result.Id = GetId(result.Content);
                }
            }
            else
            {
                result.ErrorMessage = $"An error occured while writing data to the server: {response.ReasonPhrase}";
                result.ValidationErrors = await response.Content.ReadFromJsonAsync<Dictionary<string, List<string>>>();
            }
        }
        catch (Exception exception)
        {
            result.ErrorMessage = $"Unhandled exception: {exception.Message}";
        }
        return result;
    }

    async protected Task<EZActionResult<bool>> HttpDelete(string requestUri)
    {
        EZActionResult<bool> result = new();
        try
        {
            var response = await httpClient.DeleteAsync(requestUri);
            result.StatusCode = response.StatusCode;
            result.IsSuccess = response.IsSuccessStatusCode;
            if (response.IsSuccessStatusCode)
            {
                result.Content = true;
            }
            else
            {
                result.ErrorMessage = $"An error occured while deleting data at the server: {response.ReasonPhrase}";
                result.ValidationErrors = await response.Content.ReadFromJsonAsync<Dictionary<string, List<string>>>();
            }
        }
        catch (Exception exception)
        {
            result.ErrorMessage = $"Unhandled exception: {exception.Message}";
        }
        return result;
    }

    protected abstract int? GetId(object Model);

}
