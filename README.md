# task-queue-api

API para criação e acompanhamento de tarefas processadas em background via RabbitMQ e MongoDB.

---

## Sobre o projeto

O projeto recebe tarefas através de uma API REST, publica as mensagens no RabbitMQ e realiza o processamento em background através de um Worker Service.

Cada tarefa possui um ciclo de vida acompanhado pela aplicação, permitindo consultar o status em tempo real.

O objetivo do projeto é demonstrar conceitos como:

- processamento assíncrono
- mensageria
- workers em background
- controle de concorrência
- tratamento de falhas
- retry de processamento
- separação de responsabilidades

---

## Pré-requisitos

Para executar a aplicação:

- Docker Desktop instalado e em execução
- Docker Compose disponível

Opcionalmente, para executar os testes fora do Docker:

- .NET 10 SDK

---

## Como executar

Clone o repositório:

```bash
git clone https://github.com/gustavomachado7/task-queue-api.git
```

Acesse a pasta:

```bash
cd task-queue-api
```

Suba os serviços:

```bash
docker compose up --build
```

Serão iniciados:

- MongoDB
- RabbitMQ
- API
- Worker

A aplicação aguarda os serviços de infraestrutura estarem disponíveis antes de iniciar.

API:

    http://localhost:8080

Swagger:

    http://localhost:8080/swagger

---

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/` | Informações da API |
| GET | `/health` | Health check |
| POST | `/jobs` | Criar tarefa |
| GET | `/jobs` | Listar tarefas |
| GET | `/jobs/{trackingId}` | Consultar tarefa |

---

## Criar tarefa

POST /jobs

Content-Type: application/json

Exemplo:

```json
{
  "category": "EnviarEmail",
  "payload": "{\"destinatario\":\"joao@email.com\",\"assunto\":\"Bem vindo\"}"
}
```

Para simular falha:

```json
{
  "category": "ForceError",
  "payload": "{\"motivo\":\"teste de retry\"}"
}
```

---

## Consultar tarefas

Todas:

```bash
GET /jobs
```

Por status:

```bash
GET /jobs?status=Created
GET /jobs?status=Pending
GET /jobs?status=Processing
GET /jobs?status=Completed
GET /jobs?status=Failed
```

---

## Testando em escala

Para criar 50 tarefas variadas de uma vez, rode na raiz do projeto:

```cmd
criar-tarefas.bat
```

Para acompanhar o processamento em tempo real:

```bash
docker compose logs worker -f
```

Para ver todas as tarefas e seus status:

```
http://localhost:8080/jobs
```

---

## Arquitetura

Fluxo principal:

```
Cliente
   |
   ▼
TaskQueue.Api
   |
   ▼
MongoDB
   |
   ▼
RabbitMQ
   |
   ▼
TaskQueue.Worker
   |
   ├── Sucesso → Completed
   |
   └── Falha → Retry → Failed
```

Organização da solução:

```
TaskQueue
├── TaskQueue.Core
├── TaskQueue.Infrastructure
├── TaskQueue.Api
├── TaskQueue.Worker
└── TaskQueue.Tests
```

### Core

Camada central da aplicação.

Contém modelos, enums e interfaces.

Não possui dependências externas, concentrando as regras principais do domínio.

### Infrastructure

Implementa as interfaces definidas no Core.

O `MongoJobRepository` utiliza operações atômicas do MongoDB (`FindOneAndUpdate`) para evitar que múltiplos workers processem a mesma tarefa ao mesmo tempo.

### Api

API REST construída com ASP.NET Core Minimal API.

Responsável por:

- endpoints HTTP
- validação
- documentação Swagger
- tratamento global de exceções

### Worker

Serviço responsável pelo consumo das mensagens do RabbitMQ.

Realiza:

- processamento em background
- atualização de status
- controle de tentativas
- retry em caso de falha

### Tests

Projeto responsável pelos testes automatizados.

Utiliza:

- xUnit
- Moq

Cobre:

- validações de entrada
- regras de negócio
- processamento das tarefas
- alteração de status
- cenários de falha e retry

As dependências externas são isoladas utilizando mocks, permitindo executar os testes sem MongoDB ou RabbitMQ.

---

## Ciclo de vida

```
Created
   |
   ▼
Pending
   |
   ▼
Processing
   |
   ├── Completed
   |
   └── Falha → volta pra Pending → nova tentativa
                                       |
                                       └── após 3 tentativas → Failed
```

---

## Testes automatizados

A suíte possui testes cobrindo:

- validações
- regras de negócio
- processamento
- falhas
- retry

Os testes usam mocks — não precisam da aplicação rodando nem de MongoDB ou RabbitMQ.

Executar via Docker:

```bash
docker compose --profile tests run --rm tests
```

Executar localmente:

```bash
dotnet test
```

---

## Logs

Todos os serviços:

```bash
docker compose logs -f
```

Worker:

```bash
docker compose logs worker -f
```

API:

```bash
docker compose logs api -f
```

---

## Parar e subir

```bash
docker compose down          # para os containers
docker compose down -v       # para e remove os dados do banco
docker compose up            # sobe novamente
docker compose up --build    # sobe reconstruindo as imagens
```

---

## Decisões técnicas

### MongoDB

Escolhido pela flexibilidade e suporte a operações atômicas.

O uso de `FindOneAndUpdate` garante que apenas um worker consiga pegar uma tarefa para processamento.

### RabbitMQ

Utilizado para desacoplar a API do processamento.

Permite processamento assíncrono e evita bloquear a requisição HTTP enquanto a tarefa é executada.

### Minimal API

Escolhida por ser uma abordagem mais simples e direta do ASP.NET Core para APIs menores.

### Worker Service

Utilizado para processamento em background seguindo o modelo nativo do .NET.

### Retry

Falhas durante processamento retornam a tarefa para nova tentativa.

Após o limite configurado, a tarefa é marcada como `Failed`.

---

## Próximas evoluções

- Implementar Dead Letter Queue
- Adicionar métricas de processamento
- Adicionar rastreamento distribuído
- Adicionar autenticação
- Implementar rate limiting
- Adicionar dashboards de observabilidade