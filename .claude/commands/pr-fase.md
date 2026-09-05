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
6. Quando ele voltar com o status dos checks e o comentário do `sonarqubecloud[bot]`, confira as
   **três condições de fechamento**, todas independentes de a Quality Gate passar:
   **New issues = 0**, **Security Hotspots = 0** e **Duplication on New Code = 0.0%**.
   New issues > 0 exige a lista linha a linha (`bash .claude/scripts/sonar-annotations.sh <PR>`, que
   o `entrega-pr` já traz). Security nunca fica em aberto. Duplicação se resolve extraindo para o
   padrão reutilizável, nunca copiando o bloco de novo.
7. Com a lista na mão, **decida a rota de cada apontamento**:
   - 1–2 linhas, mecânico e óbvio → corrija você mesma, inline;
   - triagem de falso positivo, pragma, apontamento em lote → agente `sonar`;
   - apontamento em código de domínio → o agente dono do arquivo (`dev-viewmodels`, `dev-xaml`,
     `cripto-backup`, `i18n`), com o `sonar` só validando a forma da correção.
   Depois de corrigir, faça novo push pelo `entrega-pr` e repita o passo 6 até "0 New issues".
8. Registre o link do PR em `plano-rodada.md`. **Nunca faça merge.**
