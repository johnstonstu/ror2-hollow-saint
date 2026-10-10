<p align="center"><a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/README.md">English</a> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/README.zh-CN.md">简体中文</a> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/README.ru.md">Русский</a> | <b>Português (BR)</b></p>

> Esta página foi traduzida por máquina. Correções são bem-vindas: [abrir uma correção de tradução](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md).

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/banner.jpg" alt="Santo Oco, um sobrevivente da tempestade para Risk of Rain 2" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=4fd2ff&style=for-the-badge" alt="Versão no Thunderstore"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.downloads&label=downloads&color=7b5cff&style=for-the-badge" alt="Downloads no Thunderstore"></a>
  <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="Licença MIT"></a>
</p>

<h3 align="center">Um ícone devocional rachado que só responde à tempestade.</h3>

<p align="center">Encadeie relâmpagos por um bando, prenda a maior ameaça com uma lança de relâmpago e guarde cada Eletrocussão como uma Carga Estática.<br>Depois gaste o estoque em um Orbe Oco, uma Nuvem Trovejante, um feixe do Olhar ou uma coroa do Circuito Aberto.</p>

<p align="center"><a href="#o-kit"><b>Habilidades</b></a> · <a href="#como-a-tempestade-funciona"><b>A tempestade</b></a> · <a href="#combinações"><b>Combinações</b></a> · <a href="#instalação-e-opções"><b>Instalar</b></a> · <a href="https://github.com/johnstonstu/ror2-hollow-saint/issues"><b>Feedback</b></a> · <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/CHANGELOG.md"><b>Changelog</b></a></p>

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/gaze-hero.webp" alt="Olhar do Oco: as cargas giram em espiral para dentro da coroa, e então o feixe abre com uma explosão" width="100%"></p>

> **Acesso antecipado:** o Santo Oco ainda está sendo ajustado, então espere mudanças de balanceamento e um bug ocasional. As descrições das habilidades no jogo sempre mostram os números das suas configurações atuais.

## O kit

| | Habilidade | Slot | Entrada | O que faz | Números principais |
|:-:|---|---|---|---|---|
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-discharge.png" width="40" alt=""> | **Prece Atendida** | Passiva | Automática | Seus acertos acumulam Estática nos inimigos. Estática cheia Eletrocuta e o arco salta para os vizinhos. Cada Eletrocussão armazena uma Carga Estática. | O estoque guarda até cinco. As cargas nunca se dissipam sozinhas. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-arc_bolt.png" width="40" alt=""> | **Seta em Arco** | Primária | Primária | Uma seta rápida que salta por um bando. A sua pressão constante e a forma mais rápida de espalhar Estática por um grupo. | 171% por acerto. Até 4 inimigos por seta. Um disparo a cada 0,5 s. Proc 1,0. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-conduit_spear.png" width="40" alt=""> | **Lança da Tempestade** | Secundária | Segure para carregar, solte para arremessar | Crava-se no que atinge e depois explode em uma cúpula de relâmpagos. Acumula o dobro de Estática em inimigos preparados. | Carga completa em 2 s. Explosão de 3 m a 10 m. Recarga 5 s. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-arc_step.png" width="40" alt=""> | **Passo em Arco** | Utilitária | Utilitária | Teleporte-se uma curta distância em qualquer direção, até no ar. Olhe para cima para subir e salte para fora de um passo para conservar o impulso. | 2 cargas. Recarga de 5 s. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-gaze.png" width="40" alt=""> | **Olhar do Oco** | Especial | Segure para reunir cargas, solte para começar. Um toque pula a carga. | Suba e canalize um feixe perfurante. O feixe abre disparando tudo o que você reuniu em uma única grande explosão, e então acumula foco em um alvo. | Feixe de 7 s, 90 m. Explosão inicial de 400% por carga, 6 m de largura mais 2 m por carga (até 20 m). Foco até +100% de dano ao longo de três segundos. Recarga 12 s. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-open_circuit.png" width="40" alt=""> | **Circuito Aberto** | Especial alternativa | Segure a Especial para alimentar a coroa com pelo menos uma carga e solte | Uma coroa que golpeia enquanto você continua lutando. | 10 s, raio de 8 m. Um pulso a cada 0,5 s com 72%. 1, 3 ou 5 cargas dão 1x, 1,5x ou 2x de frequência dos pulsos. Recarga 8 s após o fechamento da coroa. |
| | **Orbe Oco** | Secundária alternativa | Toque, ou segure por mais de 0,5 s para reunir cargas | Uma grande bola de relâmpago que salta por todo o bando. Lançamento gratuito. | Gratuito: 378% por acerto, 3 acertos, 0,9 m. Cinco cargas: 738%, 8 acertos, 1,5 m. Alcance 70 m, saltos de 18 m a 36 m. Recarga 6 s. |
| | **Nuvem Trovejante** | Terceira Especial | Toque, ou segure a Especial para despejar cargas e solte sobre o alvo | Tempestade persistente que atinge todo inimigo sob ela. Gratuita. | Um raio a cada 0,75 s por 4 s (9 s com cinco cargas). De 149% a 261% por raio. Raio de 12 m a 30 m. Mira até 80 m. Recarga 10 s. |

