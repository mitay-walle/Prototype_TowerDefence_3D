# Project Polymorphic Abstractions

Read this before adding a new abstraction, enum branch, switch dispatcher, side-channel interface, or parallel service around one of these systems.

Rule of thumb: if the behavior belongs to an existing owner below, extend that owner's polymorphic contract first. Add a separate abstraction only when the current owner cannot represent the responsibility without breaking its contract.

## Entity Aggregate

Primary files:

- `Assets/Scripts/Entities/Entity.cs`
- `Assets/Scripts/Entities/EntityComponent.cs`
- `Assets/Scripts/Entities/EntitySaveDTO.cs`
- `Assets/Scripts/Entities/EntityComponentSaveDTO.cs`

`Entity` is the aggregate root for GameObject-based runtime actors that need initialization, activation, tags, and save/load. It is sealed by design. Variation belongs in child `EntityComponent` implementations, not in `Entity` subclasses.

`Entity.Initialize()` owns the component lifecycle in this order: collect child `EntityComponent`s, validate IDs, call `Initialize()`, pass matching `EntityComponentSaveDTO` through `Load(...)`, then call `Activate()`.

When adding entity behavior:

- Create or extend an `EntityComponent` when the behavior is part of an entity aggregate.
- Keep entity-owned persistence in `EntityComponent.Save()` and `EntityComponent.Load(...)` using `EntityComponentSaveDTO` or a subclass.
- Do not make an entity component independently `ISaveable` for data that belongs to the entity aggregate.
- Do not add entity-type enums or special-case dispatch in `Entity`; let components carry variation.

## Interactive Entity

Primary files:

- `Assets/Scripts/Components/InteractableObjects/InteractiveEntity.cs`
- `Assets/Scripts/Components/InteractableObjects/Interactor.cs`
- `Assets/Scripts/UI/HUD/InteractTips.cs`

`InteractiveEntity` is the main interaction polymorphic base. It owns outline state, availability, the localized prompt fallback, and the abstract `Interact(Interactor interactor)` operation. Concrete interactive objects such as items, containers, traders, NPCs, workbenches, quest flags, and events should stay behind this base.

When adding interaction behavior:

- Prefer a new `InteractiveEntity` subclass or a small extension to the existing `InteractiveEntity -> Interactor -> InteractTips` flow.
- Put interaction availability in `IsAvailable()` when it is entity-specific.
- Put interaction execution in `Interact(...)`.
- Put prompt defaults in `DefaultInteractionPromptKey` or the serialized prompt key, not in per-feature UI side channels.
- Do not add trade-, quest-, item-, or NPC-specific prompt plumbing when the shared interaction chain can carry the data.

## Item And ItemComponent

Primary files:

- `Assets/Scripts/Entities/Items/Item.cs`
- `Assets/Scripts/Entities/Items/ItemComponent.cs`
- `Assets/Scripts/Entities/Items/Components/**`

`Item` is serialized runtime/save data for inventory items. Its polymorphic extension point is `[SerializeReference] List<ItemComponent> Components`. Item behavior is discovered by component type or by interfaces implemented by item components, such as `IUsable`, `IEquipable`, and `ITradable`.

When adding item behavior:

- Add a focused `ItemComponent` subclass when behavior travels with an item instance.
- Add a narrow item-component interface only when a caller genuinely needs a capability query across many item components.
- Keep disposable runtime cleanup in `ItemComponent.Dispose()` when the component owns temporary runtime state.
- Do not add item category enums, item-id switch logic, or special cases in container/UI code for behavior that can live on the item component.

## Containers And Commands

Primary files:

- `Assets/Scripts/Entities/Containers/Container.cs`
- `Assets/Scripts/Entities/Containers/IContainerCommand.cs`
- `Assets/Scripts/Entities/Containers/Commands/**`

`Container` is both save data and the slot owner. Mutating operations are represented by `IContainerCommand`; `IContainerCommands.ExecuteCommand(...)` executes the command, logs by concrete command type name, and invokes change notifications.

When adding container mutation:

- Add a concise command class such as `AddAmount`, `ClearSlot`, `TransferAmount`, or `Use`.
- Keep the mutation and validation inside `IContainerCommand.Execute()`.
- Prefer command composition over adding one public mutation method per use case to `Container`.
- Do not add `Operation`, `Action`, `Command`, or `Handler` suffixes to command names unless an existing contract requires that shape.

## SaveData And Save Ownership

Primary files:

- `Assets/Scripts/Saving/SaveData.cs`
- `Assets/Scripts/Saving/ISaveable.cs`
- `Assets/Scripts/Saving/SceneSaveState.cs`
- `Assets/Scripts/Saving/GameSaveState.cs`

`SaveData` is the persisted data base class. `ISaveable` owns save/load behavior, while `SaveData` subclasses should remain data containers with `SpawnSceneObject()` for scene-object restoration where needed.

