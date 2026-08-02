# KISS

Use this document before adding or changing project C# when the task can be solved either directly or by adding new state, configuration, helper abstractions, services, DTOs, events, factories, fallback behavior, or public API.

KISS in this project means one clear owner, one clear runtime path, and the smallest change that solves the current task.

## Decision Checklist

Before writing code, answer these questions from the existing project structure:

- What is the current source of truth for this behavior or data?
- Which runtime owner already starts, updates, saves, loads, displays, or executes this workflow?
- Do nearby owners execute the same or materially overlapping runtime sequence under a different domain name?
- Can the change be made in that owner or an existing extension point?
- Can an existing domain type, command, Unity API, project API, or serialized field already represent the required data?
- Is the added code needed by the current task, or only by a possible future variant?

If the current owner exists, change it directly. Do not add a local mirror, retry path, second entry point, side-channel, or fallback state to work around it.

If multiple specialized owners already execute the same workflow steps, do not add the generalized behavior to the nearest specialized owner. Move the shared sequence to one explicit workflow owner, and keep the specialized owners responsible only for their domain-specific before/after effects.

## Preferred Shape

Prefer these shapes:

- A focused change in the existing owner.
- A small concrete implementation behind an existing interface, base class, command, or `[SerializeReference]` list.
- A constructor dependency or injected dependency for long-lived responsibility.
- A per-call parameter, command, or payload only for data that truly varies per call.
- Existing project and framework APIs over handwritten helpers.

The implementation should be linear and inspectable. A reader should be able to find where the workflow starts, who owns the data, and where the behavior executes without chasing lazy initialization, global access, or fallback repair paths.

## Avoid By Default

Do not add these unless the current task has a demonstrated need:

- Extra fields or serialized options for hypothetical variants.
- Local state that duplicates availability, completion, save/load, quest flags, interaction gating, UI visibility, or runtime ownership.
- New `Registry`, `Pipeline`, `Resolver`, `Context`, `Manager`, `Service`, `Provider`, `View`, factory, DTO, event, helper, or generalized abstraction.
- Lazy initialization, `Ensure...` repair methods, fallback branches, retry binding, or rescue flows that hide a missed owner call.
- Broad public API for future use.
- Static access or singleton-style ownership for runtime behavior.

A change is not KISS if it resolves uncertainty by adding state or plumbing instead of locating the existing owner.

## When Abstraction Is Allowed

Add abstraction only when one of these is true now:

- The subsystem already has a matching polymorphic extension point.
- There is repeated concrete behavior in the current code, and the abstraction removes real duplication without hiding ownership.
- The requested behavior is a general version of workflow steps already implemented by specialized owners, and one shared owner reduces runtime entry points or control paths.
- The current owner cannot represent the responsibility without breaking its contract, and the new boundary makes that ownership clearer.

When adding abstraction, keep the smallest useful boundary. Name it by the domain role it owns, not by generic implementation words.

## Stop Conditions

Stop and explain the tradeoff before implementing when:

- The simple fix conflicts with the existing owner chain.
- The requested change requires a second lifecycle path or duplicated source of truth.
- Preserving serialized values requires a migration path.
- The implementation would need extra files, layers, services, factories, DTOs, events, or generalized helpers that are not required by the current behavior.
