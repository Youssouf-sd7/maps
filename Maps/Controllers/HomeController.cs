using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text.Json;
using Maps.Models;

namespace Maps.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private const string SecretCode = "MAPS-40"; // Le code secret pour ton prof
        private const string SessionKey = "IsAuthenticated";

        public HomeController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Page de Login
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString(SessionKey) == "true")
                return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        public IActionResult Login(string code)
        {
            if (code == SecretCode)
            {
                HttpContext.Session.SetString(SessionKey, "true");
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Code secret incorrect.");
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Liste des espèces
        public IActionResult Index()
        {
            if (!CheckAuth()) return RedirectToAction("Login");

            var jsonPath = Path.Combine(_env.WebRootPath, "Assets", "species.json");
            if (!System.IO.File.Exists(jsonPath)) return Content("Fichier species.json introuvable dans wwwroot/Assets/");

            var jsonContent = System.IO.File.ReadAllText(jsonPath);
            var speciesList = JsonSerializer.Deserialize<List<SpeciesOccurrence>>(jsonContent);

            var choices = LoadChoices();
            ViewBag.Choices = choices;

            return View(speciesList);
        }

        // Page de détail d'une espèce (Comparaison des 4 images)
        public IActionResult Details(string name)
        {
            if (!CheckAuth()) return RedirectToAction("Login");

            var jsonPath = Path.Combine(_env.WebRootPath, "Assets", "species.json");
            var jsonContent = System.IO.File.ReadAllText(jsonPath);
            var speciesList = JsonSerializer.Deserialize<List<SpeciesOccurrence>>(jsonContent);

            var currentIndex = speciesList.FindIndex(s => s.Species == name);
            if (currentIndex == -1) return NotFound();

            var species = speciesList[currentIndex];
            string baseFileName = Path.GetFileNameWithoutExtension(species.File);

            var viewModel = new SpeciesViewModel
            {
                OccurrenceData = species,
                OccurrenceImage = $"/Assets/images/{baseFileName}_occurrences.png",
                Seuil10Image = $"/Assets/images/{baseFileName}_seuil_10pct.png",
                Seuil20Image = $"/Assets/images/{baseFileName}_seuil_20pct.png",
                Seuil30Image = $"/Assets/images/{baseFileName}_seuil_30pct.png",
                HasThresholds = species.TotalOccurrences >= 30,

                // Gérer Précédent / Suivant (boucle ou limites)
                PreviousSpecies = currentIndex > 0 ? speciesList[currentIndex - 1].Species : null,
                NextSpecies = currentIndex < speciesList.Count - 1 ? speciesList[currentIndex + 1].Species : null
            };

            var choices = LoadChoices();
            var existingChoice = choices.Find(c => c.SpeciesName == name);
            viewModel.CurrentSelectedThreshold = existingChoice?.SelectedThreshold ?? "none";
            viewModel.CurrentComment = existingChoice?.Comment ?? "";

            return View(viewModel);
        }
        // Enregistrer le choix et le commentaire (via AJAX ou Formulaire)
        [HttpPost]
        public IActionResult SaveChoice(string speciesName, string selectedThreshold, string comment)
        {
            if (!CheckAuth()) return Unauthorized();

            var choices = LoadChoices();
            var existing = choices.Find(c => c.SpeciesName == speciesName);

            if (existing != null)
            {
                existing.SelectedThreshold = selectedThreshold;
                existing.Comment = comment;
            }
            else
            {
                choices.Add(new ProfessorChoice
                {
                    SpeciesName = speciesName,
                    SelectedThreshold = selectedThreshold,
                    Comment = comment
                });
            }

            SaveChoices(choices);
            return RedirectToAction("Details", new { name = speciesName });
        }

        // Télécharger toutes les images choisies sous forme de ZIP
        public IActionResult DownloadSelected()
        {
            if (!CheckAuth()) return RedirectToAction("Login");

            var choices = LoadChoices();
            var jsonPath = Path.Combine(_env.WebRootPath, "Assets", "species.json");
            var speciesList = JsonSerializer.Deserialize<List<SpeciesOccurrence>>(System.IO.File.ReadAllText(jsonPath));

            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Ajouter le fichier JSON des choix du prof dans le ZIP
                //var choiceJsonPath = Path.Combine(_env.WebRootPath, "Assets", "choix_professeur.json");
                //if (System.IO.File.Exists(choiceJsonPath))
                //{
                //    archive.CreateEntryFromFile(choiceJsonPath, "choix_professeur.json");
                //}

                foreach (var choice in choices)
                {
                    if (choice.SelectedThreshold != "none" && !string.IsNullOrEmpty(choice.SelectedThreshold))
                    {
                        var species = speciesList.Find(s => s.Species == choice.SpeciesName);
                        if (species != null)
                        {
                            string baseFileName = Path.GetFileNameWithoutExtension(species.File);
                            string imageName = $"{baseFileName}_seuil_{choice.SelectedThreshold}.png";
                            string imagePath = Path.Combine(_env.WebRootPath, "Assets", "images", imageName);

                            if (System.IO.File.Exists(imagePath))
                            {
                                archive.CreateEntryFromFile(imagePath, $"cartes_validees/{imageName}");
                            }
                        }
                    }
                }
            }

            memoryStream.Position = 0;
            return File(memoryStream, "application/zip", "Cartes_Validees.zip");
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString(SessionKey) == "true";
        }

        private List<ProfessorChoice> LoadChoices()
        {
            var filePath = Path.Combine(_env.WebRootPath, "Assets", "choix_professeur.json");
            if (!System.IO.File.Exists(filePath)) return new List<ProfessorChoice>();

            var json = System.IO.File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<ProfessorChoice>>(json) ?? new List<ProfessorChoice>();
        }

        private void SaveChoices(List<ProfessorChoice> choices)
        {
            var filePath = Path.Combine(_env.WebRootPath, "Assets", "choix_professeur.json");
            var json = JsonSerializer.Serialize(choices, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        }
    }
}