# Copilot Instructions for makekoi

## プロジェクト概要

Unity 6 + VContainer を使った鯉育成ゲーム。クリーンアーキテクチャで設計。

## 絶対守るルール（最重要）

### 1. レイヤー依存ルール
- **Domain 層・UseCases 層では `UnityEngine` 名前空間を使用禁止**
- 依存方向は `Domain ← UseCases ← Infrastructure / Presentation / DI` のみ
- Domain 層は他のどのレイヤーも参照してはならない
- UseCases 層は Domain 層のみ参照可能

### 2. DI（VContainer）ルール
- **`new` で依存オブジェクトを直接生成しない** → VContainer 経由で注入
- 通常クラスは**コンストラクタインジェクション**を使う
- MonoBehaviour は **`[Inject]` メソッドインジェクション**を使う
- LifetimeScope は `Scripts/DI/` フォルダに配置
- シーンごとに LifetimeScope を分ける

### 3. パスの扱い
- ❌ ファイル内で他ファイルのパスを直書き（ハードコード）禁止
- ❌ 絶対パス禁止（OS固有ツールからの呼び出しを除く）
- ✅ 設定モジュール経由、または相対パスで参照
- ✅ Unity のリソース参照は Addressables or ScriptableObject 経由

### 4. マジックナンバー・定数
- ❌ コード内にマジックナンバーを書かない
- ✅ ゲーム設定値 → ScriptableObject で管理
- ✅ アプリ内定数 → `Infrastructure/Config/` の定数クラスに定義
- ✅ `docs/system_config.md` に定数一覧を記載

### 5. ファイル管理・ドキュメント更新
- 📋 ファイルを作成・削除したら `docs/system_config.md` を更新
- 📋 機能を追加・変更・削除したら `docs/function_map.md` を更新
- 🔍 変更時は影響を受ける既存ファイル・機能がないかレビュー
- ⚠️ `.meta` ファイルは削除・無視しない（必ず Git に含める）

### 6. フォルダ構成
```
Assets/Scripts/
├── Domain/            # エンティティ・値オブジェクト・インターフェース
│   ├── Entities/
│   ├── ValueObjects/
│   └── Interfaces/
├── UseCases/          # アプリケーションロジック
├── Infrastructure/    # 永続化・外部サービス・設定
│   ├── Repositories/
│   ├── Services/
│   └── Config/
├── Presentation/      # MonoBehaviour・UI・入力
│   ├── Controllers/
│   ├── Views/
│   ├── Presenters/
│   └── Input/
└── DI/                # VContainer LifetimeScope
```

### 7. rootに置けるファイル（リポジトリルート）
- `README.md`
- `copilot-project-common-instruction.md`
- `.gitignore`
- `.env.example`（必要時）

## 技術スタック

| 技術 | バージョン | 用途 |
|------|-----------|------|
| Unity | 6000.3.18f1 | ゲームエンジン |
| URP | 17.3.0 | レンダリング |
| VContainer | 最新 | DI コンテナ |
| Input System | 1.19.0 | 入力制御 |
| AI Navigation | 2.0.13 | AI ナビゲーション |

## 命名規則（C#）

| 対象 | 規則 | 例 |
|------|------|----|
| クラス・構造体 | PascalCase | `KoiController` |
| インターフェース | I + PascalCase | `IKoiRepository` |
| UseCase クラス | 動詞 + 名詞 + UseCase | `FeedKoiUseCase` |
| LifetimeScope | シーン名 + LifetimeScope | `GameLifetimeScope` |
| private フィールド | _camelCase | `_currentHealth` |
| SerializeField | _camelCase | `_moveSpeed` |
| 定数 | PascalCase | `MaxKoiCount` |

## コミット規則

- 機能単位で段階的にコミット（一度に全部入れない）
- コミットメッセージは内容を明確に（日本語OK）
- ブランチ名の区切りは `_`（`/` ではなく）。例: `feature_koi_feeding`
- Copilot と作業した場合は `Co-authored-by` trailer を含める

## ドキュメント更新タイミング

| トリガー | 更新対象 |
|---------|---------|
| ファイル追加・削除 | `docs/system_config.md` |
| 機能追加・変更・削除 | `docs/function_map.md` |
| 設計方針変更 | `README.md` + `.github/copilot-instructions.md` |
| 実装とドキュメントの乖離を発見 | 即時修正 |

## VContainer 使用パターン

### 通常クラス（コンストラクタインジェクション）
```csharp
public class FeedKoiUseCase
{
    private readonly IKoiRepository _koiRepository;

    public FeedKoiUseCase(IKoiRepository koiRepository)
    {
        _koiRepository = koiRepository;
    }
}
```

### MonoBehaviour（メソッドインジェクション）
```csharp
public class KoiPresenter : MonoBehaviour
{
    private FeedKoiUseCase _feedKoiUseCase;

    [Inject]
    public void Construct(FeedKoiUseCase feedKoiUseCase)
    {
        _feedKoiUseCase = feedKoiUseCase;
    }
}
```

### LifetimeScope 登録
```csharp
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Domain Interface → Infrastructure 実装
        builder.Register<IKoiRepository, KoiRepository>(Lifetime.Singleton);

        // UseCases
        builder.Register<FeedKoiUseCase>(Lifetime.Transient);

        // Presentation（MonoBehaviour）
        builder.RegisterComponentInHierarchy<KoiPresenter>();
    }
}
```

## Copilot への追加指示

- 🔍 作業前に `README.md` と `docs/` を読んでプロジェクトを把握する
- 🧪 変更後は既存機能が壊れていないか確認する
- 📖 不明点があればユーザーに質問する（勝手に判断しない）
- 🔄 rubber-duck agent や code-review agent を活用してレビューする
- ⚠️ セキュリティ（APIキー、トークン）はログや公開コードに含めない
- 🎮 Unity 固有: Prefab の変更は慎重に（シリアライズ参照が壊れる可能性）
