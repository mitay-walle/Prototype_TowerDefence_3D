---
title: Формы данных, сервисов и жизненные циклы Unity
status: architecture-reference
updated: 2026-08-04
scope: Unity data forms, service shapes, ownership, lifecycle, persistence
project: Prototype Tower Defence 3D
related: Assets/Documentation/GAMEPLAY_SCENE_OBJECTS.md
---

# Формы данных, сервисов и жизненные циклы Unity

## 1. Назначение

Документ отвечает на три разных вопроса:

1. В какой форме хранить данные в Unity?
2. В какой форме реализовать владельца поведения или сервис?
3. Кто создаёт объект, сколько он живёт и кто его завершает?

Эти решения нельзя смешивать. `ScriptableObject` — прежде всего форма asset-данных, а не автоматический сервис. `MonoBehaviour` — компонент сцены или prefab, но не обязан быть глобальным manager. JSON — формат сохранения, но не runtime-владелец состояния.

Базовое правило выбора:

> Форма определяется авторингом, владельцем и жизненным циклом данных, а не желанием сделать всё одинаковым.

## 2. Наблюдаемый baseline проекта

На текущем этапе проект использует:

- scene-owned `MonoBehaviour` для `GameplayBootstrap`, `GameManager`, `WaveManager`, `ResourceManager` и других владельцев;
- сериализованные inspector-ссылки для композиции сцены;
- `ScriptableObject` для `WaveConfig`, tower/monster stats, level generator и balance profiles;
- `[Serializable]` для вложенных значений и `[SerializeReference]` для полиморфных upgrade/modifier rules;
- прямые asset/prefab-ссылки и `Resources.Load` для части контента;
- `PlayerPrefs` для небольшого постоянного флага starting reserve;
- runtime-поля компонентов для состояния текущей сцены и забега;
- `UnityEvent` для inspector-wiring и уведомлений;
- UniTask с cancellation token жизненного цикла объекта.

Addressables не указан в текущем `Packages/manifest.json`, поэтому это доступный вариант развития Unity-проекта, но не текущий рабочий путь. Его нельзя использовать как скрытый fallback без отдельного подключения и контракта загрузки.

## 3. Логические категории данных

Независимо от технической формы сначала определяется смысл данных.

| Категория | Мутирует | Срок жизни | Пример |
| --- | --- | --- | --- |
| Definition | Нет во время игры | Версия контента | Tower stats, enemy stats, wave, tile, aura |
| Topology | Только при авторинге/композиции | Prefab или scene | Компоненты, child-объекты, ссылки |
| RuntimeState | Да | Scene, run, wave или actor | Деньги, HP, cooldown, выбранная цель |
| SaveSnapshot | Только при создании snapshot | Между запусками | ProfileSave, RunSave |
| Command | Нет после создания | Один вызов | PlaceTower, StartWave, BuyUpgrade |
| Event | Нет после публикации | Один dispatch | EnemyKilled, CurrencyChanged |
| ReadModel | Пересоздаётся | Пока нужен consumer | HUD state, tower panel state |
| DerivedCache | Да, пересчитывается | Пока валиден источник | NavMesh, target index, final stats |
| PresentationState | Да | View или эффект | Animation progress, selected tab, VFX handle |
| Settings | Да редко | Application/profile | Громкость, input, graphics |

Одна и та же механика может использовать несколько форм: `TowerDefinition` как ScriptableObject, `Tower` как MonoBehaviour, `TowerRuntimeState` как обычный C#-объект, `TowerSaveDTO` как сериализуемый snapshot и `TowerReadModel` как неизменяемая проекция для UI.

## 4. Формы данных Unity

### 4.1 Константа, enum и value type в коде

**Форма:** `const`, `static readonly`, `enum`, маленький `struct`.

**Подходит:** технические пределы, стабильные идентификаторы состояния, маленькие значения без authoring.

**Жизненный цикл:** сборка приложения или значение в памяти владельца.

**Не подходит:** баланс, который должен менять designer; локализуемый текст; сохраняемая сущность с версионированием.

**TD3D:** `GameState` — enum; числовые цены, damage и scaling лучше вынести в Definition, если они относятся к контенту.

### 4.2 Сериализованное поле MonoBehaviour или ScriptableObject

**Форма:** `[SerializeField]` primitive, enum, Unity type, массив, `List<T>`, ссылка на `UnityEngine.Object`.

