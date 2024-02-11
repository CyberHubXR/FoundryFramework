# How the player works
A high level overview of where everything is

## Prefabs
There are two main prefabs that the player systems uses, each for a different control scheme (XR, Desktop).

### The persistant prefab 
This isn't explicitly a player prefab, but the player relies on it. It contains a group of objects and scripts that get set with DoNotDestroyOnLoad() and handle things like loading screens, the player camera, input bindings, etc. 
It's important to have an instance of this prefab in the starting scene of you app so it can ride along to all the others.

### Control rigs
1. DesktopRig
2. XRRig 

Depending on if an XR runtime was found or not one of the two prefabs are created, they contain the scripts and behaviours needed for the chosen mode to provide ik targets and movement input.
It's woth noting that neither of these prefabs contain a camera, instead when they are created they parent the camera in the persistant prefab to themselves. 
(This is a work around for steam audio requiring an audio listener to be present on scene start, but may be useful for other things as well)

### Interaction rigs
An interaction rig is a rig that "borrows" the control rig, parents it to itself and uses it as a source of input. And 
as the name implies, it provides a way of interacting with the scene.

We have a couple of example rigs in samples showing off how all the different scripts we have for players work together.
We suggest choosing the one that best fits and modifying it to suit your needs.
1. Menu Player (two raycasters and mouse support on desktop)
2. OfflinePlayer
3. NetworkedPlayer - (default for most things, a networked player with an avatar and interaction)

Something to note is that since the control rigs aren't part of the interaction rig prefab, you usually wouldn't be able to 
follow a normal game object based approach to referencing the controllers/head. But we have networked "clones" of the head
and hand trackers that have updated positions and rotations that you can use instead. When one of these trackers is active 
the game object will be enabled, otherwise, as in the case of the desktop rig's hands, or an non-full-body tracking setup 
some will be disabled. Also note that sometimes we enable a tracker that was once disabled and vice-versa as sometimes the 
desktop rig will emulate controllers or the XR rig will switch between fbt and 3point tracking.