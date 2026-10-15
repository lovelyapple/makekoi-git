# 店長キャラの立ち絵セット

`Assets/` の外で立ち絵を管理し、ビルド前に差し替えるための置き場です。

## セット

- `gamma/`   … 従来の店長。社外公開には使えない
- `shinten/` … 新しい店長。社外公開用
  - `_original/` に生成元（1024x1536）をそのまま保管
  - 直下は既存仕様に合わせて 1050x1498 に正規化したもの

## 切り替え

    ./Tools/apply_character_set.sh            # セット一覧と現在の適用先
    ./Tools/apply_character_set.sh gamma
    ./Tools/apply_character_set.sh shinten

`Assets/Resources/2D/Texture/StaffReaction/` に png だけを上書きします。
`.meta` は触らないので、GUID と取り込み設定は保たれます。

## 画像の対応

| ファイル | 表情 | 使われる場面 |
|---|---|---|
| `_01` | ニュートラル〜冷ややか | おねだり要求、落胆、下級グレード、お気に入り失敗、歓迎2 |
| `_02` | 驚き | 土下座に驚く、値切り成功 |
| `_03` | 喜び・照れ | 土下座で喜ぶ、驚き |
| `_04` | 満足・上機嫌 | 最高級グレード獲得、値切り成功 |
| `_05` | 呆れ・突き放す | ミニゲーム失敗、値切り失敗 |
| `_06` | 営業スマイル | いらっしゃいませ、中級グレード、待機 |

## 注意

マスタ（`Assets/Resources/Master/StaffReactionMaster.asset`）は `_01`〜`_06` を参照します。
`Assets/Resources/2D/Texture/StaffReaction/` に実体が無いと画像が出ないので、
ビルド前に必ずどちらかのセットを適用してください。