**Подходит:** локальная настройка компонента и явная inspector-композиция.

**Жизненный цикл:** значение хранится в scene, prefab или `.asset`; после instantiate становится частью экземпляра.

**Плюсы:** видно в Inspector, сохраняются ссылки и GUID, просто проверять.

**Риски:** runtime-изменение поля экземпляра не является save; изменение asset в Editor может загрязнить исходный контент; большие общие таблицы дублируются в каждом prefab, если не вынесены в общий asset.

### 4.3 `[Serializable]` inline data

**Форма:** обычный класс или struct с `[Serializable]`, вложенный в host object по значению.

**Подходит:** маленький составной блок, принадлежащий ровно одному host: spawn entry, damage numbers, curve set, upgrade cost row.

**Жизненный цикл:** совпадает с host scene/prefab/ScriptableObject; при копировании host получается копия данных.

**Не подходит:** общие данные, которые должны разделяться несколькими asset; сущность с собственной идентичностью; сложный граф ссылок.

### 4.4 `[SerializeReference]` managed reference

**Форма:** полиморфный `[Serializable]` class через поле interface/abstract/base class.

**Подходит:** небольшая вложенная стратегия или rule tree, принадлежащая одному host: разные stat modifiers, conditions, reward effects.

**Жизненный цикл:** managed reference хранится внутри сериализующего host. Ссылка не является общей между разными host object.

**Плюсы:** полиморфизм без отдельного `.asset` на каждый маленький rule.

**Риски:** смена имени/namespace типа требует миграции; Inspector сложнее; невозможно ссылаться на `MonoBehaviour`/`ScriptableObject` как managed reference; стандартный `Dictionary` не становится поддерживаемым от одного атрибута.

**Выбор:** если вариант должен независимо переиспользоваться и иметь собственную asset-идентичность — `ScriptableObject`; если это маленькая внутренняя часть одной Definition — `[SerializeReference]`.

### 4.5 ScriptableObject asset

**Форма:** отдельный `.asset`, наследующий `ScriptableObject`.

**Подходит:** общая неизменяемая Definition, каталог, balance profile, authored wave, тип tower/enemy/tile, локализуемые ссылки.

**Жизненный цикл:** создаётся в Editor; загружается прямой зависимостью, `Resources`, Addressables или AssetDatabase в Editor; выгружается вместе с owning load scope.

**Плюсы:** одна общая копия, стабильный GUID, удобный authoring, прямые asset-ссылки.

**Риски:** mutable runtime state в asset переживает не те границы в Editor, может попасть в dirty asset и смешивает параллельные забеги. В build ScriptableObject не является способом записать пользовательский save обратно в project asset.

**Правило TD3D:** ScriptableObject — Definition. При старте забега владелец читает Definition и создаёт runtime state/derived stats; asset не мутируется.

### 4.6 Prefab

**Форма:** сериализованный шаблон `GameObject` с компонентами, child hierarchy и ссылками.

**Подходит:** authored topology экземпляра: Tower, Enemy, Projectile, UI panel, VFX.

**Жизненный цикл:** asset живёт как контент; экземпляр — от instantiate до destroy/return to pool.

**Хранит:** состав компонентов, дефолтные значения и ссылки на Definition/presentation assets.

**Не должен хранить как источник истины:** деньги забега, глобальный wave index, профиль игрока.

**Правило:** добавление/удаление/замена компонентов авторится в prefab/scene до Play Mode. `Awake`/`Start` изменяют значения и подписки, но не ремонтируют component topology.

### 4.7 Scene asset

**Форма:** `.unity` с корневыми объектами, их компонентами и связями.

**Подходит:** scene composition, уникальные anchors, lighting, camera, bootstrap, scene-owned services.

**Жизненный цикл:** load → activation → unload. Объекты сцены уничтожаются при unload, кроме явно вынесенных application objects.

**Риски:** прямые ссылки на объекты другой выгружаемой сцены; несколько копий singleton-like owner при additive load; gameplay-данные, спрятанные в случайных scene objects.

### 4.8 Прямая `UnityEngine.Object`-ссылка

**Форма:** inspector/reference field на prefab, ScriptableObject, material, component или scene object.

**Подходит:** обязательная известная зависимость и KISS-композиция.

**Жизненный цикл:** asset dependency либо ссылка внутри загруженной сцены. Для destroyed object действует Unity null semantics.

