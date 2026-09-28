# くりくろ設計書

## 目的と範囲

くりくろは、透明な枠なしウィンドウにアナログ時計を表示する Windows 向けアクセサリである。旧版 `legacy/ClearClock.nako` の機能を C# / WPF に移植し、設定・操作・描画の対応関係を保つ。

本バージョンは旧版互換を対象とする。タスクトレイ常駐、アラーム、複数時計などの新機能は含めない。

## 実行・配布

| 項目 | 内容 |
| --- | --- |
| 実装言語 | C# |
| UI | WPF（色選択に Windows Forms の標準ダイアログを利用） |
| 対象 | Windows x64 |
| 配布物 | 専用フォルダを含む ZIP ファイル |
| ランタイム | .NET ランタイムを exe に同梱 |
| 外部パッケージ | なし |

`dotnet publish -c Release` は `release/ClearClock.exe` を生成する。`scripts/package-release.ps1` は exe と利用者向け `README.txt` を専用フォルダへまとめ、`dist/ClearClock-vX.Y.Z-windows-x64.zip` を生成する。通常利用者は GitHub Releases からこの ZIP を入手して展開する。

ZIP 内には初期状態の `ClearClock.ini` を含めない。初回終了時に exe と同じ専用フォルダへ生成される。旧版から設定を引き継ぐ利用者は、旧 `ClearClock.ini` をこのフォルダへコピーしてから起動する。

## 構成

| ファイル | 責務 |
| --- | --- |
| `App.xaml` / `App.xaml.cs` | アプリケーションの開始設定 |
| `MainWindow.xaml` | 枠なし透明ウィンドウ、Canvas、右クリックメニュー |
| `MainWindow.xaml.cs` | 状態管理、描画、メニュー処理、設定保存 |
| `ClearClock.csproj` | WPF と単体 exe 配布のビルド設定 |
| `Directory.Build.props` | 内部生成物を `.build/` に隔離する設定 |

## 状態

`MainWindow` が次の状態を保持する。

| 状態 | 初期値 | 用途 |
| --- | --- | --- |
| `size` | 200 | 時計ウィンドウの縦横サイズ（px） |
| `lineWidth` | 3 | 枠線・針・目盛りの太さ（px） |
| `color` | 黒 | 枠線・針・目盛りの色 |
| `Front.IsChecked` | true | 常に手前に表示するか |
| `Sec.IsChecked` | false | 秒針を表示するか |
| `Scale.IsChecked` | true | 12本の目盛りを表示するか |

サイズの最小値は 100 px、線幅の最小値は 2 px とする。白が選ばれた場合は旧版と同じく `#FFFFFE` に置き換える。

## 処理の流れ

```text
起動
  └ Window_Loaded
      ├ ClearClock.ini を読む（なければ初期値）
      ├ CheckAdjustment でメニューと最前面状態を同期
      ├ DrawFrame で枠と目盛りを描画
      └ ClockLoop を毎秒実行

終了
  └ Window_Closing
      └ 設定と表示座標を ClearClock.ini に保存
```

`ClockLoop` は現在時刻から針の角度を算出し、枠を描き直した後に時針・分針・必要なら秒針を描く。旧版の1秒待機は WPF の `DispatcherTimer` で置き換える。

## 描画

描画先は `ClockCanvas` である。`DrawFrame` は Canvas を空にして円形の枠と必要なら目盛りを追加する。`DrawHand` は中心から指定角度・長さの線を追加する。

| 針 | 長さ |
| --- | --- |
| 時針 | `size × 0.25` |
| 分針 | `size × 0.35` |
| 秒針 | `size × 0.40` |

時針は分、分針は秒を考慮して滑らかな角度にする。秒針の線幅は `max(1, lineWidth - 1)` とする。

## 操作

| 操作 | 処理 |
| --- | --- |
| 左ドラッグ | `DragMove` によりウィンドウを移動 |
| 右クリック | 設定用の `ContextMenu` を表示 |
| 大・中・小 | サイズを 300 / 200 / 100 に変更 |
| その他（サイズ） | 100 以上の整数を入力 |
| 太・中・細 | 線幅を 5 / 3 / 2 に変更 |
| その他（太さ） | 2 以上の整数を入力 |
| 色の変更 | Windows 標準の色選択ダイアログを表示 |
| 最前面・秒針・目盛り | チェック状態を切り替える |
| 終了 | 設定を保存して終了 |

## 設定ファイル

設定ファイルは exe と同じフォルダの `ClearClock.ini` である。テキストの各行に次の順序で保存する。

| 行 | 内容 | 例 |
| --- | --- | --- |
| 1 | サイズ | `200` |
| 2 | 線幅 | `3` |
| 3 | 色 | `#000000` |
| 4 | 最前面表示 | `1` または `0` |
| 5 | 秒針表示 | `1` または `0` |
| 6 | 目盛り表示 | `1` または `0` |
| 7 | ウィンドウ X 座標 | `120` |
| 8 | ウィンドウ Y 座標 | `80` |

設定ファイルがない場合や、読み取れない値がある場合は各項目の初期値を使用する。

### 旧版 ini の読込互換性

旧なでしこ版も設定値の行順は同じだが、色は Windows の `COLORREF` 整数値（例: `16448`）で保存する。`ReadColor` は、この整数を赤・緑・青の各バイトへ分解して読み込む。現行版の `#RRGGBB` 形式も読み込める。

終了時の保存は常に現行版の `#RRGGBB` 形式で行う。新版から旧版へ戻す互換性は持たない。

## 旧版との対応

詳細な対応表は [MIGRATION.md](MIGRATION.md) に置く。主な手続きは次のように対応する。

| なでしこ版 | C# 版 |
| --- | --- |
| 初期設定 | `Window_Loaded` / `LoadSettings` |
| 無限ループ | `ClockLoop` |
| 枠描画 | `DrawFrame` |
| チェック調整 | `CheckAdjustment` |
| 終了処理 | `ExitProcess` / `Window_Closing` |
