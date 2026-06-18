# Zeno - Roadmap de Melhorias e Novas Funcionalidades

Este documento organiza as melhorias do projeto **Zeno** em tasks práticas, separadas por prioridade.

O objetivo é evoluir o Zeno de uma API de controle financeiro para um produto mais completo, seguro, confiável e útil para o usuário final.

---

# 1. Segurança e Configuração

## TASK 1.1 - Remover secrets do appsettings.json

### Problema

Atualmente existem informações sensíveis no `appsettings.json`, como:

- JWT Key
- Google ClientId
- Google ClientSecret
- Connection string

Essas informações não devem ficar versionadas no GitHub.

### Ação

Remover os valores reais do arquivo `appsettings.json`.

### Exemplo esperado

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
  }
}
```

### Critério de aceite

- [ ] Nenhum segredo real deve ficar no GitHub.
- [ ] O projeto deve continuar funcionando usando variáveis de ambiente ou User Secrets.
- [ ] Os secrets antigos devem ser rotacionados no provedor original.
- [ ] O histórico do Git deve ser revisado se algum secret real ficou público.

---

## TASK 1.2 - Configurar User Secrets para ambiente local

### Ação

Executar os comandos abaixo no projeto da API:

```bash
dotnet user-secrets init --project Zeno/Zeno.csproj

dotnet user-secrets set "Jwt:Key" "sua-chave-forte" --project Zeno/Zeno.csproj
dotnet user-secrets set "OAuth:Google:ClientId" "seu-client-id" --project Zeno/Zeno.csproj
dotnet user-secrets set "OAuth:Google:ClientSecret" "seu-client-secret" --project Zeno/Zeno.csproj
dotnet user-secrets set "Database:ConnectionString" "sua-connection-string" --project Zeno/Zeno.csproj
```

### Critério de aceite

- [ ] A aplicação deve subir localmente sem secrets no `appsettings.json`.
- [ ] As configurações devem ser lidas corretamente via `IConfiguration`.
- [ ] O README deve explicar como configurar os secrets localmente.

---

## TASK 1.3 - Remover logs sensíveis do Program.cs

### Problema

Evitar imprimir connection string ou outros dados sensíveis no console.

### Ação

Remover qualquer linha semelhante a:

```csharp
Console.WriteLine(connectionString);
```

### Critério de aceite

- [ ] Nenhuma connection string deve aparecer no log.
- [ ] Nenhum secret deve ser exibido no console.
- [ ] Logs devem mostrar apenas mensagens seguras.

---

# 2. Documentação

## TASK 2.1 - Corrigir README.md

### Problema

O README menciona SQL Server, mas o projeto está usando PostgreSQL no Docker Compose.

### Ação

Atualizar a stack do README.

### Stack correta sugerida

```md
## Stack

- .NET 10 - ASP.NET Core Web API
- PostgreSQL 16 - Banco de dados
- Dapper - Micro ORM
- JWT Bearer - Autenticação
- FluentValidation - Validação
- BCrypt - Hash de senhas
- Swagger - Documentação da API
- Docker Compose - Ambiente local
```

### Critério de aceite

- [ ] README deve refletir a stack real do projeto.
- [ ] README deve explicar como subir o banco.
- [ ] README deve explicar como rodar a API.
- [ ] README deve ter exemplos básicos de endpoints.
- [ ] README deve explicar as variáveis de ambiente necessárias.

---

## TASK 2.2 - Adicionar seção de arquitetura no README

### Ação

Documentar a separação das camadas.

### Exemplo

```md
## Arquitetura

O projeto está dividido em camadas:

- Zeno: camada de API, controllers, middlewares e configuração.
- Zeno.Application: regras de aplicação, services, validators e contratos.
- Zeno.Domain: entidades, enums, value objects e regras de domínio.
- Zeno.Infrastructure.SQL: acesso a dados usando Dapper e PostgreSQL.
```

### Critério de aceite

- [ ] README deve explicar a responsabilidade de cada projeto.
- [ ] README deve deixar claro onde colocar novas features.
- [ ] README deve explicar o fluxo básico Controller -> Service -> Repository -> Database.

---

# 3. Organização da API

## TASK 3.1 - Criar DTOs para requests

### Problema

Os controllers não devem receber entidades de domínio diretamente.

### Ação

Criar DTOs específicos para entrada de dados.

### Estrutura sugerida

```text
Zeno.Application
  Requests
    Entries
      CreateEntryRequest.cs
      UpdateEntryRequest.cs
    Wallets
      CreateWalletRequest.cs
      UpdateWalletRequest.cs
    Homes
      CreateHomeRequest.cs
      CreateHomeExpenseRequest.cs
