# MidProject

2D 횡스크롤 환경에서 오염물질을 탐지·중화하는 Unity 게임 프로젝트입니다.  
방호복을 유지하면서 이동하고, 오염원 유형에 맞는 아이템으로 오염원을 제거하는 것이 핵심 플레이입니다.

- **엔진:** Unity `6000.4.8f1`
- **언어:** C#
- **주요 씬:** `TitleScene`, `GameScene`

---

## 게임 개요

플레이어는 제한된 구간(`x: -785` ~ `-403`)을 좌우로 이동하며 오염원에 접근합니다.  
이동 중 일정 시간이 지나면 경고 후 오염원이 생성되고, 접촉 시 방호복 HP와 오염원 HP가 실시간으로 변합니다.  
스테이지의 모든 오염원을 시간 내에 중화하면 클리어, 방호복이 소진되거나 시간이 초과되면 게임 오버입니다.

| 오염원 타입 | 예시 물질 | 추천 아이템 | HP | 처치 시간(추천 아이템) |
|-----------|----------|------------|----|----------------------|
| TypeA (부식성) | 염산, 황산, 질산 | 중화제 (`Neutralizer`) | 40 | 약 3.3초 |
| TypeB (유류) | 폐유, 윤활유 등 | 오일 흡착패드 (`OilPad`) | 20 | 약 2.5초 |
| TypeC (혼합화학액) | 폐산 혼합액, 화학 슬러지 등 | 범용 흡착 패드 (`GeneralPad`) | 55 | 약 3.9초 |

`Scanner`는 탐지용이며 오염원 HP에는 데미지를 주지 않습니다.

---

## 조작

| 입력 | 동작 |
|------|------|
| `←` / `→` 또는 `A` / `D` | 좌우 이동 |
| `Z` | 아이템 변경 *(경고 표시 후 ~ 오염원 중화 전만 가능, 아래 **아이템 선택** 참고)* |
| `ESC` | 일시정지 / 재개 토글 |
| `F1` (디버그) | 강제 클리어 |
| `F2` (디버그) | 강제 게임 오버 |

가이드 텍스트가 끝나면 이동·타이머·배경 스크롤이 활성화됩니다.

---

## 플레이 흐름

```text
게임 시작
  └ 안내 문구 표시 (페이드 인 → 유지 → 페이드 아웃)
        └ 종료 시 플레이어 이동 / 타이머 / 배경 스크롤 활성화
  └ 플레이어 우측 이동 (1차 범위)
        └ 누적 이동 시간 도달 → 경고 문구 깜빡임 (WarningTxt)
              └ GuideTxt: "Z키로 대응 아이템을 골라주세요" (약 2초, 표시만)
              └ 오염원 생성 (페이드 인)
                    └ 페이드 인 ~80% 시점에 처리 안내 팝업 (1.5초)
              └ 접촉 → 중화 (오염원/방호복 HP 실시간 변화)
        └ 오염원 중화 완료 → Scanner(기본)로 복귀, Z키 전환 잠금
              ├ 남은 오염원 있음 → 시작 지점으로 자동 복귀(3차 이동) 후 루프 반복
              └ 마지막 오염원 → 그 자리에서 클리어 처리
결과
  ├ 클리어  : 방호복 ≥ 1 & 시간 잔여 & 전체 오염원 중화 → ClearSet 패널
  └ 게임오버: 방호복 0 / 타임오버 → GameOverSet 패널
```

---

## 핵심 시스템

### 1. 플레이어 (`Player.cs`)

- **방호복:** `maxProtection` / `curProtection` (float), UI는 정수 `%` 표시
- **상태:** `Idle`, `Move`, `Die` (Animator `State` 파라미터)
- **이동:** `Rigidbody2D.MovePosition` 기반 (트리거 접촉 안정화)
- **이동 범위(1차):** `leftLimit` ~ `rightLimit`, 오염원 생성 시 `GrowRange()`로 확장
- **자동 복귀(3차 이동):** 오염원 중화 후 잔여 오염원이 있으면 `AutoReturnToStart()`로 시작 지점까지 자동 이동, 범위를 1차로 리셋
  - `returnMoveSpeed`(기본 900), `returnStopDistance`(기본 0.2)로 속도·도착 판정 조절
  - `Rigidbody2D` + `WaitForFixedUpdate` 기반 이동, `SnapToStartPosition()`으로 정확히 복귀
  - `isReturning` 플래그로 복귀 중 입력·Idle 처리 분리, 게임 종료 시 복귀 코루틴 중단
