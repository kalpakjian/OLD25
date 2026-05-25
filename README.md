# OLD25 — 傷害系統重構紀錄

## 專案簡介

本專案為 Unity 3D 動作遊戲，包含玩家（Player）與敵人（Enemy / Boss）的戰鬥系統。  
重構目標：**將分散的傷害計算統一成結構化的 `AttackData` 資料格式**，讓所有傷害來源（玩家、敵人、BOSS）都走同一套流程，並為冰凍、擊退等狀態效果預留擴充空間。

---

## 重構歷程（依 Git Commit 順序）

### 1. 初始架構（0e5bc3b）
- 建立 Unity 專案基礎設定
- 既有的傷害系統以鬆散的 `float damage` 傳遞，缺乏類型與擊退資訊

---

### 2. 新增 `faction` 欄位至 `Enemy`（f01cd42）

**修改檔案：** `Assets/Script/Enemy/Enemy.cs`

- 在 `Enemy` class 中加入 `public Faction faction = Faction.Enemy;`
- 為後續傷害來源判斷（攻擊者陣營）奠定基礎

---

### 3. 重構 `Enemy` 與 `BossController`（d6a087f）

**修改檔案：** `Assets/Script/Enemy/Enemy.cs`、`Assets/Script/Enemy/BossController.cs`

- `Enemy` 加入 `TakeDamage(AttackData)` 方法，處理：
  - 扣血、死亡判斷
  - 擊退（`Rigidbody.AddForce`）
  - 冰凍減速（`animator.speed = frozenAnimSpeed`）
  - 過渡用 `Hurt(float)` 保持舊呼叫相容
- `BossController` 改用 `OnTakeDamage` 鉤子覆寫，移除重複的受傷邏輯

---

### 4. 實作 `PlayerController` 血量與受傷系統（d2042a2）

**修改檔案：** `Assets/Script/Player/PlayerController.cs`

- 新增欄位：`faction`、`maxHP`、`hurtInterval`、`frozenAnimSpeed`
- 實作 `TakeDamage(AttackData)`：
  - 扣血、死亡、擊退、冰凍減速
  - `hurtInterval` 防止連續受傷
- 保留過渡用 `Hurt(float)` 包裝方法

---

### 5. 重構 `EnemyWeapon`（77f85ec）

**修改檔案：** `Assets/Script/Enemy/EnemyWeapon.cs`

- 移除直接呼叫 `player.Hurt(float)` 的舊寫法
- 新增 `type`（`AttackType`）與 `strength` 欄位
- `OnTriggerEnter` 組裝完整 `AttackData`，呼叫 `player.TakeDamage(attack)`
- 支援冰凍、擊退等特殊傷害效果

---

### 6. 重構 `EnemyAttack`（dd845e2）

**修改檔案：** `Assets/Script/Enemy/EnemyAttack.cs`

- 新增 `type`（預設 `AttackType.normal`）與 `strength`（預設 1）欄位
- `OnStateEnter` 將 `type` / `strength` 傳給 `EnemyWeapon`
- 敵人攻擊動畫現在可以在 Inspector 設定傷害類型與擊退強度

---

### 7. 重構 `PlayerWeapon`（10bc318）

**修改檔案：** `Assets/Script/Player/PlayerWeapon.cs`

- 移除舊 `Attack` struct 依賴
- `OnTriggerEnter` 組裝完整 `AttackData`，呼叫 `enemy.TakeDamage(attack)`
- Player 與 Enemy 武器雙方全面走 `AttackData`

---

### 8. 引入 `CombatActor` 抽象基底與 `EnemyBase`（a6bffa7）

**新增檔案：** `Assets/Script/NEW/CombatActor.cs`、`Assets/Script/NEW/EnemyBase.cs`  
**修改檔案：** `Assets/Script/Player/PlayerController.cs`、`Assets/Script/Enemy/BossController.cs`

- **`CombatActor`**：抽象基底 class，統一管理：
  - 血量、死亡、受傷 interval
  - HP 條 UI（自動尋找名為 `HPBar` 的子物件）
  - 冰凍色調、Emission 閃白特效
  - `TakeDamage(AttackData)`、`ApplyKnockback`、`ApplyStatusEffect`
  - 鉤子：`OnInitialized`、`OnTakeDamage`、`OnDie`
- **`EnemyBase : CombatActor`**：新版敵人基底
  - NavMesh 尋路、攻擊觸發、`faction = Faction.Enemy`
  - 死亡後掉落物品、延遲銷毀
- **`PlayerController`** 改繼承 `CombatActor`，移除重複的 HP / 受傷邏輯
- **`BossController`** 改繼承 `EnemyBase`，僅保留受傷音效與冰凍動畫速度覆寫
- `PlayerController` 加入 `AllowRotate` 屬性（get/set 包裝 `allowRotate`，維持外部相容）
- `CombatActor.TakeDamage` 加入 `Debug.Log` 方便追蹤傷害流

