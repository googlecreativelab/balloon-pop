# Unity-SafeAreaCanvas

iPhone X/XSのSafeAreaの「実機起動時サイズ調整」と「Editorプレビュー」に対応したUnityアセット

[README(English)](README.md)

![](https://github.com/nkjzm/Unity-SafeAreaCanvas/blob/master/Docs/sample.gif)

# 動作環境

- Unity 2017.4~
- Unity 2018.x~

これ以下のバージョンをお使いの場合は[iOSSafeAreasPlugin](https://bitbucket.org/p12tic/iossafeareasplugin/src)のご使用をオススメします。

# 使い方

1. [Releases](https://github.com/nkjzm/Unity-SafeAreaCanvas/releases)から`SafeAreaCanvas.unitypackage`をダウンロードしてください。
1. プロジェクトに`SafeAreaCanvas.unitypackage`をインポートしてください。
1. シーンに`SafeAreaCanvas/Prefabs/SafeAreaCanvas.prefab`をドラッグしてください。

# Features

- iOS実機での自動サイズ調整
- Unityエディタ上でのプレビュー
  - **注:** iPadには非対応です
- StandaloneとAndroid向けのGameビューサイズ追加機能(読み込み時)
  - iPhone X/XS Landscape (2436x1125)
  - iPhone X/XS Portrait (1125x2436)

# 使用しているライブラリ

- [unity-GameViewSizeHelper](https://github.com/anchan828/unity-GameViewSizeHelper)

# LICENSE

[MIT](https://github.com/nkjzm/Unity-SafeAreaCanvas/blob/master/LICENSE)

# Author

Nakaji Kohki

Twitter: [@nkjzm](https://twitter.com/nkjzm)
