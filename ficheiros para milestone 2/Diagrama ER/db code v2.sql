-- 🔄 Recriação da base de dados
DROP DATABASE IF EXISTS consulta_plus;
CREATE DATABASE consulta_plus;
USE consulta_plus;

-- =========================
-- TABELA: Autenticacao
-- =========================
CREATE TABLE Autenticacao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    role VARCHAR(8) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE
);

-- =========================
-- TABELA: Medico
-- =========================
CREATE TABLE Medico (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome_completo VARCHAR(150) NOT NULL,
    data_nascimento DATE,
    data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP,
    criado_por INT,
    auth_id INT,
    FOREIGN KEY (auth_id)
        REFERENCES Autenticacao(id)
        ON DELETE SET NULL
        ON UPDATE CASCADE
);

-- =========================
-- TABELA: Especialidade
-- =========================
CREATE TABLE Especialidade (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL
);

-- =========================
-- TABELA: EspecialidadeMedico (N:N)
-- =========================
CREATE TABLE EspecialidadeMedico (
    medico_id INT NOT NULL,
    especialidade_id INT NOT NULL,
    PRIMARY KEY (medico_id, especialidade_id),
    FOREIGN KEY (medico_id)
        REFERENCES Medico(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    FOREIGN KEY (especialidade_id)
        REFERENCES Especialidade(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =========================
-- TABELA: SNS
-- =========================
CREATE TABLE SNS (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome_completo VARCHAR(150) NOT NULL,
    nif VARCHAR(9) UNIQUE,
    n_utente VARCHAR(9) UNIQUE,
    data_nascimento DATE,
    telemovel VARCHAR(12),
    morada VARCHAR(100)
);

-- =========================
-- TABELA: Paciente
-- =========================
CREATE TABLE Paciente (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome_completo VARCHAR(150) NOT NULL,
    nif VARCHAR(9) UNIQUE,
    n_utente VARCHAR(9) UNIQUE,
    data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP,
    data_nascimento DATE,
    telemovel VARCHAR(12),
    morada VARCHAR(100),
    auth_id INT,
    FOREIGN KEY (auth_id)
        REFERENCES Autenticacao(id)
        ON DELETE SET NULL
        ON UPDATE CASCADE,
    FOREIGN KEY (n_utente)
        REFERENCES SNS(n_utente)
        ON UPDATE CASCADE
);

-- =========================
-- TABELA: Sala
-- =========================
CREATE TABLE Sala (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL
);

-- =========================
-- TABELA: Consulta
-- =========================
CREATE TABLE Consulta (
    id INT AUTO_INCREMENT PRIMARY KEY,
    paciente_id INT,
    sala_id INT,
    medico_id INT,
    especialidade_id INT,
    data_consulta DATETIME NOT NULL,
    estado ENUM('Pendente', 'Confirmada', 'Cancelada') DEFAULT 'Pendente',
    FOREIGN KEY (paciente_id) REFERENCES Paciente(id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    FOREIGN KEY (sala_id) REFERENCES Sala(id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    FOREIGN KEY (medico_id) REFERENCES Medico(id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    FOREIGN KEY (especialidade_id) REFERENCES Especialidade(id)
        ON DELETE SET NULL ON UPDATE CASCADE
);


-- =========================
-- TABELA: Notificacao
-- =========================
CREATE TABLE Notificacao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    categoria VARCHAR(100) NOT NULL,
    descricao TEXT,
    auth_id INT,
    data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (auth_id)
        REFERENCES Autenticacao(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);
