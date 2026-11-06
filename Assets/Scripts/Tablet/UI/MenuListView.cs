using System.Collections.Generic;
using UnityEngine;

namespace NoOneCameBack.Tablet
{
    // Vertical option list driven by TabletInput: pad/keys step through options, the mouse hovers and clicks them.
    public class MenuListView : MonoBehaviour
    {
        [Tooltip("Parent for the options. A VerticalLayoutGroup here handles spacing.")]
        [SerializeField] private RectTransform container;
        [SerializeField] private MenuOptionView optionPrefab;
        [SerializeField] private bool wrapAround = true;

        private readonly List<MenuOptionView> options = new List<MenuOptionView>();
        private int count;
        private Camera eventCamera;
        private bool cameraResolved;

        public int Count => count;
        public int SelectedIndex { get; private set; } = -1;

        public void SetOptions(IReadOnlyList<string> labels)
        {
            count = labels.Count;
            while (options.Count < count) options.Add(Instantiate(optionPrefab, container));

            for (int i = 0; i < options.Count; i++)
            {
                bool used = i < count;
                options[i].gameObject.SetActive(used);
                if (used) options[i].SetLabel(labels[i]);
            }

            Select(count > 0 ? 0 : -1);
        }

        public void Select(int index)
        {
            SelectedIndex = count == 0 ? -1 : Mathf.Clamp(index, 0, count - 1);
            for (int i = 0; i < count; i++) options[i].SetSelected(i == SelectedIndex);
        }

        // Returns the index the player confirmed this frame, or -1.
        public int HandleInput(TabletInput input)
        {
            if (count == 0) return -1;

            int step = input.NavigationStep.y;
            if (step != 0)
            {
                int next = SelectedIndex + step;
                Select(wrapAround ? (next % count + count) % count : next);
            }

            int hovered = GetOptionAt(input.PointerPosition);
            if (input.PointerMoved && hovered >= 0) Select(hovered);

            if (input.SubmitPressed) return SelectedIndex;
            if (input.ClickPressed && hovered >= 0)
            {
                Select(hovered);
                return hovered;
            }

            return -1;
        }

        public bool IsPointerOver(Vector2 screenPosition) =>
            container != null && RectTransformUtility.RectangleContainsScreenPoint(container, screenPosition, GetCamera());

        private int GetOptionAt(Vector2 screenPosition)
        {
            Camera cam = GetCamera();
            for (int i = 0; i < count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(options[i].Rect, screenPosition, cam)) return i;
            }
            return -1;
        }

        private Camera GetCamera()
        {
            if (!cameraResolved)
            {
                eventCamera = TabletUIUtility.GetEventCamera(this);
                cameraResolved = true;
            }
            return eventCamera;
        }
    }
}