- **`StopMovement()`:** 클리어/게임오버 시 `GameManager.FreezePlayOnResult()`에서 호출, 이동·속도 즉시 정지
- **일시정지·게임 종료 중 입력 차단** (`IsPaused`, `GameEnded`), 복귀 중(`isReturning`)은 예외 처리
- **사망:** 방호복 0 이하 시 `Die` 상태 → `GameManager.TriggerGameOver()` 호출

### 2. 오염원 (`Pollutant.cs`)

접촉 중 (`OnTriggerStay2D`):

1. 아이템 판정 로그 (올바른/틀린 + 추천·선택 타입)
2. 플레이어 방호복 감소 (`pollutantDps × Δt`)
3. **추천 아이템과 일치할 때만** 오염원 HP 감소 (`itemDps × Δt`) + **중화 SFX 루프 재생**
4. 틀린 아이템·접촉 해제·중화 완료 시 중화 SFX 정지
5. 오염원 HP 슬라이더 값 갱신

접촉 시작/해제 (`OnTriggerEnter2D` / `OnTriggerExit2D`):

- 접촉 시 방호복·오염원 HP 슬라이더 표시 + 추적 타겟 연결
- 해제 시 오염원 HP를 `pollutanMaxHp`로 초기화, 슬라이더 숨김

오염원 HP 0 이하 → 중화 SFX 정지 · Scanner 복귀(`ItemSelectManager.ResetToDefault()`) → 페이드아웃 후 제거 · `StageManager.AddClearedPollutant()` 호출.

### 3. 아이템 (`Item.cs`, `ItemSelectManager.cs`)

```text
Scanner     → DPS 0  (이동 중 기본 선택, 탐지용)
Neutralizer → DPS 12
GeneralPad  → DPS 14
OilPad      → DPS 8
```

**아이템 선택 규칙 (`ItemSelectManager`, `Z` 키)**

| 상황 | 선택 상태 | Z키 |
|------|----------|-----|
| 이동 중 | Scanner 고정 | 전환 불가 |
| 경고 표시 ~ 첫 Z 전 | Scanner | 전환 가능 |
| 첫 Z 이후 (오염원 HP > 0) | 중화제 / 범용패드 / 오일패드 순환 | Scanner 슬롯 딤 처리 |
| 오염원 HP = 0 | Scanner로 복귀 | 전환 불가 (이동 중과 동일) |
| 게임오버·클리어·스테이지 재시작 | Scanner 초기화 | 위 규칙 다시 적용 |

- 경고가 뜨면 `OnWarningShown()`으로 전환 허용, 오염원 중화 시 `ResetToDefault()`로 Scanner 복귀
- 첫 Z 입력: Scanner → 중화제(index 1), 이후 1→2→3 순환 (Scanner 제외)
- `ItemManager`가 선택 슬롯 밝게 / 비선택·Scanner 딤 처리

### 4. 오염원 생성 (`PollutantManager.cs`)

- 플레이어가 **이동 중**일 때만 시간 누적 (2~3초)
- 경고 깜빡임(`WarningTxt`) → **Z키 안내**(`GuideTxt`, `itemSelectHintDuration` 기본 2초) → 오염원 스폰 → 페이드 인 → 처리 안내 팝업
- 안내 문구 2초는 **표시 시간만** 해당. Z키 전환은 경고 표시 후 ~ 오염원 중화까지 유지
- 생성 시 배경 스크롤 일시정지
- 오염원 중화 후: 잔여 오염원 있으면 플레이어 자동 복귀, **마지막이면 `GameManager.TriggerClear()`**

### 5. HP 바 (`WorldSpaceUIFollower.cs`)

- HUD_Canvas의 `protectionSlider` / `pollutantSlider`를 월드 좌표로 추적
- 플레이어 머리 위 / 오염원 위에 떠서 표시
- **중화 모드(접촉) 중에만** 표시, 둘 중 하나 소멸 시 숨김

### 6. 결과·진행 관리 (`GameManager.cs`, `StageManager.cs`)

