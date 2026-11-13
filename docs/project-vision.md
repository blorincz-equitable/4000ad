# 4000 A.D. Digital Recreation

## Project Vision

A digital recreation of the classic board game 4000 A.D., created primarily for personal and family enjoyment.

The initial release will faithfully reproduce the original game mechanics, board layout, star systems, warp movement system, and visual appearance.

Future releases may introduce optional enhancements such as additional combat systems, expanded economics, technology trees, AI opponents, and custom galaxy generation.

The architecture should be data-driven from the beginning so that game content can evolve without requiring significant code changes.

---

# Core Goals

## Version 1 Goals

- Recreate the original board game experience.
- Use the original board layout and star locations.
- Support 2-4 players.
- Implement hidden warp movement.
- Implement classic deterministic combat.
- Implement original production mechanics.
- Allow local hot-seat play.

## Long-Term Goals

- Alternate combat systems.
- Enhanced resource management.
- Multiple scenarios.
- AI opponents.
- Scenario editor.
- Custom galaxies.
- Replay system.
- Statistics and game history.

---

# Technology Stack

## Engine

Godot 4

## Language

C#

## Data Storage

JSON

Future optional migration:

- SQLite
- Save-game persistence
- Campaign data
- Statistics

## Development Tools

- Passenger IDE
- GitHub
- GitHub Copilot

---

# Architectural Principles

## Data Driven

All game content should be loaded from data files.

The game engine should never hardcode:

- Stars
- Sectors
- Warp routes
- Resources
- Starting positions
- Combat rules
- Production values
- Victory conditions

## Separation of Concerns

Game logic and UI must remain separate.

The engine should operate entirely on data structures.

UI layers should observe and render game state.

## Scenario Based Design

A scenario defines:

- Galaxy
- Rules
- Players
- Victory Conditions
- Theme

The original 4000 A.D. board is implemented as a scenario.

Future scenarios should require no engine changes.

---

# Initial Scenario

Name:

Classic 4000 A.D.

Features:

- Original board layout
- Original yellow stars
- Original orange stars
- Original warp system
- Original production rules
- Original combat rules
- Original victory conditions

---

# Future Rulesets

## Classic Combat

Deterministic combat.

Example:

10 ships vs 6 ships

Winner survives with 4 ships.

## Dice Combat

Randomized combat.

Example:

Each ship rolls a d6.

Configurable hit values.

## Modern Combat

Support:

- Ship classes
- Attack values
- Defense values
- Special abilities

Possible units:

- Scout
- Destroyer
- Cruiser
- Carrier
- Battleship

---

# Expanded Economy

Potential future resources:

- Industry
- Energy
- Population
- Research

Potential future systems:

- Maintenance
- Logistics
- Trade routes
- Infrastructure

---

# AI Roadmap

## Basic AI

- Expand
- Produce ships
- Attack nearby weaknesses

## Intermediate AI

- Threat analysis
- Defensive positioning
- Strategic production

## Advanced AI

- Alliance behavior
- Long-term planning
- Predictive movement

---

# UI Philosophy

The game should feel like a living version of the original board game.

The original board aesthetic should be preserved.

Primary visual goals:

- Original board colors
- Original star colors
- Original warp lanes
- Original fleet markers

Future themes should be swappable through configuration.

---

# Recommended Development Phases

## Phase 1

Board Rendering

Deliverables:

- Load scenario
- Display board
- Display stars
- Display sectors
- Zoom and pan

## Phase 2

Movement

Deliverables:

- Create fleets
- Select destinations
- Warp movement
- Turn progression

## Phase 3

Production

Deliverables:

- Ownership
- Resource generation
- Ship production

## Phase 4

Combat

Deliverables:

- Fleet battles
- Star invasions
- Combat reports

## Phase 5

AI

Deliverables:

- Single computer opponent

## Phase 6

Advanced Features

Deliverables:

- Alternate combat modes
- Replay system
- Scenario editor
- Multiple galaxies