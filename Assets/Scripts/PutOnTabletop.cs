using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class PutOnTabletop : MonoBehaviour
{

    private Camera firstPersonCamera;
    // private Camera UICamera;
    private GameObject gameController;
    private clearSnackbar Snackbar;
    private Image myImage;
    private Color initColor;
    private bool hoveringOver = false;
    public GameObject panelPrefab;
    private GameObject hitCard = null;
    private List<GameObject> cardsOnTabletop;
    private bool jokerSwapped = false;
    private NetworkGameController networkGameController;
    private string toSendToServer = "";
    private bool isHostTabletop = false;
    private int openingSum = 0;
    private static int sumToOpen = 51;
    private List<GameObject> cardsPlayerOpenedWith;
    private bool tookCardPutDown = false;
    private string cardOnTableFromDiscard = null;
    private GameObject hand;


    // Start is called before the first frame update
    void Start()
    {
        firstPersonCamera = Camera.main;
        // UICamera = GameObject.Find("UICamera").GetComponent<Camera>();
        gameController = GameObject.Find("GameController");
        Snackbar = GameObject.FindObjectOfType<clearSnackbar>();
        myImage = this.transform.GetComponent<Image>();
        initColor = myImage.color;
        cardsOnTabletop = new List<GameObject>();
        networkGameController = GameObject.FindObjectOfType<NetworkGameController>();
        cardsPlayerOpenedWith = new List<GameObject>();
        if (this.name == "PlayerTabletopPanel") isHostTabletop = true;
        hand = GameObject.Find("Hand");
    }

    // Update is called once per frame
    void Update()
    {
        if (gameController == null || gameController.GetComponent<SelectAndDrag>() == null) return;
        if (!hoveringOver && myImage.color != initColor) myImage.color = initColor;

        if (Input.touchCount < 1) return;

        Touch touch = Input.GetTouch(0);

        RaycastHit hitobject;
        Ray ray = firstPersonCamera.ScreenPointToRay(touch.position);
        int layerMask = 1 << 5; // UI layer only

        if (gameController.GetComponent<SelectAndDrag>().draggingCards && Physics.Raycast(ray, out hitobject, Mathf.Infinity, ~layerMask) && !gameController.GetComponent<SelectAndDrag>().arrangingCardsInHand)
        {
            //---------------------------------------------------------PUTTING DOWN NEW CARDS--------------------------------------------
            // Check if what is hit is the desired object
            if (hitobject.transform == this.transform)
            {
                if (!hoveringOver) hoveringOver = true;
                if (gameController.GetComponent<SelectAndDrag>().selectedCards.Count >= 1 && !gameController.GetComponent<GameController>().canDrawCard
           && networkGameController.myTurn == networkGameController.playerTurn && networkGameController.gameStarted
           &&  hand.transform.childCount>0) //&& !gameController.GetComponent<GameController>().mustClose
                {
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        if (checkCardsToPut(gameController.GetComponent<SelectAndDrag>().selectedCards) && isHostTabletop == networkGameController.isHost)
                        {
                            myImage.color = new Color32(30, 170, 50, 40); // green
                        }
                        else
                        {
                            myImage.color = new Color32(230, 30, 17, 40); //red
                        }
                    }
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        if (checkCardsToPut(gameController.GetComponent<SelectAndDrag>().selectedCards, true, false, true) && isHostTabletop == networkGameController.isHost)
                        {
                            int i = 0;
                            foreach (GameObject card in gameController.GetComponent<SelectAndDrag>().selectedCards)
                            {
                                toSendToServer += card.name + (i == gameController.GetComponent<SelectAndDrag>().selectedCards.Count - 1 ? "" : " ");
                                i++;
                            }
                            networkGameController.CmdAddCardsToTabletop(toSendToServer, isHostTabletop);
                            toSendToServer = "";
                            gameController.GetComponent<GameController>().removedCardsFromHand(gameController.GetComponent<SelectAndDrag>().selectedCards.Count);
                            //destroy the objects
                            GameObject tookCard = GameObject.FindObjectOfType<discardCard>().getTookCard();
                            foreach (GameObject card in gameController.GetComponent<SelectAndDrag>().selectedCards)
                            {
                                if (card != tookCard)
                                {
                                    Destroy(card);
                                }
                                else
                                {
                                    tookCardPutDown = true;
                                    cardOnTableFromDiscard = card.name;
                                    card.SetActive(false);
                                }
                            }
                            gameController.GetComponent<SelectAndDrag>().selectedCards.Clear();
                        }
                        hoveringOver = false;

                    }
                }
                else
                {
                    myImage.color = new Color32(230, 30, 17, 40); //red
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        if (gameController.GetComponent<GameController>().canDrawCard) Snackbar.setText("You have to draw a card first!");
                        if (networkGameController.myTurn != networkGameController.playerTurn || !networkGameController.gameStarted)
                            Snackbar.setText("It's not your turn yet!");
                        if ( hand.transform.childCount == 0) Snackbar.setText("You must throw the last card, you can't add it to the table!");//gameController.GetComponent<GameController>().mustClose ||
                    }
                }
            }
            else
            {
                if (hoveringOver) hoveringOver = false;
            }
            //---------------------------------------------------------ADDING CARDS TO EXISTING STACKS--------------------------------------------
            if (hitobject.transform.tag == "Card" && hitobject.transform.parent.parent == this.transform)
            {
                //update the color for previous card
                if (hitCard != hitobject.transform.gameObject && hitCard != null)
                {
                    hitCard.GetComponent<Renderer>().material.color = Color.white;
                    hitCard = null;
                }
                if (!gameController.GetComponent<GameController>().canDrawCard && networkGameController.myTurn == networkGameController.playerTurn 
                    && networkGameController.gameStarted  && hand.transform.childCount > 0)//&& !gameController.GetComponent<GameController>().mustClose
                {
                    //hit a joker - make a switch
                    jokerSwapped = false;
                    if (hitobject.transform.name == "J" && gameController.GetComponent<SelectAndDrag>().selectedCards.Count == 1)
                    {
                        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                        {
                            if (checkJokerChange(hitobject) && (gameController.GetComponent<GameController>().playerOpened || (isHostTabletop == networkGameController.isHost))) // swap the joker with selected card
                            {
                                hitobject.transform.GetComponent<Renderer>().material.color = new Color32(8, 109, 18, 150); // green
                                jokerSwapped = true;
                            }
                            else
                            {
                                hitobject.transform.GetComponent<Renderer>().material.color = new Color32(230, 30, 17, 150); // red
                                
                            }
                            hitCard = hitobject.transform.gameObject;
                        }
                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            if (checkJokerChange(hitobject) && (gameController.GetComponent<GameController>().playerOpened || (isHostTabletop == networkGameController.isHost))) // swap the joker with selected card
                            {
                                networkGameController.CmdChangeJokerOnTabletop(gameController.GetComponent<SelectAndDrag>().selectedCards[0].transform.name, isHostTabletop, hitobject.transform.parent.GetSiblingIndex(), hitobject.transform.GetSiblingIndex());
                                gameController.GetComponent<SelectAndDrag>().selectedCards[0].transform.GetComponent<UpdateCard>().changeCardFace(gameController.transform.GetComponent<GameController>().jokerCardFace);
                                //hitobject.transform.GetComponent<UpdateCard>().changeCardFace(gameController.GetComponent<SelectAndDrag>().selectedCards[0].transform.GetComponent<UpdateCard>().getCardFace());
                                //hitobject.transform.name = gameController.GetComponent<SelectAndDrag>().selectedCards[0].transform.name;
                                gameController.GetComponent<SelectAndDrag>().selectedCards[0].transform.name = "J";
                                jokerSwapped = true;
                            } else
                            {
                                if (!gameController.GetComponent<GameController>().playerOpened) Snackbar.setText("You can't add to table if you're not opened!");
                            }
                        }
                    }
                    //hit some other card on tabletop

                    //hit the first or the last card
                    if ((0 == hitobject.transform.GetSiblingIndex() || hitobject.transform.parent.childCount - 1 == hitobject.transform.GetSiblingIndex()) && !jokerSwapped)
                    {
                        bool inFront = (0 == hitobject.transform.GetSiblingIndex());
                        int i = 0;
                        //make a list of current cards on that panel
                        cardsOnTabletop.Clear();
                        foreach (Transform t in hitobject.transform.parent)
                        {
                            cardsOnTabletop.Add(t.gameObject);
                        }
                        //add the cards user is trying to add to panel
                        foreach (GameObject card in gameController.GetComponent<SelectAndDrag>().selectedCards)
                        {
                            if (inFront)
                            {
                                cardsOnTabletop.Insert(i, card);
                                i++;
                            }
                            else
                            {
                                cardsOnTabletop.Add(card);
                            }

                        }
                        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                        {
                            if (checkCardsToPut(cardsOnTabletop, false) && (gameController.GetComponent<GameController>().playerOpened || (isHostTabletop == networkGameController.isHost)))
                            {
                                hitobject.transform.GetComponent<Renderer>().material.color = new Color32(8, 109, 18, 150);
                            }
                            else
                            {
                                hitobject.transform.GetComponent<Renderer>().material.color = new Color32(230, 30, 17, 150);
                                
                            }
                            hitCard = hitobject.transform.gameObject;
                        }
                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            hitobject.transform.GetComponent<Renderer>().material.color = Color.white;
                            if (checkCardsToPut(cardsOnTabletop, false, false, true) && (gameController.GetComponent<GameController>().playerOpened || (isHostTabletop == networkGameController.isHost)))
                            {
                                i = 0;
                                toSendToServer = "";
                                foreach (GameObject card in gameController.GetComponent<SelectAndDrag>().selectedCards)
                                {
                                    toSendToServer += card.name + (i == gameController.GetComponent<SelectAndDrag>().selectedCards.Count - 1 ? "" : " ");
                                    i++;
                                }
                                networkGameController.CmdAddCardsToCardOnTabletop(toSendToServer, inFront, isHostTabletop, hitobject.transform.parent.GetSiblingIndex(), hitobject.transform.GetSiblingIndex());
                                toSendToServer = "";
                                //addCardsToExistingCardOnTable();
                                gameController.GetComponent<GameController>().removedCardsFromHand(gameController.GetComponent<SelectAndDrag>().selectedCards.Count);
                                //destroy the objects
                                GameObject tookCard = GameObject.FindObjectOfType<discardCard>().getTookCard();
                                foreach (GameObject card in gameController.GetComponent<SelectAndDrag>().selectedCards)
                                {
                                    if (card != tookCard)
                                    {
                                        Destroy(card);
                                    }
                                    else
                                    {
                                        tookCardPutDown = true;
                                        cardOnTableFromDiscard = card.name;
                                        card.SetActive(false);
                                    }
                                }
                                gameController.GetComponent<SelectAndDrag>().selectedCards.Clear();
                            } else
                            {
                                if (!gameController.GetComponent<GameController>().playerOpened) Snackbar.setText("You can't add to table if you're not opened!");
                            }
                        }
                    }
                }
                else
                {
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        if (hand.transform.childCount == 0) Snackbar.setText("You must throw the last card, you can't add it to the table!");//gameController.GetComponent<GameController>().mustClose || 
                        else if (gameController.GetComponent<GameController>().canDrawCard) Snackbar.setText("You must draw first!");
                        else Snackbar.setText("It's not your turn to play yet!");
                    }
                }
            }
            else
            {
                if (hitCard != null)
                {
                    hitCard.GetComponent<Renderer>().material.color = Color.white;
                    hitCard = null;
                }
            }
        }
        else
        {
            if (hoveringOver) hoveringOver = false;
            if (hitCard != null)
            {
                hitCard.GetComponent<Renderer>().material.color = Color.white;
                hitCard = null;
            }
        }


    }

    public void PutCardsOnTabletop(List<GameObject> cards)
    {
        float scale = 7f;
        GameObject newPanel = Instantiate(panelPrefab, this.transform);

        //set the value for each card
        foreach (GameObject card in cards)
        {
            card.GetComponent<UpdateCard>().setCardValueBasedOnName();
        }
        //fix joker values
        checkCardsToPut(cards);

        foreach (GameObject card in cards)
        {
            addToSumIfPlayerIsOpening(card, cards);
            //remove the card from dragging panel and add to table
            card.layer = 0;
            card.transform.SetParent(newPanel.transform, false);
            card.GetComponent<BoxCollider>().enabled = true;
            //card.GetComponent<UpdateCard>().enabled = false;/////////////////////////////////////////////////////////////////
            card.transform.localScale = Vector3.one * scale;
        }


        if (newPanel.transform.childCount > 5)
        {
            newPanel.GetComponent<HorizontalLayoutGroup>().spacing = -245;
        }
        gameController.GetComponent<GameController>().arrangeCardsOnPanel(newPanel.transform, -scale / 2, scale / 2);

    }


    bool checkCardsToPut(List<GameObject> cards, bool initialPut = true, bool changingJoker = false, bool write = false)
    {


        //must be atleast 3 cards in stack
        if (cards.Count < 3)
        {
            // debugic.text = "You must put at least 3 cards";////////////////////////////////////
            return false;
        }

        //mustnt have more than 1 joker in stack
        int jokerNum = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].GetComponent<UpdateCard>().suit == "J")
            {
                jokerNum++;
                if (jokerNum > 1 && initialPut)
                {
                    if (write) Snackbar.setText("You can't put more than 1 joker in one group!");
                    return false;
                }
            }
        }

        //check different suits same number stack
        int number = 0;
        bool sameNumberStack = true;
        bool spade = false; bool heart = false; bool diamond = false; bool club = false;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].GetComponent<UpdateCard>().suit == "J")
            {
                if (number != 0) cards[i].GetComponent<UpdateCard>().value = number;
                else cards[i].GetComponent<UpdateCard>().value = cards[i + 1].GetComponent<UpdateCard>().value;
                if (cards[i].GetComponent<UpdateCard>().value == 1) cards[i].GetComponent<UpdateCard>().value = 10;
                continue;
            }
            if (number == 0)
            {
                number = cards[i].GetComponent<UpdateCard>().value;
            }
            else
            {
                if (cards[i].GetComponent<UpdateCard>().value != number)
                {
                    sameNumberStack = false;
                    break;
                }
            }
        }
        // if same number stack - check the suits
        if (sameNumberStack)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].GetComponent<UpdateCard>().suit == "J") continue;
                switch (cards[i].GetComponent<UpdateCard>().suit)
                {
                    case "C":
                        if (club) { if (write) Snackbar.setText("You can't have more than 1 of each suit in same-value group!"); return false; }
                        club = true; break;
                    case "D":
                        if (diamond) { if (write) Snackbar.setText("You can't have more than 1 of each suit in same-value group!"); return false; }
                        diamond = true; break;
                    case "H":
                        if (heart) { if (write) Snackbar.setText("You can't have more than 1 of each suit in same-value group!"); return false; }
                        heart = true; break;
                    case "S":
                        if (spade) { if (write) Snackbar.setText("You can't have more than 1 of each suit in same-value group!"); return false; }
                        spade = true; break;
                    default: return false;
                }
            }
            if (cards.Count > 4) { if (write) Snackbar.setText("You can't have more than 1 card of each suit in same-value group!"); return false; }
            else
                if (changingJoker && cards.Count == 3) { return false; }// you cant take the joker if all three suits are not in stack 
            else return true;
        }


        //check descending numbers
        bool descending = true;
        bool ace = false;
        bool king = false;
        int offset = 1;
        int j = 1; // start comparing from second card with the one before, but if the first card is joker, then start from third
        if (cards[0].GetComponent<UpdateCard>().suit == "J")
        {
            j++;
            //calculate joker's value
            cards[0].GetComponent<UpdateCard>().value = cards[1].GetComponent<UpdateCard>().value + 1;
        }
        for (; j < cards.Count; j++)
        {
            if (cards[j].GetComponent<UpdateCard>().suit == "J")
            {
                //offset = 2;
                if (cards[j - 1].GetComponent<UpdateCard>().value != 1) cards[j].GetComponent<UpdateCard>().value = cards[j - 1].GetComponent<UpdateCard>().value - 1;
                else cards[j].GetComponent<UpdateCard>().value = 13;
                // continue;
            }
            king = (cards[j].GetComponent<UpdateCard>().value == 13); // current card is a king
            if (cards[j].GetComponent<UpdateCard>().value != cards[j - offset].GetComponent<UpdateCard>().value - offset && !king) descending = false;
            if (king)
            {
                // before the king must be an ace or a joker, and as the first card in stack (stack 2AK is not possible)
                if (cards[j - 1].GetComponent<UpdateCard>().value != 1 && cards[j - 1].GetComponent<UpdateCard>().value != 14) descending = false;// && cards[j - 1].GetComponent<UpdateCard>().suit != "J"
                else if (j - 1 != 0) descending = false;
            }
            offset = 1;
        }
        bool ascending = true;
        if (!descending)
        {
            //check ascending numbers   
            offset = 1;
            j = 1;
            if (cards[0].GetComponent<UpdateCard>().suit == "J")
            {
                j++;
                //calculate joker's value
                cards[0].GetComponent<UpdateCard>().value = cards[1].GetComponent<UpdateCard>().value - 1;
            }
            for (; j < cards.Count; j++)
            {
                if (cards[j].GetComponent<UpdateCard>().suit == "J")
                {
                    //offset = 2;
                    if (cards[j - 1].GetComponent<UpdateCard>().value != 13) cards[j].GetComponent<UpdateCard>().value = cards[j - 1].GetComponent<UpdateCard>().value + 1;
                    else cards[j].GetComponent<UpdateCard>().value = 14;
                    //continue; 
                }
                ace = (cards[j].GetComponent<UpdateCard>().value == 1);
                if (cards[j].GetComponent<UpdateCard>().value != cards[j - offset].GetComponent<UpdateCard>().value + offset && !ace) ascending = false;
                if (ace)
                {
                    //before the ace must a king, and ace must be last in stack (KA2 is not possible)
                    if (cards[j - 1].GetComponent<UpdateCard>().value != 13) ascending = false;// && cards[j - 1].GetComponent<UpdateCard>().suit != "J"
                    else if (j != cards.Count - 1) ascending = false;
                }
                offset = 1;
            }
        }
        if (!ascending && !descending)
        {
            if (write) Snackbar.setText("Cards in a group must be of the same value, or in ascending or descending order!");
            return false;
        }

        //check same suit if descending or ascending
        spade = false; diamond = false; heart = false; club = false;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].GetComponent<UpdateCard>().suit == "J") continue;
            switch (cards[i].GetComponent<UpdateCard>().suit)
            {
                case "C":
                    club = true; break;
                case "D":
                    diamond = true; break;
                case "H":
                    heart = true; break;
                case "S":
                    spade = true; break;
                default: return false;
            }
        }
        if ((heart ^ spade ^ club ^ diamond) && !(heart && spade) && !(club && diamond)) // xor is also true if three flags are true and one is false
        {
            return true;
        }
        else
        {
            if (write) Snackbar.setText("Cards in descending or ascending order must be of the same suit!");
            return false;
        }
    }

    bool checkJokerChange(RaycastHit joker)
    {
        //function is called only when user is dragging only one card

        // if joker is hit with another joker
        if (gameController.GetComponent<SelectAndDrag>().selectedCards[0].GetComponent<UpdateCard>().suit == "J") return false;

        // check if joker can be swapped with the card
        cardsOnTabletop.Clear();
        foreach (Transform t in joker.transform.parent)
        {
            if (t != joker.transform) cardsOnTabletop.Add(t.gameObject);
            else cardsOnTabletop.Add(gameController.GetComponent<SelectAndDrag>().selectedCards[0]);
        }
        return checkCardsToPut(cardsOnTabletop, false, true);
    }

    public void addCardsToExistingCardOnTable(List<GameObject> cards, GameObject hitobject, bool inFront)
    {
        //set the value for each card
        foreach (GameObject card in cards)
        {
            card.GetComponent<UpdateCard>().setCardValueBasedOnName();
        }
        //fix joker values
        checkCardsToPut(cards);

        //add the card to tabletop
        float scale = 7f;
        int i = 0;
        foreach (GameObject card in cards)
        {
            addToSumIfPlayerIsOpening(card, cards);

            //remove the card from dragging panel and put on table
            card.layer = 0;
            card.transform.SetParent(hitobject.transform.parent, false);
            card.GetComponent<BoxCollider>().enabled = true;
            //card.GetComponent<UpdateCard>().enabled = false;//////////////////////////////////////
            card.transform.localScale = Vector3.one * scale;
            if (inFront) card.transform.SetSiblingIndex(i);
            i++;
        }

        if (hitobject.transform.parent.childCount > 5)
        {
            hitobject.transform.parent.GetComponent<HorizontalLayoutGroup>().spacing = -245;
        }
        gameController.GetComponent<GameController>().arrangeCardsOnPanel(hitobject.transform.parent, -scale / 2, scale / 2);
    }

    public void removeAllCardsFromPanel()
    {
        // debugic.text = "removing all cards from " + (networkGameController.isHost == isHostTabletop ? "my panel" : "other player's panel");
        foreach (Transform childPanel in this.transform)
        {
            Destroy(childPanel.gameObject);
        }
    }

    public void returnOpenedCardsToHand()
    {
        GameObject hand = GameObject.Find("Hand");
        gameController.GetComponent<GameController>().playerTriedToOpen = false;
        Snackbar.setText("You had " + openingSum + " in total. That's not enough for opening!");
        openingSum = 0;
        foreach (GameObject card in cardsPlayerOpenedWith)
        {
            gameController.GetComponent<GameController>().removedCardsFromHand(-1);
            if (tookCardPutDown && cardOnTableFromDiscard == card.name)
            {
                tookCardPutDown = false;
                Destroy(card);
            } else
            {
                card.transform.SetParent(hand.transform, false);
                card.layer = 5;
                Vector3 temp = Vector3.one;
                temp.x = 2;
                temp.y = 2;
                card.GetComponent<UpdateCard>().originalScale = temp;
                card.GetComponent<UpdateCard>().DeselectCard();
            }
        }
        gameController.GetComponent<GameController>().arrangeCardsOnPanel(hand.transform);
        cardsPlayerOpenedWith.Clear();
    }

    private void addToSumIfPlayerIsOpening(GameObject card, List<GameObject> cards)
    {

        int cardValue = 0;
        //player is opening
        if (isHostTabletop == networkGameController.isHost && !gameController.GetComponent<GameController>().playerOpened)
        {
            if (!gameController.GetComponent<GameController>().playerTriedToOpen) gameController.GetComponent<GameController>().playerTriedToOpen = true;
            //determine card value for ace
            if (card.GetComponent<UpdateCard>().suit != "J" && card.GetComponent<UpdateCard>().value == 1)
            {
                //if ace is the first card in stack
                if (cards.IndexOf(card) == 0)
                {
                    switch (cards[1].GetComponent<UpdateCard>().value)
                    {
                        case 1: case 13: case 10: cardValue = 10; break;
                        case 2: cardValue = 1; break;
                        default: cardValue = 1; break;
                    }
                }
                //ace is not the first card in stack
                else
                {
                    switch (cards[cards.IndexOf(card) - 1].GetComponent<UpdateCard>().value)
                    {
                        case 1: case 10: case 13: cardValue = 10; break;
                        case 2: cardValue = 1; break;
                        default: cardValue = 1; break;
                    }
                }
            }
            //determine card value for any other card
            else
            {
                cardValue = card.GetComponent<UpdateCard>().value > 10 ? 10 : card.GetComponent<UpdateCard>().value;
            }
            openingSum += cardValue;
            cardsPlayerOpenedWith.Add(card);
            if (openingSum >= sumToOpen)
            {
                gameController.GetComponent<GameController>().playerTriedToOpen = false;
                gameController.GetComponent<GameController>().playerOpened = true;
                cardsPlayerOpenedWith.Clear();
                openingSum = 0;
                tookCardPutDown = false;

            }
            
        }
    }

}
