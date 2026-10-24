# Sharp Engine

Engine de jogos própria em C#/.NET, em desenvolvimento. O objetivo é oferecer
uma base compreensível e extensível para jogos 2D e 3D, sem esconder o código do
jogo atrás de um editor ou de uma ferramenta externa.

## Requisitos e execução

- .NET 10 SDK
- SDL2 nativo compatível com o pacote `ppy.SDL2-CS`

```sh
dotnet run --project samples/SharpEngine.Demo
```

A demo mostra primitivas 2D, um cubo 3D em wireframe e movimento com `WASD` ou
setas. Pressione `Esc` para fechar.

## O que já existe

- Host SDL2 com janela, ciclo de vida e tempo de frame.
- Ciclo de vida do jogo: `Initialize`, `Update`, `Render` e `Shutdown`.
- Cenas com `GameObject`s e componentes, incluindo inicialização e destruição.
- Transformações 3D com posição, rotação e escala.
- Input de teclado com estado segurado, pressionado neste frame e solto neste
  frame.
- Primitivas 2D: limpar tela, retângulos preenchidos/contornados, linhas e
- círculos e texturas BMP, incluindo recorte de regiões para spritesheets.
- Malhas trianguladas, câmera de perspectiva e desenho 3D em wireframe.

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

O carregador atual aceita BMP, sem dependência adicional de SDL_image. O host
libera as texturas ao encerrar; elas também podem ser liberadas explicitamente
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

Esta é uma base inicial, não uma engine completa nem um APK pronto. A renderização
3D atual é wireframe; ainda faltam renderização 3D sólida com materiais,
texturas/sprites e animação 2D, carregamento e gerenciamento de assets, áudio,
física, editor, empacotamento Android e testes automatizados. A demo roda no
desktop com SDL2; o suporte a Android precisa de um backend e integração nativa
específicos e ainda não foi implementado.

## Próximas etapas sugeridas

1. Adicionar um backend 2D com texturas, sprites e assets.
2. Introduzir um backend 3D com triângulos preenchidos, profundidade e materiais.
3. Separar recursos em sistemas de assets, áudio e input ampliado.
4. Criar testes automatizados para cenas, transforms e ciclo de vida.
5. Adicionar alvos de build e empacotamento Android depois de escolher e validar
   a integração nativa.