# FIAP Tech Challenge — Chassi

Pacote .NET 10 com logs JSON, métricas, traces, correlação HTTP, tratamento de exceções, health checks e envelope genérico de mensagens. Não contém regras de negócio ou persistência.

Abra `Chassis.slnx`. A biblioteca fica em `src/`, os testes em `tests/` e o consumidor NuGet em `samples/`. Consulte a [API pública](src/Maieru.FiapTechChallenge.Chassis/README.md).

Para validar localmente, na raiz, gere o pacote antes de construir o exemplo:

```powershell
dotnet pack src/Maieru.FiapTechChallenge.Chassis -c Release -o artifacts/packages
dotnet build Chassis.slnx -c Release
dotnet test Chassis.slnx -c Release --no-build --no-restore
```

Ao alterar a biblioteca, use uma versão inédita em `dotnet pack -p:Version=<versao>` e `dotnet build -p:ChassisVersion=<versao>` para evitar o cache NuGet. O exemplo consome o `.nupkg` por `PackageReference`.

O CI valida testes, cobertura mínima de 80%, consumidor HTTP e Sonar. Configure no **novo repositório**:

| Tipo | Nome | Valor |
| --- | --- | --- |
| Secret | `SONAR_TOKEN` | Token de análise do Sonar |
| Variable | `SONAR_PROJECT_KEY` | Chave do projeto do chassi; organização `maieru` |
| Variable | `NUGET_USER` | Nome do perfil NuGet |

A tag `chassis-v0.1.0`, apontando para um commit revisado em `main`, aciona `publish-chassis.yml`. A política NuGet Trusted Publishing deve usar owner **`Maieru`**, repository **`fiap-tech-challenge-chassis`**, workflow **`publish-chassis.yml`**, environment vazio e pacote **`Maieru.FiapTechChallenge.Chassis`**. A publicação usa OIDC, sem API key permanente.