**Плюсы:** типобезопасно, не нужен строковый address и runtime search.

**Риски:** prefab asset не должен ссылаться на scene instance; persistent service не должен удерживать уничтоженную scene reference.

### 4.9 Resources

**Форма:** asset под `Assets/Resources`, загружаемый по строковому пути.

**Подходит:** небольшой обязательный каталог прототипа, который действительно нужен по runtime ID и уже следует этому пути.

**Жизненный цикл:** доступен после включения в build; загруженный object удерживается ссылками и правилами Unity unload.

**Плюсы:** простая синхронная точка входа.

**Риски:** строковые пути, слабая проверяемость зависимостей, весь каталог попадает в build, leaf-компоненты начинают сами искать контент.

**Правило TD3D:** загрузку централизует catalog/owner. Не добавлять новый `Resources.Load` в каждый actor или UI.

### 4.10 Addressables и AssetReference

**Форма:** addressable entry/catalog, label, address или typed `AssetReference`; async handle.

**Подходит:** большой контент, удалённые bundles, DLC/mod-like catalogs, независимая загрузка и выгрузка.

**Жизненный цикл:** initialize catalog → load handle → use/instantiate → release симметрично каждому load. Владельцем handle является загрузивший scope.

**Плюсы:** асинхронность, dependency management, управляемая выгрузка.

**Риски:** дополнительный build/deploy pipeline, reference counting, ошибки release, сложнее тестирование и save compatibility.

**Статус TD3D:** Deferred. Сначала нужен стабильный content ID/catalog contract; package и pipeline подключаются явной задачей.

### 4.11 Внешний JSON/CSV/YAML и importer data

**Форма:** текстовый файл, `TextAsset`, StreamingAssets, remote payload или Editor importer.

**Подходит:** обмен с таблицами, моды, локальная аналитика, authored bulk balance.

**Рекомендуемый путь:** внешний формат импортируется и валидируется в Definition assets либо загружается application-level content loader. Gameplay-объекты не парсят таблицы самостоятельно.

**Риски:** строки вместо GUID, отсутствие типов, культурные форматы чисел, version/migration, невозможные ссылки на Unity assets без отдельного ID mapping.

### 4.12 Save DTO и файл сохранения

**Форма:** обычный `[Serializable]` DTO, JSON/binary envelope, version, checksum/atomic file protocol.

**Подходит:** `ProfileSave`, `RunSave`, settings, migration.

**Жизненный цикл:** runtime owner создаёт snapshot → SaveService сериализует → файл переживает application → LoadService читает/migrates → bootstrap создаёт новый runtime state.

**Правило:** DTO хранит ID и значения, но не `GameObject`, `MonoBehaviour`, runtime delegate, target reference, NavMesh или VFX handle. Save DTO не становится живым объектом gameplay: загрузка копирует его данные во владельцев.

### 4.13 PlayerPrefs

**Форма:** key/value `int`, `float`, `string`.

**Подходит:** небольшой пользовательский setting или простой прототипный флаг.

**Не подходит:** полный run save, список башен, экономика, защищённая валюта, транзакции и сложная миграция.

**Жизненный цикл:** process-independent platform storage; запись через `Set...`, flush через `Save` или завершение приложения, но критичные данные не должны зависеть только от quit callback.

**TD3D:** текущий starting reserve допустим как прототипный meta flag; при появлении полноценного `ProfileSave` он мигрирует в одну схему.

### 4.14 Обычный runtime C# object

**Форма:** class/record/struct без `UnityEngine.Object`.

**Подходит:** run state, ledger, deterministic random, rule service, command, event, read model, snapshot builder.

**Жизненный цикл:** создаётся composition root/owner; завершается владельцем или GC, а `IDisposable`/cancellation закрываются явно.

**Плюсы:** тестируемость, нет Unity callback и fake-null semantics, можно создавать несколько сессий.

**Риски:** object не виден в Inspector и не получает Unity API/lifecycle автоматически.

### 4.15 Runtime state в MonoBehaviour

**Форма:** приватные поля компонента экземпляра.

**Подходит:** HP конкретного actor, cooldown, текущая цель, movement state, animation/view state.

**Жизненный цикл:** экземпляр prefab/scene object; при pooling нужен явный reset на каждом rent/return.

**Риск:** использовать component как глобальный save или разделять одно состояние между несколькими владельцами.

