#!/usr/bin/env bash
# 공개용 저장소(UrbanSurvival)에 올릴 사본을 만든다.
# 라이선스상 공개 저장소에 둘 수 없는 에셋(EXCLUDES)을 "모든 커밋의 기록에서" 지운 bare 복제본을 만들고 검증한다.
# 이 저장소(작업 폴더)와 GitHub는 건드리지 않는다. --push를 줄 때만 공개 저장소에 푸시한다(강제 푸시 없음).
#
# 사용:
#   tools/publish-clean-repo.sh           # 사본 생성 + 검증만 (푸시 안 함)
#   tools/publish-clean-repo.sh --push    # 검증 통과 후 공개 저장소에 --all 푸시(일반 푸시, 거부되면 중단)
#
# 환경 변수: CLEAN_DIR(사본 위치, 기본 <작업폴더>/../Zombie_clean.git), PUBLIC_REMOTE(공개 저장소 주소)
#
# 커밋된 기록만 사용한다(커밋하지 않은 변경은 포함되지 않음). 기록 정리는 결정적(deterministic)이라
# 같은 커밋에서 다시 실행하면 같은 SHA가 나오고, 새 커밋이 위에 쌓이면 공개 저장소에는 일반 푸시(fast-forward)로 올라간다.
set -euo pipefail

SRC="$(git rev-parse --show-toplevel)"
DEST="${CLEAN_DIR:-$SRC/../Zombie_clean.git}"
REMOTE_URL="${PUBLIC_REMOTE:-https://github.com/ssa25879/UrbanSurvival.git}"
EXCLUDES=("Assets/GUI PRO Kit - Simple Casual" "Assets/GUI PRO Kit - Simple Casual.meta")

case "$DEST" in
  *_clean.git) ;;
  *) echo "CLEAN_DIR는 '_clean.git'으로 끝나야 합니다(실수로 다른 폴더를 지우지 않기 위함): $DEST" >&2; exit 2 ;;
esac

echo "== 1. 사본 생성: $DEST"
rm -rf "$DEST"
git clone --bare --no-hardlinks "$SRC" "$DEST" >/dev/null 2>&1
cd "$DEST"
git remote remove origin   # 실수로 원본에 푸시하지 않도록

echo "== 2. 기록에서 제외 경로 삭제 (git filter-branch, 시간이 걸린다)"
RM_ARGS=""
for p in "${EXCLUDES[@]}"; do RM_ARGS="$RM_ARGS \"$p\""; done
export FILTER_BRANCH_SQUELCH_WARNING=1
git filter-branch -f --prune-empty --tag-name-filter cat \
  --index-filter "git rm -r -q --cached --ignore-unmatch -- $RM_ARGS" -- --branches >/tmp/publish-clean-filter.log 2>&1 \
  || { echo "filter-branch 실패: /tmp/publish-clean-filter.log 확인" >&2; exit 1; }

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
reach=$(git rev-list --objects --branches | grep -c 'GUI PRO' || true)
[ "$reach" -eq 0 ] || { echo "실패: 도달 가능한 GUI PRO 객체 $reach개" >&2; fail=1; }
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
  echo "== 5. 공개 저장소에 푸시: $REMOTE_URL (일반 푸시, --force 없음)"
  git push "$REMOTE_URL" --all
else
  echo "푸시하지 않았습니다. 올리려면: tools/publish-clean-repo.sh --push"
fi
