# Zeno.API

API de gestao financeira pessoal com suporte a casas compartilhadas e regra de economia 50/30/20.

## Stack

- **.NET 10.0** - ASP.NET Core Web API
- **PostgreSQL 16** - Banco de dados
- **Dapper** - Micro-ORM
- **JWT Bearer** - Autenticação
- **FluentValidation** - Validação
- **BCrypt** - Hash de senhas
- **Swagger** - Documentação da API
- **Docker Compose** - Ambiente local

---

## Inicio Rapido

```bash
# Subir o SQL Server
docker-compose up -d

# Restaurar dependencias e rodar
dotnet restore
dotnet run --project Zeno
```

A API sobe em `https://localhost:5001` (ou porta configurada). Swagger disponivel em `/swagger`.

Para testar endpoints autenticados no Swagger: clique no botao **Authorize** no topo da pagina, cole o token JWT obtido via `POST /api/auth/login` (apenas o token, sem "Bearer ") e clique em Authorize.

---

## Autenticacao

Todos os endpoints exigem token JWT valido no header `Authorization: Bearer {token}`, exceto `login` e `register`.

O token contem os claims:
- `sub` - UserId (Guid)
- `email` - Email do usuario
- `name` - Nome do usuario
- `jti` - Identificador unico do token (usado na blacklist de logout)

### Logout

O logout revoga o token adicionando seu `jti` a uma blacklist em memoria (IMemoryCache). O token permanece na blacklist ate sua expiracao natural.

---

## Fluxo do Usuario

### 1. Criar conta ou fazer login

```
POST /api/auth/register    -> Cria uma nova conta
POST /api/auth/login       -> Retorna o token JWT
POST /api/auth/logout      -> Revoga o token atual
```

### 2. Cadastrar salario

O salario e recorrente. Define-se o dia do mes em que deve ser creditado. Um background service verifica a cada hora se ha salarios pendentes e os credit automaticamente na carteira vinculada.

```
POST   /api/salary                -> Cria um salario vinculado a uma carteira
GET    /api/salary/wallet/{id}    -> Lista salarios de uma carteira
GET    /api/salary/{id}           -> Busca salario por ID
PUT    /api/salary                -> Atualiza salario
DELETE /api/salary/{id}           -> Remove salario
```

### 3. Criar uma carteira

Cada carteira pertence a um usuario. Nenhum outro usuario tem acesso.

```
GET    /api/wallet           -> Lista carteiras do usuario logado
GET    /api/wallet/{id}      -> Busca carteira por ID
POST   /api/wallet           -> Cria uma nova carteira
PUT    /api/wallet           -> Atualiza nome e descricao
DELETE /api/wallet/{id}      -> Remove carteira
```

### 4. Gerar lancamentos na carteira

Lancamentos (entries) sao creditos ou debitos que atualizam automaticamente o saldo da carteira.

```
GET    /api/entry?month={}&year={}&walletId={}   -> Lista lancamentos por mes/ano/carteira
POST   /api/entry                                   -> Cria lancamento
PUT    /api/entry                                   -> Atualiza lancamento
DELETE /api/entry/{id}                              -> Remove lancamento
```

Ao criar: o saldo da carteira e atualizado (+credito / -debito).
Ao atualizar: o efeito do lancamento antigo e revertido e o novo e aplicado.
Ao remover: o efeito do lancamento e revertido.

### 5. Criar uma casa e convidar membros

Uma casa (Home) e um espaco compartilhado onde multiplos usuarios gerenciam financas juntos. Quem cria a casa e automaticamente o **Admin**.

```
POST   /api/home                           -> Cria a casa (criador vira Admin)
GET    /api/home                           -> Lista casas do usuario
GET    /api/home/{id}                      -> Detalhes da casa
PUT    /api/home/{id}                      -> Atualiza casa (so Admin)
DELETE /api/home/{id}                      -> Remove casa (so Admin)
```

#### Convite de membros

O Admin pode convidar qualquer usuario ja cadastrado no sistema (pelo UserId).

```
POST   /api/home/{homeId}/members/{memberUserId}    -> Adiciona membro (so Admin)
DELETE /api/home/{homeId}/members/{memberUserId}     -> Remove membro (so Admin)
GET    /api/home/{homeId}/members                    -> Lista membros da casa
```

