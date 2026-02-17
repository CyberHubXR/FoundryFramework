using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Foundry
{
    public class LocalPlayerMenuNavigation : MonoBehaviour
    {
        public enum NavigationMode
        {
            Linear,
            Hub
        }

        [Header("Navigation Mode")]
        [SerializeField] private NavigationMode navigationMode = NavigationMode.Linear;

        [Header("Ordered Pages")]
        [Tooltip("Pages in display order")]
        [SerializeField] private List<GameObject> pages = new List<GameObject>();

        [Header("Persistent Buttons (Optional)")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;

        private int currentIndex = -1;
        private Stack<int> history = new Stack<int>();

        void Start()
        {
            // Ensure all pages are disabled first
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] != null)
                    pages[i].SetActive(false);
            }

            // Start at first page
            if (pages.Count > 0)
                ShowPage(0, false);
        }

        /// <summary>
        /// Show page by index.
        /// </summary>
        public void ShowPage(int index, bool addToHistory = true)
        {
            if (index < 0 || index >= pages.Count)
                return;

            if (currentIndex == index)
                return;

            // Hide current
            if (currentIndex >= 0 && currentIndex < pages.Count)
            {
                if (addToHistory)
                    history.Push(currentIndex);

                pages[currentIndex].SetActive(false);
            }

            currentIndex = index;

            pages[currentIndex].SetActive(true);

            UpdateButtons();
        }

        /// <summary>
        /// Linear mode only: go forward in ordered list.
        /// </summary>
        public void NextPage()
        {
            if (navigationMode != NavigationMode.Linear)
                return;

            int next = currentIndex + 1;

            if (next < pages.Count)
                ShowPage(next);
        }

        /// <summary>
        /// Linear mode only: go backward in ordered list.
        /// </summary>
        public void PreviousPage()
        {
            if (navigationMode != NavigationMode.Linear)
                return;

            int prev = currentIndex - 1;

            if (prev >= 0)
                ShowPage(prev, false); // do not re-add history
        }

        /// <summary>
        /// Hub-style navigation or direct jumps.
        /// </summary>
        public void OpenPage(int index)
        {
            ShowPage(index);
        }

        /// <summary>
        /// Go back to previous page in history stack.
        /// </summary>
        public void GoBack()
        {
            if (history.Count == 0)
                return;

            pages[currentIndex].SetActive(false);

            currentIndex = history.Pop();

            pages[currentIndex].SetActive(true);

            UpdateButtons();
        }

        /// <summary>
        /// Reset history and return to first page.
        /// Useful when reopening the menu.
        /// </summary>
        public void ResetToFirstPage()
        {
            history.Clear();

            if (pages.Count > 0)
                ShowPage(0, false);
        }

        private void UpdateButtons()
        {
            if (!nextButton || !previousButton)
                return;

            if (navigationMode == NavigationMode.Linear)
            {
                previousButton.gameObject.SetActive(currentIndex > 0);
                nextButton.gameObject.SetActive(currentIndex < pages.Count - 1);
            }
            else
            {
                // In hub mode, hide linear navigation buttons
                previousButton.gameObject.SetActive(false);
                nextButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Get current page index (optional utility).
        /// </summary>
        public int GetCurrentIndex()
        {
            return currentIndex;
        }
    }
}
