using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "HighlightLevelSelectionCardNumber_", menuName = "Actions/Single-Use/Highlight Level Selection Card")]
public class HighlightLevelSelectionCard : ActionSO
{
    [SerializeField] private Color highlightedColor = Color.gray;
    [SerializeField] private Color normalColor = Color.darkGray;
    [SerializeField] private Ease colorEase = Ease.InOutSine;
    [SerializeField] private Ease parentPositionEase = Ease.OutCirc;
    [SerializeField] private Ease scaleEase = Ease.InCubic;
    [SerializeField] private float highlightDuration = 0.5f;
    [SerializeField] private float gapBetweenCards = 0.5f;
    [SerializeField] private int highlightScaleMultiplier = 2; // 200% size to maintain pixel perfect look
    [SerializeField] private string cardNamePrefix = "LevelSelectionCard_";
    [SerializeField] private float cardScale = 5f;
    [SerializeField] private float cardHeight = 5 * 3/2; // 5 is card scale, 3/2 is aspect ratio
    [SerializeField] private float cardWidth = 5;
    private int highlightedCardLevel = 1;
    private int cardToHighlight;
    
    
    public override void Execute(Clickable2D source)
    {
        int sign = cardToHighlight > highlightedCardLevel ? -1 : 1; // if bigger, move left
        Transform card = source.transform;
        Transform parent = card.parent;

        // move parent to center the target card
        float targetParentX = -(cardToHighlight - 1) * (cardWidth + gapBetweenCards);
        Tween.LocalPositionX(parent, targetParentX, highlightDuration, parentPositionEase).OnComplete(() =>
        {
            // set card scale to highlight scale when the transition is complete
            highlightedCardLevel = cardToHighlight;
        });

        // highlight the source card
        Tween.Scale(card, cardScale * highlightScaleMultiplier * Vector3.one, highlightDuration, scaleEase);
        Tween.Color(card.GetComponent<SpriteRenderer>(), highlightedColor, highlightDuration, colorEase);
        // Cards maintain fixed grid X positions
        float cardGridX = (cardToHighlight - 1) * (cardWidth + gapBetweenCards);
        Tween.LocalPositionX(card, cardGridX, highlightDuration, scaleEase);
        Tween.LocalPositionY(card, 0, highlightDuration, scaleEase); // Center Y for bottom alignment at 2x scale

        // get all cards between current highlighted and to be highlighted
        Transform[] otherCards = GetCardsFromCurrentToPreviousHighlighted();

        // dehighlight previous card
        Tween.Scale(otherCards[^1], cardScale * Vector3.one, highlightDuration, scaleEase);
        Tween.Color(otherCards[^1].GetComponent<SpriteRenderer>(), normalColor, highlightDuration, colorEase);
        // Return to fixed grid position
        float prevCardGridX = (highlightedCardLevel - 1) * (cardWidth + gapBetweenCards);
        Tween.LocalPositionX(otherCards[^1], prevCardGridX, highlightDuration, parentPositionEase);
        Tween.LocalPositionY(otherCards[^1], -cardHeight/2, highlightDuration, scaleEase); // Bottom aligned at 1x scale

        // move other cards in between to their grid positions
        for (int i = otherCards.Length - 2; i >= 0; --i)
        {
            Transform otherCard = otherCards[i];
            // Get the card's level from its sibling index and ensure it's at its grid position
            int cardLevel = otherCard.GetSiblingIndex() + 1;
            float targetX = (cardLevel - 1) * (cardWidth + gapBetweenCards);
            Tween.LocalPositionX(otherCard, targetX, highlightDuration, scaleEase);
        }

        /// <summary>
        /// Get all cards from current highlighted to target (inclusive of current, exclusive of target)
        /// </summary>
        Transform[] GetCardsFromCurrentToPreviousHighlighted()
        {
            Transform[] cards = new Transform[Mathf.Abs(cardToHighlight - highlightedCardLevel)];
            int startIndex = highlightedCardLevel - 1; // Start from currently highlighted card
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = parent.GetChild(startIndex + sign * i);
            }
            return cards;
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
        
        if (cardToHighlight != highlightedCardLevel && cardToHighlight != 0)
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

            return -1; // invalid level
        }
    }
}
