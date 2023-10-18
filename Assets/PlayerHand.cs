using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public List<GameObject> hand;
    public GameObject cardPrefab;
    public CardInHand selectedCard;
    public static PlayerHand Instance;
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
        cards.Add(new BaseCard("Woah", "Description1", 2, 2, 2, "beast"));
        cards.Add(new BaseCard("Yeah", "Description1", 3, 3, 3, "humanoid"));
        cards.Add(new BaseCard("Ohno", "Description1", 4, 4, 4, "wug"));

        if (SingleCharacter.Instance != null) {
            cards = SingleCharacter.Instance.cards;
        }

        int cardCount = 0;
        foreach (BaseCard card in cards) {
            cardCount++;
            //instantiate a new cardinhand
            GameObject cardInHand = Instantiate(cardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            Transform cardTransform = cardInHand.GetComponent<Transform>();
            cardTransform.position += Vector3.back * 20 + Vector3.back * cardCount * 7; //to stop overlapping
            cardTransform.position += Vector3.down * 345; 
            cardTransform.position += Vector3.left * 400;
            cardTransform.position += Vector3.right * 115 * (cardCount - 1); 
            cardTransform.rotation = Quaternion.AngleAxis(7 - cardCount, Vector3.forward);
            cardTransform.localScale = new Vector3(0.4f,0.4f,0.4f);
            cardInHand.GetComponent<CardInHand>().initialise(card, this);
            cardInHand.transform.SetParent(gameObject.transform, false);

            //set the cardinhand as a child object
        }
    }

    public void setSelectedCard(CardInHand card) {
        this.selectedCard = card;
    }
    
    public void placeCreature(int team, int position) {
        BattleController.instance.teams[team].placeCreature(position, this.selectedCard.baseCard);
        Destroy(this.selectedCard.gameObject);
        this.selectedCard = null;
    }
}
