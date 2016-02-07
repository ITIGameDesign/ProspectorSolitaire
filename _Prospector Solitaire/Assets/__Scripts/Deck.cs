using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Deck : MonoBehaviour 
{
	//Suits
	public Sprite suitClub;
	public Sprite suitDiamond;
	public Sprite suitHeart;
	public Sprite suitSpade;
	
	public Sprite[] faceSprites;
	public Sprite[] rankSprites;
	
	public Sprite cardBack;
	public Sprite cardBackGold;
	public Sprite cardFront;
	public Sprite cardFrontGold;
	
	//Prefabs
	public GameObject prefabSprite;
	public GameObject prefabCard;
	
	public bool ____________________;
	
	public PT_XMLReader xmlr;
	public List<string> cardNames;
	public List<Card> cards;
	public List<Decorator> decorators;
	public List<CardDefinition> cardDefs;
	public Transform deckAnchor;
	public Dictionary<string, Sprite> dictSuits;
	
	//InitDeckis called by Prospector when it is ready
	public void InitDeck(string deckXMLText)
	{
		//This creates an anchor for all the card gameobjects in the heiarchy
		if (GameObject.Find("_Deck") == null)
		{
			GameObject anchorGO = new GameObject("_Deck");
			deckAnchor = anchorGO.transform;
		};
		
		//Initialize the Dictionary of the SuitSprites with necessary sprites
		dictSuits = new Dictionary<string, Sprite>()
		{
			{"C", suitClub}, 
			{"D", suitDiamond}, 
			{"H", suitHeart}, 
			{"S", suitSpade} 
		};
		
		ReadDeck(deckXMLText);
		MakeCards();
	}
	
	//ReadDeck parses the XML file passed to it into CardDefinitions
	public void ReadDeck(string deckXMLText)
	{
		xmlr = new PT_XMLReader();  //Create a new PT_XMLReader
		xmlr.Parse(deckXMLText);  //Use that PT_XMLReader to parse DeckXML
		
		//This prints a test line to show you how xmlr can be used.
		//For more info read about XML in the Useful Concepts section of book
		string s = "xmlr.xml[0] decorator[0]";
		s += "type="+xmlr.xml["xml"][0]["decorator"][0].att("type");
		s += " x="+xmlr.xml["xml"][0]["decorator"][0].att("x");
		s += " y="+xmlr.xml["xml"][0]["decorator"][0].att("y");
		s += " scale="+xmlr.xml["xml"][0]["decorator"][0].att("scale");
		//print(s);  //Done with this test now
		
		//Read decorators for all Cards
		decorators = new List<Decorator>();  //Init the list of Decorator
		//Grab a PT_XMLHashList of all <decorator>s in the XML file
		PT_XMLHashList xDecos = xmlr.xml["xml"][0]["decorator"];
		Decorator deco;
		for (int i=0; i<xDecos.Count; i++)
		{
			//For each decorator in the xml
			deco = new Decorator();  //Make new Decorator
			//Copy the attributes of the <decorator> to the Decorator
			deco.type = xDecos[i].att("type");
			//Set the bool flip based on bwether the text of the attribute is 
			//"1" or something else.  This is an atypical but perfectly fine
			//use of the == comparison operator.  It will return a true or 
			//false, which will be assigned to deco.flip.
			deco.flip = (xDecos[i].att ("flip") == "1");
			//floats need to be parsed from the attribute strings
			deco.scale = float.Parse(xDecos[i].att ("scale"));
			//Vector3 loc initializes to [0,0,0], so we just need to modify it
			deco.loc.x = float.Parse(xDecos[i].att ("x"));
			deco.loc.y = float.Parse(xDecos[i].att ("y"));
			deco.loc.z = float.Parse(xDecos[i].att ("z"));
			//Add the temporary deco to the list decorators
			decorators.Add (deco);
		}
		
		//Read pip locations for each card number
		cardDefs = new List<CardDefinition>();  //Init the list of cards
		//Grab a PT_XMLHashList of all the <card>s in the xml file
		PT_XMLHashList xCardDefs = xmlr.xml["xml"][0]["card"];
		for (int i=0; i<xCardDefs.Count; i++)
		{
			//for each of the <card>s
			//create new CardDefinition
			CardDefinition cDef = new CardDefinition();
			//parse the attribute values and add them to cDef
			cDef.rank = int.Parse(xCardDefs[i].att ("rank"));
			//Grab a PT_XMLHashList of all the <pip>s on this <card>
			PT_XMLHashList xPips = xCardDefs[i]["pip"];
			if (xPips != null)
			{
				for (int j=0; j<xPips.Count; j++)
				{
					//Iterate through all of the <pip>s
					deco = new Decorator();
					//<pip>s on the <card> are handled via the decorator class
					deco.type = "pip";
					deco.flip = (xPips[j].att ("flip") == "1");
					deco.loc.x = float.Parse(xPips[j].att ("x"));
					deco.loc.y = float.Parse(xPips[j].att ("y"));
					deco.loc.z = float.Parse(xPips[j].att ("z"));
					if (xPips[j].HasAtt("scale"))
					{
						deco.scale = float.Parse(xPips[j].att ("scale"));
					}
					cDef.pips.Add(deco);
				}
			}
			//Face cards (Jack, King, Queen) have a face attribute
			//cDef.face is the name of the face card Sprite
			//e.g., Jack of clubs is FaceCard_11c, hearts is FaceCard_11H, etc.
			if (xCardDefs[i].HasAtt("face"))
			{
				cDef.face = xCardDefs[i].att ("face");
			}
			cardDefs.Add(cDef);
		}
	}
	
	//Get the proper CardDefinition based on Rank (1 to 14 is Ace to King)
	public CardDefinition GetCardDefinitionByRank(int rnk)
	{
		//Search through all of the carddefinitions
		foreach (CardDefinition cd in cardDefs)
		{
			//If the rank is correct, return this definition
			if (cd.rank == rnk)
			{
				return(cd);
			}
		}
		return(null);
	}
	
	//Make the card GameObjects
	public void MakeCards()
	{
		//cardNames will be the names of cards to build
		//Each suit goes from 1 to 13 (e.g., C1 to C13 for clubs)
		cardNames = new List<string>();
		string[] letters = new string[] {"C", "D", "H", "S"};
		foreach (string s in letters)
		{
			for (int i=0; i<13; i++)
			{
				cardNames.Add(s+(i+1));
			}
		}
		
		//Make a list to hold all the cards
		cards = new List<Card>();
		//Sevral variables that will be reused sevral times
		Sprite tS = null;
		GameObject tGO = null;
		SpriteRenderer tSR = null;
		
		//Iterate through all of the card names that were just made
		for (int i=0; i<cardNames.Count; i++)
		{
			//Create a new card gameobject 
			GameObject cgo = Instantiate(prefabCard) as GameObject;
			//Set the transform.parent of the new card to the anchor.
			cgo.transform.parent = deckAnchor;
			Card card = cgo.GetComponent<Card>();  //Get the card component
			
			//This just stacks the cards so that they're all in nice rows
			cgo.transform.localPosition = new Vector3((i%13)*3, i/13*4, 0);
			
			//Assign basic values to the card 
			card.name = cardNames[i];
			card.suit = card.name[0].ToString();
			card.rank = int.Parse(card.name.Substring(1));
			if (card.suit == "D" || card.suit == "H")
			{
				card.colS = "Red";
				card.color = Color.red;
			}
			//Pull the card definition for this card
			card.def = GetCardDefinitionByRank(card.rank);
			
			//Add decorators.
			foreach (Decorator deco in decorators)
			{
				if (deco.type == "suit")
				{
					//Instiantiate a sprites gameobject
					tGO = Instantiate(prefabSprite) as GameObject;
					//Get the spriterenderer component.
					tSR = tGO.GetComponent<SpriteRenderer>();
					//Set the Sprite to the proper suit
					tSR.sprite = dictSuits[card.suit];
				} else {   //If it's not a suit, it's a rank deco
					tGO = Instantiate(prefabSprite) as GameObject;
					tSR = tGO.GetComponent<SpriteRenderer>();
					//Get the proper Sprite to show this rank
					tS = rankSprites[card.rank];
					//Assign this rank Sprite to the SpriteRenderer
					tSR.sprite = tS;
					//Set the color of the rank to match the suit
					tSR.color = card.color;
				}
				//Make the deco Sprites renderer above the card
				tSR.sortingOrder = 1;
				//Make the decorator Sprite a child of the card
				tGO.transform.parent = cgo.transform;
				//Set the localPosition based on the location from DeckXML
				tGO.transform.localPosition = deco.loc;
				//Flip the decorator if needed
				if (deco.flip)
				{
					//An Euler rotation of 180 degrees around the zAxis will flip it
					tGO.transform.rotation = Quaternion.Euler(0, 0, 180);
				}
				//Set the scale to keep the decos from being too big
				if (deco.scale !=1)
				{
					tGO.transform.localScale = Vector3.one * deco.scale;
				}
				//Name this GameObject so it;s easy to find
				tGO.name = deco.type;
				//Add this deco GameObject to the List card.decoGos
				card.decoGOs.Add(tGO);
			}
			
			//Add pips, for each of the pip in the definition
			foreach (Decorator pip in card.def.pips)
			{
				//Instiate a sprite gameobject
				tGO = Instantiate(prefabSprite) as GameObject;
				//It's too late again, for notes from here on out, see page 579 forward.
				tGO.transform.parent = cgo.transform;
				tGO.transform.localPosition = pip.loc;
				if (pip.scale != 1)
				{
					tGO.transform.localScale = Vector3.one * pip.scale;
				}
				tGO.name = "pip";
				tSR = tGO.GetComponent<SpriteRenderer>();
				tSR.sprite = dictSuits[card.suit];
				tSR.sortingOrder = 1;
				card.pipGOs.Add(tGO);
			}
			
			if (card.def.face != "")
			{
				tGO = Instantiate(prefabSprite) as GameObject;
				tSR = tGO.GetComponent<SpriteRenderer>();
				tS = GetFace(card.def.face+card.suit);
				tSR.sprite = tS;
				tSR.sortingOrder = 1;
				tGO.transform.parent = card.transform;
				tGO.transform.localPosition = Vector3.zero;
				tGO.name = "face";
			}
			
			tGO = Instantiate(prefabSprite) as GameObject;
			tSR = tGO.GetComponent<SpriteRenderer>();
			tSR.sprite = cardBack;
			tGO.transform.parent = card.transform;
			tGO.transform.localPosition = Vector3.zero;
			//This is a higher Sorting order than anything else
			tSR.sortingOrder = 2;
			tGO.name = "back";
			card.back = tGO;
			
			//Default to face up
			card.faceUp = false;
			
			//Add the card to the deck
			cards.Add(card);
		}
	}  //End MakeCards()
	
	public Sprite GetFace(string faceS)
	{
		foreach (Sprite tS in faceSprites)
		{
			if (tS.name == faceS)
			{
				return(tS);
			}
		}
		//If nothing found, will return null
		return(null);
	}
	
	static public void Shuffle(ref List<Card> oCards)
	{
		List<Card> tCards = new List<Card>();
		
		int ndx;
		tCards = new List<Card>();
		while (oCards.Count > 0)
		{
			ndx = Random.Range(0, oCards.Count);
			tCards.Add (oCards[ndx]);
			oCards.RemoveAt(ndx);
		}
		
		oCards = tCards;
	}
}