using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public List<GameObject> hand;
    public GameObject cardPrefab;
    public CardInHand selectedCard;
    public static PlayerHand Instance;
    public bool hasHoveredCard = false;
    public List<CardInHand> cardsInHand = new List<CardInHand>(); 
    // Start is called before the first frame update
    void Start()
    {
        if (Instance != null && Instance != this) {
            Destroy(this);
        }
        else {
            Instance = this;
        }
        spawnHand();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void spawnHand() {
        List<BaseCard> cards = new List<BaseCard>();
        if (SingleCharacter.Instance != null) {
            cards = SingleCharacter.Instance.cards;
        } else {
            cards.Add(new BaseCard("Woah This is a long name", "Description1", 2, 2, 2, "beast"));
            cards.Add(new BaseCard("Yeah", "Description1", 3, 3, 3, "humanoid"));
            cards.Add(new BaseCard("Ohno", "Description1", 4, 4, 4, "bird"));
            cards.Add(new BaseCard("Wahoo", "wahoo", 4, 4, 4, "aquatic"));
            cards.Add(new BaseCard("WOOGH", "Description1", 1, 5, 2, "ghost"));
            cards.Add(new BaseCard("Beepo", "Description1", 3, 1, 3, "robot"));
            cards.Add(new BaseCard("Beepo", "I exist to die and existence is pain. Please ease my suffering now.", 3, 1, 3, "robot"));        
        
        }
        int cardCount = 0;
        foreach (BaseCard card in cards) {
            cardCount++;
            //instantiate a new cardinhand and position it
            GameObject cardInHand = Instantiate(cardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            Transform cardTransform = cardInHand.GetComponent<Transform>();
            cardTransform.position += Vector3.back * 20 + Vector3.back * cardCount * 7; //to stop overlapping
            cardTransform.position += Vector3.down * 345; 
            cardTransform.position += Vector3.left * 400;
            cardTransform.position += Vector3.right * 110 * (cardCount - 1); 
            cardTransform.rotation = Quaternion.AngleAxis(7 - cardCount, Vector3.forward);
            cardTransform.localScale = new Vector3(0.4f,0.4f,0.4f);
            cardInHand.GetComponent<CardInHand>().initialise(card, this);

            //set display layer
            CardInHand cardObj = cardInHand.GetComponent<CardInHand>();
            cardObj.attackIcon.sortingOrder = 5 + cardCount;
            cardObj.healthIcon.sortingOrder = 5 + cardCount;
            cardObj.speedIcon.sortingOrder = 5 + cardCount;
            cardObj.cardMask.sortingOrder = 5 + cardCount;
            cardObj.cardBack.sortingOrder = 5 + cardCount;
            cardObj.cardNameText.sortingOrder = 5 + cardCount;
            cardObj.cardSpeedValueText.sortingOrder = 5 + cardCount;
            cardObj.cardAttackValueText.sortingOrder = 5 + cardCount;
            cardObj.cardHealthValueText.sortingOrder = 5 + cardCount;
            cardObj.creatureSpriteRenderer.sortingOrder= 5 + cardCount;

            cardInHand.transform.SetParent(gameObject.transform, false);
            cardsInHand.Add(cardInHand.GetComponent<CardInHand>());
        }
    }

    public void setSelectedCard(CardInHand card) {
        this.selectedCard = card;
    }
    
    public void placeCreature(int team, int position) {
        if (BattleController.instance.teams[team].placedCreatures[position] != null) {
            InfoPanelController.instance.setErrorView("Someone's already in that spot!!");
            return;
        }
        BattleController.instance.teams[team].placeCreature(position, this.selectedCard.baseCard);
        int cardIndex = cardsInHand.FindIndex(a => a.Equals(selectedCard));
        for (int i = cardIndex + 1; i < cardsInHand.Count; i++) {
            cardsInHand[i].gameObject.transform.position += Vector3.left * 110;
        }
        cardsInHand.Remove(selectedCard);
        Destroy(this.selectedCard.gameObject);
        this.selectedCard = null;
    }
}
