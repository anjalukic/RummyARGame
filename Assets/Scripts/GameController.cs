using GoogleARCore.Examples.CloudAnchors;
using GoogleARCore.Examples.Common;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public clearSnackbar snackbar;
    public GameObject cardPrefab;
    public GameObject hand;
    public bool flag = false;

    public Material[] cardFaces;
    public Material jokerCardFace;
    public static string[] suits = new string[] { "C", "D", "H", "S" };
    public static string[] values = new string[] { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
    public static string joker = "J";

    public static int numOfJokers = 4;
    public static int numOfDecks = 2;
    public int deckSeed;// Environment.TickCount on server

    public List<string> deck;
    public int numOfCardsLeftInDeck = 52 * numOfDecks + numOfJokers;

    public static int CardsDealtOnStart = 14; // depending on game - number od cards dealt on start
    private int handCardNum = 0;
    public bool playerOpened = false;
    public bool playerTriedToOpen = false;
    //public bool mustClose = false;

    public NetworkGameController networkGameController;
    public bool canDrawCard = false;


    //should be called only on host
    public void initGameplay()
    {
        deckSeed = Environment.TickCount;
        deck = generateDeck();
        shuffleDeck(deck);
        dealCards();
        networkGameController.RpcSetDeck(deckSeed, numOfCardsLeftInDeck);
    }


    public void dealCards()
    {
        //float offset = 0;
        int cardsToDeal = CardsDealtOnStart;
        if (networkGameController.isHost) cardsToDeal++;
        for (int i = 0; i < cardsToDeal; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, hand.transform, false);
            handCardNum++;
            newCard.name = deck[numOfCardsLeftInDeck-i-1];
        }
        if (!networkGameController.isHost)
        {
            networkGameController.CmdDrawCards(cardsToDeal);
        } else
        {
            numOfCardsLeftInDeck -= cardsToDeal;
            GameObject.FindObjectOfType<drawCard>().removeCardFromDeck(cardsToDeal);/////////////////////////////////////////
            canDrawCard = false; // first player has 15 cards and doesn't draw before discarding a card
            GameObject.FindObjectOfType<discardCard>().canDiscard = true;
        }
        arrangeCardsOnPanel(hand.transform);  
    }


    public void drawCard()
    {
        if (networkGameController.playerTurn != networkGameController.myTurn)
        {
            snackbar.setText("It isn't your turn to play yet!");
            return;
        }
        if (numOfCardsLeftInDeck > 0 && canDrawCard)
        {
            canDrawCard = false;
            GameObject.FindObjectOfType<discardCard>().canDiscard = true;
            GameObject newCard = Instantiate(cardPrefab, hand.transform, false);
            removedCardsFromHand(-1);
            newCard.name = deck[numOfCardsLeftInDeck-1];
            arrangeCardsOnPanel(hand.transform);
            networkGameController.CmdDrawCards(1);
        }
        else
        {
            if (numOfCardsLeftInDeck == 0) snackbar.setText("There's no more cards in deck!");
            else snackbar.setText("You can't draw any more cards!");
        }

    }

    public void arrangeCardsOnPanel(Transform panel, float initOffset = 0.0f, float offset = 0.001f)
    {
        // to show cards left to right
        float moveOffset = initOffset;
        Vector3 tempPos = new Vector3();
        Transform temp;
        for (int i = 0; i < panel.transform.childCount; i++)
        {
            temp = panel.transform.GetChild(i).transform;
            tempPos.Set(temp.localPosition.x, temp.localPosition.y, moveOffset);
            temp.localPosition = tempPos;
            temp.localRotation = Quaternion.identity * Quaternion.Euler(0, 180f, 0);
            moveOffset -= offset;
        }
    }


    public static List<string> generateDeck()
    {
        List<string> deck = new List<string>();
        for (int i = 0; i < numOfDecks; i++)
        {
            foreach (string s in suits)
            {
                foreach (string v in values)
                {
                    deck.Add(s + v);
                }
            }
        }
        for (int i = 0; i < numOfJokers; i++)
        {
            deck.Add(joker);
        }

        return deck;
    }

    public void shuffleDeck(List<string> deck)
    {
        System.Random random = new System.Random(deckSeed);
        int n = deck.Count;
        while (n > 1)
        {
            int k = random.Next(n);
            n--;
            string temp = deck[k];
            deck[k] = deck[n];
            deck[n] = temp;
        }
    }

    public void removedCardsFromHand(int num)
    {
        handCardNum -= num;
      //  if (handCardNum == 1 && GameObject.FindObjectOfType<discardCard>().canDiscard)
      //  {
      //      mustClose = true;
      //  }
        if (handCardNum == 0) networkGameController.CmdPlayerWon(networkGameController.isHost);

    }

    public void restartGame()
    {
        NetworkManager.Shutdown();
        SceneManager.LoadScene("GameScene");
    }



}
