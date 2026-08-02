# Project Structure

## Project Docs

- `VisualStyle.md` is the source for the gold/blue steampunk art deco UI and sprite-generation direction, including UI-kit sprite decomposition, 9-slice expectations, no-text sprite rules, and output folders.

## Unity Asset Naming

Project-owned reusable Unity asset names use `Type Domain Tags` unless a narrower project skill gives a stricter rule.

- `Type` is the concrete asset type, root owner type, or durable content contract. Examples: `Monster`, `ItemConfig`, `CraftReceipt`, `Tile`, `QuestGraph`, `DialogueGraph`.
- `Domain` is the gameplay/content owner or broad category. Examples: `AI`, `Weapons`, `Consumables`, `Level Generation`, `Repair Truck`.
- `Tags` are broad-to-narrow descriptors that make the repeated asset unique. Use PascalCase words separated by spaces.
- A trailing number is allowed only for genuinely numbered variants.

This rule applies to repeated ScriptableObject assets and repeated prefab assets. Keep the type first even when many assets share the same root script or base owner type. Do not move the type to the suffix, collapse all variants to the shared script name, or use concatenated suffix-first names.

Examples:

- `Monster AI Charging Brute.prefab`
- `Monster AI Coward Scout.prefab`
- `ItemConfig Weapons Bow.asset`
- `CraftReceipt Workbench Bandage.asset`

Avoid:

- `ChargingBruteMonster.prefab`
- `CowardScoutMonster.prefab`
- `Bow ItemConfig.asset`
- `Bandage CraftReceipt.asset`
