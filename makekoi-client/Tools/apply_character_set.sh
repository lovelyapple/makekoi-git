#!/bin/bash
# 店長キャラの立ち絵を差し替える。
#   ./Tools/apply_character_set.sh           … 使えるセットと現在の適用先を表示
#   ./Tools/apply_character_set.sh gamma     … 既存キャラを適用
#   ./Tools/apply_character_set.sh shinten   … 新キャラを適用
#
# png だけを上書きし、.meta は触らない（GUID と取り込み設定を保つため）。
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SETS_DIR="$ROOT/CharacterSets"
DEST="$ROOT/Assets/Resources/2D/Texture/StaffReaction"
MARKER="$SETS_DIR/.current"

list_sets() {
  find "$SETS_DIR" -mindepth 1 -maxdepth 1 -type d ! -name '_*' -exec basename {} \; | sort
}

if [ $# -eq 0 ]; then
  echo "使えるセット:"
  list_sets | sed 's/^/  /'
  echo
  if [ -f "$MARKER" ]; then
    echo "現在適用中: $(cat "$MARKER")"
  else
    echo "現在適用中: 不明（このスクリプトでの適用履歴なし）"
  fi
  exit 0
fi

NAME="$1"
SRC="$SETS_DIR/$NAME"

if [ ! -d "$SRC" ]; then
  echo "エラー: セット '$NAME' がありません。" >&2
  echo "使えるセット: $(list_sets | tr '\n' ' ')" >&2
  exit 1
fi

shopt -s nullglob
FILES=("$SRC"/chara_reaction_*.png)
if [ ${#FILES[@]} -eq 0 ]; then
  echo "エラー: $SRC に chara_reaction_*.png がありません。" >&2
  exit 1
fi

mkdir -p "$DEST"
for f in "${FILES[@]}"; do
  cp "$f" "$DEST/$(basename "$f")"
  echo "  $(basename "$f")"
done

echo "$NAME" > "$MARKER"
echo
echo "'$NAME' を適用しました（${#FILES[@]} 枚）。"
echo "Unity を開いている場合は、再取り込みのため一度フォーカスを当ててください。"
