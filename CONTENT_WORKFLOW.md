# I/O接続リファレンス

Unity **2022.3.62f2** で `TokenWorld/` を開きます。以下の `Assets/` はこのプロジェクト内のパスです。

## 起動

メニューの **TokenWorld > Open development scene** で `Assets/Workspace/Scenes/Workspace.unity` を開き、Play します。
Hierarchy の **Workspace** に入力とプレビュー、子の **Content** に出力を置いています。作品の素材・スクリプトは任意のフォルダで管理できます。

## 4画面の表示

展示は床を含む4画面です。Game ビューには Play 前から、部屋の中から見た並びで表示します。上段が左・正面・右、下段が床です。床は正面の下に置き、床の上端が展示で正面の壁に接する辺です。大きさは実寸の比率ではありません。

| 画面 | 描画先 | 解像度 | 展示の Spout 名 | 主な映像 |
| --- | --- | --- | --- | --- |
| 床（池） | `MainTex` | 1920×1080 | `MainSpout` | 水面・鯉・波紋。入力を受ける唯一の画面 |
| 正面（滝） | `WallTex` | 3158×850 | `WallSpout` | 滝と草木。床で反応した鯉が滝を登る |
| 左 | `SideTex` の左半分 | 1920×890 | `SideSpout`（左右で共通） | 季節の画像 |
| 右 | `SideTex` の右半分 | 1920×890 | `SideSpout`（左右で共通） | 季節の画像 |

描画先は `Assets/RowTexter/` の RenderTexture です。左右は 3840×890 の `SideTex` 1枚に並べて描き、1つの Spout で送ります。Spout 名は `SpoutObj` の子の GameObject 名です。
正面・左・右は表示のみで、クリックしても入力になりません。Play 中は `V` で4画面と床のみの表示を切り替えます。Play 前は `Workspace` の **InputPreview > Show All Screens** で切り替えます。床のみの表示では床が大きくなります。
制作シーンの `kaesu` の **Telop** は外部画像を読み込まず、Images も空のため、鯉が滝を登っても正面のテロップは出ません。確認する場合は Images に Sprite を登録します。

### Scene ビューで確認する

Scene ビューには、床と正面のカメラに映る範囲を色付きの枠と名前で表示します。水色が床（池）、橙色が正面（滝）で、水色の点線は入力（センサー）が届く範囲です。
左右の画面と、床・正面の文字や暗転は UI です。3D の場所から離れたキャンバスにあり、紫（左右）と灰色（文字・暗転）の枠で名前を付けています。左右の画面に 3D の物は映りません。
枠は、Scene ビュー上部のツールバーにある Gizmos の切り替えボタン（マウスを乗せると「Toggle visibility of all Gizmos in the Scene view」と出る）で表示・非表示を切り替えます。

Scene ビューの隅の **4画面プレビュー** は、各画面の今の映像です。Play 前でも、物を置いたり動かしたりすると更新されます。**大きく / 小さく** で大きさを変え、隠す・出すは、Scene ビューのタブを右クリックして **Overlay Menu** を開き、**4画面プレビュー** を切り替えます。

カメラの範囲に入っていても、映るかどうかはレイヤーで決まります。

| レイヤー | 床（池） | 正面（滝） |
| --- | --- | --- |
| Default・TransparentFX・Ignore Raycast・UI・Koi | 映る | 映る |
| Water・Spout・Effect_sub | 映る | 映らない |
| Telop・Effect・Effect_wall | 映らない | 映る |

## 入力とプレビュー

初期状態はマウス入力です。Game ビューの床の映像内を左ボタンで押す・ドラッグすると、水色の十字で検出位置を表示します。
自動入力では1〜3点を動かせます。どちらも URG 実機や外部設定ファイルは不要です。

| 操作 | キー |
| --- | --- |
| マウス / 自動に切り替える | `1` / `2` |
| 入力を止める・再開する | `Space` |
| 季節を変える | `B` |
| 4画面 / 床のみの表示を切り替える | `V` |
| 操作パネルを表示・非表示にする | `H` |

キー操作時は Game ビューにフォーカスします。**クリア**は検出位置と追跡状態を消し、入力を停止します。再開は **入力を再開** または `Space` です。
シミュレーションの対象は検出位置で、生のレーザー計測や遮蔽は再現しません。

## 入力から出力への接続

`Workspace` の **InteractionInput** が Mouse / Automatic / Sensor を共通のワールド座標に変換します。出力側で入力機器を区別する必要はありません。

| 接続口 | 内容 |
| --- | --- |
| PositionUpdated（Inspector: Position Updated） | 検出位置を `Vector3` で通知。入力点ごとに毎フレーム呼び出す |
| InputCleared（Inspector: Input Cleared） | 入力のクリア通知。残っている表示の解除などに使う |
| Positions | 最新の全検出位置を `IReadOnlyList<Vector3>` で参照するC#用プロパティ |

`Positions` は読み取り専用のスナップショットで、取得した一覧は次のフレームでも書き換わりません。座標に人物IDは含みません。`InputCleared` は入力点の消失・停止・切替・コンポーネント無効化により、入力点がある状態から0件になったときに一度だけ通知します。0件のフレームが続いても繰り返しません。

座標を受け取る `public void React(Vector3 worldPosition)` を用意し、対象の GameObject を **Position Updated** へドラッグして **Dynamic Vector3** からメソッドを選択します。
クリア処理は `public void Clear()` のような引数なしのメソッドを **Input Cleared** に登録します。複数点をまとめて処理する場合は `Positions` を参照します。

## 任意Prefabの出力

`Workspace > Content` の **PrefabOutput.React** は Position Updated に接続済みです。**Reaction Prefab** は初期状態で空のため、追加の描画はありません。任意の Prefab を割り当てると検出位置に生成します。

| 設定 | 内容 |
| --- | --- |
| Reaction Prefab | 検出位置に生成する Prefab |
| Interval | 生成間隔。すべての入力点で共有 |
| Lifetime | 生成物を消すまでの秒数 |
| Height Above Input | 入力面からの高さ。水面や背景に隠れる場合に調整 |

独自の出力を使う場合はイベントの接続先を追加・置き換えます。Play 中の Inspector の変更は原則として停止すると戻るため、残す設定は停止後に保存します。

## 実センサーと展示

Play を停止し、`Workspace` の **InteractionInput > Mode** を **Sensor** にしてから Play します。対応する URG 実機、ネットワーク接続、IP / Port と位置・回転・縮尺・検出範囲の調整が必要です。実機での通信・校正は未検証です。
制作シーンは外部画像の読み込み、季節の自動切替、Spout出力を初期状態で無効にしています。既存の展示用シーンは `Assets/Scenes/Main.unity` です。

反応が出ない場合は、入力の停止状態、Reaction Prefab、Position Updated の登録先を確認します。4画面が並ばない場合は、`Workspace` の **InputPreview** の Show All Screens（オフだと床のみ）と、Wall Texture・Side Texture を確認します。コンパイルエラーは **Window > General > Console** で確認できます。
