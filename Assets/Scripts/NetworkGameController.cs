using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.UI;

public class NetworkGameController : NetworkBehaviour
{
    public clearSnackbar snackbar;
    public bool isHost;
    private GameController gameController;
    public bool gameStarted = false;
    public GameObject winScreen;

    [SyncVar(hook = "_OnChangePlayerTurn")]
    public bool playerTurn;

    public bool myTurn;

    [SyncVar(hook = "_OnPlayerJoined")]
    public int playersJoined;

    private void Start()
    {
        playerTurn = true;
        playersJoined = 0;
        gameController = GameObject.FindObjectOfType<GameController>();

    }

    private void _OnChangePlayerTurn(bool newTurn)
    {
        if (!isHost)
        {
            playerTurn = newTurn;
            if (newTurn == myTurn) gameController.canDrawCard = true;
        }
        
    }

    [Command]
    public void CmdChangePlayerTurn(bool newTurn)
    {
        playerTurn = newTurn;
        if (newTurn == myTurn) gameController.canDrawCard = true;
    }


    public void _OnPlayerJoined(int newNumOfPlayers)
    {
        if (!isHost)
        {
            playersJoined = newNumOfPlayers;
            if (newNumOfPlayers == 2)
            {
                snackbar.setText("All players joined! The game can start.", false);
                gameStarted = true;
            }
        }
       
    }

    [Command]
    public void CmdPlayerJoins()
    {
        playersJoined++;
        if (playersJoined == 2)
        {
            snackbar.setText("All players joined! The game can start.", false);
            gameStarted = true;
            // if (isHost)
            GameObject.FindObjectOfType<GameController>().initGameplay();
        }
    }

    [ClientRpc]
    public void RpcSetDeck(int seed, int numOfCardsInDeck)
    {
        if (!isHost)
        {
            gameController.deckSeed = seed;
            gameController.numOfCardsLeftInDeck = numOfCardsInDeck;
            gameController.deck = GameController.generateDeck();
            gameController.shuffleDeck(gameController.deck);
            GameObject.FindObjectOfType<drawCard>().removeCardFromDeck(GameController.CardsDealtOnStart+1);///////////////////////////////////////////////////
            gameController.dealCards();
        }
    }

    [Command]
    public void CmdDrawCards(int num)
    {
        gameController.numOfCardsLeftInDeck-=num;
        GameObject.FindObjectOfType<drawCard>().removeCardFromDeck(num);///////////////////////////////////////////////////
        RpcSendDrawCards(num);
    }

    [ClientRpc]
    public void RpcSendDrawCards(int num)
    {
        if (!isHost)
        {
            gameController.numOfCardsLeftInDeck-=num;
            GameObject.FindObjectOfType<drawCard>().removeCardFromDeck(num);///////////////////////////////////////////////////
        }
    }

    [Command]
    public void CmdDiscardCard(string card)
    {
        GameObject newCard = Instantiate(gameController.cardPrefab);
        newCard.name = card;
        GameObject.FindObjectOfType<discardCard>().PutCardOnPile(newCard);
        RpcDiscardCard(card);
    }

    [ClientRpc]
    public void RpcDiscardCard(string card)
    {
        if (!isHost)
        {
            GameObject newCard = Instantiate(gameController.cardPrefab);
            newCard.name = card;
            GameObject.FindObjectOfType<discardCard>().PutCardOnPile(newCard);
        }
    }

    [Command]
    public void CmdAddCardsToTabletop(string cards, bool isHostTabletop)
    {
        List<GameObject> cardsToAddToTable = new List<GameObject>();
        string[] cardsArray = cards.Split(' ');
        for (int i =0; i< cardsArray.Length; i++)
        {
            GameObject newCard = Instantiate(gameController.cardPrefab);
            newCard.name = cardsArray[i];
            cardsToAddToTable.Add(newCard);
        }
        if (isHostTabletop) GameObject.Find("PlayerTabletopPanel").GetComponent<PutOnTabletop>().PutCardsOnTabletop(cardsToAddToTable);
        else GameObject.Find("SecondPlayerTabletopPanel").GetComponent<PutOnTabletop>().PutCardsOnTabletop(cardsToAddToTable);
        RpcAddCardsToTabletop(cards, isHostTabletop);
    }

