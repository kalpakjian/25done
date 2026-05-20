# 25done — AI 協作改進紀錄

這是一個使用 **Unity 2021.3.45f2** 製作的 3D 動作地城遊戲專案。專案最初從一個基礎場景出發，透過與 AI 反覆協作，逐步加入程序化房間、玩家戰鬥、敵人 AI、弓箭手遠程攻擊、鏡頭遮擋淡化、傳送門效果與多項戰鬥手感調整。

本 README 主要記錄：我如何使用 AI 從基礎場景開始，逐步把專案改造成現在這個可遊玩的動作遊戲雛形。

---

## 專案資訊

- **引擎版本**：Unity 2021.3.45f2
- **渲染管線**：Universal Render Pipeline（URP 12.1.15）
- **主要平台**：Android / Unity Editor
- **主要語言**：C#
- **核心套件**：
  - Unity AI Navigation
  - Universal Render Pipeline
  - TextMeshPro
  - Visual Effect Graph
  - Unity UI

---

## 從基礎場景到現在的 AI 改進總結

### 1. 程序化房間與牆壁生成

一開始專案只有較基礎的場景內容，後續透過 AI 協助加入並重構了房間生成系統。

相關腳本：

- `Assets/Script/RoomGenerator.cs`
- `Assets/Script/WallGenerator.cs`
- `Assets/Script/FloorArraySpawner.cs`

主要改進：

- 加入房間地板與牆壁的程序化生成。
- 支援角落、牆面 prefab 的自動擺放。
- 使用可重複的 wall bag / shuffle 機制，避免牆壁樣式過於單調。
- 提供 `GenerateRoom()`、`ClearRoom()`、`BakeRoom()` 等工具方法，方便在 Unity Editor 中快速重建房間。
- 使用 Gizmos 輔助檢視房間邊界與生成範圍。

這讓原本固定的場景逐步變成可以快速調整、可重建、可擴充的地城房間系統。

---

### 2. 玩家操作與戰鬥手感

AI 協助強化了玩家的行動、攻擊、翻滾、連段與自動面向敵人等功能。

相關腳本：

- `Assets/Script/Player/PlayerController.cs`
- `Assets/Script/Player/PlayerAttack.cs`
- `Assets/Script/Player/PlayerWeapon.cs`
- `Assets/Script/Player/NextAttack.cs`
- `Assets/Script/Player/AutoTarget.cs`
- `Assets/Script/Attack.cs`

主要改進：

- 加入觸控輸入判斷：點擊攻擊、拖曳移動、快速拖曳翻滾、長按防禦/蓄力偵測。
- 加入玩家自動面向最近敵人的邏輯，讓手機操作更順手。
- 攻擊命中後才開放下一段連擊，讓連段更有打擊感。
- 第二、第三段攻擊可向前踏步，改善近戰攻擊容易打空的問題。
- `Attack` 資料結構統一傳遞傷害、攻擊位置、屬性、擊退強度與是否可推敵人。
- 玩家武器命中敵人時會註冊命中狀態，供連段系統使用。

這些改進讓玩家從單純移動角色，變成可以進行接近、攻擊、連段、翻滾與戰鬥判定的完整角色控制器。

---

### 3. 敵人基礎 AI 與受傷系統

敵人系統從基礎追蹤與攻擊，逐步改造成可擴充的繼承架構。

相關腳本：

- `Assets/Script/Enemy/Enemy.cs`
- `Assets/Script/Enemy/EnemyController.cs`
- `Assets/Script/Enemy/BossController.cs`
- `Assets/Script/Enemy/EnemyAttack.cs`
- `Assets/Script/Enemy/EnemyWeapon.cs`

主要改進：

- 建立 `Enemy` 基底類別，集中處理：
  - 追蹤玩家
  - 攻擊距離判斷
  - HP 與 HP Bar
  - 受傷閃白 emission
  - 死亡與掉落物
  - 冰凍等攻擊屬性效果
- 敵人攻擊使用 Animator StateMachineBehaviour 控制有效傷害時間。
- Boss 可覆寫受傷流程，加入音效與不同的冰凍速度處理。
- 敵人武器使用 Trigger 判斷玩家命中。

