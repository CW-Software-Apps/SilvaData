<p align="center">
  <img src="https://raw.githubusercontent.com/wagenheimer/SilvaData/master/SilvaData/Resources/AppIcon/appicon.svg" width="128" alt="SilvaData Logo" />
</p>

<h1 align="center">SilvaData 🚀</h1>

<p align="center">
  <strong>Ecossistema Avançado de Gestão e Auditoria Avícola</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-70AC3E?style=for-the-badge&logo=dotnet" alt=".NET 8.0" />
  <img src="https://img.shields.io/badge/MAUI-Multi--platform-70AC3E?style=for-the-badge&logo=dotnet" alt="MAUI" />
  <img src="https://img.shields.io/badge/CI%2FCD-Worker%20Central-gray?style=for-the-badge" alt="CI/CD" />
</p>

---

## 📋 Sobre o Projeto

O **SilvaData** é uma solução de auditoria de campo desenvolvida pela CW Software, especificamente otimizada para o setor avícola. Utilizando a robustez do **.NET MAUI**, o aplicativo permite que técnicos realizem inspeções detalhadas, gerenciem lotes e sincronizem dados críticos em ambientes de baixa conectividade.

Este projeto representa a evolução da marca **SilvaData**, apresentando uma identidade visual moderna em tons de Verde e Cinza, focada em produtividade e clareza.

---

## 🔗 Origem e Sincronização

O **SilvaData** é um fork especializado derivado do projeto [ISISys-MAUI](https://github.com/CW-Software-Apps/ISISys-MAUI). Enquanto o ISISys serve como a plataforma base genérica da CW Software, o SilvaData é a implementação customizada com branding e regras de negócio específicas para o ecossistema SilvaData.

### Estratégia de Manutenção (Cherry-pick)
Para garantir que melhorias de estabilidade e correções de bugs sejam compartilhadas entre as plataformas:
- **Fluxo de Upstream**: Correções de bugs genéricos realizados no core do **ISISys** são integrados periodicamente ao **SilvaData** via `git cherry-pick`.
- **Sincronia**: Este modelo de engenharia permite que o SilvaData evolua sua identidade visual e recursos exclusivos de forma independente, sem perder o acesso às evoluções constantes do motor principal do sistema.

---

## ✨ Funcionalidades Principais

*   **Gestão de Lotes**: Visualização completa de status, idades e métricas zootécnicas.
*   **Sincronização Inteligente**: Motor de persistência SQLite com filas de sincronização assíncronas.
*   **Dashboard Executivo**: Visão consolidada de indicadores de performance diretamente na tela inicial.
*   **Modo Offline**: Primeiro a dados, garantindo que nenhuma auditoria seja perdida em campo.

---

## 🛠 Arquitetura Técnica

Implementamos os padrões mais modernos da engenharia de software para garantir estabilidade empresarial:

*   **MVVM (Model-View-ViewModel)**: Separação rigorosa de interesses.
*   **Injeção de Dependências**: Gerenciamento modular de serviços e PageModels.
*   **Observabilidade Controlada**: Integração nativa com Sentry para telemetria de erros.
*   **Otimização de Memória**: Estratégias de GC Assurance para evitar vazamentos em execuções longas.

---

## 🚀 Pipeline de Entrega (CI/CD)

O SilvaData está totalmente integrado ao sistema de automação da CW Software:

*   **Build Automatizado**: Processamento de artefatos `.aab` e `.apk` em cada interação.
*   **Monitoramento**: Painel online disponível em [dashboard.appdeployhub.com](https://dashboard.appdeployhub.com/).
*   **Telemetria de Build**: Logs detalhados e rastreio de versão simplificado.

---

## 💻 Tech Stack

- **Framework**: .NET MAUI 8.0
- **UI Toolkit**: Syncfusion Maui Toolkit
- **Banco de Dados**: SQLite (Local) / API REST (Sync)
- **Linguagem**: C# 12

---
<p align="center">
  <em>Desenvolvido com excelência por <strong>CW Software</strong></em>
</p>
