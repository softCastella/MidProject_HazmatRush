# Git 워크플로 — 2026-06-08 시각미상 — Worktree 기반

| 항목 | 내용 |
|------|------|
| **작성** | 2026-06-08 · 시각 미상 |
| **저장소** | `https://github.com/softCastella/MidProject_HazmatRush.git` |
| **기본 브랜치** | `master` |
| **관련** | [Bug 2026-06-05 클리어·가이드 (§6 브랜치 어긋남)](../Bug/2026-06-05-시각미상-클리어-가이드-중화VFX-fixes.md) |

Unity 프로젝트에서 **브랜치 전환 없이** 여러 작업을 병렬로 하기 위해 **git worktree**를 쓰는 규칙입니다.

---

## 1. 한 줄 요약

```text
master = 항상 머지 가능한 기준
기능 = master 최신에서 분기 → 전용 폴더(worktree)에서 작업 → PR → master 머지 → worktree 삭제
```

**금지:** 오래된 브랜치에 커밋만 쌓기 · `master`에 직접 push · worktree 두 개를 동시에 Unity Play.

---

## 2. 브랜치 전략 (단순 Git Flow)

| 브랜치 | 역할 | 수명 |
|--------|------|------|
| `master` | 배포·통합 기준. 항상 빌드 가능 목표 | 영구 |
| `2606XX_기능명` | 기능·버그·문서 단위 작업 | PR 머지 후 삭제 권장 |

### 브랜치 이름 규칙

```text
YYMMDD_짧은-설명
```

예: `260608_버그수정완료v`, `260609_회복스폰`, `260610_스테이지2`

- 날짜 + 한글/영문 짧은 설명 (기존 저장소 관례 유지)
- 한 브랜치 = **한 가지 목적** (회복 아이템 + 씬 전환을 한 PR에 넣지 않기)

### 커밋

- 메시지: `fix:` / `feat:` / `docs:` 접두 + 한글 한 줄 요약
- `Library/`, `Temp/`, `Logs/` 커밋 금지

---

## 3. Worktree 폴더 규칙

메인 클론과 **형제 폴더**에 worktree를 둡니다.

```text
Documents/Workspace/
├── MidProject_0/              ← 메인 worktree (보통 master 또는 안정 브랜치)
├── MidProject_0-260609-회복/  ← 기능 worktree (브랜치 260609_회복)
└── MidProject_0-260610-UI/    ← 다른 기능 worktree
```

| 규칙 | 이유 |
|------|------|
| 폴더명 `MidProject_0-{브랜치약칭}` | 어떤 작업인지 한눈에 |
| worktree **1개 = Unity 프로젝트 1개** | `Library/`·씬·메타 충돌 방지 |
| **동시에 Play는 worktree 1곳만** | 같은 프로젝트 두 에디터 금지 |
| worktree마다 Unity로 **한 번씩** 열기 | `Library/` 자동 생성 (Git 제외) |

---

## 4. 작업 주문 (매 기능마다 이 순서)

### 4.1 시작 전 (메인 폴더 `MidProject_0`)

```powershell
cd C:\Users\lanoc\Documents\Workspace\MidProject_0
git fetch origin
git checkout master
git pull origin master
```

### 4.2 새 기능 worktree 만들기

`260609_회복스폰` 예시 — **브랜치 이름과 폴더명을 본인 작업에 맞게 바꿉니다.**

```powershell
git worktree add ..\MidProject_0-260609-회복 -b 260609_회복스폰 origin/master
```

이미 원격에 브랜치가 있으면:

```powershell
git worktree add ..\MidProject_0-260609-회복 260609_회복스폰
```

### 4.3 기능 작업 (worktree 폴더에서만)

```powershell
cd C:\Users\lanoc\Documents\Workspace\MidProject_0-260609-회복
```

1. Unity Hub → **이 폴더**를 프로젝트로 연다
2. 코드·씬·문서 수정
3. Play 테스트
4. 커밋 (사용자가 명시적으로 요청할 때)

```powershell
git add (관련 파일만)
git commit -m "fix: 회복 스폰 로직 연결"
```

### 4.4 PR 전 점검

```powershell
git fetch origin
git log origin/master..HEAD --oneline
```

- `0 behind` 확인 (뒤처지면 아래 4.5 rebase)
- PR base: **`master`**

### 4.5 master가 앞서 갔을 때 (rebase)

```powershell
git fetch origin
git rebase origin/master
# 충돌 시 파일 수정 → git add → git rebase --continue
git push -u origin HEAD
```

rebase가 너무 꼬이면: [Bug §6](../Bug/2026-06-05-시각미상-클리어-가이드-중화VFX-fixes.md) 참고 — `master` 기준 새 브랜치 + 변경분만 옮기기.

### 4.6 PR 머지 후 정리

```powershell
cd C:\Users\lanoc\Documents\Workspace\MidProject_0
git checkout master
git pull origin master
git worktree remove ..\MidProject_0-260609-회복
git branch -d 260609_회복스폰
git push origin --delete 260609_회복스폰
```

마지막 `push --delete`는 원격 브랜치 정리용 (팀 합의 후).

---

## 5. 상황별 치트시트

| 하고 싶은 일 | 명령 (메인 repo에서) |
|--------------|----------------------|
| worktree 목록 | `git worktree list` |
| 새 기능 시작 | `git worktree add ..\MidProject_0-이름 -b 브랜치명 origin/master` |
| worktree 제거 | `git worktree remove ..\MidProject_0-이름` |
| master 최신 반영 (메인만) | `git checkout master; git pull origin master` |
| 현재 브랜치가 master 대비 | `git fetch origin; git status` |

---

## 6. Unity 주의

- **커밋 대상:** `Assets/`, `ProjectSettings/`, `Packages/` 등
- **커밋 금지:** `Library/`, `Temp/`, `Logs/`, `UserSettings/` (개인 설정은 선택)
- worktree 추가 후 **첫 Open은 Import 시간** 걸림 — 정상
- `.cs` 추가·삭제 시 `.meta` 함께 커밋
- 씬·프리팹 충돌 나면 **한 worktree에서만** 해결 후 커밋

---

## 7. AI(Cursor) 협업 시

- Git 명령은 **사용자가 요청할 때만** 실행 ([AGENTS.md](../../AGENTS.md))
- AI에게 줄 때: **worktree 경로 + 브랜치명 + 작업 목적**을 한 줄로 명시

```text
예: "C:\...\MidProject_0-260610-가스사망 의 260610_가스사망 브랜치에서 Player.cs 밸브 애니 루프만 수정해줘"
```

---

## 8. 현재 저장소 기준 (2026-06-08 스냅샷)

| 항목 | 값 |
|------|-----|
| 메인 작업 브랜치 | `260608_버그수정완료v` |
| 최근 커밋 | 회복 아이템 아크 드랍·문서 |
| 원격 default | `origin/master` |

**다음 기능부터:** 위 4.1~4.2로 **새 worktree**를 만들고, 메인 `MidProject_0`는 `master` pull 후 대기 또는 문서만 보는 용도로 두는 것을 권장합니다.

---

*2026-06-08 작성*
