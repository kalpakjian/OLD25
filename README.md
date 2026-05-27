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
  - 過渡用 `Hurt(Attack)` 保持舊呼叫相容
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

### 10. 移除武器與攻擊腳本的舊版 Enemy fallback（f964f9f）

**修改檔案：**
- `Assets/Script/Enemy/OLDEnemy.cs`（`Enemy.cs` 改名為 `OLDEnemy.cs`，class 名稱保持 `Enemy`）
- `Assets/Script/Enemy/EnemyController.cs`（新增：薄封裝，繼承 `Enemy` 並呼叫 `start/update/lateUpdate`）
- `Assets/Script/Enemy/EnemyAttack.cs`（移除 `enemyLegacy` fallback，僅保留 `EnemyBase`）
- `Assets/Script/Enemy/EnemyWeapon.cs`（移除舊 `Enemy` owner fallback，僅使用 `EnemyBase`）
- `Assets/Script/Player/PlayerWeapon.cs`（移除舊 `Enemy` 目標 fallback，僅使用 `CombatActor`）

**重點說明：**
- 場景 prefab 已全面換成繼承 `EnemyBase` 的新版敵人，舊版相容程式碼已可安全移除
- `PlayerWeapon` / `EnemyWeapon` / `EnemyAttack` 現在只與新版系統（`CombatActor` / `EnemyBase`）互動
- `EnemyRotate` 仍保留對舊版 `Enemy` 的 fallback（兼顧尚未完全替換的特殊場景物件）
- `OLDEnemy.cs` 本身保留作為 `EnemyController` 的基底類別，內含完整的 `TakeDamage(AttackData)` 實作

---

### 11. Level01 場景數值調整與欄位對齊（289638a）

**修改檔案：** `Assets/Level01.unity`

- 部分敵人的 `max判斷` 從 100 調高至 **200**，使戰鬥節奏更耐打
- 兩個武器組件的 `weaponDamage` 從 0 設定為 **10**，修正原本造成 0 傷害的問題
- 一種敵人類型的 script 參考換成新版 GUID（對應舊版 `EnemyController` → 新版繼承 `EnemyBase` 的腳本）
- 實體組件欄位排列重新整理：`faction`、`power`、`maxHP` 現在排在 `traceRange`、`attackRange` 等戰鬥欄位之前，與 `CombatActor` 的 Inspector 定義一致
- `AllowRotate` 改名為 `allowRotate`，與新．`CombatActor.allowRotate` 欄位名稱對齊

---

### 12. `WeaponHitbox` 邏輯統一與 Phase 判定優化（87a13cc, b258fcf, 67d26c2）

**修改檔案：** `Assets/Script/NEW/WeaponHitbox.cs`

- **邏輯統一**：移除玩家與敵人的專用武器腳本，改由單一 `WeaponHitbox` 處理所有碰撞邏輯。
- **Phase-based Hit Tracking**：引入 Phase 概念，在同一次攻擊週期（Phase）內，對同一個目標僅觸發一次傷害，有效防止多重碰撞判定導致的傷害溢出。
- **支援寶箱攻擊**：新增 `canHitTreasure` 欄位，允許玩家武器等特定武器能觸發寶箱（Treasure）的互動。

---

### 13. 實作 `CombatRotate` 統一旋轉機制（014c681, f7cb5d1）

**修改檔案：** `Assets/Script/Enemy/EnemyRotate.cs`

- **StateMachineBehaviour (SMB) 化**：將旋轉邏輯從 Animator 節點移出，改由 `CombatRotate` SMB 統一管理，減少 Animator Controller 的複雜度。
- **進階控制**：新增 `maxRange` 欄位，用於精確控制攻擊範圍內的目標偵測距離。

---

### 14. `EnemyController` 建立與 Metadata 更新（f9c80ce）

**修改檔案：** `Assets/Script/Enemy/EnemyController.cs`、`Assets/Level01.unity`

- **新版驅動器**：建立 `EnemyController` 作為舊版 `Enemy` (OLDEnemy) 的薄封裝，驅動其生命週期。
- **Metadata 同步**：更新場景資源的 Metadata，確保與新版腳本結構一致。

---

### 15. 移除 Animator 中的旋轉行為（e923eee）

