<p align="center"><a href="README.md">English</a> | <a href="README.zh-CN.md">简体中文</a> | <a href="README.ru.md">Русский</a> | <b>Português (BR)</b></p>

> Esta página foi traduzida por máquina. Correções são bem-vindas: [abrir uma correção de tradução](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md).

<p align="center">
  <img src="docs/media/banner.jpg" alt="Santo Oco, um sobrevivente da tempestade para Risk of Rain 2" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=4fd2ff&style=for-the-badge" alt="Versão no Thunderstore"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.downloads&label=downloads&color=7b5cff&style=for-the-badge" alt="Downloads no Thunderstore"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="Licença MIT"></a>
</p>

<h3 align="center">Um ícone devocional rachado que só responde à tempestade.</h3>

<p align="center">Encadeie relâmpagos por um bando, prenda a maior ameaça com uma lança de relâmpago e guarde cada Eletrocussão como uma Carga Estática.<br>Depois gaste o estoque: em um Raio que viaja na sua próxima lança, ou em um Olhar carregado que abre com uma explosão.</p>

<p align="center"><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><b>Instalar</b></a> · <a href="#novidades-da-13"><b>Novidades da 1.3</b></a> · <a href="#o-kit"><b>Habilidades</b></a> · <a href="#como-a-tempestade-funciona"><b>A tempestade</b></a> · <a href="https://github.com/johnstonstu/ror2-hollow-saint/issues"><b>Feedback</b></a></p>

<p align="center"><img src="docs/media/thundercloud-return-13.webp" alt="Thundercloud 1.3: crown release and rolling lightning" width="100%"></p>

<p align="center"><img src="docs/media/hollowed-orb-13.webp" alt="Hollowed Orb 1.3" width="49%"> <img src="docs/media/circuit-orb-13.webp" alt="Open Circuit and Hollowed Orb 1.3" width="49%"></p>

## Novidades da 1.3

**Atualizado para o patch do jogo de outubro de 2026.** Atualize as dependências principais (BepInExPack, R2API, HookGenPatcher) no seu gerenciador de mods junto com o Hollow Saint.

**1.3.1:** corrige a Seta em Arco e a Lança da Tempestade aparecendo como o projétil azul do Artífice após o patch, e traz um ajuste de balanceamento medido contra os sobreviventes originais: início mais forte (Seta em Arco 171%, proc 1,0), Orbe e Nuvem Trovejante mais fortes e com recarga menor, a Nuvem ataca mais rápido com velocidade de ataque, inimigos preparados que morrem agora rendem uma carga e o dano por nível segue a regra original de 20%. Após os testes de jogo: o Olhar troca os surtos no meio do feixe por uma grande explosão inicial e um acúmulo de foco no feixe (até +100% de dano em um alvo), alcança 90 m e ignora repulsão enquanto canaliza. A Nuvem Trovejante fica mais alta, atinge voadores e saliências abaixo dela e pode ser encerrada mais cedo com a Especial, devolvendo parte da recarga. O Pente Reserva dá ao Orbe mais um acerto por pente, e o Orbe ficou maior (0,9 m sem cargas, 1,5 m com cinco cargas).

Nova Secundária alternativa, **Orbe Oco**, e terceira Especial, **Nuvem Trovejante**. O orbe funciona sem Estática: lançamentos curtos preservam o estoque; segurar por mais de 0,5s reúne cargas para fortalecer. A nuvem também é gratuita: um toque cria uma pequena tempestade e segurar despeja cargas. Soltar cedo mantém o restante; a Utilidade cancela e ativa o Passo de Arco.

Orbe básico: 0,9 m, 378% de dano por acerto e três acertos, com energia visível vindo do corpo e braços. Circuito Aberto lança o orbe acima da cabeça e mantém a Primária disponível. **Circuito Aberto** agora reúne pelo menos uma carga ao segurar a Especial; 1/3/5 cargas dão 1/1,5/2 vezes a densidade de pulsos e mais arcos. Pulsos extras não aceleram a Estática; raio, duração e dano por pulso permanecem iguais. Sua coroa agora é um Circuito Fechado: veja a nota do ciclo da tempestade abaixo.