Regras:
- Apenas o **Admin** pode convidar ou remover membros.
- O Admin nao pode remover a si mesmo.
- Um usuario so pode ser membro de uma casa uma unica vez.
- Todos os membros podem visualizar dados da casa.

#### Vincular carteiras a casa

Membros vinculam suas carteiras a casa para que as despesas sejam divididas entre eles.

```
POST   /api/home/{homeId}/wallets/{walletId}     -> Vincula carteira a casa
DELETE /api/home/{homeId}/wallets/{walletId}      -> Desvincula carteira (so Admin)
```

### 6. Despesas da casa e regra 50/30/20

#### Despesas compartilhadas

```
POST   /api/home/{homeId}/expenses                 -> Cria despesa na casa
GET    /api/home/{homeId}/expenses?month={}&year={} -> Lista despesas por mes
DELETE /api/home/expenses/{expenseId}               -> Remove despesa (so Admin)
```

#### Divisao de despesas (Split)

```
GET    /api/home/{homeId}/split?month={}&year={}    -> Calcula divisao
```

#### Modos de divisao (SplitMode)

A casa tem um campo `SplitMode` que define como as despesas sao divididas:

| SplitMode | Valor | Descricao |
|-----------|-------|-----------|
| `ByTotalBalance` | 0 | Divisao proporcional a renda total de cada membro no mes |
| `ByIndividualAccounts` | 1 | Divisao proporcional ao salario de cada membro (peso salarial) |

#### SplitMode = 0 (ByTotalBalance) - Divisao por saldo total

A divisao e proporcional a renda (creditos) de cada carteira no mes. Calculo:

```
PercentualMembro = RendaDaCarteiraNoMes / RendaTotalDeTodasAsCarteiras
ValorAPagar = DespesasTotais * PercentualMembro
```

#### SplitMode = 1 (ByIndividualAccounts) - Divisao por contas individuais

A divisao usa o **salario cadastrado** de cada membro como peso. Isso garante que ninguem pague alem do que pode, proporcionalmente.

``:
PesoMembro = SalarioDoMembro / SalarioTotalDeTodosOsMembros
ValorAPagar = DespesasTotais * PesoMembro
```

Exemplo:
- Membro A: salario R$ 5.000
- Membro B: salario R$ 3.000
- Despesa total: R$ 2.000

Peso A = 5000 / 8000 = 62,5% → paga R$ 1.250
Peso B = 3000 / 8000 = 37,5% → paga R$ 750

#### Resposta do endpoint de split

```json
[
  {
    "walletId": "guid",
    "userId": "guid",
    "userName": "Joao",
    "walletName": "Carteira Principal",
    "walletIncome": 5000.00,
    "salaryAmount": 5000.00,
    "salaryWeight": 62.50,
    "totalIncome": 8000.00,
    "totalSalary": 8000.00,
    "percentage": 62.50,
    "amountToPay": 1250.00
  }
]
```

#### Alerta de orcamento 50/30/20

O endpoint de budget-alert verifica se as despesas da casa ultrapassam o limite de 50% da renda total destinado a necessidades pela regra 50/30/20.

```
GET /api/home/{homeId}/budget-alert?month={}&year={}
```

Regra 50/30/20 aplicada:
- **50%** da renda → Necessidades (limite maximo para despesas da casa)
- **30%** da renda → Desejos
- **20%** da renda → Investimentos/Poupanca

Resposta de exemplo:

```json
{
  "homeId": "guid",
  "month": 4,
  "year": 2026,
  "totalIncome": 8000.00,
  "totalExpenses": 4500.00,
  "maxNeedsLimit": 4000.00,
  "needsUsagePercentage": 56.25,
  "wantsLimit": 2400.00,
  "savingsLimit": 1600.00,
  "isOverBudget": true,
  "alertMessage": "ATENÇÃO: As despesas da casa (56,3% da renda) ultrapassaram o limite de 50% estabelecido pela regra 50/30/20. Limite: R$ 4000,00, Gasto: R$ 4500,00."
}
```

Campos:
- `maxNeedsLimit` — 50% da renda total dos membros no mes
- `wantsLimit` — 30% da renda total
- `savingsLimit` — 20% da renda total
- `isOverBudget` — `true` quando despesas > 50% da renda
- `alertMessage` — mensagem descritiva do status

---

## Modelo de Dados

### Diagrama de Entidades

```
Users
  |
  |--< Wallets (UserId FK)
  |      |
  |      |--< Entries (WalletId FK)
  |      |--< Salaries (WalletId FK)
  |      |--< HomeWallets (WalletId FK, composite PK)
  |
  |--< HomeMembers (UserId FK, composite PK)
         |
         Homes (HomeId FK from HomeMembers)
           |
           |--< HomeExpenses (HomeId FK)
           |--< HomeWallets (HomeId FK, composite PK)
           |--< HomeMembers (HomeId FK, composite PK)
