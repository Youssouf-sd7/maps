using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Maps.Models
{
    public class SpeciesOccurrence
    {
        [JsonPropertyName("species")]
        public string Species { get; set; }

        [JsonPropertyName("file")]
        public string File { get; set; }

        [JsonPropertyName("total_occurrences")]
        public int TotalOccurrences { get; set; }

        [JsonPropertyName("unique_occurrences")]
        public int UniqueOccurrences { get; set; }
    }

    public class ProfessorChoice
    {
        public string SpeciesName { get; set; }
        public string SelectedThreshold { get; set; } // "10pct", "20pct", "30pct" ou "none"
        public string Comment { get; set; }
    }

    public class SpeciesViewModel
    {
        public SpeciesOccurrence OccurrenceData { get; set; }
        public string OccurrenceImage { get; set; }
        public string Seuil10Image { get; set; }
        public string Seuil20Image { get; set; }
        public string Seuil30Image { get; set; }
        public bool HasThresholds { get; set; }

        public string CurrentSelectedThreshold { get; set; }
        public string CurrentComment { get; set; }

        // Navigation
        public string PreviousSpecies { get; set; }
        public string NextSpecies { get; set; }
    }
}