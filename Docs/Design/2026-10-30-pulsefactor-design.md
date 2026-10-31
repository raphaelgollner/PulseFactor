# PulseFactor — Documento de Design

Data: 2026-10-30
Status: rascunho para aprovação

Legenda: itens marcados **[decisão do Claude]** são escolhas técnicas/de default feitas pelo assistente, não pelo autor do jogo — podem ser revistas. Todo o resto foi definido pelo autor.

---

## 1. Visão e porquê

PulseFactor é um **simulador espacial arcade** para PC (teclado/mouse e controle), com referências de **X4 Foundations** e **Elite Dangerous** no escopo (comércio, mineração, contratos, estações, naves modulares) mas com **sensação de pilotagem e acessibilidade estilo Everspace 2**: casual, legível, sem curva hardcore.

O jogador nunca sai da nave. Toda interação com o mundo acontece pilotando ou pelos terminais das estações.

**Objetivo do autor:** lançar comercialmente (Steam) se o jogo ficar bom. Portanto a base técnica deve ser sólida o suficiente para crescer (dados configuráveis, sistemas desacoplados), não um protótipo descartável.

### Metas
- Pilotagem gostosa e responsiva (estilo Everspace) é a fundação de tudo.
- Ciclo de jogo: voar → docar → negociar/aceitar contratos → minerar/transportar/caçar → voltar → melhorar a nave.
- Naves construídas em **blocos visuais** combináveis (procedural + customização do jogador).
- Mundo que parece grande (escala tipo Elite) sem viagens tediosas.

### Não-metas (agora)
- Pousar em planetas.
- Sair da nave / personagem em primeira pessoa.
- Lore e narrativa (o autor tem uma base, mas fica para depois).
- Naves grandes/capitais (o sistema de skills já prevê até 3 slots para elas no futuro).
- Interdição (ser puxado da dobra).
- Multiplayer.
- Múltiplos sistemas solares.

### Anti-metas (seria fracasso mesmo funcionando)
- Ficar hardcore: controles complexos, gerenciamento de energia minucioso, navegação punitiva.
- Viagens longas e entediantes.
- Fuga de piratas impossível ou frustrante.
- HUD poluído que esconde o espaço.

---

## 2. Mundo

### 2.1 Sistema solar
- Um sistema com **sol, planetas, luas, asteroides e campos de asteroides**.
- **Toda estação orbita um corpo celeste** (planeta, lua ou asteroide grande). Deve existir ao menos uma estação construída num asteroide grande.
- **Escala estilo Elite Dangerous** (distâncias na ordem de milhões de km).
- **[decisão do Claude]** Alvo de tempo de viagem em dobra: ~1–3 min entre corpos próximos, ~4–6 min de ponta a ponta do sistema. Ajustável por dados.
- **[decisão do Claude]** Técnica: **origem flutuante** (floating origin) + coordenadas em precisão dupla para posições do mundo; corpos distantes renderizados em escala reduzida/impostores. Necessário para evitar tremedeira de float a essas distâncias.
- **[decisão do Claude]** Primeira versão: o sistema é desenhado à mão (dados em ScriptableObjects), não gerado proceduralmente. As órbitas podem ser estáticas ou lentas; decisão do construtor.

### 2.2 Corporações
- **3 corporações** no sistema. Cada estação pertence a uma.
- Nomes provisórios, guardados em dados para troca fácil quando houver lore. **[decisão do Claude]**
- Reputação por corporação (ver §6).

### 2.3 Recursos
- Recursos mineráveis: **ferro, gelo, cobre, carvão, titânio, cobalto**.
- Asteroides contêm recursos; campos diferentes têm composições diferentes.
- **[decisão do Claude]** Raridade/valor: ferro, gelo, carvão comuns; cobre médio; titânio, cobalto raros. Ajustável por dados.

---

## 3. Naves

### 3.1 Classes (apenas naves pequenas por enquanto)

| | Scout | Cargo | Assault |
|---|---|---|---|
| Papel | exploração / transporte do jogador | transporte e mineração | combate |
| Aceleração | maior | menor | média |
| Carga (unidades) | 50 | 500 | 10 |
| Slots de arma | 1 | 2 | 3 |
| Casco inicial | Padrão (100 HP) | Padrão (100 HP) | Militar (200 HP) |
| Slots de skill | 1 | 1 | 1 |

- **Velocidade máxima** de cada nave é reduzida proporcionalmente à **ocupação da carga** (mais carga → menor velocidade máxima). A **aceleração não muda** com a carga.
- **[decisão do Claude]** Redução sugerida: até −40% de velocidade máxima com porão cheio, linear. Ajustável.

