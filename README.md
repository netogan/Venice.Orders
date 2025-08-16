# Venice.Orders

## Sobre o Projeto

O **Venice.Orders** é uma API de gerenciamento de pedidos desenvolvida em .NET 8 que demonstra a implementação de uma arquitetura robusta e escalável, seguindo os princípios SOLID e utilizando padrões de design modernos para sistemas distribuídos.

## Arquitetura

### Padrão Arquitetural: Clean Architecture (Arquitetura Limpa)

O projeto implementa a **Clean Architecture**, dividindo o sistema em camadas bem definidas:

```
Venice.Orders/
├── src/
│   ├── Venice.Orders.Domain/          # Entidades de domínio e regras de negócio
│   ├── Venice.Orders.Application/     # Casos de uso e contratos
│   ├── Venice.Orders.Infrastructure/  # Implementações técnicas (dados, mensageria)
│   └── Venice.Orders.Api/             # Interface de entrada (Controllers, DI)
└── tests/
    └── Venice.Orders.Api.Tests/       # Testes automatizados
```

**Justificativas da Clean Architecture:**

1. **Independência de Frameworks**: O domínio não depende de tecnologias específicas
2. **Testabilidade**: Cada camada pode ser testada independentemente
3. **Inversão de Dependências**: Dependências apontam para dentro (domínio)
4. **Flexibilidade**: Facilita mudanças tecnológicas sem impactar regras de negócio
5. **Separação de Responsabilidades**: Cada camada tem um propósito específico

### Camadas da Aplicação

#### 1. **Domain Layer** (`Venice.Orders.Domain`)
- **Entidades**: `Pedido` com construtor privado e métodos de domínio
- **Enums**: `PedidoStatus` para estados válidos
- **Events**: `PedidoCriadoEvent` para comunicação entre bounded contexts
- **Interfaces**: Contratos para serviços de domínio

#### 2. **Application Layer** (`Venice.Orders.Application`)
- **Contracts**: DTOs de entrada e saída (`CreatePedidoRequest`, `PedidoResponse`)
- **Services**: Interfaces para casos de uso (`IPedidoEventService`)
- **Events**: Eventos de aplicação

#### 3. **Infrastructure Layer** (`Venice.Orders.Infrastructure`)
- **Persistence**: Implementação Entity Framework para SQL Server
- **Mongo**: Context e configurações MongoDB
- **Kafka**: Implementação de mensageria assíncrona
- **Migrations**: Versionamento de banco de dados

#### 4. **API Layer** (`Venice.Orders.Api`)
- **Controllers**: Endpoints HTTP com autenticação JWT
- **Extensions**: Configurações de DI específicas da API
- **Program.cs**: Configuração da aplicação e pipeline

## 🎨 Design Patterns Utilizados

### **1. Repository Pattern (Implícito via Entity Framework)**
```csharp
public class OrdersDbContext : DbContext
{
    public DbSet<Pedido> Pedidos { get; set; }
}
```

**Justificativa**: Abstrai acesso a dados, permitindo trocar ORM ou banco sem impactar domínio.

### **2. Factory Pattern (via Dependency Injection)**
```csharp
builder.Services.AddDbContext<OrdersDbContext>(opt =>
{
    opt.UseSqlServer(cs, sql =>
    {
        sql.MigrationsAssembly(typeof(OrdersDbContext).Assembly.FullName);
        sql.EnableRetryOnFailure();
    });
});
```

**Justificativa**: Container DI atua como factory, criando instâncias configuradas conforme necessário.

### **3. Publisher/Subscriber Pattern (Event-Driven)**
```csharp
public async Task PublicarPedidoCriadoAsync(PedidoCriadoEvent evento, CancellationToken cancellationToken = default)
{
    await _kafkaProducer.PublishAsync(topico, evento, cancellationToken);
}
```

**Justificativa**: Desacopla criação de pedidos de processamentos assíncronos (notificações, estoque, etc.).

### **4. Builder Pattern (Fluent Configuration)**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c => { /* configuração */ });
var app = builder.Build();
```

**Justificativa**: Configuração fluente e legível da aplicação.

### **5. Extension Methods Pattern**
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        services.AddScoped<IPedidoEventService, KafkaPedidoEventService>();
        return services;
    }
}
```

**Justificativa**: Organiza e modulariza configurações complexas de DI.

## 🐳 Docker Compose - Ambiente Completo

### Arquivo: `docker-compose.yml`

O ambiente é composto por múltiplos serviços integrados:

