# OLD25 — 傷害系統重構紀錄

## 專案簡介

本專案為 Unity 3D 動作遊戲，包含玩家（Player）與敵人（Enemy / Boss）的戰鬥系統。  
此次重構目標：**將分散的傷害計算統一成結構化的 `AttackData` 資料格式**，讓所有傷害來源（玩家、敵人、BOSS）都走同一套流程，並為冰凍、擊退等狀態效果預留擴充空間。

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

## 重構後架構總覽

```
AttackData（傷害資料結構）
├── attacker       : GameObject（攻擊者物件）
├── attackerFaction: Faction（Player / Enemy）
├── damage         : float
├── position       : Vector3（傷害來源位置，用於擊退方向）
├── type           : AttackType（normal / frozen / ...）
└── strength       : int（擊退強度，0 = 不擊退）

PlayerWeapon  ──→ enemy.TakeDamage(AttackData)
EnemyWeapon   ──→ player.TakeDamage(AttackData)

EnemyAttack（StateMachineBehaviour）
└── 設定 weapon.type / weapon.strength

PlayerAttack（StateMachineBehaviour）
└── 設定 weapon.type / weapon.strength
```

---

## 主要改動的檔案

| 檔案 | 說明 |
|------|------|
| `Assets/Script/NEW/Faction.cs` | 陣營 enum（Player / Enemy） |
| `Assets/Script/NEW/AttackData.cs` | 傷害資料結構 |
| `Assets/Script/Enemy/Enemy.cs` | 加 faction、TakeDamage、Hurt 過渡方法 |
| `Assets/Script/Enemy/BossController.cs` | 改用 OnTakeDamage 鉤子 |
| `Assets/Script/Enemy/EnemyWeapon.cs` | 改用 AttackData 打擊玩家 |
| `Assets/Script/Enemy/EnemyAttack.cs` | 補 type / strength 並傳給武器 |
| `Assets/Script/Player/PlayerController.cs` | 加血量系統與 TakeDamage |
| `Assets/Script/Player/PlayerWeapon.cs` | 改用 AttackData 打擊敵人 |

---

## 後續可擴充方向

- 新增 `AttackType`（毒、燃燒、閃電等）只需擴充 enum，無需改武器邏輯
- 可加入傷害數字 UI、傷害事件（UnityEvent）等，只需在 `TakeDamage` 內擴充
- 舊 `Attack` struct 可在確認無殘餘依賴後移除
