# PulseFactor — Backlog

Ideias e ajustes combinados que ainda não entraram no jogo.

## Plataformas de pouso estilo Elite (pedido em 2026-10-31, não prioritário)
Referência: print de "PAD 04" do Elite Dangerous (enviado no chat).
- Faixas laranja nas laterais e cantoneiras marcando a área de pouso.
- Número **pintado** na borda da plataforma: `01`, `02`, … `07` (sem P/M/G; sem holograma do tamanho).
- Ao pedir docagem: mensagem em destaque (topo em 3ª pessoa / painel central no cockpit):
  "Solicitação de pouso recebida e permissão concedida. Por favor, dirija-se até a doca 0X."
- Número **holográfico** só sobre a doca autorizada (junto com a seta azul); as outras não mostram holograma.
- Ao pousar: tela "Bem-vindo à <nome da estação>" (~2 s) e só depois o terminal da estação.

## Outros pendentes já combinados
- Jump gates: fila/custo/visual detalhado (ver design §9.2).
- Relação fabricantes × corporações (quem vende o quê).
- Posição dos encaixes de arma em cada nave.
- Lore e nomes finais das corporações.

## Ajustes menores da revisão final (2026-10-31)
- Erro de conteúdo dos piratas aborta o carregamento do setor (gates são criados depois dos campos).
- Segurar W enquanto a dobra aquece já pula para o nível 2.
- Patrulha pirata confere chegada ao waypoint com posição do quadro anterior.
- Alocações de memória por quadro (HUD, registros de alvos).
- Materiais, malhas e texturas não são liberados ao trocar de setor.
- Texto "PT / EN" fixo no terminal.
- Passos de quantidade no terminal (10 → 1 → 11); mensagem de compra mostra créditos no modo de teste.
- Proxies de corpos celestes podem se sobrepor na ordem errada (lua dentro do planeta).
- Mensagem de "saída de emergência" quando o alerta só cancela o aquecimento.
- Bloqueio de massa não é rechecado durante o aquecimento da dobra.
- Rotação da estação usa a posição interpolada da nave.
- Lacunas de teste: gerador sem cockpit/cauda/asa; caminho de autorização perdida.
- Economia: naves comerciais NPC compram/vendem e mexem nos estoques das estações (hoje só o jogador mexe).
- Economia: entregas de contratos também abastecem o estoque da estação de destino.
