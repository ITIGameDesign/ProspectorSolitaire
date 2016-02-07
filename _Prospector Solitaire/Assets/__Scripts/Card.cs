using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Card : MonoBehaviour 
{
	public string suit;  //Suit of the card (c, d, h, or s)
	public int rank;  //Rank of the card (1-14)
	public Color color = Color.black;  //Color to tint pips
	public string colS = "Black";  //or "red". Name of the color
	//This list holds all of the Decorator GameObjects
	public List<GameObject> decoGOs = new List<GameObject>();
	//This list holds all of the Pip GameObjects
	public List<GameObject> pipGOs = new List<GameObject>();
	
	public GameObject back;  //The GameObject of the back of the card
	public CardDefinition def;  //Parsed from DeckXML.xml
	
	//List of the SpriteRenderer Components of this GameObject and its children.
	public SpriteRenderer[] spriteRenderers;
	
	void Start()
	{
		SetSortOrder(0);  //Ensures that the card starts properly depth sorted.

	}
	
	public bool faceUp
	{
		get
		{
			return(!back.activeSelf);
		}
		set
		{
			back.SetActive(!value);
		}
	}
	
	//If spriteRenderers is null or empty
	public void PopulateSpriteRenderers()
	{
		//If spriteRenderers is null or empty
		if (spriteRenderers == null || spriteRenderers.Length == 0)
		{
			//Get SpriteRenderer components of this GameObject and its children
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

		}

	}
	
	//Sets the sortingLayerName on all Sprite Renderer Components
	public void SetSortingLayerName(string tSLN)
	{
		PopulateSpriteRenderers();
		
		foreach (SpriteRenderer tSR in spriteRenderers)
		{
			tSR.sortingLayerName = tSLN;

		}

	}
	
	//Sets the sortingOrder of all SpriteRenderer components
	public void SetSortOrder(int sOrd)
	{
		PopulateSpriteRenderers();
		
		//The white background of the card is on the bottom (sOrd)
		//On top of that there are all the pips, decorators, face, etc. (sOrd+1)
		//The back is on top so that when visible it covers the rest (sOrd+2)
		
		//Iterate through all of the spriteRenderers as tSR
		foreach (SpriteRenderer tSR in spriteRenderers)
		{
			if (tSR.gameObject == this.gameObject)
			{
				//If the gameObject is this.gameObject is this.gameObject, it's the background
				tSR.sortingOrder = sOrd;  //Set its order to sOrd
				continue;  //And it continues to the next iteration of the loop

			}
			//Each of the cfhildren of this GameObjectare named
			//switch based on the namespace
			switch (tSR.gameObject.name)
			{
				case "back":  //if the name is "back"
					tSR.sortingOrder = sOrd+2;
					// ^ Set it to the highest layer to cover everything else
					break;
				case "face":  //if the name is face
				default:  //or if it is anything else
					tSR.sortingOrder = sOrd+1;
					// ^ Set it to the middle layer to be above the background
					break;

			}

		}

	}
	
	//Virtual methods can be overridden by subclass methods with the same name
	virtual public void OnMouseUpAsButton()
	{
		print (name);  //When clicked, this outputs the card name

	}
}

[System.Serializable]
public class Decorator 
{
	//This class stores information about each decorator or pip from DeckXML
	public string type;  //For card pips, type = "pip"
	public Vector3 loc;  //The location of the Sprite on the card
	public bool flip = false;  //Wether to flip the sprite vertically
	public float scale = 1f;  //The scale of the sprite
}

[System.Serializable]
public class CardDefinition 
{
	//This class stores info for each rank of card
	public string face;  //Sprite to use for each face card
	public int rank;  //The rank (1-13) of this card
	public List<Decorator> pips = new List<Decorator>();
	//Pips used because decorators (from the xml) are used the same way on every card
	//in the deck, pips only stores info about the pips on numbered cards
}

