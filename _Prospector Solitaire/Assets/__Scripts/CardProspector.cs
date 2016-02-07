using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum CardState
{
	drawpile, 
	tableau, 
	target, 
	discard
}

public class CardProspector : Card
{
	public CardState state = CardState.drawpile;
	public List<CardProspector> hiddenBy = new List<CardProspector>();
	public int layoutID;
	public SlotDef slotDef;

//AAAAA page 549   AAAAAAAAAAAA

	// This allows the card to react to being clicked
	override public void OnMouseUpAsButton() {
		// Call the CardClicked method on the Prospector singleton
		Prospector.S.CardClicked(this);
		// Also call the base class (Card.cs) version of this method
		base.OnMouseUpAsButton();
	}
}