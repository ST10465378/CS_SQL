using Microsoft.AspNetCore.Mvc;using RaceDay.Web.Models;using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class HomeController(RaceDayClient api):BaseController {
 public async Task<IActionResult> Index(string? search,string? eventType){var query="api/events?from="+DateTime.Today.ToString("yyyy-MM-dd");if(!string.IsNullOrWhiteSpace(search))query+="&search="+Uri.EscapeDataString(search);if(!string.IsNullOrWhiteSpace(eventType))query+="&eventType="+Uri.EscapeDataString(eventType);var (events,error)=await api.Get<List<EventInfo>>(HttpContext,query);ViewBag.Error=error;ViewBag.Search=search;ViewBag.Type=eventType;return View(events??new());}
 public async Task<IActionResult> Event(int id){var (ev,error)=await api.Get<EventInfo>(HttpContext,$"api/events/{id}");if(ev==null){TempData["Error"]=error??"Event not found";return RedirectToAction("Index");}return View(ev);}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Enrol(int categoryId,int eventId){if(!AuthorizeRole("Participant"))return Denied();var (_,error)=await api.Post<EnrolmentInfo>(HttpContext,$"api/categories/{categoryId}/enrolments");return Feedback(error,"Home","Event",eventId);}
 public async Task<IActionResult> MyEvents(){if(!AuthorizeRole("Participant"))return Denied();var (data,error)=await api.Get<List<EnrolmentInfo>>(HttpContext,"api/users/me/enrolments");ViewBag.Error=error;return View(data??new());}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Withdraw(int id){if(!AuthorizeRole("Participant"))return Denied();var (_,error)=await api.Delete<object>(HttpContext,$"api/enrolments/{id}");return Feedback(error,"Home","MyEvents");}
 public async Task<IActionResult> MyResults(){if(!AuthorizeRole("Participant"))return Denied();var (ens,error)=await api.Get<List<EnrolmentInfo>>(HttpContext,"api/users/me/enrolments");var list=new List<(EnrolmentInfo,ResultInfo?)>();foreach(var en in ens??new()){var (result,_)=await api.Get<ResultInfo>(HttpContext,$"api/enrolments/{en.EnrolmentId}/results");list.Add((en,result));}ViewBag.Error=error;return View(list);}
 public IActionResult Error()=>View();
}
