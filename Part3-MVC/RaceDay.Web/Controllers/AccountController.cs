using Microsoft.AspNetCore.Mvc;using RaceDay.Web.Models;using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class AccountController(RaceDayClient api):BaseController {
 [HttpGet]public IActionResult Login()=>View(new LoginForm());
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Login(LoginForm form){if(!ModelState.IsValid)return View(form);var (data,error)=await api.Post<LoginInfo>(HttpContext,"api/auth/login",form);if(error!=null||data==null){ModelState.AddModelError("",error??"Login failed");return View(form);}HttpContext.Session.SetString("Role",data.User.Role);HttpContext.Session.SetString("Name",data.User.FullName);return RedirectToAction("Index",data.User.Role=="Organiser"?"Organiser":"Home");}
 [HttpGet]public IActionResult Register()=>View(new RegisterForm());
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Register(RegisterForm form){if(!ModelState.IsValid)return View(form);var (user,error)=await api.Post<UserInfo>(HttpContext,"api/auth/register",new {form.FullName,form.Email,form.Password,form.Role,form.OrganisationName,form.ContactPhone,DateOfBirth=string.IsNullOrWhiteSpace(form.DateOfBirth)?null:form.DateOfBirth,form.EmergencyContact,form.OrganiserCode});if(error!=null){ModelState.AddModelError("",error);return View(form);}TempData["Success"]="Account created. Please log in.";return RedirectToAction("Login");}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Logout(){await api.Post<object>(HttpContext,"api/auth/logout");HttpContext.Session.Clear();return RedirectToAction("Index","Home");}
}
