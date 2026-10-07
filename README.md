# YMMD-Camera

[YMM4](https://manju-noko.com/) 向け映像エフェクトプラグインです。  
MMD のカメラモーション（`.vmd`）を読み込み、YMM4 のカメラ位置・画角として再生します。

## できること

- カメラ VMD の読み込みと再生（ベジェ補間対応）
- タイムライン FPS と VMD FPS の換算（例: TL 60fps / VMD 30fps）
- MMD 座標 → YMM4 座標へのスケール・平行移動
- 左右反転
- VMD の画角（FOV）を YMM4 のパースへ反映
- 既存カメラエフェクトとの合成 / 置き換え

## 動作環境

- [YukkuriMovieMaker v4](https://manju-noko.com/)（Windows）
- .NET ビルド時は .NET 10 SDK と YMM4 のインストールが必要です

## インストール

1. [Releases](../../releases) から `.ymme` をダウンロードする
2. `.ymme` をダブルクリックするか、YMM4 のプラグイン管理からインストールする
3. YMM4 を再起動する

手動インストールの場合は、ビルド成果物の `MmdCameraPlugin.dll` を YMM4 の `plugins` フォルダへ配置してください。

## 使い方

1. タイムライン上のアイテム（グループ制御など）に映像エフェクト **「MMDカメラ」** を追加する
2. **カメラVMD** で `.vmd` ファイルを選択する
3. キャラの見た目サイズに合わせて **スケール** を調整する（初期値 `12.5`）
4. 必要に応じてオフセット・左右反転・FOV などを設定する

標準のカメラエフェクトの代わりに使うか、後段に挿して使えます。

## パラメータ

| グループ | 名前 | 説明 |
|---|---|---|
| VMD | カメラVMD | MMD のカメラモーション（`.vmd`） |
| タイミング | オフセット | タイムライン時刻から換算した VMD フレームへの加算（フレーム単位） |
| タイミング | VMD FPS | VMD 作成時のフレームレート（通常 30） |
| 変換 | スケール | MMD 座標 → YMM4 座標の倍率 |
| 変換 | オフセット X/Y/Z | 注視点の平行移動（YMM4 座標） |
| 変換 | 左右反転 | 注視点 X・ヨー・ロールを反転（初期 ON） |
| カメラ | FOVを適用 | VMD の画角をパースへ反映 |
| カメラ | 既存カメラに合成 | ON で手前のカメラ結果へ合成 / OFF で VMD カメラに置き換え |

## ビルド

YMM4 のインストールパスを `YMM4_PATH` で指定します（未指定時は `D:\V-Live\YukkuriMovieMaker_v4`）。

```powershell
# 通常ビルド
dotnet build -c Release -p:YMM4_PATH="C:\Path\To\YukkuriMovieMaker_v4"

# .ymme パッケージも作る場合
dotnet build -c Release -p:YMM4_PATH="C:\Path\To\YukkuriMovieMaker_v4" -p:PackYmme=true
```

成功すると `publish\MmdCameraPlugin.v.1.0.0.ymme` が生成されます。

## GitHub Actions（Release）

- ワークフロー（`.github/workflows/release.yml`）は `master` への push / Pull Request / `v*` タグ push で実行されます。
- GitHub Actions でビルドするには、リポジトリ Secrets に `YMM4_PATH` を設定してください（YMM4 本体と必要 DLL が存在する実パス）。
- `v1.0.0` のようなタグを push すると、`publish/*.ymme` と `MmdCameraPlugin.dll` が GitHub Release に添付されます。

## 注意

- AviUtl 向け出力には非対応です
- カメラキーのない VMD では効果が適用されません
- スケールはモデルの見た目サイズに合わせて調整してください

## ライセンス

[MIT License](LICENSE)
