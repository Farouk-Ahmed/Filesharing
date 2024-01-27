﻿#region Constructor
using Filesharing.Services.Interface;
using Microsoft.AspNetCore.Hosting;

namespace Filesharing.Controllers
{
    [Authorize]
    public class UploadController : Controller
    {
        private readonly IUploadService db;
		private readonly IWebHostEnvironment webHostEnvironment;

		public UploadController(IUploadService db , IWebHostEnvironment WebHostEnvironment)
        {
            this.db = db;
			webHostEnvironment = WebHostEnvironment;
		}

        #endregion
        
        #region Index (To_Get_Upload_By_UserID)

        public IActionResult Index()
        {
            var result = db.GetAllUploadsbyUserID(userid);
            return View(result);
        }
        #endregion

        #region Browse

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Browse()
        {
            var result = db.GetAllUploads();
            return View(result);

        }
        #endregion

        #region Download

        [HttpGet]
        public async Task<IActionResult> Download(string id)
        {
            var selectedFile = await db.FindAsync(id);
            
            if (selectedFile == null) { return NotFound(); }

            await db.IncreamentDownloadCountAsync(id);

            var path = "~/Uploads/" + selectedFile.FileName;

            #region Clear Cache
            Response.Headers.Add("Expires", DateTime.Now.AddDays(-3).ToLongDateString());
            Response.Headers.Add("Cache-Control", "no-cache");
            #endregion

            return File(path, selectedFile.ContentType, selectedFile.OriginalFileName);
        }

        #endregion

        #region Create

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InputFile model)
        {
            if (ModelState.IsValid)
            {
                // Get Filename new Guid to make it Unique ex.
                var NewFile = Guid.NewGuid().ToString();                 
                var extention = Path.GetExtension(model.File.FileName);
                var fileName = string.Concat(NewFile, extention);


                var root = webHostEnvironment.WebRootPath;
                var path = Path.Combine(root, "Uploads", fileName);

                using (var fs = System.IO.File.Create(path))
                {
                    await model.File.CopyToAsync(fs);
                }


                var inputupload = new InputUpload()
                {
                    OriginalFileName = model.File.FileName,
                    FileName = fileName,
                    ContentType = model.File.ContentType,
                    Size = model.File.Length,
                    UserId = userid,
                };

                await db.CreateAsync(inputupload);
                return RedirectToAction("Index");
            }
            return View(model);
        }

        #endregion

        #region Edit
        [HttpGet]
        public IActionResult Edit(int id)
        {
            return View();
        }

        #endregion

        #region Delete

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            // This's Code Be., Not AnyOne Delete Item From Another User 
            var selecteditem = await db.FindAsync(id , userid);
            if (selecteditem == null)
            {
                return NotFound();
            }
            return View(selecteditem);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            await db.DeleteAsync(id, userid);
            
            return RedirectToAction("Index");
        }

        #endregion

        #region Search

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Search(string term)
        {
            var result = db.Search(term);

			return View(result);
        }
		#endregion

		#region Helper
		private string userid
		{
			get
			{
				return User.FindFirstValue(ClaimTypes.NameIdentifier);
			}
		}

		#endregion
	}
}