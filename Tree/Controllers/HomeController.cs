using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO.Compression;
using Tree.Models;

namespace Tree.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _appEnvironment;
        private const string _path = @"C:\Users\sereg\source\repos\Tree\Tree\wwwroot\Files\";

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment appEnvironment)
        {
            _logger = logger;
            _appEnvironment = appEnvironment;
        }

        public IActionResult Index()
        {
            _logger.LogInformation(GetSessionId());
            return View();
        }

        public IActionResult Tree(string path)
        {
            _logger.LogInformation(path);

            if (!Directory.Exists(Path.Combine(_path, GetSessionId())))
            {
                return BadRequest();
            }

            var list = new List<Models.File>();

            path = Path.Combine(_path, path.Replace('/', '\\'));

            foreach (var item in Directory.GetDirectories(path))
            {
                list.Add(new Models.File { Name = item.Split("\\").Last(), IsFolder = true });
            }

            foreach (var item in Directory.GetFiles(path))
            {
                list.Add(new Models.File { Name = item.Split("\\").Last(), IsFolder = false });
            }

            return View(new FilesInfo { CurrentPath = path.Replace(_path, "").Replace('\\', '/'), Files = list });
        }


        [HttpPost]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> Upload(Test test)
        {
            if (test.file != null)
            {
                using (var fileStream = new FileStream(Path.Combine(_path, test.file.FileName), FileMode.Create))
                {
                    await test.file.CopyToAsync(fileStream);
                }

                var _userPath = Path.Combine(_path, GetSessionId());

                if (!Directory.Exists(_userPath))
                    Directory.CreateDirectory(_userPath);

                ZipFile.ExtractToDirectory(Path.Combine(_path, test.file.FileName), _userPath);

                return RedirectToAction(nameof(Tree), new { path = GetSessionId() });
            }

            return BadRequest();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetSessionId()
        {
            return HttpContext.Session.GetString("Id");
        }
    }
}