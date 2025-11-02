using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// Data classes for loading character configuration from YAML
    /// </summary>

    public class Vector3Config
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    public class RotationConfig
    {
        public float yaw { get; set; }
        public float pitch { get; set; }
        public float roll { get; set; }
    }

    public class ColliderConfig
    {
        public float width { get; set; }
        public float height { get; set; }
        public float depth { get; set; }
    }

    public class StressThresholdsConfig
    {
        public float doubt_effective { get; set; }
        public float accuse_effective { get; set; }
    }

    public class DialogueLine
    {
        public string speaker { get; set; }
        public string text { get; set; }
    }

    public class CharacterDialogueSequence
    {
        public string sequence_name { get; set; }
        public List<DialogueLine> lines { get; set; }
        public string on_complete { get; set; }

        // Interrogation-specific fields
        public string action { get; set; }  // "alibi", "relationship", "doubt", "accuse", "present_evidence"
        public bool is_correct { get; set; }  // true = correct action/evidence (increases stress), false = wrong (max stress)
        public bool requires_evidence { get; set; }  // true = needs evidence to select this option

        // Evidence presentation fields
        public string evidence_id { get; set; }  // ID of evidence this dialogue is for
        public float requires_stress_above { get; set; }  // Minimum stress % required (0 if not set)
        public float requires_stress_below { get; set; }  // Maximum stress % required (100 if not set)
        public float stress_increase { get; set; }  // Amount of stress to add (0 if not set, defaults based on is_correct)
    }

    public class CharacterConfig
    {
        public string name { get; set; }
        public string pronouns { get; set; }
        public string species { get; set; }
        public string gender { get; set; }
        public string age { get; set; }
        public string role { get; set; }
        public string personality { get; set; }
        public string portrait { get; set; }
        public string model { get; set; }

        public Vector3Config position { get; set; }
        public RotationConfig rotation { get; set; }
        public float scale { get; set; }

        public Vector3Config camera_position { get; set; }
        public Vector3Config camera_look_at { get; set; }

        public ColliderConfig collider { get; set; }

        public string secret { get; set; }
        public string public_story { get; set; }
        public bool is_killer { get; set; }
        public string killer_type { get; set; }
        public bool red_herring { get; set; }
        public List<string> key_evidence { get; set; }
        public StressThresholdsConfig stress_thresholds { get; set; }

        public List<CharacterDialogueSequence> dialogue { get; set; }
    }

    public class GameSettings
    {
        public float level_scale { get; set; }
        public float position_scale { get; set; }
        public List<string> spawn_sequence { get; set; }
    }

    public class LoungeCharactersData
    {
        public CharacterConfig bartender { get; set; }
        public CharacterConfig pathologist { get; set; }
        public CharacterConfig commander_von { get; set; }
        public CharacterConfig dr_thorne { get; set; }
        public CharacterConfig lt_webb { get; set; }
        public CharacterConfig ensign_tork { get; set; }
        public CharacterConfig maven_kilroth { get; set; }
        public CharacterConfig chief_solis { get; set; }
        public CharacterConfig tvora { get; set; }
        public CharacterConfig lucky_chen { get; set; }

        public GameSettings game_settings { get; set; }

        // Helper to get character by key
        public CharacterConfig GetCharacter(string key)
        {
            return key switch
            {
                "bartender" => bartender,
                "pathologist" => pathologist,
                "commander_von" => commander_von,
                "dr_thorne" => dr_thorne,
                "lt_webb" => lt_webb,
                "ensign_tork" => ensign_tork,
                "maven_kilroth" => maven_kilroth,
                "chief_solis" => chief_solis,
                "tvora" => tvora,
                "lucky_chen" => lucky_chen,
                _ => null
            };
        }
    }
}
