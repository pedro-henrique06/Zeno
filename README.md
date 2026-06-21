# Zeno.API

API de controle financeiro pessoal. Cada usuário tem um único saldo (não há mais o conceito de carteiras): todo lançamento (`Entry`) é vinculado diretamente ao usuário e classificado por tipo (`EntryKind`).

## Stack

- **.NET 10.0** - ASP.NET Core Web API
- **MongoDB** - Banco de dados
- **MongoDB.Driver** - Acesso a dados
- **JWT Bearer** - Autenticação
- **FluentValidation** - Validação
- **BCrypt** - Hash de senhas
- **Swagger** - Documentação da API
- **Docker Compose** - Ambiente local (MongoDB)

---

## Início Rápido

```bash
# Subir o MongoDB
docker-compose up -d

# Restaurar dependências e rodar
dotnet restore
dotnet run --project Zeno
```

A API sobe em `https://localhost:5001` (ou porta configurada). Swagger disponível em `/swagger`.

Para testar endpoints autenticados no Swagger: clique no botão **Authorize** no topo da página, cole o token JWT obtido via `POST /api/auth/login` (apenas o token, sem "Bearer ") e clique em Authorize.

---

## Autenticação

Todos os endpoints exigem token JWT válido no header `Authorization: Bearer {token}`, exceto `login`, `register` e o fluxo OAuth.

O token contém os claims:
- `sub` - UserId (Guid)
- `email` - Email do usuário
- `name` - Nome do usuário
- `jti` - Identificador único do token (usado na blacklist de logout)

### Logout

O logout revoga o token adicionando seu `jti` a uma blacklist em memória (`ITokenBlacklistService`). O token permanece na blacklist até sua expiração natural.

### Fluxo

```
POST /api/auth/register    -> Cria uma nova conta
POST /api/auth/login       -> Retorna o token JWT
POST /api/auth/logout      -> Revoga o token atual
POST /api/auth/refresh-token -> Renova o token a partir de um refresh token
GET  /api/auth/oauth/{provider}           -> Inicia o fluxo OAuth (ex: google)
GET  /api/auth/oauth/{provider}/callback  -> Callback do provedor OAuth
```

---

## Perfil do usuário

```
GET /api/user/me                  -> Retorna os dados do perfil do usuário logado (UserProfileResponse)
PUT /api/user/me                  -> Atualiza nome, email, telefone, documento e data de nascimento
PUT /api/user/me/password         -> Altera a senha (exige a senha atual)
PUT /api/user/me/daily-budget      -> Define o orçamento diário ("Diário previsto") usado nas projeções
```

Usuários autenticados via OAuth (sem senha local) não podem usar a troca de senha enquanto `hasPassword` (retornado em `GET /api/user/me`) for `false`.

Corpo de `PUT /api/user/me/daily-budget`:

```json
{ "dailyBudget": 43.33 }
```

---

## Lançamentos (Entries)