這使得後續加入新敵人時，可以直接繼承 `Enemy`，只改寫特殊行為。

---

### 4. 弓箭手 Rogue：距離維持、遠程攻擊與彈藥系統

弓箭手是 AI 協作中改動最多的敵人之一，從一般近戰敵人逐步變成具有遠程戰鬥邏輯的 Rogue。

相關腳本：

- `Assets/Script/Enemy/RogueController.cs`
- `Assets/Script/Enemy/Arrow.cs`
- `Assets/Arrow.prefab`

主要改進：

- 弓箭手會與玩家維持指定距離：
  - 太近會後退。
  - 太遠會靠近。
  - 在理想距離會停下來射擊。
- 加入箭矢數量與上膛機制：
  - `maxArrows`
  - `currentArrows`
  - `reloadTime`
  - 上膛倒數 Debug UI
- 加入瞄準階段與射擊階段：
  - `aimTime`
  - `arrowReleaseDelay`
  - `attackLockTime`
- 支援 Animation Event 或自動延遲射箭。
- 射箭時從 `firePoint` 產生箭矢。
- 箭矢傷害會乘上 Rogue 的 `power`。

近期也針對弓箭手戰鬥手感做了重要修正：

- 箭矢不再只用水平線瞄準玩家腳底，而是從出箭點瞄準玩家 Collider 中心。
- 箭矢加入 `SphereCastAll` 補判，避免高速或 Collider 太細導致穿過玩家。
- 箭矢 Rigidbody 在執行時改為不受重力干擾，避免飛行高度偏掉。
- 命中玩家子 Collider 時，也會往父物件尋找 `PlayerLife` 並呼叫受傷。
- 弓箭手被玩家打中時加入硬直：
  - 停止移動。
  - 中斷瞄準與射箭。
  - 預設忽略玩家擊退，避免被推開導致玩家連段落空。

這讓 Rogue 從單純敵人進化成具有「距離控制、彈藥管理、瞄準射擊、受擊中斷」的遠程敵人。

---

### 5. 鏡頭與遮擋淡化系統

在 3D 地城中，牆壁容易擋住玩家。AI 協助加入多版本的鏡頭遮擋淡化方案。

相關腳本與資源：

- `Assets/Script/CameraFollow.cs`
- `Assets/Script/CameraObstructionFader_BuiltIn.cs`
- `Assets/Script/CameraObstructionFader_GridCast.cs`
- `Assets/Script/CameraObstructionFader_ModularWalls.cs`
- `Assets/Resources/ObstructionTransparent.shader`

主要改進：

- 鏡頭跟隨玩家，並提供快速 Snap 方法。
- 當牆壁或物件擋在鏡頭與玩家之間時，自動把遮擋物淡化。
- 提供不同偵測策略：
  - 單線/內建偵測
  - Grid Cast 多點偵測
  - Modular Walls 專用偵測
- 暫存 Renderer / Material 狀態，淡出後可還原原本材質。
- 支援 URP 材質與自訂透明 shader。

這項改進大幅提升了地城場景中的可視性，避免玩家被牆壁完全擋住。

---

### 6. 傳送門與場景互動效果

專案也加入了傳送門與視覺效果，讓場景不只是靜態房間。

相關腳本：

- `Assets/Script/Portal.cs`
- `Assets/Script/PortalLightEffect.cs`
- `Assets/Script/TriggerEvent.cs`
- `Assets/Script/CumulativeEvent.cs`

主要改進：

- 傳送門可啟用、縮放出現、縮放消失。
- 傳送門可作為目的地，處理玩家進入 Trigger 後的行為。
- 加入點光源淡入淡出效果，提升傳送門的存在感。
- 支援事件觸發與累積事件，可用於房間通關、開門、啟動機關等設計。

---

### 7. 視覺、打擊回饋與遊戲感

AI 也協助加入多個提升遊戲感的輔助系統。

相關腳本：

- `Assets/Script/AttackEffect.cs`
- `Assets/Script/CameraShake.cs`
- `Assets/Script/SlowMotion.cs`
- `Assets/Script/AnimationSound.cs`
- `Assets/Script/PostProcessHeightFog.cs`
- `Assets/Script/BloomSetup.cs`
- `Assets/Script/CameraFacingBillboard.cs`

