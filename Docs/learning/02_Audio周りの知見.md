# Audio周りの知見

`AudioManager.cs` 実装(2026-07)で得た知見のまとめ。

## 構成の全体像

```
AudioMixer (Master)
├── BGM   ← bgmSource (1本、クロスフェード)
├── SE    ← seSourcesプール (8本、ラウンドロビン)
└── UI    ← SEプールを共用、再生時にUI Groupへ切り替え
```

- Mixer/Group参照は `AudioMixerSettings`(ScriptableObject)にまとめ、RootLifetimeScopeプレハブ内のAudioManagerの `Mixer Settings` にInspectorで配線する。AudioManagerはAwakeでAudioSourceを生成した後に出力先Groupを設定する([RootLifetimeScope常駐パターン](08_RootLifetimeScope常駐パターン.md))。アセットは `AudioMixerSetupTool` が `Resources/Audio/` に出力するが、`Resources.Load` はもう使っていない

## AudioResource と AudioClip (Unity 6)

- `AudioSource.clip` の上位に `AudioSource.resource` が追加された。`AudioClip` も `AudioRandomContainer` も `AudioResource` として同じAPIで扱える
- **ループの挙動が異なる**: AudioClipは `AudioSource.loop` でループするが、Audio Random ContainerはContainer側の設定に従う。そのため `bgmSource.loop = resource is AudioClip;` という分岐が必要
- Audio Random Containerは足音・ヒット音のバリエーション再生(ピッチ/音量ランダム)に便利。SE差し替えなしで単調さを消せる

## dB と Linear の変換

Mixerの音量パラメータはdB。UIスライダー(0〜1)とは対数変換が必要:

```csharp
// Linear → dB: 振幅比なので 20 * log10(x)
decibel = Mathf.Log10(linear) * 20f;
// dB → Linear
linear = Mathf.Pow(10f, decibel / 20f);
```

- `linear = 0` は `log10(0) = -∞` になるので、-80dB(Unityの実質無音)へクランプする
- **`AudioMixer.SetFloat` はExposeしたパラメータ名でしか動かない**。Mixer側でパラメータを右クリック→Expose し、名前をコード側の定数(`MasterVolume` など)と一致させる。不一致でも例外ではなく戻り値falseなので気づきにくい

## AudioSourceプール運用

- SEは同時多発するので1本では鳴らし切れない(前の音が切られる)。N本のプール+ラウンドロビンが定石
- 空きがなければ最古の音を止めて奪う。「新しい音を優先」がゲームでは自然
- 2D/3Dは `spatialBlend`(0=2D、1=3D)で切り替え。プールを共用するため、**再生のたびに全設定を上書き**しないと前回の3D設定が残る事故が起きる

## その他

- BGMフェードは `Time.unscaledDeltaTime` で進める。ポーズ(`timeScale=0`)中でもフェードを完了させるため
- フェード中に次のBGM命令が来たら `CancellationTokenSource` で前の処理を中断してから始める。`GetCancellationTokenOnDestroy()` とリンクさせておくと破棄時の後始末も兼ねられる
