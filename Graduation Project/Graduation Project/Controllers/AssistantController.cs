using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    public class AssistantController : Controller
    {
        private readonly IAssistant _assistantRepository;

        public AssistantController(IAssistant assistantRepository)
        {
            _assistantRepository = assistantRepository;
        }

        public IActionResult Index()
        {
            throw new NotImplementedException();
        }

        public IActionResult Details(int id)
        {
            throw new NotImplementedException();
        }

        public IActionResult Create()
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Assistant assistant)
        {
            throw new NotImplementedException();
        }

        public IActionResult Edit(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Assistant assistant)
        {
            throw new NotImplementedException();
        }

        public IActionResult Delete(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            throw new NotImplementedException();
        }
    }
}
