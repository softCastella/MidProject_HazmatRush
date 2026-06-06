# HazmatRush_해즈멧러쉬

2D 횡스크롤 환경에서 오염물질을 탐지·중화하는 Unity 게임입니다.  
방호복을 유지하며 이동하고, 오염원 유형에 맞는 아이템으로 오염원을 제거하는 것이 핵심 플레이입니다.

| 항목 | 내용 |
|------|------|
| **엔진** | Unity `6000.4.8f1` |
| **언어** | C# |
| **씬** | `SplashScene` → `TitleScene` → `IntroStoryScene` → `LoadingScene` → `GameScene` |
| **스테이지 데이터** | `Assets/Data/stage_data.json` (`mapPollutants` 포함) |
| **회복 아이템 정의** | `Assets/Data/recovery_items.json` |
| **AI·협업 규칙** | [AGENTS.md](AGENTS.md) |
| **버그·수정 기록** | [Bug 폴더](Assets/Docs/Bug/) — 최근: [씬 전환](Assets/Docs/Bug/2026-06-06-시각미상-씬전환-스플래시-인트로-fixes.md) |

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
| `F1` | (디버그) 강제 클리어 |
| `F2` | (디버그) 강제 게임 오버 |

가이드 텍스트가 끝나면 이동·타이머·배경 스크롤이 활성화됩니다.  
로딩 씬에서도 이동·Z/X·C/V·**K/I**·ESC 안내가 순환 표시됩니다.

---

## 씬 흐름

```text
SplashScene
  └ 로고 페이드인·확대 → splashSFX → 로고 페이드아웃(1.5s) → TitleScene (타이틀 선로드, 별도 검은 암전 없음)
TitleScene
  └ 시작 → 흰 화면 암전(0.25s) + 인트로 선로드 → IntroStoryScene
  └ 이어하기 → LoadingScene (인트로 생략)
IntroStoryScene
  └ (타이틀에서 온 경우) 흰 화면 SmoothStep 페이드인(0.8s) → 스크롤
  └ 종료 → 검은 암전(0.5s) → LoadingScene
LoadingScene
  └ stage_data + 오염원·회복 아이템 배치 확정 (PollutantSpawnPlan, RecoveryItemSpawnPlan)
  └ 조작 안내 문구 순환 (LoadingGuideTxt)
  └ GameScene 로드
GameScene
  └ 스테이지 플레이 → 클리어 / 게임오버
```

> **BGM:** 타이틀·게임 씬만 BGM 재생(페이드 인/아웃). 스플래시·인트로·로딩에서는 BGM 없음(`StopBGM`).

> **씬 페이드:** `TitleSceneFade`(타이틀→인트로 흰 암전), `AutoScrollIntro`(인트로 진입 페이드인·종료 검은 암전). 상세: [Bug 2026-06-06](Assets/Docs/Bug/2026-06-06-시각미상-씬전환-스플래시-인트로-fixes.md).

---

## 플레이 흐름 (한 구간)

```text
시작 가이드 종료 → 이동·타이머·배경 ON
  └ 1차 범위(-785~-403)에서 우측 이동 (누적 시간)
        └ 경고 (WarningTxt) + Z/X 안내 (GuideTxt)
        └ 오염원 등장 (로딩 때 만든 인스턴스 활성화 + 페이드인)
              └ 팝업: 물질·추천 아이템 안내
        └ 접촉: 판정 로그 → 방호복 감소 → 정답 아이템만 오염원 HP 감소
              └ 정답 A~C: 중화 VFX + 중화 SFX 루프
              └ D+가스밸브: 밸브 애니 + 밸브 SFX (중화 VFX 없음)
              └ 오답: 방호복만 감소, 중화 VFX·오염원 HP 감소 없음
  └ 해당 구간 예정 오염원(중화+가스) 전부 처리 전
        → 맵 전환 불가, 배경 스크롤 정지
  └ 전부 처리 후
        ├ 스테이지에 남은 오염원 있음 → x=769까지 이동 → 페이드 → 시작 위치·배경 리셋 → 반복
        └ 마지막 오염원 → Player_Clear 2초 → ClearSet (별점 + 조건별 텍스트)
```

