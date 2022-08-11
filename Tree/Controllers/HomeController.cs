using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Tree.Models;

namespace Tree.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly string _path;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment appEnvironment)
        {
            _logger = logger;
            _appEnvironment = appEnvironment;

            _path = appEnvironment.WebRootPath + @"\Files\";
        }

        public IActionResult Index()
        {
            var sessionId = GetSessionId();

            if (!Directory.Exists(Path.Combine(_path, sessionId)))
            {
                return View(new IndexModel { BeenBefore = false });
            }

            return View(new IndexModel { BeenBefore = true, SessinId = sessionId });
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
                _logger.LogInformation($"Adding data to cookies: {path}");

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

            _logger.LogInformation($"The data is derived from a cookie: {path}");

            return View(JsonSerializer.Deserialize<FilesInfo>(value));
        }


        [HttpPost]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> Upload(IndexModel im)
        {
            if (im.File != null)
            {
                var _userPath = Path.Combine(_path, GetSessionId());

                if (Directory.Exists(_userPath))
                {
                    var temp = GetSessionId();
                    HttpContext.Session.Clear();
                    HttpContext.Session.SetString("Id", temp);

                    ClearFolder(_userPath);
                    Directory.Delete(_userPath);
                }

                Directory.CreateDirectory(_userPath);

                string zipPath = Path.Combine(_userPath, im.File.FileName);

                using (var fileStream = new FileStream(zipPath, FileMode.Create))
                {
                    await im.File.CopyToAsync(fileStream);
                }

                ZipFile.ExtractToDirectory(zipPath, _userPath, entryNameEncoding:
                    Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage));

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
            string? value = HttpContext.Session.GetString("Id");

            ArgumentNullException.ThrowIfNull(value);

            return value;
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

        private void ClearFolder(string path)
        {
            var dir = new DirectoryInfo(path);

            foreach (var fi in dir.GetFiles())
            {
                fi.Delete();
            }

            foreach (var di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            }
        }

        private void AddFolderToCookie(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);

            foreach (var fi in dir.GetFiles())
            {
                fi.Delete();
            }

            foreach (var di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            }
        }
    }
}