using System.Collections.Generic;
using UnityEngine;

namespace NoOneCameBack.Tablet
{
    public class TabletTabBar : MonoBehaviour
    {
        [SerializeField] private RectTransform container;
        [SerializeField] private TabletTabView tabPrefab;

        private readonly List<TabletTabView> tabs = new List<TabletTabView>();

        public void Refresh(TabletController tablet)
        {
            IReadOnlyList<TabletApp> apps = tablet.Apps;
            while (tabs.Count < apps.Count) tabs.Add(Instantiate(tabPrefab, container));

            for (int i = 0; i < tabs.Count; i++)
            {
                bool used = i < apps.Count && apps[i] != null;
                tabs[i].gameObject.SetActive(used);
                if (!used) continue;

                TabletApp app = apps[i];
                TabletTabView.TabState state = app == tablet.CurrentApp ? TabletTabView.TabState.Selected
                    : tablet.IsUsable(app) ? TabletTabView.TabState.Available
                    : TabletTabView.TabState.Locked;
                tabs[i].Show(app, state);
            }
        }
    }
}
