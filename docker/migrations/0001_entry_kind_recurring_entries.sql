-- Migration: adds Entry.Kind, Wallet.DailyBudget and replaces Salaries/RecurringExpenses with a unified RecurrentEntries table.
-- Run manually against the already-deployed database (init.sql only runs on a fresh volume).

-- 1. Entries.Kind: Entrada=0, Saida=1, Diario=2, Economia=3, Cartao=4.
ALTER TABLE entries ADD COLUMN IF NOT EXISTS kind INT NOT NULL DEFAULT 1;

UPDATE entries
SET kind = CASE
    WHEN type = 0 THEN 0 -- Credit -> Entrada
    WHEN type = 1 AND category IN (4, 5) THEN 1 -- Debit + Utilities/Transportation -> Saida
    WHEN type = 1 AND category IN (1, 2, 3) THEN 2 -- Debit + Restaurant/Grocery/Entertainment -> Diario
    ELSE 1 -- default debit -> Saida
END;

-- 2. Wallets.DailyBudget for the daily-spend forecast (item 8).
ALTER TABLE wallets ADD COLUMN IF NOT EXISTS dailybudget NUMERIC(18,2) NULL;

-- 3. RecurrentEntries unifies Salaries and RecurringExpenses (generalized to income and expenses, wallet-based).
CREATE TABLE IF NOT EXISTS recurrententries (
    id UUID PRIMARY KEY,
    userid UUID NOT NULL,
    walletid UUID NOT NULL,
    title VARCHAR(200) NOT NULL,
    value NUMERIC(18,2) NOT NULL,
    type INT NOT NULL DEFAULT 1,
    kind INT NOT NULL DEFAULT 0,
    category INT NOT NULL DEFAULT 0,
    dayofmonth INT NOT NULL,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    createdat TIMESTAMP NOT NULL,
    lastprocessedat TIMESTAMP NULL,
    CONSTRAINT fk_recurrententries_users FOREIGN KEY (userid) REFERENCES users(id),
    CONSTRAINT fk_recurrententries_wallets FOREIGN KEY (walletid) REFERENCES wallets(id)
);

INSERT INTO recurrententries (id, userid, walletid, title, value, type, kind, category, dayofmonth, active, createdat, lastprocessedat)
SELECT s.id, s.userid, a.walletid, COALESCE(s.description, 'Salário'), s.amount, 0, 0, 6, s.dayofmonth, s.active, s.createdat, s.lastprocessedat
FROM salaries s
INNER JOIN accounts a ON s.accountid = a.id
ON CONFLICT (id) DO NOTHING;

INSERT INTO recurrententries (id, userid, walletid, title, value, type, kind, category, dayofmonth, active, createdat, lastprocessedat)
SELECT id, userid, walletid, title, value, 1, 1, 0, dayofmonth, isactive, createdat, lastprocessedat
FROM recurringexpenses
ON CONFLICT (id) DO NOTHING;

DROP TABLE IF EXISTS salaries;
DROP TABLE IF EXISTS recurringexpenses;
