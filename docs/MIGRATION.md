# なでしこ版との対応

| なでしこ版 | C# 版 |
| --- | --- |
| 初期設定（3–46行） | `Window_Loaded` / `LoadSettings` |
| 無限ループ（48–72行） | `ClockLoop` |
| 枠描画（74–88行） | `DrawFrame` |
| サイズ変更（90–123行） | `SizeLarge` ほか |
| 太さ変更（125–158行） | `WidthThick` ほか |
| 色変更（160–166行） | `ColorChange` |
| 表示切替（168–184行） | `FrontChange` ほか |
| チェック調整（186–203行） | `CheckAdjustment` |
| 終了処理（205–215行） | `ExitProcess` / `Window_Closing` |

描画は WPF の Canvas に置き換えているが、針・目盛りの角度と長さ、設定値、メニュー構造は旧版に沿う。