**Botões segurados:** mantenha a Primária pressionada enquanto o Olhar ou a Nuvem Trovejante reúne cargas para continuar disparando Seta em Arco. Esses lançamentos só reúnem as cargas que estavam no estoque ao começar; as novas ficam guardadas para depois. A Lança da Tempestade pausa as setas durante a carga; elas voltam após o arremesso se a Primária continuar pressionada.

### Notas das habilidades

- **Lança da Tempestade:** uma carga completa também chama um raio do céu. Um arremesso totalmente carregado com o estoque de Estática cheio vira um **Raio** que prepara o bando ao redor. Arremessos rápidos nunca mexem no estoque.
- **Orbe Oco:** alvos novos vêm primeiro, depois as revisitas; cada revisita ao mesmo inimigo mantém 75% do dano anterior naquele alvo. A assistência favorece a mira e respeita paredes. Cada Pente Reserva soma mais um acerto. Sem mais nada ao alcance, o orbe se fixa no alvo e descarrega ali os acertos restantes; ao se esgotar, explode numa pequena área (3 m, +0,8 m por carga) com 60% do dano do acerto. Todo acerto prepara Estática. A Utilitária cancela a reunião e devolve as cargas. Circuito Aberto lança acima da cabeça, mantendo a Primária disponível.
- **Nuvem Trovejante:** raio de 12 m sem cargas, 16 m com uma e 30 m com cinco. Pode ser posicionada numa área vazia, e voadores e inimigos em saliências também são atingidos. Os raios deixam os alvos Eletrizados e preparados, e vêm mais rápido com velocidade de ataque (até o dobro da frequência). A Utilitária cancela a reunião. Pressione a Especial de novo enquanto chove para encerrar a tempestade mais cedo e recuperar até metade da recarga, proporcional ao tempo restante. A recarga começa após o fim da tempestade.
- **Olhar do Oco:** você recebe menos dano e ignora repulsão enquanto reúne e canaliza. O foco é mantido durante um deslize de meio segundo e diminui depois disso. Especial ou Cancelar interface encerra o feixe mais cedo, e a Utilitária sai direto para o seu Passo em Arco. A explosão inicial prepara o que atinge, então o feixe termina o serviço.
- **Circuito Aberto:** é preciso pelo menos uma carga para abrir a coroa. Pulsos extras não aceleram a Estática. Inimigos que ficam dentro por três segundos levam um choque extra. **Circuito Fechado:** cada Eletrocussão a até 12 m devolve uma carga colocada; as que ainda estiverem na coroa quando ela se fecha explodem da coroa como uma única onda de choque de 12 m, com 150% de dano por carga restante, preparando o que atinge, ou voltam ao seu estoque se não houver inimigo na onda.