```

### Exemplo

```csharp
public sealed class CreateEntryRequest
{
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public EntryType Type { get; set; }
    public string? Description { get; set; }
    public Guid WalletId { get; set; }
    public DateOnly Date { get; set; }
}
```

### Critério de aceite

- [ ] Controllers devem receber DTOs, não entidades.
- [ ] Entidades de domínio não devem ser expostas diretamente no contrato da API.
- [ ] Campos internos como `Id`, `UserId`, `CreatedAt` e `UpdatedAt` não devem ser enviados pelo client.
- [ ] Validadores devem validar os DTOs de entrada ou comandos de aplicação.

---

## TASK 3.2 - Criar DTOs para responses

### Ação

Criar objetos específicos de retorno.

### Estrutura sugerida

```text
Zeno.Application
  Responses
    Entries
      EntryResponse.cs
      MonthlySummaryResponse.cs
    Wallets
      WalletResponse.cs
```

### Exemplo

```csharp
public sealed class EntryResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
}
```

### Critério de aceite

- [ ] A API deve retornar responses específicas.
- [ ] A API não deve retornar entidades diretamente.
- [ ] O contrato deve ficar mais limpo para o front-end.
- [ ] Responses devem esconder dados sensíveis ou internos.

---

## TASK 3.3 - Criar padrão de resposta da API

### Ação

Criar um wrapper padrão para respostas.

### Exemplo

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}
```

### Critério de aceite

- [ ] Todas as respostas principais devem seguir o mesmo padrão.
- [ ] Erros devem ser retornados de forma clara.
- [ ] O front-end deve conseguir tratar sucesso e erro facilmente.
- [ ] A resposta deve ser consistente entre controllers diferentes.

---

# 4. Regras de Negócio e Consistência

## TASK 4.1 - Criar transação para Entry + Wallet

### Problema

Ao criar, editar ou deletar um lançamento, o saldo da carteira também é alterado.

Se uma operação falhar no meio, o banco pode ficar inconsistente.

### Ação

Criar controle transacional com Dapper.

### Fluxos que precisam de transação

- Criar lançamento + atualizar saldo da carteira.
- Editar lançamento + recalcular saldo da carteira.
- Deletar lançamento + reverter saldo da carteira.

### Estrutura sugerida

```text
Zeno.Infrastructure.SQL
  Transactions
    IUnitOfWork.cs
    UnitOfWork.cs
```

### Exemplo conceitual

```csharp
await _unitOfWork.BeginAsync();

try
{
    await _entryRepository.CreateAsync(entry, _unitOfWork.Transaction);
    await _walletRepository.UpdateBalanceAsync(walletId, value, _unitOfWork.Transaction);

    await _unitOfWork.CommitAsync();
}
catch
{
    await _unitOfWork.RollbackAsync();
    throw;
}
```

### Critério de aceite

- [ ] Criação de lançamento deve ser atômica.
- [ ] Atualização de lançamento deve ser atômica.
- [ ] Remoção de lançamento deve ser atômica.
- [ ] Se atualizar o saldo falhar, o lançamento não deve ser salvo.
- [ ] Se salvar o lançamento falhar, o saldo não deve ser alterado.

---

## TASK 4.2 - Melhorar cálculo de saldo da carteira

### Ação

Revisar a regra de atualização do saldo.

### Regras esperadas

```text
Income aumenta saldo.
Expense diminui saldo.
Ao editar um lançamento, primeiro reverte o valor antigo e depois aplica o novo.
Ao deletar um lançamento, reverte o impacto dele no saldo.
```

### Critério de aceite

