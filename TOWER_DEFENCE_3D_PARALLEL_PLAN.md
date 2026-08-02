---
title: Prototype Tower Defence 3D
type: project-specific multi-chat plan
status: planning
scope: 3D only
---

# Prototype Tower Defence 3D

План проектирования и реализации собственных 3D tower-defense систем для нескольких параллельных чатов.

Этот файл содержит только project-specific решения, границы, зависимости и результаты. Общие правила проекта не копируются сюда.

## Нормативные источники

- AGENTS.md — общие правила проекта, архитектура, Unity-проверки и ограничения изменений.
- .codex/skills/unity-mcp-skill/SKILL.md — маршрутизация операций через Unity/MCP.
- .codex/skills/mcp-unity-find-in-file/SKILL.md — поиск по Unity-файлам.
- .codex/skills/mcp-unity-validate-script/SKILL.md — валидация изменённых C#-скриптов.
- .codex/skills/unity-recompile-menuitem/SKILL.md — принудительная перекомпиляция и Console, если она необходима.
- .codex/skills/prefab-creation/SKILL.md — создание и проверка prefab.
- .codex/skills/test-writing/SKILL.md — написание и запуск Unity-тестов.
- .codex/skills/apply-patch/SKILL.md — convention-aware редактирование текстовых файлов.

Чат обязан читать AGENTS.md и task-specific skills из своей карточки. Их содержание не дублируется в этом плане.

## 1. Целевой результат

Реализовать минимальный 3D vertical slice на базе текущего проекта:

- одна 3D карта;
- один путь врагов;
- одна 3D башня;
- один 3D враг;
- один projectile;
- одна волна;
- размещение башни;
- урон, смерть и награда;
- победа, game over и повторный запуск.

Новые системы реализуются только при наличии потребности проекта.

## 2. Объём

Входит:

- 3D Grid как базовая sandbox-сцена для проверки уровня и движения;
- GridAutopath только при необходимости текущей карте;
- проектирование Entity, capabilities, behaviours, projectiles, rewards, abilities, effectors и technologies внутри текущей архитектуры;
- точечная интеграция только после проверки owner chain.

Не входит:

- отдельные GameManager, RoundManager, SaveManager, pathfinding, projectile pool или input system;
- внешние пакеты как обязательную основу.

## 3. Исходные факты проекта

Проект: Prototype Tower Defence 3D, Unity 6000.3.7f1, URP.

Текущие владельцы:

- Игровой цикл: Assets/Scripts/GameLoop/GameplayBootstrap.cs, GameManager.cs, GameState.cs, WaveManager.cs, ResourceManager.cs.
- Башни: Assets/Scripts/Towers/Tower.cs, TowerStats.cs, TowerStatsSO.cs, TowerPlacementSystem.cs.
- Враги: Assets/Scripts/Monsters/MonsterHealth.cs, MonsterMove.cs, MonsterStats.cs, MonsterStatsSO.cs.
- Оружие: Assets/Scripts/Weapons/IWeapon.cs, Projectile.cs, GameObjectPool.cs, InstantWeapon.cs, BeamWeapon.cs, PierceWeapon.cs, AoEWeapon.cs.
- Уровень: Assets/Scripts/Levels/LevelGenerator.cs, TileMapManager.cs, TilePlacementSystem.cs, TilePlacementValidator.cs, RoadTileDef.cs, Assets/Scripts/GameLoop/NavMeshSurfaceWrapper.cs.
- 3D-контент: Assets/Scripts/Voxels/VoxelGenerator.cs, текущие 3D prefabs башен, врагов, тайлов и projectiles.
- UI/feedback: Assets/Scripts/UI/GameHUD.cs, WaveUI.cs, TowerShopUI.cs, локализация и world-space tooltip-пайплайн.
- Статы: Assets/Scripts/Stats/Stat.cs, StatsSO.cs, ModifierSO.cs, UpgradeRule.cs.

В текущем checkout нет внешнего пакета, на который должен опираться этот план. Все перечисленные системы предстоит спроектировать и реализовать внутри текущей архитектуры проекта.

## 4. Решение по владельцам