## Como a tempestade funciona

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/storm-loop.png" alt="O ciclo da tempestade: finalizar, Eletrocutar, guardar, gastar, preparar" width="100%"></p>

Os **finalizadores** rendem Cargas Estáticas, os **gastadores** as usam, e tudo o que um gastador atinge fica **preparado** para o próximo finalizador.

1. **Estática.** Acertos elegíveis carregam o inimigo. Acertos maiores, críticos e itens de alto proc carregam mais rápido. Ela se esvai se você parar de acertar.
2. **Eletrocussão.** Com Estática cheia, o inimigo é sacudido (exceto chefes) e fica **Eletrizado**, recebendo dano extra por um instante. O arco carrega os inimigos próximos também. Cada Eletrocussão guarda uma **Carga Estática**, até cinco.
3. **Gastar e preparar.** Toda habilidade que gasta cargas também prepara o que atinge, deixando-o com até 95% de Estática, sem nunca encher a barra, então um gastador jamais paga a si mesmo.
4. **Finalizar.** A Seta em Arco, a Lança da Tempestade, o feixe do Olhar e os pulsos do Circuito Aberto levam os inimigos preparados ao limite. Um bando preparado Eletrocuta em um ou dois golpes e seu estoque se reabastece.

- **Finalizadores:** Seta em Arco, Lança da Tempestade, feixe do Olhar, pulsos do Circuito Aberto.
- **Gastadores:** Orbe Oco (segurado), Nuvem Trovejante, explosão inicial do Olhar, Raio com estoque cheio. Olhar, Orbe, Nuvem e Circuito Aberto gastam a quantidade que você reuniu.
- **Preparadores gratuitos:** Orbe Oco (toque) e Nuvem Trovejante (gratuita) preparam sem gastar nada: a forma mais fácil de iniciar o ciclo.
- **Retorno:** o Circuito Aberto devolve as cargas colocadas a partir das Eletrocussões a até 12 m.

Nada leva suas cargas a menos que você tenha segurado uma habilidade para isso. Uma proteção leve de ganho (2 cargas por segundo, ajustável) impede que bandos enormes do fim de jogo inundem o estoque.

## Combinações

A Secundária decide como você começa e termina o ciclo; a Especial decide como você o gasta.

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/storm-builds.png" alt="Seis combinações: Lança da Tempestade ou Orbe Oco com Olhar, Circuito Aberto ou Nuvem Trovejante" width="100%"></p>

| Combinação | Equipamento | Como funciona | Atenção |
|---|---|---|---|
| **Evocador de Tempestades** | Lança da Tempestade + Olhar | A Seta em Arco enche o estoque. A explosão inicial do Olhar prepara, o feixe focado e suas lanças finalizam. | Lança e Olhar dividem um só estoque; só arremessos totalmente carregados o gastam. |
| **Lanceiro** | Lança da Tempestade + Circuito Aberto | Sob a coroa a Lança da Tempestade carrega muito mais rápido. Arremesse em inimigos preparados enquanto os pulsos cuidam do resto. | Fique a até 12 m da luta para receber reembolsos. |
| **Cerco** | Lança da Tempestade + Nuvem Trovejante | Solte uma nuvem sobre um grupo distante para prepará-lo e Eletrizá-lo, depois finalize à distância com lanças e saltos da Seta em Arco. | Uma tempestade gratuita ainda prepara; guarde cargas para a grande ou para uma lança totalmente carregada. |
| **Vidente** | Orbe Oco + Olhar | Orbes gratuitos preparam o bando, e então a explosão inicial do Olhar e o feixe cobram a conta. | Orbes nunca finalizam sozinhos; quem finaliza são o feixe e a Seta em Arco. |
| **Condutor** | Orbe Oco + Circuito Aberto | Orbes gratuitos preparam, coloque cargas na coroa e lute dentro dela. Os pulsos finalizam e os reembolsos mantêm o estoque cheio. A combinação mais autossustentável. | Orbes lançados acima da cabeça mantêm a Primária livre durante a coroa. |
| **Tempestade** | Orbe Oco + Nuvem Trovejante | Nuvem e orbes preparam tudo, e os saltos da Seta em Arco fazem todo o trabalho de finalizar. Alto risco, alta recompensa. | Tempestades gratuitas mantêm o ciclo; tempestades carregadas esvaziam o estoque rápido. |

