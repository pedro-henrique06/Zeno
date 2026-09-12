-- Migration: tabelas de push (tokens de aparelho + preferencia de resumo diario).
-- Rodar manualmente no banco ja implantado (init.sql so roda em volume novo).

-- 1. Tokens de push por aparelho. O token e unico no sistema: se o mesmo aparelho
--    trocar de usuario (logout/login), o registro e reatribuido em vez de duplicado.
CREATE TABLE IF NOT EXISTS devicetokens (
    id UUID PRIMARY KEY,
    userid UUID NOT NULL,
    token VARCHAR(500) NOT NULL,
    platform INT NOT NULL DEFAULT 0,
    isactive BOOLEAN NOT NULL DEFAULT TRUE,
    createdat TIMESTAMP NOT NULL,
    lastseenat TIMESTAMP NOT NULL,
    CONSTRAINT uq_devicetokens_token UNIQUE (token),
    CONSTRAINT fk_devicetokens_users FOREIGN KEY (userid) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_devicetokens_user ON devicetokens(userid, isactive);

-- 2. Preferencia de notificacao por usuario. sendhour e interpretado no fuso de timezoneid;
--    lastsenton guarda o ultimo dia local enviado para nao repetir o resumo.
CREATE TABLE IF NOT EXISTS notificationpreferences (
    userid UUID PRIMARY KEY,
    dailyenabled BOOLEAN NOT NULL DEFAULT FALSE,
    sendhour INT NOT NULL DEFAULT 9,
    timezoneid VARCHAR(100) NOT NULL DEFAULT 'America/Sao_Paulo',
    lastsenton DATE NULL,
    createdat TIMESTAMP NOT NULL,
    updatedat TIMESTAMP NOT NULL,
    CONSTRAINT fk_notificationpreferences_users FOREIGN KEY (userid) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_notificationpreferences_enabled ON notificationpreferences(dailyenabled);
