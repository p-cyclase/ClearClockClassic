# くりくろ（ClearClock）

なでしこ版 `legacy/ClearClock.nako` を、処理の対応関係を保ちながら C# に移植するプロジェクトです。

## 配布

GitHub Releases には、.NET ランタイムを同梱した単体の `ClearClock.exe` を添付します。利用者は追加のライブラリを導入せず、ダウンロード後に exe を実行するだけで使えます。

## 開発

```powershell
dotnet run
dotnet publish -c Release
```

公開用 exe は `release/ClearClock.exe` に生成されます。

## 旧版

`../legacy/` は当時の配布ファイルを変更せず保管する資料です。
