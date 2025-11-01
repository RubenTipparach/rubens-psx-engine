using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace anakinsoft.game.scenes.lounge.evidence
{
    /// <summary>
    /// Data classes for loading evidence configuration from YAML
    /// </summary>

    public class EvidenceVisualScale
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    public class EvidenceItemConfig
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int table_row { get; set; }
        public int table_column { get; set; }
        public EvidenceVisualScale visual_scale { get; set; }
        public string texture { get; set; }
        public List<string> related_characters { get; set; }
    }

    public class EvidenceData
    {
        public List<EvidenceItemConfig> evidence { get; set; }

        // Helper to get evidence by ID
        public EvidenceItemConfig GetEvidence(string id)
        {
            return evidence?.Find(e => e.id == id);
        }
    }
}
