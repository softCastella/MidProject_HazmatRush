# HazmatRush_해즈멧러쉬

2D 횡스크롤 환경에서 오염물질을 탐지·중화하는 Unity 게임입니다.  
방호복을 유지하며 이동하고, 오염원 유형에 맞는 아이템으로 오염원을 제거하는 것이 핵심 플레이입니다.

| 항목 | 내용 |
|------|------|
| **엔진** | Unity `6000.4.8f1` |
| **언어** | C# |
| **씬** | `AppScene` → `SplashScene` → `TitleScene` → (`IntroStoryScene`) → `LoadingScene` → `GameScene` · 튜토리얼 `5_TutorialScene` (타이틀 진입) · 이어하기는 Intro 생략 |
| **빌드 해상도** | **1280×720** (16:9) · Windowed · `resizableWindow` (비 16:9 리사이즈 시 HUD 깨질 수 있음) |
| **저장 (빌드)** | `[exe 폴더]/Save/gamesave.json` · 에디터 Play: `Documents/HazmatRush_Save/` |
| **회의·합의** | [회의록 폴더](Assets/Docs/회의록/) — 최근: [0614 일일 2차](Assets/Docs/회의록/2026-06-14-시각미상-0614-일일-오후2100-2차-합의.md) |
| **외부 QA 피드백** | [QA_Feedback/HazmatRush/](Assets/Docs/QA_Feedback/HazmatRush/) (PC v1.0 · 7건) |
| **스테이지 데이터** | `Assets/Data/stage_data.json` (`mapPollutants` 포함) |
| **회복 아이템 정의** | `Assets/Data/recovery_items.json` |
| **AI·협업 규칙** | [AGENTS.md](AGENTS.md) |
| **버그·수정 기록** | [Bug 폴더](Assets/Docs/Bug/) — 최근: [0614 일일 2차](Assets/Docs/Bug/2026-06-14-시각미상-0614-일일-오후2100-2차-fixes.md) |

---

## 게임 개요

플레이어는 제한된 구간(1차: `x` 약 **-785 ~ -403**)을 좌우로 이동하며 오염원에 접근합니다.  
이동 중 일정 시간이 지나면 경고 후 오염원이 **등장**하고, 접촉 시 방호복 HP와 오염원 HP가 실시간으로 변합니다.

- 스테이지의 **예정 오염원을 모두** 중화해야 구간을 나갈 수 있습니다 (중화 A~C + 가스 D, 아직 등장 전인 것 포함).
- `totalPollutant`만큼 중화하면 스테이지 클리어. 방호복 0 또는 시간 초과 시 게임 오버.
- 클리어 시 **별 3개** — 오염원 전부 중화 / 방호복 50% 이상 / 틀린 아이템 0회 (별은 **왼쪽부터** 달성 개수만큼 채움).

### 오염원 (A~C 중화 · D 가스)

| 타입 | 예시 물질 | 추천 아이템 | HP | DPS(플레이어) |
|------|----------|------------|-----|----------------|
| **TypeA** 부식성 | 염산, 황산, 질산 | 중화제 `Neutralizer` | 40 | 6 |
| **TypeB** 유류 | 폐유, 윤활유 등 | 오일패드 `OilPad` | 20 | 4 |
| **TypeC** 혼합화학액 | 폐산 혼합액 등 | 범용패드 `GeneralPad` | 55 | 9 |
| **TypeD** 가스 | 독성가스, 일산화탄소 등 | 가스밸브 `GasValve` | 24 | 5 |

- **A~C:** 한 번에 하나만 활성. 중화 전까지 다음 A~C 경고·등장 없음.
- **D(가스):** A~C와 별도 타이머로 등장 가능, 동시 존재 가능. 접촉 해제 시 A~C와 동일하게 **HP 초기화** (`pollutanMaxHp`).
- 가스 접촉 + 가스밸브 정답 시 **밸브 애니**(`Player_Valve`) + **밸브 SFX** (`squeakyValveSFX`). 가스 접촉 중 **중화 VFX 없음**.

### 아이템 DPS (중화용)

| 아이템 | DPS | 비고 |
|--------|-----|------|
| `Scanner` | 0 | 이동·탐지 기본 |
| `Neutralizer` | 12 | |
| `GeneralPad` | 14 | |
| `OilPad` | 8 | |
| `GasValve` | 12 | 가스 전용 |

---

## 조작

| 입력 | 동작 |
|------|------|
| `←` `→` / `A` `D` | 좌우 이동 |
| **`Z`** | 중화 아이템 **이전**(왼쪽) — 경고 후 ~ 해당 구간 오염원 처리 전 |
| **`X`** | 중화 아이템 **다음**(오른쪽) |
| **`K`** | **키 가이드** 패널 열기/닫기 (`KeyGuidePannel`) |
| **`I`** | **아이템 가이드** 패널 열기/닫기 (`ItemGuide`) |
| `C` / `V` | 회복 아이템 선택 (왼쪽 / 오른쪽) |
| `Space` | 회복 아이템 사용 |
| `ESC` | 일시정지 / 재개 |
| **타이틀 · 일시정지 `종료하기`** | 앱 종료 (에디터: Play 종료 · 빌드: `Application.Quit`) |
| `F1` | (디버그) 강제 클리어 |
| `F2` | (디버그) 강제 게임 오버 |

가이드 텍스트가 끝나면 이동·타이머·배경 스크롤이 활성화됩니다.  
로딩 씬에서도 이동·Z/X·C/V·**K/I**·ESC 안내가 순환 표시됩니다.

---

## 씬 흐름

```text
AppScene
  └ runInBackground · SceneLoadManager/AudioManager 생성 → SplashScene
SplashScene
  └ 로고 페이드인(0.65s, **카메라 흰 배경**) · 확대 → splashSFX → 로고 페이드아웃(1.5s) → 검정 암전(0.35s) → TitleScene (타이틀 선로드)
TitleScene
  └ 시작 → 검정 암전(0.25s) + 인트로 선로드 → IntroStoryScene
  └ 이어하기 → LoadingScene (인트로 생략 · `Save/gamesave.json` 있을 때만 버튼 표시)
  └ 우하단 `v{Application.version}` · **Default Cursor** (Player Settings)
IntroStoryScene
  └ (타이틀에서 온 경우) 검정 화면 SmoothStep 페이드인(0.8s) → 스크롤
  └ 종료 → 검은 암전(0.5s) → LoadingScene
LoadingScene
  └ stage_data + 오염원 배치 확정 (`PollutantSpawnPlan`) — 회복 아이템은 로딩 시 배치하지 않음
  └ 조작 안내 문구 순환 (LoadingGuideTxt)
  └ GameScene 로드
GameScene
  └ 스테이지 플레이 → 클리어 / 게임오버
```

