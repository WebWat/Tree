using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

            string? value = HttpContext.Session.GetString(sessionId);

            if (string.IsNullOrEmpty(value))
            {
                return View(new IndexModel());
            }

            return View(new IndexModel { SessionId = sessionId });
        }


        [ResponseCache(Location = ResponseCacheLocation.Client, Duration = 60 * 10)]
        public async Task<IActionResult> Tree(string path)
        {
            await HttpContext.Session.LoadAsync();

            string? value = HttpContext.Session.GetString(path);

            if (string.IsNullOrEmpty(value))
            {
                _logger.LogWarning("Incorrect session!");
                return RedirectToAction(nameof(Index));
            }

            return View(JsonSerializer.Deserialize<FilesInfo>(value));
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IndexModel im)
        {
            if (im.File != null)
            {
                await HttpContext.Session.LoadAsync();

                var _userPath = Path.Combine(_path, GetSessionId());
                var temp = GetSessionId();

                if (HttpContext.Session.TryGetValue(temp, out _))
                {
                    HttpContext.Session.Clear();

                    temp = Guid.NewGuid().ToString();

                    HttpContext.Session.SetString("Id", temp);

                    _logger.LogInformation("Create new id: " + temp);

                    _userPath = _userPath.Remove(_userPath.Length - 36) + temp;
                }

                Directory.CreateDirectory(_userPath);

                _logger.LogInformation("Create directory: " + _userPath);
                _logger.LogInformation("Start uploading and extracting...");

                Stopwatch stopwatch = Stopwatch.StartNew();

                string zipPath = Path.Combine(_userPath, im.File.FileName);

                using (var fileStream = new FileStream(zipPath, FileMode.Create))
                {
                    await im.File.CopyToAsync(fileStream);
                }

                ZipFile.ExtractToDirectory(zipPath, _userPath, entryNameEncoding:
                    Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage));

                stopwatch.Stop();

                _logger.LogInformation($"Uploaded and extracted! Total time: {stopwatch.ElapsedMilliseconds / 1000d:f3} s.");

                _logger.LogInformation("Save data in the session and delete all downloaded data..");

                stopwatch.Restart();

                System.IO.File.Delete(zipPath);
                AddToSessionStorage(temp);
                ClearFolder(_userPath);
                Directory.Delete(_userPath);

                _logger.LogInformation($"Loaded and cleared! Total time: {stopwatch.ElapsedMilliseconds / 1000d:f3} s.");

                await HttpContext.Session.CommitAsync();

                return RedirectToAction("tree", new { path = temp });
            }

            return RedirectToAction("index");
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

            foreach (var di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            }
        }

        private void AddToSessionStorage(string id)
        {
            FilesInfo item;

            List<Models.File> list;

            string newPath;

            DirectoryInfo dir;

            void SetFolder(string path)
            {
                list = new List<Models.File>();

                newPath = Path.Combine(_path, path.Replace('/', '\\'));

                dir = new DirectoryInfo(newPath);

                foreach (var item in dir.GetDirectories())
                {
                    list.Add(new Models.File { Name = item.Name, IsFolder = true });
                }

                foreach (var item in dir.GetFiles())
                {
                    list.Add(new Models.File { Name = item.Name, Length = GetSize(item.Length), IsFolder = false });
                    item.Delete();
                }

                item = new FilesInfo { CurrentPath = path, Files = list };

                HttpContext.Session.SetString(path, JsonSerializer.Serialize(item));

                foreach (var di in dir.GetDirectories())
                {
                    SetFolder(path + "/" + di.Name);
                }
            }

            SetFolder(id);
        }
    }
}