using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    public class StatusStripView : MonoBehaviour
    {
        [Serializable]
        private struct MindLevel
        {
            [Range(0f, 1f)] public float minPsychosis;
            public string text;
        }

        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private TMP_Text mindLabel;

        [Header("Health")]
        // TODO(diseño): health fraction under which the player reads as injured.
        [SerializeField, Range(0f, 1f)] private float injuredThreshold = 0.6f;
        [SerializeField] private string healthyText = "Bien";
        [SerializeField] private string injuredText = "Herido";
        [SerializeField] private string bleedingSuffix = " (sangrando)";

        [Header("Mind")]
        // TODO(diseño): psychosis thresholds and wording.
        [SerializeField] private List<MindLevel> mindLevels = new List<MindLevel>
        {
            new MindLevel { minPsychosis = 0f, text = "Lúcido" },
            new MindLevel { minPsychosis = 0.25f, text = "Inquieto" },
            new MindLevel { minPsychosis = 0.5f, text = "Perturbado" },
            new MindLevel { minPsychosis = 0.75f, text = "Al borde" },
        };

        private IHealth health;
        private IPsychosis psychosis;
        private IBleeding bleeding;

        private void OnDestroy() => Unbind();

        public void Bind(IHealth healthSource, IPsychosis psychosisSource, IBleeding bleedingSource)
        {
            Unbind();
            health = healthSource;
            psychosis = psychosisSource;
            bleeding = bleedingSource;

            if (health != null) health.Changed += Refresh;
            if (psychosis != null) psychosis.Changed += Refresh;
            if (bleeding != null) bleeding.Changed += Refresh;
            Refresh();
        }

        public void Unbind()
        {
            if (health != null) health.Changed -= Refresh;
            if (psychosis != null) psychosis.Changed -= Refresh;
            if (bleeding != null) bleeding.Changed -= Refresh;
            health = null;
            psychosis = null;
            bleeding = null;
        }

        private void Refresh()
        {
            if (healthLabel != null)
            {
                bool injured = health != null && health.Max > 0f && health.Current / health.Max < injuredThreshold;
                string text = injured ? injuredText : healthyText;
                if (bleeding != null && bleeding.IsBleeding) text += bleedingSuffix;
                healthLabel.text = text;
            }

            if (mindLabel != null) mindLabel.text = GetMindText(psychosis != null ? psychosis.Level01 : 0f);
        }

        private string GetMindText(float level)
        {
            string text = string.Empty;
            float best = float.MinValue;
            foreach (MindLevel mind in mindLevels)
            {
                if (level >= mind.minPsychosis && mind.minPsychosis >= best)
                {
                    best = mind.minPsychosis;
                    text = mind.text;
                }
            }
            return text;
        }
    }
}