> **BGM:** 타이틀·게임 씬만 BGM 재생(페이드 인/아웃). 스플래시·인트로·로딩에서는 BGM 없음(`StopBGM`). **게임 BGM**은 `StageManager.LoadStage` → `PlayStageBgm(bgmIndex)` — 씬 로드 시 0번 강제 재생 없음 ([0610 합의](Assets/Docs/회의록/2026-06-10-오후1430-0610-일일-오후1830-1차-합의.md)).

> **씬 페이드:** 스플래시 로고만 **흰 배경** · 그 외 씬 전환 **검정** (`SplashController` → `TitleSceneFade` → `AutoScrollIntro` → `LoadingController` · 맵 `PollutantManager`). 상세: [0611 합의](Assets/Docs/회의록/2026-06-11-오후1430-0611-일일-오후2100-2차-합의.md) · [0611 Bug](Assets/Docs/Bug/2026-06-11-오후1430-0611-일일-오후2100-2차-fixes.md) · [0606 이력](Assets/Docs/Bug/2026-06-06-시각미상-씬전환-스플래시-인트로-fixes.md).

### App 진입 (`AppScene` · PR1)

- Build index **0**. `AppBootstrap` — `Application.runInBackground = true`, `SceneLoadManager`로 `SplashScene` 싱글 로드.
- **PC 빌드 창 모드:** Player Settings **1280×720** Windowed · `resizableWindow` ([0612](Assets/Docs/Bug/2026-06-12-오후1700-0612-일일-오후1700-1차-fixes.md) · [0614 UI](Assets/Docs/Bug/2026-06-14-시각미상-0614-일일-오후2100-2차-fixes.md) §3)
- `SceneLoadManager`·`AudioManager`는 App에서 생성 후 `DontDestroyOnLoad` (Splash 로드 시 App 씬 오브젝트는 unload, 매니저만 유지).
- **서버·Additive 로딩은 미적용** (프론트 계획만). 상세: [회의록 0609 일일](Assets/Docs/회의록/2026-06-09-오후1227-0609-일일-오후1400-1차-합의.md) §App.
- **Play 루트 2가지:** (1) `AppScene` — 빌드·전체 플로우·백그라운드 실행 (2) `GameScene` 등 직접 Play — 개발용 · 해당 씬 매니저 사용. 중복 매니저는 **의도적 유지**(PR2 보류).

---

## 플레이 흐름 (한 구간)

```text
시작 가이드 종료 → 이동·타이머·배경 ON
  └ 1차 범위(-785~-403)에서 우측 이동 (누적 시간)
        └ ① 경고 WarningTxt — 고정 1.2s → 깜빡임 3회 (Bg~Bg(5)+WarningMsg 동시)
        └ ② Z/X 안내 GuideTxt (Stage 1-1만) 또는 itemSelectHintDuration 대기 (1-2+)
        └ ③ 오염원 등장 (로딩 때 만든 인스턴스 활성화 + 페이드인)
              └ ④ PopupUI: 「{물질}에 {추천도구}을 사용하세요」 — ①② 끝난 뒤
              └ A~C: PollutantArrow 바운스 3회 후 숨김 (1-2+, D·1-1 제외)
        └ 접촉: 판정 로그 → 방호복 감소 → 정답 아이템만 오염원 HP 감소
              └ 오답: 전 스테이지 오대응 패널티 2초 (dim + GuideTxt)
              └ 정답 A~C: 중화 VFX + 중화 SFX 루프
              └ D+가스밸브: 밸브 애니 + 밸브 SFX (중화 VFX 없음)
              └ 오답: 방호복만 감소, 중화 VFX·오염원 HP 감소 없음
              └ 오염원 제거(확률): 회복 아이템 아크 드랍 → 픽업 시 인벤
  └ 해당 구간 예정 오염원(중화+가스) 전부 처리 전
        → 맵 전환 불가, 배경 스크롤 정지
  └ 전부 처리 후
        ├ 스테이지에 남은 오염원 있음 → x=769까지 이동 → 페이드 → 시작 위치·배경 리셋 → 반복
        └ 마지막 오염원 → Player_Clear 2초 → ClearSet (별점 + 조건별 텍스트)
```

사망 시: Die 애니 + **`dieSplattSFX`** → `dieAnimDelay`(2초) → GameOverSet + **game-overSFX**.

---

## 핵심 시스템

### 플레이어 (`Player.cs`)

- 방호복 `curProtection` / `maxProtection` (float), UI는 정수 %
- 이동: 입력·애니 `Update`, **`Rigidbody2D.MovePosition`은 `FixedUpdate`** (물리·트리거 동기)
- 1차 범위 `leftLimit`~`rightLimit` — **이동 입력 있을 때만** `Clamp` (범위 밖에서 -403으로 끌림 방지)
- `moveSpeed` **200** (`Player` 프리팹·`GameScene`) — 배경 `scrollSpeed` **0.2**와 **함께** 튜닝 (`scrollSpeed ≈ moveSpeed / 1920` 참고 · 코드 기본 400과 씬 값 다를 수 있음)
- **배경 잠금 시 가속:** `bgLockedSpeedMultiplier` (씬 **1.7**) — `PollutantManager.IsBackgroundScrollLocked()`일 때만 (`GetMoveSpeed()`). 경고·전투·맵 회복 아이템·맵 전환. 1차 자유 이동 구간은 미적용.
- 걷기 애니: `State` 0/1 · 미끄러짐 시 Animator `Player_Move` Speed 또는 클립 `SampleRate` 추가 조절
- 첫 오염원 등장 후 `UnlockMapSegmentMovement()` — 1차 우측 한계(`-403`) 해제, 맵 끝(`769`)까지 이동 (`PollutantManager.TryUnlockSegmentMovement`)
- **중화 VFX:** 접촉 중이고 **추천 아이템과 일치**할 때만 (틀린 도구면 OFF)
- 가스 밸브 연출: `SetValveAnimActive` — `Player_Valve` + 밸브 SFX
- **클리어:** `PlayClearAnim()` — `Clear` 트리거 → `Player_Clear`(Loop)
- **사망:** Die 애니 + `AudioManager.PlayDieSplattSfx()` → 페이드 (`dieAnimDelay` 후 패널은 `GameManager`에서 처리)
- 맵 전환: `PrepareMapAdvanceWalk()` → `mapAdvanceRightX`(기본 769)