### 4.16 Native collections, Jobs и ECS data

**Форма:** `NativeArray`, `NativeList`, job data, `IComponentData`, BlobAsset.

**Подходит:** измеренная CPU/масштабная проблема с большим количеством однотипных данных.

**Жизненный цикл:** allocator/job/world определяет создание и обязательный dispose.

**Статус TD3D:** Deferred. Для прототипа MonoBehaviour/POCO проще; миграция оправдана профилированием, а не общей «правильностью ECS».

## 5. Формы сервисов и владельцев поведения

### 5.1 Scene-owned MonoBehaviour owner

**Пример:** `GameManager`, `WaveManager`, `ResourceManager`, `LevelGenerator`.

**Когда выбирать:** сервис зависит от scene objects, Unity callbacks, Inspector или Transform; одна сцена содержит одну сессию.

**Создание:** scene/prefab authoring. **Инициализация:** явный bootstrap после `Awake`. **Завершение:** scene unload/`OnDestroy`.

**Плюсы:** минимальная композиция, наглядные ссылки. **Риски:** static `Instance`, runtime search и неявный порядок `Start`.

### 5.2 Actor-owned MonoBehaviour

**Пример:** `Tower`, `MonsterHealth`, `MonsterMove`, `Projectile`.

**Когда выбирать:** состояние принадлежит одному видимому экземпляру.

**Жизненный цикл:** instantiate/rent → initialize/reset → active → death/disable → return/destroy.

Это не «сервис». Общая логика может быть вынесена в pure C# rule, но владельцем HP/цели остаётся actor.

### 5.3 Application MonoBehaviour

**Пример роли:** `ApplicationBootstrap`, platform integrations, scene flow, audio root.

**Когда выбирать:** объект действительно должен переживать смену сцен и использует Unity API/callback.

**Жизненный цикл:** первая boot scene → `DontDestroyOnLoad` → application quit.

**Ограничения:** создаётся в одном composition root, защищён от дублей, не удерживает мёртвые scene references, очищает scene binding при unload. Не каждый manager должен становиться persistent.

### 5.4 Pure C# service

**Пример роли:** damage resolver, reward roller, economy formula, save serializer, deterministic random adapter.

**Когда выбирать:** поведение не требует Transform, Inspector и per-frame callback.

**Жизненный цикл:** application, run, wave или command scope; создаётся явно и получает зависимости constructor-ом.

**Интерфейс нужен:** на реальной архитектурной границе, при нескольких реализациях или тестовом substitutable dependency. Интерфейс для каждого класса без потребителя не нужен.

### 5.5 Composition root / DI context

**Форма:** `GameplayBootstrap`, scene context/builder или application context.

**Ответственность:** создать service graph, связать зависимости, выбрать implementation, запустить владельцев в определённом порядке и завершить scope.

**Не делает:** damage, wave rewards, UI state, scene search за каждого consumer.

**TD3D Basic:** inspector-ссылки в `GameplayBootstrap`. DI framework допустим как Extended-вариант, если граф действительно перестал быть обозримым; injection callback только передаёт зависимости и не запускает gameplay.

### 5.6 Factory / pool

**Форма:** `EnemyFactory`, `TowerFactory`, `ProjectilePool` либо существующий простой owner.

**Ответственность:** создать или выдать технически валидный экземпляр и вызвать его initialization/reset contract.

**Жизненный цикл:** factory обычно scene/run; созданный actor — actor scope.

**Не делает:** не решает, когда должна стартовать волна, кому дать награду и какую цель выбрать.

### 5.7 Catalog / repository / gateway

- **Catalog** разрешает Definition по ID и валидирует контент.
- **Repository** читает/пишет snapshots или коллекцию сущностей за устойчивой границей.
- **Gateway** изолирует platform/network/file API.

Они нужны на I/O и content boundaries. Не следует называть repository обычный `List<T>` внутри `WaveManager`.

### 5.8 Provider / registry

**Provider:** отдаёт одну текущую scene-owned зависимость long-lived consumer-у.

**Registry:** хранит много динамических contributors с `Register/Unregister`.

**Подходит:** application-service должен временно видеть активную камеру; spatial/target system учитывает множество towers/enemies.

**Жизненный цикл:** contributor регистрируется после готовности и снимается при disable/destroy. Registry не становится вторым владельцем состояния contributor.

### 5.9 Static utility и singleton

