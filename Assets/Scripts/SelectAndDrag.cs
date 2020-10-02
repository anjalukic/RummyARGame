using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectAndDrag : MonoBehaviour
{
    private Camera UICamera;
    private GameObject hand;
    private Camera firstPersonCamera;
    public List<GameObject> selectedCards = new List<GameObject>();
    public bool draggingCards = false;
    public bool arrangingCardsInHand = false;

    private float scaleFactor;
    private Vector3 panelScale;
    private float initialTouchPosition;

    private Vector2 screenResolution;
    private Vector2 canvasSize;
    private GameController gameController;
    private GameObject draggedCardsPanel;
    private bool arranged = false;
    private bool scalingDown = false;
    private bool initScalingDone = false;

    void Start()
    {
        UICamera = GameObject.Find("UICamera").GetComponent<Camera>();
        hand = GameObject.Find("Hand");
        draggedCardsPanel = GameObject.Find("DraggedCards");
        gameController = GameObject.FindObjectOfType<GameController>();

        screenResolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
        Rect temp = hand.transform.parent.transform.GetComponent<RectTransform>().rect;
        canvasSize = new Vector2(temp.width, temp.height);

    }

    void Update()
    {
       
        // If the player has not touched the screen
        Touch touch;
        if (Input.touchCount < 1)
        {
            //if cards were moved or touched but not placed anywhere 
            if (selectedCards.Count > 0 || draggingCards)
            {
                foreach (GameObject card in selectedCards)
                {
                    if (card.transform.parent != hand.transform) card.transform.SetParent(hand.transform, false);
                    card.transform.GetComponent<UpdateCard>().DeselectCard(); // to scale the card back to original size
                    card.transform.GetComponent<BoxCollider>().enabled = true;
                }
                refreshFlagsForNextTouch();
            }
            return;
        }

        touch = Input.GetTouch(0);

        //player touched the first card
        if (touch.phase == TouchPhase.Began)
        {
            selectingACard(touch);
        }

        //player moved the finger - to select another card or to draw the cards out
        if (touch.phase == TouchPhase.Moved)
        {
            if (selectedCards.Count > 0)
            {
                if (!draggingCards) draggingCards = touch.deltaPosition.y > 10;

                if (!draggingCards)
                {
                    selectingACard(touch);
                }
                else
                {
                    if (!scalingDown && touch.position.y >= screenResolution.y * 2 / 5) { scalingDown = true; }
                    movingCards(touch);
                    insertingCardsInHand(touch);
                }
            }

        }

    }

    void movingCards(Touch touch)
    {
        foreach (GameObject card in selectedCards)
        {
            if (card.transform.parent == hand.transform && card.transform.GetComponent<UpdateCard>().isSelected)
            {
                card.transform.GetComponent<UpdateCard>().DeselectCard(); // to scale the card back to original size
                card.transform.SetParent(draggedCardsPanel.transform);
                card.transform.GetComponent<BoxCollider>().enabled = false;
            }
        }

        //moving the panel with cards
        draggedCardsPanel.transform.localPosition = new Vector3(touch.position.x * canvasSize.x / screenResolution.x - canvasSize.x / 2,
               touch.position.y * canvasSize.y / screenResolution.y - canvasSize.y / 2, 0);


        //if the dragging just started
        if (!arranged)
        {
            gameController.arrangeCardsOnPanel(draggedCardsPanel.transform, 0.015f);
            arranged = true;
        }

        if (scalingDown)
        {
            if (!initScalingDone)
            {
                initialTouchPosition = touch.position.y;
                panelScale = draggedCardsPanel.transform.localScale;
                initScalingDone = true;
            }

            //scaling the panel to look like its moving further away
            scaleFactor = (-touch.deltaPosition.y) / initialTouchPosition;
            panelScale += scaleFactor * panelScale;
            if (panelScale.x <= 1.5f && panelScale.x > 0.5f) { draggedCardsPanel.transform.localScale = panelScale; }
        }

    }

    void insertingCardsInHand(Touch touch)
    {
        RaycastHit hitobject;
        Ray ray = UICamera.ScreenPointToRay(touch.position);
        int layerMask = 1 << 5; // UI layer only
        if (Physics.Raycast(ray, out hitobject, Mathf.Infinity, layerMask))
        {
            if (!arrangingCardsInHand) { arrangingCardsInHand = true; }
            // Check if what is hit is the desired object
            if (hitobject.transform.tag == "Card")
            {
                if (selectedCards.Contains(hitobject.transform.gameObject)) return;
                bool goingLeft = (hitobject.transform.GetSiblingIndex() < selectedCards[0].transform.GetSiblingIndex() || selectedCards[0].transform.parent != hand.transform);
                int index = hitobject.transform.GetSiblingIndex();
                foreach (GameObject card in selectedCards)
                {
                    if (card.transform.parent != hand.transform) {
                        card.transform.SetParent(hand.transform, false);
                        card.transform.GetComponent<UpdateCard>().DeselectCard(); // to scale the card back to original size
                    }
                    if (goingLeft) index = hitobject.transform.GetSiblingIndex();
                    card.transform.SetSiblingIndex(index);
                    card.transform.GetComponent<BoxCollider>().enabled = true;
                }

                gameController.arrangeCardsOnPanel(hand.transform);
            }
        }
        else
        {
            foreach (GameObject card in selectedCards)
            {
                if (card.transform.parent == hand.transform) card.transform.SetParent(draggedCardsPanel.transform);
                card.transform.GetComponent<BoxCollider>().enabled = false;
            }
            arrangingCardsInHand = false;
            gameController.arrangeCardsOnPanel(hand.transform);
            gameController.arrangeCardsOnPanel(draggedCardsPanel.transform, 0.015f);
        }

    }

    void selectingACard(Touch touch)
    {
        RaycastHit hitobject;
        Ray ray = UICamera.ScreenPointToRay(touch.position);
        int layerMask = 1 << 5; // UI layer only
        if (Physics.Raycast(ray, out hitobject, Mathf.Infinity, layerMask))
        {
            // Check if what is hit is the desired object
            if (hitobject.transform.tag == "Card")
            {
                if (hitobject.transform.GetComponent<UpdateCard>().isSelected == false)
                {
                    //select the card                   
                    hitobject.transform.GetComponent<UpdateCard>().SelectCard();
                    if (selectedCards.Count == 0 || selectedCards[0].transform.GetSiblingIndex() < hitobject.transform.GetSiblingIndex())
                    {
                        selectedCards.Add(hitobject.transform.gameObject);
                    }
                    else //if cards were selected in reverse order
                    {
                        selectedCards.Insert(0, hitobject.transform.gameObject);
                    }

                }
            }
        }
    }

    void refreshFlagsForNextTouch()
    {
        gameController.arrangeCardsOnPanel(hand.transform);
        arranged = false;
        scalingDown = false;
        initScalingDone = false;
        draggedCardsPanel.transform.position = hand.transform.position;
        draggedCardsPanel.transform.localScale = hand.transform.localScale;
        selectedCards.Clear();
        draggingCards = false;
        arrangingCardsInHand = false;
    }

    public GameObject getDragPanel()
    {
        return draggedCardsPanel;
    }


}