### 오염원 (`Pollutant.cs`)

**A~C / D 공통:** `OnTriggerStay2D` → `ApplyPlayerContactDamage` — 판정 로그 → 방호복 DPS → 정답일 때만 오염원 HP.  
**고속 이동 보정:** `LateUpdate` bounds 겹침 시 Stay 누락 프레임 데미지 보정 · `OnTriggerExit` 패딩(`GetContactOverlapPadding`).  
**D 연출만 분기:** 가스밸브 정답 시 밸브 애니·SFX (중화 VFX 없음).

접촉 해제(전 타입): 오염원 HP `pollutanMaxHp`로 리셋.  
오염원 제거 시 아이템 선택 **유지** (스캐너/중화제로 자동 초기화 안 함).

**팝업·경고 문구 (코드):**

| 용도 | 위치 |
|------|------|
| 대응 아이템 팝업 `PopupUI` | `Pollutant.PopupText` — `Substances[][]` + `RecommendedItems[]` → `"{물질}에 {도구}을 사용하세요"` |
| 경고 `[경고] … 발견` | `PollutantManager` + `Pollutant.TypeLabels` |
| Z/X·패널티·맵 이동 | `PollutantManager` / `GameManager.wrongItemPenaltyMessage` / `GuideTxt.defaultMessage` |

### 오염원 스폰·구간 (`PollutantManager.cs`, `PollutantSpawnPlan.cs`)

| 단계 | 동작 |
|------|------|
| **로딩** | `PollutantSpawnPlan.Prepare` — 스테이지별 A~C/D 개수·위치·**타입 순서 확정** (한 판 고정) |
| **GameScene Start** | 비활성으로 미리 생성 (`SetActive(false)`) |
| **등장** | 경고 코루틴 후 해당 슬롯 `SetActive(true)` (Instantiate 최소화) |
| **구간 이탈** | `CanLeaveCurrentSegment()` — 활성 없음 + 미등장 큐 없음 + 남은 프리로드 없음 |

- `PollutantSpawner`: 인덱스 0~2 = A~C, 3~6 = D
- `mapPollutants`: 스테이지를 맵(화면) 단위로 나눔 — 맵당 오염원 수, 맵 클리어 후 `AdvanceMap()`
- `clearPanelDelay`: 마지막 오염원 페이드 후 클리어 연출까지 대기
- **타입 추첨:** ABC 슬롯마다 `stage_data`의 `pollutantTypes`에서 **독립 랜덤** → Stage 1-2(`A|B`)에서 `B,B,B` 등 **연속 동일 타입 가능** (로딩 시 한 번 확정)

### 아이템 선택 (`ItemSelectManager.cs`)

- 이동 중: `Scanner` 고정, Z/X 불가
- **첫** 경고 후: `OnWarningShown()` → 중화제 기본, Z/X 순환 (Scanner 제외)
- **이후** 경고: 이미 고른 중화 도구 **유지**
- 스테이지 재시작만 `ResetToDefault()` → Scanner

### 타이머·카운트다운 SFX (`Timer.cs`)

- `StageManager.LoadStage` → `SetStartTime()` 후 **`StartCountdown()`** (가이드 종료 전에는 정지)
- **경고 SFX:** HUD 표시 **≤ 16초**이고 **≥ 1초**일 때 `countdownSFX.ogg` **1회** 재생 (루프 아님)
- **빨간 숫자:** 같은 임계값(16초 이하, **0 포함**)
- **오염 경고 중:** `PollutantManager` → `PauseCountdown()` / 경고 종료 → `ResumeCountdown()` (SFX 재시작 없음)
- 게임오버·패널티·회복(임계값 초과) 시 `StopCountdown()` + SFX 정지
- 상세: [0614 합의](Assets/Docs/회의록/2026-06-14-시각미상-0614-일일-오후2100-2차-합의.md) · [0614 Bug §1~§2](Assets/Docs/Bug/2026-06-14-시각미상-0614-일일-오후2100-2차-fixes.md)

### 결과 연출·오대응 패널티 (`GameManager.cs`)

| 설정 | 기본 | 동작 |
|------|------|------|
| `clearAnimDelay` | 2초 | 클리어 애니 후 ClearSet·SFX |
| `dieAnimDelay` | 2초 | Die 애니 후 GameOverSet·SFX |
| `wrongItemPenaltySeconds` | 2초 | 틀린 중화 도구 접촉 — **전 스테이지** 아이템 dim + `GuideTxt` + Z/X·타이머 정지 (방호복 감소는 유지) |

### HUD 가이드 (`HelpGuideToggle.cs`)

- `HUD_Canvas`에 부착
- `KeyGuidePannel` / `ItemGuide` — 기본 비활성, **K** / **I** 토글
- Hierarchy: **부모만** 끄고 자식 UI는 켜 둠

### 스테이지·결과 (`StageManager.cs`, `StageScoreTracker.cs`, `ClearPanelUI.cs`)

- `stage_data.json`: `pollutantTypes`, `totalPollutant`, `timeLimit`, `bgIndex`, `bgmIndex`
- 별 조건: 오염원 전부 / 방호복 ≥50% / 틀린 아이템 0회
- UI 별: `starCount`만큼 **좌→우** 채움

### 스플래시 (`SplashController.cs`)

- App 또는 Splash 진입 — 로고 **페이드인(0.65s, 흰 배경)** · 확대 → 샤인 → 페이드아웃(1.5s)
- 타이틀 `LoadSceneAsync` **선로드** — 로고 alpha ≤ 0.02 시 페이드아웃 조기 종료
- **타이틀 전환:** 검정 `FadeOverlay` **`fadeToBlackDuration`**(기본 0.35s) 후 `TitleScene` 활성화
- SFX: `AudioManager.PlaySplashSfx()` — 페이드인 시작 후 `splashSfxDelay`(0.22s) 재생

