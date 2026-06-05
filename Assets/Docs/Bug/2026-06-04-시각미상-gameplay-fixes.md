# 2026-06-04 시각미상 — 게임플레이 이슈 정리 (원인 · 해결)

| **작성** | 2026-06-04 · **시각 미상** (파일명 규칙: [문서-이름-규칙.md](../문서-이름-규칙.md)) |

오늘 세션에서 보고·수정한 문제들입니다. 관련 스크립트는 `Assets/Scripts/` 기준입니다.

---

## 1. 오염원 등장 후 1차 이동 구간에서 오른쪽으로 못 감

| 항목 | 내용 |
|------|------|
| **증상** | 경고 후 오염원이 생기면 1차 이동 끝(-403) 근처에서 더 이상 오른쪽으로 가지 못함. 가스(D) 쪽으로도 접근 불가. |
| **원인** | `Player.BlockPollutant()`가 `rightLimit = Min(rightLimit, 오염원 왼쪽)`으로 **이동 한계만 줄임**. 오염원이 -403보다 오른쪽(스폰 x≈42 등)에 있어도 범위가 넓어지지 않음. 예전 `GrowRange`는 멀리 있는 오염원 쪽으로 범위를 **확장**했음. |
| **해결** | `BlockPollutant` → `GrowRange`만 사용(접근용). 통과 방지는 `Update`의 `ClampPassThroughPollutants`로 분리. |
| **파일** | `Player.cs`, `PollutantManager.cs` |

---

## 2. 가스 HP가 17 근처에서 멈춤

| 항목 | 내용 |
|------|------|
| **증상** | 가스(D) HP가 24에서 약 17까지만 줄고 더 이상 감소하지 않음. (가스 최대 HP 24 기준) |
| **원인** | `ClampPassThrough` / `BlockPollutant`가 플레이어를 트리거 **밖**에 멈춤 → `OnTriggerStay` 끊김. 가스(D)는 `OnTriggerStay`만 쓰지 않고 `Update` 경로도 있었으나, 접촉 플래그·트리거 불안정. |
| **해결** | 통과 방지 로직 완화(아래 3번). 가스는 `bounds` 겹침 시 `Update`에서 `ApplyPlayerContactDamage` 유지. 잘못된 `OnTriggerExit`는 겹침이면 무시. |
| **파일** | `Player.cs`, `Pollutant.cs` |

---

## 3. 가스 제거 후 옆 오염원(A~C) 접촉 안 됨

| 항목 | 내용 |
|------|------|
| **증상** | 가스를 없앤 뒤 바로 옆 C 등 다른 오염원에 닿아도 중화·판정이 안 됨. |
| **원인** | `ClampPassThroughPollutants`가 오염원 **앞**(`blockLeft`)에서 플레이어를 막아, 트리거 **안으로 들어가지 못함** (`OnTriggerEnter` 미발생). |
| **해결** | 한 프레임에 구간 **전체를 가로지를 때만** 막도록 변경. 접촉하려고 들어가는 이동은 허용. 콜라이더 `enabled == false`인(사라지는) 오염원은 무시. |
| **파일** | `Player.cs` |

---

## 4. 가스 제거 후 플레이어가 반대 방향으로 밀려남

| 항목 | 내용 |
|------|------|
| **증상** | 가스 중화 직후 캐릭터가 왼쪽(시작 쪽)으로 슬라이드되는 느낌. |
| **원인** | `RefreshMoveRange` → `ResetRange()`로 `rightLimit`이 -403으로 돌아감. 플레이어는 가스 위치(-320대) 등 **-403보다 오른쪽**에 있는데, 입력 없이도 매 프레임 `Clamp`가 X를 -403으로 맞춤. |
| **해결** | `EnsureRangeIncludesPosition()`로 현재 X를 이동 한계에 포함. **이동 입력이 있을 때만** `Clamp` 적용(`h != 0`). |
| **파일** | `Player.cs`, `PollutantManager.cs` |

---

## 5. 밸브 애니 중 squeakyValve 소리 안 남

| 항목 | 내용 |
|------|------|
| **증상** | 가스밸브 연출(`Player_Valve`)은 나오는데 밸브 SFX가 안 들림. |
| **원인** | `Pollutant`의 `hasPlayedNeutralizationSfx` 플래그와 가스(D) 재생 경로가 맞지 않음. 중화음만 플래그를 켜고, 밸브는 `SetValveAnim`과 타이밍이 어긋남. |
| **해결** | `Player.SetValveAnimActive(true/false)`에서 `PlayValveSfx` / `StopValveSfx` 호출. 가스(D)는 `Pollutant`에서 중화 루프음 재생 제거. |
| **파일** | `Player.cs`, `Pollutant.cs`, `AudioManager.cs` |

---

## 6. 중화음은 너무 크고 밸브음은 너무 작음