Common branches include entity saves, entity-component saves, containers, item/interactable saves, settings, wallet/trading state, combat/state save data, and spawner saves.

When adding persisted state:

- Use a `SaveData` subclass when data must live in the project save graph.
- Keep logic out of save data beyond construction/default values and the existing restoration hook.
- Reuse `EntityComponentSaveDTO` for entity component persistence.
- Do not create parallel `SaveDTO`, `SaveState`, wrapper, bridge, or migration types when `SaveData` already represents the contract.

## AI State Machine

Primary files:

- `Assets/Scripts/AI/State Machine/Runtime/State.cs`
- `Assets/Scripts/AI/State Machine/Runtime/Conditions/Condition.cs`
- `Assets/Scripts/AI/State Machine/Runtime/Nodes/EventNode.cs`
- `Assets/Scripts/AI/State Machine/Runtime/Blackboard/BlackboardVariable.cs`
- `Assets/Scripts/AI/Sensors/ISensor.cs`

The AI graph is built from several `[SerializeReference]` polymorphic families: `State`, `Condition`, `Event`, `BlackboardVariable`, and sensor implementations. The graph/runtime/editor code already knows how to select and execute those concrete types.

When adding AI behavior:

- Add a concrete `State` for a new node behavior.
- Add a concrete `Condition` for transition checks.
- Add a concrete `Event` for event-driven graph transitions.
- Add a blackboard variable type only when the existing generic variable path cannot hold the data cleanly.
- Add an `ISensor` implementation when perception varies by sensor behavior.
- Do not add state-machine enum branches, hardcoded graph node checks, or component-specific runtime dispatch outside these families.

## StoryGraph Conditions And Actions

Primary files:

- `Assets/Scripts/StoryGraph/Runtime/StoryAction.cs`
- `Assets/Scripts/StoryGraph/Runtime/StoryCondition.cs`
- `Assets/Scripts/StoryGraph/Runtime/StoryNode.cs`
- `Assets/Scripts/StoryGraph/Runtime/DialogueChoiceOption.cs`
- `Assets/Scripts/UI/ModalDialogs/ModalDialogChoice.cs`
- `Assets/Scripts/SceneVariants/SceneVariant.cs`

Story, dialogue, modal choices, and scene variants use `[SerializeReference]` lists of `StoryCondition` and `StoryAction`. This is the expected extension point for quest gates, dialogue choices, scene-variant gates, and story side effects.

When adding story behavior:

- Add a `StoryCondition` when content needs a reusable boolean gate.
- Add a `StoryAction` when content needs a reusable side effect.
- Keep dependencies flowing through `StoryExecutionContext` and `StoryActionExecutor` rather than hidden service lookup.
- Do not add quest/dialogue/scene-specific switch branches when a reusable condition or action can represent the behavior.

## Stats Active Effects

Primary files:

- `Assets/Scripts/Stats/IActiveEffect.cs`
- `Assets/Scripts/Stats/IActiveEffectDefinition.cs`
- `Assets/Scripts/Stats/ActiveEffectFactory.cs`
- `Assets/Scripts/Stats/StatsRuntimeOwner.cs`

Stats runtime effects are split into definitions, runtime effect instances, and save factories. `StatsRuntimeOwner` serializes `ActiveEffectFactory` implementations, and active effects expose apply/tick/remove/save behavior through `IActiveEffect`.

When adding stat effects:

- Add an `IActiveEffectDefinition` when authoring data needs to decide if an effect can apply and create a runtime effect.
- Add an `IActiveEffect` implementation for runtime behavior.
- Add an `ActiveEffectFactory` when saved active effects need restoration by type id.
- Do not encode effect behavior as stat-id branches inside the runtime owner.

## Smaller Existing Extension Points

Use these when the task is clearly in their domain, but do not inflate them into generic project-wide abstractions:

- `TradeOperation`: executable trade-flow command objects such as `Sell` and `BuyBack`; keep concrete names concise, put operation-specific validation/mutation in the concrete type, pass concrete dependencies directly through constructors, keep `Execute()` parameterless like `IContainerCommand.Execute()`, and do not use singleton instances, context-wrapper classes, or broad runtime-state arguments for operation objects.
- `TraderStockGenerationEntry`: trader stock generation entries in preset pools.
- `IDamageTriggerPattern`: reusable damage trigger patterns for `DamageTrigger`.
- `AttackPerformer`: polymorphic combat attack execution.
- `ZoneEffectBase`: zone-specific environmental effects.
- `GameMode`: game-loop mode behavior.
- Character Controller Pro abstractions under `Assets/Scripts/Entities/Player/Control/CharacterControllerPro/**`: treat them as imported/legacy-style local framework code; follow existing local patterns when touching that area.
