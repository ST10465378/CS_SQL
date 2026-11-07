using Microsoft.AspNetCore.Mvc;using RaceDay.Web.Models;using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class OrganiserController(RaceDayClient api):BaseController {
 public async Task<IActionResult> Index(){if(!AuthorizeRole("Organiser"))return Denied();var (events,error)=await api.Get<List<EventInfo>>(HttpContext,"api/events/mine");ViewBag.Error=error;
  var summaries=new Dictionary<int,int>();
  foreach(var ev in events??new List<EventInfo>()){
   var (categories,catError)=await api.Get<List<CategoryInfo>>(HttpContext,$"api/events/{ev.EventId}/categories");
   if(catError==null)summaries[ev.EventId]=(categories??new()).Sum(c=>c.EnrolledCount);
  }
  ViewBag.EnrolmentCounts=summaries;
  return View(events??new());}
 [HttpGet]public IActionResult Create(){if(!AuthorizeRole("Organiser"))return Denied();return View("Edit",new EventForm{EventDate=DateTime.Today.AddDays(30).ToString("yyyy-MM-dd")});}
 [HttpGet]public async Task<IActionResult> Edit(int id){if(!AuthorizeRole("Organiser"))return Denied();var (ev,error)=await api.Get<EventInfo>(HttpContext,$"api/events/{id}");if(ev==null)return Feedback(error??"Event not found","Organiser","Index");ViewBag.EventId=id;return View(new EventForm{Name=ev.Name,EventDate=ev.EventDate,Location=ev.Location,Description=ev.Description,DistanceKm=ev.DistanceKm,EventType=ev.EventType,Status=ev.Status});}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Save(int? id,EventForm form){if(!AuthorizeRole("Organiser"))return Denied();if(!ModelState.IsValid){ViewBag.EventId=id;return View("Edit",form);}var body=new {form.Name,form.EventDate,form.Location,form.Description,form.DistanceKm,form.EventType,form.Status};var (ev,error)=id==null?await api.Post<EventInfo>(HttpContext,"api/events",body):await api.Put<EventInfo>(HttpContext,$"api/events/{id}",body);if(error!=null||ev==null){ModelState.AddModelError("",error??"Could not save event");ViewBag.EventId=id;return View("Edit",form);}if(form.Banner!=null){var (_,uploadError)=await api.Upload<object>(HttpContext,$"api/events/{ev.EventId}/banner",form.Banner);if(uploadError!=null)TempData["Error"]="Event saved, but image upload failed: "+uploadError;}return Feedback(null,"Organiser","Details",ev.EventId);}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Delete(int id){if(!AuthorizeRole("Organiser"))return Denied();var (_,error)=await api.Delete<object>(HttpContext,$"api/events/{id}");return Feedback(error,"Organiser","Index");}
 public async Task<IActionResult> Details(int id){if(!AuthorizeRole("Organiser"))return Denied();var (ev,error)=await api.Get<EventInfo>(HttpContext,$"api/events/{id}");if(ev==null)return Feedback(error??"Event not found","Organiser","Index");var (cats,_)=await api.Get<List<CategoryInfo>>(HttpContext,$"api/events/{id}/categories");if(cats!=null)ev.Categories=cats;return View(ev);}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Category(int eventId,CategoryForm form){if(!AuthorizeRole("Organiser"))return Denied();if(!ModelState.IsValid)return Feedback("Invalid category values","Organiser","Details",eventId);var (_,error)=await api.Post<CategoryInfo>(HttpContext,$"api/events/{eventId}/categories",form);return Feedback(error,"Organiser","Details",eventId);}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> UpdateCategory(int eventId,int id,CategoryForm form){
  if(!AuthorizeRole("Organiser"))return Denied();
  if(!ModelState.IsValid)return Feedback("Invalid category values", "Organiser", "Details", eventId);
  var (_,error)=await api.Put<CategoryInfo>(HttpContext,$"api/categories/{id}",form);
  return Feedback(error,"Organiser","Details",eventId);
 }
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> DeleteCategory(int eventId,int id){if(!AuthorizeRole("Organiser"))return Denied();var (_,error)=await api.Delete<object>(HttpContext,$"api/categories/{id}");return Feedback(error,"Organiser","Details",eventId);}
 public async Task<IActionResult> Enrolments(int eventId,int categoryId){if(!AuthorizeRole("Organiser"))return Denied();var (entries,error)=await api.Get<List<EnrolmentInfo>>(HttpContext,$"api/categories/{categoryId}/enrolments");ViewBag.EventId=eventId;ViewBag.CategoryId=categoryId;ViewBag.Error=error;
 var (results,resultsError)=await api.Get<List<ResultInfo>>(HttpContext,$"api/categories/{categoryId}/results");
 ViewBag.Results=(results??new List<ResultInfo>()).ToDictionary(r=>r.EnrolmentId);
 if(resultsError!=null)ViewBag.ResultsError=resultsError;
 return View(entries??new());}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Result(int eventId,int categoryId,int enrolmentId,string finishTime,int? position){if(!AuthorizeRole("Organiser"))return Denied();var (_,error)=await api.Post<ResultInfo>(HttpContext,$"api/enrolments/{enrolmentId}/results",new{finishTime,position});return RedirectWithFeedback(error,eventId,categoryId);}
 IActionResult RedirectWithFeedback(string? error,int eventId,int categoryId){TempData[error==null?"Success":"Error"]=error??"Result recorded.";return RedirectToAction("Enrolments",new{eventId,categoryId});}
}