- **Orbe Oco:** reúna uma grande bola elétrica entre as duas mãos e lance à frente. O toque gratuito dá 378% de dano por acerto e 3 acertos; cada carga reunida soma dano (até 738% por acerto com 5 cargas) e mais um acerto (até 8), com diâmetro de 0,9–1,5 m. Cada Pente Reserva soma mais um acerto. Alcance de 70 m e saltos de 18–36 m conforme as cargas. Prioriza alvos novos; cada revisita ao mesmo alvo mantém 75% do dano anterior. Sem mais nada ao alcance, o orbe se fixa no alvo e descarrega ali os acertos restantes; ao se esgotar, explode numa pequena área (3 m sem cargas, +0,8 m por carga) com 60% do dano do acerto. Todo acerto prepara Estática. Circuito Aberto reúne e lança o Orbe acima da cabeça.
- **Nuvem Trovejante:** uma tempestade persistente, gratuita com o estoque vazio: um toque cria uma pequena tempestade e segurar despeja cargas na coroa. Ela sobe rápido e dura 4 s sem cargas, +1 s por carga (9 s com 5 cargas). A cada 0,75 s atinge todo inimigo sob ela dentro do raio (12 m sem cargas, 16 m com uma, 30 m com cinco) com 149% de dano sem cargas, até 261% por raio com cinco, deixando-os Eletrizados e preparados. Mira até 80 m; também pode ser posicionada numa área vazia. Os raios vêm mais rápido com velocidade de ataque (até o dobro da frequência). Pressione a Especial de novo para encerrar a tempestade mais cedo e recuperar parte da recarga.

Recargas após terminar o lançamento: Orbe 6 s, Nuvem 10 s. Lança, Olhar e Circuito Aberto continuam disponíveis. As opções ajustam as novas habilidades e seus textos. Multijogador real e controle físico ainda não foram verificados.

**Refinamento da tempestade e dos saltos:** a nuvem agora é uma tempestade persistente que ataca a cada 0,75 s enquanto dura (4–9 segundos), com descargas visuais ramificadas em cada alvo. O alcance dos saltos cresce com as cargas: 18 m sem cargas/com uma, 27 m com três e 36 m com cinco.

> **Acesso antecipado:** o Santo Oco ainda está sendo ajustado, então espere mudanças de balanceamento e um bug ocasional. Seu feedback molda o próximo patch. As descrições de habilidades no jogo sempre mostram os números das suas configurações atuais.

**Dano inicial:** a Seta em Arco causa 171% por acerto direto, antes 108%, com a mesma cadência e agora coeficiente de proc 1,0, como as outras primárias. Apenas o valor anterior exato é migrado; ajustes pessoais são preservados.

**Orbe fixado:** sem outro inimigo ao alcance, o Orbe Oco se fixa no alvo e descarrega ali os acertos restantes; ao se esgotar, explode numa pequena área (3 m sem cargas, +0,8 m por carga) com 60% do dano do acerto. Novos inimigos continuam tendo prioridade.

**Bola de relâmpagos:** arcos se movem pela superfície ao carregar e voar. Impactos ganham raios ramificados, faíscas, anéis de choque e sons elétricos nas cores da aparência.

**Ciclo da tempestade:** toda habilidade que gasta Cargas Estáticas agora prepara Estática no que atinge (Orbe Oco, Nuvem Trovejante, explosão inicial do Olhar, o respingo do Raio), até 95%, mas nunca cheio, e a Nuvem Trovejante também deixa os alvos Eletrizados. Os finalizadores (Seta em Arco, Lança da Tempestade, o feixe do Olhar e o Circuito Aberto) levam inimigos preparados à Eletrocussão e devolvem as cargas ao estoque. A Lança da Tempestade acumula o dobro de Estática em inimigos preparados, e um estoque cheio agora só é usado em um arremesso totalmente carregado. O Circuito Aberto é um **Circuito Fechado**: Eletrocussões a até 12 m devolvem as cargas que você colocou nele, e as que ainda estiverem na coroa quando ela se fecha explodem dela como uma onda de choque da coroa (12 m, 150% de dano por carga restante, preparando o que atinge). Uma proteção leve de ganho (2 cargas por segundo) impede que bandos do fim de jogo inundem o estoque. Reunir cargas leva 0,25 segundo por carga.