| Ответственность | Текущий владелец | Начальное решение |
|---|---|---|
| Game flow | GameManager + GameplayBootstrap | Keep, сравнить lifecycle |
| Round/wave flow | WaveManager + WaveConfig | Adapt, не добавлять второй manager |
| Level data | LevelGenerator + TileMapManager + NavMeshSurfaceWrapper | Adapt, сохранить текущий level owner |
| Entity composition | Tower и Monster prefab composition | Compare до миграции |
| Attack capability | Tower + IWeapon | Keep или точечно адаптировать |
| Health/death | MonsterHealth + PlayerBase | Keep текущего health owner |
| Movement | MonsterMove + NavMeshAgent | Keep текущего movement owner |
| Projectile | Projectile + IWeapon + GameObjectPool | Keep текущего pool |
| Abilities | ModifierSO + UpgradeRule + progression-ветка | Defer до vertical slice |
| Effectors | Stat + ModifierSO | Proof-of-concept после combat |
| Technology | progression-ветка | Defer до решения по прогрессии |
| SaveManager | единого владельца ещё нет | Design отдельно |
| Floating Text | текущий UI/feedback pipeline | Adapt только для 3D |
| 3D shaders | текущий URP pipeline | Keep, не менять глобально |

Термины:

- Keep — текущий владелец остаётся источником поведения.
- Adapt — сопоставляются данные или callback, без второго владельца.
- Defer — задача не нужна для первого 3D vertical slice.
- Compare — решение принимается после чтения реального реального кода проектируемой системы.

## 5. Зависимости

P0-A и P0-B запускаются параллельно.

P1-A и P1-B запускаются после P0 и могут идти параллельно.

P2-A начинается после проверки 3D уровня и prefab pipeline. P2-B использует результат P2-A, но не требует всех вариантов оружия.

P3-A, P3-B и P3-C запускаются после фиксации runtime state vertical slice.

Схема:

P0-A + P0-B -> P1-A + P1-B -> P2-A + P2-B -> P3-A + P3-B + P3-C -> P4-A

## 6. Карточки задач

### TD3D-P0-A — архитектурный аудит и 3D sandbox

Статус: ready  
Зависимости: нет  
Skills: unity-mcp-skill, mcp-unity-find-in-file

Цель: определить обязательные runtime owners и границы 3D sandbox.

Проверить:

- какие системы нужны для первого vertical slice;
- границы собственного runtime API;
- совместимость с Unity 6000.3.7f1 и текущим URP;
- 3D Grid scene;
- конфликты имён, компонентов, тегов, слоёв и ресурсов;
- необходимость каждой планируемой подсистемы;

Результат: architecture baseline, список обязательных систем и отдельная запускаемая 3D sandbox-сцена.

Не затрагивать: Gameplay.unity и текущие Tower/Monster/Wave owners.

### TD3D-P0-B — карта владельцев и callback-цепочек

Статус: ready  
Зависимости: нет; дополнить после P0-A  
Skills: mcp-unity-find-in-file, unity-mcp-skill

Цель: зафиксировать одного владельца для каждой ответственности.

Сопоставить:

- GameManager, RoundManager и GameState;
- LevelData, LevelGenerator, TileMapManager и NavMesh;
- Entity, Tower и Monster prefab composition;
- Attack/Die/Move Capability и текущие combat/movement/health owners;
- Projectile, IWeapon и GameObjectPool;
- resources, stats и modifiers.

Результат: owner map с решениями Keep/Adapt/Defer/Compare и перечнем конкретных migration points.

Не делать: bridge/wrapper только ради одинаковых названий.

### TD3D-P1-A — 3D level, path и placement

Статус: blocked by P0-A/P0-B  
Зависимости: P0-A, P0-B  
Skills: unity-mcp-skill

Цель: выбрать совместимую 3D модель уровня и движения врагов.

Проверить:

- планируемую модель LevelData;
- Grid path и, если требуется, GridAutopath;
- playable area и camera bounds;
- PlacementMode;
- текущие LevelGenerator, TileMapManager и TilePlacementValidator;
- пересборку NavMesh после изменения карты.

Результат: выбранный 3D path/placement baseline и smoke уровня.

Критерий: один враг проходит путь к базе, башня размещается в разрешённой зоне, изменение карты не использует устаревший NavMesh.


### TD3D-P1-B — 3D entity и prefab pipeline

Статус: blocked by P0-A/P0-B  
Зависимости: P0-A, P0-B  
Skills: prefab-creation, unity-mcp-skill

Цель: сопоставить модель Entity/capabilities/behaviours с текущими 3D prefab.

Проверить:

- Tower, MonsterHealth, MonsterMove;
- VoxelGenerator;
- TowerStats и MonsterStats;
- animator, collider, pivot и fire point;
- forward axis Z+;
- стабильность имён prefab/entity и Resources/Entities, если схема будет принята.

Результат: один согласованный 3D enemy prefab и один согласованный 3D tower prefab.

Критерий: prefab создаются в sandbox и сохраняют ожидаемую компонентную композицию.


### TD3D-P2-A — combat, targeting, damage и 3D projectile