```

### Tabelas

| Tabela | Campos | PK | FKs |
|--------|--------|----|-----|
| **Users** | Id, Name, Email, PasswordHash, CreatedAt | Id | - |
| **Wallets** | Id, Name, Description, Balance, UserId, CreatedAt | Id | UserId → Users |
| **Entries** | Id, Title, Value, Type, Description, Category, Date, WalletId | Id | WalletId → Wallets |
| **Salaries** | Id, WalletId, Amount, Description, DayOfMonth, Active, CreatedAt, LastProcessedAt | Id | WalletId → Wallets |
| **Homes** | Id, Name, Description, SplitMode, CreatedAt | Id | - |
| **HomeMembers** | HomeId, UserId, Role, JoinedAt | (HomeId, UserId) | HomeId → Homes, UserId → Users |
| **HomeWallets** | HomeId, WalletId | (HomeId, WalletId) | HomeId → Homes, WalletId → Wallets |
| **HomeExpenses** | Id, HomeId, Title, Value, Category, Month, Year, CreatedAt | Id | HomeId → Homes |

---

## Enums

### EntryType

| Valor | Nome | Descricao |
|-------|------|-----------|
| 0 | Credit | Entrada (receita) |
| 1 | Debit | Saida (despesa) |

### Category

| Valor | Nome |
|-------|------|
| 0 | None |
| 1 | Restaurant |
| 2 | Grocery |
| 3 | Entertainment |
| 4 | Utilities |
| 5 | Transportation |

### SplitMode

| Valor | Nome | Descricao |
|-------|------|-----------|
| 0 | ByTotalBalance | Divisao proporcional a renda do mes |
| 1 | ByIndividualAccounts | Divisao proporcional ao salario (peso) |

### HomeRole

| Valor | Nome | Descricao |
|-------|------|-----------|
| 0 | Admin | Administrador da casa (pode convidar, remover, alterar config) |
| 1 | Member | Membro comum (pode visualizar, criar despesas, vincular carteira) |

---

## Regras de Negocio

### Isolamento de dados

- Cada usuario so enxerga seus proprios dados (carteiras, lancamentos, salarios).
- A API extrai o `UserId` do token JWT e filtra todas as consultas.
- Lancamentos so podem ser criados/editados/removidos em carteiras do proprio usuario.
- Salarios so podem ser gerenciados em carteiras do proprio usuario.

### Casas (Homes)

- Quem cria a casa e automaticamente **Admin**.
- So o **Admin** pode: atualizar dados da casa, excluir a casa, convidar membros, remover membros, desvincular carteiras, remover despesas.
- Qualquer **membro** pode: visualizar dados da casa, criar despesas, vincular sua carteira.
- Um usuario so pode acessar casas das quais e membro.

### Salario recorrente

- O campo `DayOfMonth` define o dia do mes que o salario deve ser creditado.
- Um `BackgroundService` roda a cada hora e verifica se ha salarios pendentes para o dia atual.
- Ao processar, o saldo da carteira e incrementado e o campo `LastProcessedAt` e atualizado.
- Um salario so e processado uma unica vez por mes.

### Saldo da carteira

- O saldo e mantido automaticamente pela API:
  - Criar lancamento de credito: saldo aumenta.
  - Criar lancamento de debito: saldo diminui.
  - Atualizar lancamento: efeito antigo e revertido, novo efeito aplicado.
  - Remover lancamento: efeito e revertido.
  - Processamento de salario: saldo aumenta.

### Alerta de orcamento 50/30/20

- O endpoint `GET /api/home/{homeId}/budget-alert` calcula em tempo real se as despesas da casa estao dentro do orcamento.
- A renda total considerada e a soma dos creditos de todas as carteiras vinculadas a casa no mes/ano consultado.
- O limite maximo para despesas da casa e **50% da renda total** (necessidades).
- Se as despesas ultrapassarem 50%, o campo `isOverBudget` retorna `true` e o `alertMessage` avisa o usuario.
- Os outros 30% (desejos) e 20% (poupanca) sao informativos para o membro planejar suas financas pessoais.
- A validacao **nao bloqueia** a criacao de despesas, apenas alerta.

### Token e seguranca

- Senhas armazenadas com hash BCrypt.
- Tokens JWT com expiracao configuravel (appsettings → Jwt:ExpiresInHours).
- Logout invalida o token via blacklist em memoria ate sua expiracao natural.
- Todos os endpoints exigem autenticacao por padrao (FallbackPolicy), exceto login e register.

---

## Configuracao

### appsettings.json (sem valores reais)

```json
{
  "Database": {
    "ConnectionString": ""
  },
  "Jwt": {
    "Key": "",
    "Issuer": "Zeno.API",
    "Audience": "Zeno.Client",
    "ExpiresInHours": "8"
  },
  "OAuth": {
    "Google": {
      "ClientId": "",
      "ClientSecret": ""
    }
  },
  "Encryption": {
    "Key": ""
  }
}
```

> Use variáveis de ambiente ou User Secrets para fornecer os valores reais.

### Docker Compose

```bash
docker-compose up -d    # Sobe PostgreSQL
docker-compose down     # Para os containers
```

O script `docker/init.sql` cria o banco e todas as tabelas automaticamente no primeiro startup. As migrações são idempotentes (IF NOT EXISTS).

---

## Arquitetura

O projeto está dividido em camadas:

- **Zeno** (API): camada de Controllers, Middlewares, Filters e configuração do host.
- **Zeno.Application**: regras de aplicação, Services, Validators, Requests, Responses e Interfaces.
- **Zeno.Domain**: entidades, enums, value objects e regras de domínio.
- **Zeno.Infrastructure.SQL**: acesso a dados usando Dapper e PostgreSQL, contextos e configurações.

### Fluxo de uma requisição

```
Controller → Service → Validator → Repository → Database
```

1. O Controller recebe a requisição e valida o DTO.
2. O Service aplica as regras de negócio.
3. O Validator valida os dados de entrada.
4. O Repository persiste os dados no banco.

---

## Configuração

### Variáveis de Ambiente (ou User Secrets)

```bash
dotnet user-secrets init --project Zeno/Zeno.csproj
dotnet user-secrets set "Jwt:Key" "sua-chave-forte" --project Zeno/Zeno.csproj
dotnet user-secrets set "Database:ConnectionString" "Host=localhost;Port=5432;Database=ZenoDb;Username=postgres;Password=postgres" --project Zeno/Zeno.csproj
dotnet user-secrets set "Encryption:Key" "sua-chave-de-criptografia" --project Zeno/Zeno.csproj
```

### appsettings.json (sem valores reais)

```json
{
  "Database": {
    "ConnectionString": ""
  },
  "Jwt": {
    "Key": "",
    "Issuer": "Zeno.API",
    "Audience": "Zeno.Client",
    "ExpiresInHours": "8"
  },
  "OAuth": {
    "Google": {
      "ClientId": "",
      "ClientSecret": ""
    }
  },
  "Encryption": {
    "Key": ""
  }
}
```

> Use variáveis de ambiente ou User Secrets para fornecer os valores reais.

---

## Estrutura do Projeto

```
Zeno.API/
  Zeno/                          # Host (Controllers, Program.cs)
  Zeno.Domain/                   # Entidades, Enums, Interfaces de repositorio
  Zeno.Application/              # Services, Validators, Requests, Responses
  Zeno.Infrastructure.SQL/       # Repositorios (Dapper), Context, DI
  docker/                        # init.sql, entrypoint.sh
  docker-compose.yml
```
