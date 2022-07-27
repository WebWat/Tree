using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
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
            var sessionId = GetSessionId();

            if (!Directory.Exists(Path.Combine(_path, sessionId)))
            {
                return View(new Test { BeenBefore = false });
            }

            return View(new Test { BeenBefore = true, SessinId = sessionId });
        }

        public IActionResult Tree(string path)
        {
            if (!Directory.Exists(Path.Combine(_path, GetSessionId())))
            {
                return RedirectToAction(nameof(Index));
            }

            var value = HttpContext.Session.GetString(path);

            if (string.IsNullOrEmpty(value))
            {
                var list = new List<Models.File>();

                var newPath = Path.Combine(_path, path.Replace('/', '\\'));

                var di = new DirectoryInfo(newPath);

                foreach (var item in di.GetDirectories())
                {
                    list.Add(new Models.File { Name = item.Name, IsFolder = true });
                }

                foreach (var item in di.GetFiles())
                {
                    list.Add(new Models.File { Name = item.Name, Length = GetSize(item.Length), IsFolder = false });
                }

                var vm = new FilesInfo { CurrentPath = path, Files = list };

                HttpContext.Session.SetString(path, JsonSerializer.Serialize(vm));

                return View(vm);
            }

            return View(JsonSerializer.Deserialize<FilesInfo>(value));
        }


        [HttpPost]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> Upload(Test test)
        {
            if (test.file != null)
            {
                var _userPath = Path.Combine(_path, GetSessionId());

                if (!Directory.Exists(_userPath))
                    Directory.CreateDirectory(_userPath);

                string zipPath = Path.Combine(_userPath, test.file.FileName);

                using (var fileStream = new FileStream(zipPath, FileMode.Create))
                {
                    await test.file.CopyToAsync(fileStream);
                }

                ZipFile.ExtractToDirectory(zipPath, _userPath);

                System.IO.File.Delete(zipPath);

                return RedirectToAction(nameof(Tree), new { path = GetSessionId() });
            }

            return View();
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

        private string GetSize(long length)
        {
            string[] Symbols = { "b", "Kb", "Mb", "Gb" };
            int inc = 0;
            long temp = 0;

            while (length >= 1000)
            {
                if (length / 1024 < 1000)
                {
                    temp = length % 1024;
                }

                length /= 1024;
                inc++;
            }

            return $"{length}.{temp * 100 / 1024} {Symbols[inc]}";
        }
    }
}