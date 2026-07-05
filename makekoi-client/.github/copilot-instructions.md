# Copilot Instructions for makekoi-client

共通ルール: `/Users/lilin/projects/makekoi-git/copilot-project-common-instruction.md` を参照。
以下はそのルールに加えて、このプロジェクト固有の指示。

---

## 絶対守るルール（最重要）

### ファイル・クラスの削除確認
- **方針変更や重複解消によってファイル・クラスが不要になった場合、削除前に必ずユーザーに確認する**
- 作ったばかりのファイルでも例外なし
- 「同じ機能が別の方法で実現できる」と判断した時点で即削除しない

### ファイル作成・変更時のドキュメント更新
- ファイル追加・削除 → `docs/system_config.md` を更新
- 機能追加・変更・削除 → `docs/function_map.md` を更新
- 実装とドキュメントは常に同期させる

### コミット
- 機能単位で段階的にcommit（一度に全部入れない）
- ブランチ名の区切りは `/`。例: `feature/v0.1.91`

---

## 技術スタック

| レイヤー | 技術 |
|---------|------|
| ゲームエンジン | Unity 6 (URP) |
| UIフレームワーク | AstraydeFramework (`com.astrayde.framework`) |
| 非同期 | UniTask |
| リアクティブ | R3 |
| DI | VContainer |

## AstraydeFramework

- ローカル開発パス: `/Users/lilin/projects/AstraydeFramework/AstraydeFramework_Develop/`
- 変更手順は `.instructions.md` の「AstraydeFramework 開発ワークフロー」を参照
- UIウィンドウ管理は `WindowManager.OpenWindowAsync<T>()` を使用

## プロジェクト構成

```
Assets/Scripts/
├── System/          # インフラ層（MonoSingletoneBase, ResourceContainer）
│   ├── Sound/       # SoundManager, BgmFader, SeRootController等
│   ├── UI/          # UIButtonSePlayer等
│   ├── Generated/   # CommonSeType等（自動生成）
│   └── Editor/      # Editorツール
└── PartnerCreate/   # パートナー生成ロジック
```
