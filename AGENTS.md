# AGENTS.md

## Проект

Unity-проект Tower Defence на Unity `6000.3.7f1`.

- Основная и единственная сцена в Build Settings: `Assets/Scenes/Gameplay.unity`.
- Runtime-код находится в `Assets/Scripts`; asmdef для проектного кода не добавлять.
- Основные пространства имён: `TD.GameLoop`, `TD.Levels`, `TD.Towers`, `TD.Monsters`, `TD.Weapons`, `TD.UI`, `TD.Stats`, `TD.Interactions`, `TD.Voxels`.
- Общие Codex skills хранятся вне репозитория; проектные workflow skills — в `.codex/skills/`. Не копируй общие skills в проект.
- Role-профили task-агентов находятся в `.agents/`: `game-director.md`, `read-only-gameplay-architect.md`, `gameplay-designer.md`, `gameplay-systems-programmer.md`, `gameplay-tester.md`, `ui-designer.md`, `unity-editor-tools-programmer.md`, `project-auditor.md`. Используй подходящий профиль как routing instruction вместе с этим `AGENTS.md` и нужными project-local skills.
- Для gameplay direction и декомпозиции используй `.agents/game-director.md`; для read-only owner review — `.agents/read-only-gameplay-architect.md`; для authored map/rewards/roles — `.agents/gameplay-designer.md`; для runtime systems — `.agents/gameplay-systems-programmer.md`; для UI feedback — `.agents/ui-designer.md`; для tests — `.agents/gameplay-tester.md`; для editor tooling — `.agents/unity-editor-tools-programmer.md`; для project structure — `.agents/project-auditor.md`.
- `.agents` задаёт только роль и границы агента. Источниками истины остаются текущие код, сцена, prefab, assets, `Assets/Documentation/GAMEPLAY_REFERENCES.md` и этот `AGENTS.md`.
- `Assets/Plugins`, `Assets/SerializeInterfaces` и сторонние пакеты не менять без явной необходимости.

## Что это за игра

Это прототип 3D tower defense с элементами roguelite. Игрок защищает базу на процедурно собранной карте от последовательных волн монстров.

- В активной волне монстры появляются на открытых концах дорожек, идут к базе через NavMesh, а башни автоматически выбирают цели и атакуют их.
- Игрок тратит награды за убийства и завершение волн на размещение и улучшение башен, выбирая подходящие места и типы оружия.
- Между волнами можно менять карту и размещать новые тайлы; это обновляет дорожки и точки появления врагов.
- Победа достигается после завершения всех настроенных волн, поражение — после уничтожения базы.

## Архитектура

- `GameplayBootstrap` запускает последовательность: генерация уровня → NavMesh → размещение игровых объектов → инициализация систем.
- Генерация карты и проверка стыковки тайлов принадлежат `LevelGenerator`, `TileMapManager` и `TilePlacementValidator`.
- Игровым циклом владеют `GameManager`, `WaveManager` и `ResourceManager`; состояние передаётся через их события и `GameState`.
- `Tower` владеет выбором цели, атакой и апгрейдом. Оружие подключается через `IWeapon`; запасной путь использует `Projectile` и `GameObjectPool`.
- Враги разделены на `MonsterHealth` и `MonsterMove`; их конфигурация хранится в ScriptableObject-статах.
- Размещение башен: `TowerShopUI` → `TowerPlacementSystem` → экземпляр `Tower`.
- Данные, загружаемые через `Resources`, лежат в `Assets/Resources` (`WaveConfigs`, `TowerStats`, `TileDefs`, `TagDB`).

## Правила изменений

- KISS: исправляй владельца поведения в существующей цепочке, не добавляй дублирующие менеджеры, мосты, глобальное состояние или параллельные системы.
- Сначала ищи реальный callback, prefab, scene reference и ScriptableObject, затем меняй минимальный участок.
- Сохраняй GUID, сериализованные ссылки, существующие имена и namespace. Новые игровые типы называй `Tower`, а не `Turret`; старые совместимые имена не переименовывай без миграции.
- Для новой асинхронности используй UniTask с cancellation token объекта; не добавляй новые Coroutine. Существующий код не переписывай без связи с задачей.
- Для UI используй существующие Input System, TMP и Localization-паттерны проекта.
- Не добавляй новые asmdef и не делай косметический рефакторинг рядом с исправлением.
- Новые комментарии и XML-документацию в C# не добавляй; стиль ближайшего файла сохраняй.
- Не запускай `msbuild`: компиляция Unity-проекта выполняется через Unity Editor/MCP.
- Не откатывай, не удаляй, не нормализуй и не перезаписывай dirty/untracked изменения, созданные не в текущем ходе. Считай их работой пользователя, адаптируйся к ним и сообщай о конфликтах.
- Не создавай git worktree, если пользователь явно не попросил об этом.
- Не используй fallback: если основной путь недоступен или не сработал, остановись и сообщи о блокере; обходной или запасной путь допустим только по явному запросу пользователя.
- Работай в одной основной репе проекта: не создавай и не используй Codex worktree, detached checkout или параллельный task-space без явного запроса пользователя. Отдельные task-чаты выполняй последовательно в этой же основной репе.
- Если пользователь просит запомнить правило, обновляй проектный документ или project-local skill, а не ad-hoc memory note.

