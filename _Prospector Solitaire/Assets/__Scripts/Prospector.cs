using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// NOTE for possible points of error ctrl F capital letters beginning with AA

// An enum to handle all the possible scoring events
public enum ScoreEvent {
	draw,
	mine,
	mineGold,
	gameWin,
	gameLoss
}

public class Prospector : MonoBehaviour {
	static public Prospector S;
	static public int SCORE_FROM_PREV_ROUND = 0;
	static public int HIGH_SCORE = 0;

	public float reloadDelay = 1f; // The delay between rounds

	public Vector3 fsPosMid = new Vector3(0.5f, 0.90f, 0);
	public Vector3 fsPosRun = new Vector3(0.5f, 0.75f, 0);
	public Vector3 fsPosMid2 = new Vector3(0.5f, 0.5f, 0);
	public Vector3 fsPosEnd = new Vector3(1.0f, 0.65f, 0);

	public Deck deck;
	public TextAsset deckXML;

	public Layout layout;
	public TextAsset layoutXML;
	public Vector3 layoutCenter;
	public float xOffset = 3;
	public float yOffset = -2.5f;
	public Transform layoutAnchor;
	public CardProspector target;
	public List<CardProspector> tableau;
	public List<CardProspector> discardPile;
	public List<CardProspector> drawPile;  //moved in from outside of Prospector, might not actualy work.

	// Fields to track score info
	public int chain = 0; // of cards in this run
	public int scoreRun = 0;
	public int score = 0;
	public FloatingScore fsRun;

	public GUIText GTGameOver;
	public GUIText GTRoundResult;

	// void Awake is left out of many parts of the tutorial. Not sure if it needs to be part of the final code

	void Awake() {
		S = this; // Set up a Singleton for Prospector
		// Check for a high score in PlayerPrefs
		if (PlayerPrefs.HasKey ("ProspectorHighScore")) {
			HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
		}
		// Add the score from last round, which will be >0 if it was a win
		score += SCORE_FROM_PREV_ROUND;
		// And reset the SCORE_FROM_PREV_ROUND
		SCORE_FROM_PREV_ROUND = 0;

		// Set up the GUITexts that show at the end of the round
		// Get the GUIText Components
		GameObject go = GameObject.Find ("GameOver");
		if (go != null) {
			GTGameOver = go.GetComponent<GUIText>();
		}
		go = GameObject.Find ("RoundResult");
		if (go != null) {
			GTRoundResult = go.GetComponent<GUIText>();
		}
		// Make them invisible
		ShowResultGTs(false);
		go = GameObject.Find("HighScore");
		string hScore = "High Score: "+Utils.AddCommasToNumber(HIGH_SCORE);
		go.GetComponent<GUIText>().text = hScore;
	}

	void ShowResultGTs(bool show) {
		GTGameOver.gameObject.SetActive(show);
		GTRoundResult.gameObject.SetActive(show);
//}
	}

	//public List<CardProspector> drawPile;

	void Start () {
		Scoreboard.S.score = score;

		deck = GetComponent<Deck>(); // Get the Deck
		deck.InitDeck(deckXML.text); // Pass DeckXML to it
		Deck.Shuffle(ref deck.cards); // This shuffles the deck
		// The ref keyword passes a reference to deck.cards, which allows
		// deck.cards to be modified by Deck.Shuffle()

		layout = GetComponent<Layout>(); // Get the Layout
		layout.ReadLayout(layoutXML.text); // Pass LayoutXML to it

		drawPile = ConvertListCardsToListCardProspectors( deck.cards );
		LayoutGame();
	}

	// The Draw function will pull a single card from the drawPile and return it
	CardProspector Draw() {
		CardProspector cd = drawPile[0]; // Pull the 0th CardProspector
		drawPile.RemoveAt(0); // Then remove it from List<> drawPile
		return(cd); // And return it
	}

