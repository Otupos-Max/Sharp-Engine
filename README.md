# Sharp Engine

Engine de jogos própria em C#/.NET, em desenvolvimento. O objetivo é oferecer
uma base compreensível e extensível para jogos 2D e 3D, sem esconder o código do
jogo atrás de um editor ou de uma ferramenta externa.

## Requisitos e execução

- .NET 10 SDK
- SDL2 nativo compatível com o pacote `ppy.SDL2-CS`
- Para compilar o APK: .NET Android workload, Android SDK Platform 36,
  Android Build Tools 36.0.0 e JDK 17 ou 21

```sh
dotnet run --project samples/SharpEngine.Demo
```

## Gerar o APK Android

O app Android usa um backend Android Canvas e compartilha o núcleo de jogos
(cenas, componentes, input, transforms e API de desenho) com o host SDL2 desktop.
A demo Android aceita teclado e controles de toque: arraste no lado esquerdo para
movimento e toque no lado direito para `Space`.

Instale os requisitos e compile um APK de debug instalável:

```sh
dotnet workload install android
sdkmanager "platform-tools" "platforms;android-36" "build-tools;36.0.0"
export ANDROID_HOME="$HOME/Android/Sdk"
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export JAVA_HOME="/caminho/para/jdk-17-ou-21"
dotnet build samples/SharpEngine.Android/SharpEngine.Android.csproj \
  --configuration Debug \
  --framework net10.0-android \
  -t:Rebuild \
  -p:AndroidPackageFormat=apk \
  -p:AndroidBuildApplicationPackage=true
```

O APK assinado para instalação fica em
`samples/SharpEngine.Android/bin/Debug/net10.0-android/com.otupos.sharpengine-Signed.apk`.
Para criar um jogo Android próprio, implemente uma classe derivada de `Game` e
use-a no `MainActivity` do projeto Android.
O workflow [Android APK](.github/workflows/android-apk.yml) compila esse APK em
push, pull request para `main` ou execução manual e o publica como artifact
baixável da execução. É um APK de debug assinado para instalação/testes; ainda
não é um pacote de distribuição Google Play.

A demo mostra primitivas 2D, um cubo 3D em wireframe e movimento com `WASD` ou
setas. Pressione `Esc` para fechar.

## O que já existe

- Host SDL2 com janela, ciclo de vida e tempo de frame.
- Ciclo de vida do jogo: `Initialize`, `Update`, `Render` e `Shutdown`.
- Cenas com `GameObject`s e componentes, incluindo inicialização e destruição.
- Transformações 3D com posição, rotação e escala.
- Input de teclado com estado segurado, pressionado neste frame e solto neste
  frame.
- Primitivas 2D: limpar tela, retângulos preenchidos/contornados, linhas,
  círculos e texturas com recorte de regiões para spritesheets.
- Malhas trianguladas, câmera de perspectiva e desenho 3D em wireframe.
- Host Android com renderização Canvas, input de toque simples e build de APK.

## Estrutura mínima de um jogo

```csharp
public sealed class MeuJogo : Game
{
    public override void Initialize()
    {
        var jogador = new GameObject("Jogador");
        jogador.AddComponent(new MeuComponente());
        Scene.Add(jogador);
    }
}
```

Um componente pode sobrescrever `Start`, `Update`, `Render` e `Destroy`. O host
atualiza e desenha os objetos da cena automaticamente.

Texturas são carregadas pelo `RenderContext` durante a execução do jogo:

```csharp
private Texture2D? _sprite;

public override void Render(RenderContext context)
{
    // O caminho é relativo ao diretório atual do processo.
    _sprite ??= context.LoadTexture("assets/player.bmp");
    context.DrawTexture(_sprite, new Vector2(100, 100), new Vector2(64, 64));
}
```

O host desktop SDL aceita BMP sem SDL_image; o host Android usa os formatos
suportados pelo decodificador nativo `BitmapFactory`. No Android, coloque as imagens em
`samples/SharpEngine.Android/Assets` e carregue pelo caminho relativo aos assets.
Streams fornecidos a `LoadTexture(Stream)` continuam pertencendo ao chamador. O
host libera as texturas ao encerrar; elas também podem ser liberadas explicitamente
com `Dispose`.

## Testes

```sh
dotnet test SharpEngine.slnx -c Release
```

O teste de integração SDL é ignorado automaticamente em ambientes sem display.
Para executá-lo em Linux headless com SDL dummy video:

```sh
SDL_VIDEODRIVER=dummy SDL_RENDER_DRIVER=software dotnet test tests/SharpEngine.Tests/SharpEngine.Tests.csproj -c Release
```

## Limitações atuais

Esta é uma fundação funcional, ainda não uma engine completa. A renderização 3D
atual é wireframe; ainda faltam renderização 3D sólida com materiais, animação
2D, pipeline/editor de assets, áudio, física e assinatura de release própria.
O APK Android atual é um app de demonstração/debug com backend Canvas; não é um
editor nem um pacote assinado para distribuição na Google Play.

## Próximas etapas sugeridas

1. Adicionar animação 2D e ferramentas para spritesheets.
2. Introduzir renderização 3D sólida com profundidade e materiais.
3. Separar recursos em sistemas de assets e áudio, e ampliar o input mobile.
4. Criar um editor de cenas e melhorar os controles/configuração Android.
5. Preparar assinatura de release e distribuição para Google Play.