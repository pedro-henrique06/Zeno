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

### 1.1. Editar perfil e senha

```
GET /api/user/me            -> Retorna os dados do perfil do usuario logado
PUT /api/user/me            -> Atualiza nome, email, telefone, documento e data de nascimento
PUT /api/user/me/password   -> Altera a senha (exige a senha atual)
```

Exemplo de corpo para `PUT /api/user/me`:

```json
{
  "name": "Pedro Henrique",
  "email": "pedro@example.com",
  "phone": "11999999999",
  "document": "12345678900",
  "birthDate": "1995-05-20"
}
```

Exemplo de corpo para `PUT /api/user/me/password`:

```json
{
  "currentPassword": "senhaAtual123",
  "newPassword": "novaSenha456",
  "confirmNewPassword": "novaSenha456"
}
```

Usuarios autenticados via OAuth (sem senha local) nao podem usar a troca de senha enquanto `hasPassword` (retornado em `GET /api/user/me`) for `false`.

### 2. Cadastrar lancamentos recorrentes (salario, contas fixas, etc.)

Um lancamento recorrente representa receita ou despesa fixa (salario, aluguel, assinatura...), vinculado diretamente a uma carteira. Define-se o `type` (Credit/Debit), o dia do mes em que deve ser lancado e o `kind` (veja [EntryKind](#entrykind)). Um background service verifica a cada hora se ha lancamentos recorrentes pendentes para o dia atual e os credita/debita automaticamente na carteira vinculada, criando o `Entry` correspondente.

```
POST   /api/recurring-entries              -> Cria um lancamento recorrente vinculado a uma carteira
GET    /api/recurring-entries              -> Lista lancamentos recorrentes do usuario autenticado
GET    /api/recurring-entries/wallet/{id}  -> Lista lancamentos recorrentes de uma carteira
GET    /api/recurring-entries/{id}         -> Busca lancamento recorrente por ID
PUT    /api/recurring-entries              -> Atualiza lancamento recorrente
DELETE /api/recurring-entries/{id}         -> Remove lancamento recorrente
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

Lancamentos (entries) sao creditos ou debitos que atualizam automaticamente o saldo da carteira. Cada lancamento tem um `type` (Credit/Debit, efeito no saldo) e um `kind` (categoria de uso, ver [EntryKind](#entrykind)), escolhido pelo usuario na criacao.

```
GET    /api/entry?month={}&year={}&walletId={}&page={}&pageSize={}   -> Lista lancamentos por mes/ano de uma carteira (paginado)
GET    /api/entry?month={}&year={}&page={}&pageSize={}                -> Lista lancamentos do mes em TODAS as carteiras do usuario (sem walletId)
POST   /api/entry                                                      -> Cria lancamento
PUT    /api/entry                                                      -> Atualiza lancamento
DELETE /api/entry/{id}                                                 -> Remove lancamento
```

- `walletId` e opcional: quando omitido, o endpoint retorna os lancamentos agregados de todas as carteiras do usuario logado naquele mes/ano (usado pela tela "Totais").
- `page`/`pageSize` (padrao `1`/`50`, maximo `200`) paginam o resultado agregado.

Ao criar: o saldo da carteira e atualizado (+credito / -debito).
Ao atualizar: o efeito do lancamento antigo e revertido e o novo e aplicado.
Ao remover: o efeito do lancamento e revertido.

### 4.1. Saldo diario, projecao e orcamento da carteira

Endpoints para o "Horizonte de saldos": saldo real dia a dia, media diaria de curto prazo, projecao de longo prazo e orcamento diario.

```
GET  /api/wallet/balances?month={}&year={}                 -> Saldo real de fim de dia, dia a dia, agregando TODAS as carteiras do usuario
GET  /api/wallet/{id}/balances?month={}&year={}             -> Saldo real de fim de dia, dia a dia, de uma carteira
GET  /api/wallet/{id}/daily-average?months={3}              -> Media diaria de receita/despesa dos ultimos N meses (curto prazo)
GET  /api/wallet/{id}/forecast?months={3}                    -> Serie diaria projetada de saldo para os proximos N meses (longo prazo)
GET  /api/wallet/{id}/card-invoice?month={}&year={}          -> Total lancado como `Cartao` (fatura) no mes
GET  /api/wallet/{id}/daily-forecast?month={}&year={}        -> Gasto diario recomendado para o restante do mes, com base no orcamento
PUT  /api/wallet/{id}/budget                                 -> Define o orcamento diario da carteira (gasto `Diario`)
```

Corpo de `PUT /api/wallet/{id}/budget`:

```json
{ "dailyBudget": 50.00 }
```

`balances` e calculado em cima do historico real de lancamentos (saldo acumulado dia a dia), nao e uma estimativa do front. `forecast` combina a media historica de receitas/despesas (excluindo o que ja vem dos lancamentos recorrentes ativos, para nao contar em dobro) com os lancamentos recorrentes reais previstos em seus dias do mes.

### 4.2. Projecao financeira (simulacao de gasto extra)

Simula o impacto de um gasto extra hipotetico no saldo da carteira nos proximos meses, usando a media de receitas e despesas dos ultimos 3 meses como base.

```
POST   /api/projection/simulate    -> Simula projecao de saldo
```

Corpo da requisicao:

```json
{
  "walletId": "guid-da-carteira",
  "extraExpenseAmount": 300.00,
  "isRecurring": true,
  "monthsToProject": 6
}
```

- `isRecurring = true`: o gasto extra se repete todo mes na projecao.
- `isRecurring = false`: o gasto extra e aplicado uma unica vez, no primeiro mes projetado.

A resposta traz o saldo projetado mes a mes, alerta se o saldo ficar negativo e se as despesas projetadas ultrapassam o limite de 50% da regra 50/30/20.

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
  |      |--< Accounts (WalletId FK)
  |      |--< RecurrentEntries (WalletId FK)
  |      |--< HomeWallets (WalletId FK, composite PK)
  |
  |--< RecurrentEntries (UserId FK)
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
| **Users** | Id, Name, Email, PasswordHash, Phone, Document, BirthDate, Provider, ProviderId, CreatedAt, UpdatedAt, EmailVerified | Id | - |
| **Wallets** | Id, Name, Description, Balance, UserId, Currency, DailyBudget, CreatedAt | Id | UserId → Users |
| **Accounts** | Id, Name, Bank, Type, Balance, WalletId, CreatedAt | Id | WalletId → Wallets |
| **Entries** | Id, Title, Value, Type, Kind, Description, Category, Date, WalletId | Id | WalletId → Wallets |
| **RecurrentEntries** | Id, UserId, WalletId, Title, Value, Type, Kind, Category, DayOfMonth, Active, CreatedAt, LastProcessedAt | Id | UserId → Users, WalletId → Wallets |
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

### EntryKind

Classificacao de uso escolhida pelo usuario na criacao do lancamento, independente do `Type` (Credit/Debit). Usada pelo front-end para separar saldo, performance e fatura do cartao sem depender de heuristicas no cliente.

| Valor | Nome | Descricao |
|-------|------|-----------|
| 0 | Entrada | Receita (salario, renda extra, etc.) |
| 1 | Saida | Despesa comum |
| 2 | Diario | Gasto do dia a dia (ex: alimentacao, transporte) |
| 3 | Economia | Valor guardado/investido. Conta no saldo da carteira, mas deve ser **excluido** das somas de "Performance" do front-end |
| 4 | Cartao | Despesa no cartao de credito. Somada na fatura mensal (`GET /api/wallet/{id}/card-invoice`) |

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

- Cada usuario so enxerga seus proprios dados (carteiras, lancamentos, lancamentos recorrentes).
- A API extrai o `UserId` do token JWT e filtra todas as consultas.
- Lancamentos so podem ser criados/editados/removidos em carteiras do proprio usuario.
- Lancamentos recorrentes so podem ser gerenciados em carteiras do proprio usuario.

### Casas (Homes)

- Quem cria a casa e automaticamente **Admin**.
- So o **Admin** pode: atualizar dados da casa, excluir a casa, convidar membros, remover membros, desvincular carteiras, remover despesas.
- Qualquer **membro** pode: visualizar dados da casa, criar despesas, vincular sua carteira.
- Um usuario so pode acessar casas das quais e membro.

### Lancamentos recorrentes

- Generalizam o antigo modelo de "salario" e o de "despesas recorrentes" em uma unica entidade: cada `RecurrentEntry` e vinculada a uma carteira (`WalletId`) e tem um `Type` (Credit/Debit) e um `Kind` ([EntryKind](#entrykind)), podendo representar tanto receita (`Entrada`) quanto qualquer tipo de despesa fixa (aluguel, assinatura, etc.).
- O campo `DayOfMonth` define o dia do mes em que o lancamento deve ser processado.
- Um `BackgroundService` roda a cada hora e verifica se ha lancamentos recorrentes pendentes para o dia atual, considerando tanto receitas quanto despesas.
- Ao processar, um `Entry` correspondente e criado com o mesmo `Type`/`Kind`/`Category`, o saldo da carteira e atualizado e o campo `LastProcessedAt` e atualizado.
- Um lancamento recorrente so e processado uma unica vez por mes.

### Saldo da carteira

- O saldo e mantido automaticamente pela API:
  - Criar lancamento de credito: saldo aumenta.
  - Criar lancamento de debito: saldo diminui.
  - Atualizar lancamento: efeito antigo e revertido, novo efeito aplicado.
  - Remover lancamento: efeito e revertido.
  - Processamento de lancamento recorrente: saldo e atualizado conforme o `Kind` (credita se `Entrada`, debita nos demais casos).
- O saldo diario historico (`GET /api/wallet/{id}/balances`) e calculado em tempo real a partir de todos os lancamentos da carteira (soma cumulativa por dia), sem depender de um snapshot salvo.

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
