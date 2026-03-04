# Foundry Framework

Foundry Framework is the Unity-facing layer of the Foundry SDK. It builds on top of **Foundry Core** and provides the higher-level components teams use to ship interactive XR and desktop experiences: prefabs, scene patterns, interaction systems, player rigs, locomotion helpers, editor tooling, and sample content.

> **Prerequisite:** `com.cyberhub.foundry.core` must be installed first. Foundry Framework is designed to sit on top of Core, not replace it.

---

## What this package includes

At a high level, this package adds production-ready implementations around common app needs:

- **Interaction systems** for grabbables, touch, distance grab, place points, hand pose workflows, and look interactions.
- **Locomotion** helpers including teleport and portal systems.
- **Player rigs and services** for desktop/XR control rigs, camera and rig management, and local player menu flows.
- **Navigation and loading** utilities for scene transitions and loading UX.
- **Authentication and account utilities** for player identity-facing behaviors.
- **Editor tooling** for setup flows, version tracking, dev tools, and framework module definitions.
- **Samples** (prefabs/scenes/prototypes) to accelerate onboarding and experimentation.

---

## Package information

- **Package name:** `com.cyberhub.foundry.framework`
- **Display name:** `Foundry Framework`
- **Version:** `0.6.0-preview`
- **Unity version:** `2022.3`

### Key dependencies

- `com.cyberhub.foundry.core` (`0.6.0-preview`)
- `com.unity.inputsystem`
- `com.unity.textmeshpro`
- `com.unity.xr.management`
- `com.unity.xr.openxr`
- `com.unity.nuget.newtonsoft-json`
- `com.unity.addressables`

---

## Installation

Install through Unity Package Manager using one of the following approaches:

1. **Git URL** (recommended for active development).
2. **Embedded/local package** if you are iterating directly in this repository.
3. **Scoped registry** (if your organization publishes the package there).

After adding this package:

1. Ensure **Foundry Core** is installed and version-compatible.
2. Open Unity and let package imports complete.
3. Import samples from the Package Manager as needed.
4. Run the setup utilities in `Editor/Setup` if your project requires framework bootstrapping.

---

## Repository structure

Some primary folders you will likely interact with:

- `Interaction/` – interaction behaviors (grabbing, touching, hands, place points, etc.)
- `Locomotion/` – teleportation and portal systems
- `Player/` – rigs, control implementations, and player services
- `Navigation/` – scene navigation and loading systems
- `Authentication/`, `Account/` – account and auth helpers
- `Editor/` – setup tools, dev tools, and editor-only modules
- `Samples~/` – sample prefabs, prototypes, and scenes
- `Config/` – framework configuration assets/code

---

## Samples

This package exposes the following sample groups:

- **Foundry Core Prefab Samples** (`Samples~/Prefabs`)
- **Prototypes** (`Samples~/Prototypes`)
- **Foundry Core Scene Samples** (`Samples~/Scenes`)

Use these as implementation references and starting points when building new features.

---

## Getting started (quick path)

1. Create or open a Unity `2022.3` project.
2. Install **Foundry Core**.
3. Install **Foundry Framework**.
4. Import one or more samples from `Samples~`.
5. Drop sample prefabs into a scene and review related scripts in the corresponding feature folder.
6. Adapt the patterns into your own project modules.

---

## Current status

Foundry Framework is currently published as a **preview** package (`0.6.0-preview`). APIs, folder structure, and sample content may continue to evolve.

If you are integrating this into production work, pin versions and validate each upgrade against your project requirements.