사망 시: `Player_Die` **2초** → GameOverSet + 게임오버 SFX.

---

## 핵심 시스템

### 플레이어 (`Player.cs`)

- 방호복 `curProtection` / `maxProtection` (float), UI는 정수 %
- 이동: `Rigidbody2D.MovePosition`, 1차 범위 `leftLimit`~`rightLimit`
- 첫 오염원 등장 후 `UnlockMapSegmentMovement()` — 1차 우측 한계(`-403`) 해제, 맵 끝(`769`)까지 이동
- **중화 VFX:** 접촉 중이고 **추천 아이템과 일치**할 때만 (틀린 도구면 OFF)
- 가스 밸브 연출: `SetValveAnimActive` — `Player_Valve` + 밸브 SFX
- **클리어:** `PlayClearAnim()` — `Clear` 트리거 → `Player_Clear`(Loop)
- **사망:** Die 애니 → 페이드 (`dieAnimDelay` 후 패널은 `GameManager`에서 처리)
- 맵 전환: `PrepareMapAdvanceWalk()` → `mapAdvanceRightX`(기본 769)

### 오염원 (`Pollutant.cs`)

**A~C / D 공통:** `OnTriggerStay2D` → `ApplyPlayerContactDamage` — 판정 로그 → 방호복 DPS → 정답일 때만 오염원 HP.  
**D 연출만 분기:** 가스밸브 정답 시 밸브 애니·SFX (중화 VFX 없음).

접촉 해제(전 타입): 오염원 HP `pollutanMaxHp`로 리셋.  
오염원 제거 시 아이템 선택 **유지** (스캐너/중화제로 자동 초기화 안 함).

### 오염원 스폰·구간 (`PollutantManager.cs`, `PollutantSpawnPlan.cs`)

| 단계 | 동작 |
|------|------|
| **로딩** | `PollutantSpawnPlan.Prepare` — 스테이지별 A~C/D 개수·위치·타입 확정 |
| **GameScene Start** | 비활성으로 미리 생성 (`SetActive(false)`) |
| **등장** | 경고 코루틴 후 해당 슬롯 `SetActive(true)` (Instantiate 최소화) |
| **구간 이탈** | `CanLeaveCurrentSegment()` — 활성 없음 + 미등장 큐 없음 + 남은 프리로드 없음 |

- `PollutantSpawner`: 인덱스 0~2 = A~C, 3~6 = D
- `mapPollutants`: 스테이지를 맵(화면) 단위로 나눔 — 맵당 오염원 수, 맵 클리어 후 `AdvanceMap()`
- `clearPanelDelay`: 마지막 오염원 페이드 후 클리어 연출까지 대기

### 아이템 선택 (`ItemSelectManager.cs`)

- 이동 중: `Scanner` 고정, Z/X 불가
- **첫** 경고 후: `OnWarningShown()` → 중화제 기본, Z/X 순환 (Scanner 제외)
- **이후** 경고: 이미 고른 중화 도구 **유지**
- 스테이지 재시작만 `ResetToDefault()` → Scanner

### 결과 연출 (`GameManager.cs`)

| 설정 | 기본 | 동작 |
|------|------|------|
| `clearAnimDelay` | 2초 | 클리어 애니 후 ClearSet·SFX |
| `dieAnimDelay` | 2초 | Die 애니 후 GameOverSet·SFX |

### HUD 가이드 (`HelpGuideToggle.cs`)

- `HUD_Canvas`에 부착
- `KeyGuidePannel` / `ItemGuide` — 기본 비활성, **K** / **I** 토글
- Hierarchy: **부모만** 끄고 자식 UI는 켜 둠

