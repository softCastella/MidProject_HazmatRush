# AGENTS.md — MidProject AI·협업 가이드

이 파일은 **Cursor 등 AI 에이전트**가 이 프로젝트에서 코드를 쓸 때 따르는 규칙입니다.  
초보 개발자도 읽을 수 있지만, **2절(핵심 원칙)** 은 AI 전용 지침입니다.

**AI 요약:** 문법·구조는 단순하게 / 기존 변수·로직·문법 스타일 우선 / 요청 범위만 최소 수정.

**Cursor Rules (자동 적용):** `.cursor/rules/midproject-core.mdc` (항상), `.cursor/rules/unity-gameplay.mdc` (`Assets/Scripts/**/*.cs` 작업 시)

> 요약 문서: [README.md](README.md)

---

## 1. 프로젝트 한 줄 요약

**2D 횡스크롤 오염 대응 게임.** 플레이어가 이동하며 오염원에 접촉하고, **맞는 아이템**으로만 오염원 HP를 깎습니다. 접촉 중에는 방호복 HP가 계속 감소합니다.

- Unity `6000.4.8f1`
- 씬: `Assets/Scenes/TitleScene.unity`, `GameScene.unity`
- 스크립트 루트: `Assets/Scripts/`

---

## 2. AI 에이전트가 지켜야 할 것

> **이 섹션은 AI(Cursor)가 코드를 쓸 때 최우선으로 따르는 규칙입니다.**

### 핵심 원칙 (최우선)

#### 1) 문법과 구조는 단순하게

- 한 함수는 **한 가지 일**만 하게 유지
- 짧은 `if` / `for` / `switch` — 중첩 2단계 넘기지 않기
- LINQ, 고급 제네릭, 이벤트/디자인 패턴(옵저버, 싱글톤 신규 추가 등) **쓰지 않기**
- 헬퍼 클래스·인터페이스·추상 클래스 **새로 만들지 않기** (요청 없으면)
- 주석은 **왜(비즈니스 이유)** 가 안 보일 때만 짧게

```csharp
// ✅ 좋음 — 기존 파일 스타일과 비슷
float damage = pollutantDps * Time.deltaTime;
curProtection = Mathf.Max(0, curProtection - damage);

// ❌ 나쁨 — 이 프로젝트에 과함
curProtection = ApplyDamageOverTime(curProtection, pollutantDps, DamageContext.FromPollutant(this));
```

#### 2) 기존 로직·스타일을 우선

수정 전에 **해당 파일을 먼저 읽고**, 아래를 **그대로 이어서** 작성한다.

| 항목 | 이 프로젝트에서 따를 것 |
|------|------------------------|
| 변수명 | `curProtection`, `pollutanCurHp`, `pollutanDps`, `canMove` 등 **이미 있는 이름** 재사용 |
| 타입 | `Item.ItemType`, `Pollutant.PollutantType` — 새 enum 만들지 않기 |
| null 체크 | `if (player == null) return;` 패턴 유지 |
| Unity API | `FindAnyObjectByType`, `GetComponent`, `OnTriggerStay2D` 등 **파일 안에 이미 쓰인 방식** |
| 로그 | `Debug.Log($"[Player] ... {value:F2}")` 형식 유지 |
| 애니메이션 | `anim.SetInteger("State", (int)currentState)` — Trigger/복잡한 Animator 코드 추가 금지(요청 시만) |

- **기능 추가 = 기존 메서드에 몇 줄 추가**가 기본. 파일 통째 교체·대규모 리팩터링 금지
- 비슷한 기능이 이미 있으면 **복사·확장** (예: 데미지는 `ApplyPollutantDamage` / `OnTriggerStay2D` 흐름에 맞춤)

### 반드시

- **변경 범위 최소화** — 요청과 무관한 파일·리팩터링 금지
- **기존 패턴 따르기** — 네이밍, 폴더 구조, `Item.ItemType` 등 프로젝트 관례 유지
- **Unity 메타 파일** — `.cs` 추가·삭제 시 `.meta`도 함께 고려 (Unity가 자동 생성하는 경우 많음)
- **HP는 float, UI는 int** — `curProtection`, `pollutanCurHp`는 float로 계산, 화면·`%` 텍스트는 `Mathf.FloorToInt` 등으로 표시
- **접촉 데미지 순서** (`Pollutant.OnTriggerStay2D`):
  1. 아이템 판정 로그
  2. `player.ApplyPollutantDamage(pollutanDps)`
  3. **추천 아이템 == 선택 아이템**일 때만 오염원 HP 감소