主要改進：

- 攻擊狀態進入/離開時控制特效。
- 玩家受傷時觸發 Camera Shake。
- 命中與死亡可觸發 Slow Motion，增加打擊感。
- 動畫狀態可播放音效。
- 加入高度霧與 Bloom 相關輔助，強化地城氛圍。
- HP Bar / Billboard 類 UI 可面向鏡頭。

---

### 8. 道具、貨幣、武器與 UI 系統

專案中也逐步加入了基本的成長與互動系統。

相關腳本：

- `Assets/Script/TreasureBox.cs`
- `Assets/Script/CrystalCount.cs`
- `Assets/Script/Attract.cs`
- `Assets/Script/SwitchWeapon.cs`
- `Assets/Script/WeaponInventory.cs`
- `Assets/Script/IAPManager.cs`
- `Assets/Script/IAPButtonHelper.cs`

主要改進：

- 寶箱可被攻擊開啟。
- 水晶/貨幣數量可更新顯示。
- 掉落物可被吸引到玩家身上。
- 支援武器切換與武器購買邏輯。
- 預留 IAP 購買入口，例如水晶包與移除廣告。

---

## AI 協作方式

這個專案的改進不是一次完成，而是透過多輪 AI 協作逐步完成：

1. **提出問題或需求**  
   例如：「弓箭手射不中玩家」、「玩家打弓箭手很容易落空」、「牆壁擋住鏡頭」。

2. **AI 檢查現有程式碼**  
   先閱讀相關腳本，理解現有架構與資料流。

3. **找出原因**  
   例如：
   - 箭矢只瞄準水平線，忽略高度。
   - 高速箭矢用每幀位移，可能穿過 Collider。
   - Rogue 被打後沒有硬直，仍然後退或射箭。

4. **小範圍修改**  
   優先修改最相關的腳本，避免大規模破壞現有架構。

5. **檢查與測試建議**  
   修改後使用 Git diff / 語法檢查，並提供 Unity 內的測試方式。

---

## 近期 Git 改進紀錄摘要

以下是近期改進方向的摘要：

- 強化 Rogue 弓箭手：命中判定、瞄準精度、受傷硬直。
- 加入鏡頭遮擋淡化 shader 與多種 fader 實作。
- 強化玩家攻擊：瞄準、擊退、連段踏步。
- 新增箭矢 projectile 與 Rogue 射擊機制。
- 改進 Rogue 距離維持與攻擊行為。
- 重構程式結構，提高可讀性與可維護性。
- 加入程序化房間、地板與牆壁生成。

---

## 目前專案特色

- 程序化房間與牆壁生成。
- 手機觸控導向的玩家操作。
- 近戰連段、翻滾、命中確認與攻擊踏步。
- 基礎敵人、Boss 與 Rogue 遠程敵人。
- Rogue 具備距離控制、彈藥、上膛、瞄準、射箭與受擊中斷。
- 箭矢具備連續碰撞補判，降低穿模漏判。
- 鏡頭遮擋淡化，提升地城可視性。
- 傳送門、光效、慢動作、震動與攻擊特效。
- 水晶、寶箱、吸引物、武器切換與購買系統雛形。

---

## 建議後續改進

- 加入完整主選單、暫停選單與關卡選擇。
- 將 Debug UI 改成正式遊戲 UI。
- 補上音效與打擊特效的統一管理。
- 為 Rogue / Boss 加入更清楚的攻擊前搖提示。
- 加入更多敵人類型與狀態效果。
- 將程序化房間串接成完整地城流程。
- 建立測試場景，專門測試攻擊判定、箭矢命中與鏡頭遮擋。
- 優化 Android 效能與材質透明切換成本。

---

## 專案結語

這個專案展示了使用 AI 協作開發 Unity 遊戲的流程：從一個基礎場景開始，透過不斷提出具體問題、檢查程式碼、修正戰鬥手感與增加系統，逐步形成一個具有完整雛形的 3D 動作地城遊戲。

AI 在這個過程中扮演的角色不是單純產生程式碼，而是協助分析問題、拆解系統、維持既有架構，並在每一次修改後提供測試方向。這讓專案能夠快速從原型推進到更接近可遊玩的版本。