### 타이틀·인트로 페이드 (`TitleSceneFade.cs`, `AutoScrollIntro.cs`)

- **타이틀 → 인트로:** 검정 `FadeOverlay` **0.25s** + 인트로 비동기 로드 (`SceneLoadUI.StartButton`)
- **인트로 진입:** `fadeInFromTitle` 시 **검정** 화면 **0.8s** SmoothStep 페이드인 후 스크롤
- **인트로 → 로딩:** 검정 암전 **0.5s** (기존)

### 타이틀 버전 표시 (`VersionText.cs`)

- `TitleScene` · `TItle_HUD_Canvas/VersionTxt` **우하단**
- `Application.version` → TMP `"v" + Application.version` (Player Settings **Version**, 현재 `1.0`)
- 게임·로딩·일시정지에는 미표시 (1차)

### 오디오 (`AudioManager.cs`, DontDestroyOnLoad)

| 슬라이더 / 설정 | 용도 |
|----------------|------|
| `bgmVolume` / `bgmFadeDuration` | 타이틀·게임 BGM + 페이드 인/아웃 |
| `neutralizationSfxVolume` | A~C 중화 루프 |
| `valveSfxVolume` | 가스 밸브 루프 |
| `sfxVolume` | 버튼·클리어·게임오버·스플래시·**사망 splatt** |
| `stageBgmClips` | 스테이지별 탐사 BGM (`StageManager` / `bgmIndex`) |
| `splashClip` | 스플래시 로고 SFX |
| `dieSplattClip` | Die 애니 시작 시 1회 (`PlayDieSplattSfx`) — 게임오버 패널 SFX와 별도 |

**씬별 BGM:** `TitleScene`만 자동 재생. `GameScene`은 **`StageManager.LoadStage`가 `bgmIndex`로 재생** (`OnSceneLoaded`에서 0번 강제 금지).  
**싱글톤 클립:** 중복 `AudioManager` 파괴 시 `stageBgmClips` 병합 (`CopyStageBgmClipsIfNeeded`).  
**App · 단독 Play:** `AppScene` 또는 각 씬에서 Instance 생성 — 둘 다 동작하도록 위 규칙 유지.

### 회복 아이템 (`RecoveryItemManager`, `RecoveryItemInventory`)

- 정의: `Assets/Data/recovery_items.json` (id, type, displayName, effect, value)
- 런타임: `List<RecoveryInvSlot> { id, count }` — **입수 순**, 동일 id **count++**
- **드랍:** 오염원 정화 시 `RecoveryItemManager.TryDropOnPollutantCleared` — 타입별 확률 → `mapProtectRecovPrefab` / `mapTimeRecovPrefab` Instantiate
- **아크 연출:** `RecoveryItem.StartDropArc` — `dropYOffset`(Y 보정) + 포물선(`dropJumpHeight`) + 좌/우 착지(`dropLandOffsetX`, `dropDuration`). 아크 중 픽업 OFF, 착지 후 ON
- `RecoveryItem` 부착: **맵 픽업 프리팹만** (`map*RecovPrefab`). 인벤 `inv*RecovPrefab`에는 없음
- 인벤 UI: `invEmptySlotPrefab`(빈 칸 4+) + 획득 시 `InvItemView`(`invProtectRecovPrefab` / `invTimeRecovPrefab`) Instantiate
- 조작: `C`/`V` 선택, `Space` 사용 (count--, 0이면 칸 제거)
- 선택 표시: `dim` OFF = 선택됨 (`RecoveryItemInventoryUI`)
- 5번째 종류~: `EnsureSlotRootCount`로 Content에 칸 추가 (Scroll View, 6종+ 실검증 보류)
- 설계: [인벤 2026-06-06](Assets/Docs/회의록/2026-06-06-시각미상-회복아이템-인벤-설계-합의.md) · [드랍 연출 2026-06-08](Assets/Docs/회의록/2026-06-08-시각미상-회복아이템-드랍-연출-합의.md) · Bug: [인벤 UI](Assets/Docs/Bug/2026-06-06-시각미상-회복인벤-UI-fixes.md) · [아크 드랍](Assets/Docs/Bug/2026-06-08-시각미상-회복아이템-아크드랍-fixes.md)

### 가이드·경고·대응 팝업 (`GuideTxt`, `WarningTxt`, `PopupUI`)

| UI | 역할 | 우선순위 |
|----|------|----------|
| **WarningTxt** | `[경고] {타입} 오염물질 발견` — 고정 표시 후 깜빡임 | **1** |
| **GuideTxt** | 시작·Z/X(1-1)·맵 이동·오대응 패널티 — `bg0`/`bg1` | **2** |
| **PopupUI** | 오염원 등장 후 추천 도구 안내 (`Pollutant.PopupText`) | **3** (①② 종료 후) |

| 항목 | 규칙 |
|------|------|
| **GuideTxt** | `ApplyPopup` — `Hidden` / `Short`(`bg0`) / `Long`(`bg1`). 16자 초과 → `bg1` |
| **GuideTxt 자식 `bg (1~3)`** | 루트 Image와 중복 — **항상 OFF** |
| **WarningTxt** | 직계 자식 **전체** ON/OFF (`Bg`~`Bg (5)` + `WarningMsg`). 인스펙터: `PollutantManager` → **경고 연출** (`warningHoldDuration` 기본 **1.2s**, `warningBlinkCount`, `warningBlinkInterval`) |
| **PopupUI** | `WaitForPriorityHudMessages` — 가이드·경고·패널티 중이면 대기 |

상세: [0610 합의](Assets/Docs/회의록/2026-06-10-오후1430-0610-일일-오후1830-1차-합의.md) · [0611 Bug 3차](Assets/Docs/Bug/2026-06-11-오후1430-0611-일일-오후2100-2차-fixes.md) §10~§14

### 오염원 등장 화살표 (`PollutantArrowUI`)