- **커밋은 사용자가 요청할 때만** — 자동 `git commit` 하지 않음
- **`Library/`, `Temp/`, `Logs/`** — 수정·커밋 대상 아님

### 하지 말 것

- 사망 연출(Die 애니 + 페이드 코루틴)을 **사용자 요청 없이** 다시 넣지 않기 (과거 롤백된 영역)
- `ItemType` 전역 enum 복구하지 않기 → `Item.ItemType` 사용
- 접촉 데미지에 `IsEdgeContact` 조건을 **다시 켜지 않기** (접촉 중 데미지가 끊기는 원인이었음)
- `GrowRange`를 무분별하게 바꾸지 않기 — 이동 범위 버그와 연관됨

### 수정 시 자주 보는 파일

| 작업 | 파일 |
|------|------|
| 이동·방호복·사망 | `Assets/Scripts/GamePlay/Player.cs` |
| 접촉·오염원 HP·판정 로그 | `Assets/Scripts/GamePlay/Pollutant.cs` |
| 아이템 DPS·타입 | `Assets/Scripts/GamePlay/Item.cs` |
| Z키 아이템 선택 | `Assets/Scripts/Core/ItemSelectManager.cs` |
| 오염원 스폰·경고 | `Assets/Scripts/GamePlay/PollutantManager.cs` |
| 시작 가이드·이동 잠금 | `Assets/Scripts/UI/GuideTxt.cs` |
| 씬 전환 | `Assets/Scripts/Core/SceneLoadManager.cs` |

---

## 3. 왕초보 개발자용 — 이 프로젝트 읽는 법

### 3.1 먼저 읽을 순서

1. [README.md](README.md) — 전체 그림
2. `Player.cs` — 이동·방호복
3. `Pollutant.cs` — 접촉 시 일어나는 일 (가장 중요)
4. `Item.cs` + `ItemSelectManager.cs` — 아이템 종류와 선택
5. `PollutantManager.cs` — 오염원이 언제 나타나는지

### 3.2 Unity에서 꼭 아는 위치

| Hierarchy에서 찾을 이름 | 역할 |
|------------------------|------|
| Player | 플레이어, 방호복, 이동 |
| PollutantManager | 오염원 생성·경고 |
| PollutantSpawner | 오염원 생성 위치 |
| (Canvas) GuideTxt | 시작 안내, 끝나면 이동 가능 |
| ItemSelectManager | 아이템 선택 |

**Play 누르기 전:** `GameScene`이 열려 있는지 확인.

### 3.3 자주 쓰는 용어 (초보용)

| 용어 | 쉬운 설명 |
|------|----------|
| **프리팹 (Prefab)** | 복사해 두었다가 씬에 찍어내는 설계도 (오염원 A/B/C 등) |
| **씬 (Scene)** | 게임 한 판이 돌아가는 맵 파일 |
| **Collider / Trigger** | 부딪힘 감지. `Is Trigger`면 통과하지만 `OnTriggerStay` 호출 |
| **DPS** | 초당 데미지. `데미지 = DPS × Time.deltaTime` |
| **SerializeField / public** | 인스펙터에서 숫자 바꿔 보는 변수 |
| **코루틴** | 시간 지나면서 순서대로 실행 (경고 → 스폰 → 페이드) |

### 3.4 플레이 테스트 체크리스트

- [ ] 가이드 끝난 뒤 좌우 이동 되는가?
- [ ] 2~3초 이동 후 `[경고]` 로그·오염원 생성되는가?
- [ ] 접촉 시 `틀린/올바른 아이템` 로그가 먼저 나오는가?
- [ ] 접촉 중 `[Player] 방호복 HP 감소`가 반복되는가?
- [ ] **맞는 아이템**일 때만 `[Pollutant] 오염원 HP`가 실제로 줄어드는가?
- [ ] 떨어지면 `접촉 해제 -> HP 초기화` 로그가 나오는가?

