---
title: Greenfield tower-defense documentation index
type: authoritative documentation coverage map
status: active
updated: 2026-08-04
scope: every requirement from the gameplay documentation conversation
---

# Greenfield tower-defense documentation index

## 1. Статус

Этот пакет проектирует PC 3D tower-defense roguelite с нуля.

Готовый код, текущие classes, scene hierarchy, prefabs, ScriptableObjects и managers:

- не задают architecture;
- не ограничивают naming/ownership;
- не считаются доказательством design;
- будут сопоставляться этой спецификации только в отдельных future implementation tasks.

`GAMEPLAY_REFERENCES.md` используется как источник design-направления, а не как current-code contract.

## 2. Документы пакета

| File | Назначение |
| --- | --- |
| `00_INDEX.md` | Карта всех требований и маршрут чтения |
| `01_GAME_LOOPS_AND_ECONOMIES.md` | Core wave loop, run loop, meta loop и три экономики |
| `02_GAMEPLAY_MATH_AND_MECHANICS.md` | Низкоуровневая математика combat/movement/effects/upgrades/economies |
| `03_DATA_CONTEXTS_AND_FORMATS.md` | Все data contexts, Unity forms, Definitions, runtime, saves, commands/events/read models |
| `04_SERVICES_AND_LIFECYCLES.md` | Необходимые owners/services, interactions, scopes, transactions и lifecycle |
| `05_SCENE_OBJECTS_AND_COMPONENTS.md` | Hypothetical scene, behavior объектов, prefab/MonoBehaviour recipes |
| `06_KISS_SOLID_AND_ENTRY_POINT.md` | KISS, SOLID, ownership, dependency direction и single entry point |

## 3. Coverage matrix всех запросов

| № | Запрос | Authoritative coverage | Статус |
| --- | --- | --- | --- |
| 1 | Прочитать AGENTS, game director, architect, code style | Project workflow sources; KISS/SOLID отражены в `06` | Выполнено как routing, не design dependency |
| 2 | Прочитать gameplay reference | `GAMEPLAY_REFERENCES.md`; greenfield intent в `01` | Использован только как reference direction |
| 3 | Минимальная самая низкоуровневая математика TD PC game | `02`, разделы 3–10, 19, 23–24 | Полностью |
| 4 | Второстепенные mechanics, stats, upgrades, damage types, shields, auras | `02`, разделы 9–14; `03`, разделы 7–8; `05`, actor recipes | Полностью |
| 5 | Экономика в течение одной wave | `01`, разделы 3–8; `02`, раздел 16 | Полностью |
| 6 | Экономика в течение одного run | `01`, разделы 9–12; `02`, раздел 17 | Полностью |
| 7 | Meta-экономика | `01`, разделы 13–14; `02`, раздел 18 | Полностью |
| 8 | Data contexts: Definition/Inspector, Addressables/mods, SaveSession, runtime run, что ещё | `03`, разделы 2–6; explicit SaveSession раздел 4 | Полностью |
| 9 | Systems, которые знают данные и передают их: gameplay/application/VFX/SFX/UI/save | `04`, разделы 5–15 | Полностью |
| 10 | KISS и SOLID | `06`, разделы 1–2 | Полностью |
| 11 | Objects gameplay scene и data links | `05`, разделы 3–7 | Полностью |
| 12 | Single entry point | `04`, раздел 4; `05`, раздел 5; `06`, раздел 3 | Полностью |
| 13 | Hypothetical scene objects и data links | `05`, разделы 3–7 | Полностью |
| 14 | Behavior каждого object с вариантами | `05`, разделы 5–18 | Полностью |
| 15 | Unity data forms, service formats, lifecycles | `03`, разделы 3–6; `04`, разделы 3, 6, 16 | Полностью |
| 16 | Core loop без meta/full run: одна wave, действия, effects | `01`, разделы 3–8 | Полностью |
| 17 | Run loop без meta | `01`, разделы 9–12 | Полностью |
| 18 | Meta loop | `01`, разделы 13–14 | Полностью |
| 19 | Data всех нужных types во всех contexts для future tasks | `03`, разделы 2–17 | Master data contract |
| 20 | Необходимые services и interactions | `04`, разделы 2–20 | Master service contract |
| 21 | Custom MonoBehaviour components gameplay objects | `05`, разделы 2, 8–23 | Master component contract |
| 22 | Tower на дороге: Enemy погибает или ломает; path не блокируется | `01` 5.3; `02` 14; `03` 7.4–7.6; `04` 11.4; `05` 10 | Полностью, cross-cutting |
| 23 | Optional auto-repair между waves или по времени внутри wave | `02` 14.4–14.5; `03` 7.6/8.5; `04` 11.5; `05` 10.5 | Полностью |
| 24 | Flying enemies | `01` 4/6/9; `02` 6–7/10/15; `03` 7.2/7.7–7.8; `04` 9.5/10.3; `05` 11.3 | Полностью, cross-cutting |
| 25 | Не учитывать готовый код, писать docs с нуля | Весь пакет, особенно этот раздел 1 | Выполнено |