- [ ] Criar receita deve aumentar o saldo.
- [ ] Criar despesa deve diminuir o saldo.
- [ ] Editar receita para despesa deve recalcular corretamente.
- [ ] Editar despesa para receita deve recalcular corretamente.
- [ ] Editar valor de lançamento deve recalcular corretamente.
- [ ] Deletar lançamento deve reverter corretamente o saldo.

---

## TASK 4.3 - Criar idempotência nos endpoints POST

### Problema

Se o usuário clicar duas vezes em salvar, o sistema pode criar lançamentos duplicados.

### Ação

Implementar `Idempotency-Key`.

### Header esperado

```http
POST /api/entries
Idempotency-Key: user-123-entry-market-2026-05-05
```

### Tabela sugerida

```sql
CREATE TABLE idempotency_keys (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    idempotency_key VARCHAR(200) NOT NULL,
    request_hash VARCHAR(500),
    response_body TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_idempotency_user_key UNIQUE(user_id, idempotency_key)
);
```

### Critério de aceite

- [ ] Requisições POST devem aceitar `Idempotency-Key`.
- [ ] Se a mesma chave for enviada de novo, o sistema não deve criar outro registro.
- [ ] A resposta anterior pode ser retornada novamente.
- [ ] A chave deve ser única por usuário.
- [ ] Requisições com a mesma chave mas payload diferente devem ser rejeitadas.

---

# 5. Refatorações nos Services

## TASK 5.1 - Remover uso de IServiceProvider dentro dos services

### Problema

Buscar dependência manualmente com `IServiceProvider` deixa o código menos testável.

### Ação

Injetar validators e dependências diretamente no construtor.

### Antes

```csharp
var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
```

### Depois

```csharp
public EntryService(
    IValidator<Entry> entryValidator,
    IValidator<GetEntriesByMonthQuery> getEntriesValidator,
    IEntryRepository entryRepository,
    IWalletRepository walletRepository)
{
    _entryValidator = entryValidator;
    _getEntriesValidator = getEntriesValidator;
    _entryRepository = entryRepository;
    _walletRepository = walletRepository;
}
```

### Critério de aceite

- [ ] Services não devem depender de `IServiceProvider`.
- [ ] Dependências devem estar explícitas no construtor.
- [ ] Testes unitários devem ficar mais fáceis de criar.
- [ ] Nenhum service deve resolver dependência manualmente sem necessidade.

---

## TASK 5.2 - Separar responsabilidades do HomeService

### Problema

O `HomeService` concentra muitas responsabilidades.

### Ação

Separar em services menores.

### Estrutura sugerida

```text
Zeno.Application
  Services
    HomeService.cs
    HomeMemberService.cs
    HomeExpenseService.cs
    HomeSplitService.cs
    HomeBudgetService.cs
```

### Responsabilidades

#### HomeService

- Criar casa.
- Editar casa.
- Buscar casa.
- Remover casa.

#### HomeMemberService

- Adicionar membro.
- Remover membro.
- Atualizar salário do membro.
- Listar membros.

#### HomeExpenseService

- Criar despesa da casa.
- Editar despesa da casa.
- Remover despesa da casa.
- Listar despesas da casa.

#### HomeSplitService

- Calcular divisão por salário.
- Calcular divisão igualitária.
- Calcular quanto cada membro deve pagar.

#### HomeBudgetService

- Calcular orçamento da casa.
- Verificar alertas.
- Aplicar regra 50/30/20.

### Critério de aceite

- [ ] `HomeService` deve ficar menor.
- [ ] Cada service deve ter uma responsabilidade clara.
- [ ] Regras de split devem ficar isoladas.
- [ ] Regras de orçamento devem ficar isoladas.
- [ ] Services devem ser fáceis de testar separadamente.

---

# 6. Novas Funcionalidades

## 6.1 Dashboard Mensal

### TASK 6.1.1 - Criar endpoint de dashboard mensal

### Objetivo

Criar um endpoint que mostre a situação financeira do usuário no mês.

### Endpoint

```http
GET /api/dashboard?month=5&year=2026
```

### Retorno esperado

```json
{
  "totalIncome": 11917.97,
  "totalExpenses": 6500.00,
  "currentBalance": 5417.97,
  "needsLimit": 5958.98,
  "wantsLimit": 3575.39,
  "savingsLimit": 2383.59,
  "biggestExpenseCategory": "Grocery",
  "isOverNeedsBudget": true
}
```

