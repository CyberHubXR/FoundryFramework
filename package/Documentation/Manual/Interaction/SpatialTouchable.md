# Spatial Grabbable 
@"Foundry.SpatialTouchable"

![Spatial Touchable Script](/Media/Manual/Interaction/SpatialTouchableScript.png)

## Description
A script for making an object detect when it's touched. Also the basis for a lot of our gizmo scripts. 

## Usage
Add to an object with a collider and that's about it.

Touch start delay determines how long a player needs to touch this before events trigger.

OnTouchStart and OnTouchEnd are called when a player starts and stops touching this object respectively. (Start and OnTouchStay delayed by the touch start delay)

If you are using a touch start delay and want to show something like a progress bar for when the touch will be activated you can use the touchTriggerPercent property to get how close the script is to calling events. 

## Networking
To network the object add a @"Foundry.Networking.NetworkObject", then events will trigger over the network.