# Domain Model

This document defines the core game entities.

The game engine should operate primarily on these objects.

---

# GameState

Represents the entire current state of a game.

Responsibilities:

- Current turn
- Active player
- Players
- Galaxy
- Fleets
- Rules
- Victory status

Contains:

- Galaxy
- Players
- Fleets
- TurnManager

---

# Scenario

Represents a complete game definition.

Responsibilities:

- Define galaxy
- Define starting positions
- Define rules
- Define theme

Loaded from:

- galaxy.json
- players.json
- rules.json
- theme.json

---

# Galaxy

Represents the overall map.

Responsibilities:

- Contain sectors
- Contain stars
- Contain warp routes

Contains:

- Sectors
- Stars
- Connections

---

# Sector

Logical grouping of stars.

Responsibilities:

- Organization
- Display
- Scenario rules

Properties:

- Id
- Name
- Color

---

# Star

Represents a star system.

Responsibilities:

- Ownership
- Resource generation
- Fleet location

Properties:

- Id
- Name
- Position
- StarType
- ResourceType
- Owner
- ShipCount

---

# StarType

Defines star characteristics.

Examples:

- Yellow
- Orange

Properties:

- Name
- DisplayColor
- ProductionValue

Future versions may include:

- Habitable
- Industrial
- Research

---

# WarpRoute

Represents a connection between stars.

Properties:

- FromStar
- ToStar
- Distance

Responsibilities:

- Movement calculations

---

# Fleet

Represents a group of ships traveling together.

Properties:

- Id
- Owner
- ShipCount
- Location

Possible Locations:

- Star
- WarpJourney

Responsibilities:

- Travel
- Combat participation

---

# WarpJourney

Represents a fleet in transit.

Properties:

- FleetId
- OriginStar
- DestinationStar
- TotalDistance
- RemainingDistance

Responsibilities:

- Hidden movement
- Arrival processing

---

# Player

Represents a human or AI player.

Properties:

- Id
- Name
- Color
- IsAI

Responsibilities:

- Ownership
- Production
- Orders

---

# TurnManager

Controls game flow.

Responsibilities:

- Turn order
- Order resolution
- Movement resolution
- Production resolution
- Combat resolution

Typical Flow:

1. Issue Orders
2. Resolve Movement
3. Resolve Arrivals
4. Resolve Combat
5. Resolve Production
6. Check Victory
7. Advance Turn

---

# ProductionRule

Controls ship generation.

Responsibilities:

- Determine production
- Apply bonuses

Examples:

- Classic production
- Expanded economy

---

# CombatRule

Determines battle outcomes.

Responsibilities:

- Resolve engagements
- Produce battle results

Examples:

- Classic
- Dice
- Modern

---

# VictoryRule

Determines game end conditions.

Examples:

- Last player standing
- Territory control
- Point accumulation

---

# Theme

Defines presentation assets.

Properties:

- Board image
- Fleet images
- Fonts
- Colors
- Audio resources

Responsibilities:

- Presentation only

No gameplay logic should exist in Theme.

---

# JSON Configuration Files

## galaxy.json

Contains:

- Sectors
- Stars
- Warp routes

## players.json

Contains:

- Players
- Starting assets
- Starting locations

## rules.json

Contains:

- Combat rules
- Production rules
- Victory rules
- Movement rules

## theme.json

Contains:

- Visual resources
- Color palette
- Audio resources

## scenario.json

Contains references to all scenario assets.

Example:

Scenario
 ├── galaxy.json
 ├── players.json
 ├── rules.json
 └── theme.json