### Critério de aceite

- [ ] Deve retornar total de receitas do mês.
- [ ] Deve retornar total de despesas do mês.
- [ ] Deve retornar saldo atual.
- [ ] Deve calcular limites da regra 50/30/20.
- [ ] Deve retornar maior categoria de gasto.
- [ ] Deve indicar se o usuário passou do orçamento.

---

### TASK 6.1.2 - Criar resumo por categoria

### Endpoint

```http
GET /api/dashboard/categories?month=5&year=2026
```

### Retorno esperado

```json
[
  {
    "category": "Grocery",
    "total": 1200.00,
    "percentage": 18.46
  },
  {
    "category": "Restaurant",
    "total": 850.00,
    "percentage": 13.07
  }
]
```

### Critério de aceite

- [ ] Deve agrupar gastos por categoria.
- [ ] Deve calcular percentual de cada categoria no total gasto.
- [ ] Deve ordenar da maior despesa para a menor.
- [ ] Deve considerar apenas dados do usuário autenticado.

---

## 6.2 Gastos Recorrentes

### TASK 6.2.1 - Criar entidade RecurringExpense

### Objetivo

Permitir que o usuário cadastre despesas fixas mensais.

### Exemplos

```text
Aluguel
Internet
Academia
Financiamento
Spotify
Seguro
```

### Entidade sugerida

```csharp
public class RecurringExpense
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid WalletId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public int DayOfMonth { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

### Critério de aceite

- [ ] Deve ser possível cadastrar uma despesa recorrente.
- [ ] Deve ser possível ativar/desativar uma despesa recorrente.
- [ ] Deve ser possível definir o dia do mês.
- [ ] Deve estar vinculada a uma carteira.
- [ ] Valor deve ser maior que zero.

---

### TASK 6.2.2 - Criar endpoints de gastos recorrentes

### Endpoints

```http
POST /api/recurring-expenses
GET /api/recurring-expenses
GET /api/recurring-expenses/{id}
PUT /api/recurring-expenses/{id}
DELETE /api/recurring-expenses/{id}
```

### Critério de aceite

- [ ] Criar gasto recorrente.
- [ ] Listar gastos recorrentes do usuário.
- [ ] Buscar gasto recorrente por ID.
- [ ] Atualizar gasto recorrente.
- [ ] Desativar ou remover gasto recorrente.

---

### TASK 6.2.3 - Criar processamento mensal dos gastos recorrentes

### Objetivo

Gerar automaticamente os lançamentos do mês com base nos gastos recorrentes ativos.

### Regra

```text
Todo mês, para cada RecurringExpense ativo, criar um Entry do tipo Expense.
```

### Critério de aceite

- [ ] O sistema deve gerar despesa automaticamente.
- [ ] Não deve duplicar a despesa no mesmo mês.
- [ ] Deve usar idempotência ou controle de processamento.
- [ ] Deve registrar data de processamento.
- [ ] Deve permitir reprocessamento seguro.

---

## 6.3 Metas Financeiras

### TASK 6.3.1 - Criar entidade FinancialGoal

### Objetivo

Permitir que o usuário defina metas financeiras.

### Exemplos

```text
Reserva de emergência
Entrada do apartamento
Viagem
Quitar dívida
Comprar computador
```

### Entidade sugerida

```csharp
public class FinancialGoal
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal TargetAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public DateOnly TargetDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

### Critério de aceite

- [ ] Usuário deve conseguir criar uma meta.
- [ ] Usuário deve informar valor alvo.
- [ ] Usuário deve informar valor atual.
- [ ] Usuário deve informar data alvo.
- [ ] Valor alvo deve ser maior que zero.
- [ ] Valor atual não pode ser negativo.

---

### TASK 6.3.2 - Criar simulação da meta

### Endpoint

```http
GET /api/goals/{id}/simulation
```

### Retorno esperado

```json
{
  "targetAmount": 10000,
  "currentAmount": 1500,
  "remainingAmount": 8500,
  "monthsRemaining": 8,
  "requiredMonthlySaving": 1062.50
}
```

### Critério de aceite

