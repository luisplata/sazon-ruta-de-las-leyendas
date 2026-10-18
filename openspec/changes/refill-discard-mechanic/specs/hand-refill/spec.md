# Delta for hand-refill

## Purpose

Refill becomes a **hand-cycle tool**: it discards the **entire hand** (cards fly LEFT to the
discard target), then **re-deals to `config.maxHandSize`**; pot (center) cards stay.
**[Rule]** = pure testable rule; **[Presentation]** = Unity animation/HUD.

## ADDED Requirements

### Requirement: Refill Discards The Entire Hand

Refill SHALL remove every active-hand card and queue each for discard, even with an empty corner.

#### Scenario: [Rule] Whole hand leaves

- GIVEN a hand of N > 0, budget remaining
- WHEN Refill is triggered
- THEN all N cards leave the hand and one use is consumed

#### Scenario: [Presentation] Fly left then vanish

- GIVEN a card leaving the hand
- WHEN its discard animation plays
- THEN it eases left to the discard target and scales to 0 before destruction

### Requirement: Pot Cards Are Never Discarded

Center/pot parked cards SHALL NOT be discarded or moved by Refill.

#### Scenario: [Rule] Pot intact

- GIVEN K cards parked in the pot
- WHEN Refill is triggered
- THEN those K cards remain untouched

### Requirement: Full Hand Re-Dealt Via Existing Draw Cursor

After discard, Refill SHALL re-deal to `config.maxHandSize` via the existing wrap-around cursor
and SHALL NOT re-shuffle mid-game.

#### Scenario: [Rule] Redeal to full, cursor advanced

- GIVEN Refill discards N cards
- WHEN discard completes
- THEN the hand returns to `maxHandSize`, the cursor advances per card, and no reshuffle occurs

### Requirement: Refill Budget

`_refillsUsed` SHALL increment once per Refill that discards at least one card (hand or corner),
and Refill SHALL be a no-op at `_refillsUsed >= config.maxRefillsPerGame`.

#### Scenario: [Rule] Empty press is free

- GIVEN hand = 0 and corner = 0
- WHEN Refill is triggered
- THEN nothing is discarded/dealt and `_refillsUsed` is unchanged

#### Scenario: [Rule] Budget exhausted

- GIVEN `_refillsUsed == maxRefillsPerGame`, full hand
- WHEN Refill is triggered
- THEN the hand is unchanged and no use is consumed

### Requirement: Re-Entrancy Guard

While a Refill is in progress, further presses SHALL be ignored.

#### Scenario: [Rule] Double-click ignored

- GIVEN a Refill routine is running
- WHEN Refill is triggered again
- THEN no second discard/redeal occurs and one use is consumed

### Requirement: Corner/Trash Flush Composition

If the corner holds parked cards, Refill SHALL flush them within the same action and single use.

#### Scenario: [Rule] Empty hand + parked corner

- GIVEN hand = 0 and corner >= 1
- WHEN Refill is triggered
- THEN the corner is flushed, one use consumed, hand re-dealt to full

### Requirement: Discard Completes Before Redeal

The new hand SHALL be dealt only after all discard animations complete; a mid-routine scene end
SHALL abort without a partial hand.

#### Scenario: [Presentation] Ordering / safe interrupt

- GIVEN discard animations are running
- WHEN they have not finished
- THEN no new card is dealt, and a mid-routine scene unload deals no partial hand

### Requirement: Refill Label And Tooltip Reflect Hand-Discard

The refill label SHALL show uses remaining/used, refreshed on consume; "trash dump" tooltips
SHALL be updated to hand-discard.

#### Scenario: [Presentation] Label reflects consume

- GIVEN a real Refill consumes a use
- WHEN the label renders
- THEN it shows the updated count and no tooltip calls Refill a trash-only dump

## MODIFIED Requirements

### Requirement: Refill Discard Semantics

Refill SHALL discard the whole hand plus parked corner cards, consuming one use per real
discard; an empty press SHALL be a no-op.
(Previously: Refill drained only the corner and topped up the hand; an empty-trash Refill was
free and the hand was never discarded.)

#### Scenario: [Rule] Real discard consumes, empty press free

- GIVEN hand = 0 and corner = 0
- WHEN Refill is triggered
- THEN nothing is discarded/dealt/consumed
- AND with any hand or corner card, one use is consumed and the hand re-deals to full