	// Convert from the layoutID int to the CardProspector with that ID
	CardProspector FindCardByLayoutID(int layoutID) {
		foreach (CardProspector tCP in tableau) {
			// Search through all cards in the tableau List<>
			if (tCP.layoutID == layoutID) {
				// If the card has the same ID, return it
				return( tCP );
			}
		}
		// If it's not found, return null
		return( null );
	}
	// LayoutGame() positions the initial tableau of cards, a.k.a. the "mine"
	void LayoutGame() {
		// Create an empty GameObject to serve as an anchor for the tableau //1
		if (layoutAnchor == null) {
			GameObject tGO = new GameObject("_LayoutAnchor");
			// ^ Create an empty GameObject named _LayoutAnchor in the Hierarchy
			layoutAnchor = tGO.transform; // Grab its Transform
			layoutAnchor.transform.position = layoutCenter; // Position it
		}

		CardProspector cp;
		// Follow the layout
		foreach (SlotDef tSD in layout.slotDefs) {
			// ^ Iterate through all the SlotDefs in the layout.slotDefs as tSD
			cp = Draw(); // Pull a card from the top (beginning) of the drawPile
			cp.faceUp = tSD.faceUp; // Set its faceUp to the value in SlotDef
			cp.transform.parent = layoutAnchor; // Make its parent layoutAnchor
			// This replaces the previous parent: deck.deckAnchor, which appears
			// as _Deck in the Hierarchy when the scene is playing.
			cp.transform.localPosition = new Vector3(
				layout.multiplier.x * tSD.x,
				layout.multiplier.y * tSD.y,
				-tSD.layerID );
			// ^ Set the localPosition of the card based on slotDef
			cp.layoutID = tSD.id;
			cp.slotDef = tSD;
			cp.state = CardState.tableau;
			// CardProspectors in the tableau have the state CardState.tableau

			cp.SetSortingLayerName(tSD.layerName); // Set the sorting layers

			tableau.Add(cp); // Add this CardProspector to the List<> tableau
		}

		// Set which cards are hiding others
		foreach (CardProspector tCP in tableau) {
			foreach( int hid in tCP.slotDef.hiddenBy ) {
				cp = FindCardByLayoutID(hid);
				tCP.hiddenBy.Add(cp);
			}
		}

		// Set up the initial target card
		MoveToTarget(Draw ());

		// Set up the Draw pile
		UpdateDrawPile();
	}

	// CardClicked is called any time a card in the game is clicked
	public void CardClicked(CardProspector cd) {
		// The reaction is determined by the state of the clicked card
		switch (cd.state) {
		case CardState.target:
			// Clicking the target card does nothing
			break;
		case CardState.drawpile:
			// Clicking any card in the drawPile will draw the next card
			MoveToDiscard(target); // Moves the target to the discardPile
			MoveToTarget(Draw()); // Moves the next drawn card to the target
			UpdateDrawPile(); // Restacks the drawPile
			ScoreManager(ScoreEvent.draw);
			break;
		case CardState.tableau:
			// Clicking a card in the tableau will check if it's a valid play
			bool validMatch = true;
			if (!cd.faceUp) {
				// If the card is face-down, it's not valid
				validMatch = false;
			}
			if (!AdjacentRank(cd, target)) {
				// If it's not an adjacent rank, it's not valid
				validMatch = false;
			}
			if (!validMatch) return; // return if not valid
			// Yay! It's a valid card.
			tableau.Remove(cd); // Remove it from the tableau List
			MoveToTarget(cd); // Make it the target card
			SetTableauFaces(); // Update tableau card face-ups
			ScoreManager(ScoreEvent.mine);
			break;
		}
		// Check to see whether the game is over or not
		CheckForGameOver();
	}

	//AAAAAAAAAAAAAAAA pg 552 "return true if the two  cards.." comes here after the elipsis

	//AAAAAAAAAAAAAAAAG pg 553 "// This turns cards in the Mine face-up or face-down" comes after same elipsis