- **A~C** 페이드인 완료 후 · **D·Stage 1-1** 제외
- `WorldSpaceUIFollower` + 코드 바운스 **3회** 후 숨김
- `PollutantManager.pollutantArrowPrefab` → `HUD_Canvas` 하위 생성
- 설정: [pollutant-arrow-유니티-설정가이드.md](Assets/Docs/2026-06-11-시각미상-pollutant-arrow-유니티-설정가이드.md)

### 방호복 HUD 아웃라인

- `protectionHp` → `Bg` (`5_7`) — `MidProject/UI/ImageOutline` + `ProtectionHpBar5_7Outline.mat`
- 오염원 접촉 HP바(`pollutantHpSlider`)와 **별개**

### 저장·이어하기 (`GameSaveManager.cs`)

- **빌드:** `[exe 폴더]/Save/gamesave.json` — exe 옆 `Save/` (Documents 미사용)
- **에디터 Play:** `%USERPROFILE%/Documents/HazmatRush_Save/gamesave.json`
- 타이틀 **시작** 시 `SceneLoadManager.StartButton()` → `EnsureSaveDirectory()` (빌드 Save 폴더 생성)
- **이어하기** 버튼: 저장 파일 있을 때만 표시 (`SceneLoadUI`)
- 상세: [0614 Bug §4](Assets/Docs/Bug/2026-06-14-시각미상-0614-일일-오후2100-2차-fixes.md)

### 기타

| 모듈 | 역할 |
|------|------|
| `AppBootstrap` | App 진입 · `runInBackground` · 첫 씬(`SplashScene`) 로드 |
| `GameManager` | 클리어·게임오버·일시정지·`QuitGame` · 디버그 키 · 사망 시 `HidePollutantHpBar` |
| `GameSaveManager` | 저장/로드 · 빌드 exe-local `Save/` |
| `SceneLoadManager` | 씬 전환, `pendingStageIndex` · 시작 시 Save 폴더 보장 |
| `SceneLoadUI` | 타이틀 시작/이어하기/타이틀 복귀 · `QuitApplication` |
| `CameraAspectFit` | 게임 배경 **cover** (풀창 좌우 검정 방지) · ortho size 고정 · HUD는 `CanvasBackgroundCover` 별도 |
| `CanvasBackgroundCover` | HUD 캔버스 **cover** (1280×720 등 비 16:9 리사이즈 보정) · 카메라 배경색 강제 변경 **없음** (스플래시 흰 배경 유지) |
| `LoadingController` / `LoadingGuideTxt` | 페이드·플랜 준비·조작 안내 |
| `Background` | 스크롤, 오염원 활성/구간 미완료 시 정지 |
| `WorldSpaceUIFollower` | 오염원 HP 바 (캔버스 직속, 접촉 시만 표시) |
| `Timer` | 제한 시간 HUD · 카운트다운 SFX · 경고 중 pause/resume |

---

## 스테이지 예시 (`stage_data.json`)

| 스테이지 | 오염원 타입 | total | 제한 시간 |
|----------|------------|-------|-----------|
| 1-1 | D | 2 | 120초 |
| 1-2 | A \| B | 3 | 100초 |
| 1-3 | A \| B \| C \| D | 4 | 80초 |

---

## 프로젝트 구조

```text
Assets/
├── Data/stage_data.json
├── Data/recovery_items.json
├── Scenes/
│   ├── AppScene.unity          ← Build 0 · 매니저 본체
│   ├── SplashScene.unity
│   ├── TitleScene.unity
│   ├── IntroStoryScene.unity
│   ├── LoadingScene.unity
│   ├── 5_TutorialScene.unity   ← 튜토리얼 (타이틀 TutorialButton)
│   └── GameScene.unity
├── Scripts/
│   ├── Core/          AppBootstrap, GameManager, GameSaveManager, SceneLoadManager, AudioManager, …
│   ├── GamePlay/      Player, Pollutant, PollutantManager, Item, …
│   └── UI/            GuideTxt, WarningTxt, Timer, VersionText, SplashController, CanvasBackgroundCover, …
├── Shaders/           SpriteOutline, UIImageOutline
├── Materials/         Pollutant*Outline, ProtectionHpBar5_7Outline
├── Prefabs/Game/      Player, PollutantA~D
├── Prefabs/Item/      Scanner, Neutralizer, pads, GasValve, map*Recov
├── Prefabs/InvenItem/ inv*Recov, invEmptySlotPrefab
├── Audio/SFX/
├── Docs/Bug/          버그·수정 기록
└── Animations/        Player Idle / Move / Die / Valve / Clear
```

### Hierarchy

**AppScene:** `App` (`AppBootstrap`) · `SceneLoadManager` · `AudioManager`

**GameScene (요약):**

| 이름 | 역할 |
|------|------|
| Player | 이동·방호복·애니·중화 VFX |
| PollutantManager | 경고·등장·맵 전환 |
| RecoveryItemManager | 오염원 정화 후 회복 아이템 드랍·아크 |
| HUD_Canvas | `HelpGuideToggle`, `KeyGuidePannel`, `ItemGuide`, **`PollutantArrow`** (A~C 등장 화살표) |
| ItemSelectManager | Z/X 아이템 |
| PollutantManager | 오염원 등장·`pollutantArrow` 참조 |

> Title/Game/Splash 등에도 `SceneLoadManager`·`AudioManager` 중복 배치(개발용). App Play 시 App 인스턴스가 우선.

### Inspector 체크

