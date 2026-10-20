#!/usr/bin/env bash
# 공개용 저장소(UrbanSurvival)에 올릴 사본을 만든다.
# 라이선스상 공개 저장소에 둘 수 없는 에셋(EXCLUDES)을 "모든 커밋의 기록에서" 지운 bare 복제본을 만들고 검증한다.
# 이 저장소(작업 폴더)와 GitHub는 건드리지 않는다. --push를 줄 때만 공개 저장소에 푸시한다(강제 푸시 없음).
#
# 사용:
#   tools/publish-clean-repo.sh           # 사본 생성 + 검증만 (푸시 안 함)
#   tools/publish-clean-repo.sh --push    # 검증 통과 후 PUSH_BRANCHES(기본: SideProject-Mobile)만 일반 푸시(거부되면 중단)
#                                          main은 올리지 않는다: 공개 main이 비공개 main과 분기돼 있어 AGENTS.md 절차대로 사본에서 병합 커밋을 만들어 따로 올린다
#
# 환경 변수: CLEAN_DIR(사본 위치, 기본 <작업폴더>/../Zombie_clean.git), PUBLIC_REMOTE(공개 저장소 주소),
#           PUSH_BRANCHES(--push로 올릴 브랜치, 공백 구분), ALLOW_UNSYNCED=1(로컬과 origin이 다른 브랜치가 있어도 계속)
#
# 커밋된 기록만 사용한다(커밋하지 않은 변경은 포함되지 않음). 기록 정리는 결정적(deterministic)이라
# 같은 커밋에서 다시 실행하면 같은 SHA가 나오고, 새 커밋이 위에 쌓이면 공개 저장소에는 일반 푸시(fast-forward)로 올라간다.
set -euo pipefail
# Git Bash(MSYS)가 'feature/x:.gitignore' 같은 인자를 경로 목록으로 바꿔 버리지 않게 한다(슬래시가 든 브랜치 이름)
export MSYS2_ARG_CONV_EXCL='*'

