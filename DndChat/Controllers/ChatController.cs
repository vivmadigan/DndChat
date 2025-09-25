using DndChat.ViewModels;
using Microsoft.AspNetCore.Mvc;


namespace DndChat.Controllers
{
    public class ChatController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(new JoinViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(JoinViewModel model)
        {             
            if (!ModelState.IsValid)
                return View("Index", model);

            model.Joined = true;
            return View("Index", model);
        }
    }
}