---

### 9. 修正新舊系統相容性 + `EnemyRotate` 重構（a8ec4da）

**修改檔案：**
- `Assets/Script/Enemy/BossController.cs`（AudioSource 改 `private`，修正空值防護）
- `Assets/Script/Enemy/EnemyRotate.cs`（新增 StateMachineBehaviour，統一攻擊時朝向玩家）
- `Assets/Script/Player/PlayerAttack.cs`（強化尋找方式，加 null 防護）
- `Assets/Script/Enemy/EnemyAttack.cs`（新增舊版 `Enemy` fallback）
- `Assets/Script/Player/PlayerWeapon.cs`（新增舊版 `Enemy` fallback 打擊目標）
- `Assets/Script/Enemy/EnemyWeapon.cs`（新增舊版 `Enemy` fallback 作為 owner）

**問題根源：**
- 場景 prefab 上仍掛舊版 `Enemy : MonoBehaviour`，但新版武器只找 `EnemyBase : CombatActor`，導致雙方打不到對方

**修正內容：**
- `PlayerAttack` / `EnemyAttack`：強化從自己→父層→root 子孫搜尋，加 null 防護與 Debug.Log
- `EnemyAttack`：新增 `enemyLegacy`（舊 `Enemy`）fallback，`OwnerPower` 屬性自動切換
- `PlayerWeapon`：先找 `CombatActor`，找不到改找舊 `Enemy` 作為受傷目標
- `EnemyWeapon`：先找 `EnemyBase`，找不到改找舊 `Enemy` 作為 owner；先找 `CombatActor`，找不到改找 `PlayerController` 作為目標
- `EnemyRotate`：攻擊動畫播放時鎖定旋轉、持續朝向玩家，動畫結束後恢復 NavMesh 旋轉控制，同時相容新版 `EnemyBase` 與舊版 `Enemy`

---

## 目前架構總覽

```
CombatActor（抽象基底）
├── PlayerController（玩家）   faction = Player
└── EnemyBase（敵人基底）      faction = Enemy
    └── BossController（Boss）  受傷音效 + 冰凍速度

AttackData（傷害資料）
├── attacker       : GameObject
├── attackerFaction: Faction（Player / Enemy）
├── damage         : float
├── position       : Vector3（用於擊退方向）
├── type           : AttackType（normal / frozen / ...）
└── strength       : int（擊退強度）

PlayerAttack（SMB）──→ PlayerWeapon ──→ target.TakeDamage(AttackData)
                                         ├── EnemyBase（新版）
                                         └── Enemy（舊版 fallback）

EnemyAttack（SMB）──→ EnemyWeapon ──→ target.TakeDamage(AttackData)
                                        ├── PlayerController（新版 CombatActor）
                                        └── PlayerController（舊版 fallback）

EnemyRotate（SMB）──→ 攻擊時強制朝向玩家，相容新舊 Enemy
```

---

## 檔案一覽

| 檔案 | 說明 |
|------|------|
| `Assets/Script/NEW/Faction.cs` | 陣營 enum（Player / Enemy / Neutral） |
| `Assets/Script/NEW/AttackData.cs` | 傷害資料結構 |
| `Assets/Script/NEW/CombatActor.cs` | 抽象基底：HP、受傷、死亡、特效 |
| `Assets/Script/NEW/EnemyBase.cs` | 新版敵人基底：NavMesh + 攻擊 AI |
| `Assets/Script/Enemy/Enemy.cs` | 舊版敵人（仍在使用中，漸進棄用） |
| `Assets/Script/Enemy/BossController.cs` | Boss：繼承 EnemyBase，加受傷音效 |
| `Assets/Script/Enemy/EnemyWeapon.cs` | 敵人武器：相容新舊 owner，打擊玩家 |
| `Assets/Script/Enemy/EnemyAttack.cs` | 敵人攻擊 SMB：相容新舊 owner，控制傷害窗口 |
| `Assets/Script/Enemy/EnemyRotate.cs` | 攻擊時朝向玩家的 SMB |
| `Assets/Script/Player/PlayerController.cs` | 玩家：繼承 CombatActor，觸控移動 |
| `Assets/Script/Player/PlayerWeapon.cs` | 玩家武器：相容新舊目標，打擊敵人 |
| `Assets/Script/Player/PlayerAttack.cs` | 玩家攻擊 SMB：控制傷害窗口 |

---

## 後續可擴充方向

- 新增 `AttackType`（毒、燃燒、閃電等）只需擴充 enum，無需改武器邏輯
- 場景 prefab 完全換成 `EnemyBase` 後，可移除舊 `Enemy` 相容的 fallback 程式碼
- 舊 `Attack` struct 可在確認無殘餘依賴後移除
- 可加入傷害數字 UI、傷害事件（UnityEvent）等，只需在 `CombatActor.TakeDamage` 內擴充