## O kit

| | Habilidade | Slot | Em resumo |
|:-:|---|---|---|
| <img src="docs/media/icon-discharge.png" width="40" alt=""> | **Prece Atendida** | Passiva | Acertos acumulam Estática; Eletrocussões armazenam Cargas Estáticas. |
| <img src="docs/media/icon-arc_bolt.png" width="40" alt=""> | **Seta em Arco** | Primária | Uma seta rápida que salta por um bando. |
| <img src="docs/media/icon-conduit_spear.png" width="40" alt=""> | **Lança da Tempestade** | Secundária | Segure, arremesse, crave, exploda. |
| <img src="docs/media/icon-arc_step.png" width="40" alt=""> | **Passo em Arco** | Utilitária | Dois teleportes, no chão ou no ar. |
| <img src="docs/media/icon-gaze.png" width="40" alt=""> | **Olhar do Oco** | Especial | Carregue, flutue, queime uma linha através deles. |
| <img src="docs/media/icon-open_circuit.png" width="40" alt=""> | **Circuito Aberto** | Especial alternativa | Uma coroa que golpeia enquanto você continua lutando. |
| | **Orbe Oco** | Secundária alternativa | Gratuito; cargas opcionais; novos alvos primeiro e fixa-se no alvo quando não há mais nada ao alcance. |
| | **Nuvem Trovejante** | Terceira Especial | Tempestade persistente e gratuita sobre a área visada. |

<h3><img src="docs/media/icon-discharge.png" width="40" alt=""> Prece Atendida <sub>Passiva</sub></h3>

Seus acertos acumulam **Estática** nos inimigos. Estática cheia **Eletrocuta**: o inimigo fica eletrizado e o arco salta para os vizinhos. Cada Eletrocussão armazena uma **Carga Estática** (até cinco). Um estoque cheio transforma o seu próximo arremesso da Lança da Tempestade em um **Raio**, e o Olhar gasta todas as cargas na sua explosão inicial. As cargas nunca se dissipam sozinhas.

<p align="center"><img src="docs/media/storm.webp" alt="Eletrocussões enchendo o estoque, depois um Raio" width="70%"></p>

<h3><img src="docs/media/icon-arc_bolt.png" width="40" alt=""> Seta em Arco <sub>Primária</sub></h3>

Dispare uma seta que salta para até mais três inimigos. É a sua pressão constante e a forma mais rápida de espalhar Estática por um grupo.

<p align="center"><img src="docs/media/arc-bolt.webp" alt="Seta em Arco saltando por um bando" width="70%"></p>

<h3><img src="docs/media/icon-conduit_spear.png" width="40" alt=""> Lança da Tempestade <sub>Secundária</sub></h3>

Segure para formar uma lança de relâmpago e solte para arremessar. Ela se crava no que atinge e depois explode em uma cúpula de relâmpagos que cresce com a carga. Uma carga completa também chama um raio do céu; um arremesso totalmente carregado com o estoque de Estática cheio a transforma em um Raio que prepara o bando ao redor. Os acertos da lança acumulam o dobro de Estática em inimigos preparados, o que a torna a Secundária para finalizar o que sua Especial prepara.

<p align="center"><img src="docs/media/stormspear.webp" alt="Uma Lança da Tempestade carregada cravando e explodindo" width="70%"></p>

### Orbe Oco — Secundária alternativa

Toque e solte para lançar sem cargas: bola de 0,9 m, 378% de dano por acerto e três acertos. Segurar por mais de 0,5 s reúne cargas uma a uma; cada carga soma dano (até 738% por acerto com 5 cargas) e mais um acerto (até 8). Cada Pente Reserva soma mais um acerto. Diâmetro até 1,5 m. Alcance 70 m, saltos de 18–36 m conforme as cargas. Cada revisita ao mesmo inimigo mantém 75% do dano anterior naquele alvo; um alvo novo recebe o dano inicial completo. A assistência favorece a mira e respeita paredes. Todo acerto prepara Estática, o que torna o toque gratuito a Secundária para armar sua Especial e a Seta em Arco.

