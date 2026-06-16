# Scene Transition

Scene transitions now live in the game layer and are configured by `SceneTransitionGraph_SO`.
Scene positions are stored as reusable `SceneTransitionPoint_SO` assets.

## Runtime Flow

1. `SceneTransitionRegisterNode` is installed through the existing `ConfigInstaller`.
2. The register node calls `SceneTransitionSystem.Initialize(transitionGraph)`.
3. `SceneTransitionTrigger2D` stores only an `edgeId`.
4. `SceneTransitionSystem.TransitionAsync(traveler, edgeId)` resolves the edge, unloads the current gameplay scene, loads the target scene additively, sets it active, waits for `MapGridRuntimeLoader`, then places the traveler at:

```text
MapGridManager.GetCellCenterWorld(edge.toPoint.cell) + edge.toPoint.worldOffset
```

`toPoint.cell` is the logical authority. `toPoint.worldOffset` only fine tunes entity placement.

## Setup

- Create `SceneTransitionPoint_SO` assets for each named point in a scene.
- Add or edit edges in `SceneTransitionGraph` by assigning `fromPoint` and `toPoint`.
- `edgeId` is generated as `{fromPoint.displayName}_to_{toPoint.displayName}`.
- Point scene fields use `[WSScene]`.
- Put `MapGridRuntimeLoader` in every transition target scene and assign both `MapGridData_SO` and `Grid`.
- Put `SceneTransitionTrigger2D` on trigger colliders and fill `edgeId` manually.

The old `SceneTransitionConfig`, `Route`, `TargetSpawnId`, `SceneSpawnRoot`, and trigger PropertyDrawer flow is removed.