- [ ] Deve calcular valor restante.
- [ ] Deve calcular meses restantes.
- [ ] Deve calcular quanto guardar por mês.
- [ ] Deve retornar alerta caso a data alvo já tenha passado.
- [ ] Deve retornar alerta caso a meta já tenha sido alcançada.

---

## 6.4 Dívidas

### TASK 6.4.1 - Criar entidade Debt

### Objetivo

Permitir que o usuário registre dívidas e acompanhe o plano de pagamento.

### Entidade sugerida

```csharp
public class Debt
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal MonthlyPayment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public decimal RemainingAmount => TotalAmount - PaidAmount;
}
```

### Critério de aceite

- [ ] Usuário deve conseguir cadastrar uma dívida.
- [ ] Usuário deve informar valor total.
- [ ] Usuário deve informar valor já pago.
- [ ] Usuário deve informar pagamento mensal planejado.
- [ ] Valor pago não pode ser maior que o valor total.
- [ ] Pagamento mensal não pode ser negativo.

---

### TASK 6.4.2 - Criar simulação de quitação da dívida

### Endpoint

```http
GET /api/debts/{id}/payoff-simulation
```

### Retorno esperado

```json
{
  "totalAmount": 4200,
  "paidAmount": 600,
  "remainingAmount": 3600,
  "monthlyPayment": 600,
  "estimatedMonthsToPayOff": 6
}
```

### Critério de aceite

- [ ] Deve calcular valor restante.
- [ ] Deve calcular quantidade estimada de meses.
- [ ] Deve lidar com pagamento mensal zerado ou inválido.
- [ ] Deve indicar quando a dívida já estiver quitada.

---

### TASK 6.4.3 - Criar resumo geral de dívidas

### Endpoint

```http
GET /api/debts/summary
```

### Retorno esperado

```json
{
  "totalDebt": 10000,
  "totalPaid": 2500,
  "totalRemaining": 7500,
  "averageMonthlyPayment": 1200,
  "estimatedMonthsToBecomeDebtFree": 7
}
```

### Critério de aceite

- [ ] Deve somar todas as dívidas do usuário.
- [ ] Deve calcular total pago.
- [ ] Deve calcular total restante.
- [ ] Deve estimar prazo geral para quitar as dívidas.
- [ ] Deve considerar apenas dívidas do usuário autenticado.

---

## 6.5 Categorias Personalizadas

### TASK 6.5.1 - Transformar categorias fixas em tabela

### Problema

Categorias em enum limitam muito o produto.

### Ação

Criar tabela de categorias.

### SQL sugerido