SRC="$(git rev-parse --show-toplevel)"
DEST="${CLEAN_DIR:-$SRC/../Zombie_clean.git}"
REMOTE_URL="${PUBLIC_REMOTE:-https://github.com/ssa25879/UrbanSurvival.git}"
EXCLUDES=("Assets/GUI PRO Kit - Simple Casual" "Assets/GUI PRO Kit - Simple Casual.meta")
# 공개본에서 가릴 작성자(2026-10-06 사용자 결정: 이름·주소 모두 가림). 커밋 작성자·커미터와 SCRUB_FILES 본문에서 바꾼다.
# 가릴 주소는 공개본에 남지 않도록 이 파일에 적지 않고, Git 밖의 로컬 파일 <.git>/publish-clean.env에서
# HIDE_EMAIL=<주소> 한 줄로 읽는다(환경 변수 HIDE_EMAIL이 있으면 그것을 쓴다). 다른 PC에서는 이 파일을 직접 만든다.
CONF="$(git rev-parse --absolute-git-dir)/publish-clean.env"
if [ -z "${HIDE_EMAIL:-}" ] && [ -f "$CONF" ]; then HIDE_EMAIL="$(sed -n 's/^HIDE_EMAIL=//p' "$CONF" | tr -d '\r' | head -1)"; fi
[ -n "${HIDE_EMAIL:-}" ] || { echo "가릴 주소가 없습니다. $CONF 에 'HIDE_EMAIL=<주소>'를 적거나 환경 변수로 주세요." >&2; exit 2; }
HIDE_TO_NAME="contributor"
HIDE_TO_EMAIL="contributor@noreply.invalid"
# 비공개 origin은 e038672에서 .gitignore의 GUI PRO 제외 규칙을 지우고 GUI PRO를 다시 추적한다.
# 공개본 clone 사용자가 GUI PRO를 임포트해 실수로 커밋하지 않도록, 이 커밋의 후손에서만 공개본 .gitignore에 규칙을 되살린다.
# (이미 공개된 커밋은 바뀌지 않아 SHA가 유지된다)
GITIGNORE_ANCHOR="e038672"
export GIGNORE_BLOCK=$'# Third-party asset not redistributed in this public repository (license; see docs/store/third-party-not-in-repo.md)\n/Assets/GUI PRO Kit - Simple Casual/\n/Assets/GUI PRO Kit - Simple Casual.meta\n'
# 본문에 HIDE_EMAIL이 적힌 적이 있는 파일(git log -S로 확인한 것만). 새로 생기면 여기에 추가한다
SCRUB_FILES=("docs/superpowers/2026-10-06-mobile-port-final-handoff.md" "tools/publish-clean-repo.sh")
# 2026-10-20에 추가한 가림(실명·타인 경로)은 이미 공개된 이력을 다시 쓰지 않도록 NEWSCRUB_ANCHOR의 "후손" 커밋에서만 적용한다.
# (앵커 이전 커밋의 내용이 바뀌면 SHA가 달라져 공개 저장소에 일반 푸시를 할 수 없다. 이미 공개된 부분은 그대로 남는다)
NEWSCRUB_FILES=("docs/superpowers/specs/2026-09-21-unused-apocalyptic-assets-design.md" "docs/superpowers/specs/2026-10-13-code-review-remediation-design.md" "docs/superpowers/plans/2026-10-12-code-review-roadmap.md" "ProjectSettings/FbxExportSettings.asset")
NEWSCRUB_ANCHOR="66607b6856b484895e5409f08c41df567ddf9042" # 공개 SideProject-Mobile 끝(8d71f87)에 해당하는 비공개 커밋
PUBLISHED_TIPS="8d71f874680b7c1a8579bdd663e6b7ffd96100ea f79596ebf8f0df986479ff3bab159a5827a03247" # 이미 공개된 SideProject-Mobile·SideProject 끝(사본에서 재현돼야 한다)
# 올릴 브랜치(기본은 SideProject-Mobile만). SideProject 계열은 새 가림의 앵커 후손이 아니라 실명·타인 경로가 미공개 커밋에 남아 있으므로, 올리려면 그 계열의 앵커를 먼저 정해야 한다
PUSH_BRANCHES="${PUSH_BRANCHES:-SideProject-Mobile}"
# 공개본 파일 본문에서 가릴 문자열(2026-10-20): 작성자 실명(스크립트 파일에 그대로 적지 않으려고 UTF-8 바이트로 만든다)과 타인의 로컬 경로.
# 해당 문자열은 SCRUB_FILES의 파일 본문에서만 바뀐다. 새 문서에 실명을 쓰면 아래 검증이 실패해 알려 준다
HIDE_NAME_KO="$(printf '\xec\x9c\xa4\xec\x9a\xb0\xec\x83\x81')"
HIDE_TO_NAME_KO="(author)"
HIDE_FOREIGN_PATH=""
HIDE_FOREIGN_USER="(user)"

LOGFILE="${TMPDIR:-/tmp}/publish-clean-filter.log"
IDX_SCRIPT="${TMPDIR:-/tmp}/publish-clean-index-filter.sh"

case "$DEST" in
  *_clean.git) ;;
  *) echo "CLEAN_DIR는 '_clean.git'으로 끝나야 합니다(실수로 다른 폴더를 지우지 않기 위함): $DEST" >&2; exit 2 ;;
esac

echo "== 0. 사전 확인 (전체 기록·모든 브랜치가 로컬에 있어야 한다)"
if [ "$(git -C "$SRC" rev-parse --is-shallow-repository)" = "true" ]; then
  echo "얕은(shallow) 저장소입니다. 기록이 잘려 있으면 SHA가 달라집니다. 'git fetch --unshallow' 후 다시 실행하세요." >&2; exit 3
fi
# 이 스크립트는 로컬 브랜치(refs/heads)만 사본에 넣는다. origin에만 있는 브랜치가 있으면 공개본에서 빠지므로 중단한다
missing=$(comm -13 \
  <(git -C "$SRC" for-each-ref --format='%(refname:short)' refs/heads | sort) \
  <(git -C "$SRC" for-each-ref --format='%(refname:short)' refs/remotes/origin | sed 's#^origin/##' | grep -v -e '^HEAD$' -e '^origin$' | sort) || true)