**Static utility подходит:** чистая функция без состояния, например числовая формула или validation helper.

**Static singleton допустим ограниченно:** уникальный engine/application facade с ясным reset и lifecycle.

**Риски:** скрытые зависимости, state leak при отключённом Domain Reload, сложные тесты, дубли при scene load.

**TD3D:** существующие `Instance` — текущий путь совместимости, но новые системы не должны автоматически копировать этот формат. Сначала проверяется, нельзя ли передать прямую ссылку от bootstrap/owner.

### 5.10 ScriptableObject service и event channel

Unity позволяет хранить mutable fields и events в ScriptableObject, но это не делает asset безопасным runtime-сервисом.

**Допустимо:** stateless strategy, immutable catalog, editor-authored signal identifier.

**Рискованно:** деньги, текущая волна, selected target, subscribers и cancellation в asset. Asset может жить дольше сцены, повторно включаться в Editor и разделяться несколькими сессиями.

**Решение TD3D:** Definition — ScriptableObject; mutable owner — scene/run service. Event channel вводится только при реальной необходимости развязать assembly/scene boundary, а не вместо прямого C# event.

## 6. Жизненные циклы приложения и gameplay

| Scope | Создаёт | Начало | Конец | Что хранит | Сохраняется |
| --- | --- | --- | --- | --- | --- |
| Editor asset | Unity/author | Import/create | Удаление asset | Definition/topology | Сам asset |
| Application | ApplicationBootstrap | Запуск | Quit | Settings, scene flow, save/content services | Через Profile/Settings save |
| Profile | ProfileService | Load/create profile | Смена профиля | Meta progression, unlock | Да |
| Scene | SceneFlow/Unity | Scene load | Scene unload | Camera, HUD, level owners | Нет напрямую |
| Run | GameplayBootstrap | New/continue run | Victory/defeat/abandon | Seed, map, economy, towers | RunSave snapshot |
| Phase | GameManager | Enter state | Exit state | Разрешённые команды, offer | Часть RunSave |
| Wave | WaveManager | Start wave | Resolve | Spawn cursor, alive count | Только при mid-wave save |
| Actor | Factory/pool | Spawn/rent | Death/leak/return | HP, shield, cooldown | Только если входит в snapshot |
| Effect | Actor/aura | Apply | Expire/remove | Source, duration, stacks | Только mid-wave |
| Command | Caller | Method call | Return/completion | Input payload | Нет |
| Event | Owner | Publish | Dispatch complete | Immutable result | Нет |
| Frame/job | Unity/system | Tick/schedule | End/complete | Temporary work data | Нет |

Главное правило: dependency может жить столько же или дольше consumer. Application-service не должен напрямую удерживать scene object после unload; actor не должен владеть run service; UI не должен переживать владельца read model.

## 7. Unity callback lifecycle

### Awake

- установить локальные инварианты экземпляра;
- получить обязательные sibling-компоненты, если topology гарантирована prefab;
- подготовить локальные ссылки без запуска gameplay;
- не полагаться на `Start` другого объекта.

### OnEnable

- подписаться на input/events;
- зарегистрировать активный contributor;
- возобновить view/actor behavior.

`OnEnable` вызывается повторно, поэтому подписка должна иметь симметричный unsubscribe.

### Start

- допустим для простого scene-local старта после `Awake`/`OnEnable`;
- для межсистемного порядка предпочтительнее явный `GameplayBootstrap.Initialize/Start...`;
- не должен искать и создавать отсутствующие обязательные владельцы как fallback.

### Update / FixedUpdate / LateUpdate

- `Update` — frame input, timers и visual/gameplay tick только когда действительно нужен polling;
- `FixedUpdate` — physics-step logic;
- `LateUpdate` — camera/follow и post-update presentation;
- событие или scheduled task предпочтительнее пустого `Update` у сотен объектов.

### OnDisable

- отписаться от input/events;
- остановить активные presentation/process hooks;
- убрать active registration;
- помнить, что pooled object может быть disabled и затем включён снова.

### OnDestroy

- завершить object-owned async через cancellation;
- снять оставшиеся регистрации;
- освободить owned handles/native resources;
- очистить static `Instance`, только если он указывает на этот объект.

### OnApplicationQuit

Это уведомление, а не надёжная единственная точка сохранения. Критичные snapshots записываются в безопасных gameplay/application переходах.

### ScriptableObject callbacks

