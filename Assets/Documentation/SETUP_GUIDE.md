# Tower Defense Setup Guide

## Quick Start - Automated Setup

1. Open Unity menu: **TD → Game Setup Helper**
2. Click **"Create All Resources"** - Creates folders, TowerStats, WaveConfigs
3. Click **"Setup Scene"** - Creates all managers, UI, spawn points, base
4. Done! Basic structure is ready

## Manual Prefab Creation

### 1. Create Enemy Prefab

1. Create empty GameObject: `Enemy`
2. Add `VoxelGenerator` component
3. Create new `MonsterGenerationProfile` asset
4. Assign profile to VoxelGenerator
5. Click "Generate" button to create voxel enemy
6. Add `NavMeshAgent` component
   - Speed: 3
   - Angular Speed: 120
   - Stopping Distance: 0.5
7. Add `EnemyHealth` component
   - Max Health: 100
   - Reward Amount: 10
8. Add `MoveToBase` component
9. Add `CapsuleCollider` for projectile detection
10. Save as prefab: `Assets/Prefabs/Enemies/BasicEnemy.prefab`

### 2. Create Tower Prefab

1. Create empty GameObject: `Tower`
2. Add `VoxelGenerator` component
3. Create new `TurretGenerationProfile` asset
4. Assign profile to VoxelGenerator
5. Click "Generate" button to create voxel tower
6. Find the "Turret" child object (rotating part)
7. Add `Turret` component to root object
   - Assign TowerStats asset (created by setup helper)
   - Turret Rotation Part: Drag "Turret" child here
   - Fire Points: Leave empty (will use turret position)
   - Rotation Speed: 180
   - Show Range: true
8. Add `SphereCollider` for selection (isTrigger = false)
9. Save as prefab: `Assets/Prefabs/Towers/BasicTower.prefab`

### 3. Assign Enemy Prefab to Wave Config

1. Find `Assets/Resources/WaveConfigs/Wave_01.asset`
2. Expand "Enemy Spawns" list
3. Add new element:
   - Enemy Prefab: Drag BasicEnemy prefab
   - Count: 5
   - Spawn Delay: 1.0
   - Health Multiplier: 1.0
   - Speed Multiplier: 1.0
4. Repeat for Wave_02 through Wave_05 with increasing counts

### 4. Assign Tower to Shop UI

1. Find TowerShopUI component in scene
2. Add BasicTower prefab to "Towers" list
3. UI will auto-generate preview icons

## Scene Setup (if not using automated setup)

### Managers (Create as empty GameObjects)

1. **GameManager**
   - Add GameManager component
   - Logs: true (for debugging)

2. **ResourceManager**
   - Add ResourceManager component
   - Starting Currency: 500
   - Passive Income Per Wave: 100
   - Logs: false

3. **WaveManager**
   - Add WaveManager component
   - Waves: Assign all Wave configs from Resources/WaveConfigs
   - Loop Waves: true
   - Difficulty Scaling Per Loop: 1.5
   - Auto Start Next Wave: false (player clicks button)
   - Logs: true

4. **ProjectilePool**
   - Add ProjectilePool component
   - Projectile Prefab: Assign Projectile prefab
   - Initial Pool Size: 50
   - Max Pool Size: 200

### Spawn Points

Create 3 empty GameObjects:
- Name: SpawnPoint_1, SpawnPoint_2, SpawnPoint_3
- Tag: "SpawnPoint"
- Position: Spread across map edge
- Parent: Create "SpawnPoints" GameObject to organize

### Player Base

1. Create Cube GameObject
2. Scale: (5, 3, 5)
3. Position: Center of map or end of path
4. Add `Base` component
   - Max Health: 20
5. Material: Green color

### NavMesh

1. Select ground/terrain objects
2. Mark as "Navigation Static" in Inspector
3. Window → AI → Navigation
4. Click "Bake" button
5. Verify blue overlay appears on walkable surfaces

### UI Setup

1. Create Canvas (if not exists)
   - Render Mode: Screen Space - Overlay
2. Add `GameHUD` component to Canvas
3. Create UI elements:
   - **Currency Text** (TextMeshPro)
   - **Wave Text** (TextMeshPro)
   - **Enemies Text** (TextMeshPro)
   - **Base Health Text** (TextMeshPro)
   - **Base Health Slider** (Slider)
   - **Wave Progress Slider** (Slider)
   - **Start Wave Button** (Button)
   - **Game Over Panel** (Panel, disabled by default)
4. Assign all UI elements to GameHUD component fields

## Testing

### Test 1: Managers Initialization
1. Press Play
2. Check Console for logs:
   - `[GameManager] State changed: Menu -> Playing`
   - No errors about missing references

### Test 2: Wave Spawning
1. Click "Start Wave" button
2. Check Console:
   - `[WaveManager] Starting wave 1, enemies: X`
3. Verify enemies spawn at spawn points
4. Verify enemies move toward base using NavMesh

### Test 3: Tower Targeting
1. Place tower using placement system (existing)
2. Start wave
3. Enable "Logs" on Turret component
4. Check Console:
   - `[Turret] TowerName selected target: EnemyName using Nearest priority`
   - `[Turret] TowerName firing at EnemyName`

### Test 4: Projectiles & Damage
1. Enable "Logs" on Projectile prefab
2. Watch tower fire
3. Check Console:
   - `[Projectile] Impact at (x,y,z), damage: 10, area: False`
4. Verify enemy health decreases (red flash)
5. Verify enemy dies at 0 HP

### Test 5: Economy
1. Kill enemy
2. Check currency increases
3. Check Console (if Logs enabled on ResourceManager):
   - `[ResourceManager] Gained 10, total: 510`
4. Complete wave
5. Check completion reward + passive income

### Test 6: Game Over
1. Let enemies reach base
2. Verify base health decreases
3. When base HP = 0:
   - Game Over panel appears
   - Time stops
   - Console: `[GameManager] State changed: Playing -> GameOver`

## Common Issues

### Enemies don't move
- Check NavMesh is baked
- Verify ground has "Navigation Static" checked
- Check Base exists in scene for pathfinding target

### Towers don't shoot
- Verify TowerStats asset is assigned
- Check tower has projectile pool reference
- Enable Logs to see targeting messages
- Verify enemies have colliders for detection

### Projectiles don't hit
- Check Projectile prefab has correct impact radius
- Verify enemies have colliders
- Enable Logs on Projectile to see impact messages

### UI doesn't update
- Verify all UI elements assigned to GameHUD
- Check managers exist in scene and initialized
- Look for null reference errors in Console

### No currency gain
- Check EnemyHealth has "Give Reward" enabled
- Verify ResourceManager exists
- Enable Logs on ResourceManager to track currency

## Next Steps

After Core Loop is working:
1. Create more tower types with different stats
2. Create more enemy types with different profiles
3. Tune wave configs for difficulty curve
4. Add tower upgrade UI
5. Implement run-based upgrade system
6. Add VFX and audio
7. Create main menu scene

## Debug Controls

**Logs Toggle**: Use Inspector checkboxes on:
- GameManager - Game state changes
- WaveManager - Wave start/complete, enemy spawning
- ResourceManager - Currency transactions
- Turret - Targeting and firing
- Projectile - Impact detection

**Recommended for initial testing**:
- GameManager: Logs = true
- WaveManager: Logs = true
- Turret: Logs = false (too spammy)
- Projectile: Logs = false (too spammy)
- ResourceManager: Logs = false

Enable Turret/Projectile logs only when debugging specific issues.