**修改檔案：** `Assets/Skeleton/Enemy.controller`、`Assets/Warrior/Player.controller`

- **架構重構**：移除原本直接寫在 Animator 節點中的旋轉 logic，全面改用獨立的 SMB 進行管理，達成邏輯與動畫數據的分離。

---

## 目前架構總覽

```
CombatActor（抽象基底）
├── PlayerController（玩家）   faction = Player
└── EnemyBase（敵人基底）      faction = Enemy
    └── BossController（Boss）  受傷音效 + 冰凍速度（0.8f）

Enemy（舊版基底，OLDEnemy.cs）
└── EnemyController（薄封裝，直接呼叫 start/update/lateUpdate）

AttackData（傷害資料）
├── attacker       : GameObject
├── attackerFaction: Faction（Player / Enemy / Neutral）
├── damage         : float
├── position       : Vector3（用於擊退方向）
├── type           : AttackType（normal / frozen / ...）
└── strength       : int（擊退強度）

PlayerAttack（SMB）──→ PlayerWeapon ──→ FindCombatActor() ──→ target.TakeDamage(AttackData)
                                          └── EnemyBase（新版，僅此）

EnemyAttack（SMB）──→ EnemyWeapon ──→ FindCombatActor() ──→ target.TakeDamage(AttackData)
   └── 只找 EnemyBase               └── 只找 EnemyBase owner      └── CombatActor（新版，僅此）

EnemyRotate（SMB）──→ 攻擊時強制朝向玩家
   ├── 新版：EnemyBase.allowRotate
   └── 舊版 fallback：Enemy.AllowRotate（仍保留）
```

---

## 檔案一覽

| 檔案 | 說明 |
|------|------|
| `Assets/Script/NEW/Faction.cs` | 陣營 enum（Player / Enemy / Neutral） |
| `Assets/Script/NEW/AttackData.cs` | 傷害資料結構 |
| `Assets/Script/NEW/CombatActor.cs` | 抽象基底：HP、受傷、死亡、特效 |
| `Assets/Script/NEW/EnemyBase.cs` | 新版敵人基底：NavMesh + 攻擊 AI |
| `Assets/Script/Enemy/OLDEnemy.cs` | 舊版敵人基底（原 Enemy.cs，class 名稱保持 `Enemy`，漸進棄用） |
| `Assets/Script/Enemy/EnemyController.cs` | 舊版敵人薄封裝：繼承 `Enemy`，驅動 Update / LateUpdate |
| `Assets/Script/Enemy/BossController.cs` | Boss：繼承 EnemyBase，加受傷音效 + 冰凍速度 0.8f |
| `Assets/Script/Enemy/EnemyWeapon.cs` | 敵人武器：僅使用 EnemyBase，打擊 CombatActor 目標 |
| `Assets/Script/Enemy/EnemyAttack.cs` | 敵人攻擊 SMB：僅使用 EnemyBase，控制傷害窗口 |
| `Assets/Script/Enemy/EnemyRotate.cs` | 攻擊時朝向玩家的 SMB，仍保留舊版 Enemy fallback |
| `Assets/Script/Player/PlayerController.cs` | 玩家：繼承 CombatActor，觸控移動 + 翻滾 |
| `Assets/Script/Player/PlayerWeapon.cs` | 玩家武器：僅使用 CombatActor，打擊敵人 |
| `Assets/Script/Player/PlayerAttack.cs` | 玩家攻擊 SMB：控制傷害窗口 |
| `Assets/Script/Player/RotatableMotion.cs` | 觸控旋轉輔助 |

---

## 後續可擴充方向

- `EnemyRotate` 的舊版 `Enemy` fallback 可在確認所有 prefab 已換成 `EnemyBase` 後移除
- `OLDEnemy.cs` / `EnemyController.cs` 完全棄用後可整體刪除，徹底清除舊系統
- 新增 `AttackType`（毒、燃燒、閃電等）只需擴充 enum，無需改武器邏輯
- 可加入傷害數字 UI、傷害事件（UnityEvent）等，只需在 `CombatActor.TakeDamage` 內擴充
- 舊 `Attack` struct（`Assets/Script/Attack.cs`）確認無殘餘依賴後可移除