- `GameManager` (씬 싱글톤): 클리어 / 게임 오버 / 일시정지 제어
  - 클리어 조건: 방호복 ≥ 1 & 시간 잔여 & 전체 오염원 중화
  - 게임 오버 원인: `ProtectionDepleted`, `TimeOver`, `Debug`
  - **`FreezePlayOnResult()`:** 타이머 정지 · `Player.StopMovement()` · `PollutantManager.StopReturnFlow()`
  - 클리어/게임오버 시 BGM 정지 + 결과 SFX, 스테이지 재시작·다음 스테이지 시 게임 BGM 재생
  - 결과/원인은 **Console 로그로만** 출력 (패널 텍스트는 미사용)
  - `Time.timeScale` 기반 일시정지 (`ESC` / `PauseSet` 패널)
- `StageManager`: `stageLabel`, `totalPollutants`, `clearedPollutants` 관리 + `IsAllCleared()`
- `Timer`: `GameEnded` 시 카운트다운 추가 감소 방지

### 7. 씬 전환 (`SceneLoadManager.cs`)

- 타이틀 ↔ 게임 씬 로드 (`StartButton()` / `TitleButton()`)
- **씬마다 존재하는 일반 컴포넌트** (DontDestroyOnLoad 미사용)
  - 싱글톤+DontDestroyOnLoad로 두면 새 씬의 인스턴스가 파괴되어 버튼 OnClick 참조가 끊기는 문제가 있어 일반 컴포넌트로 유지
- 씬 로드 시 `Time.timeScale = 1f` 복구

### 8. 오디오 (`AudioManager.cs`)

- DontDestroyOnLoad 싱글톤 (BGM 씬 전환 중 유지)
- **BGM:** 타이틀 / 게임 (`PlayTitleBGM()` / `PlayGameBGM()` / `StopBGM()`), 씬 로드 시 자동 전환
- **SFX (PlayOneShot):** 버튼 클릭 · 클리어(`clearSFX`) · 게임오버(`game-overSFX`)
- **중화 SFX:** 정답 아이템 접촉 중 `neutralizationSFX` 루프 재생, `neutralizationSfxVolume`으로 별도 볼륨 조절
- BGM / SFX 소스 분리 (`bgmSource`, `sfxSource`)

```text
Assets/Audio/SFX/
├── selectionSFX.ogg      # 버튼
├── clearSFX.ogg          # 클리어
├── game-overSFX.ogg      # 게임오버
└── neutralizationSFX.wav # 중화 (루프)
```

### 9. UI

| 스크립트 | 역할 |
|---------|------|
| `GuideTxt` | 시작 가이드, 종료 후 이동/타이머/배경 활성화 · **경고 후 Z키 아이템 선택 안내** (`ShowItemSelectHintRoutine`) |
| `WarningTxt` | 오염원 발견 경고 깜빡임만 (`ShowWarningRoutine`) |
| `PopupUI` | 오염물질·추천 아이템 안내 팝업 |
| `Timer` | 제한 시간, 0 도달 시 게임 오버 트리거 |
| `Background` | 무한 스크롤 배경 (`player.hasInput` 연동, 경계 Repeat) |
| `ItemManager` | 선택 아이템 HUD (비선택·Scanner 딤 처리) |
| `ItemSelectManager` | Scanner 기본 / 경고 후 전환 / 중화 후 리셋 |
| `StageUI` | 스테이지 라벨 / 오염원 수 표시 |

---

## 프로젝트 구조

```text
Assets/
├── Scenes/
│   ├── TitleScene.unity
│   └── GameScene.unity
├── Scripts/
│   ├── Core/
│   │   ├── SceneLoadManager.cs
│   │   ├── ItemSelectManager.cs
│   │   ├── GameManager.cs
│   │   ├── AudioManager.cs
│   │   └── StageManager.cs
│   ├── GamePlay/
│   │   ├── Player.cs
│   │   ├── Pollutant.cs
│   │   ├── PollutantManager.cs
│   │   ├── PollutantSpawner.cs
│   │   └── Item.cs
│   └── UI/
│       ├── GuideTxt.cs
│       ├── WarningTxt.cs
│       ├── PopupUI.cs
│       ├── Timer.cs
│       ├── Background.cs
│       ├── StageUI.cs
│       ├── ItemManager.cs
│       ├── UIItem.cs
│       └── WorldSpaceUIFollower.cs
├── Prefabs/
│   ├── Game/          # Player, PollutantA/B/C, PollutantSpawner
│   └── Item/          # Scanner, Neutralizer, GeneralPad, OilPad
├── Audio/SFX/         # 버튼·클리어·게임오버·중화 효과음
├── UI/                # HUD·슬롯·결과 패널용 스프라이트 (2026-05-31 UI 교체 분 포함)
└── Animations/        # Player Idle / Move
```

