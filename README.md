***

# OLD25 — 傷害與戰鬥系統重構紀錄

## 專案簡介

本專案為 Unity 3D 動作遊戲，包含玩家（Player）與敵人（Enemy / Boss）的戰鬥系統。  
核心目標是將分散的傷害邏輯整合成結構化的 `AttackData` 流程，並以 `CombatActor` 作為統一基底，讓玩家與敵人共用同一套受傷與死亡機制。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/f8b636e8-ec82-4bf1-a1c1-dcbd8b81a42c/Player_animator_dump.txt)

***

## 目前核心架構

### CombatActor 家族

```text
CombatActor（抽象基底）
├── PlayerController（玩家）       faction = Player
└── EnemyBase（敵人基底）         faction = Enemy
    └── BossController（Boss）    受傷音效 + 冰凍速度（例如 0.8f）
```

`CombatActor` 負責：

- 血量與死亡流程（HP、死亡旗標、延遲銷毀等）
- 受傷間隔（hurt interval）與多次傷害防護
- 冰凍動畫速度、受擊特效（例如閃白、色調變化）
- `TakeDamage(AttackData)`、`ApplyKnockback`、`ApplyStatusEffect`
- 鉤子：`OnInitialized`、`OnTakeDamage`、`OnDie` [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/73e06f0f-ad9b-4d55-8e0e-51d401376f0d/Enemy_animator_dump.txt)

### AttackData 流程

```text
AttackData
├── attacker        : GameObject
├── attackerFaction : Faction（Player / Enemy / Neutral）
├── damage          : float
├── position        : Vector3（通常為命中位置，用於擊退方向）
├── type            : AttackType（normal / frozen / ...）
└── strength        : int（擊退 / 狀態強度）
```

所有傷害來源（玩家、敵人、Boss）都會組裝 `AttackData`，並呼叫目標的 `CombatActor.TakeDamage(attack)`。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/f8b636e8-ec82-4bf1-a1c1-dcbd8b81a42c/Player_animator_dump.txt)

### 武器與攻擊管線

```text
PlayerAttack（SMB） ──→ WeaponHitbox ──→ FindCombatActor() ──→ target.TakeDamage(AttackData)
EnemyAttack（SMB）  ──→ WeaponHitbox ──→ FindCombatActor() ──→ target.TakeDamage(AttackData)
```

- `WeaponHitbox`：統一處理所有武器碰撞（玩家 / 敵人）。  
- 使用 Phase 概念，在同一次攻擊週期內，同一個目標只會吃一次傷害，避免多重碰撞溢傷。  
- 支援對角色（CombatActor）以及特定互動物件（如寶箱）的命中判定。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/73e06f0f-ad9b-4d55-8e0e-51d401376f0d/Enemy_animator_dump.txt)

***

## Animator 共用規格（Player / Enemy）

### 參數

目前 Player / Enemy Animator 已共用同一套核心參數： [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/f8b636e8-ec82-4bf1-a1c1-dcbd8b81a42c/Player_animator_dump.txt)

- `speed` : Float  
  控制 locomotion Blend Tree，0=idle，0.5=walk，1=run。
- `attack` : Trigger  
  觸發攻擊動畫。
- `hurt` : Trigger  
  從 Any State 進入受傷。
- `die` : Trigger  
  從 Any State 進入死亡。
- `roll` : Trigger（可選）  
  目前主要由 Player 使用。

### 狀態與流程

**Base Layer 共用骨架：** [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/73e06f0f-ad9b-4d55-8e0e-51d401376f0d/Enemy_animator_dump.txt)

- `move`（Default State）  
  - Motion: Blend Tree（idle / walk / run）  
  - 驅動來源：Controller 設定 `speed`。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/f8b636e8-ec82-4bf1-a1c1-dcbd8b81a42c/Player_animator_dump.txt)

- 攻擊狀態  
  - Player：`attack01 → attack02 → attack03`（連段）。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/f8b636e8-ec82-4bf1-a1c1-dcbd8b81a42c/Player_animator_dump.txt)
  - Enemy：單一 `attack` 狀態。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/73e06f0f-ad9b-4d55-8e0e-51d401376f0d/Enemy_animator_dump.txt)

- `roll`（可選）  
  - 目前主要為 Player 翻滾使用。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/f8b636e8-ec82-4bf1-a1c1-dcbd8b81a42c/Player_animator_dump.txt)

- `hurt`  
  - Any State → `hurt`（Trigger：`hurt`）。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/73e06f0f-ad9b-4d55-8e0e-51d401376f0d/Enemy_animator_dump.txt)

- `die` / `death`  
  - Any State → 死亡狀態（Trigger：`die`）。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/73e06f0f-ad9b-4d55-8e0e-51d401376f0d/Enemy_animator_dump.txt)

**共通轉場規則：** [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/f8b636e8-ec82-4bf1-a1c1-dcbd8b81a42c/Player_animator_dump.txt)

- `move -> attack*`：`attack` Trigger，無 Exit Time。  
- `move -> roll`：`roll` Trigger，無 Exit Time（只有有翻滾能力的角色）。  
- `attack* -> move`：有 Exit Time，自動回 `move`。  
- `hurt -> move`：有 Exit Time，自動回 `move`。  
- `roll -> move`：有 Exit Time，自動回 `move`。  
- `Any State -> hurt`：`hurt` Trigger。  
- `Any State -> die`：`die` Trigger。