`OnEnable`/`OnDisable` у ScriptableObject относятся к загрузке asset/Editor lifecycle и не равны началу/концу забега. В них нельзя неявно запускать run service.

### Domain Reload и static state

При настройках Enter Play Mode без Domain Reload static fields могут пережить повторный вход в Play Mode. Любой допустимый static state имеет явный reset на application bootstrap/runtime initialization; полагаться только на initializer поля опасно.

## 8. Async lifecycle

Каждая async-операция принадлежит scope:

- actor task отменяется при destroy/return to pool;
- wave task — при остановке волны или завершении run;
- scene load/content task — при отмене перехода сцены;
- application task — при quit.

Для нового runtime-кода используется UniTask и cancellation token владельца. `GetCancellationTokenOnDestroy()` подходит object scope, но не заменяет отдельный wave/run token, если операция должна закончиться раньше уничтожения объекта.

Нельзя оставлять fire-and-forget без владельца ошибки и отмены. Bootstrap либо успешно завершает последовательность, либо сообщает блокирующую ошибку и не переводит `GameManager` в следующее состояние.

## 9. Asset loading lifecycle

### Direct reference

Зависимость включается Unity вместе с host/asset graph. Отдельного release handle нет; scope определяется загруженной scene/asset dependency.

### Resources

Catalog/owner вызывает load, проверяет обязательный результат и хранит ссылку нужный срок. Leaf actor не повторяет поиск. Выгрузка планируется централизованно; `UnloadUnusedAssets` не является заменой ownership.

### Addressables

Каждый `LoadAssetAsync`/`InstantiateAsync` имеет владельца handle и симметричный `Release`/`ReleaseInstance`. Run/scene scope хранит handles своего контента и освобождает их после остановки consumers.

### Pool

Pool удерживает экземпляры и их asset dependency, пока существует сам pool. Return сбрасывает actor state, subscriptions, effects и target references; release pool выполняется после завершения всех пользователей.

## 10. Потоки данных между слоями

```text
Definition assets/content catalog
              ↓
Application composition → GameplayBootstrap → Runtime owners
                                              ↓            ↑
                                         ReadModel       Command
                                              ↓            ↑
                                             UI / Input
                                              ↑
                                      Event / presentation cue
                                         ↙             ↘
                                       VFX             SFX

Runtime owners → SaveSnapshot → SaveService → file
file → SaveService → migrated DTO → GameplayBootstrap → new RuntimeState
```

Направления:

- Definition идут сверху вниз и не мутируются;
- command идут от input/UI к владельцу;
- event и read model идут от владельца к consumers;
- save получает snapshot, а не доступ к внутренностям всех компонентов;
- VFX/SFX не возвращают gameplay-решение обратно владельцу.

## 11. Рекомендованное соответствие для TD3D

| Потребность | Рекомендуемая форма сейчас | Вариант позже |
| --- | --- | --- |
| Tower/enemy/wave/tile stats | ScriptableObject Definition | Addressable/mod catalog |
| Маленькое вложенное правило | `[Serializable]` | `[SerializeReference]`, если нужен полиморфизм |
| Общая переиспользуемая стратегия | ScriptableObject Definition | Pure C# implementation из catalog |
| Topology Tower/Enemy/UI | Prefab | Addressable prefab |
| Уникальная композиция gameplay | Scene + inspector refs | Scene DI context |
| Run state machine | Текущий `GameManager` | Pure run state внутри того же owner, если потребуется |
| Wave runtime | Текущий `WaveManager` | Отдельный pure scheduler, принадлежащий `WaveManager` |
| Валюта | Текущий `ResourceManager` | Ledger/state object, принадлежащий тому же owner |
| Actor HP/cooldown/target | MonoBehaviour runtime fields | Data-oriented storage после профилирования |
| UI state | Read model/snapshot | Reactive binding без нового источника истины |
| Уведомления | C# event/UnityEvent у owner | Typed event bus только при доказанной cross-boundary нужде |
| Сохранение забега | DTO + SaveService | Versioned multi-slot/cloud gateway |
| Простые настройки | PlayerPrefs/settings DTO | Platform settings service |
| Динамическое создание | Existing factory/pool/owner | Typed factory catalog |
| Async | UniTask + owner token | Run/wave linked cancellation scopes |

## 12. Матрица выбора