Não existe mais o conceito de carteira: todo lançamento pertence diretamente ao usuário (`Entry.UserId`) e tem um `Kind` que define seu papel no saldo e nos totais (ver [EntryKind](#entrykind)).

```
GET    /api/entry?month={}&year={}&page={}&pageSize={}   -> Lista lançamentos do mês do usuário logado (paginado, padrão page=1, pageSize=50)
POST   /api/entry                                         -> Cria lançamento
PUT    /api/entry                                         -> Atualiza lançamento
DELETE /api/entry/{id}                                     -> Remove lançamento
```

Corpo de `POST /api/entry` / `PUT /api/entry`:

```json
{
  "title": "Salário",
  "value": 5000.00,
  "kind": 0,
  "description": "Salário do mês",
  "tagId": null,
  "date": "2026-06-05"
}
```

---

## Tags

Tags são livres (criadas pelo usuário) e usadas para categorizar lançamentos. Substituem o antigo enum fixo `Category`.

```
GET    /api/tags          -> Lista tags do usuário logado
POST   /api/tags          -> Cria tag
PUT    /api/tags          -> Atualiza tag
DELETE /api/tags/{id}      -> Remove tag
```

---

## Saldos (tela "Saldos")

Retorna o saldo dia a dia do mês: dias passados/hoje usam os lançamentos reais; dias futuros são projetados subtraindo o orçamento diário (`DailyBudget`) do usuário a cada dia.

```
GET /api/balances?month={}&year={}
```

Resposta (`BalancesResponse`):

```json
{
  "month": 6,
  "year": 2026,
  "days": [
    {
      "day": 1,
      "entrada": 5000.00,
      "saida": 0,
      "diario": 43.33,
      "economia": 0,
      "cartao": 0,
      "balance": 4956.67,
      "isProjected": false,
      "isToday": false
    }
  ]
}
```

- `isToday` marca o dia atual; dias `<= hoje` usam valores reais lançados.
- `isProjected = true` marca dias futuros: `diario` é preenchido com o `DailyBudget` do usuário e o saldo desconta esse valor projetado dia a dia.
- O saldo (`balance`) é cumulativo, começando do saldo acumulado de todos os lançamentos anteriores ao mês consultado.

---

## Totais (tela "Totais")

Cálculos consolidados do mês e total de movimentações por tipo.

```
GET /api/summary?month={}&year={}
```

Resposta (`SummaryResponse`):

```json
{
  "performance": 3500.00,
  "economizedPercent": 10.0,
  "costOfLiving": 1500.00,
  "dailyAverageReal": 35.20,
  "dailyBudget": 43.33,
  "daysElapsed": 20,
  "daysRemaining": 10,
  "daysInMonth": 30,
  "movements": {
    "entrada": 5000.00,
    "saida": 600.00,
    "diario": 704.00,
    "economia": 500.00,
    "cartao": 196.00
  }
}
```

Cálculos:
- `performance` = Entrada − Saída − Diário − Cartão − (orçamento diário projetado para os dias restantes do mês). `Economia` não entra no cálculo de performance (é dinheiro guardado, não gasto).
- `economizedPercent` = Economia / Entrada × 100.
- `costOfLiving` = Saída + Diário + Cartão + (orçamento diário projetado para os dias restantes).
- `dailyAverageReal` = total gasto em `Diário` dividido pelos dias já decorridos no mês.
- `movements` = soma de cada `EntryKind` no mês, usada na lista "Movimentações do mês".

---

## Enums

### EntryKind

Classificação de uso de cada lançamento, escolhida pelo usuário na criação.

| Valor | Nome | Descrição |
|-------|------|-----------|
| 0 | Entrada | Receita (salário, renda extra, etc.) — soma no saldo |
| 1 | Saida | Despesa fixa/comum — subtrai do saldo |
| 2 | Diario | Gasto do dia a dia (alimentação, transporte etc.) — subtrai do saldo |
| 3 | Economia | Valor guardado/investido — subtrai do saldo, mas é **excluído** do cálculo de "Performance" |
| 4 | Cartao | Despesa no cartão de crédito — subtrai do saldo |

---

## Modelo de Dados (MongoDB)

| Coleção | Campos principais |
|---------|--------------------|
| **users** | Id, Name, Email, PasswordHash, Phone, Document, BirthDate, Provider, ProviderId, DailyBudget, EmailVerified, CreatedAt, UpdatedAt |
| **entries** | Id, UserId, Title, Value, Kind, Description, TagId, Date |
| **tags** | Id, UserId, Name, CreatedAt |
| **refreshTokens** | Id, UserId, Token, ExpiresAt, CreatedAt |

Todas as coleções filtram por `UserId` — não há entidades compartilhadas entre usuários.

---

## Regras de Negócio

### Isolamento de dados

- Cada usuário só enxerga seus próprios dados (lançamentos, tags, perfil).
- A API extrai o `UserId` do token JWT e filtra todas as consultas.

### Saldo

- Não há mais saldo armazenado em uma entidade própria: o saldo é sempre derivado em tempo real a partir da soma de todos os lançamentos do usuário (`Entrada` soma, os demais tipos subtraem).
- A projeção de dias futuros (tela "Saldos") usa o `DailyBudget` do usuário como gasto diário estimado.

### Token e segurança

- Senhas armazenadas com hash BCrypt.
- Tokens JWT com expiração configurável (`appsettings` → `Jwt:ExpiresInHours`).
- Logout invalida o token via blacklist em memória até sua expiração natural.
- Todos os endpoints exigem autenticação por padrão (FallbackPolicy), exceto login, register e OAuth.

---

## Configuração

### appsettings.json (sem valores reais)

```json
{
  "Database": {
    "ConnectionString": "mongodb://localhost:27017/zeno_db"
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

```bash
dotnet user-secrets init --project Zeno/Zeno.csproj
dotnet user-secrets set "Jwt:Key" "sua-chave-forte" --project Zeno/Zeno.csproj
dotnet user-secrets set "Database:ConnectionString" "mongodb://root:rootpassword@localhost:27017/zeno_db?authSource=admin" --project Zeno/Zeno.csproj
dotnet user-secrets set "Encryption:Key" "sua-chave-de-criptografia" --project Zeno/Zeno.csproj
```

### Docker Compose

```bash
docker-compose up -d    # Sobe o MongoDB
docker-compose down     # Para os containers
```

---

## Arquitetura

O projeto está dividido em camadas:

- **Zeno** (API): Controllers, Program.cs e configuração do host.
- **Zeno.Application**: Services, Validators, Requests, Responses e Interfaces.
- **Zeno.Domain**: entidades, enums e interfaces de repositório.
- **Zeno.Infrastructure.SQL**: acesso a dados via MongoDB.Driver (repositórios, contexto e DI).

### Fluxo de uma requisição

```
Controller → Service → Validator → Repository → MongoDB
```

1. O Controller recebe a requisição e valida o DTO.
2. O Service aplica as regras de negócio.
3. O Validator valida os dados de entrada (FluentValidation).
4. O Repository persiste/consulta os dados no MongoDB.

### Estrutura do Projeto

```
Zeno.API/
  Zeno/                          # Host (Controllers, Program.cs)
  Zeno.Domain/                   # Entidades, Enums, Interfaces de repositorio
  Zeno.Application/              # Services, Validators, Requests, Responses
  Zeno.Infrastructure.SQL/       # Repositorios (MongoDB.Driver), Context, DI
  docker-compose.yml             # MongoDB local
```
