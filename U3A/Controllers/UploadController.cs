using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace U3A.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public partial class UploadController : ControllerBase
    {
        protected string ContentRootPath { get; set; }

        [HttpPost("[action]")]
        public ActionResult UploadFile(IFormFile myFile)
        {
            try
            {
                var path = GetOrCreateUploadFolder();
                using (var fileStream = System.IO.File.Create(Path.Combine(path, myFile.FileName)))
                {
                    myFile.CopyTo(fileStream);
                }
            }
            catch
            {
                Response.StatusCode = 400;
            }
            return new EmptyResult();
        }

        public UploadController(IWebHostEnvironment hostingEnvironment)
        {
            ContentRootPath = hostingEnvironment.ContentRootPath;
        }

        public string GetOrCreateUploadFolder()
        {
            var pathname = "uploads";
            var path = Path.Combine(ContentRootPath, pathname);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            DeleteOldTempFiles(path);
            return path;
        }
        private void DeleteOldTempFiles(string dirName)
        {
            Directory.GetFiles(dirName)
                 .Select(f => new FileInfo(f))
                 .Where(f => f.LastAccessTime < DateTime.UtcNow.AddHours(-6))
                 .ToList()
                 .ForEach(f => f.Delete());
        }

    }
}