### HUD_Canvas 구성 (주요 패널)

```text
HUD_Canvas
├── Protection / StageInfo / Timer / ItemContainer ...
├── protectionSlider / pollutantSlider   (WorldSpaceUIFollower)
├── Result_HUD
│   ├── ClearSet       (GameManager.clearSet)
│   └── GameOverSet    (GameManager.gameOverSet)
└── Pause_HUD
    └── PauseSet       (GameManager.pauseSet)
```

> 패널 표시 규칙: 부모(`Result_HUD`, `Pause_HUD`)는 항상 활성, 자식 세트(`ClearSet`/`GameOverSet`/`PauseSet`)는 비활성 상태로 두면 코드가 토글합니다.

---

## 실행 방법

1. Unity Hub에서 프로젝트 폴더 열기 (`6000.4.8f1` 권장)
2. `Assets/Scenes/TitleScene.unity` 또는 `GameScene.unity` 실행
3. Play 후 가이드 종료 → 이동 → 오염원 접촉 테스트

### 결과 테스트

- **클리어:** `StageManager.totalPollutants`를 작게(예: 1) 두고 실제 중화, 또는 `F1`
- **게임 오버:** 오염원에 계속 접촉해 방호복 소진, `Timer.startSeconds`를 짧게 두고 타임오버, 또는 `F2`

### 디버그 로그

```text
올바른 아이템입니다. 추천 = ..., 선택 = ...
[Player] 방호복 HP 감소: -0.xx ...
[Pollutant] 오염원 HP 감소: -0.xx ...    (정답 아이템일 때만 실제 감소)
[GameManager] 스테이지 클리어 - 오염원수 x/x / 남은 시간: xx초 / 방호복 내구도: xx%
[GameManager] 게임 오버 - 원인: ...
```

---

## 현재 구현 상태

### 완료

- 타이틀 ↔ 게임 씬 연결 (씬별 SceneLoadManager)
- 시작 연출: 안내 문구 → 이동/타이머/배경 스크롤 활성화
- 플레이어 1차 이동 구간 제한 및 무한 배경 스크롤
- 오염원 경고(깜빡임)·**Z키 아이템 선택 안내(GuideTxt)**·생성·페이드 인/아웃·처리 안내 팝업
- **아이템 선택 단계형 로직** (이동 중 Scanner 고정 → 경고 후 전환 → 중화 후 리셋)
- 아이템 선택 HUD (`Z`, Scanner 딤 처리)
- **결과·중화 SFX** (클리어 / 게임오버 / 중화 루프) 및 BGM 연동
- **플레이어 자동 복귀 안정화** (FixedUpdate 이동, 도착 거리, 게임 종료 시 중단)
- **결과 시 플레이 동결** (`FreezePlayOnResult`, 복귀 코루틴·타이머 정지)
- **TitleScene / GameScene UI·오디오 클립 연결** (로컬, 미푸시)
- 접촉 시 방호복 / 오염원 HP 초당 감소 (정답 아이템일 때만 오염원 감소)
- 접촉 해제 시 오염원 HP 초기화
- 오염원/방호복 HP 바 (월드 추적, 중화 모드 중에만 표시)
- 오염원 중화 후 플레이어 자동 복귀(3차 이동) 및 루프
- 스테이지 클리어 / 게임 오버 판정 및 패널 연동
- 일시정지(ESC / 패널 버튼) 및 `Time.timeScale` 처리
- 디버그 강제 클리어/게임오버(F1/F2)

### 미구현 / 보류

- 사망 Die 애니메이션 + 페이드아웃 (코드 롤백 상태)
- 멀티 스테이지 진행 (씬 분리 또는 StageData 기반 — 설계 검토 단계)