    [ClientRpc]
    public void RpcAddCardsToTabletop(string cards, bool isHostTabletop)
    {
        if (!isHost)
        {
            List<GameObject> cardsToAddToTable = new List<GameObject>();
            string[] cardsArray = cards.Split(' ');
            for (int i = 0; i < cardsArray.Length; i++)
            {
                GameObject newCard = Instantiate(gameController.cardPrefab);
                newCard.name = cardsArray[i];
                cardsToAddToTable.Add(newCard);
            }
            if (isHostTabletop) GameObject.Find("PlayerTabletopPanel").GetComponent<PutOnTabletop>().PutCardsOnTabletop(cardsToAddToTable);
            else GameObject.Find("SecondPlayerTabletopPanel").GetComponent<PutOnTabletop>().PutCardsOnTabletop(cardsToAddToTable);
        }
    }

    [Command]
    public void CmdChangeJokerOnTabletop(string cardName, bool isHostTabletop, int panelChildNum, int cardChildNum)
    {
        GameObject temp;
        if (isHostTabletop) temp = GameObject.Find("PlayerTabletopPanel");
        else temp = GameObject.Find("SecondPlayerTabletopPanel");
        temp = temp.transform.GetChild(panelChildNum).GetChild(cardChildNum).gameObject; // the joker card we have to change is in temp
        temp.name = cardName;//change the name, on next update the value will change too
        temp.GetComponent<UpdateCard>().changeCardFace(cardName);//change the cardface
        RpcChangeJokerOnTabletop(cardName, isHostTabletop, panelChildNum, cardChildNum);
    }

    [ClientRpc]
    public void RpcChangeJokerOnTabletop(string cardName, bool isHostTabletop, int panelChildNum, int cardChildNum){
        if (!isHost)
        {
            GameObject temp;
            if (isHostTabletop) temp = GameObject.Find("PlayerTabletopPanel");
            else temp = GameObject.Find("SecondPlayerTabletopPanel");
            temp = temp.transform.GetChild(panelChildNum).GetChild(cardChildNum).gameObject; // the joker card we have to change is in temp
            temp.name = cardName;//change the name, on next update the value will change too
            temp.GetComponent<UpdateCard>().changeCardFace(cardName);//change the cardface
        }
    }

    [Command]
    public void CmdAddCardsToCardOnTabletop(string cardsToAdd, bool inFront, bool isHostTabletop, int panelChildNum, int cardChildNum)
    {
        List<GameObject> cardsToAddToTable = new List<GameObject>();
        string[] cardsArray = cardsToAdd.Split(' ');
        for (int i = 0; i < cardsArray.Length; i++)
        {
            GameObject newCard = Instantiate(gameController.cardPrefab);
            newCard.name = cardsArray[i];
            cardsToAddToTable.Add(newCard);
        }
        GameObject temp;
        GameObject panel;
        if (isHostTabletop) panel = GameObject.Find("PlayerTabletopPanel");
        else panel = GameObject.Find("SecondPlayerTabletopPanel");
        temp = panel.transform.GetChild(panelChildNum).GetChild(cardChildNum).gameObject; // the card we have to add to is in temp
        panel.GetComponent<PutOnTabletop>().addCardsToExistingCardOnTable(cardsToAddToTable, temp, inFront);
        RpcAddCardsToCardOnTabletop(cardsToAdd, inFront, isHostTabletop, panelChildNum, cardChildNum);
    }

