# makekoi

鯉を育てるゲーム（Unity クライアント）

## 概要

makekoi は鯉を育成するゲームアプリケーションです。Unity をクライアントとして使用します。

## 技術スタック

| レイヤー | 技術 |
|---------|------|
| ゲームエンジン | Unity 6 (6000.3.18f1) |
| レンダリング | Universal Render Pipeline (URP) 17.3.0 |
| DI コンテナ | VContainer |
| 入力システム | Input System 1.19.0 |
| AI ナビゲーション | AI Navigation 2.0.13 |
| UI | uGUI 2.0.0 |
| タイムライン | Timeline 1.8.12 |
| テスト | Unity Test Framework 1.6.0 |
| IDE | Rider / Visual Studio |

## アーキテクチャ

クリーンアーキテクチャ + VContainer による依存性注入。
依存関係は**外側から内側への一方向**のみ。

```
makekoi-git/                        # リポジトリルート
├── README.md                       # プロジェクト全体像（本ファイル）
├── copilot-project-common-instruction.md  # Copilot 共通ルールテンプレート
├── .github/
│   └── copilot-instructions.md     # Copilot が守るべきルール
├── docs/
│   ├── system_config.md            # ファイル一覧・設定値管理台帳
│   └── function_map.md             # 機能とファイルの対応表
└── makekoi-client/                 # Unity プロジェクト本体
    ├── Assets/
    │   ├── Scripts/
    │   │   ├── Domain/            # 【最内層】エンティティ・ビジネスルール
    │   │   │   ├── Entities/      # ドメインモデル（Koi, Pond, Feed 等）
    │   │   │   ├── ValueObjects/  # 値オブジェクト
    │   │   │   └── Interfaces/    # リポジトリ・サービスのインターフェース定義
    │   │   ├── UseCases/          # 【ユースケース層】アプリケーションロジック
    │   │   │   ├── Koi/           # 鯉の育成・成長ユースケース
    │   │   │   └── Pond/          # 池管理ユースケース
    │   │   ├── Infrastructure/    # 【インフラ層】外部接続・永続化
    │   │   │   ├── Repositories/  # リポジトリ実装（セーブデータ等）
    │   │   │   ├── Services/      # 外部サービス連携
    │   │   │   └── Config/        # 設定管理・定数定義
    │   │   ├── Presentation/      # 【プレゼンテーション層】Unity依存の表示・入力
    │   │   │   ├── Controllers/   # MonoBehaviour（入力・イベント仲介）
    │   │   │   ├── Views/         # UI制御・表示更新
    │   │   │   ├── Presenters/    # UseCaseとViewの橋渡し
    │   │   │   └── Input/         # Input System ハンドリング
    │   │   └── DI/                # 【DI設定】VContainer LifetimeScope
    │   │       ├── RootLifetimeScope.cs    # アプリ全体のDI登録
    │   │       ├── GameLifetimeScope.cs    # ゲームシーンのDI登録
    │   │       └── TitleLifetimeScope.cs   # タイトルシーンのDI登録
    │   ├── Prefabs/               # プレハブ
    │   ├── Scenes/                # シーンファイル
    │   ├── Materials/             # マテリアル・シェーダー
    │   ├── Textures/              # テクスチャ
    │   ├── Audio/                 # 音声ファイル
    │   ├── Animations/            # アニメーション
    │   ├── ScriptableObjects/     # ScriptableObject アセット（設定データ）
    │   └── Resources/             # Resources フォルダ（最小限に）
    ├── Packages/                   # パッケージ定義
    └── ProjectSettings/            # Unity プロジェクト設定
```

### レイヤー依存ルール

```
Domain ← UseCases ← Infrastructure
                  ← Presentation
                  ← DI（全レイヤーを参照し、バインディングを定義）
```

| レイヤー | 責務 | Unity 依存 |
|---------|------|-----------|
| **Domain** | ビジネスルール、エンティティ、インターフェース定義 | ❌ 禁止 |
| **UseCases** | アプリケーション固有のロジック（育成、成長計算等） | ❌ 禁止 |
| **Infrastructure** | データ永続化、外部サービス、設定読み込み | ✅ 許可 |
| **Presentation** | MonoBehaviour、UI、入力、描画 | ✅ 許可 |
| **DI** | VContainer LifetimeScope でのバインディング定義 | ✅ 許可 |

