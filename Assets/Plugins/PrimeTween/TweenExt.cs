using System.Reflection;
using UnityEngine;

namespace PrimeTween
{
    /// <summary>
    /// Extension methods for PrimeTween to support IColorable interface.
    /// Allows tweening color on any component that implements IColorable.
    /// </summary>
    public static class TweenExt
    {
        private static readonly bool enableDebug = false;
        /// <summary>
        /// Tweens the color property of any component implementing IColorable.
        /// Works with SpriteRenderer, UI.Image, and any custom component with IColorable.
        /// </summary>
        public static Tween Color(Component target, Color endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0f, float endDelay = 0f, bool useUnscaledTime = false)
        {
            if (target == null)
            {
                Debug.LogError("TweenExt.Color: target is null");
                return default;
            }

            // Check if target implements IColorable
            if (target is IColorable colorable)
            {
                Color colorableStartValue = colorable.color;
                
                if (enableDebug && Application.isEditor)
                    Debug.Log($"TweenExt.Color: Tweening IColorable {target.GetType().Name} from {colorableStartValue} to {endValue}");
                
                return Tween.Custom(colorableStartValue, endValue, duration, onValueChange: newColor =>
                {
                    if (target != null && target is IColorable c)
                        c.color = newColor;
                }, ease: ease, cycles: cycles, cycleMode: cycleMode, startDelay: startDelay, endDelay: endDelay, useUnscaledTime: useUnscaledTime);
            }

            // Fallback to reflection for components with color property but no IColorable
            PropertyInfo colorProperty = target.GetType().GetProperty("color");
            
            if (colorProperty == null || colorProperty.PropertyType != typeof(Color))
            {
                Debug.LogError($"TweenExt.Color: {target.GetType().Name} does not have a 'color' property or implement IColorable");
                return default;
            }

            Color reflectionStartValue;
            try
            {
                reflectionStartValue = (Color)colorProperty.GetValue(target);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TweenExt.Color: Failed to get color from {target.GetType().Name}: {e.Message}");
                return default;
            }

            if (enableDebug && Application.isEditor)
                Debug.Log($"TweenExt.Color: Tweening reflection-based {target.GetType().Name} from {reflectionStartValue} to {endValue}");
            
            return Tween.Custom(reflectionStartValue, endValue, duration, onValueChange: newColor =>
            {
                if (target != null)
                {
                    try
                    {
                        colorProperty.SetValue(target, newColor);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"TweenExt.Color: Failed to set color on {target.GetType().Name}: {e.Message}");
                    }
                }
            }, ease: ease, cycles: cycles, cycleMode: cycleMode, startDelay: startDelay, endDelay: endDelay, useUnscaledTime: useUnscaledTime);
        }
    }
}
