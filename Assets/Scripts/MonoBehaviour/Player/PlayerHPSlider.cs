using UnityEngine;

public class PlayerHPSlider : MonoBehaviour
{
    [SerializeField] private LineRenderer hpLineRenderer;
    [SerializeField] private Gradient hpGradient;
    [SerializeField] private LineRenderer backgroundLineRenderer;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private readonly float showDuration = 2f;
    private float showTimer = 0f;

    void Start()
    {
        backgroundLineRenderer.positionCount = 2;
        hpLineRenderer.positionCount = 2;
        hpLineRenderer.SetPosition(0, backgroundLineRenderer.GetPosition(0));
        hpLineRenderer.SetPosition(1, backgroundLineRenderer.GetPosition(1));

        playerStats.OnHealthChanged += UpdateHPSlider;

        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        playerStats.OnHealthChanged -= UpdateHPSlider;
    }

    void Update()
    {
        if (gameObject.activeSelf)
        {
            showTimer += Time.deltaTime;
            if (showTimer >= showDuration)
            {
                gameObject.SetActive(false);
                showTimer = 0f;
            }
        }
    }

    private void UpdateHPSlider()
    {
        showTimer = 0f;

        if (playerStats.CurrentHealth <= 0)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        int currentHP = playerStats.CurrentHealth;
        int maxHP = playerStats.MaxHealth;
        Color hpColor = hpGradient.Evaluate((float)currentHP / maxHP);
        hpLineRenderer.startColor = hpColor;
        hpLineRenderer.endColor = hpColor;
        hpLineRenderer.SetPosition(1, Vector3.Lerp(backgroundLineRenderer.GetPosition(0), backgroundLineRenderer.GetPosition(1), (float)currentHP / maxHP));
    }
}