### 3.2 Construção em blocos (visual)
- Toda nave é montada de blocos: **cabine, corpo, cauda, asas** (pode haver mais tipos depois).
- Blocos são combináveis para gerar **naves procedurais** (NPCs/piratas) e para **customização do jogador**.
- Cada bloco define **pontos de encaixe** (sockets) para os vizinhos e pontos de montagem de arma/motor (efeitos de propulsor).
- **Cores independentes por módulo** e **padrões de pintura** (aplicados por material/shader, independentes do modelo).
- **Arte inicial: formas primitivas geradas por código/prefabs simples**, trocáveis pela arte final sem quebrar o sistema.

### 3.2.1 Fabricantes e modelos
3 fabricantes, 2 modelos cada. Cada modelo é uma nave de fábrica completa, montada com os blocos daquele modelo.

| Fabricante | Modelo | Classe | Peças |
|---|---|---|---|
| Razzor Ships | Rapid Fire | Assault | exclusivas da classe |
| Razzor Ships | Cruise | Scout | multiclasse |
| CyberTech SA | Type C | Cargo | multiclasse |
| CyberTech SA | Type A | Assault | exclusivas da classe |
| Master Hauling | Crusader | Scout | exclusivas da classe |
| Master Hauling | Horder | Cargo | multiclasse |

- Peças **multiclasse** podem ser montadas em naves de qualquer classe; peças **exclusivas** só na classe do modelo de origem.
- **[decisão do Claude]** O **corpo** define a classe e os atributos (carga, slots de arma, casco inicial). Cabine, cauda e asas são visuais (a cabine também define o interior do cockpit). Asas são um par espelhado.
- Em aberto: relação entre fabricantes e corporações (quem vende o quê).

### 3.2.1a Estilo de arte
- Referências em `Docs/Design/refs/naves/` (geradas por IA; servem para **formato e estilo**, não são finais).
- Estilo geral: **low-poly limpo e "chunky"**, sombreamento simples, pouca textura, detalhes funcionais (antenas, pratos, tanques, painéis). Superfícies lisas para receber cor por módulo e padrões de pintura.
- **As armas que aparecem nas referências NÃO fazem parte dos blocos.** Blocos são modelados sem armas. Cada bloco expõe pontos de montagem de arma, cujas posições serão definidas depois, nave por nave. Antenas e canos pequenos nos cockpits são decoração.
- **Propulsores podem existir em qualquer bloco** (asas com nacele, cauda com motores). Cada bloco declara seus pontos de propulsor; brilho/rastro acendem em todos. **[decisão do Claude]**
- **Encaixes padronizados por tipo de bloco** entre fabricantes, para que peças multiclasse caibam em qualquer corpo. **[decisão do Claude]**

### 3.2.1b Identidade dos fabricantes
Cada fabricante deve ser reconhecível à distância por paleta e silhueta.
- **Razzor Ships** — militar/agressivo, angular, placas sobrepostas, desgaste na pintura. Cinza/azul ou verde-oliva com **acentos laranja**.
- **CyberTech SA** — high-tech corporativo: grafite escuro, branco frio, **linhas emissivas ciano**, superfícies lisas e chanfradas, recortes hexagonais. **[decisão do Claude, aprovada pelo autor]** As referências do Type A e Type C vieram com cores de outros fabricantes; usar só o formato delas.
- **Master Hauling** — serviço/industrial, simpático, formas arredondadas, **acentos amarelos**. **[decisão do Claude]** Base branca/cinza-claro com painéis azul-marinho, unificando Crusader (branco) e Horder (azul-marinho).

### 3.2.1c Blocos por modelo

| Modelo | Cockpit | Corpo | Asas (par espelhado) | Cauda |
|---|---|---|---|---|
| Crusader | domo geodésico de vidro, visão panorâmica | cilindro com pontas, prato de antena, tanques | treliças finas com painéis solares | bloco compacto com bocais |
| Cruise | domo de vidro em grade, compacto | alongado e facetado, tanques laterais | nacele com propulsor | motor principal grande + estabilizadores |
| Rapid Fire | nariz pontudo, canopy pequeno de caça | largo, blindado em placas, entradas de ar | nacele com propulsor | 3 motores redondos em triângulo + estabilizadores |
| Type A | nariz de gunship, canopy longo segmentado | blindado, aleta dorsal | nacele com aleta vertical na ponta | 2 motores retangulares + estabilizadores |
| Type C | cabine de caminhão, para-brisa grande | contêiner de carga com moldura e porta | curtas e retas, com nacele | 4 motores em grade 2×2 + estabilizadores |
| Horder | cabine de caminhão blindada, janelas menores | contêiner de carga | nacele tipo turbofan com entrada amarela | turbina única grande + aletas |

- **[decisão do Claude]** Type C e Horder vieram com corpo quase idêntico; diferenciar pelo estilo do fabricante (Type C liso/high-tech; Horder robusto, arredondado, reforçado e gasto).

