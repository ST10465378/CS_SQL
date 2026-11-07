using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.Filters;
namespace RaceDay.Web.Controllers;
public abstract class BaseController : Controller {
 protected string? Role=>HttpContext.Session.GetString("Role");
 protected bool AuthorizeRole(string role){if(Role==null){TempData["Error"]="Please sign in to continue.";return false;}if(Role!=role){TempData["Error"]="You cannot access that page.";return false;}return true;}
 protected IActionResult Denied()=>RedirectToAction("Login","Account");
 protected IActionResult Feedback(string? error,string redirectController,string redirectAction,int? id=null){TempData[error==null?"Success":"Error"]=error??"Saved successfully.";return RedirectToAction(redirectAction,redirectController,id==null?null:new{id});}
}
