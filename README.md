# FIAP Tech Challenge — Chassi

Pacotes .NET 10 com logs JSON, métricas, traces, correlação HTTP, tratamento de exceções, health checks e publicação Kafka. Não contêm regras de negócio ou persistência.

Abra `Chassis.slnx`. As bibliotecas ficam em `src/`, os testes em `tests/` e os consumidores NuGet em `samples/`.
Consulte a [API pública](src/Maieru.FiapTechChallenge.Chassis/README.md), o [adaptador Kafka](src/Maieru.FiapTechChallenge.Messaging.Kafka/README.md),
o [padrão de mensageria](https://github.com/Maieru/fiap-tech-challenge/blob/main/docs/arquitetura/mensageria.md) e as [decisões arquiteturais](https://github.com/Maieru/fiap-tech-challenge/blob/main/docs/ADRs/README.md).

O chassi 1.1.0 acrescenta IMessagingService e preserva MessageEnvelope<T>. O adaptador Kafka 0.1.0 é separado:
consumidores HTTP do chassi não recebem Confluent.Kafka. Contratos de negócio pertencem ao Application.Messaging de cada produtor.

Para validar localmente, na raiz, gere o pacote antes de construir o exemplo:

```powershell
dotnet pack src/Maieru.FiapTechChallenge.Chassis -c Release -o artifacts/packages
dotnet pack src/Maieru.FiapTechChallenge.Messaging.Kafka -c Release -o artifacts/packages
dotnet build Chassis.slnx -c Release
dotnet test Chassis.slnx -c Release --no-build --no-restore
```

Ao alterar uma biblioteca, use versões inéditas para evitar cache NuGet. Em candidatos conjuntos, empacote o chassi com
`-p:Version=<versao>`, o adaptador com `-p:Version=<versao> -p:ChassisVersion=<versao>`, e compile com
`-p:ChassisVersion=<versao> -p:KafkaMessagingVersion=<versao>`. Os exemplos consomem os `.nupkg` por `PackageReference`.

O teste real Kafka exige Docker e KAFKA_TEST_BOOTSTRAP_SERVERS; o [guia](https://github.com/Maieru/fiap-tech-challenge/blob/main/docs/arquitetura/mensageria.md#verificação-local-desta-etapa)
contém os comandos para iniciar o broker isolado, testar e encerrar. Sem a variável, somente esse teste é ignorado.
O CI inicia o broker, executa a integração e verifica o consumidor NuGet.

O CI valida testes, cobertura mínima de 80%, consumidor HTTP e Sonar. Configure no **novo repositório**:

| Tipo | Nome | Valor |
| --- | --- | --- |
| Secret | `SONAR_TOKEN` | Token de análise do Sonar |
| Variable | `SONAR_PROJECT_KEY` | Chave do projeto do chassi; organização `maieru` |
| Variable | `NUGET_USER` | Nome do perfil NuGet |

A tag `v1.1.0`, no padrão `vX.Y.Z` e apontando para um commit revisado em `main`, aciona `publish-chassis.yml`.
A política NuGet Trusted Publishing deve usar owner **`Maieru`**, repository **`fiap-tech-challenge-chassis`**,
workflow **`publish-chassis.yml`**, environment vazio e pacote **`Maieru.FiapTechChallenge.Chassis`**.
A publicação usa OIDC, sem API key permanente.

O adaptador usa tag independente `kafka-v0.1.0`, workflow `publish-kafka.yml` e pacote
`Maieru.FiapTechChallenge.Messaging.Kafka`, com os mesmos owner/repository/environment.
Publicar o chassi 1.1.0 antes do adaptador 0.1.0. O workflow do adaptador restaura somente de NuGet.org;
não depende do feed local de candidatos. Nenhuma tag ou publicação foi executada nesta revisão.

O workflow valida separadamente o formato da tag, a presença do commit em `main` e `NUGET_USER`.
Esse usuário é o nome do perfil NuGet.org, sem email ou token; configure-o em **Variables**.