### 3.2.2 Frota do jogador
- O jogador pode ter **mais de uma nave**.
- **[decisão do Claude]** Naves ficam guardadas na estação onde foram deixadas (estilo Elite); troca de nave só em estações com estaleiro. Cada nave é uma entidade persistente com local, blocos, pintura, módulos e carga.

### 3.3 Cabines e cockpit
- Cada cabine tem formato próprio **por dentro e por fora**, e o interior **coincide com o exterior** (o canopy visto de dentro é o mesmo vidro visto de fora).
- Cada bloco de cabine traz: modelo externo, modelo interno, e **encaixes de tela** onde os painéis do HUD físico são desenhados. **[decisão do Claude]** O conteúdo dos painéis é um sistema único reaproveitado por todas as cabines.

### 3.4 Módulos internos (afetam desempenho, não visual)
Quatro módulos, cada um com **tiers** (melhor tier = melhor desempenho):
- **Gerador** — define a capacidade de energia; limita o escudo máximo que a nave pode usar.
- **Motor** — aceleração e velocidade máxima.
- **Casco** — único jeito de aumentar a vida (HP). Tipos iniciais: Padrão (100 HP) e Militar (200 HP); ambos podem ser melhorados (fórmula a definir — ver §10).
- **Skill** — contém a(s) skill(s) ativa(s). Naves pequenas: 1 slot; sistema suporta até 3 (naves grandes no futuro).

### 3.5 Escudo
- Toda nave tem escudo; pode ser melhorado, limitado pela capacidade máxima do gerador.
- **[decisão do Claude]** Escudo absorve dano antes do casco e regenera após alguns segundos sem levar dano.

### 3.6 Armas
- Slots de arma por classe (tabela acima).
- **Laser de mineração** é equipável em qualquer nave e **ocupa um slot de arma**.
- Armas **balísticas** consomem munição (recarregável em qualquer estação). **[decisão do Claude]** Armas de energia não consomem munição; usam calor/energia simples (sem gerenciamento complexo).
- **Grupos de fogo**: num menu de armas, o jogador atribui cada slot ao **primário** (botão esquerdo) ou **secundário** (botão direito). Ex.: Assault com as 3 armas no primário dispara todas juntas.

### 3.7 Skills
- **Recarga de escudo**: restaura 100% do escudo.
- **Sobrecarga**: aumenta o dano das armas por um tempo.
- **Boost**: aumenta muito aceleração e velocidade máxima por um tempo.
- Ativadas pelas teclas **1–3** (um por slot).
- **[decisão do Claude]** Cada skill tem cooldown; o módulo de skill é comprado/trocado em estaleiro.

### 3.8 Combustível
- Naves consomem combustível; toda estação reabastece.
- **[decisão do Claude]** O consumo vem principalmente da dobra; o subluz consome pouco ou nada. Ficar sem combustível não deve ser punitivo demais (ex.: subluz continua funcionando). Anti-meta: não ser hardcore.

---

## 4. Pilotagem e controles

### 4.1 Modelo de voo (estilo Everspace)
- **A nave persegue a mira do mouse** (mouse-aim): o mouse move a mira/câmera e a nave gira em direção a ela.
- Ao soltar o acelerador, a nave **desacelera devagar** (amortecimento suave, não física newtoniana pura).
- **Modo cruzeiro**: mantém uma velocidade constante; W aumenta, S reduz.

### 4.2 Mapa de controles (teclado/mouse)

| Ação | Tecla |
|---|---|
| Mirar / virar a nave | Mouse |
| Acelerar / desacelerar | W / S |
| Strafe esq. / dir. | A / D |
| Subir / descer (vertical) | Espaço / C |
| Roll esq. / dir. | Q / E |
| Fogo primário / secundário | Botão esquerdo / direito |
| Skills | 1 / 2 / 3 |

- Suporte completo a **controle (gamepad)**. **[decisão do Claude]** Analógico direito mira, esquerdo strafe/vertical, gatilhos atiram, botões para roll/skills. Teclas não especificadas (modo cruzeiro, dobra, pedir docagem, trocar câmera, mapa, alvo) ficam a cargo do construtor.
- **[decisão do Claude]** Todos os controles remapeáveis desde o início (Unity Input System, já presente no projeto).