## Aparências

Seis aparências, cada uma com seu próprio nimbo e cor de relâmpago. **Voto Carmesim** é a aparência de maestria: vença ou obtenha a obliteração na Monção como Santo Oco.

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/skin-lineup.png" alt="As seis aparências do Santo Oco: Ícone Rachado, Santo de Obsidiana, Relíquia de Azinhavre, Vésperas Solares, Coro Umbral e Voto Carmesim" width="100%"></p>

## Instalação e opções

- **Gerenciador de mods (recomendado):** instale com o [r2modman](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) ou o Thunderstore Mod Manager. As dependências são instaladas automaticamente. Após um patch do jogo, atualize BepInExPack, R2API e HookGenPatcher junto com o Hollow Saint.
- **Manual:** instale as dependências listadas nesta página e copie a pasta `plugins/HollowSaint` do pacote para `BepInEx/plugins/`. Mantenha `HollowSaint.dll`, `hollowsaintassets` e `HollowSaint.language` juntos nessa pasta.
- **Opções:** cada número de balanceamento está no menu **Configurações → Opções de mods → Hollow Saint** ([Risk Of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/), instalado com o mod) e em `BepInEx/config/com.johnstonstu.hollowsaint.cfg`, junto com interruptores de apresentação: mão da lança, exibição de itens, movimento dos braços e sensação de impacto.
- **Idiomas:** o mod segue o idioma definido no Risk of Rain 2. Chinês simplificado, russo e português do Brasil estão incluídos (tradução automática; correções são bem-vindas em uma [issue de tradução](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md)). Qualquer outro idioma mostra inglês. O menu Opções de mods continua em inglês.

## Feedback e limitações conhecidas

Bugs e comentários de balanceamento são bem-vindos nas [issues do GitHub](https://github.com/johnstonstu/ror2-hollow-saint/issues). Para um relatório de bug, ligue **Verbose log** (Opções de mods → 6. Misc), reproduza o problema e anexe `BepInEx/LogOutput.log` com a fase e o que você estava fazendo.

- **O multiplayer ainda não teve um playtest de verdade.** As habilidades são autoritativas no servidor e sincronizadas em rede, mas espere arestas. Todos os jogadores devem usar a mesma versão e configuração.
- **A aceitação em controle físico não foi verificada.** Informe seu controle e a configuração de entrada nos relatos de entrada.
- As exibições de itens emprestam as posições do Comando, então alguns itens ficam um pouco fora do lugar.

## Também de JohnstonStu

<table>
  <tr>
    <td><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/ah64-icon.png" alt="AH64" width="96"></a></td>
    <td><b><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/">AH64</a></b>: um sobrevivente helicóptero de ataque Apache. Ele paira e nunca pousa, com metralhadora, foguetes Hydra, mísseis Hellfire e Longbow, e rolamentos evasivos.</td>
  </tr>
</table>

## Créditos, licença e changelog

- Criado por JohnstonStu: design, código, modelo, animação e efeitos.
- Sons de raio feitos a partir de amostras do Pixabay (Pixabay Content License). Os outros sons são originais.
- Feito com BepInEx, R2API e Risk Of Options.
- [Changelog completo](https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/CHANGELOG.md).

[MIT](https://github.com/johnstonstu/ror2-hollow-saint/blob/main/LICENSE) © 2026 JohnstonStu.
