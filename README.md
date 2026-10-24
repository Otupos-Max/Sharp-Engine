# Sharp Engine

Uma engine de jogos em C#/.NET, em desenvolvimento.

## Estado atual

A primeira base inclui:

- host SDL2 com janela e renderer acelerado;
- ciclo de vida do jogo (`Initialize`, `Update`, `Render`, `Shutdown`);
- tempo de frame e input básico de teclado;
- demo que abre uma janela; pressione `Esc` para sair.

## Executar a demo

Requisitos: .NET 10 SDK e a biblioteca nativa SDL2 disponível no sistema.

```sh
dotnet run --project samples/SharpEngine.Demo
```

## Direção do projeto

A meta é evoluir por etapas verificáveis: primeiro um núcleo confiável e exemplos
pequenos; depois ferramentas e recursos 2D; em seguida renderização e recursos
3D, física, áudio e editor. Uma engine comparável a projetos AAA exige equipes,
anos de desenvolvimento e ferramentas próprias; esta base é o primeiro passo,
não uma tentativa de reproduzir essas engines de uma vez.