    [ClientRpc]
    public void RpcAddCardsToCardOnTabletop(string cardsToAdd, bool inFront, bool isHostTabletop, int panelChildNum, int cardChildNum)
    {
        if (!isHost)
        {
            List<GameObject> cardsToAddToTable = new List<GameObject>();
            string[] cardsArray = cardsToAdd.Split(' ');
            for (int i = 0; i < cardsArray.Length; i++)
            {
                GameObject newCard = Instantiate(gameController.cardPrefab);
                newCard.name = cardsArray[i];
                cardsToAddToTable.Add(newCard);
            }
            GameObject temp;
            GameObject panel;
            if (isHostTabletop) panel = GameObject.Find("PlayerTabletopPanel");
            else panel = GameObject.Find("SecondPlayerTabletopPanel");
            temp = panel.transform.GetChild(panelChildNum).GetChild(cardChildNum).gameObject; // the card we have to add to is in temp
            panel.GetComponent<PutOnTabletop>().addCardsToExistingCardOnTable(cardsToAddToTable, temp, inFront);
        }
    }

    [Command]
    public void CmdFailedToOpen(bool isHostTabletop, bool tookCardFromPile, string card)
    {
        GameObject panel;
        if (isHostTabletop) panel = GameObject.Find("PlayerTabletopPanel");
        else panel = GameObject.Find("SecondPlayerTabletopPanel");
        if (isHost == isHostTabletop) panel.GetComponent<PutOnTabletop>().returnOpenedCardsToHand();
        panel.GetComponent<PutOnTabletop>().removeAllCardsFromPanel();
        if (tookCardFromPile)
        {
            CmdDiscardCard(card);
        }
        RpcFailedToOpen(isHostTabletop);
    }

    [ClientRpc]
    public void RpcFailedToOpen(bool isHostTabletop)
    {
        if (!isHost)
        {
            GameObject panel;
            if (isHostTabletop) panel = GameObject.Find("PlayerTabletopPanel");
            else panel = GameObject.Find("SecondPlayerTabletopPanel");
            if (isHost == isHostTabletop) panel.GetComponent<PutOnTabletop>().returnOpenedCardsToHand();
            panel.GetComponent<PutOnTabletop>().removeAllCardsFromPanel();
        }
    }

    [Command]
    public void CmdDrawCardFromPile(bool isHostPlayer)
    {
        if (isHostPlayer != isHost)
        {
            //other player took the card - removing it from the pile
            GameObject.FindGameObjectWithTag("DiscardPile").GetComponent<discardCard>().removeCardFromPile();
        } else
        {
            //this player drew the card - adding it to hand
            GameObject.FindGameObjectWithTag("DiscardPile").GetComponent<discardCard>().takeACardFromPile();
        }
        RpcDrawCardFromPile(isHostPlayer);
        
    }

    [ClientRpc]
    public void RpcDrawCardFromPile(bool isHostPlayer)
    {
        if (!isHost)
        {
            if (isHostPlayer != isHost)
            {
                //other player took the card - removing it from the pile
                GameObject.FindGameObjectWithTag("DiscardPile").GetComponent<discardCard>().removeCardFromPile();
            }
            else
            {
                //this player drew the card - adding it to hand
                GameObject.FindGameObjectWithTag("DiscardPile").GetComponent<discardCard>().takeACardFromPile();
            }
        }
    }

    [Command]
    public void CmdPlayerWon(bool isHostPlayer)
    {
        disableTheGame(isHostPlayer);
        RpcPlayerWon(isHostPlayer);
    }

    [ClientRpc]
    public void RpcPlayerWon(bool isHostPlayer)
    {
        if (!isHost)
        {
            disableTheGame(isHostPlayer);
            
        }
    }

    private void disableTheGame(bool winner)
    {
        gameController.enabled = false;
        GameObject.FindObjectOfType<SelectAndDrag>().enabled = false;
        GameObject.FindObjectOfType<SelectAndDrag>().enabled = false;
        GameObject.FindObjectOfType<drawCard>().enabled = false;
        GameObject.FindObjectOfType<discardCard>().enabled = false;
        GameObject.Find("PlayerTabletopPanel").GetComponent<PutOnTabletop>().enabled = false;
        GameObject.Find("SecondPlayerTabletopPanel").GetComponent<PutOnTabletop>().enabled = false;
        winScreen.SetActive(true);
        if (winner != isHost) winScreen.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "You lost!";

    }

}
