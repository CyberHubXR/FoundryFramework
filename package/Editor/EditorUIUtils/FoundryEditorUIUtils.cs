using UnityEngine;
using UnityEngine.UIElements;

namespace Foundry.Core.Editor.UIUtils
{
    public static class EditorUIUtils
    {
        public static void SetBorderRadius(VisualElement element, float radius)
        {
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
        }

        public static void SetPadding(VisualElement element, float padding)
        {
            element.style.paddingLeft = padding;
            element.style.paddingRight = padding;
            element.style.paddingTop = padding;
            element.style.paddingBottom = padding;
        }
        
        public static void SetMargin(VisualElement element, float margin)
        {
            element.style.marginLeft = margin;
            element.style.marginRight = margin;
            element.style.marginTop = margin;
            element.style.marginBottom = margin;
        }

        public static void SetBorderWidth(VisualElement element, float width)
        {
            element.style.borderBottomWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderLeftWidth = width;
        }
        
        public static void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderBottomColor = color;
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderLeftColor = color;
        }
    }
}