### 스테이지·결과 (`StageManager.cs`, `StageScoreTracker.cs`, `ClearPanelUI.cs`)

- `stage_data.json`: `pollutantTypes`, `totalPollutant`, `timeLimit`, `bgIndex`, `bgmIndex`
- 별 조건: 오염원 전부 / 방호복 ≥50% / 틀린 아이템 0회
- UI 별: `starCount`만큼 **좌→우** 채움

### 스플래시 (`SplashController.cs`)

- `SplashScene` 첫 실행 — 로고 페이드인·확대 → 샤인 → 로고 페이드아웃 → `TitleScene`
- 타이틀 `LoadSceneAsync` **선로드** — 로고 사라진 뒤 흰 화면 대기 최소화
- SFX: `AudioManager.PlaySplashSfx()` — 페이드인 `splashSfxDelay` 후 재생

### 타이틀·인트로 페이드 (`TitleSceneFade.cs`, `AutoScrollIntro.cs`)

- **타이틀 → 인트로:** 흰 `FadeOverlay` 0.25초 + 인트로 비동기 로드 (`SceneLoadUI.StartButton`)
- **인트로 진입:** `fadeInFromTitle` 시 흰 화면 0.8초 SmoothStep 페이드인 후 스크롤
- **인트로 → 로딩:** 검은 암전 0.5초 (기존)

### 오디오 (`AudioManager.cs`, DontDestroyOnLoad)

| 슬라이더 / 설정 | 용도 |
|----------------|------|
| `bgmVolume` / `bgmFadeDuration` | 타이틀·게임 BGM + 페이드 인/아웃 |
| `neutralizationSfxVolume` | A~C 중화 루프 |
| `valveSfxVolume` | 가스 밸브 루프 |
| `sfxVolume` | 버튼·클리어·게임오버·스플래시 |

**씬별 BGM:** `TitleScene` / `GameScene`만 재생. 그 외는 페이드 아웃 후 정지.

### 회복 아이템 (`RecoveryItemSpawnPlan`, `RecoveryItemManager`, `RecoveryItemInventory`)

- 정의: `Assets/Data/recovery_items.json` (id, type, displayName, value, inv/map 프리팹 이름)
- 맵 프리팹: `mapProtectRecovPrefab`, `mapTimeRecovPrefab` — 로딩 시 배치 확정 → `GameScene` 스폰
- 인벤 프리팹: `invProtectRecovPrefab`, `invTimeRecovPrefab` (UI Instantiate용)
- 조작: `C`/`V` 선택, `Space` 사용
- **설계 합의(진행 중):** 종류(id)당 1칸 + count 스택, 기본 빈 슬롯 4칸, 5번째 종류부터 Scroll View — [회의록 2026-06-06](Assets/Docs/회의록/2026-06-06-시각미상-회복아이템-인벤-설계-합의.md)
- 현재 코드: `RecoveryItemInventory`는 고정 4칸 bool — 스택·스크롤·`invEmptySlotPrefab` **미구현**
- 회복 UI 선택: `dim` OFF = 선택됨 (`RecoveryItemInventoryUI`)

### 기타

| 모듈 | 역할 |
|------|------|
| `GameManager` | 클리어·게임오버·일시정지·디버그 키 |
| `SceneLoadManager` | 씬 전환, `pendingStageIndex` |
| `LoadingController` / `LoadingGuideTxt` | 페이드·플랜 준비·조작 안내 |
| `Background` | 스크롤, 오염원 활성/구간 미완료 시 정지 |
| `WorldSpaceUIFollower` | 방호복·오염원 HP 바 |

---

## 스테이지 예시 (`stage_data.json`)

| 스테이지 | 오염원 타입 | total | 제한 시간 |
|----------|------------|-------|-----------|
| 1-1 | C \| D | 2 | 60초 |
| 1-2 | A \| B | 3 | 55초 |
| 1-3 | A \| B \| C \| D | 4 | 50초 |

---

## 프로젝트 구조

