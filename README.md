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

| 오염원 타입 | 예시 물질 | 추천 아이템 |
|-----------|----------|------------|
| TypeA (부식성) | 염산, 황산, 질산 | 중화제 (`Neutralizer`) |
| TypeB (유류) | 폐유, 윤활유 등 | 오일 흡착패드 (`OilPad`) |
| TypeC (혼합화학액) | 폐산 혼합액, 화학 슬러지 등 | 범용 흡착 패드 (`GeneralPad`) |

`Scanner`는 탐지용이며 오염원 HP에는 데미지를 주지 않습니다.

---

## 조작

| 입력 | 동작 |
|------|------|
| `←` / `→` 또는 `A` / `D` | 좌우 이동 |
| `Z` | 아이템 변경 (Scanner → Neutralizer → GeneralPad → OilPad) |

가이드 텍스트가 끝나면 이동이 활성화됩니다 (`Player.canMove`).

---

## 핵심 시스템

### 1. 플레이어 (`Player.cs`)

- **방호복:** `maxProtection` / `curProtection` (float), UI는 정수 `%` 표시
- **상태:** `Idle`, `Move`, `Die` (Animator `State` 파라미터)
- **이동 범위:** `leftLimit` ~ `rightLimit`, 오염원 생성 시 `GrowRange()`로 확장 가능
- **사망:** 방호복 0 이하 시 `Die` 상태 후 오브젝트 제거 (사망 연출·페이드는 미구현)

### 2. 오염원 (`Pollutant.cs`)

접촉 중 (`OnTriggerStay2D`):

1. 아이템 판정 로그 (올바른/틀린 + 추천·선택 타입)
2. 플레이어 방호복 감소 (`pollutantDps × Δt`)
3. **추천 아이템과 일치할 때만** 오염원 HP 감소 (`itemDps × Δt`)

접촉 해제 (`OnTriggerExit2D`):

- 오염원 HP를 `pollutanMaxHp`로 초기화
- 판정 로그 상태 리셋

오염원 HP 0 이하 시 페이드아웃 후 제거.

### 3. 아이템 (`Item.cs`)

```text
Scanner     → DPS 0
Neutralizer → DPS 10
GeneralPad  → DPS 18
OilPad      → DPS 20
```

`ItemSelectManager`가 선택 인덱스·UI·타입을 관리합니다.

### 4. 오염원 생성 (`PollutantManager.cs`)

- 플레이어가 **이동 중**일 때만 시간 누적 (2~3초)
- 경고 문구 깜빡임 → 오염원 스폰 (`PollutantSpawner`)
- 생성 시 배경 스크롤 일시정지, 팝업으로 처리 방법 안내

### 5. 씬 전환 (`SceneLoadManager.cs`)

타이틀 ↔ 게임 씬 로드.

### 6. UI

| 스크립트 | 역할 |
|---------|------|
| `GuideTxt` | 시작 가이드, 종료 후 이동 허용 |
| `WarningTxt` | 오염원 발견 경고 |
| `PopupUI` | 오염물질·추천 아이템 안내 |
| `Timer` | 제한 시간 |
| `Background` | 배경 스크롤 (이동 방향 반대) |
| `ItemManager` | 선택 아이템 HUD |

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
│       └── ...
├── Prefabs/
│   ├── Game/          # Player, PollutantA/B/C, PollutantSpawner
│   └── Item/          # Scanner, Neutralizer, GeneralPad, OilPad
└── Animations/        # Player Idle / Move
```

---

## 실행 방법

1. Unity Hub에서 프로젝트 폴더 열기 (`6000.4.8f1` 권장)
2. `Assets/Scenes/GameScene.unity` 또는 `TitleScene.unity` 실행
3. Play 후 가이드 종료 → 이동 → 오염원 접촉 테스트

### 디버그 로그 확인 시

Console에서 **Collapse**를 끄면 접촉 중 매 프레임 로그를 구분하기 쉽습니다.  
접촉 중 예상 로그 순서:

```text
올바른 아이템입니다. 추천 = ..., 선택 = ...
[Player] 방호복 HP 감소: -0.xx ...
[Pollutant] 오염원 HP 감소: -0.xx ...   (정답 아이템일 때만 실제 감소)
```

---

## 현재 구현 상태

### 완료

- 타이틀 ↔ 게임 씬 연결
- 플레이어 1차 이동 구간 제한 및 배경 스크롤
- 오염원 경고·생성·페이드 인/아웃
- 아이템 선택 (`Z`) 및 HUD
- 접촉 시 방호복 / 오염원 HP 초당 감소
- 추천 아이템 판정 로그
- 정답 아이템일 때만 오염원 HP 감소
- 접촉 해제 시 오염원 HP 초기화

### 미구현 / 보류

- 사망 Die 애니메이션 + 페이드아웃 (코드 롤백 상태)
- 스테이지 클리어·게임 오버 UI 연동 (`GameManager`, `StageManager`는 골격만 존재)

---

## 개발 메모

- HP 계산은 **float**, UI 표시는 **정수(Floor)** 로 분리
- 오염원 타입별 `pollutanMaxHp` / `pollutantDps`는 `Pollutant.SetHpByType()`에서 설정
- 로컬 변경 사항은 Git에 커밋되지 않았을 수 있으므로, 배포 전 `git status`로 확인 권장
