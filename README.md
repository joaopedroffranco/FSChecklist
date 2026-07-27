# FSChecklist

Assistente local de checklist por voz para o Microsoft Flight Simulator 2024.
Ele simula o fluxo *challenge and response*: o copiloto lê o item, o piloto
responde por push-to-talk, e o software só avança quando a resposta corresponde
ao JSON.

## Executar o aplicativo

Para usar a versao compilada, abra `dist\FSChecklist.exe`.

Para gerar ou atualizar o executavel:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

O build usa o compilador .NET Framework que ja acompanha o Windows e copia as
checklists para `dist\checklists`.

## Executar a versao PowerShell

Requisitos:

- Windows 10 ou 11
- Windows PowerShell 5.1
- pacote de reconhecimento de voz em português (Brasil) instalado no Windows
- microfone configurado como dispositivo de entrada padrão

Dê dois cliques em `Start-FSChecklist.cmd`. Se o Windows bloquear o arquivo,
use **Propriedades → Desbloquear**.

1. Escolha a aeronave e a checklist.
2. Clique em **INICIAR**.
3. Segure o botão azul ou `F9`, fale a resposta e solte.
4. Se a resposta não estiver no JSON, o item permanece pendente.

`F9` funciona enquanto a janela do FSChecklist está em foco. Um hotkey global
será uma evolução posterior, para não capturar teclas do simulador sem
configuração explícita.

## Adicionar checklists

Coloque arquivos `.json` em `checklists/`. O aplicativo nunca cria itens, muda
a ordem ou usa IA para decidir o próximo passo.

O formato original do projeto também é aceito. Quando `items` contém apenas
textos e `rules.acceptAnyAnswer` é `true`, qualquer resposta reconhecida confirma
o item:

```json
{
  "aircraft": "Fenix A320",
  "rules": { "acceptAnyAnswer": true },
  "checklists": [
    {
      "id": "before_start",
      "name": "Before Start",
      "items": ["Parking Brake", "Navi Lights"]
    }
  ]
}
```

Para exigir respostas específicas, use itens estruturados:

```json
{
  "schemaVersion": 1,
  "aircraft": "B777",
  "language": "pt-BR",
  "checklists": [
    {
      "id": "before-start",
      "name": "Before Start",
      "completedCallout": "Before start checklist complete",
      "items": [
        {
          "id": "parking-brake",
          "callout": "Parking brake",
          "responses": ["set", "acionado"]
        }
      ]
    }
  ]
}
```

O arquivo `checklists/a320.json` contém a checklist fornecida pelo proprietário
do projeto.

## Decisões de segurança

- sequência e conteúdo são controlados exclusivamente pelo JSON;
- resposta não reconhecida não avança;
- baixa confiança do reconhecimento pede nova tentativa;
- a tela sempre mostra o item atual, a resposta esperada e o progresso;
- botão **VOLTAR** permite corrigir o item anterior;
- todo o áudio permanece local no computador.

Este projeto é para simulação. Não deve ser usado em operações aeronáuticas reais.
