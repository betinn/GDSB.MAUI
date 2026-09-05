---
description: Abre uma fase do plano da rodada, monta o briefing e distribui para os subagentes
argument-hint: <número da fase>
---

Abra a fase **$1** da rodada em execução, como orquestradora.

1. Leia `.claude/projeto/plano-rodada.md`: confirme que a fase $1 existe, que as fases de que ela
   depende estão concluídas, e pegue a URL do artifact.
2. Leia no artifact a seção da fase $1 — arquivos a criar/alterar, regras e o "Pronto quando".
   Se precisar localizar código antes de decidir, delegue ao `mapeador`.
3. **Decida** o desenho e o corte de escopo. Isso não se delega.
4. Monte o briefing fechado de cada subagente, com: objetivo em uma frase, critério de pronto,
   lista fechada de arquivos, as armadilhas relevantes **copiadas** de `plano-rodada.md`, o que não
   fazer (não commitar, não abrir PR, não trocar decisão fechada) e como verificar.
5. Dispare os agentes do domínio, paralelizando o que não se cruza. Lembre: **só o `i18n` escreve
   `.resx`** — os outros consomem chaves já criadas.
6. Quando voltarem, **leia o diff**, não o relatório. Peça a verificação ao `verificador` e a
   varredura ao par `verificador` + `sonar`.
7. Não commite nem abra PR aqui — isso é o `/pr-fase`.