```sql
CREATE TABLE categories (
    id UUID PRIMARY KEY,
    user_id UUID NULL,
    name VARCHAR(100) NOT NULL,
    type INT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

### Regras

```text
Categorias globais podem ter user_id NULL.
Categorias personalizadas devem ter user_id preenchido.
```

### Critério de aceite

- [ ] Usuário deve conseguir criar categoria personalizada.
- [ ] Sistema deve ter categorias padrão.
- [ ] Entry deve referenciar CategoryId.
- [ ] Não depender apenas de enum para categoria.
- [ ] Categorias globais não devem ser editadas por usuários comuns.

---

### TASK 6.5.2 - Criar endpoints de categorias

### Endpoints

```http
POST /api/categories
GET /api/categories
PUT /api/categories/{id}
DELETE /api/categories/{id}
```

### Critério de aceite

- [ ] Criar categoria.
- [ ] Listar categorias padrão e personalizadas.
- [ ] Editar categoria do usuário.
- [ ] Remover ou desativar categoria do usuário.
- [ ] Impedir remoção de categoria global do sistema.

---

## 6.6 Regras Automáticas de Categoria

### TASK 6.6.1 - Criar entidade CategoryRule

### Objetivo

Permitir que o sistema sugira ou aplique categoria automaticamente com base na descrição.

### Exemplos

```text
"ifood" => Alimentação
"uber" => Transporte
"spotify" => Assinaturas
"petz" => Pet
```

### SQL sugerido

```sql
CREATE TABLE category_rules (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    keyword VARCHAR(100) NOT NULL,
    category_id UUID NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

### Critério de aceite

- [ ] Usuário deve conseguir criar regra.
- [ ] Regra deve ter uma palavra-chave.
- [ ] Regra deve estar ligada a uma categoria.
- [ ] Regra deve pertencer a um usuário.
- [ ] Palavra-chave não deve ser vazia.

---

### TASK 6.6.2 - Aplicar regra automática ao criar lançamento

### Regra

Quando o usuário criar um lançamento sem categoria, o sistema deve procurar uma regra pela descrição.

### Exemplo

```text
Descrição: "Compra no mercado Assaí"
Regra encontrada: "mercado"
Categoria aplicada: Mercado
```

### Critério de aceite

- [ ] Se encontrar uma regra, aplicar categoria automaticamente.
- [ ] Se não encontrar regra, manter categoria nula ou padrão.
- [ ] A busca deve ignorar maiúsculas/minúsculas.
- [ ] Deve funcionar com parte da descrição.
- [ ] Deve priorizar regras do próprio usuário.

---

# 7. Testes

## TASK 7.1 - Criar projeto de testes

### Estrutura sugerida

```text
Zeno.Tests
  UnitTests
  IntegrationTests
```

### Comandos

```bash
dotnet new xunit -n Zeno.Tests
dotnet sln add Zeno.Tests/Zeno.Tests.csproj

dotnet add Zeno.Tests/Zeno.Tests.csproj reference Zeno.Application/Zeno.Application.csproj
dotnet add Zeno.Tests/Zeno.Tests.csproj reference Zeno.Domain/Zeno.Domain.csproj
```

### Critério de aceite

- [ ] Projeto de testes deve estar na solution.
- [ ] Deve ser possível rodar `dotnet test`.
- [ ] Pipeline futuro deve conseguir executar os testes.
- [ ] Testes devem rodar localmente sem depender de ambiente externo complexo.

---

## TASK 7.2 - Criar testes para EntryService

### Cenários

```text
Criar receita deve aumentar saldo.
Criar despesa deve diminuir saldo.
Editar lançamento deve recalcular saldo.
Deletar lançamento deve reverter saldo.
Não deve permitir valor menor ou igual a zero.
Não deve permitir lançamento sem título.
```

### Critério de aceite

- [ ] Testes devem validar regras principais de lançamento.
- [ ] Testes devem cobrir cenários de erro.
- [ ] Testes devem cobrir atualização do saldo.
- [ ] Testes devem validar se a transação evita inconsistência.

---

## TASK 7.3 - Criar testes para regra 50/30/20

### Cenários

```text
Calcular 50% para necessidades.
Calcular 30% para desejos.
Calcular 20% para investimentos/reserva.
Identificar quando usuário passou do limite.
Identificar quando usuário está dentro do limite.
```

### Critério de aceite

- [ ] Regra deve ser validada isoladamente.
- [ ] Testes devem ser independentes do banco.
- [ ] A regra deve ficar fácil de reutilizar no dashboard.

---

## TASK 7.4 - Criar testes para HomeSplitService

### Cenários

```text
Dividir despesa igualmente entre membros.
Dividir despesa proporcionalmente ao salário.
Calcular quanto cada membro deve pagar.
Tratar membro sem salário configurado.
```

### Critério de aceite

- [ ] Split igualitário deve funcionar.
- [ ] Split proporcional deve funcionar.
- [ ] Erros devem ser tratados com clareza.
- [ ] Cálculo deve ser validado com valores decimais.

---

# 8. Banco de Dados e Migrations

## TASK 8.1 - Organizar scripts SQL versionados

### Estrutura sugerida

```text
Zeno.Infrastructure.SQL
  Database
    Scripts
      001_create_users.sql
      002_create_wallets.sql
      003_create_entries.sql
      004_create_homes.sql
      005_create_home_members.sql
      006_create_home_expenses.sql
      007_create_recurring_expenses.sql
      008_create_financial_goals.sql
      009_create_debts.sql
      010_create_categories.sql
      011_create_category_rules.sql
```

### Critério de aceite

- [ ] Scripts devem estar versionados.
- [ ] Scripts devem ter ordem clara.
- [ ] Ambiente novo deve conseguir criar banco do zero.
- [ ] Scripts devem ser compatíveis com PostgreSQL.

---

## TASK 8.2 - Avaliar uso do DbUp

### Objetivo

Automatizar execução dos scripts SQL.

### Pacote

```bash
dotnet add Zeno.Infrastructure.SQL package DbUp
```

### Critério de aceite

- [ ] Scripts SQL devem rodar automaticamente.
- [ ] Scripts já executados não devem rodar de novo.
- [ ] Banco deve ser inicializado com segurança no ambiente local.
- [ ] Aplicação deve falhar de forma clara se migration der erro.

---

# 9. Observabilidade e Saúde da API

## TASK 9.1 - Adicionar Health Checks

### Endpoint

```http
GET /health
```

### O que verificar

```text
API online
Banco PostgreSQL acessível
```

### Critério de aceite

- [ ] Endpoint `/health` deve responder.
- [ ] Deve validar conexão com banco.
- [ ] Deve ser útil para Railway, Docker ou Azure.
- [ ] Deve retornar erro quando banco estiver indisponível.

---

## TASK 9.2 - Melhorar logs estruturados

### Ação

Adicionar logs úteis nas principais operações.

### Exemplos

```text
Usuário criou lançamento.
Usuário atualizou carteira.
Processamento de salário recorrente iniciado.
Erro ao atualizar saldo da carteira.
```

### Critério de aceite

- [ ] Logs devem ajudar no debug.
- [ ] Logs não devem expor secrets.
- [ ] Logs devem incluir informações úteis, como `UserId`, `EntryId` e `WalletId`.
- [ ] Logs de erro devem manter stack trace.

---

# 10. Paginação e Filtros

## TASK 10.1 - Adicionar paginação nos endpoints de listagem

### Endpoints candidatos

```http
GET /api/entries
GET /api/wallets
GET /api/homes/{id}/expenses
GET /api/categories
```

### Query params

```http
GET /api/entries?page=1&pageSize=20
```

### Response sugerida

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 100,
  "totalPages": 5
}
```

### Critério de aceite

- [ ] Endpoints de listagem devem aceitar `page`.
- [ ] Endpoints de listagem devem aceitar `pageSize`.
- [ ] Deve retornar total de itens.
- [ ] Deve evitar retornar listas gigantes.
- [ ] Deve definir limite máximo de `pageSize`.

---

## TASK 10.2 - Adicionar filtros em lançamentos

### Filtros sugeridos

```http
GET /api/entries?month=5&year=2026&type=Expense&categoryId=...
```

### Critério de aceite

- [ ] Filtrar por mês.
- [ ] Filtrar por ano.
- [ ] Filtrar por tipo.
- [ ] Filtrar por categoria.
- [ ] Filtrar por carteira.
- [ ] Filtros devem considerar apenas dados do usuário autenticado.

---

# 11. Autenticação

## TASK 11.1 - Implementar Refresh Token

### Problema

Hoje o JWT pode expirar e o usuário precisaria logar novamente.

### Ação

Criar refresh token.

### Tabela sugerida

```sql
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    token VARCHAR(500) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    revoked_at TIMESTAMP NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