	// Moves the current target to the discardPile
	void MoveToDiscard(CardProspector cd) {
		// Set the state of the card to discard
		cd.state = CardState.discard;
		discardPile.Add(cd); // Add it to the discardPile List<>
		cd.transform.parent = layoutAnchor; // Update its transform parent
		cd.transform.localPosition = new Vector3(
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID+0.5f );
		// ^ Position it on the discardPile
		cd.faceUp = true;
		// Place it on top of the pile for depth sorting
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortOrder(-100+discardPile.Count);
	}

	// Make cd the new target card
	void MoveToTarget(CardProspector cd) {
		// If there is currently a target card, move it to discardPile
		if (target != null) MoveToDiscard(target);
		target = cd; // cd is the new target
		cd.state = CardState.target;
		cd.transform.parent = layoutAnchor;
		// Move to the target position
		cd.transform.localPosition = new Vector3(
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID );
		cd.faceUp = true; // Make it face-up
		// Set the depth sorting
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortOrder(0);
	}
	// Arranges all the cards of the drawPile to show how many are left
	void UpdateDrawPile() {
		CardProspector cd;
		// Go through all the cards of the drawPile
		for (int i=0; i<drawPile.Count; i++) {
			cd = drawPile[i];
			cd.transform.parent = layoutAnchor;
			// Position it correctly with the layout.drawPile.stagger
			Vector2 dpStagger = layout.drawPile.stagger;
			cd.transform.localPosition = new Vector3(
				layout.multiplier.x * (layout.drawPile.x + i*dpStagger.x),
				layout.multiplier.y * (layout.drawPile.y + i*dpStagger.y),
				-layout.drawPile.layerID+0.1f*i );
			cd.faceUp = false; // Make them all face-down
			cd.state = CardState.drawpile;
			// Set depth sorting
			cd.SetSortingLayerName(layout.drawPile.layerName);
			cd.SetSortOrder(-10*i);
		}
	} //where pg550 text ends
//}

	List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD) {
		List<CardProspector> lCP = new List<CardProspector>();
		CardProspector tCP;
		foreach( Card tCD in lCD ) {
			tCP = tCD as CardProspector; // 1
			lCP.Add( tCP );
		}
		return( lCP );
	}


//CCCCCCCCCCC These lines may be in the wrong place, ctrl F "AAAAAA" for alternate location

// Return true if the two cards are adjacent in rank (A & K wrap around)
public bool AdjacentRank(CardProspector c0, CardProspector c1) {
	// If either card is face-down, it's not adjacent.
	if (!c0.faceUp || !c1.faceUp) return(false);
	// If they are 1 apart, they are adjacent
	if (Mathf.Abs(c0.rank - c1.rank) == 1) {
		return(true);
	}
	// If one is A and the other King, they're adjacent
	if (c0.rank == 1 && c1.rank == 13) return(true);
	if (c0.rank == 13 && c1.rank == 1) return(true);
	// Otherwise, return false
	return(false);
}

// This turns cards in the Mine face-up or face-down
void SetTableauFaces() {
	foreach( CardProspector cd in tableau ) {
		bool fup = true; // Assume the card will be face-up
		foreach( CardProspector cover in cd.hiddenBy ) {
			// If either of the covering cards are in the tableau
			if (cover.state == CardState.tableau) {
				fup = false; // then this card is face-down
			}
		}
		cd.faceUp = fup; // Set the value on the card
	}
}

// BBBBBBBBBBBBBBB pg 554 entry after the final elipsis

// Test whether the game is over
void CheckForGameOver() {
	// If the tableau is empty, the game is over
	if (tableau.Count==0) {
		// Call GameOver() with a win
		GameOver(true);
		return;
	}
	// If there are still cards in the draw pile, the game's not over
	if (drawPile.Count>0) {
		return;
	}
	// Check for remaining valid plays
	foreach ( CardProspector cd in tableau ) {
		if (AdjacentRank(cd, target)) {
			// If there is a valid play, the game's not over
			return;
		}
	}
	// Since there are no valid plays, the game is over
	// Call GameOver with a loss
	GameOver (false);
}

