using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class discardCard : MonoBehaviour
{

    private Camera firstPersonCamera;
    private GameObject gameController;
    private clearSnackbar snackbar;
    private Material myMat;
    private float discardedCardOffset = 0.05f;
    private Color initColor;
    private bool hoveringOver = false;
    private NetworkGameController networkGameController;
    public bool canDiscard = false;
    private GameObject tookCard = null;

    // Start is called before the first frame update
    void Start()
    {
        firstPersonCamera = Camera.main;
        gameController = GameObject.Find("GameController");
        snackbar = GameObject.FindObjectOfType<clearSnackbar>();
        myMat = this.transform.GetComponent<MeshRenderer>().material;
        initColor = myMat.color;
        networkGameController = GameObject.FindObjectOfType<NetworkGameController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!hoveringOver && myMat.color != initColor) myMat.color = initColor;

        if (Input.touchCount < 1)
        {
            if (hoveringOver) hoveringOver = false;
            return;
        }

        Touch touch = Input.GetTouch(0);
        RaycastHit hitobject;
        Ray ray = firstPersonCamera.ScreenPointToRay(touch.position);

        //update canDiscard
        //if (!canDiscard && !gameController.GetComponent<GameController>().canDrawCard
        //    && networkGameController.GetComponent<NetworkGameController>().myTurn == networkGameController.GetComponent<NetworkGameController>().playerTurn && networkGameController.gameStarted)
        //    canDiscard = true;

        if (Physics.Raycast(ray, out hitobject))
        {

            if (gameController.GetComponent<SelectAndDrag>().draggingCards)
            {
                // Check if what is hit is the desired object
                if (hitobject.transform.tag == "DiscardPile")
                {
                    if (!hoveringOver) hoveringOver = true;
                    if (gameController.GetComponent<SelectAndDrag>().selectedCards.Count == 1 && canDiscard)
                    {
                        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                        {
                            if (!gameController.GetComponent<GameController>().playerTriedToOpen)
                            {
                                myMat.color = Color.green;
                            }
                            else
                            {
                                myMat.color = Color.red;
                                
                            }
                        }
                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            if (!gameController.GetComponent<GameController>().playerTriedToOpen)
                            {
                                GameObject card = gameController.GetComponent<SelectAndDrag>().selectedCards[0];
                                //remove the card from dragging panel
                                gameController.GetComponent<SelectAndDrag>().selectedCards.Remove(card);
                                networkGameController.CmdDiscardCard(card.name);
                                //PutCardOnPile(card);
                                Destroy(card);
                                gameController.GetComponent<GameController>().removedCardsFromHand(1);
                                networkGameController.CmdChangePlayerTurn(!networkGameController.playerTurn);
                                canDiscard = false;
                                hoveringOver = false;
                                if (tookCard != null && tookCard.activeSelf == false)
                                {
                                    Destroy(tookCard);///////////////////////////    
                                }
                                tookCard = null;
                            }
                            else
                            {
                                networkGameController.CmdFailedToOpen(networkGameController.isHost, tookCard!=null, (tookCard != null?tookCard.name:""));
                                if (tookCard != null)
                                {
                                    canDiscard = false;
                                    gameController.GetComponent<GameController>().removedCardsFromHand(1);////////////////////////////////////////////
                                    if (gameController.GetComponent<SelectAndDrag>().selectedCards.Contains(tookCard))
                                    {
                                        gameController.GetComponent<SelectAndDrag>().selectedCards.Remove(tookCard);
                                    }
                                    Destroy(tookCard);
                                    tookCard = null;
                                    gameController.GetComponent<GameController>().canDrawCard = true;
                                } 
                            }
                        }

                    }
                    else
                    {
                        myMat.color = Color.red;
                        if (!canDiscard && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)) snackbar.setText("You can't throw away a card right now!");
                    }
                    return;
                }

            }
            //if user is not dragging cards - just tapping to draw the card on top of the pile
            else
            {
                if (hitobject.transform.tag == "DiscardPile" && touch.phase == TouchPhase.Began) {
                    if (gameController.GetComponent<GameController>().canDrawCard
                        && networkGameController.GetComponent<NetworkGameController>().myTurn == networkGameController.GetComponent<NetworkGameController>().playerTurn && networkGameController.gameStarted)
                    {
                        gameController.GetComponent<GameController>().canDrawCard = false;
                        canDiscard = true;
                        //cmd to remove the card from all players' piles and add to this player's hand
                        networkGameController.GetComponent<NetworkGameController>().CmdDrawCardFromPile(networkGameController.GetComponent<NetworkGameController>().isHost);
                        
                    } else
                    {
                        snackbar.setText("You can't pick up that card right now!");
                    }
                }
            }
        }
        if (hoveringOver) hoveringOver = false;

    }

    public void PutCardOnPile(GameObject card)
    {
        card.layer = 0;
        //rotation and position of the card
        card.GetComponent<BoxCollider>().enabled = false;
        card.transform.SetParent(this.transform);
        card.transform.localScale = Vector3.one * 0.9f;
        Vector3 temp = Vector3.zero;
        temp.z = discardedCardOffset;
        temp.x = Random.Range(-1.5f, 1.5f);
        temp.y = Random.Range(-1.5f, 1.5f);
        card.transform.localPosition = temp;
        discardedCardOffset += 0.05f;
        temp = Vector3.zero;
        temp.z = Random.Range(-2.5f, 2.5f);
        Quaternion rotation = Quaternion.identity;
        rotation.eulerAngles = temp;
        card.transform.localRotation = rotation;

    }

    public void takeACardFromPile()
    {
        GameObject hand = GameObject.Find("Hand");
        GameObject card = this.transform.GetChild(this.transform.childCount - 1).gameObject;
        if (!gameController.GetComponent<GameController>().playerOpened)
        {
            tookCard = card;
            gameController.GetComponent<GameController>().playerTriedToOpen = true;
        }
        card.transform.SetParent(hand.transform, false);
        card.layer = 5;
        card.GetComponent<BoxCollider>().enabled = true;
        Vector3 temp = Vector3.one;
        temp.x = 2;
        temp.y = 2;
        card.GetComponent<UpdateCard>().originalScale = temp;
        card.GetComponent<UpdateCard>().DeselectCard();

        gameController.GetComponent<GameController>().arrangeCardsOnPanel(hand.transform);
        gameController.GetComponent<GameController>().removedCardsFromHand(-1);
    }

    public void removeCardFromPile()
    {
        Destroy(this.transform.GetChild(this.transform.childCount - 1).gameObject);
    }

    public GameObject getTookCard()
    {
        return tookCard;
    }



}