### Endpoints

```http
POST /api/auth/login
POST /api/auth/refresh-token
POST /api/auth/logout
```

### Critério de aceite

- [ ] Login deve gerar access token e refresh token.
- [ ] Refresh token deve gerar novo access token.
- [ ] Logout deve revogar refresh token.
- [ ] Refresh token expirado não deve ser aceito.
- [ ] Refresh token revogado não deve ser aceito.

---

# 12. CI/CD

## TASK 12.1 - Criar GitHub Actions para build e testes

### Arquivo

```text
.github/workflows/dotnet.yml
```

### Exemplo

```yaml
name: Zeno API CI

on:
  push:
    branches: [ "master" ]
  pull_request:
    branches: [ "master" ]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore Zeno.slnx

      - name: Build
        run: dotnet build Zeno.slnx --configuration Release --no-restore

      - name: Test
        run: dotnet test Zeno.slnx --configuration Release --no-build
```

### Critério de aceite

- [ ] Pipeline deve rodar no push.
- [ ] Pipeline deve rodar no pull request.
- [ ] Pipeline deve executar restore.
- [ ] Pipeline deve executar build.
- [ ] Pipeline deve executar testes.
- [ ] Pipeline deve falhar se algum teste quebrar.

---

# 13. Ordem Recomendada de Execução

