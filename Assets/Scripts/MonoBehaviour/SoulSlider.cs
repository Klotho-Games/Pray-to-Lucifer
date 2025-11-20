using UnityEngine;

public class SoulSlider : MonoBehaviour
{
    [SerializeField] private LineRenderer soulLineRenderer;
    [SerializeField] private Gradient soulGradient;
    [SerializeField] private LineRenderer backgroundLineRenderer;
    [SerializeField] private PlayerStats playerStats;

    void Start()
    {
        backgroundLineRenderer.positionCount = 2;
        soulLineRenderer.positionCount = 2;
        soulLineRenderer.SetPosition(0, backgroundLineRenderer.GetPosition(0));
        soulLineRenderer.SetPosition(1, backgroundLineRenderer.GetPosition(0));

        playerStats.OnSoulChanged += UpdateSoulSlider;
    }

    void OnDestroy()
    {
        playerStats.OnSoulChanged -= UpdateSoulSlider;
    }

    private void UpdateSoulSlider()
    {
        if (playerStats.CurrentHealth <= 0)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        int currentSoul = playerStats.CurrentSoul;
        int maxSoul = playerStats.MaxSoul;
        Color soulColor = soulGradient.Evaluate((float)currentSoul / maxSoul);
        soulLineRenderer.startColor = soulColor;
        soulLineRenderer.endColor = soulColor;
        soulLineRenderer.SetPosition(1, Vector3.Lerp(backgroundLineRenderer.GetPosition(0), backgroundLineRenderer.GetPosition(1), (float)currentSoul / maxSoul));
    }
}
