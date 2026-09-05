---
description: Roda a varredura local do SonarAnalyzer e tria os apontamentos
---

1. Dispare o agente `verificador` com a **Receita 4** (varredura local do Sonar): criar o
   `Directory.Build.props` temporário com `SonarAnalyzer.CSharp`, compilar os projetos `net10.0`,
   coletar os `warning S####` com arquivo e linha, apagar o arquivo e **confirmar a remoção**.
2. Passe a lista que ele devolver ao agente `sonar`, junto com o escopo do que mudou nesta branch
   (`git diff --stat origin/main...HEAD`), para ele triar e corrigir seguindo o catálogo de falsos
   positivos.
3. Depois das correções, peça ao `verificador` uma segunda varredura e o `dotnet test` dos dois
   projetos.
4. Confirme, antes de encerrar, que `Directory.Build.props` **não existe** na raiz.

Lembre que o perfil do pacote NuGet é mais estreito que o "Sonar way" do SonarCloud: local limpo
reduz o ruído, mas não substitui a leitura do comentário do bot no PR.
