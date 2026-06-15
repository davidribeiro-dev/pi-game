# pi-game

[![License: All Rights Reserved](https://img.shields.io/badge/license-All%20Rights%20Reserved-lightgrey.svg)](#license)
[![Last commit](https://img.shields.io/github/last-commit/davidribeiro-dev/pi-game)](https://github.com/davidribeiro-dev/pi-game/commits/Main)
[![Top language](https://img.shields.io/github/languages/top/davidribeiro-dev/pi-game)](#tech-stack)
[![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-000?logo=unity)](https://unity.com/)

> Copyright © 2024–2026 David Ribeiro (Pi Studios). All rights reserved.

A Unity game project that evolved across three versions — from a tutorial-driven tabletop tank prototype, to a multiplayer tag game built using Photon Engine, to **Vertigo**, a from-scratch rebuild using Unity netcode with an advanced movement system.

## Contents

- [Overview](#overview)
- [Versions](#versions)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Open in Unity](#open-in-unity)
- [Status](#status)
- [Author](#author)
- [License](#license)

## Overview

Pi Game began as a guided YouTube walkthrough — a single-machine tabletop tank game built to learn Unity fundamentals. Over two later versions it became a multiplayer tag game, and, in the latest iteration (**Vertigo**), a from-scratch rebuild with a real networking stack and a fluid movement system. This repository preserves all three versions side-by-side as a record of how the project, and my skills as a developer, evolved.

## Versions

| Version | What it is | Status |
| --- | --- | --- |
| **v2.0** | Tabletop tank game. Started from a YouTube walkthrough and extended with original features. First Unity project. | Archived |
| **v3.0** | First attempt at a multiplayer tag game, built using **Photon Engine**. Networking did not fully stabilize. | Archived |
| **v3.0 Vertigo** | Full rebuild using **Unity's netcode**. Fluid advanced movement and proper tag mechanics. UI still in progress. | Active reference |

## Features

### Vertigo — Movement system

Built around a charge-based design where dash energy can be spent on advanced traversal options.

- Walk / Sprint
- Jump / Double Jump
- Dash / Air Dash (chargeable; spendable on other moves)
- Wall Run (horizontal along walls, reduced gravity)
- Wall Jump
- ODM / Hook — grappling, zipping, swinging
- *Vault / Slide* — planned, not yet implemented

### Vertigo — Multiplayer

- Local and networked multiplayer using Unity's built-in netcode.
- Tag mechanic with synced state between players.
- Initial spawn map and scene wired up for first release.

### v3.0

- Multiplayer tag prototype built using Photon Engine.
- Tag system, multiplayer UI, and basic map geometry.
- Network sync did not fully stabilize — the driver behind the Vertigo rebuild.

### v2.0

- Tabletop tank game built from a YouTube tutorial.
- Extended with personal features beyond the tutorial.

## Tech Stack

| Layer      | v2.0  | v3.0          | Vertigo        |
| ---------- | ----- | ------------- | -------------- |
| Engine     | Unity | Unity         | Unity          |
| Language   | C#    | C#            | C#             |
| Networking | —     | Photon Engine | Unity netcode  |

## Project Structure

```
pi-game/
├── v2.0/                 # Tabletop tank (first Unity project)
├── v3.0/                 # Multiplayer tag, Photon
├── v3.0-vertigo/         # Rebuild on Unity netcode (UI in progress)
├── .gitignore            # Unity-aware
└── README.md
```

## Open in Unity

Each version is a self-contained Unity project. To open one:

1. Clone the repository:
```bash
   git clone https://github.com/davidribeiro-dev/pi-game.git
```
2. Open **Unity Hub** → **Open** → **Add project from disk**.
3. Select the version folder you want — e.g., `v3.0-vertigo/`.
4. Unity Hub will prompt for the editor version if it's missing. Install and open.

## Status

- **v2.0** and **v3.0** are archived as references.
- **Vertigo** is the most recent and most refined version. All planned movement and multiplayer mechanics are functional. UI is in progress; Vault / Slide is planned but not implemented.

## Author

Developed solo under the **Pi Studios** moniker by **David Ribeiro** ([@davidribeiro-dev](https://github.com/davidribeiro-dev)) during the Computer Information Systems program at Blue Hills Regional Technical.

## License

Source code in this repository is © 2024–2026 David Ribeiro (Pi Studios). All rights reserved. No reuse without permission.

