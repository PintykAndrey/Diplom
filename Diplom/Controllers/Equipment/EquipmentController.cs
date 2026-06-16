using Diplom.Controllers.Base;

using Diplom.Data;

using Diplom.Models;
using Diplom.Models.Identity;
using Microsoft.AspNetCore.Mvc;


namespace Diplom.Controllers

{

    public class EquipmentController : BaseController

    {

        public EquipmentController(ApplicationDbContext context) : base(context) { }


        public IActionResult Index()

        {

            return View("~/Views/Equipment/Index.cshtml");

        }


        [HttpGet]

        public IActionResult AddEquipment()

        {

            if (!CanViewSection(SharedDataSection.Equipment)) return Forbid();

            var equipments = _context.Equipments
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId)
                .ToList();

            ViewBag.Operators = _context.Operators
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId)
                .ToList();

            return View("~/Views/Equipment/AddEquipment.cshtml", equipments);

        }


        [HttpPost]

        public IActionResult SaveEquipment(List<EquipmentModel> models)

        {

            if (!CanEditSection(SharedDataSection.Equipment)) return ForbidSharedEdit();

            if (models == null || !models.Any())

                return RedirectToAction(nameof(AddEquipment));


            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");


            if (!Directory.Exists(uploadsFolder))

                Directory.CreateDirectory(uploadsFolder);


            foreach (var model in models)

            {

                if (string.IsNullOrWhiteSpace(model.Name))

                    continue;


                DeleteSelectedPhotos(model, uploadsFolder);

                AddNewPhotos(model, uploadsFolder);

                SaveOrUpdateModel(model);

            }


            _context.SaveChanges();

            return RedirectToAction(nameof(AddEquipment));

        }


        private void DeleteSelectedPhotos(EquipmentModel model, string uploadsFolder)

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


        private void AddNewPhotos(EquipmentModel model, string uploadsFolder)

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


        private void SaveOrUpdateModel(EquipmentModel model)

        {

            if (model.Id == 0)

            {

                model.OwnerUserId = EffectiveOwnerUserId;
                _context.Equipments.Add(model);

            }

            else

            {

                var existing = _context.Equipments.FirstOrDefault(x => x.Id == model.Id && x.OwnerUserId == EffectiveOwnerUserId);

                if (existing == null)

                    return;


                bool hasChanges = false;


                if (existing.Name != model.Name) { existing.Name = model.Name; hasChanges = true; }

                if (existing.Type != model.Type) { existing.Type = model.Type; hasChanges = true; }

                if (existing.Year != model.Year) { existing.Year = model.Year; hasChanges = true; }

                if (existing.WorkingHours != model.WorkingHours) { existing.WorkingHours = model.WorkingHours; hasChanges = true; }

                if (existing.Operator != model.Operator) { existing.Operator = model.Operator; hasChanges = true; }


                if (model.PhotoPaths != null)

                {

                    existing.PhotoPaths = model.PhotoPaths;

                    hasChanges = true;

                }


                if (hasChanges)

                {

                    _context.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                }

            }

        }

    }

}