# TokenWorld

Unity **2019.3.15f1** のプロジェクト。Unity Hub には、このリポジトリ内の `TokenWorld/` を追加する。

Build Settings に登録されているシーンは `Assets/Scenes/Main.unity`。
初回起動時にはパッケージの解決とアセットのインポートが行われる。

## Gitで管理するもの

- `TokenWorld/Assets/`：スクリプト、シーン、素材、プラグインと、それぞれの `.meta`
- `TokenWorld/Packages/`：`manifest.json` と `packages-lock.json`
- `TokenWorld/ProjectSettings/`：共有するプロジェクト設定

アセットの追加・移動・削除は、対応する `.meta` と一緒にコミットする。
Unityの設定は **Visible Meta Files** と **Force Text** を使用する。
既存のバイナリ形式のアセットはそのまま保持している。

## 除外と差分の設定

ルートの `.gitignore` はOS・IDEの一時ファイルを、`TokenWorld/.gitignore` はUnityのキャッシュ、ログ、ローカル設定、ビルド出力を除外する。
ビルド先は `TokenWorld/Builds/` にすると、実行ファイルと付随データをまとめて管理対象外にできる。
除外されたファイルはローカルに残り、Gitには追加されない。

`.gitattributes` はテキストを自動判定し、画像・音声・ネイティブプラグインなどをバイナリとして扱う。
`.asset` と `.prefab` はテキストとバイナリが混在するため、拡張子だけでテキスト扱いを強制しない。
既存ファイルの改行は一括変換していない。

参考：[Unityの外部バージョン管理](https://docs.unity3d.com/2019.3/Documentation/Manual/ExternalVersionControlSystemSupport.html)、[Gitの属性設定](https://git-scm.com/docs/gitattributes)。