| 오브젝트 | 확인 |
|----------|------|
| **GameManager** | `clearAnimDelay`, `dieAnimDelay`, `clearSet`, `gameOverSet` |
| **HUD_Canvas** | `HelpGuideToggle` — 패널 참조 |
| **Player** Animator | `Clear` 트리거 → `Player_Clear` (Loop) |
| **AudioManager** | `splashClip`, `dieSplattClip`, `squeakyValveClip`, 볼륨·페이드 설정 |
| **Timer** (HUD) | `countdownSFX`, `warningSfxStartSeconds`(16), `warningTextColor`(빨강) |
| **HUD_Canvas** | `CanvasBackgroundCover` (1280 빌드 UI 보정) |
| **ItemSelectManager** | `itemPrefabs` 5종 |
| **SplashController** | `fadeToBlackDuration`, `logoCanvasGroup` |
| **TitleSceneFade** | `fadeOutDuration` (타이틀→인트로 검정) |
| **TitleScene / VersionTxt** | 빌드 버전 표시 |
| **RecoveryItemManager** | `protectionItemPrefab` / `timeItemPrefab`, `dropYOffset`·`dropJumpHeight`·`dropLandOffsetX`, `testAlwaysDrop` 빌드 전 OFF |
| **PollutantManager** | **경고 연출** · `itemSelectHintDuration` · `popupShowDuration` · `pollutantArrowPrefab` |
| **PollutantArrow** (프리팹) | `worldOffsetY` · `bounceCount`(3) · `dropDownDuration` / `riseDuration` → [설정 가이드](Assets/Docs/2026-06-11-시각미상-pollutant-arrow-유니티-설정가이드.md) |
| **GameManager** | `wrongItemPenaltyMessage` · `guideTxt` · `itemManager` 연결 |

> **오염원 화살표 (Unity 초보용):** [pollutant-arrow-유니티-설정가이드.md](Assets/Docs/2026-06-11-시각미상-pollutant-arrow-유니티-설정가이드.md) — Hierarchy 위치, **어디에 뭘 붙이는지**, Play 확인법.

---

## 실행 방법

1. Unity `6000.4.8f1`로 프로젝트 열기
2. **권장(빌드·전체 플로우):** `AppScene` 실행 후 Play
3. **개발용:** `GameScene` 등 씬 직접 Play 가능 (해당 씬 매니저 사용 · `runInBackground` 미적용)
4. 정상 플로우: App → 스플래시 → 타이틀 → (시작) 인트로 → 로딩 → 게임

### 플레이 테스트 체크리스트

**App · 전체 플로우**

- [ ] `AppScene` Play → 스플래시 페이드인(0.65s, **흰 배경**)·SFX → 검정(0.35s) → 타이틀 BGM · 우하단 `v1.0`
- [ ] 이어하기 — 저장 없으면 버튼 숨김 · 빌드는 exe 옆 `Save/gamesave.json`
- [ ] 이어하기 시 Intro 생략 → 로딩 → 게임
- [ ] Alt+Tab 후에도 앱 동작 (`runInBackground`)

**게임플레이** (`App` 또는 `GameScene` 직접 Play)

- [ ] Start → 검정 암전 → 인트로 검정 페이드인 (흰 전환 없음)
- [ ] 로딩 씬 K/I 안내 문구 표시
- [ ] 가이드 후 좌우 이동, 경고 → 오염원 등장 → 첫 오염원 후 -403 해제·맵 안 이동
- [ ] 고속 이동 중 접촉 HP·방호복 **끊김 없음**
- [ ] **Z 왼쪽 / X 오른쪽** 아이템 전환
- [ ] **K / I** 가이드 패널 토글 (기본 숨김)
- [ ] 정답: 오염원 HP + 중화 VFX / 오답: 방호복만 + VFX 없음
- [ ] 오염원 1마리 제거 후 선택 아이템 유지
- [ ] 가스: 밸브 애니 + 밸브 SFX
- [ ] 가이드: 짧은 문구 `bg0` / Z·X 안내 `bg1` (겹침 없음)
- [ ] 스테이지 전환 시 BGM `bgmIndex` 변경
- [ ] 클리어: Clear 애니 2초 → 패널
- [ ] **타이머:** 16초 이하 빨간 숫자 + countdown SFX 1회 · 오염 경고 중 pause/resume
- [ ] 사망: Die 애니 + **dieSplattSFX** → 2초 → 게임오버 패널 + game-overSFX · 오염원 HP바 숨김
- [x] 가스 사망 시 밸브↔Die 애니 루프 없음 — [0610 Bug §5](Assets/Docs/Bug/2026-06-10-오후1430-0610-일일-오후2030-2차-fixes.md)
- [x] 사망 Die → 페이드 → 1초 → 게임오버 패널 — [0610 Bug §8](Assets/Docs/Bug/2026-06-10-오후1430-0610-일일-오후2030-2차-fixes.md)
- [ ] 클리어 별 **왼쪽부터** N개
- [ ] 오염원 정화 후 회복 아이템 **아크 드랍** (위로 튀었다 좌/우 착지, 땅에 묻히지 않음)
- [ ] 회복 아이템 픽업 → 인벤 아이콘·이름·Count 표시
- [ ] 같은 회복 아이템 재획득 → Count만 증가
- [ ] `C`/`V` 회복 선택, `Space` 사용
- [ ] 타이틀·일시정지 **종료하기** → Play 종료 / exe 닫힘
- [ ] Windows 빌드 — **1280×720** 창 모드 · exe 아이콘 · `Hazmat Rush_Data` 동봉 · HUD 하단 UI 위치

---

## 구현 상태

### 완료