### 4.3 Modos de voo
- **Subluz**: velocidade padrão. Combate, mineração, docagem.
- **Dobra ("Velocidade de Dobra")**: muito mais rápida, para viagens entre corpos celestes. **3 níveis** (decisão do autor, 2026-10-31): nível 1 até 500 km/s, nível 2 até 1 500 km/s, nível 3 até 5 000 km/s (ajustado no playtest de 2026-10-31; antes 200/500/1 000). Todas as naves têm os 3 níveis; **W/S trocam o nível durante a dobra** (marchas). O jogador **pilota com o mouse durante a dobra**; alvo não é obrigatório. **[decisão do Claude]** Rampa de ~3 s até o teto; freio automático v ≤ 0,5/s × distância ao alvo quando apontado para ele; v ≤ 1/s × distância à superfície do corpo mais próximo.
  - **Aquecimento** de alguns segundos antes de entrar em dobra — a fuga fica difícil mas não impossível nem frustrante. **[decisão do Claude]** ~5 s; levar dano atrasa (não cancela) o aquecimento.
  - **Na dobra o jogador está seguro** (sem interdição por enquanto).
  - **[decisão do Claude]** Estilo supercruise do Elite: velocidade cresce com a distância de massas grandes; bloqueio por massa (mass lock) perto de estações (5 km) e planetas/luas (1,5 × raio do centro, dentro da zona do alerta de proximidade); sem armas/mineração em dobra; saída a alguns km do alvo selecionado.

### 4.4 Câmeras
- **Primeira pessoa (cockpit)** e **terceira pessoa**, alternáveis em voo.
- **Segurar Z**: olhar livre (o mouse gira a câmera em 1ª ou 3ª pessoa, inclusive em dobra); ao soltar, volta ao centro.

---

## 5. Estações

### 5.1 Serviços
- **Toda estação**: combustível, reparo de danos, recarga de munição balística, mercado, contratos.
- **Apenas algumas**: estaleiro (comprar naves, trocar/melhorar módulos e armas, customização visual).
- Estações **não compram/vendem os mesmos recursos** — cada uma tem sua lista e seus preços (base do comércio).

### 5.2 Docagem
1. No alcance da estação, o jogador **solicita docagem**.
2. A estação responde com a **doca liberada** (número/marcador).
3. Ao chegar **bem perto da doca**, ativa-se o **pouso automático**.
- **[decisão do Claude]** Pedido negado se não houver doca livre (raro na v1). Decolagem também automática até sair da doca.

### 5.3 Terminal (docado)
- Menus sempre em **primeira pessoa**, como "terminal" na tela, estilo Elite Dangerous.
- Ao entrar em **modificações/estaleiro**, a câmera passa para **terceira pessoa** mostrando a nave, para o jogador ver as mudanças.

---

## 6. Contratos e reputação

### 6.1 Tipos
- **Entrega**: levar X unidades de um recurso desta estação para outra. A quantidade é mostrada para o jogador saber se cabe.
- **Coleta**: entregar X unidades de um recurso nesta estação. O jogador decide se minera ou compra em outro lugar. Quantidade mostrada. **Permite várias viagens** (entregas parciais acumulam) quando a quantidade excede a carga.
- **Bounty Hunter (caça de recompensa)**: eliminar pirata/grupo-alvo. Paga bem pelo risco.

### 6.2 Prazo
- Todo contrato tem **prazo**. Se não for cumprido a tempo, o contrato é **perdido** e a **reputação** com a corporação **cai**.
- **[decisão do Claude]** Prazo em tempo de jogo (relógio do mundo), não em tempo real fora do jogo.

### 6.3 Reputação
- Cumprir contratos de uma estação **aumenta a reputação com a corporação dona**.
- Falhar contratos **reduz** a reputação.
- Ao atingir níveis de reputação, o jogador ganha **descontos em melhorias e equipamentos** nas estações daquela corporação.
- **[decisão do Claude]** Por enquanto corporações não são hostis entre si e não há guerra entre elas.

---

## 7. Piratas e combate

- Inimigos iniciais: **piratas**.
- Surgem **sozinhos ou em grupos de até 5**, usando **Scouts e Assaults**.
- Surgem **sempre longe de estações**; costumam rondar **campos de asteroides densos** para atacar mineradores.
- **[decisão do Claude]** Piratas atacam o jogador por conta própria (não só via contrato). "Roubar carga" = atacar; se o jogador for destruído ou largar carga, eles recolhem. Sem abordagem.
- Naves piratas são montadas pelo mesmo sistema de blocos (procedural).
- **[decisão do Claude]** Morte do jogador: a ser definida (ver §10).

---

## 8. HUD e interface

### 8.1 Terceira pessoa (referência: Everspace 2)
- Barras empilhadas no topo esquerdo: **escudo, casco, combustível**.
- Mira central limpa; alvos com colchete, nome e distância; setas para alvos fora da tela.
- Inferior esquerdo: **grupos de fogo** (com munição). Inferior direito: **skills 1–3** com cooldown.
- Contrato ativo com prazo na lateral.
- Velocidade, indicador de cruzeiro, ocupação de carga, estado da dobra (aquecendo / pronta / bloqueada).
- Marcadores coloridos por tipo: estação, pirata, alvo de bounty, asteroide com recurso.
- Cores frias com acentos quentes, sem esconder o espaço. **Inspiração, não cópia** — identidade visual própria.