Статус: blocked by P1-A/P1-B  
Зависимости: P1-A, P1-B  
Skills: mcp-unity-validate-script, unity-recompile-menuitem, test-writing

Цель: получить end-to-end combat slice через одного владельца.

Проверить:

- выбор цели и поворот Tower;
- модель AttackCapability против текущего Tower/IWeapon;
- Projectile по оси Z+;
- Ignore Raycast layer;
- hit, damage, armor, death и reward;
- pooling;
- отсутствие двойного урона и двойного death event.

Результат: одна башня уничтожает одного 3D врага через согласованный owner chain.

Не входит: второй projectile pool и перенос project-specific damage formula без необходимости.

### TD3D-P2-B — rounds, resources и игровой цикл

Статус: blocked by P1-A/P1-B/P2-A smoke  
Зависимости: P1-A, P1-B, P2-A  
Skills: mcp-unity-validate-script, unity-recompile-menuitem, test-writing

Цель: сопоставить модель RoundManager, rewards, Endless и accelerated speed с текущим циклом.

Проверить:

- WaveConfig и несколько волн;
- initial entities;
- rewards за убийство и волну;
- pause, acceleration, victory и game over;
- повторный запуск без дублирования состояния.

Результат: единый wave loop через GameManager, WaveManager и ResourceManager.

Не входит: отдельный RoundManager.

### TD3D-P3-A — abilities, effectors и technology

Статус: deferred  
Зависимости: P2-A, P2-B  
Skills: mcp-unity-validate-script, test-writing

Цель: добавить минимальный progression proof-of-concept.

Проверить:

- совместимость модель Ability/Effector/Technology с ModifierSO, Stat и UpgradeRule;
- один ability;
- один временный effector;
- одна technology;
- add/remove эффекта;
- неизменность исходного ScriptableObject.

Результат: один проверенный progression chain от asset до runtime.

Не входит: полный каталог контента до утверждения игровых правил.

### TD3D-P3-B — save/load и lifecycle

Статус: deferred  
Зависимости: P2-B, P3-A  
Skills: unity-mcp-skill, test-writing

Цель: спроектировать единого владельца сохранения runtime state.

Определить:

- формат волн, ресурсов, башен, карты и прогрессии;
- DTO/state boundary;
- границы SaveManager и runtime state;
- поведение Save -> Restart -> Load;
- поведение Pause, GameOver и повторной загрузки.

Результат: решение по SaveManager и один проверенный сценарий загрузки.

Не входит: автоматическое копирование внешнего SaveManager.

### TD3D-P3-C — 3D feedback и presentation hooks

Статус: deferred  
Зависимости: P2-A, P2-B  
Skills: unity-mcp-skill, prefab-creation


Проверить:

- 3D hit flash в URP;
- floating damage text;
- muzzle/impact VFX;
- projectile/tower event hooks;
- совместимость feedback с pooling;
- точки подключения audio без отдельной параллельной event system.

Результат: один согласованный 3D hit feedback и минимальные VFX/audio hooks.


### TD3D-P4-A — integration QA, performance и balance

Статус: deferred  
Зависимости: выбранные P2/P3 задачи  
Skills: test-writing, unity-mcp-skill

Цель: проверить собранный 3D vertical slice и определить следующий контентный цикл.

Проверить:

- генерацию и проходимость;
- placement;
- combat и rewards;
- wave lifecycle;
- save/load, если P3-B принят;
- pooling, NavMesh rebuild и particle lifetime;
- баланс волн, башен и экономики.

Результат: список подтверждённых сценариев, найденных проблем и следующих контентных задач.

## 7. Порядок открытия чатов

Волна 1:

- TD3D-P0-A
- TD3D-P0-B

Волна 2 после P0:

- TD3D-P1-A
- TD3D-P1-B

Волна 3 после P1:

- TD3D-P2-A
- TD3D-P2-B

Волна 4 после 3D vertical slice:

- TD3D-P3-A
- TD3D-P3-B
- TD3D-P3-C

Волна 5:

- TD3D-P4-A

## 8. Task-specific handoff

Чтобы не дублировать AGENTS.md и skills, результат каждого чата должен содержать только:

- ID задачи;
- статус: done / blocked / rejected;
- решение по owner chain;
- затронутые owner chain;
- изменённые assets/scripts/scenes;
- подтверждённые сценарии;
- блокеры и зависимости следующей задачи.

## 9. Первый шаг

Начать с TD3D-P0-A и TD3D-P0-B.

До их результатов не перестраивать Gameplay.unity, не создавать новый RoundManager или SaveManager и не запускать массовую миграцию Tower/Monster систем.
NVbsIEpSdEai-beBsCMfwvoxuC-YhT_Vgg/edit)
