using ChainCrafting.Utils;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChainCrafting.uiLogic
{
    public class uGUI_CraftingHelper : uGUI_PDATab
    {
        public uGUI_CraftingHelper Instance
        {
            get
            {
                return _instance;
            }
            set
            {
                if (_instance != null && value != null)
                {
                    Plugin.Logger.LogError($"More than 1 uGUI_CraftingHelper in the scene! Attempted to set {value} to _Instance");
                    return;
                }

                _instance = value;
            }
        }

        private static uGUI_CraftingHelper _instance;

        public CanvasGroup canvas;

        new public void Awake()
        {
            canvas = GetComponentInChildren<CanvasGroup>();

            Instance = this;
        }

        public void Start()
        {
            GameObject label = transform.Find("Content/LogLabel").gameObject;
            label.name = "Crafting Helper";
            label.GetComponent<TextMeshProUGUI>().text = "This is the crafting helper";

            GetComponentInChildren<RectMask2D>().enabled = true;

            Sprite sprite = SpriteManager.Get(TechType.CopperWire);
            CreateIcon(new(10, 10), sprite);
        }
        private void CreateIcon(Vector2 anchoredPosition, Sprite sprite)
        {
            GameObject iconObj = new("TechIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(transform, false);

            RectTransform rect = iconObj.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(40, 40);

            Image img = iconObj.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
        }

        public void OnDestroy()
        {
            Instance = null;
        }
    }
}