Referência: `Docs/Design/refs/hud-terceira-pessoa.webp`.

### 8.2 Cockpit (referência: `Docs/Design/refs/cockpit.webp`)
- Informações mostradas **fisicamente** nas telas/painéis da cabine (status do alvo, status da nave, armas, skills, mostradores).
- No vidro, apenas o essencial: mira, marcadores de alvo, arcos de escudo/casco ao redor da mira.
- Moldura do canopy enquadra a visão.

---

## 9. Base técnica

- **Unity 6000.3.9f1**, Input System 1.12 (já no projeto).
- **[decisão do Claude]** Trocar o pacote experimental `com.unity.pipeline` por **URP** (Universal Render Pipeline): bloom/glow para sol, escudos, propulsores; desempenho bom.
- **[decisão do Claude]** Dados de jogo (naves, blocos, módulos, tiers, recursos, estações, corporações, preços, contratos) em **ScriptableObjects**, para balancear sem mexer em código.
- **Idiomas no lançamento: português (PT-BR) e inglês.** **[decisão do Claude]** Todo texto de UI e de dados passa pelo Unity Localization desde a fase 1.
- **Sem VR e sem head tracking.** HUD de tela em terceira pessoa é permitido.
- **[decisão do Claude]** Projeto não está em git; recomendado inicializar repositório git (com .gitignore de Unity) antes de começar a construir.
- **[decisão do Claude]** Salvamento: o jogo precisa salvar progresso (crédito, nave, módulos, carga, reputação, contratos ativos). Formato a cargo do construtor (ex.: JSON).

---

## 9.1 Aprovação e primeira fatia (2026-10-30)

Documento aprovado pelo autor. A **primeira fatia jogável** (substitui a Fase 1 abaixo como primeiro alvo) contém:
- Controles das naves (estilo Everspace, §4) com subluz, cruzeiro e dobra.
- Câmera alternando entre primeira pessoa (cockpit) e terceira pessoa.
- Pelo menos **uma estação** com pedido de docagem, pouso automático e terminal de **compra e venda**.
- **Asteroides para minerar** com laser de mineração e carga.
- **Naves piratas procedurais**: montadas com os blocos seguindo as regras de classe/multiclasse, **sempre com corpo de Assault** (substitui "Scouts e Assaults" do §7). Pintura pirata em **vermelho, preto e branco** — uma cor só ou mescladas. **[decisão do Claude]** Nesta fatia os piratas só surgem e patrulham campos de asteroides; combate fica para a fase de combate.
- **Modo de teste**: tudo gratuito e reputação zerada. **[decisão do Claude]** Comprar no mercado não desconta créditos (preços aparecem normalmente); vender ainda credita, para testar o fluxo.

### 9.2 Setores e jump gates (decisão do autor, 2026-10-30)
- O sistema é dividido em **setores**, estilo X4 Foundations: cada setor é a vizinhança de **um planeta** com suas luas, estações e campos de asteroides.
- Dentro do setor: subluz e dobra, como no §4.3. Entre setores: **jump gates**.
- Jump gate = **"rosquinha" gigante** (toro). **[decisão do Claude]** Raio externo 1 000 m, raio do tubo 150 m (abertura de ~1,7 km). Atravessar o anel leva ao gate pareado no setor vizinho; o jogador sai 1,5 km à frente do gate de chegada, mantendo a velocidade.
- O sol aparece ao fundo em todos os setores (direção e tamanho por setor).
- Setores recebem o **nome do corpo celeste principal** (decisão do autor): Setor Aster (planeta, inicial), Setor Venera (planeta), Setor Centaurus (gigante gasoso com 4 luas), Setor Cinturão (só asteroides, estação central num asteroide gigante).
- **[decisão do Claude]** Só o setor atual fica carregado. A fatia 1 tem os **4 setores**. Rotas: Aster↔Venera, Aster↔Cinturão, Cinturão↔Centaurus. O Cinturão é o setor mais perigoso (campo denso, mais piratas).

## 10. Plano de fases (cada fase jogável)

1. **Voo e mundo** — uma nave em blocos (primitivos), controles estilo Everspace, subluz/cruzeiro/dobra, sistema solar pequeno com origem flutuante, câmeras cockpit e terceira pessoa, HUD básico. **[decisão do Claude]** "HUD básico" = HUD de tela simples na terceira pessoa (escudo/casco/combustível, velocidade, cruzeiro, dobra, marcador do alvo) + o sistema de painéis físicos funcionando numa cabine (mesmos dados), sem polimento visual.
2. **Estações** — pedido de docagem, pouso automático, terminal, combustível/reparo/munição, mercado (compra e venda).
3. **Mineração e economia** — asteroides com recursos, laser de mineração, carga e efeito na velocidade, preços por estação.
4. **Contratos e corporações** — entrega, coleta (multi-viagem), prazos, reputação, descontos.
5. **Combate e piratas** — armas, escudo, casco, skills, grupos de fogo, IA pirata, bounty.
6. **Estaleiro** — comprar naves, módulos e tiers, customização visual (cor por módulo, padrões de pintura) com câmera em terceira pessoa.