## Сцены и ассеты

- Сцену, prefab и serialized asset редактируй через Unity, AssetDatabase, PrefabUtility или существующие editor-инструменты.
- Перемещение и переименование Unity-ассетов выполняй средствами Unity, чтобы сохранить ссылки.
- Не редактируй вручную `.unity`, `.prefab`, `.asset`, `.mat`, `.anim` и `.meta`, если Unity/MCP или editor-инструмент может выполнить операцию. Ручное редактирование допустимо только для маленького, понятного и необходимого serialized-изменения.
- Сохраняй существующие `.meta` GUID; не создавай новые GUID для существующих ассетов.
- Не редактируй `Library`, `Temp`, `Logs`, `obj`, `UserSettings` и файлы сборок.
- Перед изменением проверь `git status`; существующие изменения принадлежат пользователю и должны быть сохранены.

## Текст и форматирование

- Перед редактированием существующего текстового файла проверь EOL, BOM, финальный перевод строки и UTF-8; сохраняй исходные соглашения.
- Используй convention-aware `apply-patch` `write`/`replace`; после изменений с кириллицей проверяй кодировку и отсутствие mojibake или replacement characters.
- Для C# сохраняй отступы и стиль ближайшего файла. Не запускай широкие форматтеры, normalize EOL или organize-usings без явной задачи.
- Держи diff минимальным и не вноси whitespace-only изменения.

## Editor automation и проверка

- В начале Unity-задачи проверь доступность Unity MCP и предпочитай MCP для scene, prefab, asset, import, console и MenuItem операций.

- Unity Editor может быть открыт на основном worktree, пока Codex работает в detached worktree того же проекта; перед project-scoped проверкой сверяй project root и хэши целевых файлов, а расхождение явно фиксируй в отчёте.
- Формулировка «запускай задачи» означает: создай отдельный Codex task/chat для каждой указанной задачи; не считать продолжение текущего чата запуском, если пользователь не уточнил обратное.
- После завершения отдельной задачи слей её изменения в основной рабочий space, проверь итог и удали task-space/worktree; не оставляй завершённые task-spaces без явной причины.
- Если Unity Editor запущен, а Unity MCP недоступен, сначала самостоятельно восстанови или перезапусти MCP server доступными штатными средствами и повторно проверь подключение. Не перекладывай это действие на пользователя. Если после этого MCP всё ещё недоступен, явно сообщи точную причину блокера; если операция требует состояния Unity Editor, остановись.
- Для изменённых C# используй `mcp-unity-validate-script`, затем дождись автоматической компиляции Unity. Меню `TD/Automation/Force Recompile All` используй, если автоматическая компиляция явно зависла, нужна принудительная диагностика или пользователь попросил об этом.
- После компиляции проверь Console на ошибки до дальнейшей диагностики.
- Для editor-инструментов используй `Assets/Editor` или подпапку `Editor`; меню проекта начинается с `TD/`.
- Тесты находятся в `Assets/Tests/Editor` и запускаются через Unity Test Runner. `total=0` не считается успешным прохождением.
- Runtime-изменения дополнительно проверяй коротким Play Mode smoke-тестом основной сцены. Если Unity/Play Mode не запускался, явно укажи это в результате.
- При отчёте отделяй подтверждённые проверки от предположений и не объявляй систему рабочей только по компиляции.

## Unity MCP allocator safety

- Do not use generic `manage_gameobject.get_components`, `get_component`, or reflection-based `component_properties`/`set_component_property` operations for routine Unity work in this project. Prefer narrow typed MCP tools, Unity Editor APIs, or purpose-built MenuItems.
- If Console reports `TLS Allocator ALLOC_TEMP_TLS`, `ALLOC_TEMP_MAIN`, or `ValidTRS()` after a GameObject/Component MCP call, stop all further GameObject/Component MCP calls and Play/Save attempts. Clearing Console is not a fix; restart Unity before continuing. Do not edit `Library/PackageCache`.

## Odin Inspector

- Используй Odin Inspector для новых custom inspector/editor window поверх существующих паттернов проекта.
- Не заменяй обычный Odin Inspector на plain `UnityEditor.Editor` без конкретной причины.

## Источники истины

Текущий код, сцена и ассеты важнее старых описаний в `README.md`, `SESSION_SUMMARY.md`, `READY_TO_TEST.md` и `Assets/Documentation`. Перед реализацией сверяй их с фактическими компонентами и ссылками в `Gameplay.unity`.