---

## 변경 이력

### 2026-05-31 (로컬 작업 · GitHub 미푸시)

#### 아이템 선택

| 항목 | 내용 |
|------|------|
| 이동 중 | Scanner 고정, Z키 전환 불가 |
| 경고 후 | 전환 허용. 첫 Z 전 Scanner, 이후 Scanner 딤 + 대응 아이템(1→2→3)만 순환 |
| 중화 완료 | Scanner 복귀, Z 잠금 |
| 스테이지 리셋 | 게임오버·클리어·재시작 시 Scanner 초기화 |

#### UI / 안내

| 항목 | 내용 |
|------|------|
| 경고 | `WarningTxt` — 오염원 발견 깜빡임만 (`ShowWarningRoutine`) |
| Z키 안내 | `GuideTxt` — "Z키로 대응 아이템을 골라주세요" (기본 2초, **표시만**) |
| HUD | `ItemManager` — 선택 슬롯 밝게 / Scanner 딤 처리 |
| 에셋 | `Assets/UI/` Gemini·ChatGPT 생성 아이콘 등 UI 교체 분 추가 |
| 씬 | `TitleScene.unity`, `GameScene.unity` Inspector 연결 갱신 |

#### 오디오

| SFX | 트리거 |
|-----|--------|
| `clearSFX.ogg` | 스테이지 클리어 (BGM 정지 후) |
| `game-overSFX.ogg` | 게임 오버 (BGM 정지 후) |
| `neutralizationSFX.wav` | 정답 아이템 접촉 중 루프 (틀린 아이템·해제·중화 완료 시 정지) |
| `selectionSFX.ogg` | 버튼 클릭 (기존) |

- `neutralizationSfxVolume` — 중화음만 별도 볼륨
- 스테이지 재시작/다음 스테이지 → 게임 BGM 재생

#### 게임플레이·버그 수정

| 파일 | 변경 |
|------|------|
| `Player.cs` | 자동 복귀 FixedUpdate 이동, `returnStopDistance`, `isReturning`, `StopMovement()` |
| `GameManager.cs` | `FreezePlayOnResult()` — 타이머·이동·복귀 코루틴 일괄 정지 + 결과 SFX |
| `PollutantManager.cs` | `StopReturnFlow()`, 스테이지 리셋 시 아이템·가이드 초기화 |
| `Pollutant.cs` | 중화 SFX 재생/정지, HP 0 시 Scanner 리셋 |
| `Timer.cs` | `GameEnded` 후 시간 추가 감소 방지 |

#### 수정·추가 파일 목록

```text
Scripts/
  Core/     AudioManager.cs, GameManager.cs, ItemSelectManager.cs
  GamePlay/ Player.cs, Pollutant.cs, PollutantManager.cs
  UI/       GuideTxt.cs, WarningTxt.cs, ItemManager.cs, Timer.cs
Scenes/     TitleScene.unity, GameScene.unity
Audio/SFX/  clearSFX.ogg, game-overSFX.ogg, neutralizationSFX.wav
UI/         아이콘·HUD 스프라이트 (Gemini / ChatGPT 생성분)
Docs/       README.md
```

---

## 개발 메모

- HP 계산은 **float**, UI 표시는 **정수(Floor)** 로 분리
- 오염원 타입별 `pollutanMaxHp` / `pollutantDps`는 `Pollutant.SetHpByType()`, 아이템 DPS는 `Item.GetDps()`에서 설정
- 결과 패널은 **활성화만** 담당하고, 상세 정보는 Console 로그로 출력
- `DontDestroyOnLoad`는 상태 유지가 필요한 `AudioManager`에만 사용 (UI/씬 매니저는 씬별 컴포넌트로 유지)
- 로컬 변경 사항은 Git에 커밋되지 않았을 수 있으므로, 배포 전 `git status`로 확인 권장

##  폰트, 미디어 저작권
 - Galmuri Font(© 2019-2023 Minseo Lee (itoupluk427@gmail.com))
 - 시작버튼 SFX
   Sounds of button selection in the game menu (sound effect) 2번째 사운드
   [BoostSound] https://www.youtube.com/watch?v=YNSbL-Cek1c
 - TitleBGM, GameSceneBGM
   generated by SUNO