- Данные редактируются designer и разделяются между экземплярами? **ScriptableObject Definition.**
- Данные принадлежат одному prefab/asset и не имеют собственной идентичности? **`[Serializable]` inline.**
- Нужен полиморфный вложенный rule без россыпи `.asset`? **`[SerializeReference]`.**
- Нужна переиспользуемая полиморфная сущность с GUID? **ScriptableObject.**
- Нужны Transform, Inspector или Unity callbacks? **MonoBehaviour.**
- Нужна чистая формула/оркестрация без Unity API? **Pure C# object/service.**
- Данные должны пережить процесс? **Versioned DTO через SaveService.**
- Нужен небольшой setting/flag? **PlayerPrefs**, пока не появился общий ProfileSave.
- Нужна динамическая выгрузка большого контента? **Addressables**, после явного подключения pipeline.
- Нужны тысячи однотипных элементов и профиль показывает CPU bottleneck? **Jobs/ECS/native data.**
- Consumer и dependency имеют разный scope? Dependency живёт дольше либо используется **provider/snapshot**, но не stale scene reference.

## 13. KISS и SOLID для сервисов

- **KISS:** сначала прямой owner и inspector/constructor dependency; framework, bus, repository и provider добавляются только для конкретной границы.
- **SRP:** `WaveManager` планирует волну, `ResourceManager` меняет валюту, SaveService пишет snapshot; один сервис не поглощает все данные забега.
- **OCP:** варианты контента добавляются Definition/strategy там, где вариативность реальна; не нужен switch-free дизайн ради одной реализации.
- **LSP:** все weapon/effect variants соблюдают единый контракт результата и жизненного цикла.
- **ISP:** UI получает узкий read/command API, а не полный mutable manager.
- **DIP:** интерфейсы полезны на нестабильных границах — save, content loading, clock/random, platform API. Между двумя стабильными concrete owners достаточно прямой зависимости.

Антипаттерны:

- mutable ScriptableObject как глобальный run state;
- новый singleton для каждого набора функций;
- service locator/`FindAnyObjectByType` из leaf-компонентов;
- UI/VFX/SFX как владелец gameplay state;
- одна и та же валюта в manager, UI и save DTO одновременно;
- save живых `GameObject`-ссылок;
- создание обязательных компонентов и владельцев в `Awake` как fallback;
- application-service, удерживающий уничтоженную scene reference;
- async без cancellation и owner;
- Addressables load без симметричного release.

## 14. Минимальный рекомендуемый lifecycle проекта

1. Application загружает settings/profile/content catalog.
2. `SceneFlow` открывает `Gameplay.unity` и передаёт new/continue request.
3. `GameplayBootstrap` проверяет scene references и Definition.
4. Bootstrap создаёт/восстанавливает run state, карту и derived caches.
5. Bootstrap явно инициализирует текущих `GameManager`, `WaveManager`, `ResourceManager`, HUD.
6. `GameManager` переводит run в `Preparation`.
7. Actor создаются factory/pool и получают Definition плюс runtime initialization payload.
8. Между волнами владельцы формируют `RunSave` snapshot; SaveService записывает его.
9. Victory/Defeat закрывает run tasks, создаёт `RunResult` и передаёт meta-награду application layer.
10. Scene unload уничтожает scene-owned objects; application services освобождают scene binding и остаются готовы к следующей сцене.

## 15. Проверка решения

Перед добавлением формы данных или сервиса ответить:

- Кто единственный владелец mutable state?
- Кто создаёт объект и кто его завершает?
- Какой точный scope: application, scene, run, wave, actor или command?
- Нужен ли объекту Unity API/Transform/Inspector?
- Данные authoring, runtime, snapshot или cache?
- Ссылка переживёт consumer и scene unload?
- Где выполняются subscribe/unsubscribe, load/release, create/dispose?
- Что произойдёт при повторном `OnEnable`, pooling и отмене async?
- Нужен ли интерфейс хотя бы одному реальному consumer?
- Можно ли решить задачу расширением существующего владельца?
- Является ли отсутствующий контент ошибкой вместо fallback?

## 16. Официальные ссылки Unity

- [ScriptableObject, Unity 6.1 Manual](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html)
- [`SerializeReference` Scripting API](https://docs.unity3d.com/ScriptReference/SerializeReference.html)
- [Execution order of event functions](https://docs.unity3d.com/Manual/execution-order.html)
- [Addressables memory management](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/MemoryManagement.html)
