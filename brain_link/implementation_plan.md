# Static Dust System Positioning and Scaling Support

Enable the static `Dust_System` GameObject to be manually repositioned and scaled in the Unity editor to cover the entire environment (defined by the `Walls` game objects), and provide an automatic fitting feature.

## User Review Required

> [!IMPORTANT]
> **Particle System Scaling Mode Change**
> Currently, if you scale a GameObject containing a Particle System, the particles themselves scale up with it (making them look like huge, unnatural blobs). We will set `main.scalingMode = ParticleSystemScalingMode.Shape` in `BunkerDustManager.cs`. This ensures that scaling the parent `Dust_System` GameObject only scales the **emission volume box** (so you can cover the whole room) while keeping the actual dust particles at their normal, tiny, realistic size.
>
> **Auto-Fitting Target Bounds**
> We will add an optional `fitToTarget` field. By dragging the `Walls` parent GameObject (or any environment object) into this field, the script will automatically calculate the bounding box of the walls, position the dust system at the center, and scale the particle system shape to match the volume exactly. This makes setup automatic and instantaneous!

---

## Proposed Changes

### Dust System Core Emitter

#### [MODIFY] [BunkerDustManager.cs](file:///i:/PG%20projects/BunkerDen/Assets/Assets/Scripts/BunkerDustManager.cs)
- Set `main.scalingMode = ParticleSystemScalingMode.Shape` during initialization.
- Add `public GameObject fitToTarget;` to allow auto-fitting environment bounds.
- Implement a `FitToTargetBounds()` method decorated with `[ContextMenu("Fit to Target Bounds")]` so it can be manually triggered via the Inspector component menu.
- In `Update()`, if `followCamera` is false and `fitToTarget` is assigned, run `FitToTargetBounds()` in the Editor (when not in Play Mode) to automatically adjust the dust volume bounds as the user designs.
- In `InitializeSystem()`, if `followCamera` is false, adjust shape position/scale setup dynamically (using `fitToTarget` if available, or keeping the user's manual scale).

---

## Verification Plan

### Manual Verification
1. Open Unity Editor and open the `BunkerScene_v2` scene.
2. Select the `Dust_System` GameObject in the Hierarchy.
3. Uncheck **Follow Camera** on the `BunkerDustManager` component in the Inspector.
4. Drag the **Walls** GameObject from the Hierarchy into the new **Fit To Target** slot of the `BunkerDustManager` component.
5. Verify that the `Dust_System` automatically repositions to the center of the walls and scales up its emission box to cover the entire bunker environment.
6. Alternatively, clear the **Fit To Target** slot and manually change the Position and Scale of the `Dust_System` GameObject. Verify that the emission box matches the scale while the individual particles remain tiny and realistic.
