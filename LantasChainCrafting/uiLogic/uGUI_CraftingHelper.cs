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
        private static TechType _currentType;
        public static TechType TreeType 
        { 
            get => _currentType;
            set
            {
                Plugin.Logger.LogInfo($"CurrentTechType: {_currentType}");
                _currentType = value;
                Plugin.Logger.LogInfo($"CurrentTechType set to {value}.");
                _instance.UpdateIcon(new(10, 10), value);
            } 
        }
        public static uGUI_CraftingHelper Instance
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
            label.GetComponent<TextMeshProUGUI>().text = "Crafting Helper";

            CreateIcon(new(10, 10), TechType.Welder);
        }
        private void CreateIcon(Vector2 anchoredPosition, TechType type)
        {
            if (type == TechType.None) return;
            Sprite sprite = SpriteManager.Get(type);
            GameObject iconObj = new("TechIcon", typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(canvas.transform, false);

            //RectTransform rect = iconObj.GetComponent<RectTransform>();
            //rect.anchoredPosition = anchoredPosition;
            //rect.sizeDelta = new Vector2(400, 400);
            ResourceTree wawa = new(null, new(type));
            Plugin.Logger.LogInfo($"{wawa.Data}: 360 / {wawa.GetOuterRing()}");
            wawa.GetRoot().LogTree();


            Image img = iconObj.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
        }

        private void DrawArch(double innerRad, double outerRad, double startAngle, int membercount, int ringLayer)
        {
        }

        private void UpdateIcon(Vector2 anchoredPosition, TechType type)
        {
            Image img = canvas.transform.Find("TechIcon").GetComponent<Image>();
            if (type == TechType.None)
            {
                img.enabled = false;
                return;
            }
            Sprite sprite = SpriteManager.Get(type);
            img.sprite = sprite;
            img.preserveAspect = true;
            img.enabled = true;
        }

        public override void Open()
        {
            canvas.SetVisible(true);
        }

        public override void Close()
        {
            canvas.SetVisible(false);
        }

        public void OnDestroy()
        {
            Instance = null;
        }
    }
}