### StateMachineBehaviour 掛載規則

目前 Animator 只保留「狀態效果」類的 Behaviour，不再承擔旋轉或 AI 控制。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/73e06f0f-ad9b-4d55-8e0e-51d401376f0d/Enemy_animator_dump.txt)

- 攻擊（`attack`, `attack01~03`）  
  - `AnimationSound`  
  - `AttackEffect`  
  - `CombatAttack`  
  - `NextAttack`（僅連段角色）

- 翻滾（`roll`）  
  - `AnimationSound`  
  - `Invincible`（翻滾期間無敵）

- 受傷（`hurt`）  
  - `AnimationSound`

- 死亡（`die` / `death`）  
  - `AnimationSound`

***

## 控制層責任分配

### PlayerController（玩家控制）

- 繼承 `CombatActor`，負責：  
  - 觸控輸入 → 移動方向與速度計算。  
  - 旋轉：  
    - 移動中：朝輸入方向。  
    - 待機時：若戒備範圍有敵人，朝最近敵人。  
    - roll：鎖定觸發當下方向。  
    - 攻擊前：可先朝最近敵人再出手。  
  - 按需設定 Animator 參數：`speed / attack / roll / hurt / die`。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/c7471fe5-acdd-4579-bc9a-2efb2b9b45f2/PlayerController.cs)

### EnemyBase（敵人基底）

- 繼承 `CombatActor`，負責：  
  - NavMesh 尋路與追擊邏輯。  
  - 攻擊判定邏輯（追擊距離、攻擊距離、攻擊間隔）。  
  - 旋轉：  
    - 以 `RotateTowards` 朝目標轉向。  
    - 未轉正前不移動、不攻擊。  
  - 設定 Animator 參數：`speed / attack / hurt / die`。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/5f6f3d82-0966-433e-88eb-1024c538c00d/EnemyBase.cs)

Boss 由 `BossController : EnemyBase` 擴充受傷音效與冰凍動畫速度等特殊行為。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/5f6f3d82-0966-433e-88eb-1024c538c00d/EnemyBase.cs)

***

## 重要已刪檔案（已完成重構）

以下 C# 檔案已確定不再使用，可視為歷史記錄：

| 檔案 | 說明 |
|------|------|
| `Assets/Script/NEW/CombatRotate.cs` | 原通用旋轉 SMB，Player / Enemy 現已改由 Controller 控制面向。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/98d02764-3dec-4eb5-a33b-2161d6a10d89/CombatRotate.cs) |
| `Assets/Script/Player/AutoTarget.cs` | 原玩家自動鎖定腳本，邏輯已移入 `PlayerController`。 |
| `Assets/Script/Enemy/EnemyRotate.cs` | 原敵人旋轉 SMB，已由 `EnemyBase` 接手控制。 |
| `Assets/Script/Player/RotatableMotion.cs` | 原移動/攻擊旋轉 SMB，已由 `PlayerController` 接手控制。 |

（舊版 `Enemy` / `EnemyController` 如仍存在專案中，僅保留為過去版本參考；新戰鬥系統只依賴 `CombatActor` / `EnemyBase` 家族。）

***

## 檔案一覽（現行系統）

| 檔案 | 說明 |
|------|------|
| `Assets/Script/NEW/Faction.cs` | 陣營 enum（Player / Enemy / Neutral）。 |
| `Assets/Script/NEW/AttackData.cs` | 傷害資料結構。 |
| `Assets/Script/NEW/CombatActor.cs` | 抽象基底：HP、受傷、死亡、特效、狀態。 |
| `Assets/Script/NEW/EnemyBase.cs` | 新版敵人基底：NavMesh + 攻擊 AI。 |
| `Assets/Script/Enemy/BossController.cs` | Boss：繼承 EnemyBase，加受傷音效與自訂冰凍速度。 |
| `Assets/Script/NEW/WeaponHitbox.cs` | 統一武器碰撞與 Phase 判定，支援玩家與敵人。 |
| `Assets/Script/Enemy/EnemyWeapon.cs` | （如仍存在）可逐步收斂到 WeaponHitbox 管線。 |
| `Assets/Script/Enemy/EnemyAttack.cs` | 敵人攻擊 SMB：控制攻擊窗口與傷害啟用/關閉。 |
| `Assets/Script/Player/PlayerController.cs` | 玩家：繼承 CombatActor，觸控移動、翻滾與鎖敵。 |
| `Assets/Script/Player/PlayerWeapon.cs` | 玩家武器命中處理（若已整合 WeaponHitbox，可標註為 legacy）。 |
| `Assets/Script/Player/PlayerAttack.cs` | 玩家攻擊 SMB：控制攻擊窗口。 |
| `Assets/Skeleton/Player.controller` | Player Animator（共用參數規格）。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/f8b636e8-ec82-4bf1-a1c1-dcbd8b81a42c/Player_animator_dump.txt) |
| `Assets/Skeleton/Enemy.controller` | Enemy Animator（共用參數規格）。 [ppl-ai-file-upload.s3.amazonaws](https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/67336197/73e06f0f-ad9b-4d55-8e0e-51d401376f0d/Enemy_animator_dump.txt) |

***