```text
Assets/
├── Data/stage_data.json
├── Data/recovery_items.json
├── Scenes/
│   ├── SplashScene.unity
│   ├── TitleScene.unity
│   ├── IntroStoryScene.unity
│   ├── LoadingScene.unity
│   └── GameScene.unity
├── Scripts/
│   ├── Core/          GameManager, StageManager, ItemSelectManager, AudioManager, …
│   ├── GamePlay/      Player, Pollutant, PollutantManager, Item, …
│   └── UI/            GuideTxt, HelpGuideToggle, LoadingGuideTxt, SplashController, …
├── Prefabs/Game/      Player, PollutantA~D
├── Prefabs/Item/      Scanner, Neutralizer, pads, GasValve, map*Recov
├── Prefabs/InvenItem/ inv*Recov, invEmptySlotPrefab (예정)
├── Audio/SFX/
├── Docs/Bug/          버그·수정 기록
└── Animations/        Player Idle / Move / Die / Valve / Clear
```

### Hierarchy (GameScene)

| 이름 | 역할 |
|------|------|
| Player | 이동·방호복·애니·중화 VFX |
| PollutantManager | 경고·등장·맵 전환 |
| HUD_Canvas | `HelpGuideToggle`, `KeyGuidePannel`, `ItemGuide` |
| ItemSelectManager | Z/X 아이템 |
| AudioManager | BGM·SFX (DontDestroyOnLoad) |

### Inspector 체크

| 오브젝트 | 확인 |
|----------|------|
| **GameManager** | `clearAnimDelay`, `dieAnimDelay`, `clearSet`, `gameOverSet` |
| **HUD_Canvas** | `HelpGuideToggle` — 패널 참조 |
| **Player** Animator | `Clear` 트리거 → `Player_Clear` (Loop) |
| **AudioManager** | `splashClip`, `squeakyValveClip`, 볼륨·페이드 설정 |
| **ItemSelectManager** | `itemPrefabs` 5종 |

---

## 실행 방법

1. Unity `6000.4.8f1`로 프로젝트 열기
2. **권장:** `SplashScene` 실행 후 Play
3. 정상 플로우: 스플래시 → 타이틀 → 인트로 → 로딩 → 게임

### 플레이 테스트 체크리스트

- [ ] 스플래시 SFX·타이틀 BGM 페이드 인
- [ ] 로딩 씬 K/I 안내 문구 표시
- [ ] 가이드 후 좌우 이동, 경고 → 오염원 등장
- [ ] **Z 왼쪽 / X 오른쪽** 아이템 전환
- [ ] **K / I** 가이드 패널 토글 (기본 숨김)
- [ ] 정답: 오염원 HP + 중화 VFX / 오답: 방호복만 + VFX 없음
- [ ] 오염원 1마리 제거 후 선택 아이템 유지
- [ ] 가스: 밸브 애니 + 밸브 SFX
- [ ] 클리어: Clear 애니 2초 → 패널
- [ ] 사망: Die 애니 2초 → 게임오버 패널
- [ ] 클리어 별 **왼쪽부터** N개

---

## 구현 상태

### 완료

- 씬 흐름·`stage_data.json`·로딩 프리스폰·맵 구간 (`mapPollutants`)
- 접촉 판정·정답만 오염원 HP·가스 HP 초기화·밸브 애니/SFX
- Z/X 방향, 아이템 선택 유지(맵 중), 틀린 아이템 시 VFX 미재생
- 스플래시·BGM 씬별 페이드
- **씬 전환** — 스플래시 선로드, 타이틀→인트로 흰 페이드, 인트로 SmoothStep 진입
- **K/I** HUD 가이드, 로딩 안내
- 회복 아이템 JSON·맵/인벤 프리팹·`dim` 선택 UI (스택·Scroll 인벤 **진행 중**)
- **Player_Clear** 클리어 애니 + 2초 후 패널
- **Player_Die** 2초 후 게임오버 패널
- 클리어 별 좌→우, 일시정지·재시작

