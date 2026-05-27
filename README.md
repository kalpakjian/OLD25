# OLD25 — 超精簡 README

## 這個專案現在怎麼運作

戰鬥核心已統一成：**所有可戰鬥角色都走 `CombatActor` + `AttackData`**。[1][2]

目前主要角色結構：

```text
CombatActor
├── PlayerController
└── EnemyBase
    └── BossController
```

- `CombatActor`：統一處理 HP、受傷、死亡、擊退、狀態效果。[1][2]
- `PlayerController`：處理玩家輸入、移動、鎖敵朝向、roll 方向、攻擊觸發。[3][1]
- `EnemyBase`：處理敵人追擊、轉身、攻擊距離與移動邏輯。[2]

***

## 傷害流程

所有傷害都應該組成 `AttackData`，然後呼叫目標的 `TakeDamage(AttackData)`。[1][2]

標準流程：

```text
攻擊動畫 / Hitbox
→ 組裝 AttackData
→ 找到目標 CombatActor
→ target.TakeDamage(attack)
```

不要再新增舊式 `Hurt(float)` 直打流程，新的功能一律走 `AttackData`。[1][2]

***

## Animator 規格

Player 和 Enemy 現在都已經移除 `CombatRotate` 依賴，旋轉不再由 Animator 控制。[1][2]

共用核心參數：

- `speed`
- `attack`
- `hurt`
- `die`
- `roll`（只有部分角色需要，例如 Player）[1][2]

共用原則：

- Controller 決定移動、旋轉、攻擊時機。[3][2]
- Animator 只負責播 `move / attack / roll / hurt / die` 和狀態效果。[1][2]

***

## 旋轉規則

### Player

- 移動中：只面向輸入方向。[3]
- 待機時：若戒備範圍內有敵人，面向最近敵人。[3]
- roll：鎖定觸發當下方向，不可被鎖敵覆蓋。[3]
- 攻擊前：可先朝最近敵人再出手。[3][1]
- hurt / die：不做自動轉向。[1][3]

### Enemy

- 由 `EnemyBase` 控制朝向玩家。[2]
- 未轉正前不移動、不攻擊。[2]
- Animator 不再負責敵人轉向。[2]

***

## 新增一個可戰鬥角色時要做什麼

### 如果是新敵人

1. 繼承 `EnemyBase`。[2]
2. 設定追擊距離、攻擊距離、攻擊間隔等數值。[2]
3. 準備符合共用 Animator 參數規格的動畫。[2]
4. 攻擊 hitbox 透過 `AttackData` 打目標。[2]

### 如果是新玩家角色 / 可切換角色

1. 繼承 `CombatActor`，或做一個新的控制器類別。[1][3]
2. 旋轉邏輯放在 Controller，不要放回 Animator。[1][2]
3. Animator 參數盡量維持 `speed / attack / roll / hurt / die`。[1][2]
4. 所有傷害來源仍然走 `AttackData`。[1][2]

***

## 現在不要再做的事

- 不要把旋轉邏輯寫回 Animator StateMachineBehaviour。[1][2]
- 不要重新依賴 `CombatRotate`、`RotatableMotion`、`EnemyRotate`、`AutoTarget` 這類舊做法。[1][2]
- 不要新增繞過 `AttackData` 的臨時傷害入口。[1][2]
- 不要讓 Player 移動中自動面向敵人，除非未來真的加入移動攻擊設計。[3]

***

## 下次打開專案先檢查

- Player / Enemy Animator 是否仍遵守共用參數規格。[1][2]
- 新攻擊是否都有正確組裝 `AttackData`。[1][2]
- 新角色的旋轉是否還留在 Controller，而不是 Animator。[3][2]
- 專案裡是否還殘留舊系統腳本引用。[1][2]