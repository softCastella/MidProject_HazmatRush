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
| `Z` | 아이템 변경 (Scanner → Neutralizer → GeneralPad → OilPad) |
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
        └ 누적 이동 시간 도달 → 경고 문구 깜빡임(1.5초)
              └ 오염원 생성 (페이드 인)
                    └ 페이드 인 ~80% 시점에 처리 안내 팝업 (1.5초)
              └ 접촉 → 중화 (오염원/방호복 HP 실시간 변화)
        └ 오염원 중화 완료
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
- **자동 복귀(3차 이동):** 오염원 중화 후 잔여 오염원이 있으면 `AutoReturnToStart()`로 시작 지점까지 자동 이동, 범위를 1차로 리셋. 이동 속도는 `returnMoveSpeed`로 별도 조절. 복귀 중에는 오른쪽을 향하도록 고정
- **일시정지 중 입력 차단** 및 `canMove=false` 시 입력 무시
- **사망:** 방호복 0 이하 시 `Die` 상태 → `GameManager.TriggerGameOver()` 호출 후 오브젝트 제거

### 2. 오염원 (`Pollutant.cs`)

접촉 중 (`OnTriggerStay2D`):

1. 아이템 판정 로그 (올바른/틀린 + 추천·선택 타입)
2. 플레이어 방호복 감소 (`pollutantDps × Δt`)
3. **추천 아이템과 일치할 때만** 오염원 HP 감소 (`itemDps × Δt`)
4. 오염원 HP 슬라이더 값 갱신

접촉 시작/해제 (`OnTriggerEnter2D` / `OnTriggerExit2D`):

- 접촉 시 방호복·오염원 HP 슬라이더 표시 + 추적 타겟 연결
- 해제 시 오염원 HP를 `pollutanMaxHp`로 초기화, 슬라이더 숨김

오염원 HP 0 이하 → 페이드아웃 후 제거, 이때 `StageManager.AddClearedPollutant()` 호출 + HP 바 숨김.

### 3. 아이템 (`Item.cs`)

```text
Scanner     → DPS 0
Neutralizer → DPS 12
GeneralPad  → DPS 14
OilPad      → DPS 8
```

`ItemSelectManager`가 선택 인덱스·UI·타입을 관리합니다 (`Z` 키 순환, 일시정지 중 차단).

### 4. 오염원 생성 (`PollutantManager.cs`)

- 플레이어가 **이동 중**일 때만 시간 누적 (2~3초)
- 경고 문구 깜빡임(`WarningTxt`) → 오염원 스폰(`PollutantSpawner`) → 페이드 인 → 처리 안내 팝업
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
  - 결과/원인은 **Console 로그로만** 출력 (패널 텍스트는 미사용)
  - `Time.timeScale` 기반 일시정지 (`ESC` / `PauseSet` 패널)
- `StageManager`: `stageLabel`, `totalPollutants`, `clearedPollutants` 관리 + `IsAllCleared()`

### 7. 씬 전환 (`SceneLoadManager.cs`)

- 타이틀 ↔ 게임 씬 로드 (`StartButton()` / `TitleButton()`)
- **씬마다 존재하는 일반 컴포넌트** (DontDestroyOnLoad 미사용)
  - 싱글톤+DontDestroyOnLoad로 두면 새 씬의 인스턴스가 파괴되어 버튼 OnClick 참조가 끊기는 문제가 있어 일반 컴포넌트로 유지
- 씬 로드 시 `Time.timeScale = 1f` 복구

### 8. 오디오 (`AudioManager.cs`)

- DontDestroyOnLoad 싱글톤 (BGM 씬 전환 중 유지)
- 타이틀 / 게임 BGM 크로스페이드 (`PlayTitleBGM()` / `PlayGameBGM()`)

### 9. UI

| 스크립트 | 역할 |
|---------|------|
| `GuideTxt` | 시작 가이드, 종료 후 이동/타이머/배경 활성화 |
| `WarningTxt` | 오염원 발견 경고 (깜빡임) |
| `PopupUI` | 오염물질·추천 아이템 안내 팝업 |
| `Timer` | 제한 시간, 0 도달 시 게임 오버 트리거 |
| `Background` | 무한 스크롤 배경 (`player.hasInput` 연동, 경계 Repeat) |
| `ItemManager` | 선택 아이템 HUD (dim 처리) |
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
│   │   ├── ItemType.cs
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
- 오염원 경고(깜빡임)·생성·페이드 인/아웃·처리 안내 팝업
- 아이템 선택(`Z`) 및 HUD
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
- BGM 클립 연결 및 씬별 재생 호출 연동

---

## 개발 메모

- HP 계산은 **float**, UI 표시는 **정수(Floor)** 로 분리
- 오염원 타입별 `pollutanMaxHp` / `pollutantDps`는 `Pollutant.SetHpByType()`, 아이템 DPS는 `Item.GetDps()`에서 설정
- 결과 패널은 **활성화만** 담당하고, 상세 정보는 Console 로그로 출력
- `DontDestroyOnLoad`는 상태 유지가 필요한 `AudioManager`에만 사용 (UI/씬 매니저는 씬별 컴포넌트로 유지)
- 로컬 변경 사항은 Git에 커밋되지 않았을 수 있으므로, 배포 전 `git status`로 확인 권장
