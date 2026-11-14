# FIAP Tech Challenge — Chassi

Bibliotecas .NET 10 para logs JSON, métricas, traces, correlação HTTP, tratamento de exceções, health checks e publicação Kafka. Não contêm entidades de negócio ou persistência.

| Pacote | Versão declarada no projeto | Conteúdo |
| --- | --- | --- |
| `Maieru.FiapTechChallenge.Chassis` | 1.1.1 | Recursos HTTP/telemetria, `MessageEnvelope<T>` e `IMessagingService` |
| `Maieru.FiapTechChallenge.Messaging.Kafka` | 0.1.1 | Publicador Kafka com confirmação de persistência |

As versões NuGet de release vêm da tag. `Directory.Build.props` ainda fornece 1.1.0/0.1.0 como defaults dos consumidores de exemplo; para testar os pacotes recém-gerados, informe explicitamente as duas propriedades conforme abaixo.

## Estrutura e pré-requisitos

Abra [Chassis.slnx](Chassis.slnx). `src` contém as bibliotecas, `tests` as suítes e `samples` consumidores reais por `PackageReference`. Use .NET SDK 10; os comandos abaixo usam PowerShell 7. A integração Kafka também exige Docker com containers Linux.

- [API pública do chassi](src/Maieru.FiapTechChallenge.Chassis/README.md).
- [Adaptador Kafka](src/Maieru.FiapTechChallenge.Messaging.Kafka/README.md).
- [Padrão de mensageria](https://github.com/Maieru/fiap-tech-challenge/blob/main/docs/arquitetura/mensageria.md).

## Validar candidatos locais

Na raiz, use versões inéditas para evitar reutilizar o cache NuGet:

```powershell
$chassisVersion = '1.1.1-local.1'
$kafkaVersion = '0.1.1-local.1'
dotnet pack src/Maieru.FiapTechChallenge.Chassis -c Release -o artifacts/packages "-p:Version=$chassisVersion"
dotnet pack src/Maieru.FiapTechChallenge.Messaging.Kafka -c Release -o artifacts/packages "-p:Version=$kafkaVersion" "-p:ChassisVersion=$chassisVersion"
dotnet restore Chassis.slnx "-p:ChassisVersion=$chassisVersion" "-p:KafkaMessagingVersion=$kafkaVersion"
dotnet build Chassis.slnx -c Release --no-restore "-p:ChassisVersion=$chassisVersion" "-p:KafkaMessagingVersion=$kafkaVersion"
dotnet test Chassis.slnx -c Release --no-build --no-restore "-p:ChassisVersion=$chassisVersion" "-p:KafkaMessagingVersion=$kafkaVersion"
```

`NuGet.Config` combina `artifacts/packages` e NuGet.org com source mapping. Empacote primeiro o chassi, depois o adaptador e só então os exemplos. `NuGet.Public.Config` restaura somente do feed público; use-o para verificar versões já disponibilizadas sem depender de candidatos locais. Não apague ou substitua uma versão publicada para validar outra compilação.

## Kafka real e exemplos

Na raiz:

```powershell
docker compose -f tests/kafka.compose.yaml up -d --wait --wait-timeout 120
$env:KAFKA_TEST_BOOTSTRAP_SERVERS = '127.0.0.1:19092'
dotnet test Chassis.slnx -c Release --no-build --no-restore "-p:ChassisVersion=$chassisVersion" "-p:KafkaMessagingVersion=$kafkaVersion"
dotnet run --project samples/Maieru.FiapTechChallenge.Messaging.Sample -c Release --no-build --no-restore -- 127.0.0.1:19092
docker compose -f tests/kafka.compose.yaml down
Remove-Item Env:KAFKA_TEST_BOOTSTRAP_SERVERS
```

O teste de broker é ignorado quando a variável não está definida; isso não comprova a integração Kafka. O exemplo de mensageria cria um tópico único, publica e consome um envelope e remove o tópico ao terminar.

Para conferir o consumidor HTTP já compilado:

```powershell
dotnet run --project samples/Maieru.FiapTechChallenge.Chassis.Sample -c Release --no-build --no-restore --no-launch-profile --urls http://localhost:5099
```

Consulte [localhost:5099/health/ready](http://localhost:5099/health/ready) e envie `X-Correlation-ID` para verificar a correlação na resposta e nos logs.

## CI e publicação

CI constrói candidatos imutáveis, inicia Kafka isolado, executa testes, verifica consumo NuGet, exige cobertura mínima de 80% e análise Sonar. Configure:

| Tipo | Nome | Uso |
| --- | --- | --- |
| Secret | `SONAR_TOKEN` | Análise Sonar |
| Variable | `SONAR_PROJECT_KEY` | Projeto da organização maieru |
| Variable | `NUGET_USER` | Nome do perfil NuGet.org, sem e-mail/token |

A publicação usa Trusted Publishing/OIDC, sem API key permanente:

| Pacote | Tag | Workflow |
| --- | --- | --- |
| Chassi | `vX.Y.Z` | `publish-chassis.yml` |
| Adaptador | `kafka-vX.Y.Z` | `publish-kafka.yml` |

Configure a política NuGet com owner `Maieru`, repository `fiap-tech-challenge-chassis`, o workflow correspondente e environment vazio. A tag deve ter formato válido e apontar para commit integrado em main. A versão do chassi exigida pelo adaptador precisa estar disponível no feed público antes de publicar o adaptador; o workflow Kafka usa `NuGet.Public.Config`.

## Contrato e diagnóstico

O chassi não valida payloads de negócio nem implementa consumidores, Outbox ou transações distribuídas. Publicação confirmada pelo broker não comprova processamento pelo consumidor ou commit do banco. Serviços cuidam de validação, deduplicação, offsets e reconciliação.

Se houver `NU1101/NU1102`, confira versão, feed local, source mapping e ordem de empacotamento. Se Kafka falhar, confira Docker, porta 19092 e `docker compose -f tests/kafka.compose.yaml logs kafka`. Sem Collector, mantenha `EnableOtlp=false` para uso local. Licença [MIT](LICENSE).