### VContainer DI 構成パターン

```csharp
// DI/GameLifetimeScope.cs
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Domain のインターフェースに Infrastructure の実装をバインド
        builder.Register<IKoiRepository, KoiRepository>(Lifetime.Singleton);

        // UseCases 登録
        builder.Register<FeedKoiUseCase>(Lifetime.Transient);
        builder.Register<GrowKoiUseCase>(Lifetime.Transient);

        // Presentation（MonoBehaviour）登録
        builder.RegisterComponentInHierarchy<KoiView>();
        builder.RegisterComponentInHierarchy<KoiPresenter>();
    }
}
```

### 依存性逆転の原則 (DIP)
- Domain 層にインターフェースを定義（`IKoiRepository` 等）
- Infrastructure 層が実装を提供（`KoiRepository` 等）
- UseCase 層はインターフェースのみ参照
- VContainer の LifetimeScope で具象クラスをバインド

## プロジェクトルール

### コーディングルール
- ❌ **パス直書き・絶対パス禁止** → 設定モジュール経由 or 相対パスで解決
- ❌ **マジックナンバー禁止** → `Infrastructure/Config/` or ScriptableObject で管理
- ❌ **Domain/UseCases 層で UnityEngine 名前空間を使用禁止**
- ❌ **`new` で依存を生成しない** → VContainer 経由で注入
- ✅ ファイル作成・削除時は `docs/system_config.md` を更新
- ✅ 機能追加・変更時は `docs/function_map.md` を更新
- ✅ `[SerializeField]` で Inspector 公開、public フィールドは避ける
- ✅ コンストラクタインジェクションを優先（MonoBehaviour は `[Inject]` メソッド）

### 命名規則（C#）
| 対象 | 規則 | 例 |
|------|------|----|
| クラス・構造体 | PascalCase | `KoiController` |
| インターフェース | I + PascalCase | `IKoiRepository` |
| UseCase クラス | 動詞 + 名詞 + UseCase | `FeedKoiUseCase` |
| LifetimeScope | シーン名 + LifetimeScope | `GameLifetimeScope` |
| public メソッド | PascalCase | `FeedKoi()` |
| private メソッド | PascalCase | `CalculateGrowth()` |
| private フィールド | _camelCase | `_currentHealth` |
| public プロパティ | PascalCase | `MaxHealth` |
| SerializeField | _camelCase | `_moveSpeed` |
| 定数 | PascalCase | `MaxKoiCount` |
| enum | PascalCase | `KoiType.Goldfish` |

### Git ルール
- 📦 機能単位で段階的にコミット
- 📝 コミットメッセージは日本語OK、内容を明確に
- 🏷️ ブランチ名の区切りは `_`（`/` ではなく）。例: `feature_koi_feeding`
- ⚠️ `.meta` ファイルは必ずコミットに含める

## セットアップ

### 前提条件
- Unity Hub がインストール済み
- Unity 6 (6000.3.18f1) がインストール済み

### 手順
1. リポジトリをクローン
   ```bash
   git clone <repository-url>
   ```
2. Unity Hub で `makekoi-client/` フォルダを開く
3. Package Manager で VContainer をインストール（未導入の場合）
   - Window > Package Manager > Add package from git URL:
   - `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.16.8`
4. Unity エディタで `Assets/Scenes/` 内のシーンを開く

## 起動方法

Unity エディタ上で Play ボタンを押す。

## TODO

- [ ] プロジェクト基盤構築（フォルダ構成、VContainer 導入）
- [ ] Domain 層設計（鯉エンティティ、値オブジェクト、インターフェース）
- [ ] UseCase 層実装（育成・成長ロジック）
- [ ] Infrastructure 層実装（セーブ/ロード、設定管理）
- [ ] Presentation 層実装（鯉の表示・操作UI）
- [ ] DI LifetimeScope 構築
- [ ] サウンドシステム