﻿// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using TMPro;
using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    public class StatisticsMenu : MenuSingleton<StatisticsMenu>
    {
        #region Fields
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI authorText;
        [SerializeField] private TextMeshProUGUI complexityText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private TextMeshProUGUI dietText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI abilitiesText;
        [SerializeField] private Transform dialog;
        #endregion

        #region Properties
        public bool HasEntered { get; set; }
        #endregion

        #region Methods
        public void Setup(BodyPart bodyPart, Vector3 position)
        {
            nameText.text = $"{bodyPart.name} (${bodyPart.Price})";
            authorText.text = $"{bodyPart.Author}";
            complexityText.text = bodyPart.Complexity.ToString();
            healthText.text = bodyPart.Health.ToString();
            weightText.text = $"{bodyPart.Weight}kg";

            // Speed
            speedText.transform.parent.gameObject.SetActive(bodyPart.Speed != 0);
            speedText.text = $"{((bodyPart.Speed > 0) ? "+" : "-")}{Mathf.Abs(bodyPart.Speed)}m/s";

            // Diet
            bool isMouth = bodyPart is Mouth;
            dietText.transform.parent.gameObject.SetActive(isMouth);
            if (isMouth)
            {
                dietText.text = LocalizationUtility.Localize($"diet_{(bodyPart as Mouth).Diet}".ToLower());
            }

            // Abilities
            bool hasAbilities = bodyPart.Abilities.Count > 0;
            abilitiesText.transform.parent.gameObject.SetActive(hasAbilities);
            if (hasAbilities)
            {
                abilitiesText.text = string.Join(", ", bodyPart.Abilities);
            }

            dialog.position = position;
            Open(HasEntered);

            HasEntered = true;
        }
        public void Clear()
        {
            HasEntered = false;

            this.Invoke(delegate
            {
                if (!HasEntered)
                {
                    Close();
                }
            }, 0.15f);
        }
        #endregion
    }
}