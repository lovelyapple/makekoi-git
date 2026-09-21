# makekoi-client

## Unity CLI（起動中の Editor を操作する）

このプロジェクトは MCP ではなく **Unity 公式 CLI + `com.unity.pipeline`** で起動中の Editor を操作する。
Editor は GUI で起動したままにしておき、CLI がその Editor の HTTP サーバ（既定 7800）に接続する。

### 必ずサンドボックス外で実行する

`unity` コマンドは **必ず `dangerouslyDisableSandbox: true` で実行すること。**

サンドボックス内だと localhost:7800 への接続が遮断され、CLI は
`No Unity Editor instances found with reachable Pipeline servers` を返す。
これは「Editor が起動していない」ように見えるが**誤報**。
descriptor ファイル自体はサンドボックス内からも読めるので、ファイルの有無では切り分けられない。

切り分け:

| descriptor | `unity status` | 原因 |
|---|---|---|
| 見える | 失敗 | サンドボックスの localhost 遮断 → サンドボックス外で再実行 |
| 見えない | 失敗 | サーバが起動していない（後述のコンパイルエラーを疑う） |
| 見える | ready | 正常 |

### よく使うコマンド

```bash
unity status                                   # 接続確認（ready なら繋がる）
unity command                                  # 利用可能なコマンド一覧
unity command eval '<C#>'                      # 任意の C# を Editor 上で実行
unity command eval_file --file <path>          # ファイルから C# を実行（長いコードはこちら）
unity command recompile                        # コンパイル
unity command get_console_logs --severity error
unity command run_tests --mode EditMode
unity command capture_game_view --save_path <path>
```

### CLI を使わずに生死を確かめる

```bash
lsof -nP -iTCP:7800                            # Unity ... (LISTEN) が出るか
grep "Start HTTP server: port:7800" ~/Library/Logs/Unity/Editor.log
ls -la Library/Pipeline/.unity-pipeline-port   # ポートと認証トークンを持つ descriptor（0600）
```

認証トークンはこの descriptor にしか無い（Editor の SessionState 由来）。
ファイルが無いと CLI でも HTTP 直叩きでも Editor には入れない。

## C# を編集したら、まずコンパイルエラーを確認する

```bash
grep -E "error CS[0-9]+" ~/Library/Logs/Unity/Editor.log | tail
```

**コンパイルエラーがあるとドメインリロードが失敗し、`PipelineServerStartup` が走らず
Pipeline サーバが復帰しない。** その結果 descriptor が消え、CLI から一切繋がらなくなる。
「急に Editor に繋がらなくなった」ときは、まず自分が直前に書いたコードを疑うこと。
Safe Mode にはならないので `unity pipeline list` の `safeMode: false` は当てにならない。

よく踏むもの: UniTask の `SuppressCancellationThrow()` は `bool` ではなく
`(bool IsCanceled, T Result)` を返す。

## Prefab の参照付け

**Prefab の参照を繋ぐために使い捨ての Editor 拡張スクリプトを作らないこと。**
リポジトリに一度きりしか使わないコードが残る。

正しいやり方は `unity command eval` から `SerializedObject` / `SerializedProperty` を
書いて `PrefabUtility.SaveAsPrefabAsset` で保存する:

```csharp
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
var so = new UnityEditor.SerializedObject(component);
so.FindProperty("_field").objectReferenceValue = target;
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
UnityEditor.PrefabUtility.UnloadPrefabContents(root);
```

CLI が繋がらない場合は、使い捨てスクリプトで回避しようとせず、
Unity を閉じてもらって prefab の YAML を直接編集するか、手で刺してもらう。

`LoadPrefabContents` → `SaveAsPrefabAsset` で直接書き換えると、
すでに開いている Inspector は古い値を表示したままのことがある（選択し直すと更新される）。

## Master データ

TSV が正で、Editor メニューから asset をビルドする。enum に値を追加したら必ず実行すること。

- `Makekoi/StaffReactionMaster/Build` ← `Assets/Scripts/Game/StaffReaction/Editor/staff_reaction_master_source.tsv`
- `Makekoi/PartsMaster/Build` ← `Assets/Scripts/Game/Master/Editor/parts_master_source.tsv`

TSV の改行は `¥n`（インポータが `\n` に変換する）。
