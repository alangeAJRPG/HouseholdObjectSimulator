# Possession system setup

1. Put `OrbitCamera` on the main camera. It can begin without a target.
2. Create a `PossessionManager` object, then assign that camera component to it. Optionally assign a `Controllable` to **Starting Controllable** to begin the scene possessing it immediately.
3. Add `PossessionSelector` to the same object (or another persistent player object), and assign the camera and manager.
4. Give every possessable object a collider and a `Controllable` component. Put selectable objects on a dedicated `Possessable` layer and use that layer in the selector's **Selectable Layers** field.
5. Add object-specific components beside `Controllable`. Components implementing `IPossessionInputReceiver` receive input only while possessed. Components implementing `IControllableAction` respond to the primary action click. `RigidbodyPossessedMover` is an intentionally simple movement example.

The selector only identifies a `Controllable`; it does not make any assumptions about how that object moves. The manager owns the current object and invokes its lifecycle. The camera follows the `CameraTarget` offered by the interface, which can be a child anchor instead of the object's pivot. This means a small spoon, a tall lamp, and a vehicle can all share the same possession flow while using different movement components and camera anchors.

Right-click toggles possession mode. While it is active, the cursor is visible, camera orbiting is disabled, and a subtle vignette fades in. Left-clicking a `Controllable` locks player input and smoothly moves the camera to the new target. Possession mode automatically closes only after that transition completes. Possessable objects under the mouse use a screen-space outline with a consistent pixel width; the object currently possessed never displays an outline. Outside possession mode, left-click invokes `Action()` on each `IControllableAction` beside the currently possessed object.
