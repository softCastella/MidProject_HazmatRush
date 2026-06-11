# PollutantArrow(오염원 화살표) — Unity 설정 가이드 (초보용)

오염원 A~C가 등장할 때 **1초간** 화살표가 오염원 위를 가리키는 기능입니다.

> **씬에 오브젝트를 둘 필요 없습니다.**  
> `Assets/Prefabs/Game/PollutantArrow.prefab` 을 쓰고, Play 시 `HUD_Canvas` 아래에 **자동 생성**됩니다.

---

## 1. 한 줄 요약

| 항목 | 내용 |
|------|------|
| **표시 대상** | 오염원 **A·B·C** 등장 시만 (가스 D는 제외) |
| **표시 시간** | 기본 **1초** |
| **내가 할 일** | 대부분 **없음**. 화살표 높이만 맞추고 싶을 때 `World Offset Y` 숫자만 바꾸면 됨 |
| **테스트 스테이지** | **1-1은 D만** 나와서 화살표 안 보임 → **1-2 이상**에서 확인 |

---

## 2. 프리팹 · Hierarchy

**프리팹 위치:** `Assets/Prefabs/Game/PollutantArrow.prefab`

**Play 전** Hierarchy에는 없어도 됩니다. **Play 후** A~C 오염원 등장 시 아래처럼 생깁니다:

```text
HUD_Canvas
└── PollutantArrow   ← 런타임 Instantiate (PollutantManager가 생성)
```

높이·시간을 바꾸려면 **프리팹**을 열어 `Pollutant Arrow UI` 값을 수정하세요.

---

## 3. PollutantArrow에 이미 달려 있는 것

`PollutantArrow`를 선택했을 때 Inspector에 아래가 있어야 합니다.

| 컴포넌트 | 역할 | 직접 건드릴까? |
|----------|------|----------------|
| **Rect Transform** | UI 위치·크기 | 보통 안 함 |
| **Image** | 화살표 그림 | 아트 바꿀 때만 |
| **Animator** | `PollutantArrow` 애니 (살짝 튀는 연출) | 이미 연결됨 |
| **World Space UI Follower** | **HP바와 동일** — 월드 오염원 → 화면 좌표 추적 | `worldOffset` Y (기본 120) |
| **Pollutant Arrow UI** (`PollutantArrowUI.cs`) | 1초 후 숨김·추적 대상 연결 | **World Offset Y**·**Show Duration** |

### Pollutant Arrow UI — 바꿔도 되는 값

| 필드 | 기본값 | 설명 |
|------|--------|------|
| **World Offset Y** | `120` | HP바와 같은 단위. 오염원 **머리 위**로 얼마나 올릴지 (클수록 위) |
| **Show Duration** | `1` | 화살표가 보이는 초 |

---

## 4. PollutantManager 연결

1. Hierarchy에서 **`PollutantManager`** 선택
2. Inspector **`Pollutant Arrow Prefab`** 슬롯에  
   `Assets/Prefabs/Game/PollutantArrow` 프리팹이 들어가 있으면 정상
3. Play 시 프리팹이 `HUD_Canvas` 아래에 **한 번 생성**됨 (씬에 미리 둘 필요 없음)

코드는 오염원 A~C가 나올 때 자동으로 `PollutantArrow.ShowAt(...)` 을 호출합니다. **Play 중에 버튼 누를 일 없음.**

---

## 5. Play로 확인하는 순서

1. `GameScene` 열기
2. 상단 **▶ Play** 클릭
3. 가이드 끝난 뒤 이동 → 경고 → 오염원 등장
4. **A/B/C** 오염원 위에 화살표가 **약 1초** 보이는지 확인

| 스테이지 | 오염원 | 화살표 |
|----------|--------|--------|
| 1-1 | D(가스)만 | **안 나옴** (정상) |
| 1-2 | A, B | **나와야 함** |
| 1-3 | A, B, C, D | A/B/C 등장 시만 |

화살표가 너무 위/아래면: **Play 중지** → `HUD_Canvas/PollutantArrow` 선택 → **World Offset Y** 변경 → 다시 Play.

---

## 6. 슬롯이 비었을 때만 (복구)

`PollutantManager`의 **Pollutant Arrow**가 `None` 이면:

1. Hierarchy에서 `HUD_Canvas` → `PollutantArrow` 선택
2. Inspector 맨 아래 **Add Component** 클릭
3. 검색창에 `Pollutant Arrow UI` 입력 → 추가  
   (이미 있으면 이 단계 생략)
4. Hierarchy에서 `PollutantManager` 선택
5. `PollutantArrow` 오브젝트를 Inspector의 **Pollutant Arrow** 슬롯으로 **드래그**

---

## 7. 구조 그림 (개발자·AI용)

```text
오염원 A~C 등장 (PollutantManager)
        │
        ▼
PollutantArrowUI.ShowAt(오염원 Transform)
        │
        ├─ 월드 좌표 → HUD_Canvas 좌표로 변환 (매 프레임)
        ├─ Animator: 바운스 연출 (위치는 추적 좌표 + 애니 편차)
        └─ 1초 후 Hide()
```

관련 스크립트:

- `Assets/Scripts/UI/PollutantArrowUI.cs` — 화살표 표시·추적
- `Assets/Scripts/GamePlay/PollutantManager.cs` — A~C 등장 시 호출
- `Assets/Animations/PollutantArrow.anim` — 연출 클립

---

## 8. 자주 하는 질문

**Q. 화살표가 전혀 안 보여요.**  
- 1-1만 플레이했는지 확인 (D만 있으면 의도적으로 안 나옴)  
- `PollutantManager` → `Pollutant Arrow` 슬롯 연결 확인 (§6)  
- `PollutantArrow` 오브젝트에 `Pollutant Arrow UI` 컴포넌트 있는지 확인

**Q. 화살표가 오염원이 아닌 엉뚱한 곳에 있어요.**  
- `World Offset Y` 조절 (§3)  
- `PollutantArrow`가 **HUD_Canvas 자식**인지 확인

**Q. 컴포넌트를 어디에 붙이나요?**  
- **`PollutantArrow` 오브젝트 하나에만** `Pollutant Arrow UI`  
- `PollutantManager`에는 **참조(슬롯)** 만 연결 — 스크립트를 Manager에 붙이는 게 아님