Alvos novos vêm primeiro. Sem mais nada ao alcance, o orbe se fixa no alvo e descarrega ali os acertos restantes, o que o torna útil contra um chefe sozinho por perto, sem gastar as cargas que você guarda para a Especial. Ao se esgotar, explode numa pequena área (3 m sem cargas, +0,8 m por carga) com 60% do dano do acerto. A Utilidade cancela a reunião e devolve cargas e uso. Circuito Aberto lança acima da cabeça, mantendo a Primária disponível. Recarga 6 s após o fim do lançamento.

### Nuvem Trovejante — terceira Especial

Gratuita com o estoque vazio: um toque cria uma pequena tempestade; ou segure a Especial para despejar cargas na coroa e solte sobre a área visada. A coroa sobe rápido e a tempestade permanece 4 s sem cargas, +1 s por carga (9 s com cinco). A cada 0,75 s ataca todo inimigo sob ela dentro do raio (12 m sem cargas, 16 m com uma, 30 m com cinco) com 149% de dano sem cargas, até 261% por raio com cinco, deixando-os Eletrizados e preparados. Mira até 80 m e pode ser posicionada numa área vazia. A nuvem fica no alto e atinge toda a coluna abaixo dela, então voadores e inimigos em saliências também são atingidos. Os raios vêm mais rápido com velocidade de ataque (até o dobro da frequência). A Utilidade cancela a reunião. Pressione a Especial de novo enquanto chove para encerrar a tempestade mais cedo e recuperar até metade da recarga, proporcional ao tempo restante. Recarga 10 s após o fim da tempestade.

<h3><img src="docs/media/icon-arc_step.png" width="40" alt=""> Passo em Arco <sub>Utilitária</sub></h3>

Teleporte-se uma curta distância em qualquer direção, até no ar. Olhe para cima para subir e salte para fora de um passo para conservar o impulso. Duas cargas: uma para achar um ângulo, outra para escapar.

<p align="center"><img src="docs/media/arc-step.webp" alt="Passo em Arco para a esquerda, para a direita e para cima" width="70%"></p>

<h3><img src="docs/media/icon-gaze.png" width="40" alt=""> Olhar do Oco <sub>Especial</sub></h3>

Segure para atrair suas Cargas Estáticas para a coroa, depois suba e canalize um feixe perfurante de 90 m por sete segundos. O feixe abre disparando tudo o que você reuniu em uma única grande explosão (400% por carga, 6 m de largura mais 2 m por carga, até 20 m). Enquanto ele queima, o núcleo do feixe acumula **foco** no que estiver segurando: até +100% de dano ao longo de três segundos, mantido durante um deslize de meio segundo e diminuindo depois disso, com o zumbido subindo conforme cresce. Você recebe menos dano e ignora repulsão enquanto reúne e canaliza. Toque no Especial para pular a carga; Especial ou Cancelar interface encerra o feixe mais cedo, e a utilitária sai direto para o seu Passo em Arco. A explosão inicial prepara o que atinge, então o feixe termina o serviço.

<p align="center"><img src="docs/media/gaze.webp" alt="Olhar do Oco varrendo um bando" width="70%"></p>

<h3><img src="docs/media/icon-open_circuit.png" width="40" alt=""> Circuito Aberto <sub>Especial alternativa</sub></h3>

Segure a Especial para alimentar a coroa com pelo menos uma Carga Estática e solte. A coroa ataca inimigos ao redor por dez segundos; mais cargas aumentam a frequência dos pulsos. A Lança da Tempestade carrega mais rápido, e o estoque cheio fortalece seu raio. O Orbe Oco é reunido e lançado acima da cabeça, mantendo a Primária disponível. Inimigos que ficam dentro por três segundos levam um choque extra. **Circuito Fechado:** cada Eletrocussão a até 12 m devolve uma carga colocada; as que ainda estiverem na coroa quando ela se fecha explodem da coroa como uma única onda de choque de 12 m, com 150% de dano por carga restante, preparando o que atinge, ou voltam ao seu estoque se não houver inimigo na onda.

<p align="center"><img src="docs/media/crown.webp" alt="Circuito Aberto golpeando um círculo de inimigos" width="70%"></p>

