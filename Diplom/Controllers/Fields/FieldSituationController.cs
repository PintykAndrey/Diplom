using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models.Fields;
using Diplom.Models.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Diplom.Controllers.Fields
{
    public class FieldSituationController : BaseController
    {
        public FieldSituationController(ApplicationDbContext context) : base(context) { }

        [HttpGet]
        public IActionResult FieldSituationLog()
        {
            if (!CanViewSection(SharedDataSection.Fields)) return Forbid();

            ViewBag.Fields = _context.Fields
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .OrderBy(x => x.Name)
                .ToList();

            var model = _context.FieldSituationLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();

            return View("~/Views/Fields/FieldSituationLog.cshtml", model);
        }

        [HttpPost]
        public IActionResult SaveSituationLog(List<FieldSituationLogModel> models)
        {
            if (!CanEditSection(SharedDataSection.Fields)) return ForbidSharedEdit();

            if (models == null || !models.Any())
                return RedirectToAction("FieldSituationLog");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            foreach (var model in models)
            {
                if (model.FieldId <= 0 && string.IsNullOrWhiteSpace(model.Description))
                    continue;

                if (!_context.Fields.Any(x => x.Id == model.FieldId && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null))
                    continue;

                model.Date = model.Date?.ToUniversalTime();

                DeleteSelectedPhotos(model, uploadsFolder);
                AddNewPhotos(model, uploadsFolder);
                SaveOrUpdateModel(model);
            }

            _context.SaveChanges();
            return RedirectToAction("FieldSituationLog");
        }

        private void DeleteSelectedPhotos(FieldSituationLogModel model, string uploadsFolder)
        {
            if (model.PhotosToDelete == null || !model.PhotosToDelete.Any())
                return;

            if (model.PhotoPaths == null)
                model.PhotoPaths = new List<string>();

            foreach (var path in model.PhotosToDelete)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var fullPath = Path.Combine(uploadsFolder, Path.GetFileName(path));

                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                model.PhotoPaths.Remove(path);
            }
        }

        private void AddNewPhotos(FieldSituationLogModel model, string uploadsFolder)
        {
            if (model.PhotoUploads == null)
                return;

            if (model.PhotoPaths == null)
                model.PhotoPaths = new List<string>();

            foreach (var file in model.PhotoUploads)
            {
                if (file.Length <= 0)
                    continue;

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                file.CopyTo(stream);

                model.PhotoPaths.Add($"/uploads/{fileName}");
            }
        }

        private void SaveOrUpdateModel(FieldSituationLogModel model)
        {
            if (model.Id == 0)
            {
                model.LastModified = DateTime.UtcNow;
                model.OwnerUserId = EffectiveOwnerUserId;
                _context.FieldSituationLogs.Add(model);
            }
            else
            {
                var existing = _context.FieldSituationLogs.FirstOrDefault(x => x.Id == model.Id && x.OwnerUserId == EffectiveOwnerUserId);
                if (existing == null)
                    return;

                bool hasChanges = false;

                if (existing.FieldId != model.FieldId) { existing.FieldId = model.FieldId; hasChanges = true; }
                if (existing.Date != model.Date) { existing.Date = model.Date; hasChanges = true; }
                if (existing.Description != model.Description) { existing.Description = model.Description; hasChanges = true; }
                if (existing.Recommendations != model.Recommendations) { existing.Recommendations = model.Recommendations; hasChanges = true; }

                if (model.PhotoPaths != null)
                {
                    existing.PhotoPaths = model.PhotoPaths;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    existing.LastModified = DateTime.UtcNow;
                    _context.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
            }
        }
    }
}