Cada fase recebe seu próprio plano de implementação detalhado.

## 11. Em aberto (definir mais tarde, não bloqueia a fase 1)
- Fórmula/valores de upgrade de casco, gerador, motor e escudo por tier.
- Preço das naves, módulos e créditos iniciais.
- O que acontece quando o jogador é destruído (perde carga? seguro estilo Elite?).
- Níveis de reputação e percentuais de desconto.
- Nomes e lore das corporações.
- Nave inicial do jogador.

---

## 12. Fase 2 — Contratos, reputação, save e novas estações (aprovado em 2026-10-31)

### 12.1 Corporações e estações
Lore completa em `Docs/Lore.md`. A **Federação** é dona do sistema; corporações operam com licença.

| Setor | Estação (id) | Corporação | Estilo / docas |
|---|---|---|---|
| Aster | Estação Gollner (`gollner`) | Nova Technology | anel giratório; 4 P, 2 M, 1 G |
| Aster | Posto Avançado Apollo (`apollo`) | Nova Technology | base sobre asteroide em órbita de Aster I; 1 P, 1 M, 1 G; sem estaleiro |
| Venera | Estação Magna (`magna`) | Magna Hauling Co. | plataforma industrial; 4 P, 2 M, 1 G |
| Cinturão | Central do Cinturão (`cinturao_central`) | Atlas Universal | anel (provisório) |
| Centaurus | Doca Centaurus II (`centaurus_ii_yard`) | Federação | anel (provisório) |

- **[decisão do Claude]** Magna e Apollo são estáticos (não giram); só as estações em anel giram.
- A rixa Atlas × Magna e a segurança da Atlas não têm efeito no jogo por enquanto.

### 12.2 Contratos (decisões do autor)
- **Entrega**: a estação fornece a carga; o jogador leva ao destino. Prazo depende da rota: ~30 min dentro do setor (ex.: Gollner → Apollo), maior para outros setores.
- **Requisição**: a estação pede X unidades; o jogador consegue como quiser (minerar/comprar). Várias viagens, entregas parciais acumulam. Prazo longo: **2 h**.
- Prazos em **tempo de jogo** (só corre com o jogo aberto).
- O jogador pode ter vários contratos ao mesmo tempo.

**[decisões do Claude]**
- Mural por estação: 3–6 ofertas, renova a cada 20 min de jogo; ofertas geradas do mercado da estação (o que vende → entrega; o que compra → requisição).
- Máximo de **5 contratos ativos**.
- Entrega: só aceita se a carga inteira couber; a carga fica **reservada** (não pode ser vendida). Prazo = 30 min + 25 min por salto de gate. Recompensa = 25% do valor da carga + 400 cr + 600 cr por salto.
- Requisição: quantidade 40–150 (pode exceder o porão). Recompensa = 160% do valor de mercado.
- Falha (prazo vence) ou abandono: perde reputação; na entrega, a carga reservada é confiscada e cobra-se multa de 50% do valor (até o saldo disponível).
- HUD: contratos ativos com prazo na lateral; no cockpit, painel esquerdo.

### 12.3 Reputação (decisão do autor: funciona com descontos)
- Por corporação, de −100 a +100, começa em 0. Entrega cumprida +3 (+1 por salto); requisição +5; falha −5 (entrega) / −6 (requisição).
- **[decisão do Claude]** Desconto no preço de **compra** nas estações da corporação: rep ≥ 10 → 2%, ≥ 25 → 5%, ≥ 50 → 8%, ≥ 80 → 12%.

### 12.4 Economia
- **Modo de teste desligado** (decisão do autor): comprar debita créditos.
- Mercados complementares entre Gollner, Apollo e Magna para o comércio valer a pena.
- Futuro: **Mercado Galáctico** (demanda variável, atualiza a cada 3 h de jogo com alerta "Atualização no Mercado Galáctico") — próxima fase.

### 12.5 Save (decisão do Claude — necessário com prazos de 2 h)
- Save automático em JSON (`persistentDataPath/save.json`): créditos, carga, reputação, contratos ativos, relógio de jogo, setor e posição. Ao docar, decolar, saltar de gate e sair do jogo. Carrega ao iniciar.
- Murais não são salvos (regeneram). Atalho de desenvolvimento para novo jogo: **F10** (apaga o save).

