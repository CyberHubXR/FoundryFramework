# Foundry Samples 

Foundry provides a lot of sample assets to help you get started. 

## Sample Prefabs
At the moment a lot of the core functionality is best accessed by copying and editing the prefabs from here.

### Scene Setup
The most important prefabs are the ones in scene setup.

The Foundry Persistent prefab is the most important, it has the default setup for all the core context needed to run Foundry.

FoundryUIEventSystem is a UI Event System that works with the foundry input system.

NetworkManager is an example network manager.

### Rigs and Player
Rigs and player contain the two main default interaction rigs. 

The XR_MenuPlayer is a simple rig that is designed to be used for login scenes. Depending on the platform it will either use mouse input or provide raycasters in VR.

The NetworkPlayer is the base rig for both networked and non-networked players. It provides a simple way to move around, interact with objects, and UI.
It also contains a default player, though this is pending a refactor soon.

### Player Utls 
Player/Utils contains a few prefabs for common player utilities, such as distance grabbing, Microphone Muting, Teleport raycasters, UI raycaster, wrist menus, and more.

## Interactables
Interactables contains a bunch prefabs for common interactables, such as buttons, sliders, and toggles. As well as our touch and place point systems.

## Sample Scenes
Sample scenes provides an example offline and online scene.
The offline scene is an empty scene with a play button that will load the online scene.
The online scene is a simple scene with a player prefab and a network manager, and some example objects.

If you have a network provider set up you should be able to try out the networked scene with a friend!

## Sample Materials
The materials needed by the sample scenes
