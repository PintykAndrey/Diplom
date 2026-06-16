using Microsoft.AspNetCore.Mvc;
using Diplom.Data;
using Diplom.Models.Tools;
using Microsoft.EntityFrameworkCore;
using Diplom.Localization;
using Diplom.Controllers.Base;
using System.Collections.Generic;
using System.Linq;

namespace Diplom.Controllers.Tools
{
    public class VocabularyController : BaseController
    {
        private readonly VocabularyCache _cache;

        public VocabularyController(ApplicationDbContext context, VocabularyCache cache) : base(context)
        {
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Vocabulary()
        {
            var model = _context.Vocabulary
                .AsNoTracking()
                .GroupBy(v => v.Key)
                .Select(g => new
                {
                    Key = g.Key,
                    Translations = g.Select(t => new { t.Language, t.Value }).ToList()
                })
                .ToList();

            return View("~/Views/Tools/Vocabulary.cshtml", model);
        }

        [HttpPost]
        public IActionResult UpdateAjax([FromBody] List<TranslationUpdateRequest> requests)
        {
            foreach (var req in requests)
            {
                var entry = _context.Vocabulary
                    .FirstOrDefault(v => v.Key == req.Key && v.Language == req.Language);

                if (entry != null)
                {
                    entry.Value = req.Value;
                }
            }

            _context.SaveChanges();
    
            _cache.Load(_context);

            return Json(new { success = true });
        }

        public class TranslationUpdateRequest
        {
            public string Key { get; set; }
            public string Language { get; set; }
            public string Value { get; set; }
        }
    }
}
