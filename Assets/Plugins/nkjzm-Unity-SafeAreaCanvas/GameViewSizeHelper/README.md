GameViewSizeHelper
==================

ScriptからGameViewSizeを作成、また設定するヘルパークラス

## API



### AddCustomSize

GameViewサイズを追加します

```cs
public static void AddCustomSize (GameViewSizeGroupType groupType, GameViewSize gameViewSize)
public static void AddCustomSize (GameViewSizeGroupType groupType, GameViewSizeType type, int width, int height, string baseText)
```

名前|説明
:---|:---
groupType|追加したいプラットフォーム
gameViewSize|サイズやアスペクト比など必要な情報を格納したGameViewSizeオブジェクト
type|アスペクト比かピクセルサイズか
width|幅
height|高さ
baseText|この設定の名前

### RemoveCustomSize

GameViewサイズを削除します

```cs
public static bool RemoveCustomSize (GameViewSizeGroupType groupType, GameViewSize gameViewSize)
public static bool RemoveCustomSize (GameViewSizeGroupType groupType, GameViewSizeType type, int width, int height, string baseText)
```

### Contains

設定しようとしているGameViewサイズが既に設定済みか確認します

```cs
public static bool Contains (GameViewSizeGroupType groupType, GameViewSize gameViewSize)
public static bool Contains (GameViewSizeGroupType groupType, GameViewSizeType type, int width, int height, string baseText)
```

### ChangeGameViewSize

指定のGameViewサイズに変更します<br>
必ずUnityEditorをgroupTypeと同じプラットフォームに指定しておかなければいけません

```cs
public static void ChangeGameViewSize (GameViewSizeGroupType groupType, GameViewSize gameViewSize)
public static void ChangeGameViewSize (GameViewSizeGroupType groupType, GameViewSizeType type, int width, int height, string baseText)
```