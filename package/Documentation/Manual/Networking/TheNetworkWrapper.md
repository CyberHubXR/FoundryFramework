# The Network Wrapper
Foundry is built in such a way that it can theoretically support any network backend given the right backend plugin. This page focuses not on how to write a backend plugin, but rather how the netcode on the foundry side works to allow for this, and how all the parts work together.

## The Network State
The state graph is the core of the network wrapper. It is a directed graph that represents the state of the network.