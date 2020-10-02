using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateCard : MonoBehaviour
{

    private Material cardFace;
    public Material defaultFace;
    private Material[] cardMats;
    private GameController gameController;
    public bool isSelected = false;
   // private bool lastSelectedFlag = false;
    public Vector3 originalScale;
    private Vector3 zoomOffset;
   // private bool changeScale = true;
    public string suit = "";
    public string valueString = "";
    public int value = 0;
    // Start is called before the first frame update
    void Start()
    {

        cardMats = this.GetComponent<MeshRenderer>().materials;
        gameController = FindObjectOfType<GameController>();
        bool setFace=false;

        if (this.name == "J")
        {
            cardFace = gameController.jokerCardFace;
            setFace = true;
        }else
        {
            int i = 0;
            List<string> deck = GameController.generateDeck();
            foreach (string card in deck)
            {
                if (card == this.name)
                {
                    cardFace = gameController.cardFaces[i];
                    setFace = true;
                    break;
                }
                i++;
            }
        }

        if (!setFace) cardFace = defaultFace;
        cardMats = this.GetComponent<MeshRenderer>().materials;

        originalScale = this.transform.localScale;
        zoomOffset = new Vector3(0.5f, 0.5f, 0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (cardMats[0] != cardFace)
        {
            cardMats[0] = cardFace;
            this.GetComponent<MeshRenderer>().materials = cardMats;
        }
        /*

        if (changeScale)
        {
            if (isSelected != lastSelectedFlag)
            {
                lastSelectedFlag = isSelected;
                if (isSelected)
                {
                    this.transform.localScale = originalScale + zoomOffset;
                }
                else
                {
                    this.transform.localScale = originalScale;
                }
            }
            
        }
        */
        setCardValueBasedOnName();
    
    }

    public void changeCardFace(Material newFace)
    {
        cardFace = newFace;
        cardMats[0] = cardFace;
        this.GetComponent<MeshRenderer>().materials = cardMats;
    }

    public void changeCardFace(string name)
    {
        if (name == "J")
        {
            cardFace = gameController.jokerCardFace;
        }
        else
        {
            int i = 0;
            List<string> deck = GameController.generateDeck();
            foreach (string card in deck)
            {
                if (card == name)
                {
                    cardFace = gameController.cardFaces[i];
                    break;
                }
                i++;
            }
        }
        cardMats[0] = cardFace;
        this.GetComponent<MeshRenderer>().materials = cardMats;
    }

    public Material getCardFace()
    {
        return cardFace;
    }

    private int stringToVal(string val)
    {
        int value = 0;

        if (!System.Int32.TryParse(val, out value)) {
            switch (val)
            {
                case "A":
                    value=1;
                    break;
                case "J":
                    value = 11;
                    break;
                case "Q":
                    value = 12;
                    break;
                case "K":
                    value = 13;
                    break;
                default:
                    value = 0;
                    break;
            }
        }
        return value;
    }
    /*
    public void StopScaling()
    {
        changeScale = false;
    }
    */
    public void SelectCard()
    {
        this.transform.localScale = originalScale + zoomOffset;
        isSelected = true;
    }

    public void DeselectCard()
    {
        this.transform.localScale = originalScale;
        isSelected = false;
    }

    public void setCardValueBasedOnName()
    {
        if (this.name.Substring(0, 1) != suit) suit = this.name.Substring(0, 1);
        if (this.name != "J" && this.name.Substring(1) != valueString)
        {
            valueString = this.name.Substring(1);
            value = stringToVal(valueString);
        }
    }
}