- **2026-06-14 일일** (타이머 countdown SFX · dieSplatt · 1280×720 UI · 빌드 Save 경로 · 스플래시 흰 배경) — [회의록](Assets/Docs/회의록/2026-06-14-시각미상-0614-일일-오후2100-2차-합의.md) · [Bug](Assets/Docs/Bug/2026-06-14-시각미상-0614-일일-오후2100-2차-fixes.md)
- **2026-06-12 일일** (빌드 직전 · 종료 버튼 · PC 창 모드·리사이즈 HUD · 외부 QA 1차 7건 취합) — [회의록](Assets/Docs/회의록/2026-06-12-오후1700-0612-일일-오후1700-1차-합의.md) · [Bug](Assets/Docs/Bug/2026-06-12-오후1700-0612-일일-오후1700-1차-fixes.md)
- **2026-06-11 일일** (타이틀 버전 · 페이드 검정 · 외부 QA Formal `qa-B.html`) — [회의록](Assets/Docs/회의록/2026-06-11-오후1430-0611-일일-오후2100-2차-합의.md) · [Bug](Assets/Docs/Bug/2026-06-11-오후1430-0611-일일-오후2100-2차-fixes.md)
- **2026-06-10 일일** (스테이지 BGM · 가이드/경고 팝업 · 방호복 UI 아웃라인 · 가스 사망 HP바 · 맵 회복 스폰 폐기) — [회의록](Assets/Docs/회의록/2026-06-10-오후1430-0610-일일-오후1830-1차-합의.md) · [Bug](Assets/Docs/Bug/2026-06-10-오후1430-0610-일일-오후2030-2차-fixes.md)
- **2026-06-09 일일** (경고·맵·이동·접촉·App·스플래시·방호복 HUD) — [회의록](Assets/Docs/회의록/2026-06-09-오후1227-0609-일일-오후1700-2차-합의.md)
- 씬 흐름·`stage_data.json`·로딩 프리스폰·맵 구간 (`mapPollutants`)
- 접촉 판정·정답만 오염원 HP·가스 HP 초기화·밸브 애니/SFX
- Z/X 방향, 아이템 선택 유지(맵 중), 틀린 아이템 시 VFX 미재생
- 스플래시·BGM 씬별 페이드
- **씬 전환** — 스플래시 선로드, **검정** 페이드 통일(로고만 흰), 타이틀 버전 표시
- **K/I** HUD 가이드, 로딩 안내
- 회복 아이템 JSON·맵/인벤 프리팹·스택 인벤·`InvItemView` UI·`dim` 선택 ([Bug](Assets/Docs/Bug/2026-06-06-시각미상-회복인벤-UI-fixes.md))
- 회복 아이템 **오염원 정화 드랍·아크 연출**·`dropYOffset`·아크 디버그 로그 ([Bug](Assets/Docs/Bug/2026-06-08-시각미상-회복아이템-아크드랍-fixes.md)) — **유일한 획득 경로** (맵 포인트 `RecoveryItemSpawner` 폐기)
- **Player_Clear** 클리어 애니 + 2초 후 패널
- **Player_Die** 2초 후 게임오버 패널
- 클리어 별 좌→우, 일시정지·재시작

### 미구현·보류

- **창 크기 16:9 고정 / 해상도 프리셋 UI** (1920·1600·1280) — 논의만, 미구현
- **`AppBootstrap.ForceWindowedOnPcBuild`** vs Player Settings 1280×720 — 정렬 필요 시 확인
- **5_TutorialScene** Play·타이틀 TutorialButton — 🟡 미검증
- **시계 HUD 중앙 배치** — 외부 QA 피드백 (Item Guide 겹침) · 목표 UI 아래로 이동 예정 ([0612 합의](Assets/Docs/회의록/2026-06-12-오후1700-0612-일일-오후1700-1차-합의.md))
- **커스텀 커서** — Player Settings Default Cursor만 사용 (추가 작업 없음)
- **가스(D) 사망 시 밸브↔Die 애니 루프** — HP바는 숨김 처리됨, 애니 반복은 미해결 ([0610 Bug §5](Assets/Docs/Bug/2026-06-10-오후1430-0610-일일-오후1830-1차-fixes.md))
- App **PR2** 중복 매니저 제거 · **PR3** Additive 로딩 · **서버** 연동 (계획만)
- 좌우 연타 가속 체감 (`Background.SmoothDamp`) — 분석만, 수정 보류
- 회복 인벤 6종+ Scroll View 스크롤 실검증, 중화 HUD와 패널 Hierarchy 최종 분리 확인
- 멀티 스테이지 자동 진행 UI polish
- `AGENTS.md` 일부 구버전 설명(ClampPassThrough 등) — README·Bug 문서 기준 우선

---

## 문서

| 파일 | 용도 |
|------|------|
| [README.md](README.md) | 프로젝트 개요 (이 파일) |
| [AGENTS.md](AGENTS.md) | AI·협업·코딩 규칙 |
| [문서 하네스](Assets/Docs/문서-이름-규칙.md) | Bug/회의록 작성·당일 취합·5필드 파일명 |
| [0614 일일 회의록](Assets/Docs/회의록/2026-06-14-시각미상-0614-일일-오후2100-2차-합의.md) | **최근** — 타이머 SFX · dieSplatt · 1280 UI · Save 경로 |
| [0614 일일 Bug](Assets/Docs/Bug/2026-06-14-시각미상-0614-일일-오후2100-2차-fixes.md) | Timer · Audio · CanvasBackgroundCover · GameSaveManager |
| [0612 일일 회의록](Assets/Docs/회의록/2026-06-12-오후1700-0612-일일-오후1700-1차-합의.md) | 빌드 직전 · 종료 · 창 모드 · QA 1차 · 시계 UI |
| [0612 일일 Bug](Assets/Docs/Bug/2026-06-12-오후1700-0612-일일-오후1700-1차-fixes.md) | Popup · 재시작 인벤 · 창 모드 · 리사이즈 · QuitGame |
| [외부 QA 피드백 (7건)](Assets/Docs/QA_Feedback/HazmatRush/) | PC v1.0 Formal 응답 원본 |
| [0611 일일 회의록](Assets/Docs/회의록/2026-06-11-오후1430-0611-일일-오후2100-2차-합의.md) | 버전 · 페이드 · 외부 QA Formal · PC 빌드 배포 |
| [0611 일일 Bug](Assets/Docs/Bug/2026-06-11-오후1430-0611-일일-오후2100-2차-fixes.md) | VersionText · 페이드 · QA `_Data` 문구 · `testAlwaysDrop` |
| [0610 일일 회의록](Assets/Docs/회의록/2026-06-10-오후1430-0610-일일-오후1830-1차-합의.md) | BGM·팝업·아웃라인·가스 사망 |
| [0610 일일 Bug](Assets/Docs/Bug/2026-06-10-오후1430-0610-일일-오후2030-2차-fixes.md) | 위 합의 대응 fixes · **§5 밸브·§8 사망 패널** |
| [0609 일일 회의록](Assets/Docs/회의록/2026-06-09-오후1227-0609-일일-오후1700-2차-합의.md) | 방호복 HUD · 오염원 HP바 · 접촉 |
| [회복 아이템 아크 드랍 fixes](Assets/Docs/Bug/2026-06-08-시각미상-회복아이템-아크드랍-fixes.md) | 아크 수치·Y 보정·디버그 로그 |
| [회복 드랍 연출 합의](Assets/Docs/회의록/2026-06-08-시각미상-회복아이템-드랍-연출-합의.md) | 오염원 정화 드랍·아크·Inspector 파라미터 |
| [회복 인벤 UI fixes](Assets/Docs/Bug/2026-06-06-시각미상-회복인벤-UI-fixes.md) | 획득 표시·레이아웃·비율 |
| [회복 인벤 설계](Assets/Docs/회의록/2026-06-06-시각미상-회복아이템-인벤-설계-합의.md) | 스택·스크롤·JSON·프리팹·구현 갱신 |
| [씬 전환 fixes](Assets/Docs/Bug/2026-06-06-시각미상-씬전환-스플래시-인트로-fixes.md) | 스플래시·타이틀·인트로 페이드 |
| [클리어·가이드·VFX fixes](Assets/Docs/Bug/2026-06-05-시각미상-클리어-가이드-중화VFX-fixes.md) | 클리어/사망 연출, K/I 가이드, VFX |
| [스플래시·오디오 fixes](Assets/Docs/Bug/2026-06-05-시각미상-스플래시-오디오-fixes.md) | 스플래시·BGM |
| [맵구간 fixes](Assets/Docs/Bug/2026-06-05-오후1825-맵구간-게임플레이-fixes.md) | 맵·스폰 |
| [gameplay fixes](Assets/Docs/Bug/2026-06-04-시각미상-gameplay-fixes.md) | 접촉·이동·오디오 |
| [회의록 §4](Assets/Docs/회의록/2026-06-05-시각미상-플레이어-오염원-로직-검토.md) | Pollutant/Player 접촉 규칙 |
| [Git workflow (worktree)](Assets/Docs/2026-06-08-시각미상-git-workflow-worktree.md) | 브랜치·worktree·PR 절차 |

