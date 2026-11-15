using System;
using CourseMod.Components.Atoms;
using TMPro;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.PackedPreviewList {
    public class PackedPreviewList : MonoBehaviour {
        public TextMeshProUGUI textPrefab;
        public GameObject separatorPrefab;

        public RectTransform textContainer;
        public int maxVisibleTexts;

        public TextMeshProUGUI prefix;
        public TextI18N andMoreText;

        private void Awake() => ResetDisplay();

        public void UpdateDisplay(string[] texts) {
            if (texts == null)
                throw new ArgumentNullException(nameof(texts));

            if (texts.Length == 0) {
                ResetDisplay();
                return;
            }

            var visibleCount = maxVisibleTexts == -1
                ? texts.Length
                : Mathf.Min(texts.Length, Mathf.Max(0, maxVisibleTexts));

            // Text, Separator, Text, Separator, Text...
            var desiredChildCount = Math.Max(0, visibleCount * 2 - 1);

            while (textContainer.childCount > desiredChildCount)
                RemoveLastChild();

            while (textContainer.childCount < desiredChildCount) {
                if (textContainer.childCount > 0)
                    Instantiate(separatorPrefab, textContainer);

                Instantiate(textPrefab, textContainer);
            }

            for (var i = 0; i < visibleCount; i++) {
                textContainer.GetChild(i * 2)
                    .GetComponent<TextMeshProUGUI>().text = texts[i];
            }

            var more = texts.Length - visibleCount;
            if (more > 0)
                andMoreText.UpdateArguments(new() { ["count"] = more });

            andMoreText.gameObject.SetActive(more > 0);
        }

        private void ResetDisplay() {
            while (textContainer.childCount > 1)
                RemoveLastChild();

            var text = textContainer.childCount == 0
                ? Instantiate(textPrefab, textContainer)
                : textContainer.GetChild(0).GetComponent<TextMeshProUGUI>();

            text.text = "-";
            andMoreText.gameObject.SetActive(false);
        }

        private void RemoveLastChild() {
            var child = textContainer.GetChild(textContainer.childCount - 1);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }
}