## Como a tempestade funciona

<p align="center"><img src="docs/media/storm-loop.png" alt="O ciclo da tempestade: finalizar, Eletrocutar, guardar, gastar, preparar" width="100%"></p>

O Santo Oco funciona com um único ciclo: os **finalizadores** rendem Cargas Estáticas, os **gastadores** as usam, e tudo o que um gastador atinge fica **preparado** para o próximo finalizador.

1. **Estática.** Acertos elegíveis carregam o inimigo. Acertos maiores, críticos e itens de alto proc carregam mais rápido. Ela se esvai se você parar de acertar.
2. **Eletrocussão.** Com Estática cheia, o inimigo é sacudido (exceto chefes) e fica **Eletrizado**, recebendo dano extra por um instante. O arco salta para inimigos próximos e os carrega também. Cada Eletrocussão guarda uma **Carga Estática**, até cinco.
3. **Gastar e preparar.** Toda habilidade que gasta cargas também prepara o que atinge, deixando-o com até 95% de Estática. Ela nunca enche a barra sozinha, então um gastador jamais paga a si mesmo.
4. **Finalizar.** A Seta em Arco, a Lança da Tempestade, o feixe do Olhar e os pulsos do Circuito Aberto levam os inimigos preparados ao limite. Um bando preparado Eletrocuta em um ou dois golpes e seu estoque se reabastece.

| Papel | Habilidades | No ciclo |
|---|---|---|
| Finalizador | Seta em Arco, Lança da Tempestade, feixe do Olhar, pulsos do Circuito Aberto | Acumulam Estática, Eletrocutam, guardam cargas. A Lança da Tempestade acumula o dobro de Estática em inimigos preparados. |
| Gastador | Orbe Oco (segurado), Nuvem Trovejante, explosão inicial do Olhar, Raio com estoque cheio | Transformam cargas em dano e deixam os alvos preparados. A Nuvem Trovejante também Eletriza. |
| Preparador gratuito | Orbe Oco (toque), Nuvem Trovejante (gratuita) | Ambos preparam sem gastar nada: a forma mais fácil de iniciar o ciclo. |
| Retorno | Circuito Aberto | As cargas colocadas voltam das Eletrocussões a até 12 m; as sobras explodem como uma onda de choque da coroa no fechamento. |

Nada leva suas cargas a menos que você tenha segurado uma habilidade para isso: arremessos rápidos da lança e toques rápidos do Orbe nunca mexem no estoque. Uma proteção leve de ganho (2 cargas por segundo, ajustável) impede que bandos enormes do fim de jogo inundem o estoque.

## Combinações

A Secundária decide como você começa e termina o ciclo; a Especial decide como você o gasta.

<p align="center"><img src="docs/media/storm-builds.png" alt="Seis combinações: Lança da Tempestade ou Orbe Oco com Olhar, Circuito Aberto ou Nuvem Trovejante" width="100%"></p>

| Combinação | Equipamento | Como o ciclo funciona | Atenção |
|---|---|---|---|
| **Evocador de Tempestades** | Lança da Tempestade + Olhar | A Seta em Arco espalha Estática e o estoque enche. Abra o Olhar sobre o bando: a explosão inicial prepara, o feixe focado e suas lanças finalizam. | Lança e Olhar dividem um só estoque; só arremessos totalmente carregados o gastam. |
| **Lanceiro** | Lança da Tempestade + Circuito Aberto | Sob a coroa a Lança da Tempestade carrega muito mais rápido. Arremesse em inimigos preparados enquanto os pulsos cuidam do resto; cada Eletrocussão próxima reembolsa a coroa. | Fique a até 12 m da luta para receber reembolsos. |
| **Cerco** | Lança da Tempestade + Nuvem Trovejante | Solte uma nuvem sobre um grupo distante para prepará-lo e Eletrizá-lo, depois finalize à distância com lanças e saltos da Seta em Arco. | Uma tempestade gratuita ainda prepara; guarde cargas para a grande ou para uma lança totalmente carregada. |
| **Vidente** | Orbe Oco + Olhar | Orbes gratuitos preparam o bando, e então a explosão inicial do Olhar e o feixe cobram a conta. | Orbes nunca finalizam sozinhos; quem finaliza são o feixe e a Seta em Arco. |
| **Condutor** | Orbe Oco + Circuito Aberto | Orbes gratuitos preparam, coloque cargas na coroa e lute dentro dela. Os pulsos finalizam inimigos preparados os reembolsos mantêm o estoque cheio e as sobras explodem como uma onda de choque da coroa. A combinação mais autossustentável. | Orbes lançados acima da cabeça mantêm a Primária livre durante a coroa. |
| **Tempestade** | Orbe Oco + Nuvem Trovejante | Tudo prepara: nuvem e orbes acendem o bando, e os saltos da Seta em Arco fazem todo o trabalho de finalizar. Alto risco, alta recompensa. | Tempestades gratuitas mantêm o ciclo; tempestades carregadas esvaziam o estoque rápido. |