```yaml
services:
  sqlserver:      # Banco principal para pedidos
  mongodb:        # Banco para itens de pedidos
  redis:          # Cache distribuído
  kafka:          # Message broker
  kafdrop:        # Interface web do Kafka
  redisinsight:   # Interface web do Redis
  api:            # Nossa aplicação (docker-compose.override.yml)
```

### **Health Checks Implementados**

Todos os serviços possuem health checks que garantem disponibilidade:

```yaml
healthcheck:
  test: ["CMD-SHELL", "comando-de-verificacao"]
  interval: 10s
  timeout: 5s
  retries: 10
```

**Justificativa**: Garante que dependências estejam prontas antes da API inicializar.

## 🚀 Como Executar Localmente

### **Pré-requisitos**
- Docker Desktop 4.0+
- .NET 8 SDK (opcional, para desenvolvimento)
- Git

### **1. Execução via Docker Compose (Recomendado)**

```bash
# Clonar o repositório
git clone <repository-url>
cd Venice.Orders

# Navegar para o diretório docker
cd .docker

# Subir toda a infraestrutura + API
docker compose -f docker-compose.yml -f docker-compose.override.yml up -d

# Verificar logs da API
docker logs venice-orders-api -f
```

### **2. Execução para Desenvolvimento**

```bash
# Apenas infraestrutura (sem API)
docker compose up -d

# Executar API localmente
cd ../src/Venice.Orders.Api
dotnet run

# API disponível em: https://localhost:7001 (HTTPS) ou http://localhost:5001 (HTTP)
```

### **3. Verificação de Funcionamento**

```bash
# Health check da API
curl http://localhost:5000/health/ready

# Swagger UI
http://localhost:5000/swagger

# Interfaces de monitoramento
http://localhost:9000     # Kafdrop (Kafka)
http://localhost:8001     # RedisInsight
```

## 🔧 Configurações Técnicas

### **Variáveis de Ambiente**
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ConnectionStrings__SqlServer=Server=sqlserver,1433;Database=VeniceOrdersDB;...
  - ConnectionStrings__Mongo=mongodb://mongodb:27017
  - ConnectionStrings__Redis=redis:6379
  - Kafka__BootstrapServers=kafka:9092
  - Auth__SigningKey=P5xpoJwP2n3c0lBiv83M0IYzF8pQ7kz7JpA1Xrdu2L0=
```

### **Autenticação JWT**
- **Endpoint**: `POST /api/auth/token`
- **Teste**: Usuário `admin` / Senha `123456`
- **Header**: `Authorization: Bearer <token>`

### **Endpoints Principais**
- `GET /health/ready` - Status da aplicação
- `POST /api/auth/token` - Autenticação
- `POST /api/orders` - Criar pedido
- `GET /api/orders/{id}` - Buscar pedido
- `GET /swagger` - Documentação API

## 🏆 Decisões Técnicas

### **1. Polyglot Persistence**
- **SQL Server**: Dados relacionais críticos (pedidos)
- **MongoDB**: Dados semi-estruturados (itens de pedidos)
- **Redis**: Cache de alta performance

**Justificativa**: Cada tecnologia otimizada para seu propósito específico.

### **2. Event-Driven Architecture**
- **Kafka**: Eventos de domínio assíncronos
- **Publisher/Subscriber**: Desacoplamento entre bounded contexts

**Justificativa**: Escalabilidade horizontal e resilência.

### **3. Containerização Completa**
- **Docker Multi-stage**: Build otimizado da aplicação
- **Health Checks**: Garantia de disponibilidade
- **Volumes Named**: Persistência de dados

**Justificativa**: Consistência entre ambientes e facilidade de deploy.

### **4. Security First**
- **JWT Authentication**: Stateless e escalável
- **HTTPS Ready**: Configurado para produção
- **Secrets Management**: Via environment variables

**Justificativa**: Segurança desde o desenvolvimento.

### **5. Observabilidade**
- **Health Checks**: Monitoramento de dependências
- **Structured Logging**: Logs estruturados para análise
- **Monitoring Tools**: Kafdrop, RedisInsight

**Justificativa**: Facilita debugging e monitoramento em produção.

---

## 📚 Tecnologias Utilizadas

- **.NET 8** - Framework principal
- **Entity Framework Core** - ORM para SQL Server
- **MongoDB Driver** - Acesso ao MongoDB
- **StackExchange.Redis** - Cliente Redis
- **Confluent.Kafka** - Cliente Kafka
- **JWT Bearer** - Autenticação
- **Swagger/OpenAPI** - Documentação
- **Docker & Docker Compose** - Containerização