---

## 변경 이력 (요약)

| 날짜 | 주요 내용 |
|------|-----------|
| 2026-06-14 | 타이머 countdown SFX(16s) · dieSplatt · 1280×720 HUD · 빌드 Save 경로 · 스플래시 흰 배경 — [회의록](Assets/Docs/회의록/2026-06-14-시각미상-0614-일일-오후2100-2차-합의.md) · [Bug](Assets/Docs/Bug/2026-06-14-시각미상-0614-일일-오후2100-2차-fixes.md) |
| 2026-06-12 | 종료 버튼 · PC 창 모드·리사이즈 HUD · 외부 QA 1차 7건 · 시계 UI 중앙(예정) — [회의록](Assets/Docs/회의록/2026-06-12-오후1700-0612-일일-오후1700-1차-합의.md) · [Bug](Assets/Docs/Bug/2026-06-12-오후1700-0612-일일-오후1700-1차-fixes.md) |
| 2026-06-11 | 타이틀 버전 · 페이드 검정 · 외부 QA `qa-B.html` · PC 빌드 배포 규칙 — [회의록](Assets/Docs/회의록/2026-06-11-오후1430-0611-일일-오후2100-2차-합의.md) · [Bug](Assets/Docs/Bug/2026-06-11-오후1430-0611-일일-오후2100-2차-fixes.md) |
| 2026-06-10 | 스테이지 BGM·가이드/경고 팝업·방호복 UI 아웃라인·가스 사망 HP바 — [회의록](Assets/Docs/회의록/2026-06-10-오후1430-0610-일일-오후1830-1차-합의.md) · [Bug](Assets/Docs/Bug/2026-06-10-오후1430-0610-일일-오후2030-2차-fixes.md) (밸브·사망 패널 2차) |
| 2026-06-09 | 경고·맵·이동·접촉·App·스플래시·방호복 HUD — [회의록](Assets/Docs/회의록/2026-06-09-오후1227-0609-일일-오후1700-2차-합의.md) |
| 2026-06-08 | 회복 아이템 아크 드랍 수치·`dropYOffset`·디버그 로그 — [Bug](Assets/Docs/Bug/2026-06-08-시각미상-회복아이템-아크드랍-fixes.md) · [회의록](Assets/Docs/회의록/2026-06-08-시각미상-회복아이템-드랍-연출-합의.md) |
| 2026-06-06 | 회복 인벤 1차 구현(스택·InvItemView·UI 레이아웃), 씬 전환 페이드 — [인벤 UI Bug](Assets/Docs/Bug/2026-06-06-시각미상-회복인벤-UI-fixes.md) · [씬 Bug](Assets/Docs/Bug/2026-06-06-시각미상-씬전환-스플래시-인트로-fixes.md) · [회의록](Assets/Docs/회의록/2026-06-06-시각미상-회복아이템-인벤-설계-합의.md) |
| 2026-06-05 | 클리어/사망 2초 연출, K/I 가이드, 아이템 유지, 틀린 아이템 VFX 수정 — [Bug](Assets/Docs/Bug/2026-06-05-시각미상-클리어-가이드-중화VFX-fixes.md) |
| 2026-06-05 | 스플래시·BGM, 맵 구간 — [오디오](Assets/Docs/Bug/2026-06-05-시각미상-스플래시-오디오-fixes.md) · [맵](Assets/Docs/Bug/2026-06-05-오후1825-맵구간-게임플레이-fixes.md) |
| 2026-06-04 | 로딩 프리스폰, 이동/가스/ZX/별점 — [Bug](Assets/Docs/Bug/2026-06-04-시각미상-gameplay-fixes.md) |

---

## 개발 메모

- HP는 **float** 계산, UI는 **int** (`FloorToInt`)
- 타입: `Item.ItemType`, `Pollutant.PollutantType`만 사용
- `AudioManager`·`SceneLoadManager` — `DontDestroyOnLoad` (`AppScene` 또는 각 씬에서 최초 생성)
- **Play 권장:** 빌드·QA는 `AppScene` · gameplay만 보면 `GameScene` 직접 Play 가능
- **빌드 저장 테스트:** exe 폴더의 `Save/gamesave.json` 삭제 후 이어하기 버튼 숨김 확인 (Documents 아님)
- 가이드 패널: 부모만 비활성, 자식은 활성 유지
- `Library/`, `Temp/`, `Logs/` — Git·수정 대상 아님

---

## 폰트·미디어 저작권

- **Galmuri Font** — © 2019–2023 Minseo Lee
- **시작 버튼 SFX** — [BoostSound](https://www.youtube.com/watch?v=YNSbL-Cek1c)
- **Title / Game BGM** — SUNO 생성
- **그 외 SFX** - freesoung.org
