# かげぴったんとは

『かげぴったん』は2019年3月16-17日に開催された【未来の福岡の運動会】で制作された種目です。

[かげぴったんの競技風景](https://www.youtube.com/watch?v=gOzXL_HBkOA&t=52)(52-58秒)  
（NaoChannel様のYoutubeより。NaoChannnel様ありがとうございます！）

このリポジトリは『かげぴったん』を公開するためのリポジトリです。  
制作に使用したUnityプロジェクトとビルド済みのパッケージを公開しています。  

# ビルド済みパッケージの入手方法

リリースページからパッケージのzipファイルを入手できます。  
zipファイルをダウンロードして展開し、Kagepittan.exeを実行すると『かげぴったん』を遊べます。  
遊び方の詳細は同梱のREADME.txtファイルを参照してください。  

https://github.com/KzoNag/Kagepittan/releases/tag/v0.1

# Unityプロジェクトの使用方法

## 制作環境

* Windows10 64bit
* Unity2018.2.18f1
* OpenCV plus Unity v1.7.1

## Unityプロジェクトの入手

リポジトリをクローンするかzipファイルをダウンロードしてUnityで開きます。

## "OpenCV plus Unity"のインポート

『かげぴったん』では画像処理に無料アセットの"OpenCV+Unity"を使用しています。  
UnityのアセットストアでOpenCV plus Unityを入手しプロジェクトにインポートしてください。  

https://assetstore.unity.com/packages/tools/integration/opencv-plus-unity-85928

## 実行方法

Assets/Scenes/Kagepittanシーンを開いて実行するとUnity上で『かげぴったん』をプレイできます。  
MainオブジェクトのKagepittanコンポーネントとAssets/Data/configオブジェクトで各種設定を調整できます。  

## クリア音の追加

パッケージ版ではお題をクリアしたときに効果音が流れますが、  
再配布禁止のフリーの効果音素材を使用しているためリポジトリからは除外しています。  
クリア音を追加したい場合はKagepittanコンポーネントの"Ok Clip"に任意のAudioClipを設定してください。  

パッケージ版では効果音ラボ様の"正解1"の一部を切り出して使用しています。  
https://soundeffect-lab.info/sound/anime/