## Aparências

Seis aparências, cada uma com seu próprio nimbo e cor de relâmpago. **Voto Carmesim** é a aparência de maestria: vença ou obtenha a obliteração na Monção como Santo Oco.

<p align="center"><img src="docs/media/skin-lineup.png" alt="As seis aparências do Santo Oco: Ícone Rachado, Santo de Obsidiana, Relíquia de Azinhavre, Vésperas Solares, Coro Umbral e Voto Carmesim" width="100%"></p>

## Instalação

**Gerenciador de mods (recomendado):** instale com o [r 2modman](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) ou o Thunderstore Mod Manager. As dependências são instaladas automaticamente.

**Manual:** instale as dependências listadas nesta página e copie a pasta `plugins/HollowSaint` do pacote para `BepInEx/plugins/`. Mantenha `HollowSaint.dll`, `hollowsaintassets` e `HollowSaint.language` juntos nessa pasta.

## Opções

Cada número de balanceamento está no menu **Configurações → Opções de mods → Hollow Saint** ([Risk Of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/), instalado com o mod) e em `BepInEx/config/com.johnstonstu.hollowsaint.cfg`, junto com interruptores de apresentação: mão da lança, exibição de itens, movimento dos braços e sensação de impacto.

## Idiomas

O mod segue o idioma definido no Risk of Rain 2. Chinês simplificado, russo e português do Brasil estão incluídos (tradução automática; correções são bem-vindas em uma [issue de tradução](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md)). Qualquer outro idioma mostra inglês. O menu Opções de mods continua em inglês.

## Feedback e limitações conhecidas

Bugs e comentários de balanceamento são bem-vindos nas [issues do GitHub](https://github.com/johnstonstu/ror2-hollow-saint/issues). Para um relatório de bug, ligue **Verbose log** (Opções de mods → 6. Misc), reproduza o problema e anexe `BepInEx/LogOutput.log` com a fase e o que você estava fazendo.

- **O multiplayer ainda não teve um playtest de verdade.** As habilidades são autoritativas no servidor e sincronizadas em rede, mas espere arestas. Todos os jogadores devem usar a mesma versão e configuração.
- **A aceitação em controle físico não foi verificada.** Informe seu controle e a configuração de entrada nos relatos de entrada.
- As exibições de itens emprestam as posições do Comando, então alguns itens ficam um pouco fora do lugar.

## Também de JohnstonStu

<table>
  <tr>
    <td><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="docs/media/ah64-icon.png" alt="AH 64" width="96"></a></td>
    <td><b><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/">AH 64</a></b>: um sobrevivente helicóptero de ataque Apache. Ele paira e nunca pousa, com metralhadora, foguetes Hydra, mísseis Hellfire e Longbow, e rolamentos evasivos.</td>
  </tr>
</table>

## Créditos e licença

- Criado por JohnstonStu: design, código, modelo, animação e efeitos.
- Sons de raio feitos a partir de amostras do Pixabay (Pixabay Content License). Os outros sons são originais.
- Feito com BepInEx, R 2API e Risk Of Options.

[MIT](LICENSE) © 2026 JohnstonStu.

## Para desenvolvedores

A documentação para desenvolvedores está disponível apenas em inglês: veja a [seção "For developers" do README em inglês](README.md#for-developers).