if [ -n "$missing" ] && [ "${ALLOW_PARTIAL:-0}" != "1" ]; then
  echo "origin에는 있지만 로컬에 없는 브랜치가 있습니다(공개본에서 빠집니다):" >&2
  echo "$missing" | sed 's/^/  - /' >&2
  echo "로컬에 만들려면: git branch --track <이름> origin/<이름>   (일부러 빼려면 ALLOW_PARTIAL=1)" >&2
  exit 3
fi

# 로컬 브랜치가 비공개 origin과 다르면 알린다(사본은 로컬 기준이라, 뒤처진 브랜치를 올리면 공개본이 최신이 아니다).
# --push일 때는 PUSH_BRANCHES가 origin과 다르면 중단한다(ALLOW_UNSYNCED=1로 무시)
unsynced=0
for b in $(git -C "$SRC" for-each-ref --format='%(refname:short)' refs/heads); do
  if git -C "$SRC" show-ref --verify --quiet "refs/remotes/origin/$b"; then
    counts=$(git -C "$SRC" rev-list --left-right --count "$b...origin/$b")
    if [ "$counts" != "0	0" ]; then
      echo "  주의: $b 는 origin과 다릅니다 (로컬 앞섬/뒤처짐: $counts)" >&2
      case " ${PUSH_BRANCHES:-SideProject-Mobile} " in *" $b "*) unsynced=1 ;; esac
    fi
  fi
done
if [ "$unsynced" = "1" ] && [ "${1:-}" = "--push" ] && [ "${ALLOW_UNSYNCED:-0}" != "1" ]; then
  echo "푸시 대상 브랜치가 비공개 origin과 다릅니다. 먼저 동기화(pull/push)하거나 ALLOW_UNSYNCED=1로 다시 실행하세요." >&2
  exit 3
fi

echo "== 1. 사본 생성: $DEST"
rm -rf "$DEST"
git clone --bare --no-hardlinks "$SRC" "$DEST" >/dev/null 2>&1
cd "$DEST"
git remote remove origin   # 실수로 원본에 푸시하지 않도록

