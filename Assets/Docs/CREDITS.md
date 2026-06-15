# HazmatRush — 에셋 출처·라이선스

| **작성** | 2026-06-15 |
| **프로젝트** | HazmatRush (해즈멧러쉬) · Unity `6000.4.8f1` |
| **용도** | 중간 팀프로젝트 P5(에셋 출처·라이선스) · 제출·발표 참고 |

본 문서는 **실제 빌드·씬에서 사용 중인** 외부 에셋과, 프로젝트에 포함만 되어 있던 패키지 정리 이력을 함께 기록합니다.

---

## 1. 폰트

| 에셋 | 경로 | 출처 | 라이선스 |
|------|------|------|----------|
| **Galmuri11 / GalmuriMono11** | `Assets/Fonts/` | [Galmuri](https://github.com/quiple/galmuri) · Minseo Lee | © 2019–2023 Minseo Lee · 프로젝트 README 및 Galmuri 배포 조건 준수 |
| **LiberationSans (TMP 기본)** | `Assets/TextMesh Pro/Fonts/` | Google / TMP | [SIL Open Font License 1.1](Assets/TextMesh%20Pro/Fonts/LiberationSans%20-%20OFL.txt) |
| **Tilt Warp (Layer Lab 팩)** | `Assets/UI/Layer Lab/GUI-BasicButtonPack/Font/` | Layer Lab | [SIL OFL 1.1](Assets/UI/Layer%20Lab/GUI-BasicButtonPack/Font/TiltWarp%20-%20OFL.txt) |

---

## 2. UI · 2D 그래픽

| 에셋 | 경로 | 용도 | 출처·라이선스 |
|------|------|------|----------------|
| **GUI-BasicButtonPack** | `Assets/UI/Layer Lab/GUI-BasicButtonPack/` | 버튼 PNG·데모 (일부 UI 참고) | Layer Lab · Asset Store 패키지 · 폰트 OFL |
| **HudIconSheet_StarArrowSkip** | `Assets/UI/HudIconSheet_StarArrowSkip.png` | 별·화살표·스킵 아이콘 스프라이트 시트 | 팀 제작·편집 (구 `00.png`) |
| **HudIconSheet_PanelBg** | `Assets/UI/HudIconSheet_PanelBg.png` | HUD 패널 배경 스프라이트 시트 | 팀 제작·편집 (구 `All.png`) |
| **캐릭터·배경·아이템 아이콘 등** | `Assets/UI/Character/`, `Bg/`, `Items/`, `Pollutant/`, `Logo/` | 게임 HUD·오염원·로고 | 팀 제작·AI 보조 이미지 (ChatGPT/Gemini 등) · 교육용 프로젝트 |
| **HazmatRush 아이콘** | `Assets/UI/Icon/HazmatRush_Ico.png` | exe·Player Settings 아이콘 | 팀 제작 |

---

## 3. VFX (파티클)

| 에셋 | 경로 | 게임 내 사용 | 출처·라이선스 |
|------|------|--------------|----------------|
| **Cartoon FX Remaster FREE** | `Assets/Effect/JMO Assets/Cartoon FX Remaster/` | **사용 중** — `CFXR Electrified 3`(플레이어 중화 VFX), `PollutantD` 연기 등 | © Jean Moreno · Unity Asset Store [Standard EULA](https://unity.com/legal/as-terms) (상업·비상업 게임 내 사용 가능) · Readme: `Assets/Effect/JMO Assets/Cartoon FX Remaster/Readme Cartoon FX Remaster FREE.html` |
| **CFXR Electrified 3** (프리팹) | `Assets/Prefabs/Effect/CFXR Electrified 3.prefab` | `Player` 중화 VFX 자식 | 위 CFXR FREE 패키지 |

**Kino Bloom** (CFXR 데모용): `Assets/Effect/JMO Assets/Cartoon FX Remaster/Demo Assets/Kino Bloom/` — [MIT License](Assets/Effect/JMO%20Assets/Cartoon%20FX%20Remaster/Demo%20Assets/Kino%20Bloom/Kino%20Bloom%20License.txt)

---

## 4. 오디오

| 파일 | 경로 | 용도 | 출처·라이선스 |
|------|------|------|----------------|
| **TitleTrack.ogg** | `Assets/Audio/BGM/` | 타이틀 BGM | SUNO AI 생성 · 교육 프로젝트 자체 사용 |
| **Exploration Mode_1.ogg**, **Exploration Mode_2.mp3** | `Assets/Audio/BGM/` | 스테이지 탐사 BGM | SUNO AI 생성 |
| **Neutralization_1.mp3**, **Neutralization_2.mp3** | `Assets/Audio/BGM/` | (예비/미사용 가능) | SUNO AI 생성 |
| **splashSFX.ogg** | `Assets/Audio/SFX/` | 스플래시 | [freesound.org](https://freesound.org) |
| **selectionSFX.ogg** | `Assets/Audio/SFX/` | UI 선택 | freesound.org |
| **clearSFX.ogg** | `Assets/Audio/SFX/` | 클리어 | freesound.org |
| **game-overSFX.ogg** | `Assets/Audio/SFX/` | 게임오버 | freesound.org |
| **countdownSFX.ogg** | `Assets/Audio/SFX/` | 타이머 경고(16초) | freesound.org |
| **neutralizationSFX.ogg** | `Assets/Audio/SFX/` | A~C 중화 루프 | freesound.org |
| **SqueakyValve.ogg** | `Assets/Audio/SFX/` | 가스 밸브 | freesound.org |
| **dieSplattSFX.ogg** | `Assets/Audio/SFX/` | 사망 1회 | freesound.org |
| **시작 버튼 SFX** (타이틀) | AudioManager 연결 클립 | 시작·UI | [BoostSound (YouTube)](https://www.youtube.com/watch?v=YNSbL-Cek1c) · 출처 표기 |

> freesound 개별 클립 ID는 Unity Inspector·`.ogg.meta`의 `assetBundleName` 등에 기록되지 않은 경우가 있어, **제출 시 freesound 계정·다운로드 이력**으로 CC0/CC-BY 준수 여부를 팀 내부 확인합니다.

---

## 5. Unity · 기타

| 항목 | 비고 |
|------|------|
| **Unity Engine** | Unity 6000.4.8f1 · Unity Technologies |
| **TextMesh Pro** | Unity 패키지 · UGUI 텍스트 |
| **URP / Input System** | `Assets/Settings/`, Package Manager |
| **UIImageOutline / SpriteOutline** | `Assets/Shaders/` · 프로젝트 자체 셰이더 |

---

## 6. 제거된 미사용 패키지 (2026-06-15)

아래는 **씬·프리팹·스크립트 GUID 참조 0건** 확인 후 저장소에서 삭제했습니다. (게임 동작에 영향 없음)

| 패키지 | 원 경로 | 삭제 사유 |
|--------|---------|-----------|
| loadingBar | `Assets/loadingBar/` | 프로젝트 미참조 · 샘플 전용 |
| Hovl Studio Magic effects pack | `Assets/Effect/Hovl Studio/` | 미참조 |
| Matthew Guz Hits Effects FREE | `Assets/Effect/Matthew Guz/` | 미참조 |
| Loading Effect | `Assets/Effect/Loading Effect/` | 미참조 (자체 LoadingController 사용) |
| CFXR orphan 프리팹 6종 | `Assets/Prefabs/Effect/` (Electrified 3 **제외**) | GUID 참조 0 · Electrified 3만 Player 사용 |
| JMO legacy unitypackage 메타 | `Assets/Effect/JMO Assets/JMO Assets old legacy/` | 미추출 잔여 |

**유지:** `Cartoon FX Remaster` 본체 — 셰이더·머티리얼·`CFXR Electrified 3` 의존.

---

## 7. 폴더·네이밍 규칙 (자체 자산)

```text
Assets/
├── Scripts/Core|GamePlay|UI/     ← C# 역할별
├── Prefabs/Game|Item|InvenItem|Effect|Bg/
├── Data/                         ← JSON
├── Scenes/*Scene.unity
├── UI/HudIconSheet_*.png         ← HUD 스프라이트 시트 (GUID 유지 rename)
├── Docs/CREDITS.md               ← 이 파일
└── Effect/JMO Assets/            ← 외부 VFX (사용분만 유지)
```

- **`Prefabs/Item/`** — 중화·맵 드랍 아이템  
- **`Prefabs/InvenItem/`** — 회복 인벤 UI (`inv*`)

---

## 8. 관련 문서

- [README.md](../../README.md) — 프로젝트 개요
- [Git workflow](2026-06-08-시각미상-git-workflow-worktree.md)
- CFXR Readme: `Assets/Effect/JMO Assets/Cartoon FX Remaster/Readme Cartoon FX Remaster FREE.html`