## Fase 1 - Correções urgentes

- [ ] TASK 1.1 - Remover secrets do appsettings.json
- [ ] TASK 1.2 - Configurar User Secrets
- [ ] TASK 1.3 - Remover logs sensíveis do Program.cs
- [ ] TASK 2.1 - Corrigir README.md
- [ ] TASK 2.2 - Adicionar seção de arquitetura no README

---

## Fase 2 - Base técnica

- [ ] TASK 3.1 - Criar DTOs para requests
- [ ] TASK 3.2 - Criar DTOs para responses
- [ ] TASK 3.3 - Criar padrão de resposta da API
- [ ] TASK 4.1 - Criar transação para Entry + Wallet
- [ ] TASK 4.2 - Melhorar cálculo de saldo da carteira
- [ ] TASK 5.1 - Remover IServiceProvider dos services

---

## Fase 3 - Refatoração

- [ ] TASK 5.2 - Separar responsabilidades do HomeService
- [ ] TASK 10.1 - Adicionar paginação
- [ ] TASK 10.2 - Adicionar filtros em lançamentos

---

## Fase 4 - Funcionalidades novas

- [ ] TASK 6.1.1 - Criar dashboard mensal
- [ ] TASK 6.1.2 - Criar resumo por categoria
- [ ] TASK 6.2.1 - Criar entidade RecurringExpense
- [ ] TASK 6.2.2 - Criar endpoints de gastos recorrentes
- [ ] TASK 6.2.3 - Criar processamento mensal dos gastos recorrentes
- [ ] TASK 6.3.1 - Criar entidade FinancialGoal
- [ ] TASK 6.3.2 - Criar simulação da meta
- [ ] TASK 6.4.1 - Criar entidade Debt
- [ ] TASK 6.4.2 - Criar simulação de quitação da dívida
- [ ] TASK 6.4.3 - Criar resumo geral de dívidas
- [ ] TASK 6.5.1 - Transformar categorias fixas em tabela
- [ ] TASK 6.5.2 - Criar endpoints de categorias
- [ ] TASK 6.6.1 - Criar entidade CategoryRule
- [ ] TASK 6.6.2 - Aplicar regra automática ao criar lançamento

---

## Fase 5 - Testes e maturidade

- [ ] TASK 7.1 - Criar projeto de testes
- [ ] TASK 7.2 - Criar testes para EntryService
- [ ] TASK 7.3 - Criar testes para regra 50/30/20
- [ ] TASK 7.4 - Criar testes para HomeSplitService
- [ ] TASK 8.1 - Organizar scripts SQL versionados
- [ ] TASK 8.2 - Avaliar uso do DbUp
- [ ] TASK 9.1 - Adicionar Health Checks
- [ ] TASK 9.2 - Melhorar logs estruturados
- [ ] TASK 11.1 - Implementar Refresh Token
- [ ] TASK 12.1 - Criar GitHub Actions para build e testes

---

# 14. Prioridade Final

Se for para escolher somente as tasks mais importantes agora, seguir esta ordem:

1. Remover secrets do GitHub.
2. Corrigir README.
3. Criar DTOs para request/response.
4. Adicionar transação entre Entry e Wallet.
5. Corrigir cálculo de saldo.
6. Criar dashboard mensal.
7. Criar gastos recorrentes.
8. Criar testes.
9. Criar metas financeiras.
10. Criar dívidas.
11. Criar categorias personalizadas.
12. Criar regras automáticas de categoria.
13. Adicionar refresh token.
14. Criar CI/CD.

---

# 15. Ideia central do Zeno

O Zeno deve responder perguntas reais do usuário:

```text
Quanto eu ganho?
Quanto eu gasto?
Quanto sobra?
Onde estou gastando mais?
Estou passando do limite?
Quanto preciso guardar?
Quanto tempo falta para sair das dívidas?
Quanto posso gastar sem ficar negativo?
```

A API não deve ser apenas um CRUD financeiro.

Ela deve ajudar o usuário a entender a própria vida financeira.
