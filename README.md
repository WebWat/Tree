## About this project
A simple file browser written in ASP.NET Core using **MVC architecture** and **sessions**.

## How it works
In the readme the methods is slightly simplified for general understanding!

The process of interaction with the application will look like this:
  1. First, the user uploads a **zip file** to a special form. Before unzip a project, check if user have upload data beforу. 
     
     >> We do this to create a new Id (if the user has already upload), 
        because the main `Tree` method, which displays the files on the site, uses the [ResponseCache](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/response?view=aspnetcore-6.0#responsecache-attribute) attribute. 
        If we use the old Id, the Tree method will redirect us to the old files.
     
     Next we need to load the unzipped data into a hash table (e.g. folder names, file names, paths, etc.) and delete files from the server.
     
     >> Why do we need to remove them from the server? 
        Because each client's files will take up a lot of space, and it is quite problematic to delete them after the session is over.
       
     Methods AddToSessionStorage and ClearFolder you can see in detail in the code.
        
     ``` csharp
      // The methods is slightly simplified for general understanding.
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

              string zipPath = Path.Combine(_userPath, im.File.FileName);

              using (var fileStream = new FileStream(zipPath, FileMode.Create))
              {
                  await im.File.CopyToAsync(fileStream);
              }

              ZipFile.ExtractToDirectory(zipPath, _userPath, entryNameEncoding:
                  Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage));

              System.IO.File.Delete(zipPath);
              AddToSessionStorage(temp);
              ClearFolder(_userPath);
              Directory.Delete(_userPath);

              await HttpContext.Session.CommitAsync();

              return RedirectToAction("tree", new { path = temp });
          }

          return RedirectToAction("index");
      }
     ```
    
  2. If all went well, we move on to the `Tree` method. It's pretty straightforward.
     ``` csharp
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
     ```

## Contributing
Ask any questions and submit pull requests that will help make this project better!
  
  
  