### 12.6 Plataformas e docagem
- Plataformas estilo Elite: faixas laranja laterais, cantoneiras, número **pintado** `01`…`07` na borda (sem P/M/G, sem holograma do tamanho).
- Ao conceder docagem: aviso em destaque "Solicitação de pouso recebida e permissão concedida. Por favor, dirija-se até a doca 0X." + holograma do número **só** sobre a doca concedida (com a seta azul).
- Ao pousar: "Bem-vindo à <estação>" (~2 s) e então o terminal.

### 12.7 Mapa do sistema, rota e dobra com alvo (decisão do autor, 2026-10-31)
Referências: `Docs/Design/refs/mapa-sistema.webp`, `Docs/Design/refs/mapa-setor-zoom.webp`.
- **M** abre o mapa. Visão geral: estrela no centro, corpos principais em órbita, cada setor delimitado por linhas.
- **Scroll** sobre um setor dá zoom: planeta, luas, estações, campos, portais. Scroll para trás volta.
- Clique num ponto: nome, descrição, facção (Federação), corporação, foco de comércio e botão **Definir rota**.
- Destino da rota: **losango roxo** no HUD. Em outro setor, a rota aponta o próximo portal do caminho.
- **Ctrl+T** limpa rota e alvo.
- **A dobra só ativa com alvo** (selecionado com T ou o próximo ponto da rota). **[decisão do Claude]** A dobra só sai sozinha a 10 km do alvo quando a nave está indo na direção dele.
- **[decisão do Claude]** Rota é salva no save. Descrições dos lugares são provisórias. Órbitas continuam estáticas (movimento orbital no backlog).

## 13. Fase 2B — Ajustes de jogo, módulos e cockpit (decisões do autor, 2026-10-31)

### 13.1 Sistema
- O sistema se chama **Tau Ceti** (estrela real; planetas não confirmados → os nossos são ficção). Poder dominante: **Federação**.
- Corporação dominante por setor: Aster → Nova Technology; Venera → Magna Hauling Co.; Cinturão → Atlas Universal; Centaurus → Federação.

### 13.2 Pilotagem
- **Peso em dobra**: a partir do nível 1 a nave fica mais pesada, mais no 2 e ainda mais no 3 — só ajustes de direção, sem curvas fechadas. **[decisão do Claude]** Giro máximo 50% / 30% / 15% do normal e resposta angular proporcionalmente mais lenta.
- **Penalidade de carga** reduzida. **[decisão do Claude]** −15% de velocidade máxima com porão cheio (antes −40%).
- **Boost (Shift esquerdo)**: barra consumida enquanto segura; +50% na velocidade máxima. Toda nave P: **150** de boost. Melhorável → **módulo Propulsor**. **[decisão do Claude]** Consome 30/s, recarrega 15/s após 1 s sem usar; não funciona em dobra.
- **Skill "Sobrecarga de Motor"** (antes "Boost"): impulso extremo em subluz, até **200%** da velocidade máxima padrão da nave, por tempo limitado.
- **Nenhuma skill ou armamento funciona em dobra.**

### 13.3 Estações e docagem
- Distâncias voltam às originais da fatia 1 (desfeitos o −20% geral e o +20% das estações). O Apollo mantém o ajuste individual (perto de Aster I, virado para a Gollner).
- **Bloqueio de massa de estações: 7 km.** **Pedido de docagem: até 6 km.**
- **Pouso automático** só começa com a nave **em cima** da plataforma e próxima dela.

### 13.4 Módulos (atualiza §3.4)
Além de gerador, motor, casco e skill:
- **Propulsor (Boost)**: capacidade da barra de boost (P padrão 150). Melhorável.
- **Scanner de sinal**: alcance para detectar **naves e asteroides mineráveis** (padrão **5 km** em todas as naves atuais). Fora do alcance, não aparecem no HUD. Estações, planetas, luas e portais sempre aparecem. Melhorável.

### 13.5 Ícones (HUD e mapa iguais)
- Planeta: círculo. **[decisão do Claude]** Lua: círculo menor. Portal: anel duplo. Campo de asteroides: 3 círculos irregulares.
- Estação: círculo com **$** no meio.
- Asteroide minerável: círculo irregular.
- Nave: quadrado. Nave pirata: quadrado **vermelho**.
- Alvo marcado: triângulo invertido **branco** (neutro), **verde** (aliado — **[decisão do Claude]** reputação ≥ 25 com a corporação), triângulo normal **vermelho** (inimigo; por enquanto só piratas).