// Called when the game is over. Simple for now, but expandable
void GameOver(bool won) {
	if (won) {
		ScoreManager(ScoreEvent.gameWin); // This replaces the old line
	} else {
		ScoreManager(ScoreEvent.gameLoss); // This replaces the old line
	}
	// Reload the scene in reloadDelay seconds
	// This will give the score a moment to travel
	Invoke ("ReloadLevel", reloadDelay); //1
	// Application.LoadLevel("__Prospector_Scene_0"); // Now commented out
	// Reload the scene, resetting the game

	// EEEEEEEEE  The line below may need to be commented out.
	Application.LoadLevel("__Prospector_Scene_0");
}

void ReloadLevel() {
	// Reload the scene, resetting the game
	Application.LoadLevel("__Prospector_Scene_0");
}

// ScoreManager handles all of the scoring
void ScoreManager(ScoreEvent sEvt) {
	List<Vector3> fsPts;
	switch (sEvt) {
		// Same things need to happen whether it's a draw, a win, or a loss
	case ScoreEvent.draw: // Drawing a card
	case ScoreEvent.gameWin: // Won the round
	case ScoreEvent.gameLoss: // Lost the round
		chain = 0; // resets the score chain
		score += scoreRun; // add scoreRun to total score
		scoreRun = 0; // reset scoreRun
		// Add fsRun to the _Scoreboard score
		if (fsRun != null) {
			// Create points for the Bezier curve
			fsPts = new List<Vector3>();
			fsPts.Add( fsPosRun );
			fsPts.Add( fsPosMid2 );
			fsPts.Add( fsPosEnd );
			fsRun.reportFinishTo = Scoreboard.S.gameObject;
			fsRun.Init(fsPts, 0, 1);
			// Also adjust the fontSize
			fsRun.fontSizes = new List<float>(new float[] {28,36,4});
			fsRun = null; // Clear fsRun so it's created again
		}
		break;
	case ScoreEvent.mine: // Remove a mine card
		chain++; // increase the score chain
		scoreRun += chain; // add score for this card to run
		// Create a FloatingScore for this score
		FloatingScore fs;
		// Move it from the mousePosition to fsPosRun
		Vector3 p0 = Input.mousePosition;
		p0.x /= Screen.width;
		p0.y /= Screen.height;
		fsPts = new List<Vector3>();
		fsPts.Add( p0 );
		fsPts.Add( fsPosMid );
		fsPts.Add( fsPosRun );
		fs = Scoreboard.S.CreateFloatingScore(chain,fsPts);
		fs.fontSizes = new List<float>(new float[] {4,50,28});
		if (fsRun == null) {
			fsRun = fs;
			fsRun.reportFinishTo = null;
		} else {
			fs.reportFinishTo = fsRun.gameObject;
		}
		break;
	}

	// This second switch statement handles round wins and losses
	switch (sEvt) {
	case ScoreEvent.gameWin:
		GTGameOver.text = "Round Over";
		// If it's a win, add the score to the next round
		// static fields are NOT reset by Application.LoadLevel()
		Prospector.SCORE_FROM_PREV_ROUND = score;
		print ("You won this round! Round score: "+score);
		GTRoundResult.text = "You won this round!\nRound Score: "+score;
		ShowResultGTs(true);
		break;
	case ScoreEvent.gameLoss:
		GTGameOver.text = "Game Over";
		// If it's a loss, check against the high score
		if (Prospector.HIGH_SCORE <= score) {
			print("You got the high score! High score: "+score);
			string sRR = "You got the high score!\nHigh score: "+score;
			GTRoundResult.text = sRR;
			Prospector.HIGH_SCORE = score;
			PlayerPrefs.SetInt("ProspectorHighScore", score);
		} else {
			print ("Your final score for the game was: "+score);
			GTRoundResult.text = "Your final score was: "+score;
		}
		ShowResultGTs(true);
		break;
	default:
		print ("score: "+score+" scoreRun:"+scoreRun+" chain:"+chain);
		break;
	}
}
}