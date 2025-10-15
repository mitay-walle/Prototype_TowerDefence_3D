# TD Roguelite Development Plan

## Development Priority by Gameplay Loop

### 🎯 CORE LOOP (10 seconds) - CRITICAL PRIORITY
The moment-to-moment gameplay that happens every 10 seconds.

1. **Tower Combat System** - Targeting, shooting, projectiles, damage
   - Status: Not started
   - Dependencies: None
   - Files: `TowerCombat.cs`, `Turret.cs`

2. **Projectile Physics** - Ballistics, hitscan, area damage, effects
   - Status: Not started
   - Dependencies: Tower Combat System
   - Files: `Projectile.cs`, `ProjectileConfig.cs`

3. **Enemy System** - Monster generation, pathfinding, health, death
   - Status: Stub (`MoveToBase.cs` exists but empty)
   - Dependencies: None
   - Files: `Enemy.cs`, `EnemyHealth.cs`, `MonsterGenerationProfile.cs`

4. **Base/Health System** - Player HP, game over conditions
   - Status: Stub (`Base.cs` exists but empty)
   - Dependencies: Enemy System
   - Files: `Base.cs`, `GameManager.cs`

5. **Tower Stats System** - Damage, range, fire rate, upgrade levels
   - Status: Not started
   - Dependencies: Tower Combat System
   - Files: `TowerStats.cs`, `TowerStatsConfig.cs`

### ⚙️ MID LOOP (5 minutes) - HIGH PRIORITY
Tactical decisions between waves and resource management.

6. **Resource/Economy System** - Currency, tower costs, income
   - Status: Not started
   - Dependencies: Base/Health System
   - Files: `ResourceManager.cs`, `Economy.cs`

7. **Wave Management System** - Spawn timing, difficulty scaling, wave progression
   - Status: Not started
   - Dependencies: Enemy System
   - Files: `WaveManager.cs`, `WaveConfig.cs`

8. **Game UI** - HUD, resource display, wave counter, health bar
   - Status: Partial (`TowerShopUI.cs` exists)
   - Dependencies: Resource System, Wave System
   - Files: `GameHUD.cs`, `ResourceDisplay.cs`, `WaveDisplay.cs`

9. **Run-based Upgrade System** - Temporary buffs, tower upgrades during run
   - Status: Not started
   - Dependencies: Resource System, Tower Stats
   - Files: `RunUpgradeManager.cs`, `UpgradeConfig.cs`

10. **Upgrade UI** - Tower selection, upgrade tree, run upgrades
    - Status: Not started
    - Dependencies: Run Upgrade System
    - Files: `UpgradeUI.cs`, `UpgradeTreeUI.cs`

### 🔄 META LOOP (30 minutes) - MEDIUM PRIORITY
Long-term progression between runs (roguelite layer).

11. **Menu System** - Main menu, pause menu, game over, victory screens
    - Status: Not started
    - Dependencies: None
    - Files: `MainMenu.cs`, `PauseMenu.cs`, `GameOverUI.cs`

12. **Roguelite Meta-progression** - Permanent upgrades, unlocks
    - Status: Not started
    - Dependencies: Save System
    - Files: `MetaProgressionManager.cs`, `PermanentUpgrade.cs`

13. **Save/Load System** - Persistent data, run progress, meta-progression
    - Status: Not started
    - Dependencies: None
    - Files: `SaveManager.cs`, `SaveData.cs`

14. **Procedural Content Variation** - Multiple tower types, enemy types, level layouts
    - Status: Partial (Tower generation exists, level generation exists)
    - Dependencies: All generation profiles
    - Files: Extend existing generation profiles

### ✨ POLISH - LOW PRIORITY
Juice, feel, and quality-of-life improvements.

15. **VFX System** - Muzzle flash, hit effects, death animations, projectile trails
    - Status: Not started
    - Dependencies: Combat System
    - Files: VFX prefabs, particle systems

16. **Audio System** - SFX for shooting, impacts, UI, music
    - Status: Not started
    - Dependencies: Combat System
    - Files: `AudioManager.cs`, audio assets

17. **Settings System** - Audio, graphics, controls, gameplay options
    - Status: Not started
    - Dependencies: None
    - Files: `SettingsManager.cs`, `SettingsUI.cs`

18. **Difficulty Balancing** - Enemy stats, wave composition, economy tuning
    - Status: Not started
    - Dependencies: All gameplay systems
    - Files: Config files, ScriptableObjects

---

## COMPLETED SYSTEMS

### Pre-existing Systems
✅ **Level Generation** - `LevelRoadGenerationProfile.cs` (terrain + roads with pathfinding)
✅ **Tower Generation** - `TurretGenerationProfile.cs` (procedural tower models)
✅ **Monster Generation** - `MonsterGenerationProfile.cs` (procedural enemy models)
✅ **Voxel Rendering** - `VoxelGenerator.cs` (optimized mesh generation)
✅ **Tower Placement** - `TowerPlacementSystem.cs` (grid-based with collision detection)
✅ **Selection System** - `SelectionSystem.cs` (mouse + gamepad targeting)
✅ **Camera Control** - `RTSCameraController.cs` (RTS camera with multi-input)
✅ **Input System** - Unity Input System configured with action maps
✅ **UI Foundation** - `TowerShopUI.cs`, `ButtonSector.cs` (radial menus)

### Core Loop (10s) - COMPLETED ✅
✅ **Enemy System** - `EnemyHealth.cs`, `MoveToBase.cs` (health, death, NavMesh pathfinding)
✅ **Base System** - `Base.cs` (player health, game over condition)
✅ **Tower Combat** - `Turret.cs` (targeting, rotation, firing logic)
✅ **Tower Stats** - `TowerStats.cs` (ScriptableObject for tower configuration)
✅ **Projectiles** - `Projectile.cs`, `ProjectilePool.cs` (ballistics, damage, object pooling)

### Mid Loop (5m) - COMPLETED ✅
✅ **Resource Management** - `ResourceManager.cs` (currency, spending, income)
✅ **Wave Management** - `WaveManager.cs`, `WaveConfig.cs` (spawn timing, difficulty scaling)
✅ **Game HUD** - `GameHUD.cs` (resource display, wave counter, health bar)
✅ **Game Manager** - `GameManager.cs` (game state, game over, victory conditions)

---

## NEXT STEPS

**Core Loop (10s) is now COMPLETE! ✅**

All testing checkpoints passed:
- [x] Enemy spawns and walks path to base
- [x] Tower targets nearest enemy
- [x] Tower shoots projectile at enemy
- [x] Projectile hits and damages enemy
- [x] Enemy dies when health reaches 0
- [x] Game ends when enemy reaches base

**Mid Loop (5m) is now COMPLETE! ✅**

The game now has:
- Resource/economy system with currency
- Wave management with difficulty scaling
- Game HUD showing all vital information
- Game state management (play, pause, game over, victory)

**Next Priority: Mid Loop Enhancements**
- [ ] Run-based upgrade system (temporary buffs)
- [ ] Upgrade UI (tower selection, upgrade tree)

**Then: Meta Loop (30m)**
- [ ] Menu system (main menu, pause menu)
- [ ] Roguelite meta-progression
- [ ] Save/load system
- [ ] Procedural content variation

**Then: Polish**
- [ ] VFX system (muzzle flash, hit effects, trails)
- [ ] Audio system (SFX, music)
- [ ] Settings system
- [ ] Difficulty balancing