### 미구현·보류

- 회복 인벤: `invEmptySlotPrefab`, Scroll View 4칸+스택, JSON 로더, 중화 HUD와 패널 분리
- 멀티 스테이지 자동 진행 UI polish
- `AGENTS.md` 일부 구버전 설명(ClampPassThrough 등) — README·Bug 문서 기준 우선

---

## 문서

| 파일 | 용도 |
|------|------|
| [README.md](README.md) | 프로젝트 개요 (이 파일) |
| [AGENTS.md](AGENTS.md) | AI·협업·코딩 규칙 |
| [씬 전환 fixes](Assets/Docs/Bug/2026-06-06-시각미상-씬전환-스플래시-인트로-fixes.md) | **최근** — 스플래시·타이틀·인트로 페이드 |
| [회복 인벤 설계](Assets/Docs/회의록/2026-06-06-시각미상-회복아이템-인벤-설계-합의.md) | **최근** — 스택·스크롤·JSON·프리팹 |
| [클리어·가이드·VFX fixes](Assets/Docs/Bug/2026-06-05-시각미상-클리어-가이드-중화VFX-fixes.md) | 클리어/사망 연출, K/I 가이드, VFX |
| [스플래시·오디오 fixes](Assets/Docs/Bug/2026-06-05-시각미상-스플래시-오디오-fixes.md) | 스플래시·BGM |
| [맵구간 fixes](Assets/Docs/Bug/2026-06-05-오후1825-맵구간-게임플레이-fixes.md) | 맵·스폰 |
| [gameplay fixes](Assets/Docs/Bug/2026-06-04-시각미상-gameplay-fixes.md) | 접촉·이동·오디오 |
| [회의록 §4](Assets/Docs/회의록/2026-06-05-시각미상-플레이어-오염원-로직-검토.md) | Pollutant/Player 접촉 규칙 |

---

## 변경 이력 (요약)

| 날짜 | 주요 내용 |
|------|-----------|
| 2026-06-06 | 씬 전환(스플래시 선로드, 타이틀→인트로 흰 페이드), 회복 아이템 JSON·인벤 설계 — [Bug](Assets/Docs/Bug/2026-06-06-시각미상-씬전환-스플래시-인트로-fixes.md) · [회의록](Assets/Docs/회의록/2026-06-06-시각미상-회복아이템-인벤-설계-합의.md) |
| 2026-06-05 | 클리어/사망 2초 연출, K/I 가이드, 아이템 유지, 틀린 아이템 VFX 수정 — [Bug](Assets/Docs/Bug/2026-06-05-시각미상-클리어-가이드-중화VFX-fixes.md) |
| 2026-06-05 | 스플래시·BGM, 맵 구간 — [오디오](Assets/Docs/Bug/2026-06-05-시각미상-스플래시-오디오-fixes.md) · [맵](Assets/Docs/Bug/2026-06-05-오후1825-맵구간-게임플레이-fixes.md) |
| 2026-06-04 | 로딩 프리스폰, 이동/가스/ZX/별점 — [Bug](Assets/Docs/Bug/2026-06-04-시각미상-gameplay-fixes.md) |

---

## 개발 메모

- HP는 **float** 계산, UI는 **int** (`FloorToInt`)
- 타입: `Item.ItemType`, `Pollutant.PollutantType`만 사용
- `AudioManager`·`SceneLoadManager` — `DontDestroyOnLoad`
- 가이드 패널: 부모만 비활성, 자식은 활성 유지
- `Library/`, `Temp/`, `Logs/` — Git·수정 대상 아님

---

## 폰트·미디어 저작권

- **Galmuri Font** — © 2019–2023 Minseo Lee
- **시작 버튼 SFX** — [BoostSound](https://www.youtube.com/watch?v=YNSbL-Cek1c)
- **Title / Game BGM** — SUNO 생성