## 4. Design pillars

1. Preparation создаёт осмысленное решение.
2. Map topology — часть build.
3. Threat intel приходит до расходов.
4. Combat автоматический, но читаемый.
5. Ground/Flying меняют target/movement decisions, не дублируют game loop.
6. Economy создаёт trade-off, а не выдаёт всё.
7. Run сохраняет последствия, meta расширяет варианты.
8. Horizontal unlock приоритетнее permanent power.
9. UI/VFX/SFX объясняют result, но не решают его.
10. Required errors явны; fallback нет.

## 5. Минимальный продуктовый слой

Greenfield Basic включает:

- one gameplay scene entry;
- finite run of waves;
- Preparation/WaveActive/WaveResolve;
- Ground + простой Flying movement;
- map/route + placements;
- several Tower roles and target filters;
- HP, basic shield/armor/damage types;
- one run currency and one meta currency;
- between-wave save;
- horizontal meta unlocks;
- road-contact Tower/auto-repair только для включённого content.

## 6. Маршрут чтения

### Новая mechanic/balance

`02 Math → 01 Loop context → 03 Data → 04 Owner/service → 05 Component if Unity object`

### Tower/Enemy/Tile/prefab

`05 Scene/components → 03 completeness/data → 02 formulas → 04 dependencies`

### Flying/road-contact/repair

`05 sections 10–11 → 02 sections 6–7/14 → 03 definitions/runtime → 04 interactions`

### Save/meta

`01 run/meta loops → 03 persistence → 04 application/profile/save lifecycle → 06 ownership`

### UI/VFX/SFX

`04 presentation/data flow → 03 ReadModels/Events/Cues → 05 scene presentation objects`

## 7. Source-of-truth policy

Within this greenfield package:

- `01` owns player loops and economy decisions;
- `02` owns formulas/order/units;
- `03` owns data shape/context/persistence;
- `04` owns service boundaries/interactions/lifecycle;
- `05` owns scene/prefab/component topology;
- `06` owns architecture principles and entry/ownership rules.

При overlap specialized document wins in its domain. Например, road-contact formula берётся из `02`, runtime data из `03`, interaction owner из `04`, prefab shape из `05`.

## 8. Неавторитетные материалы для greenfield design

Другие repository docs могут оставаться historical/current-implementation notes. Они не переопределяют этот пакет, если содержат:

- current code names/owners;
- old statuses «implemented/completed»;
- old setup steps;
- tags/singletons/runtime component creation;
- fallback paths;
- assumptions only about Ground enemies.

Future implementation task отдельно строит mapping:

```text
Greenfield contract → current implementation gap → scoped migration/vertical slice
```

Такой mapping не должен загрязнять greenfield docs именами ready code.

## 9. Future task card

```text
Player-facing outcome:
Greenfield source sections:
Basic/Extended/Deferred:
Formula and units:
Definition data:
Runtime owner/state:
Commands/Results/Events/ReadModels:
Scene/prefab/component topology:
Ground/Flying behavior:
Wave/run/meta economy impact:
Save/lifecycle/cancellation:
UI/VFX/SFX feedback:
Failure/no fallback:
Acceptance and verification:
Implementation mapping audit (separate, only when coding starts):
```

## 10. Documentation definition of done

- Every conversation request appears in section 3.
- Every request has one or more precise authoritative sections.
- No greenfield rule depends on ready code.
- Wave/run/meta boundaries are separate.
- Definition/runtime/save/read/presentation contexts are separate.
- Ground/Flying and road-contact/repair are propagated through math/data/services/components.
- Single entry point and one-owner rules are explicit.
- Basic/Extended/Deferred prevent speculative scope.
- Links, Markdown, encoding and EOL validate.

