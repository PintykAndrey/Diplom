using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using Diplom.Models.Fields;
using Diplom.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Diplom.Controllers.Fields
{
    public class KmlController : Controller
    {
        private readonly ApplicationDbContext _context;
        public KmlController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult UploadKml()
        {
            var model = new UploadKmlViewModel();
            return View("~/Views/Fields/UploadKml.cshtml", model);
        }

        [HttpPost]
        public IActionResult UploadKml(IFormFile file)
        {
            var model = new UploadKmlViewModel();

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Select the KML file.");
                return View("~/Views/Fields/UploadKml.cshtml", model);
            }

            using (var stream = file.OpenReadStream())
            {
                var doc = XDocument.Load(stream);
                XNamespace ns = "http://www.opengis.net/kml/2.2";

                var placemarks = doc.Descendants(ns + "Placemark");

                foreach (var placemark in placemarks)
                {
                    var name = placemark.Element(ns + "name")?.Value;
                    var coordinates = placemark.Descendants(ns + "coordinates").FirstOrDefault()?.Value;

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(coordinates))
                    {
                        var field = new KmlField
                        {
                            Name = name,
                            CoordinatesJson = coordinates.Trim()
                        };
                        model.Fields.Add(field);
                    }
                }
            }

            return View("~/Views/Fields/UploadKml.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveField(string name, double areaHectares, double perimeterMeters, string geometry)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(geometry))
                return BadRequest("Missing data");

            var field = new FieldEntity
            {
                Name = name,
                OwnerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                AreaHectares = areaHectares,
                PerimeterMeters = perimeterMeters,
                Geometry = geometry
            };

            _context.Fields.Add(field);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}