| 항목 | 내용 |
|------|------|
| **증상** | A~C 중화 루프 SFX는 크고, 가스 밸브음은 거의 안 들림. |
| **원인** | 둘 다 `neutralizationSfxVolume` 하나만 사용. GameScene 기본값 0.1 등 동일 소스·동일 볼륨. |
| **해결** | `valveSfxVolume` 슬라이더 분리. 재생 시 클립별로 다른 볼륨 적용. (GameScene 예: 중화 0.08, 밸브 0.9) |
| **파일** | `AudioManager.cs`, `TitleScene.unity`, `GameScene.unity` |

---

## 7. Z / X 키 방향 (아이템 선택)

| 항목 | 내용 |
|------|------|
| **증상** | Z·X와 좌·우 방향키 의미가 기대와 반대. |
| **원인** | Z=`SelectNextItem`, X=`SelectPrevItem`로 매핑되어 있었음. |
| **해결** | **Z = 왼쪽(이전)**, **X = 오른쪽(다음)**. 안내 문구: `PollutantManager`, `LoadingGuideTxt` 동기화. |
| **파일** | `ItemSelectManager.cs`, `PollutantManager.cs`, `LoadingGuideTxt.cs` |

---

## 8. 중화·가스 다 안 하면 그 화면에서 못 나가야 함

| 항목 | 내용 |
|------|------|
| **증상** | C만 없애고 가스가 아직 **등장 전**(비활성 프리로드)이면 맵 전환이 됨. “이 구간 오염원 전부 처리”와 불일치. |
| **원인** | 맵 전환 조건이 `HasAnyActive()`만 확인. 비활성(`SetActive(false)`) 오염원은 `activeCount`에 안 잡힘. |
| **해결** | `CanLeaveCurrentSegment()`: 활성 없음 + 로딩 플랜 **미등장 큐 없음** + **남은 프리로드 오브젝트 없음**. 배경 스크롤도 동일 조건으로 정지/재개. |
| **파일** | `PollutantManager.cs` |

---

## 9. 클리어 별이 조건별 인덱스에 붙어 ★☆★ 형태

| 항목 | 내용 |
|------|------|
| **증상** | 오염원·정확 달성, 방호복만 미달 시 별이 **1번·3번만** 채워짐 (가운데 빈 별). |
| **원인** | `ClearPanelUI`가 `starClear[i]`를 슬롯 i에 직접 연결 (0=클리어, 1=안전, 2=정확). |
| **해결** | `result.starCount`만큼 **왼쪽부터 순서대로** 채움. 텍스트 `[달성]/[미달]` 줄은 조건별 그대로 유지. |
| **파일** | `ClearPanelUI.cs` |

---

## 10. (구조 개선) 로딩 시 오염원 미리 생성

| 항목 | 내용 |
|------|------|
| **배경** | 런타임 `Instantiate` + 이동 막기 + 트리거 꼬임으로 예외 상황이 많았음. |
| **해결** | `RecoveryItemSpawnPlan`과 같이 `PollutantSpawnPlan`으로 로딩 중 배치·순서 확정 → `PollutantManager`가 비활성으로 생성 후 경고 시 `SetActive`만. |
| **파일** | `PollutantSpawnPlan.cs`, `LoadingController.cs`, `PollutantManager.cs`, `PollutantSpawner.cs`, `Pollutant.cs` |
| **Unity** | LoadingScene `LoadingController`에 `stage_data.json` 연결 필요. |

---

## 빠른 참조 — 핵심 함수

| 함수 / 플래그 | 역할 |
|---------------|------|
| `Player.GrowRange` / `BlockPollutant` | 오염원 방향으로 이동 **한계 확장** |
| `Player.ClampPassThroughPollutants` | 오염원 **통과만** 방지 (접촉 진입 허용) |
| `PollutantManager.CanLeaveCurrentSegment` | 맵 전환·스크롤 재개 가능 여부 |
| `PollutantSpawnPlan` | 로딩 시 스테이지별 오염원 큐 |
| `AudioManager.neutralizationSfxVolume` / `valveSfxVolume` | 중화 vs 밸브 볼륨 |
| `ClearPanelUI` + `starCount` | 별 좌→우 순차 채움 |

---

## 테스트 체크리스트 (회귀)

- [ ] 가이드 후 이동 → 경고 → 오염원 방향으로 **접근 가능**
- [ ] 가스밸브 + 가스 접촉 → HP **0까지**, 밸브 SFX 들림
- [ ] 가스 제거 후 **옆 C 접촉**·중화 가능, **밀려남 없음**
- [ ] C·가스 **둘 다** 처리 전까지 맵 전환·스크롤 안 됨
- [ ] Z 왼쪽 / X 오른쪽 아이템 전환
- [ ] 클리어 시 별 **왼쪽부터 N개**만 채움 (가운데만 빈 ★☆★ 아님)

---

*작성: 2026-06-04 · MidProject 디버그 세션 기준 · 작성 시각 미상*
