# How the player works
A high level overview of where everything is

## Prefabs
There are three main prefabs that the player systems uses, two different control rigs, and the networked player.

### The persistant prefab 
This isn't explicitaly a player prefab, but the player relies on it. It contains a group of objects and scripts that get set with DoNotDestroyOnLoad() and handle things like loading screens, the player camera, input bindings, etc. 
It's important to have an instance of this prefab in the starting scene of you app so it can ride along to all the others.

### Control rigs
1. DesktopRig
2. XRRig 

Depending on if an XR runtime was found or not one of the two prefabs are created, they contain the scripts and behaviours needed for the chosen mode to provide ik targets and movement input.
It's woth noting that neither of these prefabs contain a camera, instead when they are created they parent the camera in the persistant prefab to themselves. 
(This is a work around for steam audio requiring an audio listener to be present on scene start, but may be useful for other things as well)

### Interaction rigs
1. Menu Player (two raycasters and mouse support on desktop)
2. OfflinePlayer
3. NetworkedPlayer - (default for most things, a networked player with an avatar and interaction)

Interaction is the best way to describe these, thing of a control rig as a spirit and an interaction rig as a body. Control rigs survive from scene to scene but Interaction rigs are tied to the lifetime of a scene in most cases. 

When an interaction rig is crerated it parents the currently active control rig to itself and uses it as a source of input. 

Something to note is that most of our systems don't rely on transform references, instead positions are passed through code (this makes it a lot easier to network the variables later down the line). 

This may change since the whole system is due for a refactor soon.