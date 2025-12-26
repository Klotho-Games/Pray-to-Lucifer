using PrimeTween;
using UnityEngine;

[CreateAssetMenu(fileName = "HighlightLevelSelectionCardNumber_", menuName = "Actions/Single-Use/Highlight Level Selection Card")]
public class HighlightLevelSelectionCard : ActionSO
{
    [SerializeField] private Color highlightedColor = Color.gray;
    [SerializeField] private Color normalColor = Color.darkGray;
    [SerializeField] private Ease colorEase = Ease.InOutSine;
    [SerializeField] private Ease parentPositionEase = Ease.OutCirc;
    [SerializeField] private Ease localPositionAndScaleEase = Ease.InCubic;
    [SerializeField] private float highlightDuration = 0.5f;
    [SerializeField] private float gapBetweenCards = 0.5f;
    [SerializeField] private int highlightScaleMultiplier = 2; // 200% size to maintain pixel perfect look
    [Tooltip("Number of cards to the right/left of the highlighted card to adjust position for")]
    [SerializeField] private int bufferCardsToAdjust = 4;
    [SerializeField] private float cardScale = 5f;
    [SerializeField] private float cardHeight = 5 * 3/2; // 5 is card scale, 3/2 is aspect ratio
    [SerializeField] private float cardWidth = 5;
    [SerializeField] private int highlightedCardLevel = 1;
    [SerializeField] private int cardToHighlight = 1;
    
    
    public override void Execute(Clickable2D source)
    {
        Transform card = source.transform;
        Transform parent = card.parent;

        // move parent to center the target card
        float targetParentX = -(cardToHighlight - 1) * (cardWidth + gapBetweenCards);
        Tween.LocalPositionX(parent, targetParentX, highlightDuration, parentPositionEase);

    // HIGHLIGHTING CARD
        HighlightableElement2D highlightable = card.GetComponent<HighlightableElement2D>();
        highlightable.PreHighlightScaleCached = false; // Clear cached scale to prevent controller interference
        highlightable.enabled = false; // Disable to avoid scaling conflicts

        var enterButton = GetLevelEnterButtonHighlightable(card); // Enable level entering button
        enterButton.enabled = true;
        
        Tween.Color(card.GetComponent<SpriteRenderer>(), highlightedColor, highlightDuration, colorEase);
        Tween.LocalPositionX(card, (cardToHighlight - 1) * (cardWidth + gapBetweenCards), highlightDuration, localPositionAndScaleEase); // Reset X position
        Tween.LocalPositionY(card, 0, highlightDuration, localPositionAndScaleEase); // Center Y for bottom alignment at 2x scale
        Tween.Scale(card, cardScale * highlightScaleMultiplier * Vector3.one, highlightDuration, localPositionAndScaleEase).OnComplete(() =>
        {
            // mark the card as highlighted
            highlightedCardLevel = cardToHighlight;
        });

    // UNHIGHLIGHTING PREVIOUS CARD
        if (highlightedCardLevel != -1)
        {
            card = parent.GetChild(highlightedCardLevel - 1);
            HighlightableElement2D prevHighlightable = card.GetComponent<HighlightableElement2D>();
            prevHighlightable.PreHighlightScaleCached = false; // Clear cached scale

            enterButton = GetLevelEnterButtonHighlightable(card); // Disable level entering button
            enterButton.enabled = false;
            
            Tween.Color(card.GetComponent<SpriteRenderer>(), normalColor, highlightDuration, colorEase);
            Tween.LocalPositionY(card, -cardHeight / 2, highlightDuration, localPositionAndScaleEase); // Reset Y for bottom alignment at normal scale
            Tween.Scale(card, cardScale * Vector3.one, highlightDuration, localPositionAndScaleEase).OnComplete(() =>
            {
                // Re-enable HighlightableElement2D on the previously highlighted card
                prevHighlightable.enabled = true;
            });
        }

    // ADJUSTING OTHER CARDS
        float offset = (highlightScaleMultiplier - 1) * cardWidth / 2;

    // ADJUSTING CARDS TO THE RIGHT OF HIGHLIGHTED CARD
        for (int i = cardToHighlight + 1; i <= parent.childCount && i <= cardToHighlight + bufferCardsToAdjust; ++i)
        {
            card = parent.GetChild(i - 1);

            float typicalPosition = (i - 1) * (cardWidth + gapBetweenCards);
            float offsetedPosition = typicalPosition + offset; // shift cards right

            if (offsetedPosition != card.localPosition.x)
                Tween.LocalPositionX(card, offsetedPosition, highlightDuration, localPositionAndScaleEase);
        }

    // ADJUSTING CARDS TO THE LEFT OF HIGHLIGHTED CARD
        for (int i = cardToHighlight - 1; i >= 1 && i >= cardToHighlight - bufferCardsToAdjust; --i)
        {
            card = parent.GetChild(i - 1);

            float typicalPosition = (i - 1) * (cardWidth + gapBetweenCards);
            float offsetedPosition = typicalPosition - offset; // shift cards left

            if (offsetedPosition != card.localPosition.x)
                Tween.LocalPositionX(card, offsetedPosition, highlightDuration, localPositionAndScaleEase);
        }

        static HighlightableElement2D GetLevelEnterButtonHighlightable(Transform card)
        {
            var ret = card.GetChild(0).GetComponent<HighlightableElement2D>(); // Ensure button is active
            if (ret == null)
            {
                Debug.LogError("Level enter button highlightable not found! Make sure it is the first child of the card.");
            }
            return ret;
        }
    }

    public override bool CanExecute(Clickable2D source)
    {
        int cardLevel = GetCardLevel();

        // TODO false if card level is higher than highest unlocked level

        // Don't allow highlighting the same card or if a transition is in progress
        if (cardLevel == highlightedCardLevel)
        {
            Debug.LogWarning($"Card level {cardLevel} is already highlighted.");
            return false;
        }
        
        if (cardToHighlight != highlightedCardLevel)
        {
            Debug.LogWarning($"Another highlight transition is in progress (going to level {cardToHighlight}).");
            return false;
        }
        
        Debug.Log($"Highlighting level selection card level {cardLevel}");
        // store highlighted card level
        cardToHighlight = cardLevel;
        return true;

        int GetCardLevel()
        {
            // get last two characters and convert to int
            string name = source.gameObject.name;
            string levelString = name[^2..];
            if (int.TryParse(levelString, out int level))
            {
                return level;
            }

            if (int.TryParse(name[^1..], out level))
            {
                return level;
            }

            return -1; // invalid level
        }
    }
}
