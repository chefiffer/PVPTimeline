# PvP Timeline

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [한국어](README.ko-KR.md) | [Русский](README.ru-RU.md) | **Português (Brasil)** | [Español](README.es-ES.md) | [ไทย](README.th-TH.md) | [Tiếng Việt](README.vi-VN.md) | [日本語](README.ja-JP.md) | [Deutsch](README.de-DE.md) | [Français](README.fr-FR.md)

Uma linha do tempo pessoal e em tempo real das ações PvP em FINAL FANTASY XIV.

Veja uso de ações, conjuração, recarga global (GCD), bloqueio de animação, suas animações e retorno dos efeitos em uma única linha. Assim, você pode revisar quando usou uma ação, quando o efeito chegou e quais habilidades foram intercaladas.

![Prévia do PvP Timeline no jogo](assets/pvp-timeline-preview.gif)

> Esta é uma versão de teste. Atualizações do jogo podem afetar a observação e a exibição, e algumas etapas ainda podem não ter retorno. A linha mostra observações locais, não uma reconstrução exata da detecção interna do servidor. Ela não automatiza ações.

## Como funciona

![Observação local e fluxo de mensagens](assets/how-pvp-timeline-works.png)

O diagrama separa as mensagens entre cliente e servidor das informações de tempo observadas localmente. O PvP Timeline registra solicitações de ação aceitas, estados visíveis de conjuração e recarga, bloqueio e reprodução de animação, mensagens de efeito recebidas e instantâneos dos seus estados. As cores têm o mesmo significado da exibição padrão.

O plugin não envia pacotes de combate adicionais nem acessa decisões internas do servidor. Um clique que não se torna solicitação de ação não pode ser observado. O horário de retorno é quando o cliente local recebeu a mensagem, incluindo atrasos de rede, servidor e cliente.

## Recursos

- **Faixas separadas:** ícones grandes abaixo para magias, técnicas de arma e ações especiais; ícones menores acima para habilidades, em até duas linhas.
- **Tempos distintos:** GCD, conjuração, bloqueio e reprodução de animação são registrados separadamente.
- **Retorno dos efeitos:** um losango rosa marca dano, cura, aplicação de estado e outros retornos; acertos críticos e diretos iluminam suavemente o ícone original.
- **Efeitos atrasados:** detonações automáticas e etapas posteriores compatíveis são ligadas ao uso original ou exibidas com ícones translúcidos, sem inventar outro clique.
- **Seus estados:** histórico compacto de benefícios selecionados e penalidades observadas.
- **Visual configurável:** bloqueio com passagem de cliques, layout horizontal/vertical, rolagem inversa, cores, opacidade, tamanhos e escala geral.
- **Pausar e revisar:** congele a tela ou limpe os registros.

A observação é limitada às áreas compatíveis de **Wolves' Den Pier, Crystalline Conflict, Frontline e Rival Wings**. Ela não funciona em áreas PvE comuns; mapas novos exigem atualização da lista.

## Primeiros passos

### Instalação

É necessário um ambiente para plugins da **Dalamud API 15**. Use uma versão compatível com o jogo e o Dalamud atuais.

1. Instale e inicie o XIVLauncher com o Dalamud ativado.
2. Abra **Dalamud Settings → Experimental**.
3. Adicione este repositório:

   ```text
   https://raw.githubusercontent.com/chefiffer/dalamud-plugins/main/repo.json
   ```

4. Procure **PVPTimeline** no instalador e instale.
5. Abra as configurações com:

   ```text
   /pvptimeline
   ```

6. Entre em Wolves' Den Pier ou em um mapa PvP compatível e use ações.

### Configuração inicial

Desative **Combat only** e teste em um boneco de treino em Wolves' Den Pier:

1. Desbloqueie a sobreposição em **Display**, mova e redimensione.
2. Em **Layout**, ajuste tamanhos de GCD/habilidades, posições e espessuras.
3. Ative **Locked** para permitir que os cliques atravessem a linha.

