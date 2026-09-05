---
description: Fecha a fase - commit, push, PR e ciclo do SonarCloud
argument-hint: <número da fase>
---

Feche a fase **$1**.

1. Leia o diff completo da branch (`git diff origin/main...HEAD` e `git status --short`) e confira
   contra o "Pronto quando" da fase, em `.claude/projeto/plano-rodada.md` e no artifact.
2. Peça ao `verificador` o `dotnet test` dos dois projetos e o checklist estático de XAML dos
   arquivos tocados. Não siga com falha em aberto.
3. Atualize `.claude/projeto/plano-rodada.md`: status da fase $1 (o link do PR entra depois).
4. **Você redige** — o `entrega-pr` não escreve texto:
   - nome da branch no padrão `<plano-feature-da-rodada>/fase$1-<nome-curto>`;
   - mensagem de commit;
   - título do PR;
   - corpo do PR, explicando **o que mudou e por quê** (PR nunca é mudo), com o "Pronto quando" da
     fase como checklist.
5. Dispare o `entrega-pr` passando os quatro textos prontos.
6. Quando ele voltar com o status dos checks e o comentário do `sonarqubecloud[bot]`: leia,
   **decida** o que é corrigível e delegue a correção ao `sonar` (com nova varredura pelo
   `verificador`). Corrija mesmo com a Quality Gate verde — o alvo é "0 New issues".
7. Registre o link do PR em `plano-rodada.md`. **Nunca faça merge.**