echo "== 2. 기록에서 제외 경로 삭제, 작성자 가림 (git filter-branch, 시간이 걸린다)"
{
  printf 'git rm -r -q --cached --ignore-unmatch --'
  for p in "${EXCLUDES[@]}"; do printf ' %q' "$p"; done
  printf '\n'
  # 주소는 환경 변수로 넘긴다(perl 정규식에 직접 쓰면 @가 배열로 해석됨)
  perl_old='s/\Q$ENV{HIDE_EMAIL}\E/$ENV{HIDE_TO_EMAIL}/g'
  perl_new='s/\Q$ENV{HIDE_EMAIL}\E/$ENV{HIDE_TO_EMAIL}/g; s/\Q$ENV{HIDE_FOREIGN_PATH}\E//g; s/\Q$ENV{HIDE_FOREIGN_USER}\E/(user)/g; s/\Q$ENV{HIDE_NAME_KO}\E/$ENV{HIDE_TO_NAME_KO}/g'
  # 앵커의 후손(앵커 자신은 제외)이면 NEWSCRUB=1
  printf 'NEWSCRUB=0; if [ "$GIT_COMMIT" != %q ] && git merge-base --is-ancestor %q "$GIT_COMMIT" 2>/dev/null; then NEWSCRUB=1; fi\n' "$NEWSCRUB_ANCHOR" "$NEWSCRUB_ANCHOR"
  for f in "${SCRUB_FILES[@]}"; do
    # 파일이 있는 커밋에서만 본문을 바꾼다(바이트 그대로 처리, 줄바꿈 변환 없음). 앵커 이전은 예전과 같은 식(주소만)을 써서 SHA를 유지한다
    printf 'e=$(git ls-files -s -- %q)\n' "$f"
    printf 'if [ -n "$e" ]; then m=${e%%%% *}; s=$(echo "$e" | cut -d" " -f2); x=%q; if [ "$NEWSCRUB" = 1 ]; then x=%q; fi; n=$(git cat-file blob "$s" | perl -pe "$x" | git hash-object -w --stdin); git update-index --cacheinfo "$m,$n,"%q; fi\n' \
      "$perl_old" "$perl_new" "$f"
  done
  for f in "${NEWSCRUB_FILES[@]}"; do
    printf 'e=$(git ls-files -s -- %q)\n' "$f"
    printf 'if [ "$NEWSCRUB" = 1 ] && [ -n "$e" ]; then m=${e%%%% *}; s=$(echo "$e" | cut -d" " -f2); n=$(git cat-file blob "$s" | perl -pe %q | git hash-object -w --stdin); git update-index --cacheinfo "$m,$n,"%q; fi\n' \
      "$perl_new" "$f"
  done
  # GITIGNORE_ANCHOR의 후손이고 .gitignore에 GUI PRO 규칙이 없으면 끝에 덧붙인다
  printf 'if git merge-base --is-ancestor %q "$GIT_COMMIT" 2>/dev/null; then e=$(git ls-files -s -- .gitignore); if [ -n "$e" ]; then m=${e%%%% *}; s=$(echo "$e" | cut -d" " -f2); if ! git cat-file blob "$s" | grep -qF "/Assets/GUI PRO Kit - Simple Casual/"; then n=$({ git cat-file blob "$s"; printf %%s "$GIGNORE_BLOCK"; } | git hash-object -w --stdin); git update-index --cacheinfo "$m,$n,.gitignore"; fi; fi; fi\n' "$GITIGNORE_ANCHOR"
} >"$IDX_SCRIPT"
export FILTER_BRANCH_SQUELCH_WARNING=1 HIDE_EMAIL HIDE_TO_NAME HIDE_TO_EMAIL HIDE_NAME_KO HIDE_TO_NAME_KO HIDE_FOREIGN_PATH HIDE_FOREIGN_USER
git filter-branch -f --prune-empty --tag-name-filter cat \
  --env-filter '
    if [ "$GIT_AUTHOR_EMAIL" = "$HIDE_EMAIL" ]; then GIT_AUTHOR_NAME="$HIDE_TO_NAME"; GIT_AUTHOR_EMAIL="$HIDE_TO_EMAIL"; fi
    if [ "$GIT_COMMITTER_EMAIL" = "$HIDE_EMAIL" ]; then GIT_COMMITTER_NAME="$HIDE_TO_NAME"; GIT_COMMITTER_EMAIL="$HIDE_TO_EMAIL"; fi
    export GIT_AUTHOR_NAME GIT_AUTHOR_EMAIL GIT_COMMITTER_NAME GIT_COMMITTER_EMAIL' \
  --index-filter ". '$IDX_SCRIPT'" -- --branches >"$LOGFILE" 2>&1 \
  || { echo "filter-branch 실패: $LOGFILE 확인" >&2; exit 1; }

echo "== 3. 이전 기록 잔여물 제거"
for r in $(git for-each-ref --format='%(refname)' refs/original); do git update-ref -d "$r"; done
git reflog expire --expire=now --all
git gc --prune=now -q

echo "== 4. 검증"
fail=0
for p in "${EXCLUDES[@]}"; do
  n=$(git log --branches --name-only --format='' -- "$p" | wc -l)
  [ "$n" -eq 0 ] || { echo "실패: 기록에 '$p'가 남아 있음($n)" >&2; fail=1; }
done
objects_list=$(git rev-list --objects --branches)
for p in "${EXCLUDES[@]}"; do
  reach=$(printf '%s\n' "$objects_list" | grep -cF -- "$p" || true)
  [ "$reach" -eq 0 ] || { echo "실패: 도달 가능한 객체 중 '$p' 경로 $reach개" >&2; fail=1; }
done
for b in $(git for-each-ref --format='%(refname:short)' refs/heads); do
  if git -C "$SRC" merge-base --is-ancestor "$GITIGNORE_ANCHOR" "$b" 2>/dev/null; then
    git show "$b:.gitignore" | grep -qF "/Assets/GUI PRO Kit - Simple Casual/" \
      || { echo "실패: $b 공개본 .gitignore에 GUI PRO 제외 규칙이 없음" >&2; fail=1; }
  fi