### 13.6 Painéis do cockpit
Toda nave: **3 painéis embaixo e 2 em cima** (formato/posição variam por cabine; conteúdo igual, layout adaptável).
- **Central (baixo)**: título "STATUS DA NAVE"; CASCO, ESCUDOS, BOOST (barras na largura toda); VELOCIDADE. Título "INFORMAÇÕES DO SISTEMA"; nome do sistema; nome do setor; poder dominante do sistema (Federação); corporação dominante do setor.
- **Esquerdo (baixo)**: alvo marcado; distância; saúde e escudo do alvo (se houver); filiação (corporação, neutro ou pirata — pirata em vermelho).
- **Direito (baixo), com abas**: CENTRAL DE MENSAGENS (feed com hora real), CONTRATOS ATIVOS, REPUTAÇÃO, ARMAMENTO E MÓDULOS. **F** ativa o modo mouse para clicar; **6** vai para a aba à direita, **7** para a esquerda, sem dar a volta.
- **Cima esquerdo**: hora real, centralizada.
- **Cima direito**: nome da nave (grande; máx. 18 caracteres, definido na compra), classe e fabricante (menores). **[decisão do Claude]** Até existir estaleiro: nome padrão "PULSE-01", renomeável na aba Serviços do terminal.
- **[decisão do Claude]** Em terceira pessoa o HUD de tela continua como está; as abas existem nos painéis físicos.

## 14. Fase 2C — Ajustes de playtest (decisões do autor, 2026-10-31)
- **Bloqueio de massa padrão 2 km** (estações, postos, portais). **Pedido de docagem: 2 km.** **[decisão do Claude]** Planetas/luas mantêm o bloqueio ligado à zona do alerta (1,1 R do centro). A dobra sai a **3 km** do alvo.
- Saídas da dobra aparecem no feed com o motivo (bloqueio de massa com o nome do objeto, destino alcançado, emergência).
- **Boost**: recarga 70% mais lenta (4,5/s). **Sem limite de velocidade** enquanto houver barra; ao acabar, desacelera gradualmente até o máximo normal. Em cruzeiro, o alvo sobe junto.
- **Asteroides**: **[decisão do Claude]** ~25% têm recurso. No HUD só aparecem quando marcados, como "Asteroide". O painel esquerdo mostra o recurso ou "Sem recursos mineráveis".
- **Número da plataforma** pintado legível de cima.
- **Portal de salto** novo (ref. `Docs/Design/refs/portal-de-salto.webp`), **grande** para naves capitais no futuro: **[decisão do Claude]** raio externo 2 km, abertura ~1,7 km de raio.
- **Cockpit**: cima-esquerda = feed; cima-direita = informações do sistema; central = **minimapa (radar)** com a nave no centro; direita = abas **NAVE** ("NOME - Modelo", barras, velocidade), **CARGA** (conteúdo + barra), **CONTRATOS**, **REPUTAÇÃO**, **MÓDULOS**, **ENCAIXES** (grupo 1 = botão esquerdo, grupo 2 = botão direito por arma).

## 15. Fase 2D — Nave M, fila de docagem e tráfego NPC (decisões do autor, 2026-10-31)

### 15.1 Hulker (primeira nave M)
Master Hauling · **Hulker** · tamanho **M** · classe Cargo · carga **600** · encaixes **3** · subluz máx. **300 m/s** · aceleração **igual à Scout (40)** · casco **200 HP** · não multiclasse. Ref.: `Docs/Design/refs/naves/hulker.webp`. Comprimento ≈ 3 naves P, largura ≈ 2 naves P. Sem asas (cabine, corpo, cauda).
**[decisão do Claude, aprovada]** giro 35°/s, 1 slot de skill, boost 200, scanner 6 km. **Só NPC** até existir estaleiro.

### 15.2 Docagem
- Cada nave **só pousa na plataforma do seu tamanho** (P, M, G).
- Sem vaga: **fila**, com aviso no feed "Aguardando na fila de docagem. Por favor aguarde. Previsão X segundos". Ao liberar: seta + mensagem de concessão.
- NPC docado: no máximo **20 s (P), 45 s (M), 60 s (G)**. NPCs também esperam na fila.

### 15.3 Tráfego NPC
- **Patrulhas** (corpo de Assault): transitam entre estações do mesmo setor, pousam e decolam; spawn no estilo dos piratas. **Afastam piratas** (sem combate).
- **Comércio P**: sozinhas ou em grupos de até 3; com 3, uma patrulha acompanha.
- **Comércio M** (Hulker): chance de 1 ou 2 patrulhas P de escolta.
- **Escoltas não docam**: orbitam a estação até o cargueiro decolar e seguem junto.
- **Destinos aleatórios** entre estações e setores (A→B→C, A→C, C→B...).
- **[decisão do Claude]** Só o setor atual existe: NPC com destino em outro setor entra no portal e some; NPCs também chegam saindo dos portais. Patrulhas pintadas com a cor da corporação dominante do setor. ~8 grupos ativos por setor, reposição a cada 45–90 s. NPC docado sorteia entre metade e o máximo do tempo.

### 15.4 Peso em dobra
+20% a cada nível: giro 40% / 24% / 12% do normal.
