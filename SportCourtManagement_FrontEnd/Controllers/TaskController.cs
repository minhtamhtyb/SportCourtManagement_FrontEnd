using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SportCourtManagement_FrontEnd.Controllers
{
    [Route("manager/tasks")]
    public class TaskController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiBase = "https://localhost:7075/api/manager/complexes/1";
        private readonly JsonSerializerOptions _jsonOpts;

        public TaskController()
        {
            _client = new HttpClient();
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // ── GET: Support multiple routes for ease of use
        [HttpGet]
        public async Task<IActionResult> Tasks()
        {
            return View();
        }

    }
}