**Console이 멈춘 것처럼 보이면:** `Collapse` 끄기, Clear 후 다시 Play.

---

## 4. AGENTS.md에 넣으면 좋은 내용 (추천 목록)

왕초보인 본인을 위해, 아래를 **시간 날 때마다** 채워 넣으면 도움이 됩니다.

### 이미 이 파일에 넣은 것

- [x] 프로젝트 한 줄 요약
- [x] AI가 지켜야 할 규칙 / 금지 사항
- [x] 핵심 파일 맵
- [x] 읽는 순서·용어집·테스트 체크리스트

### 추가하면 더 좋은 것 (직접 채우기)

| 항목 | 예시 | 왜 좋은가 |
|------|------|----------|
| **팀 연락처 / 역할** | "기획 OOO, 아트 OOO" | AI가 임의로 기획 바꾸지 않게 |
| **브랜치 규칙** | `main`에 직접 push 안 함 | 실수 방지 |
| **인스펙터에서 꼭 연결할 참조** | Player → protectionNumText | "NullReference" 디버깅 시간 절약 |
| **알려진 버그** | "이동이 -403 전에 멈춤 → GrowRange 확인" | 같은 실수 반복 방지 |
| **다음 할 일 (TODO)** | "Die 애니 다시 넣기" | AI에게 작업 지시할 때 명확 |
| **스크린샷 / GIF 링크** | 정상 플레이 영상 | 말로 설명하기 어려울 때 |
| **빌드 방법** | Windows 빌드 설정 | 나중에 배포할 때 |
| **자주 나는 에러와 해결** | "Animator에 State 파라미터 없음" | 초보가 검색하기 전에 해결 |

### Cursor Rules (이미 생성됨)

| 파일 | 적용 |
|------|------|
| `.cursor/rules/midproject-core.mdc` | **항상** (단순 코드, 기존 스타일, 커밋 금지 등) |
| `.cursor/rules/unity-gameplay.mdc` | `Assets/Scripts/**/*.cs` 열 때 (HP, 접촉, Pollutant) |

Cursor에서 확인: **Settings → Rules** 또는 채팅 입력창 근처 Rules 목록.

이 `AGENTS.md`는 Rules보다 긴 설명·초보자 가이드용입니다.

---

## 5. AI에게 요청할 때 쓰기 좋은 문장 예시

초보일수록 **파일 이름 + 원하는 동작 + 재현 방법**을 같이 주면 결과가 좋습니다.

```text
좋은 예:
"Pollutant.cs에서 접촉 중 틀린 아이템일 때는 [Pollutant] HP 로그를 생략해줘.
 Scanner로 GeneralPad 오염원에 닿았을 때 로그가 너무 많이 나와."

나쁜 예:
"로그 고쳐줘"
```

```text
좋은 예:
"Player.cs만 수정해줘. 방호복 0이 되면 Destroy 말고 2초 대기 후 Destroy.
 Die 애니는 아직 넣지 마."

나쁜 예:
"사망 연출 예쁘게 해줘"  ← 범위가 너무 큼
```

---

## 6. 현재 구현 상태 (AI·개발자 공통 기준)

| 기능 | 상태 |
|------|------|
| 이동 (-785 ~ -403) | ✅ |
| 오염원 경고·스폰·페이드 | ✅ |
| 아이템 선택 (Z) | ✅ |
| 접촉 시 방호복 감소 | ✅ |
| 정답 아이템만 오염원 HP 감소 | ✅ |
| 접촉 해제 시 오염원 HP 초기화 | ✅ |
| 판정 로그 (올바른/틀린) | ✅ |
| Die 애니 + 페이드 사망 | ❌ 롤백됨 — 요청 시에만 작업 |
| GameManager / StageManager 게임 흐름 | ❌ 골격만 |

---

## 7. 변경 이력 (수동으로 적기)

| 날짜 | 내용 |
|------|------|
| 2026-05-28 | 접촉·중화·판정 로그 시스템, Item.ItemType 구조, README/AGENTS.md 작성 |
| | *(이 아래에 본인이 직접 추가)* |

---

*이 파일은 팀과 AI가 같은 기준으로 작업하기 위한 문서입니다. 막히면 README → 이 파일 → 해당 `.cs` 순으로 보면 됩니다.*
