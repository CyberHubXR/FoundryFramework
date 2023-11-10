# Spatial Grabbable 
@"Foundry.SpatialGrabbable"

![SpatialDoor](/Media/Manual/Interaction/SpatialGrabbableScript.png)

## Description
A script for making an object grabbable.

## Usage
This is one of the easier scripts to use, simply add it to a game object with a collider and it will be grabbable. 

If you want this object to work with physics, add a rigidbody and the script will detect that.

OnGrab and OnRelease are called when the object is grabbed and released respectively. If this object is networked, these functions will be called on all clients, so an ownership check using the an attached network object may be needed to prevent unwanted behavior.

On OnFirstHighlight and OnFinalStopHighlight are called when a hand hovers within grabbing range of this object, this can be used for highlighting the object or other effects.

You may restrict the grabbable to only be grabbable by a certain hand by setting the Hand Type field, you can also temporarily disable grabbing by setting the isGrabbable property to false.

## Networking
To network the object add a @"Foundry.Networking.NetworkObject" and @"Foundry.Networking.NetworkTransform" to the same game object as the grabbable script.

## Examples
[Interactable Grabbable Example](~/Manual/GettingStarted/Samples/Interactables/InteractableGrabbableExample.md)