Uma linha bloqueada não pode ser clicada diretamente. Use `/pvptimeline`; desbloqueada, ela também aceita clique direito.

## Lendo a linha do tempo

### Ícones e tempos

`0s` é o instante atual e valores negativos indicam o passado. A grade e os rótulos ficam fixos enquanto ações e barras se movem para o histórico.

A faixa inferior não significa que todas as ações usam GCD: alguns Limit Breaks e ações especiais também aparecem nela. A barra azul de GCD só é desenhada quando uma recarga compartilhada é realmente observada.

| Aparência padrão | Significado |
| --- | --- |
| Barra azul | GCD observado |
| Barra verde | Intervalo de conjuração observado localmente |
| Barra amarela curta | Bloqueio de animação separado do GCD |
| Barra roxa | Reprodução observada da sua animação |
| Losango rosa cheio | Retorno direto: dano, cura ou estado; em movimentos verificados pode ser confirmação do servidor |
| Losango rosa vazio | Efeito visual ou alteração de medidor; sozinho não prova dano ou cura |
| Ícone translúcido | Conjuração não confirmada ou efeito real sem uso manual correspondente; não significa outro clique |
| Brilho do ícone | Acerto crítico, direto ou crítico direto vindo de mensagens reais |
| Barra cinza fina (desativada por padrão) | Recarga independente da ação, não o GCD |

Todas as cores podem ser alteradas. Ações em múltiplos alvos podem ter resultados diferentes; o ícone combina as marcas observadas. Um brilho crítico direto não significa que todos os alvos receberam esse resultado.

Recuperate e Shadowbringer também têm **barras azuis curtas e independentes** na faixa de habilidades. Continuam sendo recargas próprias, não ações GCD.

### Por que uma barra roxa ou losango às vezes não aparece?

As barras roxas dependem da observação real das suas animações. Falhas de observação, associação incerta, pausa ou substituição podem removê-las ou encurtá-las. Um marcador de substituição só aparece quando outra ação comprovadamente sobrescreveu a animação; o fim normal não cria losango laranja.

Losangos rosas também exigem retorno real. Se ele não chegou, não pode ser associado ou pertence a uma etapa sem suporte, o plugin não inventa um marcador pela descrição.

**A ausência de barra ou marcador não prova que a ação falhou.** GCD ou bloqueio não substituem o tempo de reprodução.

## Lendo as linhas de estado

Elas mostram principalmente **estados realmente presentes no seu personagem**, vindos de você, aliado ou inimigo. O uso de uma ação por um aliado, sozinho, não prova que você recebeu o efeito.

- O ícone inicial se move com o tempo e não é duplicado na borda esquerda antes de chegar.
- Ao chegar, entra na fila; se o tempo restante for conhecido, mostra até dois dígitos, sem unidade ou zero.
- Estados que terminam antes ficam mais à direita.
- **Um pequeno sublinhado vermelho marca o ponto final.** Ele pode aparecer na área futura antes do fim e não significa que o estado já acabou.
- Após terminar, o ponto se move ao passado e o segmento desaparece ao encontrar o início.

Fontes diferentes podem ser separadas. Renovações, acúmulos e remoções seguem a lista real; o plugin não inventa acúmulos pela quantidade de usos.

Nem todo estado é exibido. O plugin filtra benefícios de jobs revisados e certos indicadores de seleção, prontidão e continuação. Não é uma base completa; abra uma issue para sugerir outro efeito.

**Somente estados e contagens confirmáveis são mostrados.** Sem tempo restante legível, não há contagem; condições como “em combate” ainda não são avaliadas separadamente.

## Configurações comuns

