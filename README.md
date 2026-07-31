# PsicViewer

Aplicativo de monitoramento de humor e suporte psicológico, conectando **Paciente** e **Psicólogo** — desenvolvido como Trabalho de Conclusão de Curso.

## 🧱 Tecnologias

- **.NET 8 MAUI** — aplicativo mobile (Android)
- **ASP.NET Core Web API** — backend
- **MySQL** (via Entity Framework Core / Pomelo) — persistência de dados
- **SignalR** — chat em tempo real
- **Clean Architecture** — Core / Application / Infrastructure / Api

A API está hospedada no **Azure** (App Service + MySQL Flexible Server), então não é necessário rodar nenhum servidor localmente para testar o app.

## ▶️ Como executar

### Opção 1 — Testar rápido (recomendado)

1. Clona o repositório
2. Abre a solução **`MauiApp1.sln`** no Visual Studio 2022 (com a carga de trabalho **.NET Multi-platform App UI development** instalada)
3. No **Solution Explorer** (painel lateral com a lista de projetos), clica com o **botão direito** em cima do projeto **`MauiApp1`** → **Set as Startup Project** (definir como projeto de inicialização). Ele deve ficar em **negrito** na lista — é assim que se confirma que está certo. A solução tem 5 projetos ao todo (`MauiApp1`, `PsicViewer.Api`, `PsicViewer.Core`, `PsicViewer.Infrastructure`, `PsicViewer.Application`), mas só o `MauiApp1` precisa rodar — os outros são bibliotecas que ele usa por baixo dos panos, não têm um "programa" próprio pra executar.
4. Na **barra de ferramentas** do topo, ao lado do botão verde de play (▶️), tem dois seletores:
   - O primeiro mostra o **framework de destino** — escolhe **`net8.0-android`**
   - O segundo mostra o **dispositivo/emulador** — escolhe um emulador Android já configurado, ou um celular físico conectado por USB com a Depuração USB ativada (nas Opções do Desenvolvedor do Android)
5. Clica no botão verde ▶️ (ou aperta **F5**) — o Visual Studio compila, instala o app no emulador/celular e abre ele automaticamente. O app já se conecta à API hospedada no Azure, sem nenhuma configuração adicional.

> Se não tiver nenhum emulador Android configurado ainda, o próprio seletor de dispositivo tem uma opção pra abrir o **Android Device Manager** e criar um novo direto pelo Visual Studio.

### Opção 2 — Rodar a API localmente (para desenvolvimento)

1. Configura um servidor MySQL local
2. Ajusta a connection string em `PsicViewer.Api/appsettings.json`
3. Define **`PsicViewer.Api`** como Startup Project (mesmo processo do passo 3 acima, só que nesse projeto) e roda ele (ele cria o banco automaticamente na primeira execução)
4. No `MauiApp1/Services/ApiConfig.cs`, troca a URL pelo IP da sua máquina na rede local (`http://SEU_IP:5299`) — necessário porque o celular acessa a API por IP de rede, não por `localhost`
5. Define `MauiApp1` como Startup Project de novo e roda ele normalmente

## 👤 Contas de teste

Pra explorar o app sem precisar cadastrar do zero:

| Perfil | E-mail | Senha |
|---|---|---|
| Psicóloga | `ana@teste.com` | `123456` |
| Paciente | `pedro@teste.com` | `123456` |
| Paciente | `julia@teste.com` | `123456` |

Ana já está vinculada a Pedro e Julia, com um questionário de humor ativo — os dois pacientes têm respostas registradas nos últimos 14 dias, então já dá pra ver o **Histórico** e os **Relatórios de Humor** funcionando com dados reais sem precisar gerar nada manualmente.

## 📱 Como usar

### Como Paciente

1. Faz login (ou cria uma conta em "Sou paciente")
2. Em **Chat**, toca em "Buscar Psicólogo" pra encontrar e solicitar vínculo com um profissional (ou aceita um convite, se o psicólogo te procurar primeiro)
3. Em **Questionários**, responde as perguntas do dia — pode ser em escala (rostinhos), texto livre ou múltipla escolha, com espaço pra observações em texto ou áudio
4. Acompanha suas próprias respostas em **Histórico**
5. Conversa com seu psicólogo pelo **Chat** — texto, áudio, foto ou documento

### Como Psicólogo

1. Faz login (ou cria uma conta em "Sou psicólogo")
2. Em **Pacientes**, busca e convida pacientes, ou aceita solicitações recebidas
3. Em **Questionários**, cria questionários personalizados (perguntas de escala, texto ou múltipla escolha, cada uma com seu horário de notificação) e vincula aos pacientes
4. Acompanha o **Histórico** de respostas de cada paciente, e gera **Relatórios de Humor** com gráficos comparando diferentes perguntas ao longo do tempo
5. Ao ver uma resposta nova (via notificação no sino), pode responder com um **Feedback** (texto ou áudio), que aparece pro paciente direto no chat, citando a pergunta e resposta original

## 🏗️ Estrutura do projeto

```
PsicViewer.Core            → Entidades e interfaces de domínio
PsicViewer.Application      → Casos de uso
PsicViewer.Infrastructure    → EF Core, repositórios (MySQL)
PsicViewer.Api               → API (SignalR + endpoints REST)
MauiApp1                    → Aplicativo mobile (MVVM)
```