done
n=$(git log --branches --format='%an <%ae>%n%cn <%ce>' | grep -cF -- "$HIDE_EMAIL" || true)
[ "$n" -eq 0 ] || { echo "실패: 작성자·커미터에 '$HIDE_EMAIL'이 남아 있음($n)" >&2; fail=1; }
n=$(git log --branches -S"$HIDE_EMAIL" --format='%h' | wc -l)
[ "$n" -eq 0 ] || { echo "실패: 파일 본문 기록에 '$HIDE_EMAIL'이 남아 있음(커밋 $n개, SCRUB_FILES 확인)" >&2; fail=1; }
# 실명·타인 경로는 이미 공개된 커밋(PUBLISHED_TIPS 이전)을 뺀 새 커밋에서만 0건이어야 한다(공개된 이력은 다시 쓰지 않는다)
excl=""
verify_refs=""
for b in $PUSH_BRANCHES; do verify_refs="$verify_refs refs/heads/$b"; done
for t in $PUBLISHED_TIPS; do
  if git cat-file -e "$t^{commit}" 2>/dev/null; then excl="$excl $t"; else echo "  주의: 이미 공개된 커밋 $t 를 사본에서 재현하지 못했습니다(공개 당시와 다르게 이력을 바꿨을 수 있음)" >&2; fi
done
for s in "$HIDE_NAME_KO" "$HIDE_FOREIGN_PATH"; do
  # 새 커밋이 "추가한 줄"에 있는지만 본다(-S는 공개된 부모에서 지워진 경우도 잡아 오탐이 난다)
  n=$(git log -p --format= $verify_refs --not $excl 2>/dev/null | grep '^+' | grep -cF -- "$s" || true)
  [ "$n" -eq 0 ] || { echo "실패: 새 커밋이 추가한 줄에 가려야 할 문자열이 ${n}번 남아 있음(SCRUB_FILES/NEWSCRUB_FILES에 파일을 추가하세요)" >&2; fail=1; }
done
if [ -n "$(git fsck --full 2>&1)" ]; then echo "실패: git fsck 출력이 있음" >&2; fail=1; fi
for b in $(git for-each-ref --format='%(refname:short)' refs/heads); do
  o=$(git -C "$SRC" rev-parse "$b^{tree}" 2>/dev/null || echo none)
  c=$(git rev-parse "$b^{tree}")
  echo "  $b $(git rev-parse --short "$b")  커밋 $(git rev-list --count "$b")  트리 $([ "$o" = "$c" ] && echo '원본과 동일' || echo '원본과 다름(제외 경로 삭제)')"
done
echo "  크기: $(du -sm . | cut -f1) MB"
[ "$fail" -eq 0 ] || { echo "검증 실패 — 푸시하지 않습니다." >&2; exit 1; }
echo "검증 통과."

if [ "${1:-}" = "--push" ]; then
  # --all은 쓰지 않는다: 공개 main이 비공개 main과 분기돼 있어 main까지 밀면 거부되거나 이력이 섞인다.
  # main은 AGENTS.md 절차(사본에서 [공개 main, 새 Mobile 끝]을 부모로 하는 병합 커밋)로 따로 올린다
  echo "== 5. 공개 저장소에 푸시: $REMOTE_URL  브랜치: $PUSH_BRANCHES (일반 푸시, --force 없음)"
  for b in $PUSH_BRANCHES; do
    git show-ref --verify --quiet "refs/heads/$b" || { echo "사본에 브랜치 '$b'가 없습니다." >&2; exit 4; }
    # 공개 저장소의 같은 브랜치 끝이 사본의 조상이어야 일반 푸시(fast-forward)가 된다. 아니면 푸시 전에 알리고 중단한다
    remote_tip=$(git ls-remote "$REMOTE_URL" "refs/heads/$b" | cut -f1)
    if [ -n "$remote_tip" ]; then
      git fetch -q "$REMOTE_URL" "refs/heads/$b:refs/pub/$b" 
      git merge-base --is-ancestor "$remote_tip" "refs/heads/$b" || { echo "공개 저장소의 $b ($remote_tip)가 사본의 조상이 아닙니다 - 이력이 갈라져 일반 푸시를 할 수 없습니다. 푸시하지 않습니다." >&2; exit 5; }
    fi
    git push "$REMOTE_URL" "refs/heads/$b:refs/heads/$b"
  done
else
  echo "푸시하지 않았습니다. 올리려면: tools/publish-clean-repo.sh --push"
fi