| Local | Opções |
| --- | --- |
| Topo | Idioma, escala, pausar/retomar, limpar |
| Display | Ativar, passagem de cliques, somente combate, ocultar em cenas, estados, observação nativa, orientação, fundo |
| Layout | Tamanhos, altura/espaçamento, posições e espessuras, conjuração/animação |
| Tracks | Visibilidade e cores, recargas curtas, habilidades, ataques automáticos |
| Time | Pixels por segundo, histórico, futuro, grade e subdivisões |
| Feedback | Cores do brilho, substituição de animação, opacidade dos efeitos |

Digite valores ou use a roda do mouse sobre o campo. A escala geral aceita **0,2–2,0×**, incluindo o texto.

A interface oferece chinês simplificado/tradicional, inglês, alemão, francês e japonês. Inicialmente segue o idioma do Dalamud, com escolha manual. Nomes usam recursos multilíngues e podem voltar ao inglês, idioma do cliente ou ID. Nomes de jogadores não são traduzidos. `ui-localization.txt`, ao lado da DLL, controla apenas textos da interface; edite e recarregue o plugin.

## Perguntas frequentes

### A linha não aparece

Confira se está em área PvP compatível, se o plugin está ativo e as opções **Combat only** e **Hide in cutscene**. Consulte **Diagnostics**. Atualizações podem exigir correção de compatibilidade.

### Por que nem todo clique repetido aparece?

São mostrados usos observáveis, não cliques físicos. Cliques sem solicitação do jogo são invisíveis e tentativas malsucedidas não criam uma sequência de ícones.

### Por que dano automático usa ícone translúcido?

Um efeito causado por você pode chegar sem uso manual correspondente. Uma detonação automática causa dano real sem outro clique. Etapas ligadas com segurança atualizam a ação original; outros efeitos reais podem ficar translúcidos.

### O horário é o da detecção do servidor?

Não. É a recepção pelo cliente e inclui todos os atrasos. A animação também não é o instantâneo do servidor, e o fim da conjuração não determina o primeiro momento possível de movimento.

### Ele envia dados ou salva partidas completas?

Não automaticamente. Mantém registros recentes limitados na memória e os limpa ao mudar de área ou apagar. Diagnósticos só vão para a área de transferência ao clicar e podem conter IDs e horários; revise antes de compartilhar.

## Limites e cuidados

- A janela de associação é de no máximo **14 segundos**; retornos atrasados ou ambíguos não são forçados.
- O histórico é limitado a 256 entradas e 120 segundos; os estados também têm limites.
- A observação se concentra no seu personagem e não reconstrói totalmente animações do alvo, projéteis ou efeitos no chão.
- A observação nativa usa interfaces internas; atualizações podem quebrá-la ou afetar a estabilidade. Desativá-la não remove necessariamente todos os pontos de observação.
- É um plugin não oficial, sem garantia de segurança da conta ou ausência de falhas. Respeite as regras do jogo e da plataforma.

## Relatando um problema

Informe versão, região/versão do jogo, idioma, job e ação, acionamento manual/automático, resultado esperado/real e passos. Inclua captura ou GIF, diagnóstico de efeitos para associações e logs do Dalamud sem dados privados para falhas de carregamento. Não compartilhe tokens, senhas ou logs pessoais sem relação.

## Agradecimentos

- [ActionTimelineEx](https://github.com/Ted-Jin-Lab/ActionTimelineEx)
- [ActionTimelineReplacement](https://github.com/Loskh/ActionTimelineReplacement)

Esses projetos inspiraram a apresentação, o bloqueio e a personalização. PvP Timeline não é uma versão oficial. Ícones e materiais pertencem aos respectivos titulares.

## Licença

PvP Timeline é licenciado sob a GNU Affero General Public License v3.0 (AGPL-3.0). Consulte [LICENSE](https://github.com/chefiffer/PVPTimeline/blob/main/LICENSE).

Os avisos do projeto original sob MIT são preservados em [THIRD-PARTY-NOTICES.txt](https://github.com/chefiffer/PVPTimeline/blob/main/THIRD-PARTY-NOTICES.txt).
