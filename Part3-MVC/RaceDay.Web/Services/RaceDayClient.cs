using System.Net; using System.Net.Http.Json; using System.Text.Json; using Microsoft.AspNetCore.Http;
namespace RaceDay.Web.Services;
public sealed class RaceDayClient(HttpClient http) {
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    public async Task<(T? Data,string? Error)> Send<T>(HttpContext ctx, HttpMethod method,string path,object? body=null, IFormFile? file=null) {
        using var req = new HttpRequestMessage(method,path.TrimStart('/'));
        var cookie = ctx.Session.GetString("ApiCookie"); if(!string.IsNullOrEmpty(cookie))req.Headers.TryAddWithoutValidation("Cookie",cookie);
        if(file!=null){var form=new MultipartFormDataContent();var stream=new StreamContent(file.OpenReadStream());stream.Headers.ContentType=new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);form.Add(stream,"file",Path.GetFileName(file.FileName));req.Content=form;}
        else if(body!=null)req.Content=JsonContent.Create(body, options:Json);
        try { using var res=await http.SendAsync(req);if(path=="api/auth/login" && res.IsSuccessStatusCode) {var c=res.Headers.TryGetValues("Set-Cookie",out var v)?v.FirstOrDefault():null; if(c!=null)ctx.Session.SetString("ApiCookie",c.Split(';')[0]);}
            if(!res.IsSuccessStatusCode) {var raw=await res.Content.ReadAsStringAsync();try{using var d=JsonDocument.Parse(raw);return(default,d.RootElement.TryGetProperty("detail",out var s)?s.GetString():d.RootElement.TryGetProperty("title",out s)?s.GetString():raw);}catch{return(default,$"Request failed (HTTP {(int)res.StatusCode}). Please check your details and try again.");}}
            if(res.StatusCode==HttpStatusCode.NoContent)return(default,null);
            return(await res.Content.ReadFromJsonAsync<T>(Json),null);
        } catch(HttpRequestException) {return(default,"The RaceDay API is unavailable. Start the API and try again.");}
        catch(TaskCanceledException) {return(default,"The RaceDay API timed out. Please try again.");}
    }
    public Task<(T? Data,string? Error)> Get<T>(HttpContext c,string path)=>Send<T>(c,HttpMethod.Get,path);
    public Task<(T? Data,string? Error)> Post<T>(HttpContext c,string path,object? value=null)=>Send<T>(c,HttpMethod.Post,path,value);
    public Task<(T? Data,string? Error)> Put<T>(HttpContext c,string path,object value)=>Send<T>(c,HttpMethod.Put,path,value);
    public Task<(T? Data,string? Error)> Delete<T>(HttpContext c,string path)=>Send<T>(c,HttpMethod.Delete,path);
    public Task<(T? Data,string? Error)> Upload<T>(HttpContext c,string path,IFormFile file)=>Send<T>(c,HttpMethod.Post,path,file:file);
}
